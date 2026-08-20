using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies value-fact mapping for reduced extension-method calls.
    /// </summary>
    public sealed class DOC611_ReducedExtensionCallContextTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that a proven non-null extension
        /// receiver is mapped to the original extension receiver parameter.
        /// </summary>
        [Fact]
        public void KnownNonNullReceiver_ProjectTransitive_SuppressesReceiverNullGuard()
        {
            AssertKnownNonNullReceiverSuppressesReceiverNullGuard(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a proven non-null extension
        /// receiver is mapped to the original extension receiver parameter.
        /// </summary>
        [Fact]
        public void KnownNonNullReceiver_SolutionTransitive_SuppressesReceiverNullGuard()
        {
            AssertKnownNonNullReceiverSuppressesReceiverNullGuard(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that an unknown extension
        /// receiver remains potentially null.
        /// </summary>
        [Fact]
        public void UnknownReceiver_ProjectTransitive_RemainsPotentiallyNull()
        {
            AssertUnknownReceiverRemainsPotentiallyNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that an unknown extension
        /// receiver remains potentially null.
        /// </summary>
        [Fact]
        public void UnknownReceiver_SolutionTransitive_RemainsPotentiallyNull()
        {
            AssertUnknownReceiverRemainsPotentiallyNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that explicit arguments of a
        /// reduced extension call retain their original parameter offset.
        /// </summary>
        [Fact]
        public void KnownNonNullExplicitArgument_ProjectTransitive_MapsToOriginalParameter()
        {
            AssertKnownNonNullExplicitArgumentMapsToOriginalParameter(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that explicit arguments of a
        /// reduced extension call retain their original parameter offset.
        /// </summary>
        [Fact]
        public void KnownNonNullExplicitArgument_SolutionTransitive_MapsToOriginalParameter()
        {
            AssertKnownNonNullExplicitArgumentMapsToOriginalParameter(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that an unknown explicit
        /// argument of a reduced extension call remains potentially null.
        /// </summary>
        [Fact]
        public void UnknownExplicitArgument_ProjectTransitive_RemainsPotentiallyNull()
        {
            AssertUnknownExplicitArgumentRemainsPotentiallyNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that an unknown explicit
        /// argument of a reduced extension call remains potentially null.
        /// </summary>
        [Fact]
        public void UnknownExplicitArgument_SolutionTransitive_RemainsPotentiallyNull()
        {
            AssertUnknownExplicitArgumentRemainsPotentiallyNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies receiver-to-parameter mapping for a reduced extension
        /// method after an earlier helper proves the receiver non-null.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertKnownNonNullReceiverSuppressesReceiverNullGuard(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates a prepared item.</summary>
                    public void M(Item? item)
                    {
                        Prepare(item);
                        item.Validate();
                    }

                    private static void Prepare(Item? item)
                    {
                        _ = item.Name;
                    }
                }

                public static class Extensions
                {
                    public static void Validate(this Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }
                }

                public sealed class Item
                {
                    public string Name => string.Empty;
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that invoking an extension method does not itself prove
        /// its nullable receiver non-null.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertUnknownReceiverRemainsPotentiallyNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates an unknown item.</summary>
                    public void M(Item? item)
                    {
                        item.Validate();
                    }
                }

                public static class Extensions
                {
                    public static void Validate(this Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }
                }

                public sealed class Item
                {
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            AssertArgumentNullFinding(findings);
        }

        /// <summary>
        /// Verifies that the first explicit argument of a reduced extension
        /// call maps to parameter one of the original static extension method.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertKnownNonNullExplicitArgumentMapsToOriginalParameter(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates a known value.</summary>
                    public void M(Item? item)
                    {
                        item.ValidateValue(new object());
                    }
                }

                public static class Extensions
                {
                    public static void ValidateValue(
                        this Item? item,
                        object? value)
                    {
                        _ = item;
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }

                public sealed class Item
                {
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that an unknown explicit argument of a reduced extension
        /// call remains potentially null.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertUnknownExplicitArgumentRemainsPotentiallyNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates an unknown value.</summary>
                    public void M(Item? item, object? value)
                    {
                        item.ValidateValue(value);
                    }
                }

                public static class Extensions
                {
                    public static void ValidateValue(
                        this Item? item,
                        object? value)
                    {
                        _ = item;
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }

                public sealed class Item
                {
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
