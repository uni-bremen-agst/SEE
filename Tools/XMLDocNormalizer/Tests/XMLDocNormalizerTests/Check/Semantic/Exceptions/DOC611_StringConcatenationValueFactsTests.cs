using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value facts derived from built-in C# string concatenations.
    /// </summary>
    public sealed class DOC611_StringConcatenationValueFactsTests
    {
        /// <summary>
        /// Ensures that fixed non-whitespace text in the left operand proves
        /// the complete concatenation non-null, non-empty, and non-whitespace.
        /// </summary>
        [Fact]
        public void FixedPrefix_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a message with a fixed prefix.
                    /// </summary>
                    public void M(string? value)
                    {
                        Validate(
                            "Prefix: " + value);
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that fixed non-whitespace text in the right operand proves
        /// the complete concatenation non-null, non-empty, and non-whitespace.
        /// </summary>
        [Fact]
        public void FixedSuffix_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a message with a fixed suffix.
                    /// </summary>
                    public void M(string? value)
                    {
                        Validate(
                            value + ".suffix");
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that concatenating two nullable strings proves only that
        /// the result is non-null while it may still be empty or whitespace.
        /// </summary>
        [Fact]
        public void UnknownOperands_ProduceOnlyArgumentException()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a concatenation of unknown values.
                    /// </summary>
                    public void M(
                        string? left,
                        string? right)
                    {
                        Validate(
                            left + right);
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding =
                Assert.Single(findings);

            Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a fixed whitespace segment proves the concatenation
        /// non-null and non-empty but not non-whitespace.
        /// </summary>
        [Fact]
        public void FixedWhitespace_StillProducesArgumentException()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a concatenation containing fixed whitespace.
                    /// </summary>
                    public void M(string? value)
                    {
                        Validate(
                            "  " + value);
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Finding finding =
                Assert.Single(findings);

            Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that facts from interpolated strings remain available when
        /// the interpolation is part of a larger concatenation.
        /// </summary>
        [Fact]
        public void InterpolatedOperandWithFixedText_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a concatenated diagnostic message.
                    /// </summary>
                    public void M(string? value)
                    {
                        Validate(
                            $"Value '{value}' " +
                            "could not be resolved.");
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a user-defined addition operator returning a string is
        /// not mistaken for the built-in string concatenation operation.
        /// </summary>
        [Fact]
        public void UserDefinedAddition_DoesNotReceiveStringFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public readonly struct Value
                {
                    public static string? operator +(
                        Value left,
                        Value right)
                    {
                        return null;
                    }
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a user-defined addition result.
                    /// </summary>
                    public void M(
                        Value left,
                        Value right)
                    {
                        Validate(
                            left + right);
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
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
                    finding.Message.Contains(
                        "System.ArgumentNullException",
                        StringComparison.Ordinal));

            Assert.Contains(
                findings,
                finding =>
                    finding.Message.Contains(
                        "System.ArgumentException",
                        StringComparison.Ordinal));
        }
    }
}
