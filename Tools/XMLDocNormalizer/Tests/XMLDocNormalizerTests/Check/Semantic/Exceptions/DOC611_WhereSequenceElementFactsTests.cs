using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies preservation of non-null sequence-element facts through
    /// filtering LINQ operations.
    /// </summary>
    public sealed class DOC611_WhereSequenceElementFactsTests
    {
        /// <summary>
        /// Ensures that <c>Enumerable.Where</c> preserves a non-null element
        /// guarantee established by <c>Enumerable.OfType&lt;T&gt;</c>.
        /// </summary>
        [Fact]
        public void OfTypeElementsThroughWhereAndToArray_RemainNonNull()
        {
            const string source =
                """
                #nullable enable
                using System.Collections.Generic;
                using System.Linq;

                public sealed class TestClass
                {
                    /// <summary>Validates filtered values.</summary>
                    public void M(IEnumerable<object?> values)
                    {
                        foreach (string value in values
                                     .OfType<string>()
                                     .Where(static _ => true)
                                     .ToArray())
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(string? value)
                    {
                        System.ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Assert.Empty(
                projectFindings);

            Assert.Empty(
                solutionFindings);
        }

        /// <summary>
        /// Ensures that an element-transforming LINQ operation does not inherit
        /// a non-null element guarantee from its source sequence.
        /// </summary>
        [Fact]
        public void OfTypeElementsThroughNullableSelect_RemainPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System.Collections.Generic;
                using System.Linq;

                public sealed class TestClass
                {
                    /// <summary>Validates transformed values.</summary>
                    public void M(IEnumerable<object?> values)
                    {
                        foreach (string? value in values
                                     .OfType<string>()
                                     .Select(static _ => (string?)null)
                                     .ToArray())
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(string? value)
                    {
                        System.ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            AssertArgumentNullExceptionPresent(
                projectFindings);

            AssertArgumentNullExceptionPresent(
                solutionFindings);
        }

        /// <summary>
        /// Asserts that the findings contain a missing transitive
        /// <see cref="ArgumentNullException"/> documentation issue.
        /// </summary>
        /// <param name="findings">
        /// The findings to inspect.
        /// </param>
        private static void AssertArgumentNullExceptionPresent(
            IReadOnlyList<Finding> findings)
        {
            Assert.Contains(
                findings,
                static finding =>
                    finding.Smell.ID ==
                        XmlDocSmells
                            .MissingTransitiveExceptionDocumentation
                            .ID &&
                    string.Equals(
                        finding.Context.TargetName,
                        "System.ArgumentNullException",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that reading the length of a materialized array does not invalidate
        /// a previously established non-null element guarantee.
        /// </summary>
        [Fact]
        public void WhereToArrayElementsAcrossLengthObservation_RemainNonNull()
        {
            const string source =
                """
        #nullable enable
        using System.Collections.Generic;
        using System.Linq;

        public sealed class TestClass
        {
            /// <summary>Validates filtered values.</summary>
            public void M(IEnumerable<object?> values)
            {
                string[] runtimeTargets =
                    values
                        .OfType<string>()
                        .Where(static _ => true)
                        .ToArray();

                if (runtimeTargets.Length == 0)
                {
                    return;
                }

                foreach (string runtimeTarget in runtimeTargets)
                {
                    Validate(runtimeTarget);
                }
            }

            private static void Validate(string? value)
            {
                System.ArgumentNullException.ThrowIfNull(value);
            }
        }
        """;

            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Assert.Empty(
                projectFindings);

            Assert.Empty(
                solutionFindings);
        }
    }
}
