using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests nullability reasoning for thrown expressions in productive
    /// exception-flow summaries.
    /// </summary>
    public sealed class ExceptionFlowSummaryThrowNullabilityTests
    {
        /// <summary>
        /// Ensures that an explicitly created exception is known non-null even
        /// when nullable reference type analysis is disabled.
        /// </summary>
        [Fact]
        public void ObjectCreationWithNullableDisabled_DoesNotProduceUncertainty()
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

            Assert.False(
                run.Result.HasUncertainPaths);

            Assert.Empty(
                run.Result.UncertainTargets);

            Assert.Contains(
                run.Result.ThrownExceptions,
                exceptionType =>
                    exceptionType.ToDisplayString() ==
                    "System.InvalidOperationException");
        }
    }
}
