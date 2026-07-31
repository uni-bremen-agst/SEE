using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value facts derived from constructed strings and source-method
    /// return values.
    /// </summary>
    public sealed class ExceptionFlowReturnValueFactsTests
    {
        /// <summary>
        /// Ensures that fixed non-whitespace text in an interpolated string
        /// suppresses impossible null and whitespace guard exceptions.
        /// </summary>
        [Fact]
        public void InterpolatedStringWithFixedText_IsNonWhiteSpace()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(string? detail)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            $"Known prefix: {detail}");
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertNoNullOrWhiteSpaceGuardExceptions(
                run);
        }

        /// <summary>
        /// Ensures that concatenating a fixed non-whitespace string preserves
        /// non-whitespace facts even when the other operand may be null.
        /// </summary>
        [Fact]
        public void StringConcatenationWithFixedText_IsNonWhiteSpace()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(string? detail)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            "Known prefix: " + detail);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertNoNullOrWhiteSpaceGuardExceptions(
                run);
        }

        /// <summary>
        /// Ensures that facts from a supported source-method return value are
        /// propagated into a subsequently invoked guarded method.
        /// </summary>
        [Fact]
        public void SourceMethodReturnFacts_ArePropagated()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(string? detail)
                    {
                        AddUncertainTarget(
                            CreateMessage(detail));
                    }

                    private static string CreateMessage(
                        string? detail)
                    {
                        string resolvedDetail =
                            detail ??
                            "<unknown>";

                        return
                            $"Additional runtime dispatch targets for '{resolvedDetail}' " +
                            "may exist outside the analyzed project closure.";
                    }

                    private static void AddUncertainTarget(
                        string target)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            target);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertNoNullOrWhiteSpaceGuardExceptions(
                run);
        }

        /// <summary>
        /// Ensures that a source method that may return whitespace remains a
        /// possible <see cref="ArgumentException"/> source while its proven
        /// non-null return suppresses <see cref="ArgumentNullException"/>.
        /// </summary>
        [Fact]
        public void PossibleWhitespaceReturn_RemainsReported()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(bool whitespace)
                    {
                        Validate(
                            CreateMessage(whitespace));
                    }

                    private static string CreateMessage(
                        bool whitespace)
                    {
                        return whitespace
                            ? " "
                            : "value";
                    }

                    private static void Validate(
                        string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            value);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentException));

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }

        /// <summary>
        /// Ensures that neither exception produced by a null-or-whitespace
        /// guard remains in the analysis result.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer test run.
        /// </param>
        private static void AssertNoNullOrWhiteSpaceGuardExceptions(
            ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentException));

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }
    }
}
