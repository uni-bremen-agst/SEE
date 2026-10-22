using System.Text.Json;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>A3 compiler-local context and bounded recursion audit; these do not claim a production correction.</summary>
    public sealed class ExceptionFlowConstructedInvocationContextTests
    {
        private const string Fault = "public class Fault<T> : System.Exception {} ";

        [Theory]
        [InlineData("method", "public static class Api { public static void M<T>() { throw new Fault<T>(); } }", "Api.M<int>();")]
        [InlineData("containing", "public static class Api<T> { public static void M() { throw new Fault<T>(); } }", "Api<int>.M();")]
        [InlineData("both", "public static class Api<T> { public static void M<U>() { throw new Fault<System.Collections.Generic.KeyValuePair<T,U>>(); } }", "Api<int>.M<string>();")]
        [InlineData("nested", "public class Outer<T> { public class Leaf<U,V> : System.Exception {} public static class Inner<U> { public static void M<V>() { throw new Leaf<U,V>(); } } }", "Outer<int>.Inner<string>.M<bool>();")]
        [InlineData("non-generic", "public static class Api { public static void M() { throw new System.InvalidOperationException(); } }", "Api.M();")]
        public void ActualCallSiteRetainsBothDimensions_ButActualContextUsesDefinition(string scenario, string api, string call)
        {
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Fault + api + " public static class Entry { public static void Root() { " + call + " } }", "Root");
            var tree = run.Compilation.SyntaxTrees.Single();
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.ValueText == "Root");
            var model = run.Compilation.GetSemanticModel(tree);
            var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var selected = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
            var caller = new ExceptionFlowCallContext(model.GetDeclaredSymbol(root));
            var context = ExceptionFlowAnalyzer.CreateInvocationCallContext(invocation, selected, model, caller);
            var exact = RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected);
            var definition = RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected, normalizeToOriginalDefinition: true);
            Assert.Equal(scenario == "non-generic", exact.Equals(definition));
            Assert.True(SymbolEqualityComparer.Default.Equals(selected.OriginalDefinition, context.CallableSymbol));
            Assert.Equal(definition, context.CanonicalContext!.Callable);
            Assert.Empty(context.Key);
            if (scenario is "method" or "both" or "nested") { Assert.NotEmpty(exact.TypeArguments); Assert.Empty(context.CanonicalContext.Callable!.TypeArguments); }
            if (scenario is "containing" or "both" or "nested") { Assert.NotEqual(exact.ContainingType, definition.ContainingType); }
            var nodes = Nodes(run);
            Evidence("callsite-" + scenario, new
            {
                Scenario = scenario,
                Selected = exact,
                Definition = definition,
                ActualContext = context.CanonicalContext,
                Sources = nodes.SelectMany(pair => pair.Summary.Sources).Select(item => RoslynCanonicalIdentityFactory.CreateTypeIdentity(item.ExceptionType)).ToArray(),
                CurrentNodeCount = run.Graph.Count,
                ProductionCorrected = false
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DifferentConstructionsShareActualNode_WhileExactCanonicalIdentitiesDistinguishThem(bool containing)
        {
            string api = containing ? "public static class Api<T> { public static void M() { throw new Fault<T>(); } }" : "public static class Api { public static void M<T>() { throw new Fault<T>(); } }";
            string calls = containing ? "Api<int>.M(); Api<string>.M(); Api<int>.M();" : "Api.M<int>(); Api.M<string>(); Api.M<int>();";
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Fault + api + "public static class Entry { public static void Root() { " + calls + " } }", "Root");
            var edges = run.RootSummary.CallEdges;
            Assert.Equal(3, edges.Count);
            Assert.Equal(edges[0].Target, edges[1].Target);
            Assert.Equal(edges[0].Target, edges[2].Target);
            var tree = run.Compilation.SyntaxTrees.Single();
            var model = run.Compilation.GetSemanticModel(tree);
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.ValueText == "Root");
            var identities = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(item => RoslynCanonicalIdentityFactory.CreateCallableIdentity((IMethodSymbol)model.GetSymbolInfo(item).Symbol!)).ToArray();
            Assert.NotEqual(identities[0], identities[1]);
            Assert.Equal(identities[0], identities[2]);
            Evidence("cache-" + containing, new { Containing = containing, ExactIdentities = identities, ActualDistinctTargets = edges.Select(item => item.Target).Distinct().Count(), RequiredDistinctConstructions = 2, CurrentNodeCount = run.Graph.Count, ProductionCorrected = false });
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ExactConstructedKeys_ChangingTypeRecursionHasNoFiniteReuse_FiniteHarnessOnly(bool containing, bool growing)
        {
            string next = growing ? "System.Collections.Generic.List<T>" : "T";
            string api = containing
                ? "public static class Api<T> { public static void Recur() { if (Flags.Stop) throw new Fault<T>(); Api<" + next + ">.Recur(); } }"
                : "public static class Api { public static void Recur<T>() { if (Flags.Stop) throw new Fault<T>(); Recur<" + next + ">(); } }";
            string call = containing ? "Api<int>.Recur();" : "Api.Recur<int>();";
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Fault + "public static class Flags { public static bool Stop; } " + api + " public static class Entry { public static void Root() { " + call + " } }", "Root");
            var actualNode = Assert.Single(Nodes(run).Where(pair => pair.Key.Symbol.Name == "Recur"));
            Assert.Contains(actualNode.Summary.CallEdges, edge => edge.Target.Equals(actualNode.Key));
            var source = Assert.Single(actualNode.Summary.Sources);
            Assert.IsAssignableFrom<ITypeParameterSymbol>(Assert.Single(source.ExceptionType.TypeArguments));

            var tree = run.Compilation.SyntaxTrees.Single();
            var model = run.Compilation.GetSemanticModel(tree);
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(item => item.Identifier.ValueText == "Root");
            var selected = (IMethodSymbol)model.GetSymbolInfo(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
            var list = run.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!;
            var proposed = new ExceptionFlowSummaryGraph();
            var instances = new Dictionary<ExceptionFlowCallableKey, IMethodSymbol>();
            var visited = new HashSet<CanonicalCallableIdentity>();
            void Add(IMethodSymbol method)
            {
                var exact = RoslynCanonicalIdentityFactory.CreateCallableIdentity(method);
                var key = new ExceptionFlowCallableKey(method, RoslynCanonicalIdentityFactory.CreateDeterministicSortKey(exact));
                instances.TryAdd(key, method);
                proposed.GetOrAdd(key);
            }
            Add(selected);
            const int proofSteps = 32; // Only a bounded test experiment, never a production budget or fix.
            for (int step = 0; step < proofSteps; step++)
            {
                var pending = proposed.DequeuePendingOrDefault();
                if (pending == null) break;
                var instance = instances[pending];
                Assert.True(visited.Add(RoslynCanonicalIdentityFactory.CreateCallableIdentity(instance)));
                var argument = containing ? instance.ContainingType.TypeArguments.Single() : instance.TypeArguments.Single();
                var nextArgument = growing ? list.Construct(argument) : argument;
                var nextMethod = containing
                    ? instance.ContainingType.OriginalDefinition.Construct(nextArgument).GetMembers("Recur").OfType<IMethodSymbol>().Single()
                    : instance.OriginalDefinition.Construct(nextArgument);
                Add(nextMethod);
            }
            Assert.Equal(growing ? proofSteps : 1, visited.Count);
            Assert.Equal(growing ? proofSteps + 1 : 1, proposed.Count);
            Assert.Equal(growing, proposed.DequeuePendingOrDefault() != null);
            Evidence("recursion-" + containing + "-" + growing, new
            {
                Containing = containing,
                Growing = growing,
                ActualDefinitionNodeCount = run.Graph.Count,
                ActualOpenSource = RoslynCanonicalIdentityFactory.CreateTypeIdentity(source.ExceptionType),
                TestExpansionSteps = proofSteps,
                ExactContextsVisited = visited.Count,
                ExactContextsRegistered = proposed.Count,
                PendingWorkRemainsAtHarnessLimit = growing,
                Recurrence = growing ? "T[n+1] = List<T[n]>; strict argument-depth growth, no repeated exact constructed key" : "T[n+1] = T[n]; exact key reused",
                UnboundedProductRunExecuted = false,
                ProductionBudgetAdded = false,
                ProductionCorrected = false
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SupportingSourceResolverPreservesContainingConstruction_ButNotMethodArguments(bool containing)
        {
            string api = containing ? "public static class Api<T> { public static void M() { throw new Fault<T>(); } }" : "public static class Api { public static void M<T>() { throw new Fault<T>(); } }";
            var b = Compile("B", Fault + api);
            using var image = new MemoryStream();
            Assert.True(b.Emit(image).Success);
            var reference = MetadataReference.CreateFromImage(image.ToArray());
            var a = Compile("A", "public static class Entry { public static void Root() { " + (containing ? "Api<int>.M();" : "Api.M<int>();") + " } }", reference);
            var tree = a.SyntaxTrees.Single();
            var selected = (IMethodSymbol)a.GetSemanticModel(tree).GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
            var rebound = ExceptionFlowCrossCompilationResolver.ResolveMethod(selected, b);
            Assert.NotNull(rebound);
            Assert.Equal("B", rebound.ContainingAssembly.Name);
            var expected = RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected);
            var actual = RoslynCanonicalIdentityFactory.CreateCallableIdentity(rebound);
            Assert.Equal(containing, expected.Equals(actual));
            Assert.True(SymbolEqualityComparer.Default.Equals(new ExceptionFlowCallContext(rebound).CallableSymbol, rebound.OriginalDefinition));
            Evidence("cross-project-rebind-" + containing, new { Containing = containing, SelectedMetadataMethod = expected, ReboundSourceMethod = actual, Context = new ExceptionFlowCallContext(rebound).CanonicalContext, ProductionCorrected = false });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GenericBodyCatchSymbolsAlsoNeedInvocationSubstitution(bool containing)
        {
            string api = containing
                ? "public static class Api<T> { public static void M() { try { Thrower(); } catch (Fault<T>) {} } private static void Thrower() { throw new Fault<T>(); } }"
                : "public static class Api { public static void M<T>() { try { Thrower<T>(); } catch (Fault<T>) {} } private static void Thrower<T>() { throw new Fault<T>(); } }";
            string call = containing ? "Api<int>.M();" : "Api.M<int>();";
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Fault + api + " public static class Entry { public static void Root() { " + call + " } }", "Root");
            var catchTypes = Nodes(run).SelectMany(pair => pair.Summary.CallEdges).SelectMany(edge => edge.CaughtExceptionTypes).ToArray();
            var caught = Assert.Single(catchTypes);
            Assert.IsAssignableFrom<ITypeParameterSymbol>(Assert.Single(caught.TypeArguments));
            Evidence("catch-" + containing, new { Containing = containing, CollectedCatchType = RoslynCanonicalIdentityFactory.CreateTypeIdentity(caught), RequiredArgument = "System.Int32", ProductionCorrected = false });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness(bool containing)
        {
            string api = containing
                ? "public static class Api<T> { public static void Recur() { if (Flags.Stop) throw new Fault<T>(); Api<System.Collections.Generic.List<T>>.Recur(); } }"
                : "public static class Api { public static void Recur<T>() { if (Flags.Stop) throw new Fault<T>(); Recur<System.Collections.Generic.List<T>>(); } }";
            string call = containing ? "Api<int>.Recur();" : "Api.Recur<int>();";
            string source = Fault + "public static class Flags { public static bool Stop; } " + api + " public static class Entry { public static void Root() { " + call + " } }";
            var current = (CanonicalExceptionFlowAnalysisResult)typeof(ExceptionFlowCanonicalExceptionIdentityTests)
                .GetMethod("CurrentResult", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [source])!;
            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            string worker = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin", configuration, "net8.0/XMLDocNormalizer.HistoricalWorker.dll");
            var historical = await new HistoricalWorkerClient("dotnet", worker, TimeSpan.FromSeconds(30))
                .AnalyzeAsync(new WorkerAnalysisInput(source, "Entry", "Root", new()));
            Assert.True(historical.Success, historical.Failure?.Message);
            Assert.Equal(current, historical.Result);
            var entry = Assert.Single(current.Entries);
            Assert.Equal("Fault`1", entry.ExceptionType.MetadataName);
            Assert.Empty(entry.ExceptionType.TypeArguments);
            HistoricalWorkerLifetimeAssertions.Stopped(historical);
            Assert.All(historical.Identity!.LoadedRoslyn, image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical");
            Evidence("worker-recursion-" + containing, new { Containing = containing, Current = current, Historical = historical.Result, historical.Identity, historical.ProcessExit, SharedDefinitionFold = true, ConstructedCorrectnessProven = false, ProductionCorrected = false });
        }

        private static List<(ExceptionFlowCallableKey Key, ExceptionFlowSummary Summary)> Nodes(ExceptionFlowSummaryGraphTestRun run)
        {
            var nodes = new List<(ExceptionFlowCallableKey, ExceptionFlowSummary)>();
            var queue = new Queue<ExceptionFlowCallableKey>();
            var seen = new HashSet<ExceptionFlowCallableKey>();
            queue.Enqueue(run.RootKey);
            while (queue.TryDequeue(out var key))
            {
                if (!seen.Add(key)) continue;
                var summary = run.GetRequiredSummary(key);
                nodes.Add((key, summary));
                foreach (var edge in summary.CallEdges) queue.Enqueue(edge.Target);
            }
            return nodes;
        }
        private static CSharpCompilation Compile(string name, string source, params MetadataReference[] extra)
        {
            var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "/" + name + ".cs")],
                MetadataReferences.Default.Concat(extra), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return compilation;
        }
        private static void Evidence(string name, object value)
        {
            string path = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/p5o2b25a3/analysis", name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
