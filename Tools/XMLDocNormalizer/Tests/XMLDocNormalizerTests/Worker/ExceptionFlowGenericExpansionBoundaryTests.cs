using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>A6 boundary witnesses, not an implementation of generic expansion.</summary>
    public sealed class ExceptionFlowGenericExpansionBoundaryTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ExactMaterialization_HasFreshSuccessorAtEveryWitnessStep(bool containing, bool dictionary)
        {
            string next = dictionary ? "System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<T>>" : "System.Collections.Generic.List<T>";
            var (run, selected) = Build(containing, next);
            var graph = new ExceptionFlowSummaryGraph();
            IMethodSymbol instance = selected;
            var observations = new List<object>();
            // A finite observation prefix only; NOT a production bound or a termination experiment.
            const int witnessSteps = 24;
            for (int step = 0; step < witnessSteps; step++)
            {
                var key = ExactCandidateKey(instance);
                graph.GetOrAdd(key);
                Assert.Equal(key, graph.DequeuePendingOrDefault());
                Assert.Null(graph.DequeuePendingOrDefault());
                var argument = containing ? instance.ContainingType.TypeArguments.Single() : instance.TypeArguments.Single();
                var list = run.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!.Construct(argument);
                ITypeSymbol successorArgument = dictionary
                    ? run.Compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")!.Construct(run.Compilation.GetSpecialType(SpecialType.System_String), list)
                    : list;
                IMethodSymbol successor = Recreate(instance, successorArgument, containing);
                Assert.NotEqual(key, ExactCandidateKey(successor));
                // This is the CURRENT defect: the real key still erases the construction.
                Assert.Equal(new ExceptionFlowCallableKey(instance, string.Empty), new ExceptionFlowCallableKey(successor, string.Empty));
                Assert.Equal(step + 1, graph.Count);
                observations.Add(new { Step = step, ExactKey = CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(instance)), SuccessorKey = CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(successor)) });
                instance = successor;
            }
            graph.GetOrAdd(ExactCandidateKey(instance));
            Assert.Equal(witnessSteps + 1, graph.Count);
            Assert.NotNull(graph.DequeuePendingOrDefault());
            Assert.Equal(2, run.Graph.Count); // Root plus the CURRENT definition-folded node.
            Evidence("growth-" + containing + "-" + dictionary, new { Containing = containing, Dictionary = dictionary, WitnessSteps = witnessSteps, Observations = observations, ExactCandidateNodes = graph.Count, ProductionChanged = false, FinitePrefixIsNotTerminationProof = true });
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void IdentityAndClosingSubstitution_RepeatAnExactState(bool containing, bool closing)
        {
            var (run, selected) = Build(containing, closing ? "int" : "T");
            var successor = Recreate(selected, run.Compilation.GetSpecialType(closing ? SpecialType.System_Int32 : SpecialType.System_String), containing);
            var repeated = Recreate(successor, containing ? successor.ContainingType.TypeArguments.Single() : successor.TypeArguments.Single(), containing);
            Assert.Equal(ExactCandidateKey(successor), ExactCandidateKey(repeated));
            Assert.Equal(!closing, ExactCandidateKey(selected).Equals(ExactCandidateKey(successor)));
            Evidence("finite-" + containing + "-" + closing, new { Containing = containing, Closing = closing, Initial = RoslynCanonicalIdentityFactory.CreateCallableIdentity(selected), Successor = RoslynCanonicalIdentityFactory.CreateCallableIdentity(successor), Repeated = RoslynCanonicalIdentityFactory.CreateCallableIdentity(repeated), ExactDistinctStates = closing ? 2 : 1, ProductionChanged = false });
        }

        private static ExceptionFlowCallableKey ExactCandidateKey(IMethodSymbol method) =>
            new(method.OriginalDefinition, CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(method)));

        private static IMethodSymbol Recreate(IMethodSymbol method, ITypeSymbol argument, bool containing) => containing
            ? method.ContainingType.OriginalDefinition.Construct(argument).GetMembers("Recur").OfType<IMethodSymbol>().Single()
            : method.OriginalDefinition.Construct(argument);

        private static (ExceptionFlowSummaryGraphTestRun Run, IMethodSymbol Selected) Build(bool containing, string next)
        {
            string api = containing
                ? "public static class Api<T> { public static void Recur() { if (Flags.Stop) throw new Fault<T>(); Api<" + next + ">.Recur(); } }"
                : "public static class Api { public static void Recur<T>() { if (Flags.Stop) throw new Fault<T>(); Recur<" + next + ">(); } }";
            string call = containing ? "Api<string>.Recur();" : "Api.Recur<string>();";
            var run = ExceptionFlowSummaryGraphTestHelper.Build("public class Fault<T> : System.Exception {} public static class Flags { public static bool Stop; } " + api + "public static class Entry { public static void Root() { " + call + " } }", "Root");
            var tree = run.Compilation.SyntaxTrees.Single();
            var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.ValueText == "Root");
            var selected = (IMethodSymbol)run.Compilation.GetSemanticModel(tree).GetSymbolInfo(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
            return (run, selected);
        }

        private static void Evidence(string name, object value)
        {
            string directory = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/p5o2b25a6/analysis");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
