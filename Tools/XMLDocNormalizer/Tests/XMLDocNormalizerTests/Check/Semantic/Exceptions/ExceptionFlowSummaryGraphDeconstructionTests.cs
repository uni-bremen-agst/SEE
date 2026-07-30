using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests compiler-selected deconstruction methods and conversions in
    /// exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphDeconstructionTests
    {
        /// <summary>
        /// Ensures that deconstruction into existing variables creates a
        /// <c>Deconstruct</c> edge.
        /// </summary>
        [Fact]
        public void DeconstructionAssignment_CreatesDeconstructEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Pair pair)
                    {
                        int left;
                        int right;
                        (left, right) = pair;
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall);

            AssertTargetException(
                run,
                edge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a declaration deconstruction creates an edge.
        /// </summary>
        [Fact]
        public void DeconstructionDeclaration_CreatesDeconstructEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Pair pair)
                    {
                        var (left, right) = pair;
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new ArgumentException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall),
                "ArgumentException");
        }

        /// <summary>
        /// Ensures that nested deconstruction records both methods.
        /// </summary>
        [Fact]
        public void NestedDeconstruction_CreatesOuterAndInnerEdges()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Outer value)
                    {
                        var (first, (second, third)) = value;
                    }
                }

                public sealed class Outer
                {
                    public void Deconstruct(
                        out int first,
                        out Inner remainder)
                    {
                        first = 0;
                        remainder = new Inner();
                    }
                }

                public sealed class Inner
                {
                    public void Deconstruct(
                        out int second,
                        out int third)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall);

            Assert.Equal(
                2,
                edges.Length);

            ExceptionFlowSummaryCallEdge innerEdge =
                Assert.Single(
                    edges.Where(
                        edge =>
                            edge.Target.Symbol.ContainingType?.Name ==
                            "Inner"));

            AssertTargetException(
                run,
                innerEdge,
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that tuple deconstruction invents no method call.
        /// </summary>
        [Fact]
        public void TupleDeconstruction_DoesNotCreateDeconstructEdge()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M()
                    {
                        (int left, int right) = (1, 2);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall));
        }

        /// <summary>
        /// Ensures that extension receiver facts are retained.
        /// </summary>
        [Fact]
        public void ExtensionDeconstruct_MapsReceiverFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        var (left, right) = new Pair();
                    }
                }

                public sealed class Pair
                {
                }

                public static class PairExtensions
                {
                    public static void Deconstruct(
                        this Pair? pair,
                        out int left,
                        out int right)
                    {
                        ArgumentNullException.ThrowIfNull(pair);
                        left = 0;
                        right = 0;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall);

            Assert.Equal(
                "PairExtensions",
                edge.Target.Symbol.ContainingType?.Name);

            Assert.Empty(
                run.GetRequiredSummary(
                        edge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that deconstruction conversions are recorded once.
        /// </summary>
        [Fact]
        public void DeconstructionConversion_CreatesConversionEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Pair pair)
                    {
                        (Target left, Target right) = pair;
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        left = 0;
                        right = 0;
                    }
                }

                public readonly struct Target
                {
                    public static implicit operator Target(int value)
                    {
                        throw new FormatException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge conversionEdge =
                Assert.Single(
                    GetEdges(
                        run,
                        ExceptionFlowPathStepKind
                            .ConversionOperatorCall));

            AssertTargetException(
                run,
                conversionEdge,
                "FormatException");
        }

        /// <summary>
        /// Ensures that foreach-variable deconstruction is represented.
        /// </summary>
        [Fact]
        public void ForEachVariableDeconstruction_CreatesDeconstructEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Pair[] pairs)
                    {
                        foreach (var (left, right) in pairs)
                        {
                        }
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            AssertTargetException(
                run,
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall),
                "InvalidOperationException");
        }

        /// <summary>
        /// Ensures that an outer catch filters deconstruction flow.
        /// </summary>
        [Fact]
        public void CatchAroundDeconstruction_SuppressesDeconstructEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Pair pair)
                    {
                        try
                        {
                            var (left, right) = pair;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge edge =
                GetRequiredEdge(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall);

            Assert.True(
                edge.Suppresses(
                    run.GetRequiredType(
                        "System.InvalidOperationException")));
        }

        /// <summary>
        /// Ensures that an uncalled lambda remains excluded.
        /// </summary>
        [Fact]
        public void DeconstructionInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Pair pair)
                    {
                        Action action =
                            () =>
                            {
                                var (left, right) = pair;
                            };
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall));
        }

        /// <summary>
        /// Ensures that separate statements remain separate call sites.
        /// </summary>
        [Fact]
        public void TwoDeconstructionStatements_CreateSeparateEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(
                        Pair first,
                        Pair second)
                    {
                        var (firstLeft, firstRight) = first;
                        var (secondLeft, secondRight) = second;
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        left = 0;
                        right = 0;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] edges =
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall);

            Assert.Equal(
                2,
                edges.Length);

            Assert.NotEqual(
                edges[0].CallSiteStep.Line,
                edges[1].CallSiteStep.Line);
        }

        /// <summary>
        /// Ensures that a repeatedly selected nested method creates two
        /// runtime call edges.
        /// </summary>
        [Fact]
        public void RepeatedNestedDeconstructMethod_CreatesTwoEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(Node node)
                    {
                        var (first, (second, remainder)) = node;
                    }
                }

                public sealed class Node
                {
                    public void Deconstruct(
                        out int value,
                        out Node remainder)
                    {
                        value = 0;
                        remainder = this;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                2,
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall)
                    .Length);
        }

        /// <summary>
        /// Ensures that right-side evaluation remains separate.
        /// </summary>
        [Fact]
        public void FactoryResultDeconstruction_KeepsBothCallEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M()
                    {
                        var (left, right) = Create();
                    }

                    private static Pair Create()
                    {
                        return new Pair();
                    }
                }

                public sealed class Pair
                {
                    public void Deconstruct(
                        out int left,
                        out int right)
                    {
                        left = 0;
                        right = 0;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.MethodCall));

            Assert.Single(
                GetEdges(
                    run,
                    ExceptionFlowPathStepKind.DeconstructCall));
        }

        /// <summary>
        /// Gets all root edges of one kind.
        /// </summary>
        private static ExceptionFlowSummaryCallEdge[] GetEdges(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowPathStepKind kind)
        {
            return run.RootSummary.CallEdges
                .Where(
                    edge =>
                        edge.CallSiteStep.Kind ==
                        kind)
                .ToArray();
        }

        /// <summary>
        /// Gets the single root edge of one kind.
        /// </summary>
        private static ExceptionFlowSummaryCallEdge GetRequiredEdge(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowPathStepKind kind)
        {
            return Assert.Single(
                GetEdges(
                    run,
                    kind));
        }

        /// <summary>
        /// Checks the single local exception of an edge target.
        /// </summary>
        private static void AssertTargetException(
            ExceptionFlowSummaryGraphTestRun run,
            ExceptionFlowSummaryCallEdge edge,
            string expectedExceptionName)
        {
            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    run.GetRequiredSummary(
                            edge.Target)
                        .Sources);

            Assert.Equal(
                expectedExceptionName,
                exceptionSource.ExceptionType.Name);
        }
    }
}
