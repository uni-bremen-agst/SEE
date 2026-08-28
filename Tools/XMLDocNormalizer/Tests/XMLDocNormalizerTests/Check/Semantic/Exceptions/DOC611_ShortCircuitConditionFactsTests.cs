using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value facts established by earlier short-circuit Boolean
    /// operands.
    /// </summary>
    public sealed class DOC611_ShortCircuitConditionFactsTests
    {
        /// <summary>
        /// Ensures that a non-null comparison in an earlier AND operand
        /// supplies a non-null fact to a later method call.
        /// </summary>
        [Fact]
        public void EarlierAndOperandProvesNonNull_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Uses a value only after an earlier AND operand proves
                    /// it to be non-null.
                    /// </summary>
                    public static void M(object? value)
                    {
                        if (value != null
                            && Forward(value))
                        {
                        }
                    }

                    private static bool Forward(object? value)
                    {
                        Validate(value);
                        return true;
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
        /// Ensures that the false result of an earlier OR operand supplies the
        /// corresponding non-null fact to a later method call.
        /// </summary>
        [Fact]
        public void EarlierOrOperandFalseProvesNonNull_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Uses a value only when an earlier null comparison was
                    /// false.
                    /// </summary>
                    public static void M(object? value)
                    {
                        if (value == null
                            || Forward(value))
                        {
                        }
                    }

                    private static bool Forward(object? value)
                    {
                        Validate(value);
                        return true;
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
        /// Ensures that an unrelated earlier Boolean operand does not invent a
        /// non-null fact for a later argument.
        /// </summary>
        [Fact]
        public void UnrelatedEarlierOperand_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Passes a nullable value after an unrelated condition.
                    /// </summary>
                    public static void M(object? value, bool enabled)
                    {
                        if (enabled
                            && Forward(value))
                        {
                        }
                    }

                    private static bool Forward(object? value)
                    {
                        Validate(value);
                        return true;
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

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }
    }
}
