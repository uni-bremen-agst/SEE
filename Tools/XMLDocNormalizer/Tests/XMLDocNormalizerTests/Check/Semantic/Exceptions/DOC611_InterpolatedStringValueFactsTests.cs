using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value facts derived from interpolated strings during transitive
    /// exception-flow analysis.
    /// </summary>
    public sealed class DOC611_InterpolatedStringValueFactsTests
    {
        /// <summary>
        /// Ensures that a fixed non-whitespace text segment proves that an
        /// interpolated string satisfies
        /// <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
        /// </summary>
        [Fact]
        public void InterpolatedStringWithFixedNonWhitespaceText_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Adds a suffix to a possibly null display name.
                    /// </summary>
                    public void M(
                        string? displayName,
                        bool isAdd)
                    {
                        string accessorName =
                            isAdd
                                ? "add"
                                : "remove";

                        Validate(
                            $"{displayName}.{accessorName}");
                    }

                    private static void Validate(
                        string? value)
                    {
                        ArgumentException
                            .ThrowIfNullOrWhiteSpace(
                                value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(
                findings);
        }

        /// <summary>
        /// Ensures that an interpolation without fixed text does not prove
        /// that the resulting string is non-empty or non-whitespace.
        /// </summary>
        [Fact]
        public void InterpolatedStringWithoutFixedText_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates an interpolated unknown value.
                    /// </summary>
                    public void M(
                        string? value)
                    {
                        Validate(
                            $"{value}");
                    }

                    private static void Validate(
                        string? value)
                    {
                        ArgumentException
                            .ThrowIfNullOrWhiteSpace(
                                value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding =>
                    finding.Smell.ID ==
                        XmlDocSmells
                            .MissingTransitiveExceptionDocumentation
                            .ID &&
                    finding.Message.Contains(
                        "System.ArgumentException",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that fixed whitespace text alone does not prove that an
        /// interpolated string contains a non-whitespace character.
        /// </summary>
        [Fact]
        public void InterpolatedStringWithOnlyFixedWhitespace_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates an interpolated value surrounded by spaces.
                    /// </summary>
                    public void M(
                        string? value)
                    {
                        Validate(
                            $"  {value}  ");
                    }

                    private static void Validate(
                        string? value)
                    {
                        ArgumentException
                            .ThrowIfNullOrWhiteSpace(
                                value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding =>
                    finding.Smell.ID ==
                        XmlDocSmells
                            .MissingTransitiveExceptionDocumentation
                            .ID &&
                    finding.Message.Contains(
                        "System.ArgumentException",
                        StringComparison.Ordinal));
        }
    }
}
