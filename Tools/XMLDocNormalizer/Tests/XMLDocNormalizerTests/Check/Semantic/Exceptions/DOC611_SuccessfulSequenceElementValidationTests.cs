using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests non-null sequence-element facts established by successful
    /// completion of source-level validation helpers.
    /// </summary>
    public sealed class DOC611_SuccessfulSequenceElementValidationTests
    {
        /// <summary>
        /// Ensures that successful complete validation of a local list proves
        /// its elements non-null for a later helper call.
        /// </summary>
        [Fact]
        public void SuccessfulValidationHelper_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    /// <summary>
                    /// Validates and consumes sequence elements.
                    /// </summary>
                    public static void M(List<object?> values)
                    {
                        ValidateAll(values);
                        Consume(values);
                    }

                    private static void ValidateAll(
                        IReadOnlyList<object?> values)
                    {
                        if (values == null || values.Count == 0)
                        {
                            return;
                        }

                        foreach (object? value in values)
                        {
                            _ = value.ToString();
                        }
                    }

                    private static void Consume(
                        IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
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
        /// Ensures that a helper which stops enumeration early does not prove
        /// the remaining sequence elements non-null.
        /// </summary>
        [Fact]
        public void ValidationHelperWithBreak_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    /// <summary>
                    /// Partially validates and consumes sequence elements.
                    /// </summary>
                    public static void M(List<object?> values)
                    {
                        ValidateFirst(values);
                        Consume(values);
                    }

                    private static void ValidateFirst(
                        IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            _ = value.ToString();
                            break;
                        }
                    }

                    private static void Consume(
                        IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that mutation after successful validation invalidates the
        /// sequence-element fact.
        /// </summary>
        [Fact]
        public void NullInsertedAfterValidation_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    /// <summary>
                    /// Mutates a validated sequence before consuming it.
                    /// </summary>
                    public static void M(List<object?> values)
                    {
                        ValidateAll(values);
                        values.Add(null);
                        Consume(values);
                    }

                    private static void ValidateAll(
                        IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            _ = value.ToString();
                        }
                    }

                    private static void Consume(
                        IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that conditionally skipped validation does not establish a
        /// sequence-element fact.
        /// </summary>
        [Fact]
        public void ConditionalValidation_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    /// <summary>
                    /// Conditionally validates sequence elements.
                    /// </summary>
                    public static void M(
                        List<object?> values,
                        bool validate)
                    {
                        ValidateConditionally(values, validate);
                        Consume(values);
                    }

                    private static void ValidateConditionally(
                        IReadOnlyList<object?> values,
                        bool validate)
                    {
                        if (!validate)
                        {
                            return;
                        }

                        foreach (object? value in values)
                        {
                            _ = value.ToString();
                        }
                    }

                    private static void Consume(
                        IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that switch-local break statements after an unconditional element
        /// dereference do not invalidate successful sequence validation.
        /// </summary>
        [Fact]
        public void ValidationHelperWithSwitchBreak_DoesNotProduceFinding()
        {
            const string source =
                """
        #nullable enable
        using System;
        using System.Collections.Generic;

        public sealed class Item
        {
            public int Severity { get; init; }
        }

        public static class TestClass
        {
            /// <summary>
            /// Validates and consumes sequence elements.
            /// </summary>
            public static void M(List<Item?> values)
            {
                ValidateAll(values);
                Consume(values);
            }

            private static void ValidateAll(
                IReadOnlyList<Item?> values)
            {
                if (values == null || values.Count == 0)
                {
                    return;
                }

                foreach (Item? value in values)
                {
                    int severity = value.Severity;

                    switch (severity)
                    {
                        case 0:
                            break;

                        case 1:
                            break;

                        default:
                            break;
                    }
                }
            }

            private static void Consume(
                IReadOnlyList<Item?> values)
            {
                foreach (Item? value in values)
                {
                    Validate(value);
                }
            }

            private static void Validate(Item? value)
            {
                ArgumentNullException.ThrowIfNull(value);
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
        /// Verifies that an ArgumentNullException finding remains present.
        /// </summary>
        private static void AssertArgumentNullFinding(string source)
        {
            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }
    }
}
