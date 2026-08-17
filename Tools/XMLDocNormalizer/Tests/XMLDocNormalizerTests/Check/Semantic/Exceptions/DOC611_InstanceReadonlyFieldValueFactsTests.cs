using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value facts derived from instance readonly fields initialized by
    /// constructors.
    /// </summary>
    public sealed class DOC611_InstanceReadonlyFieldValueFactsTests
    {
        /// <summary>
        /// Ensures that a readonly field assigned after a successful
        /// null-or-whitespace guard retains the proven string facts.
        /// </summary>
        [Fact]
        public void GuardedConstructorAssignment_DoesNotProduceFinding()
        {
            const string source =
                """
                using System;

                public sealed class Reporter
                {
                    private readonly string outputPath;

                    public Reporter(string outputPath)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            outputPath);

                        this.outputPath =
                            outputPath;
                    }

                    public void Complete()
                    {
                        Write(
                            outputPath);
                    }

                    private static void Write(string outputPath)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            outputPath);
                    }
                }

                public static class EntryPoint
                {
                    public static void M()
                    {
                        new Reporter("report.json").Complete();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            AssertNoNullOrWhiteSpaceExceptions(
                run);
        }

        /// <summary>
        /// Ensures that a terminal constructor reached through
        /// <c>this(...)</c> establishes the readonly-field facts.
        /// </summary>
        [Fact]
        public void DelegatingConstructor_UsesTerminalAssignmentFacts()
        {
            const string source =
                """
                using System;

                public sealed class Reporter
                {
                    private readonly string outputPath;

                    public Reporter()
                        : this("report.json")
                    {
                    }

                    private Reporter(string outputPath)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            outputPath);

                        this.outputPath =
                            outputPath;
                    }

                    public void Complete()
                    {
                        Write(
                            outputPath);
                    }

                    private static void Write(string outputPath)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            outputPath);
                    }
                }

                public static class EntryPoint
                {
                    public static void M()
                    {
                        new Reporter().Complete();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            AssertNoNullOrWhiteSpaceExceptions(
                run);
        }

        /// <summary>
        /// Ensures that a constructor that may store whitespace does not gain
        /// an incorrect non-whitespace fact.
        /// </summary>
        [Fact]
        public void PossiblyWhitespaceConstructorAssignment_RemainsReported()
        {
            const string source =
                """
                using System;

                public sealed class Reporter
                {
                    private readonly string outputPath;

                    public Reporter(bool whitespace)
                    {
                        outputPath =
                            whitespace
                                ? " "
                                : "report.json";
                    }

                    public void Complete()
                    {
                        Write(
                            outputPath);
                    }

                    private static void Write(string outputPath)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(
                            outputPath);
                    }
                }

                public static class EntryPoint
                {
                    public static void M(bool whitespace)
                    {
                        new Reporter(whitespace).Complete();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
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
        /// Ensures that null and whitespace guard exceptions are absent.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        private static void AssertNoNullOrWhiteSpaceExceptions(
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
