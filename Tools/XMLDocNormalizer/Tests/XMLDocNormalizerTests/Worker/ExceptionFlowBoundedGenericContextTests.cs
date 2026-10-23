using System.Text.Json;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizerTests.Helpers;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>A4 boundary proofs, not a corrected generic summary engine.</summary>
    public sealed class ExceptionFlowBoundedGenericContextTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void FiniteMutualCycle_CachedPartialResultDependsOnRootEdgeOrder(bool generic, bool uncaughtFirst)
        {
            string parameter = generic ? "<T>" : "";
            string recursive = generic ? "<T>" : "";
            string concrete = generic ? "<int>" : "";
            string caught = "try { A" + concrete + "(); } catch (Fault) {} ";
            // Both calls must be try fragments: outer calls are collected
            // before nested tries, independently of their textual position.
            string uncaught = "try { B" + concrete + "(); } finally {} ";
            string source = "public class Fault : System.Exception {} public static class Flags { public static bool Stop; } public static class Entry { " +
                "public static void Root() { " + (uncaughtFirst ? uncaught + caught : caught + uncaught) + " } " +
                "public static void A" + parameter + "() { if (Flags.Stop) throw new Fault(); B" + recursive + "(); } " +
                "public static void B" + parameter + "() { A" + recursive + "(); } }";
            var run = ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");
            Assert.Equal(3, run.Graph.Count);
            var first = run.RootSummary.CallEdges[0];
            var second = run.RootSummary.CallEdges[1];
            Assert.Equal(uncaughtFirst ? "B" : "A", first.Target.Symbol.Name);
            Assert.Equal(uncaughtFirst ? "A" : "B", second.Target.Symbol.Name);
            var a = first.Target.Symbol.Name == "A" ? first.Target : second.Target;
            var b = first.Target.Symbol.Name == "B" ? first.Target : second.Target;
            Assert.Equal(b, Assert.Single(run.GetRequiredSummary(a).CallEdges).Target);
            Assert.Equal(a, Assert.Single(run.GetRequiredSummary(b).CallEdges).Target);
            Assert.Single(run.GetRequiredSummary(a).Sources);
            Assert.Empty(run.GetRequiredSummary(b).Sources);
            var evaluator = new ExceptionFlowSummaryGraphEvaluator();
            var root = evaluator.Evaluate(run.Graph, run.RootKey, run.Compilation);
            var bAlone = evaluator.Evaluate(run.Graph, b, run.Compilation);
            Assert.Equal("Fault", Assert.Single(bAlone.ThrownExceptions).Name);
            Assert.False(bAlone.HasUncertainPaths);
            Assert.False(root.HasUncertainPaths);
            // A5-authorized correction of the caught-first cache-loss cases.
            // Historical A4 report/audit/witnesses retain the original defect.
            if (uncaughtFirst) { Assert.Equal("Fault", Assert.Single(root.ThrownExceptions).Name); }
            else { Assert.Equal("Fault", Assert.Single(root.ThrownExceptions).Name); }
            Evidence("finite-scc-" + generic + "-" + uncaughtFirst, new
            {
                Generic = generic,
                UncaughtFirst = uncaughtFirst,
                Source = source,
                GraphCount = run.Graph.Count,
                Root = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(root),
                IndependentlyEvaluatedB = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(bAlone),
                RequiredEscapingType = "Fault",
                LostReachableExceptionWithoutUncertainty = false,
                ExistingEvaluatorCorrected = !uncaughtFirst,
                SolutionFindingAdmitted = false
            }, b25a5: !uncaughtFirst);
        }

        [Theory]
        [InlineData("identity")]
        [InlineData("constant")]
        [InlineData("growing")]
        [InlineData("mutual")]
        [InlineData("containing")]
        public void RealRecurrences_DistinguishFiniteClosureFromIncreasingExactArguments(string scenario)
        {
            string api = scenario switch
            {
                "identity" => "public static class Api { public static void A<T>() { if (Flags.Stop) throw new Fault<T>(); A<T>(); } }",
                "constant" => "public static class Api { public static void A<T>() { if (Flags.Stop) throw new Fault<T>(); A<int>(); } }",
                "mutual" => "public static class Api { public static void A<T>() { if (Flags.Stop) throw new Fault<T>(); B<System.Collections.Generic.List<T>>(); } public static void B<T>() { A<T>(); } }",
                "containing" => "public static class Api<T> { public static void A() { if (Flags.Stop) throw new Fault<T>(); Api<System.Collections.Generic.List<T>>.A(); } }",
                _ => "public static class Api { public static void A<T>() { if (Flags.Stop) throw new Fault<T>(); A<System.Collections.Generic.List<T>>(); } }"
            };
            string call = scenario == "containing" ? "Api<string>.A();" : "Api.A<string>();";
            string program = "public class Fault<T> : System.Exception {} public static class Flags { public static bool Stop; } " + api + " public static class Entry { public static void Root() { " + call + " } }";
            var production = ExceptionFlowFiniteOrbitAdmissionTests.Create(program);
            var outcome = production.Session.Analyze(production.Root).ProfileOutcome!;
            var tree = production.Compilation.SyntaxTrees.Single();
            var model = production.Compilation.GetSemanticModel(tree);
            var rootSyntax = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
            var selected = (IMethodSymbol)model.GetSymbolInfo(rootSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
            var text = production.Compilation.GetSpecialType(SpecialType.System_String);
            var number = production.Compilation.GetSpecialType(SpecialType.System_Int32);
            var list = production.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!;
            var firstArgument = scenario == "constant" ? number : scenario is "identity" ? text : list.Construct(text);
            var secondArgument = scenario is "constant" or "identity" ? firstArgument : list.Construct(firstArgument);
            IMethodSymbol Recreate(ITypeSymbol argument) => scenario == "containing"
                ? selected.ContainingType.OriginalDefinition.Construct(argument).GetMembers("A").OfType<IMethodSymbol>().Single()
                : selected.OriginalDefinition.Construct(argument);
            var identities = new[] { selected, Recreate(firstArgument), Recreate(secondArgument) }
                .Select(method => RoslynCanonicalIdentityFactory.CreateCallableIdentity(method)).ToArray();
            int distinct = identities.Distinct().Count();
            Assert.Equal(scenario == "identity" ? 1 : scenario == "constant" ? 2 : 3, distinct);
            ExceptionFlowSummaryGraphTestRun? run = null;
            if (scenario is "identity" or "constant")
            {
                Assert.True(outcome.Complete, string.Join("\n", outcome.Details));
                run = ExceptionFlowSummaryGraphTestHelper.Build(program, "Root");
                Assert.Equal(scenario == "constant" ? 3 : 2, run.Graph.Count); // P07; identity count unchanged.
                var actual = run.GetRequiredSummary(run.RootSummary.CallEdges.Single().Target);
                Assert.Equal(SpecialType.System_String, Assert.Single(Assert.Single(actual.Sources).ExceptionType.TypeArguments).SpecialType); // P06
            }
            else // R3: classify before any unbounded exact-context materialization.
            {
                Assert.False(outcome.Complete);
                Assert.Contains(outcome.Reasons, reason => reason.Category == "PositiveConstructorCycle");
                Assert.Equal(0, ExceptionFlowFiniteOrbitAdmissionTests.Graph(production.Session).Count);
            }
            Evidence("recurrence-" + scenario, new
            {
                Scenario = scenario,
                Source = tree.ToString(),
                ExactAContexts = identities,
                DistinctAContexts = distinct,
                ActualGraphCount = run?.Graph.Count,
                Profile = outcome,
                Observation = scenario == "mutual" ? "Each A -> B<List<T>> -> A<T> cycle adds one List constructor to A's argument" : scenario,
                FiniteIllustrationOnly = true,
                ProductExpansionBudgetAdded = false,
                ExactAdmitted = outcome.Complete
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RoslynComposition_PreservesScopedMethodAndContainingArguments(bool nested)
        {
            string api = nested
                ? "public class Outer<T> { public class Inner<U> { public static void M<V>() {} } }"
                : "public static class Api { public static void M<T>() {} }";
            string call = nested ? "Outer<int>.Inner<string>.M<System.Collections.Generic.List<bool>>();" : "Api.M<System.Collections.Generic.List<int>>();";
            var run = ExceptionFlowSummaryGraphTestHelper.Build(api + " public static class Entry { public static void Root() { " + call + " } }", "Root");
            var tree = run.Compilation.SyntaxTrees.Single();
            var model = run.Compilation.GetSemanticModel(tree);
            var selected = (IMethodSymbol)model.GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
            var listArgument = Assert.IsAssignableFrom<INamedTypeSymbol>(Assert.Single(selected.TypeArguments));
            Assert.Equal("List", listArgument.Name);
            Assert.Equal(nested ? SpecialType.System_Boolean : SpecialType.System_Int32, Assert.Single(listArgument.TypeArguments).SpecialType);
            if (nested)
            {
                Assert.Equal(SpecialType.System_String, Assert.Single(selected.ContainingType.TypeArguments).SpecialType);
                Assert.Equal(SpecialType.System_Int32, Assert.Single(selected.ContainingType.ContainingType!.TypeArguments).SpecialType);
            }
            var exact = RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected);
            Assert.NotEqual(exact, RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected, normalizeToOriginalDefinition: true));
            var recreated = selected.ConstructedFrom.Construct(selected.TypeArguments.ToArray());
            Assert.Equal(exact, RoslynCanonicalIdentityFactory.CreateCallableIdentity(recreated));
            if (nested)
            {
                Assert.NotEqual(exact, RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected.OriginalDefinition.Construct(selected.TypeArguments.ToArray())));
            }
            Evidence("composition-" + nested, new { Nested = nested, Source = tree.ToString(), Selected = exact, BodyProjectionImplemented = false });
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness(bool generic, bool uncaughtFirst)
        {
            string parameter = generic ? "<T>" : "";
            string concrete = generic ? "<int>" : "";
            string caught = "try { A" + concrete + "(); } catch (Fault) {} ";
            string uncaught = "try { B" + concrete + "(); } finally {} ";
            string source = "public class Fault : System.Exception {} public static class Flags { public static bool Stop; } public static class Entry { " +
                "public static void Root() { " + (uncaughtFirst ? uncaught + caught : caught + uncaught) + " } " +
                "public static void A" + parameter + "() { if (Flags.Stop) throw new Fault(); B" + parameter + "(); } " +
                "public static void B" + parameter + "() { A" + parameter + "(); } }";
            var current = (CanonicalExceptionFlowAnalysisResult)typeof(ExceptionFlowCanonicalExceptionIdentityTests)
                .GetMethod("CurrentResult", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [source])!;
            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            string worker = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin", configuration, "net8.0/XMLDocNormalizer.HistoricalWorker.dll");
            var historical = await new HistoricalWorkerClient("dotnet", worker, TimeSpan.FromSeconds(30))
                .AnalyzeAsync(new WorkerAnalysisInput(source, "Entry", "Root", new()));
            Assert.True(historical.Success, historical.Failure?.Message);
            Assert.Empty(historical.Diagnostics);
            Assert.Equal(current, historical.Result);
            Assert.Empty(current.Uncertainties);
            if (uncaughtFirst) { Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName); }
            else { Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName); }
            HistoricalWorkerLifetimeAssertions.Stopped(historical);
            Assert.All(historical.Identity!.LoadedRoslyn, image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical");
            Evidence("worker-scc-" + generic + "-" + uncaughtFirst, new { Generic = generic, UncaughtFirst = uncaughtFirst, Source = source, Current = current, Historical = historical.Result, historical.Identity, historical.ProcessExit, CorrectnessProven = !uncaughtFirst }, b25a5: !uncaughtFirst);
        }

        private static void Evidence(string name, object value, bool b25a5 = false)
        {
            string path = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), b25a5 ? "artifacts/p5o2b25a10/analysis/a5" : "artifacts/p5o2b25a10/analysis/a4", name + ".json"); // R6
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(value));
        }
    }
}
