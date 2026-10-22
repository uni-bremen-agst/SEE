using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Finite recursive cache closure, independently checked before Worker parity.</summary>
    public sealed class ExceptionFlowRecursiveSummaryClosureTests
    {
        private static readonly string[] Scenarios = ["self", "mutual", "three", "outgoing", "incoming", "repeat", "independent", "acyclic"];

        public static IEnumerable<object[]> CurrentCases => Scenarios.SelectMany(scenario => new[] { new object[] { scenario, false }, new object[] { scenario, true } });
        public static IEnumerable<object[]> WorkerCases => Scenarios.Select(scenario => new object[] { scenario });

        [Theory]
        [MemberData(nameof(CurrentCases))]
        public void Current_ClosesFiniteRecursiveDependencies(string scenario, bool generic)
        {
            string source = Source(scenario, generic);
            var run = ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");
            var evaluator = new ExceptionFlowSummaryGraphEvaluator();
            var first = evaluator.Evaluate(run.Graph, run.RootKey, run.Compilation);
            string[] expected = scenario == "independent" ? ["Fault", "Other"] : ["Fault"];
            Assert.Equal(expected, first.ThrownExceptions.Select(type => type.Name).Order().ToArray());
            Assert.False(first.HasUncertainPaths);
            Assert.Empty(first.ExternalDocumentationEvidenceExceptions);
            var before = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(first);
            var repeated = evaluator.Evaluate(run.Graph, run.RootKey, run.Compilation);
            Assert.Equal(before, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(repeated));
            // Returned mutable DTOs are not a long-lived per-session result cache.
            first.ClearThrownExceptions();
            Assert.Equal(expected, evaluator.Evaluate(run.Graph, run.RootKey, run.Compilation).ThrownExceptions.Select(type => type.Name).Order().ToArray());
            Evidence("current-" + scenario + "-" + generic, new { Source = source, Scenario = scenario, Generic = generic, Result = before, RepeatedEqual = true, NoCrossEvaluationMutableResultReuse = true });
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(3, false)]
        [InlineData(3, true)]
        public void EverySccEntryAndRootOrder_PreservesReachableFault(int size, bool reverse)
        {
            string[] names = size == 2 ? ["A", "B"] : ["A", "B", "C"];
            var order = reverse ? names.Reverse().ToArray() : names;
            string calls = "try { " + order[0] + "(); } catch (Fault) {} " + string.Join(" ", order.Select(name => "try { " + name + "(); } finally {}"));
            string methods = "public static void A() { if (Flags.Stop) throw new Fault(); B(); } public static void B() { " + (size == 2 ? "A" : "C") + "(); } " + (size == 3 ? "public static void C() { A(); }" : "");
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Preamble + "public static class Entry { public static void Root() { " + calls + " } " + methods + " }", "Root");
            var evaluator = new ExceptionFlowSummaryGraphEvaluator();
            Assert.Equal(order[0], run.RootSummary.CallEdges[0].Target.Symbol.Name);
            foreach (var key in run.RootSummary.CallEdges.Select(edge => edge.Target).Append(run.RootKey).Distinct())
            {
                var result = evaluator.Evaluate(run.Graph, key, run.Compilation);
                Assert.Equal("Fault", Assert.Single(result.ThrownExceptions).Name);
                Assert.False(result.HasUncertainPaths);
            }
            Evidence("order-" + size + "-" + reverse, new { Size = size, Reverse = reverse, AllEntriesClosed = true });
        }

        [Theory]
        [InlineData("absent")]
        [InlineData("unbuilt")]
        [InlineData("unbuilt-cycle")]
        public void IncompleteDependency_IsUncertainNotSilentlyComplete(string state)
        {
            var run = ExceptionFlowSummaryGraphTestHelper.Build("public static class Entry { public static void Root() {} public static void Missing() {} }", "Root");
            var symbol = run.Compilation.GetTypeByMetadataName("Entry")!.GetMembers("Missing").Single();
            var missing = new ExceptionFlowCallableKey(symbol, "");
            var edge = new ExceptionFlowSummaryCallEdge(missing, Step("Missing"));
            var fragment = new ExceptionFlowSummaryFragment();
            fragment.AddCallEdge(edge);
            run.RootSummary.Merge(fragment);
            if (state != "absent")
            {
                var placeholder = run.Graph.GetOrAdd(missing);
                Assert.False(placeholder.HasExecutableBody);
                if (state == "unbuilt-cycle")
                {
                    var cycle = new ExceptionFlowSummaryFragment();
                    cycle.AddCallEdge(new ExceptionFlowSummaryCallEdge(run.RootKey, Step("Root")));
                    placeholder.Merge(cycle);
                }
            }
            var evaluator = new ExceptionFlowSummaryGraphEvaluator();
            for (int evaluation = 0; evaluation < 2; evaluation++)
            {
                var result = evaluator.Evaluate(run.Graph, run.RootKey, run.Compilation);
                Assert.Empty(result.ThrownExceptions);
                Assert.True(result.HasUncertainPaths);
                Assert.Contains("Entry.Missing()", result.UncertainTargets);
            }
            Evidence("incomplete-" + state, new { State = state, UncertaintyRetained = true, EmptyResultNotClaimedConclusive = true });
        }

        [Fact]
        public void RealGraphComponents_SeparateIndependentCyclesAndAcyclicNodes()
        {
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Source("independent", false), "Root");
            var find = typeof(ExceptionFlowSummaryGraphEvaluator).GetMethod("FindSummaryComponents", BindingFlags.Static | BindingFlags.NonPublic)!;
            var components = (Dictionary<ExceptionFlowCallableKey, (int Component, int Member)>)find.Invoke(null, [run.Graph, run.RootKey])!;
            int Component(string name) => components.Single(pair => pair.Key.Symbol.Name == name).Value.Component;
            Assert.Equal(Component("A"), Component("B"));
            Assert.Equal(Component("C"), Component("D"));
            Assert.NotEqual(Component("A"), Component("C"));
            Assert.NotEqual(Component("Root"), Component("A"));
            Assert.NotEqual(Component("Root"), Component("C"));
            Assert.Equal(components.Count, components.Values.Select(value => value.Member).Distinct().Count());
        }

        [Fact]
        public void AllThreeNodeGraphs_MatchIndependentPerTypeReachabilityClosure()
        {
            // Supplemental exhaustive model proof; real compiler/production cases above
            // and genuine Worker cases below are not replaced by this finite model.
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Preamble + "public static class Entry { public static void Root() {} public static void A() {} public static void B() {} }", "Root");
            var owner = run.Compilation.GetTypeByMetadataName("Entry")!;
            var keys = new[] { run.RootKey, new ExceptionFlowCallableKey(owner.GetMembers("A").Single(), ""), new ExceptionFlowCallableKey(owner.GetMembers("B").Single(), "") };
            var fault = run.Compilation.GetTypeByMetadataName("Fault")!;
            int checks = 0;
            for (int mask = 0; mask < 512; mask++)
            {
                for (int source = 0; source < 3; source++)
                {
                    var graph = new ExceptionFlowSummaryGraph();
                    for (int node = 0; node < 3; node++)
                    {
                        var summary = graph.GetOrAdd(keys[node]);
                        summary.MarkExecutableBodyAnalyzed();
                        var fragment = new ExceptionFlowSummaryFragment();
                        if (node == source)
                        {
                            fragment.AddSource(new ExceptionFlowSummarySource(fault, new ExceptionFlowPath(Step("throw")), ExceptionFlowSourceKind.ProvenException));
                        }
                        for (int target = 0; target < 3; target++)
                        {
                            if ((mask & (1 << (node * 3 + target))) == 0) { continue; }
                            var edge = new ExceptionFlowSummaryCallEdge(keys[target], Step(node + "->" + target));
                            // Fixed nontrivial edge filter, also applied by the independent oracle.
                            if ((node + target + source) % 3 == 0) { edge.AddCaughtExceptionType(fault); }
                            fragment.AddCallEdge(edge);
                        }
                        summary.Merge(fragment);
                    }
                    for (int root = 0; root < 3; root++)
                    {
                        HashSet<int> reached = [root];
                        Queue<int> work = new([root]);
                        while (work.TryDequeue(out int node))
                        {
                            for (int target = 0; target < 3; target++)
                            {
                                if ((mask & (1 << (node * 3 + target))) != 0 && (node + target + source) % 3 != 0 && reached.Add(target)) { work.Enqueue(target); }
                            }
                        }
                        var actual = new ExceptionFlowSummaryGraphEvaluator().Evaluate(graph, keys[root], run.Compilation);
                        Assert.Equal(reached.Contains(source), actual.ThrownExceptions.Contains(fault));
                        Assert.False(actual.HasUncertainPaths);
                        checks++;
                    }
                }
            }
            Assert.Equal(4608, checks);
            Evidence("finite-oracle", new { Graphs = 512, SourcePlacements = 3, Roots = 3, Checks = checks, IndependentFilteredReachability = true });
        }

        [Theory]
        [MemberData(nameof(WorkerCases))]
        public async Task GenuineWorker_UsesIndependentlyProvenFiniteClosure(string scenario)
        {
            string source = Source(scenario, true);
            var current = (CanonicalExceptionFlowAnalysisResult)typeof(ExceptionFlowCanonicalExceptionIdentityTests).GetMethod("CurrentResult", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [source])!;
            Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray());
            Assert.Empty(current.Uncertainties);
            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            string worker = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin", configuration, "net8.0/XMLDocNormalizer.HistoricalWorker.dll");
            var historical = await new HistoricalWorkerClient("dotnet", worker, TimeSpan.FromSeconds(30)).AnalyzeAsync(new WorkerAnalysisInput(source, "Entry", "Root", new()));
            Assert.True(historical.Success, historical.Failure?.Message);
            Assert.Empty(historical.Diagnostics);
            Assert.Equal(current, historical.Result);
            HistoricalWorkerLifetimeAssertions.Stopped(historical);
            Assert.All(historical.Identity!.LoadedRoslyn, image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical");
            Evidence("worker-" + scenario, new { Source = source, Scenario = scenario, Current = current, Historical = historical.Result, historical.Identity, historical.ProcessExit });
        }

        private const string Preamble = "public class Fault : System.Exception {} public class Other : System.Exception {} public static class Flags { public static bool Stop; } ";

        private static string Source(string scenario, bool generic)
        {
            string caught = "try { A(); } catch (Fault) {} ";
            string escaped = "try { B(); } finally {} ";
            string body = scenario switch
            {
                "self" => "public static void Root() { A(); } public static void A() { if (Flags.Stop) throw new Fault(); A(); }",
                "mutual" => "public static void Root() { " + caught + escaped + " } public static void A() { if (Flags.Stop) throw new Fault(); B(); } public static void B() { A(); }",
                "three" => "public static void Root() { " + caught + escaped + " } public static void A() { if (Flags.Stop) throw new Fault(); B(); } public static void B() { C(); } public static void C() { A(); }",
                "outgoing" => "public static void Root() { " + caught + escaped + " } public static void A() { B(); } public static void B() { A(); C(); } public static void C() { throw new Fault(); }",
                "incoming" => "public static void Root() { try { X(); } catch (Fault) {} " + escaped + " } public static void X() { A(); } public static void A() { if (Flags.Stop) throw new Fault(); B(); } public static void B() { A(); }",
                "repeat" => "public static void Root() { B(); B(); } public static void A() { if (Flags.Stop) throw new Fault(); B(); } public static void B() { A(); }",
                "independent" => "public static void Root() { " + caught + escaped + " try { C(); } catch (Other) {} try { D(); } finally {} } public static void A() { if (Flags.Stop) throw new Fault(); B(); } public static void B() { A(); } public static void C() { if (Flags.Stop) throw new Other(); D(); } public static void D() { C(); }",
                "acyclic" => "public static void Root() { A(); } public static void A() { B(); } public static void B() { C(); } public static void C() { throw new Fault(); }",
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
            if (generic)
            {
                foreach (string name in new[] { "A", "B", "C", "D", "X" }) { body = body.Replace(name + "()", name + "<T>()", StringComparison.Ordinal); }
                // Only Root's calls supply int. Recursive bodies keep the SAME T.
                int endOfRoot = body.IndexOf("public static void ", "public static void Root()".Length, StringComparison.Ordinal);
                body = body[..endOfRoot].Replace("<T>", "<int>", StringComparison.Ordinal) + body[endOfRoot..];
            }
            return Preamble + "public static class Entry { " + body + " }";
        }

        private static ExceptionFlowPathStep Step(string name) => new(ExceptionFlowPathStepKind.MethodCall, name, "finite-model.cs", 1, 1);

        private static void Evidence(string name, object value)
        {
            string path = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/p5o2b25a5/analysis", name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
