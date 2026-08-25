using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests non-null facts established by earlier successful dereferences of
    /// stable get-only auto-properties.
    /// </summary>
    public sealed class DOC611_PrecedingSuccessfulPropertyDereferenceTests
    {
        /// <summary>
        /// Ensures that a stable get-only property remains proven non-null
        /// after an earlier successful dereference.
        /// </summary>
        [Fact]
        public void StableGetOnlyPropertyAfterSuccessfulDereference_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public Holder(object? value)
                    {
                        Value = value;
                    }

                    public object? Value { get; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a previously dereferenced stable property.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        Validate(holder.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a stable property fact
        /// established before a foreach remains valid inside the loop.
        /// </summary>
        [Fact]
        public void StableGetOnlyPropertyBeforeForEach_ProjectTransitive_DoesNotProduceFinding()
        {
            AssertStableGetOnlyPropertyBeforeForEachDoesNotProduceFinding(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a stable property fact
        /// established before a foreach remains valid inside the loop.
        /// </summary>
        [Fact]
        public void StableGetOnlyPropertyBeforeForEach_SolutionTransitive_DoesNotProduceFinding()
        {
            AssertStableGetOnlyPropertyBeforeForEachDoesNotProduceFinding(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures that replacing the receiver invalidates a property fact
        /// established by an earlier successful dereference.
        /// </summary>
        [Fact]
        public void ReceiverReassignedAfterPropertyDereference_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public Holder(object? value)
                    {
                        Value = value;
                    }

                    public object? Value { get; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a property after replacing its receiver.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        holder = new Holder(null);

                        Validate(holder.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            AssertArgumentNullFinding(findings);
        }

        /// <summary>
        /// Ensures that replacing the receiver at the end of a foreach
        /// iteration prevents reuse of a pre-loop property fact on a later
        /// iteration.
        /// </summary>
        [Fact]
        public void ReceiverReassignedInsideForEach_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public Holder(object? value)
                    {
                        Value = value;
                    }

                    public object? Value { get; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a stable property across foreach iterations.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        foreach (int item in new[] { 1, 2 })
                        {
                            _ = item;
                            Validate(holder.Value);
                            holder = new Holder(null);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            AssertArgumentNullFinding(findings);
        }

        /// <summary>
        /// Ensures that a calculated get-only property is not treated as
        /// stable merely because it has no setter.
        /// </summary>
        [Fact]
        public void CalculatedGetOnlyProperty_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    private int reads;

                    public object? Value
                    {
                        get
                        {
                            reads++;

                            if (reads == 1)
                            {
                                return new object();
                            }

                            return null;
                        }
                    }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a calculated property after an earlier read.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        Validate(holder.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            AssertArgumentNullFinding(findings);
        }

        /// <summary>
        /// Verifies that a stable property fact established before a foreach
        /// remains valid inside the loop for one transitive analysis mode.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertStableGetOnlyPropertyBeforeForEachDoesNotProduceFinding(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public Holder(object? value)
                    {
                        Value = value;
                    }

                    public object? Value { get; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a stable property inside a foreach body.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        foreach (int item in new[] { 1 })
                        {
                            _ = item;
                            Validate(holder.Value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that an ArgumentNullException finding remains present.
        /// </summary>
        /// <param name="findings">
        /// The findings to inspect.
        /// </param>
        private static void AssertArgumentNullFinding(List<Finding> findings)
        {
            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that dereferencing a stable property on one receiver does not
        /// prove the same property non-null on another receiver.
        /// </summary>
        [Fact]
        public void DifferentReceiverWithSameStableProperty_StillProducesFinding()
        {
            const string source =
                """
        #nullable enable
        using System;

        public sealed class Holder
        {
            public Holder(object? value)
            {
                Value = value;
            }

            public object? Value { get; }
        }

        public static class TestClass
        {
            /// <summary>
            /// Validates the same property on a different receiver.
            /// </summary>
            public static void M(Holder first, Holder second)
            {
                _ = first.Value.ToString();

                Validate(second.Value);
            }

            private static void Validate(object? value)
            {
                ArgumentNullException.ThrowIfNull(value);
            }
        }
        """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            AssertArgumentNullFinding(findings);
        }
    }
}
