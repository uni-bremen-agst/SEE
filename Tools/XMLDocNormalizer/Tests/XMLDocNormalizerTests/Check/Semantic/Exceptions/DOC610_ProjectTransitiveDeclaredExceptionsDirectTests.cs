using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies direct exception requirements in
    /// ProjectTransitiveDeclaredExceptions mode.
    /// </summary>
    public sealed class
        DOC610_ProjectTransitiveDeclaredExceptionsDirectTests
    {
        /// <summary>
        /// Ensures that a directly thrown framework exception remains a
        /// documentation requirement.
        /// </summary>
        [Fact]
        public void UndocumentedDirectFrameworkException_ProducesDoc610()
        {
            const string member =
                """
                /// <summary>Executes the operation.</summary>
                public void M()
                {
                    throw new System.ArgumentException();
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode
                        .ProjectTransitiveDeclaredExceptions);

            Finding finding =
                Assert.Single(
                    findings,
                    current =>
                        current.Smell.ID ==
                        XmlDocSmells.MissingExceptionTag.ID);

            Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a directly invoked framework throw helper remains a
        /// documentation requirement.
        /// </summary>
        [Fact]
        public void UndocumentedDirectFrameworkThrowHelper_ProducesDoc610()
        {
            const string member =
                """
                /// <summary>Executes the operation.</summary>
                public void M(object? value)
                {
                    System.ArgumentNullException.ThrowIfNull(value);
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode
                        .ProjectTransitiveDeclaredExceptions);

            Finding finding =
                Assert.Single(
                    findings,
                    current =>
                        current.Smell.ID ==
                        XmlDocSmells.MissingExceptionTag.ID);

            Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a documented directly thrown framework exception is
        /// accepted even though framework exception types are not relevant
        /// for transitive requirements in this mode.
        /// </summary>
        [Fact]
        public void
            DocumentedDirectFrameworkException_ProducesNoExceptionFlowFinding()
        {
            const string member =
                """
                /// <summary>Executes the operation.</summary>
                /// <exception cref="System.ArgumentException">
                /// Thrown when the operation cannot accept its input.
                /// </exception>
                public void M()
                {
                    throw new System.ArgumentException();
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForMember(
                    member,
                    ExceptionAnalysisMode
                        .ProjectTransitiveDeclaredExceptions);

            Assert.DoesNotContain(
                findings,
                current =>
                    current.Smell.ID ==
                        XmlDocSmells.MissingExceptionTag.ID ||
                    current.Smell.ID ==
                        XmlDocSmells.ExceptionFlowNotDecidable.ID ||
                    current.Smell.ID ==
                        XmlDocSmells
                            .ExceptionTagWithoutTransitiveThrow
                            .ID);
        }
    }
}
