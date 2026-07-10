using XMLDocNormalizer.Cli;

namespace XMLDocNormalizerTests.Cli
{
    /// <summary>
    /// Tests invalid command-line values for the exception analysis mode option.
    /// </summary>
    [Collection("Console-dependent tests")]
    public sealed class ArgParsing_InvalidExceptionAnalysisModeTests
    {
        /// <summary>
        /// Ensures that an invalid exception analysis mode is rejected with an <see cref="ArgumentException"/>.
        /// </summary>
        [Fact]
        public void InvalidExceptionAnalysisMode_IsRejected()
        {
            string[] args =
            [
                "--check",
                "--project", "Test.csproj",
                "--exception-analysis-mode", "totally-invalid-mode"
            ];

            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;

            StringWriter capturedOut = new();
            StringWriter capturedError = new();

            try
            {
                Console.SetOut(capturedOut);
                Console.SetError(capturedError);

                ArgumentException exception = Assert.Throws<ArgumentException>(
                    () => ArgParsing.TryParseOptions(args, out ToolOptions? _));

                Assert.Contains("Invalid exception analysis mode", exception.Message, StringComparison.Ordinal);

                                string output = capturedOut.ToString();

                Assert.Contains("project-transitive-declared-exceptions", output, StringComparison.Ordinal);
                Assert.Contains("solution-transitive", output, StringComparison.Ordinal);
                Assert.DoesNotContain("project-transitive-project-exceptions", output, StringComparison.Ordinal);
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
