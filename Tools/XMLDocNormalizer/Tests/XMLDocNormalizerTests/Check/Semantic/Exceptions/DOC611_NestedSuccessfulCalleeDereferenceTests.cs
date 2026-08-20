using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies non-null facts established by source-helper calls that are
    /// nested inside expressions which must themselves complete
    /// successfully.
    /// </summary>
    public sealed class DOC611_NestedSuccessfulCalleeDereferenceTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that successful evaluation of
        /// an outer invocation preserves a non-null fact established by a
        /// source helper in its required receiver expression.
        /// </summary>
        [Fact]
        public void SourceHelperInRequiredInvocationReceiver_ProjectTransitive_ProvesArgumentNonNull()
        {
            AssertSourceHelperInRequiredInvocationReceiverProvesArgumentNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that successful evaluation of
        /// an outer invocation preserves a non-null fact established by a
        /// source helper in its required receiver expression.
        /// </summary>
        [Fact]
        public void SourceHelperInRequiredInvocationReceiver_SolutionTransitive_ProvesArgumentNonNull()
        {
            AssertSourceHelperInRequiredInvocationReceiverProvesArgumentNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a nested source helper
        /// whose dereference is conditional does not establish a non-null
        /// fact.
        /// </summary>
        [Fact]
        public void ConditionalSourceHelperInInvocationReceiver_ProjectTransitive_DoesNotProveArgumentNonNull()
        {
            AssertConditionalSourceHelperInInvocationReceiverDoesNotProveArgumentNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a nested source helper
        /// whose dereference is conditional does not establish a non-null
        /// fact.
        /// </summary>
        [Fact]
        public void ConditionalSourceHelperInInvocationReceiver_SolutionTransitive_DoesNotProveArgumentNonNull()
        {
            AssertConditionalSourceHelperInInvocationReceiverDoesNotProveArgumentNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a write performed by the
        /// outer invocation invalidates a non-null fact established by its
        /// receiver expression.
        /// </summary>
        [Fact]
        public void OuterInvocationWritesArgument_ProjectTransitive_DoesNotPreserveNestedNonNullFact()
        {
            AssertOuterInvocationWritesArgumentDoesNotPreserveNestedNonNullFact(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a write performed by the
        /// outer invocation invalidates a non-null fact established by its
        /// receiver expression.
        /// </summary>
        [Fact]
        public void OuterInvocationWritesArgument_SolutionTransitive_DoesNotPreserveNestedNonNullFact()
        {
            AssertOuterInvocationWritesArgumentDoesNotPreserveNestedNonNullFact(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies that a mandatory source-helper call nested in the receiver
        /// of another invocation proves its argument non-null after the
        /// complete statement succeeds.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertSourceHelperInRequiredInvocationReceiverProvesArgumentNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates an item after preparing it.</summary>
                    public void M(Item? item)
                    {
                        int hash = Prepare(item).GetHashCode();
                        _ = hash;
                        Validate(item);
                    }

                    private static Marker Prepare(Item? item)
                    {
                        _ = item.Name;
                        return new Marker();
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }

                    private sealed class Marker
                    {
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

            Assert.Empty(
                findings);
        }

        /// <summary>
        /// Verifies that successful evaluation of an outer invocation does
        /// not strengthen a nested helper whose argument dereference is not
        /// required on every normal completion path.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertConditionalSourceHelperInInvocationReceiverDoesNotProveArgumentNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates an item after conditionally observing it.</summary>
                    public void M(Item? item)
                    {
                        int hash = Prepare(item).GetHashCode();
                        _ = hash;
                        Validate(item);
                    }

                    private static Marker Prepare(Item? item)
                    {
                        if (item != null)
                        {
                            _ = item.Name;
                        }

                        return new Marker();
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }

                    private sealed class Marker
                    {
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

            AssertArgumentNullFinding(
                findings);
        }

        /// <summary>
        /// Verifies that a write performed later in the same invocation
        /// invalidates a non-null fact established while evaluating the
        /// invocation receiver.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertOuterInvocationWritesArgumentDoesNotPreserveNestedNonNullFact(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    /// <summary>Validates an item after resetting it.</summary>
                    public void M(Item? item)
                    {
                        int result = Prepare(item).Reset(ref item);
                        _ = result;
                        Validate(item);
                    }

                    private static Marker Prepare(Item? item)
                    {
                        _ = item.Name;
                        return new Marker();
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }

                    private sealed class Marker
                    {
                        public int Reset(ref Item? item)
                        {
                            item = null;
                            return 0;
                        }
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

            AssertArgumentNullFinding(
                findings);
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
