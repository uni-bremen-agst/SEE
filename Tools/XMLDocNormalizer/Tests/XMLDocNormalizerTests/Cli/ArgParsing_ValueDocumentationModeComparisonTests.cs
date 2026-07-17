using XMLDocNormalizer.Cli;

namespace XMLDocNormalizerTests.Cli
{
    /// <summary>
    /// Tests parsing of the value-documentation mode comparison command-line option.
    /// </summary>
    [Collection("Console-dependent tests")]
    public sealed class ArgParsing_ValueDocumentationModeComparisonTests
    {
        /// <summary>
        /// Ensures that the value-documentation mode comparison flag is parsed correctly.
        /// </summary>
        [Fact]
        public void CompareValueDocumentationModesFlag_IsParsedCorrectly()
        {
            string[] args =
            [
                "--check",
                "--compare-value-documentation-modes"
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.True(options.CompareValueDocumentationModes);
            Assert.False(options.CompareExceptionAnalysisModes);
        }

        /// <summary>
        /// Ensures that the value-documentation mode comparison requires check mode.
        /// </summary>
        [Fact]
        public void CompareValueDocumentationModesFlag_WithoutCheckMode_IsRejected()
        {
            string[] args =
            [
                "--fix",
                "--compare-value-documentation-modes"
            ];

            string output = CaptureConsoleOutput(
                () =>
                {
                    bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

                    Assert.False(success);
                    Assert.Null(options);
                });

            Assert.Contains(
                "Option --compare-value-documentation-modes requires --check.",
                output,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that exception and value-documentation mode comparisons cannot be combined.
        /// </summary>
        [Fact]
        public void CompareValueDocumentationModesFlag_WithExceptionComparisonFlag_IsRejected()
        {
            string[] args =
            [
                "--check",
                "--compare-exception-analysis-modes",
                "--compare-value-documentation-modes"
            ];

            string output = CaptureConsoleOutput(
                () =>
                {
                    bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

                    Assert.False(success);
                    Assert.Null(options);
                });

            Assert.Contains(
                "Options --compare-exception-analysis-modes and --compare-value-documentation-modes cannot be used together.",
                output,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that the help output documents the value-documentation mode comparison flag.
        /// </summary>
        [Fact]
        public void HelpOutput_ContainsValueDocumentationModeComparisonFlag()
        {
            string[] args =
            [
                "--help"
            ];

            string output = CaptureConsoleOutput(
                () =>
                {
                    bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

                    Assert.False(success);
                    Assert.Null(options);
                });

            Assert.Contains("--compare-value-documentation-modes", output, StringComparison.Ordinal);
        }

        /// <summary>
        /// Captures console output produced by an action.
        /// </summary>
        /// <param name="action">
        /// The action to execute.
        /// </param>
        /// <returns>
        /// The captured standard output and standard error text.
        /// </returns>
        private static string CaptureConsoleOutput(Action action)
        {
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;

            StringWriter capturedOut = new();
            StringWriter capturedError = new();

            try
            {
                Console.SetOut(capturedOut);
                Console.SetError(capturedError);

                action();

                return capturedOut.ToString() + capturedError.ToString();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);

                capturedOut.Dispose();
                capturedError.Dispose();
            }
        }
    }
}
