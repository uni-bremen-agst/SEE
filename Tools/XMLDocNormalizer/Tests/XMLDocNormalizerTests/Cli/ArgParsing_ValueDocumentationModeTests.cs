using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;

namespace XMLDocNormalizerTests.Cli
{
    /// <summary>
    /// Tests parsing of the value-documentation mode command-line option.
    /// </summary>
    [Collection("Console-dependent tests")]
    public sealed class ArgParsing_ValueDocumentationModeTests
    {
        /// <summary>
        /// Provides canonical value-documentation mode values and their expected enum value names.
        /// </summary>
        /// <returns>Canonical command-line values and expected mode names.</returns>
        public static IEnumerable<object[]> CanonicalModeValues()
        {
            yield return new object[]
            {
                "disabled",
                "None"
            };

            yield return new object[]
            {
                "all-readable-properties",
                "AllReadableProperties"
            };

            yield return new object[]
            {
                "exclude-dto-like-types",
                "ExcludeDtoLikeTypes"
            };

            yield return new object[]
            {
                "indexers-only",
                "IndexersOnly"
            };
        }

        /// <summary>
        /// Provides value-documentation mode aliases and their expected enum value names.
        /// </summary>
        /// <returns>Alias command-line values and expected mode names.</returns>
        public static IEnumerable<object[]> AliasModeValues()
        {
            yield return new object[]
            {
                "off",
                "None"
            };

            yield return new object[]
            {
                "none",
                "None"
            };

            yield return new object[]
            {
                "all",
                "AllReadableProperties"
            };

            yield return new object[]
            {
                "strict",
                "AllReadableProperties"
            };

            yield return new object[]
            {
                "non-dto",
                "ExcludeDtoLikeTypes"
            };

            yield return new object[]
            {
                "exclude-dto",
                "ExcludeDtoLikeTypes"
            };

            yield return new object[]
            {
                "indexers",
                "IndexersOnly"
            };

            yield return new object[]
            {
                "indexer-only",
                "IndexersOnly"
            };
        }

        /// <summary>
        /// Ensures that omitting the value-documentation mode uses the central default.
        /// </summary>
        [Fact]
        public void WithoutValueDocumentationMode_UsesDefaultMode()
        {
            string[] args = ["--check"];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(
                XmlDocOptions.DefaultValueDocumentationMode,
                options.XmlDocOptions.ValueDocumentationMode);
        }

        /// <summary>
        /// Ensures that canonical value-documentation mode values are parsed correctly.
        /// </summary>
        /// <param name="value">The command-line mode value.</param>
        /// <param name="expectedModeName">The expected value-documentation mode name.</param>
        [Theory]
        [MemberData(nameof(CanonicalModeValues))]
        public void CanonicalModeValue_IsParsedCorrectly(
            string value,
            string expectedModeName)
        {
            string[] args =
            [
                "--check",
        "--value-documentation-mode",
        value
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(expectedModeName, options.XmlDocOptions.ValueDocumentationMode.ToString());
        }

        /// <summary>
        /// Ensures that value-documentation mode aliases are parsed correctly.
        /// </summary>
        /// <param name="value">The command-line mode alias.</param>
        /// <param name="expectedModeName">The expected value-documentation mode name.</param>
        [Theory]
        [MemberData(nameof(AliasModeValues))]
        public void AliasModeValue_IsParsedCorrectly(
            string value,
            string expectedModeName)
        {
            string[] args =
            [
                "--check",
                "--value-documentation-mode",
                value
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(expectedModeName, options.XmlDocOptions.ValueDocumentationMode.ToString());
        }

        /// <summary>
        /// Ensures that value-documentation mode parsing is case-insensitive.
        /// </summary>
        [Fact]
        public void ValueDocumentationMode_IsParsedCaseInsensitively()
        {
            string[] args =
            [
                "--check",
                "--value-documentation-mode",
                "NON-DTO"
            ];

            bool success = ArgParsing.TryParseOptions(args, out ToolOptions? options);

            Assert.True(success);
            Assert.NotNull(options);
            Assert.Equal(
                ValueDocumentationMode.ExcludeDtoLikeTypes,
                options.XmlDocOptions.ValueDocumentationMode);
        }

        /// <summary>
        /// Ensures that invalid value-documentation modes are rejected.
        /// </summary>
        [Fact]
        public void InvalidValueDocumentationMode_IsRejected()
        {
            string[] args =
            [
                "--check",
                "--value-documentation-mode",
                "invalid-value-mode"
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

                Assert.Contains(
                    "Invalid value documentation mode",
                    exception.Message,
                    StringComparison.Ordinal);

                string output = capturedOut.ToString();

                Assert.Contains("disabled", output, StringComparison.Ordinal);
                Assert.Contains("all-readable-properties", output, StringComparison.Ordinal);
                Assert.Contains("exclude-dto-like-types", output, StringComparison.Ordinal);
                Assert.Contains("indexers-only", output, StringComparison.Ordinal);
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
