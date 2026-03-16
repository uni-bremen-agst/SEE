using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Cli
{
    /// <summary>
    /// Tests parsing of the exception analysis mode command-line option.
    /// </summary>
    [Collection("Console-dependent tests")]
    public sealed class ArgParsing_ExceptionAnalysisModeTests
    {
        /// <summary>
        /// Ensures that the direct mode value is parsed correctly.
        /// </summary>
        [Fact]
        public void DirectMode_IsParsedCorrectly()
        {
            string[] args = ["--check", "--project", "Test.csproj", "--exception-analysis-mode", "direct"];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(ExceptionAnalysisMode.Direct, options.XmlDocOptions.ExceptionAnalysisMode);
        }

        /// <summary>
        /// Ensures that the project-transitive mode value is parsed correctly.
        /// </summary>
        [Fact]
        public void ProjectTransitiveMode_IsParsedCorrectly()
        {
            string[] args = ["--check", "--project", "Test.csproj", "--exception-analysis-mode", "project-transitive"];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(ExceptionAnalysisMode.ProjectTransitive, options.XmlDocOptions.ExceptionAnalysisMode);
        }

        /// <summary>
        /// Ensures that the project-transitive-project-exceptions mode value is parsed correctly.
        /// </summary>
        [Fact]
        public void ProjectTransitiveProjectExceptionsMode_IsParsedCorrectly()
        {
            string[] args =
            [
                "--check",
                "--project", "Test.csproj",
                "--exception-analysis-mode", "project-transitive-project-exceptions"
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(
                ExceptionAnalysisMode.ProjectTransitiveProjectExceptions,
                options.XmlDocOptions.ExceptionAnalysisMode);
        }

        /// <summary>
        /// Ensures that the solution-transitive mode value is parsed correctly.
        /// </summary>
        [Fact]
        public void SolutionTransitiveMode_IsParsedCorrectly()
        {
            string[] args = ["--check", "--project", "Test.csproj", "--exception-analysis-mode", "solution-transitive"];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(ExceptionAnalysisMode.SolutionTransitive, options.XmlDocOptions.ExceptionAnalysisMode);
        }
    }
}
