using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests non-null facts established by earlier successful runtime
    /// dereferences.
    /// </summary>
    public sealed class DOC611_PrecedingSuccessfulDereferenceTests
    {
        /// <summary>
        /// Ensures that execution after an unconditional instance-property
        /// access proves the receiver non-null.
        /// </summary>
        [Fact]
        public void PropertyDereferenceBeforeGuard_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object Value { get; } = new object();
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a previously dereferenced value.
                    /// </summary>
                    public void M(Holder? holder)
                    {
                        object value =
                            holder.Value;

                        Validate(holder);
                    }

                    private static void Validate(Holder? holder)
                    {
                        ArgumentNullException.ThrowIfNull(holder);
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
        /// Ensures that a value dereferenced before it is forwarded to a
        /// constructor carries a non-null call-site fact.
        /// </summary>
        [Fact]
        public void DereferencedValuePassedToConstructor_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public Holder OriginalDefinition => this;
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Creates a target from a previously dereferenced value.
                    /// </summary>
                    public void M(Holder? holder)
                    {
                        Holder normalized =
                            holder.OriginalDefinition;

                        _ = new Target(holder);
                    }

                    private sealed class Target
                    {
                        public Target(Holder? holder)
                        {
                            ArgumentNullException.ThrowIfNull(holder);
                        }
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
        /// Ensures that a conditional access does not establish a non-null
        /// receiver fact.
        /// </summary>
        [Fact]
        public void ConditionalAccessBeforeGuard_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object Value { get; } = new object();
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a conditionally accessed value.
                    /// </summary>
                    public void M(Holder? holder)
                    {
                        object? value =
                            holder?.Value;

                        Validate(holder);
                    }

                    private static void Validate(Holder? holder)
                    {
                        ArgumentNullException.ThrowIfNull(holder);
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
        }

        /// <summary>
        /// Ensures that a dereference in the right operand of a short-circuit
        /// operation does not establish an unconditional non-null fact.
        /// </summary>
        [Fact]
        public void ShortCircuitedDereference_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object Value { get; } = new object();
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a conditionally dereferenced value.
                    /// </summary>
                    public void M(
                        bool inspect,
                        Holder? holder)
                    {
                        bool hasValue =
                            inspect &&
                            holder.Value != null;

                        Validate(holder);
                    }

                    private static void Validate(Holder? holder)
                    {
                        ArgumentNullException.ThrowIfNull(holder);
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
        }

        /// <summary>
        /// Ensures that a later write invalidates a fact established by an
        /// earlier successful dereference.
        /// </summary>
        [Fact]
        public void WriteAfterDereference_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object Value { get; } = new object();
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a value changed after it was dereferenced.
                    /// </summary>
                    public void M(Holder? holder)
                    {
                        object value =
                            holder.Value;

                        holder = null;

                        Validate(holder);
                    }

                    private static void Validate(Holder? holder)
                    {
                        ArgumentNullException.ThrowIfNull(holder);
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
        }

        /// <summary>
        /// Ensures that a dereference in the right operand of a
        /// null-coalescing assignment does not establish an unconditional
        /// non-null fact.
        /// </summary>
        [Fact]
        public void CoalesceAssignmentDereference_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object Value { get; } = new object();
                }

                public sealed class TestClass
                {
                    /// <summary>
                    /// Validates a conditionally dereferenced value.
                    /// </summary>
                    public void M(Holder? holder)
                    {
                        object? value =
                            new object();

                        value ??=
                            holder.Value;

                        Validate(holder);
                    }

                    private static void Validate(Holder? holder)
                    {
                        ArgumentNullException.ThrowIfNull(holder);
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
        }

        /// <summary>
        /// Ensures in project-transitive mode that a successful dereference before a
        /// foreach statement proves the receiver non-null inside the loop when the
        /// loop does not modify it.
        /// </summary>
        [Fact]
        public void DereferenceBeforeForEach_ProjectTransitive_ProvesReceiverNonNullInsideBody()
        {
            AssertDereferenceBeforeForEachProvesReceiverNonNullInsideBody(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a successful dereference before a
        /// foreach statement proves the receiver non-null inside the loop when the
        /// loop does not modify it.
        /// </summary>
        [Fact]
        public void DereferenceBeforeForEach_SolutionTransitive_ProvesReceiverNonNullInsideBody()
        {
            AssertDereferenceBeforeForEachProvesReceiverNonNullInsideBody(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a write later in a foreach body
        /// prevents a pre-loop dereference fact from being reused on a later
        /// iteration.
        /// </summary>
        [Fact]
        public void DereferenceBeforeForEachWithLaterBodyWrite_ProjectTransitive_RemainsPotentiallyNull()
        {
            AssertDereferenceBeforeForEachWithLaterBodyWriteRemainsPotentiallyNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a write later in a foreach body
        /// prevents a pre-loop dereference fact from being reused on a later
        /// iteration.
        /// </summary>
        [Fact]
        public void DereferenceBeforeForEachWithLaterBodyWrite_SolutionTransitive_RemainsPotentiallyNull()
        {
            AssertDereferenceBeforeForEachWithLaterBodyWriteRemainsPotentiallyNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies that a successful dereference before a foreach statement proves
        /// the receiver non-null inside the loop when the loop does not modify it.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertDereferenceBeforeForEachProvesReceiverNonNullInsideBody(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
        #nullable enable
        using System;

        public sealed class Holder
        {
            public object Value { get; } = new object();
        }

        public sealed class TestClass
        {
            /// <summary>
            /// Validates a previously dereferenced value inside a foreach body.
            /// </summary>
            public void M(Holder? holder)
            {
                object value = holder.Value;

                foreach (int item in new[] { 1 })
                {
                    _ = item;
                    _ = value;
                    Validate(holder);
                }
            }

            private static void Validate(Holder? holder)
            {
                ArgumentNullException.ThrowIfNull(holder);
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
        /// Verifies that a write later in a foreach body prevents a pre-loop
        /// dereference fact from being reused on a later iteration.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertDereferenceBeforeForEachWithLaterBodyWriteRemainsPotentiallyNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
        #nullable enable
        using System;

        public sealed class Holder
        {
            public object Value { get; } = new object();
        }

        public sealed class TestClass
        {
            /// <summary>
            /// Validates a value that can become null between foreach iterations.
            /// </summary>
            public void M(Holder? holder)
            {
                object value = holder.Value;

                foreach (int item in new[] { 1, 2 })
                {
                    _ = item;
                    _ = value;
                    Validate(holder);
                    holder = null;
                }
            }

            private static void Validate(Holder? holder)
            {
                ArgumentNullException.ThrowIfNull(holder);
            }
        }
        """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Contains(
                findings,
                finding =>
                    finding.Message.Contains(
                        "System.ArgumentNullException",
                        StringComparison.Ordinal));
        }
    }
}
