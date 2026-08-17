using System.Text;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests productive expansion of completely constructed exception-flow
    /// summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphEvaluationTests
    {
        /// <summary>
        /// Ensures that a direct root source remains a single-step path.
        /// </summary>
        [Fact]
        public void DirectRootSource_RemainsSingleStepPath()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            ExceptionFlowPath path =
                Assert.Single(
                    run.Result.GetExceptionPaths(
                        exceptionType));

            ExceptionFlowPathStep step =
                Assert.Single(
                    path.Steps);

            Assert.Equal(
                ExceptionFlowPathStepKind.ExplicitThrow,
                step.Kind);
        }

        /// <summary>
        /// Ensures that multiple summary edges are prepended in root-to-source
        /// order.
        /// </summary>
        [Fact]
        public void MultiHopCall_ExpandsOrderedPath()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        First();
                    }

                    private static void First()
                    {
                        Second();
                    }

                    private static void Second()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            ExceptionFlowPath path =
                Assert.Single(
                    run.Result.GetExceptionPaths(
                        exceptionType));

            ExceptionFlowPathStepKind[] kinds =
                path.Steps
                    .Select(
                        static step =>
                            step.Kind)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    ExceptionFlowPathStepKind.MethodCall,
                    ExceptionFlowPathStepKind.MethodCall,
                    ExceptionFlowPathStepKind.ExplicitThrow
                },
                kinds);

            Assert.Contains(
                "First",
                path.Steps[0].SymbolName,
                StringComparison.Ordinal);

            Assert.Contains(
                "Second",
                path.Steps[1].SymbolName,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a typed catch on an outer call edge suppresses a
        /// matching exception produced several summaries deeper.
        /// </summary>
        [Fact]
        public void OuterTypedCatch_SuppressesDeepException()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        try
                        {
                            First();
                        }
                        catch (ArgumentException)
                        {
                        }
                    }

                    private static void First()
                    {
                        Second();
                    }

                    private static void Second()
                    {
                        throw new ArgumentNullException();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.DoesNotContain(
                exceptionType,
                run.Result.ThrownExceptions);

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    exceptionType));
        }

        /// <summary>
        /// Ensures that an unrelated typed catch does not suppress a deep
        /// exception.
        /// </summary>
        [Fact]
        public void UnrelatedTypedCatch_PreservesDeepException()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        try
                        {
                            First();
                        }
                        catch (ArgumentException)
                        {
                        }
                    }

                    private static void First()
                    {
                        Second();
                    }

                    private static void Second()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    exceptionType));
        }

        /// <summary>
        /// Ensures that mutually recursive summaries terminate and do not
        /// create infinitely repeated paths.
        /// </summary>
        [Fact]
        public void RecursiveCycle_ProducesFinitePathSet()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        First();
                    }

                    private static void First()
                    {
                        Second();
                    }

                    private static void Second()
                    {
                        First();
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            ExceptionFlowPath path =
                Assert.Single(
                    run.Result.GetExceptionPaths(
                        exceptionType));

            Assert.Equal(
                3,
                path.Steps.Count);
        }

        /// <summary>
        /// Ensures that an interface target without a complete executable
        /// implementation set leaves productive uncertainty.
        /// </summary>
        [Fact]
        public void MissingExecutableTarget_AddsUncertainty()
        {
            const string source =
                """
                public interface IService
                {
                    void Execute();
                }

                public static class EntryPoint
                {
                    public static void M(IService service)
                    {
                        service.Execute();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            Assert.True(
                run.Result.HasUncertainPaths);

            Assert.NotEmpty(
                run.Result.UncertainTargets);
        }

        /// <summary>
        /// Ensures that context-sensitive summaries preserve only the unsafe
        /// invocation of a guarded method.
        /// </summary>
        [Fact]
        public void ContextSensitiveCalls_PreserveOnlyUnsafePath()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(object? value)
                    {
                        Validate("known");
                        Validate(value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    exceptionType));
        }

        /// <summary>
        /// Ensures that productive graph expansion retains the configured
        /// maximum number of distinct paths and records truncation.
        /// </summary>
        [Fact]
        public void MoreThanMaximumPaths_AreTruncated()
        {
            StringBuilder sourceBuilder =
                new();

            sourceBuilder.AppendLine(
                "using System;");

            sourceBuilder.AppendLine(
                "public static class EntryPoint");

            sourceBuilder.AppendLine(
                "{");

            sourceBuilder.AppendLine(
                "    public static void M()");

            sourceBuilder.AppendLine(
                "    {");

            for (int callIndex = 0;
                 callIndex < 65;
                 callIndex++)
            {
                sourceBuilder.AppendLine(
                    "        ThrowingCall();");
            }

            sourceBuilder.AppendLine(
                "    }");

            sourceBuilder.AppendLine(
                "    private static void ThrowingCall()");

            sourceBuilder.AppendLine(
                "    {");

            sourceBuilder.AppendLine(
                "        throw new InvalidOperationException();");

            sourceBuilder.AppendLine(
                "    }");

            sourceBuilder.AppendLine(
                "}");

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        sourceBuilder.ToString(),
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            Assert.Equal(
                64,
                run.Result.GetExceptionPaths(
                        exceptionType)
                    .Count);

            Assert.True(
                run.Result.ArePathsTruncated(
                    exceptionType));
        }

        /// <summary>
        /// Ensures that a large shared diamond graph reuses completed node
        /// results instead of enumerating every complete root-to-source path.
        /// </summary>
        [Fact]
        public void SharedDiamondGraph_ReusesCompletedNodeResults()
        {
            const int levelCount =
                24;

            StringBuilder sourceBuilder =
                new();

            sourceBuilder.AppendLine(
                "using System;");

            sourceBuilder.AppendLine(
                "public static class EntryPoint");

            sourceBuilder.AppendLine(
                "{");

            sourceBuilder.AppendLine(
                "    public static void M()");

            sourceBuilder.AppendLine(
                "    {");

            sourceBuilder.AppendLine(
                "        Left0();");

            sourceBuilder.AppendLine(
                "    }");

            for (int level = 0;
                 level < levelCount;
                 level++)
            {
                int nextLevel =
                    level + 1;

                sourceBuilder.AppendLine(
                    $"    private static void Left{level}()");

                sourceBuilder.AppendLine(
                    "    {");

                sourceBuilder.AppendLine(
                    $"        Left{nextLevel}();");

                sourceBuilder.AppendLine(
                    $"        Right{nextLevel}();");

                sourceBuilder.AppendLine(
                    "    }");

                sourceBuilder.AppendLine(
                    $"    private static void Right{level}()");

                sourceBuilder.AppendLine(
                    "    {");

                sourceBuilder.AppendLine(
                    $"        Left{nextLevel}();");

                sourceBuilder.AppendLine(
                    $"        Right{nextLevel}();");

                sourceBuilder.AppendLine(
                    "    }");
            }

            sourceBuilder.AppendLine(
                $"    private static void Left{levelCount}()");

            sourceBuilder.AppendLine(
                "    {");

            sourceBuilder.AppendLine(
                "        ThrowingCall();");

            sourceBuilder.AppendLine(
                "    }");

            sourceBuilder.AppendLine(
                $"    private static void Right{levelCount}()");

            sourceBuilder.AppendLine(
                "    {");

            sourceBuilder.AppendLine(
                "        ThrowingCall();");

            sourceBuilder.AppendLine(
                "    }");

            sourceBuilder.AppendLine(
                "    private static void ThrowingCall()");

            sourceBuilder.AppendLine(
                "    {");

            sourceBuilder.AppendLine(
                "        throw new InvalidOperationException();");

            sourceBuilder.AppendLine(
                "    }");

            sourceBuilder.AppendLine(
                "}");

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        sourceBuilder.ToString(),
                        "M");

            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    "System.InvalidOperationException");

            Assert.Equal(
                64,
                run.Result.GetExceptionPaths(
                        exceptionType)
                    .Count);

            Assert.True(
                run.Result.ArePathsTruncated(
                    exceptionType));
        }
    }
}
