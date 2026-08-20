using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies non-null return-value reasoning for source parameters whose
    /// value is proven by preceding control flow inside the callee.
    /// </summary>
    public sealed class DOC611_SourceReturnSuccessfulDereferenceTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that a parameter dereferenced
        /// before being returned is known non-null on normal return.
        /// </summary>
        [Fact]
        public void ParameterDereferencedBeforeReturn_ProjectTransitive_ProvesReturnedValueNonNull()
        {
            AssertParameterDereferencedBeforeReturnProvesReturnedValueNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a parameter dereferenced
        /// before being returned is known non-null on normal return.
        /// </summary>
        [Fact]
        public void ParameterDereferencedBeforeReturn_SolutionTransitive_ProvesReturnedValueNonNull()
        {
            AssertParameterDereferencedBeforeReturnProvesReturnedValueNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a terminating null branch
        /// proves a parameter non-null at a later return.
        /// </summary>
        [Fact]
        public void ParameterProtectedByTerminatingNullBranch_ProjectTransitive_ProvesReturnedValueNonNull()
        {
            AssertParameterProtectedByTerminatingNullBranchProvesReturnedValueNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a terminating null branch
        /// proves a parameter non-null at a later return.
        /// </summary>
        [Fact]
        public void ParameterProtectedByTerminatingNullBranch_SolutionTransitive_ProvesReturnedValueNonNull()
        {
            AssertParameterProtectedByTerminatingNullBranchProvesReturnedValueNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a merely conditional
        /// dereference does not make the returned parameter non-null.
        /// </summary>
        [Fact]
        public void ConditionallyDereferencedParameter_ProjectTransitive_RemainsPotentiallyNull()
        {
            AssertConditionallyDereferencedParameterRemainsPotentiallyNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a merely conditional
        /// dereference does not make the returned parameter non-null.
        /// </summary>
        [Fact]
        public void ConditionallyDereferencedParameter_SolutionTransitive_RemainsPotentiallyNull()
        {
            AssertConditionallyDereferencedParameterRemainsPotentiallyNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies that successful dereference of a parameter before its
        /// return proves the invocation result non-null.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertParameterDereferencedBeforeReturnProvesReturnedValueNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates a value returned after dereference.</summary>
                    public void M(Item? item)
                    {
                        Item? normalized = Preserve(item);
                        Validate(normalized);
                    }

                    private static Item? Preserve(Item? item)
                    {
                        _ = item.Name;
                        return item;
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }

                    public sealed class Item
                    {
                        public string Name => string.Empty;
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that a terminating null branch proves the parameter
        /// non-null at the later return statement.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertParameterProtectedByTerminatingNullBranchProvesReturnedValueNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates a normalized value.</summary>
                    public void M(Item? item)
                    {
                        Item? normalized = Normalize(item);
                        Validate(normalized);
                    }

                    private static Item? Normalize(Item? item)
                    {
                        if (item == null)
                        {
                            return new Item();
                        }

                        return item;
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }

                    public sealed class Item
                    {
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that a dereference performed only on one branch does not
        /// prove the returned parameter non-null.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertConditionallyDereferencedParameterRemainsPotentiallyNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates a conditionally observed value.</summary>
                    public void M(Item? item)
                    {
                        Item? normalized = Preserve(item);
                        Validate(normalized);
                    }

                    private static Item? Preserve(Item? item)
                    {
                        if (item != null)
                        {
                            _ = item.Name;
                        }

                        return item;
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }

                    public sealed class Item
                    {
                        public string Name => string.Empty;
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            AssertArgumentNullFinding(findings);
        }

        /// <summary>
        /// Asserts one missing transitive
        /// <see cref="ArgumentNullException"/> documentation finding.
        /// </summary>
        /// <param name="findings">
        /// The findings to inspect.
        /// </param>
        private static void AssertArgumentNullFinding(
            IReadOnlyList<Finding> findings)
        {
            Finding finding = Assert.Single(findings);

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
