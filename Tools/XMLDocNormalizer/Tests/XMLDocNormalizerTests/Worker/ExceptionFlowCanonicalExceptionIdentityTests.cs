using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>A2 boundary audit: valid compiler forms and losses before and during normalization, not a production repair.</summary>
    public sealed class ExceptionFlowCanonicalExceptionIdentityTests
    {
        private const string Definitions = "#nullable enable\nusing System; namespace Shared { " +
            "public class Fault : Exception {} public class Fault<T> : Exception {} public class Fault<T,U> : Exception {} " +
            "public class Outer<T> { public class Leaf : Exception {} public class Nested<U> : Exception {} } " +
            "public class OuterOne { public class Leaf : Exception {} } public class OuterTwo { public class Leaf : Exception {} } } ";

        [Theory]
        [InlineData("Shared.Fault", "Shared.Fault", true)]
        [InlineData("Shared.Fault<int>", "Shared.Fault<int>", true)]
        [InlineData("Shared.Fault<int>", "Shared.Fault<string>", false)]
        [InlineData("Shared.Fault<int>", "Shared.Fault<int,string>", false)]
        [InlineData("Shared.Outer<int>.Leaf", "Shared.Outer<int>.Leaf", true)]
        [InlineData("Shared.Outer<int>.Leaf", "Shared.Outer<string>.Leaf", false)]
        [InlineData("Shared.Outer<int>.Nested<string>", "Shared.Outer<int>.Nested<string>", true)]
        [InlineData("Shared.OuterOne.Leaf", "Shared.OuterTwo.Leaf", false)]
        [InlineData("Shared.Fault<string?>", "Shared.Fault<string>", true)]
        [InlineData("Shared.Fault<(int X,int Y)>", "Shared.Fault<(int A,int B)>", true)]
        [InlineData("Shared.Fault<dynamic>", "Shared.Fault<object>", true)]
        [InlineData("Shared.Fault<nint>", "Shared.Fault<IntPtr>", true)]
        [InlineData("InvalidOperationException", "InvalidOperationException", true)]
        public void CompilerModel_ValidExceptionAndCatchForms(string thrownText, string caughtText, bool identityConversion)
        {
            var compilation = Compile("CompilerModel", Definitions +
                $"public static class Entry {{ public static void Root() {{ try {{ throw new {thrownText}(); }} catch ({caughtText}) {{ }} }} }}");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var thrown = (INamedTypeSymbol)model.GetTypeInfo(tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single()).Type!;
            var caught = (INamedTypeSymbol)model.GetTypeInfo(tree.GetRoot().DescendantNodes().OfType<CatchDeclarationSyntax>().Single().Type).Type!;
            Assert.Equal(identityConversion, compilation.ClassifyConversion(thrown, caught).IsIdentity);
            Evidence("compiler-" + Safe(thrownText + "-" + caughtText), new
            {
                ThrownText = thrownText,
                CaughtText = caughtText,
                IdentityConversion = identityConversion,
                SymbolEqual = SymbolEqualityComparer.Default.Equals(thrown, caught),
                CanonicalEqual = Identity(thrown).Equals(Identity(caught)),
                Thrown = Identity(thrown),
                Caught = Identity(caught),
                ExistingCatchMatches = ExceptionFlowCaughtTypeFilter.IsSameOrDerivedFrom(thrown, caught)
            });
        }

        [Fact]
        public void CompilerModel_FourIdentityFamiliesRetainDifferentOwnersArityAndContainingChains()
        {
            var a = Compile("A", Definitions);
            var b = Compile("B", Definitions);
            var aFault = a.GetTypeByMetadataName("Shared.Fault")!;
            var bFault = b.GetTypeByMetadataName("Shared.Fault")!;
            Assert.Equal(DocumentationCommentId.CreateDeclarationId(aFault), DocumentationCommentId.CreateDeclarationId(bFault));
            Assert.NotEqual(Identity(aFault), Identity(bFault));
            Assert.NotEqual(Identity(b.GetTypeByMetadataName("Shared.OuterOne+Leaf")!), Identity(b.GetTypeByMetadataName("Shared.OuterTwo+Leaf")!));
            Assert.NotEqual(Identity(b.GetTypeByMetadataName("Shared.Fault`1")!), Identity(b.GetTypeByMetadataName("Shared.Fault`2")!));
            var original = b.GetTypeByMetadataName("Shared.Outer`1")!;
            var closed = original.Construct(b.GetSpecialType(SpecialType.System_Int32));
            Assert.NotEqual(Identity(original.GetTypeMembers("Leaf").Single()), Identity(closed.GetTypeMembers("Leaf").Single()));
        }

        [Theory]
        [InlineData("closed-source", "public static void Helper() { throw new Shared.Outer<int>.Leaf(); }", "Helper();")]
        [InlineData("constructed-callee", "", "GenericThrower<int>.Throw();")]
        [InlineData("two-constructed-callees", "", "GenericThrower<int>.Throw(); GenericThrower<string>.Throw();")]
        public void CompilerModel_ObserveActualSummarySourcesBeforeNormalization(string scenario, string helper, string call)
        {
            string source = Definitions + " public static class GenericThrower<T> { public static void Throw() { throw new Shared.Outer<T>.Leaf(); } } " +
                $"public static class Entry {{ {helper} public static void Root() {{ {call} }} }}";
            var run = ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");
            var summaries = new List<(ExceptionFlowCallableKey Key, ExceptionFlowSummary Summary)>();
            var pending = new Queue<ExceptionFlowCallableKey>();
            var seen = new HashSet<ExceptionFlowCallableKey>();
            pending.Enqueue(run.RootKey);
            while (pending.TryDequeue(out var key))
            {
                if (!seen.Add(key)) continue;
                var summary = run.GetRequiredSummary(key);
                summaries.Add((key, summary));
                foreach (var edge in summary.CallEdges) pending.Enqueue(edge.Target);
            }
            var sources = summaries.SelectMany(pair => pair.Summary.Sources).ToArray();
            Assert.NotEmpty(sources);
            var expected = run.Compilation.GetTypeByMetadataName("Shared.Outer`1")!.Construct(run.Compilation.GetSpecialType(SpecialType.System_Int32)).GetTypeMembers("Leaf").Single();
            Assert.Single(sources);
            Assert.All(summaries, pair => Assert.Empty(pair.Summary.UncertainTargets));
            if (scenario == "closed-source")
            {
                Assert.Equal(Identity(expected), Identity(sources[0].ExceptionType));
            }
            else
            {
                // The missing call-site substitution precedes the type resolver.
                Assert.NotEqual(Identity(expected), Identity(sources[0].ExceptionType));
                Assert.IsAssignableFrom<ITypeParameterSymbol>(Assert.Single(sources[0].ExceptionType.ContainingType.TypeArguments));
                var callee = Assert.Single(summaries.Where(pair => pair.Key.Symbol.Name == "Throw"));
                Assert.True(callee.Key.Symbol.ContainingType.IsDefinition);
                Assert.Empty(callee.Key.CanonicalIdentity!.ContainingType!.TypeArguments);
                Assert.Empty(callee.Key.CanonicalContext!.Callable!.ContainingType!.TypeArguments);
                if (scenario == "two-constructed-callees")
                {
                    Assert.Equal(2, run.RootSummary.CallEdges.Count);
                    Assert.Equal(run.RootSummary.CallEdges[0].Target, run.RootSummary.CallEdges[1].Target);
                }
            }
            Evidence("summary-origin-" + scenario, new
            {
                Scenario = scenario,
                ExpectedClosedType = Identity(expected),
                SourceTypes = sources.Select(source => Identity(source.ExceptionType)).ToArray(),
                ExactExpectedSourcePresent = sources.Any(source => Identity(source.ExceptionType).Equals(Identity(expected))),
                Callables = summaries.Select(pair => new { Callable = pair.Key.CanonicalIdentity, Context = pair.Key.CanonicalContext, pair.Key.ContextKey }).ToArray(),
                UncertainTargets = summaries.SelectMany(pair => pair.Summary.UncertainTargets).Select(item => item.ToString()).ToArray()
            });
        }

        [Theory]
        [InlineData("closed-source", "Helper();", "public static void Helper() { throw new Shared.Outer<int>.Leaf(); }")]
        [InlineData("constructed-callee", "GenericThrower<int>.Throw();", "")]
        [InlineData("two-constructed-callees", "GenericThrower<int>.Throw(); GenericThrower<string>.Throw();", "")]
        public async Task StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness(string scenario, string call, string helper)
        {
            string source = Definitions + " public static class GenericThrower<T> { public static void Throw() { throw new Shared.Outer<T>.Leaf(); } } " +
                $"public static class Entry {{ {helper} public static void Root() {{ {call} }} }}";
            var current = CurrentResult(source);
            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            string worker = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin", configuration, "net8.0/XMLDocNormalizer.HistoricalWorker.dll");
            var historical = await new HistoricalWorkerClient("dotnet", worker, TimeSpan.FromSeconds(30))
                .AnalyzeAsync(new WorkerAnalysisInput(source, "Entry", "Root", new()));
            Assert.True(historical.Success, historical.Failure?.Message);
            Assert.Empty(historical.Diagnostics);
            Assert.Equal(current, historical.Result);
            var entry = Assert.Single(current.Entries);
            Assert.Equal("Leaf", entry.ExceptionType.MetadataName);
            Assert.Equal("Outer`1", entry.ExceptionType.ContainingType!.MetadataName);
            Assert.Empty(entry.ExceptionType.ContainingType.TypeArguments);
            Assert.Empty(current.Uncertainties);
            HistoricalWorkerLifetimeAssertions.Stopped(historical);
            Assert.All(historical.Identity!.LoadedRoslyn, image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical");
            Evidence("worker-construction-loss-" + scenario, new
            {
                Scenario = scenario,
                Current = current,
                Historical = historical.Result,
                historical.Identity,
                historical.ProcessExit,
                Parity = true,
                CorrectConstructedType = false,
                ProductionRepaired = false,
                SolutionFindingAdmission = false
            });
        }

        private static CanonicalExceptionFlowAnalysisResult CurrentResult(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            Assert.Equal(7, references.Length);
            var compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var environment = new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
            return RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions(root, environment));
        }

        private static string Safe(string value) => string.Concat(value.Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_'));
        private static CanonicalTypeIdentity Identity(ITypeSymbol type) => RoslynCanonicalIdentityFactory.CreateTypeIdentity(type);
        private static CSharpCompilation Compile(string name, string source)
        {
            var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "/" + name + ".cs")],
                MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return compilation;
        }
        private static void Evidence(string name, object value)
        {
            string path = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/p5o2b25a2/analysis", name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
