using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
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
        /// Provides canonical exception analysis mode values and their expected enum value names.
        /// </summary>
        /// <returns>Canonical command-line values and expected mode names.</returns>
        public static IEnumerable<object[]> CanonicalModeValues()
        {
            yield return new object[]
            {
                "direct",
                "Direct"
            };

            yield return new object[]
            {
                "project-transitive-declared-exceptions",
                "ProjectTransitiveDeclaredExceptions"
            };

            yield return new object[]
            {
                "project-transitive",
                "ProjectTransitive"
            };

            yield return new object[]
            {
                "solution-transitive",
                "SolutionTransitive"
            };
        }

                /// <summary>
        /// Provides exception analysis mode aliases and their expected enum value names.
        /// </summary>
        /// <returns>Alias command-line values and expected mode names.</returns>
        public static IEnumerable<object[]> AliasModeValues()
        {
            yield return new object[]
            {
                "d",
                "Direct"
            };

            yield return new object[]
            {
                "ptd",
                "ProjectTransitiveDeclaredExceptions"
            };

            yield return new object[]
            {
                "declared",
                "ProjectTransitiveDeclaredExceptions"
            };

            yield return new object[]
            {
                "project-declared",
                "ProjectTransitiveDeclaredExceptions"
            };

            yield return new object[]
            {
                "project-transitive-declared",
                "ProjectTransitiveDeclaredExceptions"
            };

            yield return new object[]
            {
                "pt",
                "ProjectTransitive"
            };

            yield return new object[]
            {
                "project",
                "ProjectTransitive"
            };

            yield return new object[]
            {
                "st",
                "SolutionTransitive"
            };

            yield return new object[]
            {
                "solution",
                "SolutionTransitive"
            };
        }

        /// <summary>
        /// Ensures that omitting the exception analysis mode uses the central default.
        /// </summary>
        [Fact]
        public void WithoutExceptionAnalysisMode_UsesDefaultMode()
        {
            string[] args = ["--check"];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(
                XmlDocOptions.DefaultExceptionAnalysisMode,
                options.XmlDocOptions.ExceptionAnalysisMode);
        }

        /// <summary>
        /// Ensures that canonical exception analysis mode values are parsed correctly.
        /// </summary>
        /// <param name="value">The command-line mode value.</param>
        /// <param name="expectedModeName">The expected exception analysis mode name.</param>
        [Theory]
        [MemberData(nameof(CanonicalModeValues))]
        public void CanonicalModeValue_IsParsedCorrectly(
            string value,
            string expectedModeName)
        {
            string[] args =
            [
                "--check",
                "--exception-analysis-mode", value
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(expectedModeName, options.XmlDocOptions.ExceptionAnalysisMode.ToString());
        }

        /// <summary>
        /// Ensures that exception analysis mode aliases are parsed correctly.
        /// </summary>
        /// <param name="value">The command-line mode alias.</param>
        /// <param name="expectedModeName">The expected exception analysis mode name.</param>
        [Theory]
        [MemberData(nameof(AliasModeValues))]
        public void AliasModeValue_IsParsedCorrectly(
            string value,
            string expectedModeName)
        {
            string[] args =
            [
                "--check",
                "--exception-analysis-mode", value
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(expectedModeName, options.XmlDocOptions.ExceptionAnalysisMode.ToString());
        }

        /// <summary>
        /// Ensures that exception analysis mode parsing is case-insensitive.
        /// </summary>
        [Fact]
        public void ExceptionAnalysisMode_IsParsedCaseInsensitively()
        {
            string[] args =
            [
                "--check",
                "--exception-analysis-mode", "SOLUTION"
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal("SolutionTransitive", options.XmlDocOptions.ExceptionAnalysisMode.ToString());
        }
    }
}
