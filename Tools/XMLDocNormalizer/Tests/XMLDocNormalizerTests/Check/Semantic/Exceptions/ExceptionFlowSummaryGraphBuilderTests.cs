using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests nonrecursive construction of context-sensitive exception-flow
    /// summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphBuilderTests
    {
        /// <summary>
        /// Ensures that two calls to the same context-sensitive target remain
        /// separate edges while the target body is summarized once.
        /// </summary>
        [Fact]
        public void TwoMethodCalls_CreateTwoEdgesToOneTargetSummary()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        ThrowException();\n" +
                "        ThrowException();\n" +
                "    }\n" +
                "\n" +
                "    private static void ThrowException()\n" +
                "    {\n" +
                "        throw new ArgumentException();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                2,
                run.Graph.Count);

            Assert.Equal(
                2,
                run.RootSummary.CallEdges.Count);

            ExceptionFlowCallableKey firstTarget =
                run.RootSummary.CallEdges[0].Target;

            ExceptionFlowCallableKey secondTarget =
                run.RootSummary.CallEdges[1].Target;

            Assert.Equal(
                firstTarget,
                secondTarget);

            int?[] callSiteLines =
                run.RootSummary.CallEdges
                    .Select(
                        edge => edge.CallSiteStep.Line)
                    .OrderBy(
                        static line => line)
                    .ToArray();

            Assert.Equal(
                new int?[] { 6, 7 },
                callSiteLines);

            ExceptionFlowSummary targetSummary =
                run.GetRequiredSummary(
                    firstTarget);

            Assert.True(
                targetSummary.HasExecutableBody);

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    targetSummary.Sources);

            Assert.Equal(
                ExceptionFlowPathStepKind.ExplicitThrow,
                sourceEntry.LocalPath.Steps[0].Kind);
        }

        /// <summary>
        /// Ensures that a typed catch remains attached to a call edge while
        /// the target summary retains all of its local exception sources.
        /// </summary>
        [Fact]
        public void TypedCatch_FiltersCallEdge()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            ThrowException();\n" +
                "        }\n" +
                "        catch (ArgumentException)\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void ThrowException()\n" +
                "    {\n" +
                "        throw new ArgumentNullException();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge callEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.True(
                callEdge.Suppresses(
                    argumentException));

            Assert.True(
                callEdge.Suppresses(
                    argumentNullException));

            ExceptionFlowSummary targetSummary =
                run.GetRequiredSummary(
                    callEdge.Target);

            ExceptionFlowSummarySource targetSource =
                Assert.Single(
                    targetSummary.Sources);

            Assert.True(
                SymbolEqualityComparer.Default.Equals(
                    argumentNullException,
                    targetSource.ExceptionType));
        }

        /// <summary>
        /// Ensures that a catch-all removes every escaping edge from the
        /// protected root fragment.
        /// </summary>
        [Fact]
        public void CatchAll_RemovesProtectedCallEdge()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            ThrowException();\n" +
                "        }\n" +
                "        catch\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void ThrowException()\n" +
                "    {\n" +
                "        throw new InvalidOperationException();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);

            Assert.Empty(
                run.RootSummary.UncertainTargets);
        }

        /// <summary>
        /// Ensures that constructors, property getters, and indexer getters
        /// are represented by their dedicated edge kinds.
        /// </summary>
        [Fact]
        public void CallableKinds_CreateDedicatedEdges()
        {
            const string source =
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Helper helper = new Helper();\n" +
                "        _ = helper.Value;\n" +
                "        _ = helper[0];\n" +
                "    }\n" +
                "}\n" +
                "\n" +
                "public sealed class Helper\n" +
                "{\n" +
                "    public Helper()\n" +
                "    {\n" +
                "        throw new ArgumentException();\n" +
                "    }\n" +
                "\n" +
                "    public int Value\n" +
                "    {\n" +
                "        get\n" +
                "        {\n" +
                "            throw new InvalidOperationException();\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    public int this[int index]\n" +
                "    {\n" +
                "        get\n" +
                "        {\n" +
                "            throw new IndexOutOfRangeException();\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                3,
                run.RootSummary.CallEdges.Count);

            ExceptionFlowPathStepKind[] edgeKinds =
                run.RootSummary.CallEdges
                    .Select(
                        edge => edge.CallSiteStep.Kind)
                    .OrderBy(
                        static kind => kind)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.ConstructorCall,
                    ExceptionFlowPathStepKind.PropertyGetter,
                    ExceptionFlowPathStepKind.IndexerGetter
                }
                .OrderBy(
                    static kind => kind)
                .ToArray(),
                edgeKinds);

            Assert.All(
                run.RootSummary.CallEdges,
                edge =>
                {
                    ExceptionFlowSummary targetSummary =
                        run.GetRequiredSummary(
                            edge.Target);

                    Assert.True(
                        targetSummary.HasExecutableBody);

                    Assert.Single(
                        targetSummary.Sources);
                });
        }

        /// <summary>
        /// Ensures that the same method reached with different proven
        /// parameter facts produces distinct context-sensitive graph nodes.
        /// </summary>
        [Fact]
        public void DifferentCallContexts_CreateDistinctTargetNodes()
        {
            const string source =
                "#nullable enable\n" +
                "using System;\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        Guard(null);\n" +
                "        Guard(\"value\");\n" +
                "    }\n" +
                "\n" +
                "    private static void Guard(string? value)\n" +
                "    {\n" +
                "        if (value == null)\n" +
                "        {\n" +
                "            throw new ArgumentNullException(nameof(value));\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                3,
                run.Graph.Count);

            Assert.Equal(
                2,
                run.RootSummary.CallEdges.Count);

            ExceptionFlowCallableKey firstTarget =
                run.RootSummary.CallEdges[0].Target;

            ExceptionFlowCallableKey secondTarget =
                run.RootSummary.CallEdges[1].Target;

            Assert.NotEqual(
                firstTarget,
                secondTarget);

            ExceptionFlowSummary firstSummary =
                run.GetRequiredSummary(
                    firstTarget);

            ExceptionFlowSummary secondSummary =
                run.GetRequiredSummary(
                    secondTarget);

            int[] sourceCounts =
            [
                firstSummary.Sources.Count,
                secondSummary.Sources.Count
            ];

            Array.Sort(sourceCounts);

            Assert.Equal(
                new[] { 0, 1 },
                sourceCounts);
        }

        /// <summary>
        /// Ensures that a recursive callgraph terminates during graph
        /// construction and retains its cycle edges.
        /// </summary>
        [Fact]
        public void RecursiveCallGraph_TerminatesAndRetainsCycle()
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M()\n" +
                "    {\n" +
                "        A();\n" +
                "    }\n" +
                "\n" +
                "    private static void A()\n" +
                "    {\n" +
                "        B();\n" +
                "    }\n" +
                "\n" +
                "    private static void B()\n" +
                "    {\n" +
                "        A();\n" +
                "    }\n" +
                "}\n";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                3,
                run.Graph.Count);

            ExceptionFlowSummaryCallEdge rootToA =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary aSummary =
                run.GetRequiredSummary(
                    rootToA.Target);

            ExceptionFlowSummaryCallEdge aToB =
                Assert.Single(
                    aSummary.CallEdges);

            ExceptionFlowSummary bSummary =
                run.GetRequiredSummary(
                    aToB.Target);

            ExceptionFlowSummaryCallEdge bToA =
                Assert.Single(
                    bSummary.CallEdges);

            Assert.Equal(
                rootToA.Target,
                bToA.Target);
        }
    }
}
