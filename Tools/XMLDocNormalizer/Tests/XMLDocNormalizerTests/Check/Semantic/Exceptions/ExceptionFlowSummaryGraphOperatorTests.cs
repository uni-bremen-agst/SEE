using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests user-defined operators and conversions in exception-flow summary
    /// graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphOperatorTests
    {
        /// <summary>
        /// Ensures that a user-defined binary operator creates a graph edge.
        /// </summary>
        [Fact]
        public void BinaryOperator_CreatesOperatorEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Number left,
                        Number right)
                    {
                        _ = left + right;
                    }
                }

                public readonly struct Number
                {
                    public static Number operator +(
                        Number left,
                        Number right)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge operatorEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.BinaryOperatorCall,
                operatorEdge.CallSiteStep.Kind);

            Assert.Equal(
                "op_Addition",
                operatorEdge.Target.Symbol.Name);

            ExceptionFlowSummary operatorSummary =
                run.GetRequiredSummary(
                    operatorEdge.Target);

            ExceptionFlowSummarySource exceptionSource =
                Assert.Single(
                    operatorSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                exceptionSource.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a built-in binary operator does not create a callable
        /// graph edge.
        /// </summary>
        [Fact]
        public void BuiltInBinaryOperator_DoesNotCreateEdge()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(
                        int left,
                        int right)
                    {
                        _ = left + right;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);
        }

        /// <summary>
        /// Ensures that a user-defined unary operator creates a graph edge.
        /// </summary>
        [Fact]
        public void UnaryOperator_CreatesOperatorEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Number value)
                    {
                        _ = -value;
                    }
                }

                public readonly struct Number
                {
                    public static Number operator -(
                        Number value)
                    {
                        throw new NotSupportedException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge operatorEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.UnaryOperatorCall,
                operatorEdge.CallSiteStep.Kind);

            Assert.Equal(
                "op_UnaryNegation",
                operatorEdge.Target.Symbol.Name);

            ExceptionFlowSummary operatorSummary =
                run.GetRequiredSummary(
                    operatorEdge.Target);

            Assert.Equal(
                "NotSupportedException",
                Assert.Single(
                    operatorSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a user-defined increment operator creates a unary
        /// operator edge.
        /// </summary>
        [Fact]
        public void IncrementOperator_CreatesUnaryOperatorEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Number value)
                    {
                        value++;
                    }
                }

                public readonly struct Number
                {
                    public static Number operator ++(
                        Number value)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge operatorEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.UnaryOperatorCall,
                operatorEdge.CallSiteStep.Kind);

            Assert.Equal(
                "op_Increment",
                operatorEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that a compound property assignment records getter,
        /// operator, and setter calls.
        /// </summary>
        [Fact]
        public void CompoundPropertyAssignment_CreatesThreeEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(
                        Target target,
                        Number delta)
                    {
                        target.Value += delta;
                    }
                }

                public sealed class Target
                {
                    public Number Value
                    {
                        get;
                        set;
                    }
                }

                public readonly struct Number
                {
                    public static Number operator +(
                        Number left,
                        Number right)
                    {
                        return left;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowPathStepKind[] kinds =
                run.RootSummary.CallEdges
                    .Select(
                        edge =>
                            edge.CallSiteStep.Kind)
                    .OrderBy(
                        static kind =>
                            kind)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.PropertyGetter,
                    ExceptionFlowPathStepKind.PropertySetter,
                    ExceptionFlowPathStepKind.BinaryOperatorCall
                }
                .OrderBy(
                    static kind =>
                        kind)
                .ToArray(),
                kinds);
        }

        /// <summary>
        /// Ensures that an implicit user-defined conversion creates a
        /// conversion edge.
        /// </summary>
        [Fact]
        public void ImplicitConversion_CreatesConversionEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Source source)
                    {
                        Target target = source;
                    }
                }

                public readonly struct Source
                {
                }

                public sealed class Target
                {
                    public static implicit operator Target(
                        Source source)
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge conversionEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.ConversionOperatorCall,
                conversionEdge.CallSiteStep.Kind);

            Assert.Equal(
                "op_Implicit",
                conversionEdge.Target.Symbol.Name);

            ExceptionFlowSummary conversionSummary =
                run.GetRequiredSummary(
                    conversionEdge.Target);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    conversionSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that an explicit user-defined conversion creates a
        /// conversion edge.
        /// </summary>
        [Fact]
        public void ExplicitConversion_CreatesConversionEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Source source)
                    {
                        _ = (Target)source;
                    }
                }

                public readonly struct Source
                {
                }

                public sealed class Target
                {
                    public static explicit operator Target(
                        Source source)
                    {
                        throw new ArgumentException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge conversionEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.ConversionOperatorCall,
                conversionEdge.CallSiteStep.Kind);

            Assert.Equal(
                "op_Explicit",
                conversionEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that a built-in numeric conversion does not create a
        /// callable edge.
        /// </summary>
        [Fact]
        public void BuiltInConversion_DoesNotCreateEdge()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(
                        int source)
                    {
                        long target = source;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);
        }

        /// <summary>
        /// Ensures that value facts are transferred to binary-operator
        /// parameters.
        /// </summary>
        [Fact]
        public void BinaryOperator_PropagatesOperandFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Guarded value)
                    {
                        _ = value + "known";
                    }
                }

                public readonly struct Guarded
                {
                    public static Guarded operator +(
                        Guarded left,
                        string? right)
                    {
                        ArgumentNullException.ThrowIfNull(right);
                        return left;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge operatorEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary operatorSummary =
                run.GetRequiredSummary(
                    operatorEdge.Target);

            Assert.Empty(
                operatorSummary.Sources);
        }

        /// <summary>
        /// Ensures that value facts are transferred to conversion-operator
        /// parameters.
        /// </summary>
        [Fact]
        public void ConversionOperator_PropagatesSourceFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Target target = "known";
                    }
                }

                public sealed class Target
                {
                    public static implicit operator Target(
                        string? source)
                    {
                        ArgumentNullException.ThrowIfNull(source);
                        return new Target();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge conversionEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary conversionSummary =
                run.GetRequiredSummary(
                    conversionEdge.Target);

            Assert.Empty(
                conversionSummary.Sources);
        }

        /// <summary>
        /// Ensures that a Boolean condition records a user-defined true
        /// operator.
        /// </summary>
        [Fact]
        public void BooleanCondition_CreatesTrueOperatorEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Flag value)
                    {
                        if (value)
                        {
                        }
                    }
                }

                public readonly struct Flag
                {
                    public static bool operator true(
                        Flag value)
                    {
                        throw new InvalidOperationException();
                    }

                    public static bool operator false(
                        Flag value)
                    {
                        return false;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge trueEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                ExceptionFlowPathStepKind.UnaryOperatorCall,
                trueEdge.CallSiteStep.Kind);

            Assert.Equal(
                "op_True",
                trueEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that conditional-and records both the false operator and
        /// the binary-and operator.
        /// </summary>
        [Fact]
        public void ConditionalAnd_CreatesFalseAndBinaryEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(
                        Flag left,
                        Flag right)
                    {
                        _ = left && right;
                    }
                }

                public readonly struct Flag
                {
                    public static Flag operator &(
                        Flag left,
                        Flag right)
                    {
                        return left;
                    }

                    public static bool operator true(
                        Flag value)
                    {
                        return true;
                    }

                    public static bool operator false(
                        Flag value)
                    {
                        return false;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] targetNames =
                run.RootSummary.CallEdges
                    .Select(
                        edge =>
                            edge.Target.Symbol.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "op_BitwiseAnd",
                    "op_False"
                },
                targetNames);
        }

        /// <summary>
        /// Ensures that conditional-or records both the true operator and the
        /// binary-or operator.
        /// </summary>
        [Fact]
        public void ConditionalOr_CreatesTrueAndBinaryEdges()
        {
            const string source =
                """
                public static class EntryPoint
                {
                    public static void M(
                        Flag left,
                        Flag right)
                    {
                        _ = left || right;
                    }
                }

                public readonly struct Flag
                {
                    public static Flag operator |(
                        Flag left,
                        Flag right)
                    {
                        return left;
                    }

                    public static bool operator true(
                        Flag value)
                    {
                        return true;
                    }

                    public static bool operator false(
                        Flag value)
                    {
                        return false;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] targetNames =
                run.RootSummary.CallEdges
                    .Select(
                        edge =>
                            edge.Target.Symbol.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "op_BitwiseOr",
                    "op_True"
                },
                targetNames);
        }

        /// <summary>
        /// Ensures that operators inside an uncalled lambda remain outside the
        /// containing method summary.
        /// </summary>
        [Fact]
        public void OperatorInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Number left,
                        Number right)
                    {
                        Action action =
                            () =>
                            {
                                _ = left + right;
                            };
                    }
                }

                public readonly struct Number
                {
                    public static Number operator +(
                        Number left,
                        Number right)
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
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);
        }

        /// <summary>
        /// Ensures that a lifted operator is not treated as executed when one
        /// operand is the constant null value.
        /// </summary>
        [Fact]
        public void LiftedOperatorWithConstantNull_DoesNotCreateEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Number? right)
                    {
                        Number? result =
                            ((Number?)null) + right;
                    }
                }

                public readonly struct Number
                {
                    public static Number operator +(
                        Number left,
                        Number right)
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
                run.RootSummary.CallEdges);

            Assert.Empty(
                run.RootSummary.Sources);
        }
    }
}
