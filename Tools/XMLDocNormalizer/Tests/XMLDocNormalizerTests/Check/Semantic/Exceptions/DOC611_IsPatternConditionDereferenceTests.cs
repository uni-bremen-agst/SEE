using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies non-null value facts established by successful evaluation of
    /// pattern-matching branch conditions.
    /// </summary>
    public sealed class DOC611_IsPatternConditionDereferenceTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that evaluating the expression
        /// of an <c>is</c> pattern can prove its receiver non-null.
        /// </summary>
        [Fact]
        public void MemberDereferenceInsideIsPattern_ProjectTransitive_ProvesReceiverNonNull()
        {
            AssertMemberDereferenceInsideIsPatternProvesReceiverNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that evaluating the expression
        /// of an <c>is</c> pattern can prove its receiver non-null.
        /// </summary>
        [Fact]
        public void MemberDereferenceInsideIsPattern_SolutionTransitive_ProvesReceiverNonNull()
        {
            AssertMemberDereferenceInsideIsPatternProvesReceiverNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that conditional access inside an
        /// <c>is</c> pattern does not prove its receiver non-null.
        /// </summary>
        [Fact]
        public void ConditionalAccessInsideIsPattern_ProjectTransitive_DoesNotProveReceiverNonNull()
        {
            AssertConditionalAccessInsideIsPatternDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that conditional access inside
        /// an <c>is</c> pattern does not prove its receiver non-null.
        /// </summary>
        [Fact]
        public void ConditionalAccessInsideIsPattern_SolutionTransitive_DoesNotProveReceiverNonNull()
        {
            AssertConditionalAccessInsideIsPatternDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies the mandatory member-dereference pattern scenario.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertMemberDereferenceInsideIsPatternProvesReceiverNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable

                public sealed class TestClass
                {
                    /// <summary>Validates an item after evaluating a pattern.</summary>
                    public void M(Item? item)
                    {
                        if (item.Value is not object value)
                        {
                            Validate(item);
                            return;
                        }

                        _ = value;
                    }

                    private static void Validate(Item? item)
                    {
                        System.ArgumentNullException.ThrowIfNull(item);
                    }

                    public sealed class Item
                    {
                        public object? Value { get; set; }
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(
                findings);
        }

        /// <summary>
        /// Verifies that null-safe conditional access inside a pattern does not
        /// establish a non-null receiver fact.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertConditionalAccessInsideIsPatternDoesNotProveReceiverNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable

                public sealed class TestClass
                {
                    /// <summary>Validates an optionally accessed item.</summary>
                    public void M(Item? item)
                    {
                        if (item?.Value is not object)
                        {
                            Validate(item);
                        }
                    }

                    private static void Validate(Item? item)
                    {
                        System.ArgumentNullException.ThrowIfNull(item);
                    }

                    public sealed class Item
                    {
                        public object? Value { get; set; }
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Finding finding =
                Assert.Single(
                    findings);

            Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID);

            Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName);
        }
    }
}
