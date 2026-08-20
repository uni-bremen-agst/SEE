using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies non-null value facts established by successful evaluation of
    /// enclosing foreach source expressions.
    /// </summary>
    public sealed class DOC611_EnclosingForEachDereferenceTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that entering a foreach body
        /// after directly dereferencing a receiver in the source expression
        /// proves that receiver non-null.
        /// </summary>
        [Fact]
        public void DirectDereferenceInForEachSource_ProjectTransitive_ProvesReceiverNonNullInsideBody()
        {
            AssertDirectDereferenceInForEachSourceProvesReceiverNonNullInsideBody(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that entering a foreach body
        /// after directly dereferencing a receiver in the source expression
        /// proves that receiver non-null.
        /// </summary>
        [Fact]
        public void DirectDereferenceInForEachSource_SolutionTransitive_ProvesReceiverNonNullInsideBody()
        {
            AssertDirectDereferenceInForEachSourceProvesReceiverNonNullInsideBody(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that conditional access in a
        /// foreach source does not establish a non-null receiver fact when a
        /// fallback sequence can still enter the loop body.
        /// </summary>
        [Fact]
        public void ConditionalAccessInForEachSource_ProjectTransitive_DoesNotProveReceiverNonNull()
        {
            AssertConditionalAccessInForEachSourceDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that conditional access in a
        /// foreach source does not establish a non-null receiver fact when a
        /// fallback sequence can still enter the loop body.
        /// </summary>
        [Fact]
        public void ConditionalAccessInForEachSource_SolutionTransitive_DoesNotProveReceiverNonNull()
        {
            AssertConditionalAccessInForEachSourceDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a write inside the foreach
        /// body invalidates the fact established by the source expression.
        /// </summary>
        [Fact]
        public void ReceiverReassignedInsideForEachBody_ProjectTransitive_DoesNotPreserveNonNullFact()
        {
            AssertReceiverReassignedInsideForEachBodyDoesNotPreserveNonNullFact(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a write inside the foreach
        /// body invalidates the fact established by the source expression.
        /// </summary>
        [Fact]
        public void ReceiverReassignedInsideForEachBody_SolutionTransitive_DoesNotPreserveNonNullFact()
        {
            AssertReceiverReassignedInsideForEachBodyDoesNotPreserveNonNullFact(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a write after the guarded
        /// use still invalidates a foreach-source fact for a later iteration.
        /// </summary>
        [Fact]
        public void ReceiverReassignedAfterUseInsideForEachBody_ProjectTransitive_DoesNotPreserveNonNullFactAcrossIterations()
        {
            AssertReceiverReassignedAfterUseInsideForEachBodyDoesNotPreserveNonNullFactAcrossIterations(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a write after the guarded
        /// use still invalidates a foreach-source fact for a later iteration.
        /// </summary>
        [Fact]
        public void ReceiverReassignedAfterUseInsideForEachBody_SolutionTransitive_DoesNotPreserveNonNullFactAcrossIterations()
        {
            AssertReceiverReassignedAfterUseInsideForEachBodyDoesNotPreserveNonNullFactAcrossIterations(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a foreach source which writes
        /// the receiver after dereferencing it does not establish a non-null fact.
        /// </summary>
        [Fact]
        public void ReceiverReassignedInsideForEachSource_ProjectTransitive_DoesNotProveReceiverNonNull()
        {
            AssertReceiverReassignedInsideForEachSourceDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a foreach source which writes
        /// the receiver after dereferencing it does not establish a non-null fact.
        /// </summary>
        [Fact]
        public void ReceiverReassignedInsideForEachSource_SolutionTransitive_DoesNotProveReceiverNonNull()
        {
            AssertReceiverReassignedInsideForEachSourceDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies the mandatory foreach-source dereference scenario for one
        /// transitive exception-analysis mode.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertDirectDereferenceInForEachSourceProvesReceiverNonNullInsideBody(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class TestClass
                {
                    /// <summary>Validates a container inside a foreach body.</summary>
                    public void M(Container? container)
                    {
                        foreach (int value in container.Values)
                        {
                            _ = value;
                            Validate(container);
                        }
                    }

                    private static void Validate(Container? container)
                    {
                        ArgumentNullException.ThrowIfNull(container);
                    }

                    public sealed class Container
                    {
                        public IReadOnlyList<int> Values { get; } =
                            Array.Empty<int>();
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
        /// Verifies that conditional access with a non-empty fallback sequence
        /// does not prove the receiver non-null on loop-body entry.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertConditionalAccessInForEachSourceDoesNotProveReceiverNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class TestClass
                {
                    /// <summary>Validates an optionally accessed container.</summary>
                    public void M(Container? container)
                    {
                        foreach (int value in
                                 container?.Values ?? new[] { 1 })
                        {
                            _ = value;
                            Validate(container);
                        }
                    }

                    private static void Validate(Container? container)
                    {
                        ArgumentNullException.ThrowIfNull(container);
                    }

                    public sealed class Container
                    {
                        public IReadOnlyList<int> Values { get; } =
                            Array.Empty<int>();
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
        /// Verifies that a write inside the foreach body invalidates the
        /// non-null fact established while evaluating the source expression.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertReceiverReassignedInsideForEachBodyDoesNotPreserveNonNullFact(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class TestClass
                {
                    /// <summary>Validates a reassigned container.</summary>
                    public void M(Container? container)
                    {
                        foreach (int value in container.Values)
                        {
                            _ = value;
                            container = null;
                            Validate(container);
                        }
                    }

                    private static void Validate(Container? container)
                    {
                        ArgumentNullException.ThrowIfNull(container);
                    }

                    public sealed class Container
                    {
                        public IReadOnlyList<int> Values { get; } =
                            Array.Empty<int>();
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
        /// Verifies that a write after the guarded use can affect a later
        /// foreach iteration and therefore invalidates the source-derived fact.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertReceiverReassignedAfterUseInsideForEachBodyDoesNotPreserveNonNullFactAcrossIterations(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class TestClass
                {
                    /// <summary>Validates a container across foreach iterations.</summary>
                    public void M(Container? container)
                    {
                        foreach (int value in container.Values)
                        {
                            _ = value;
                            Validate(container);
                            container = null;
                        }
                    }

                    private static void Validate(Container? container)
                    {
                        ArgumentNullException.ThrowIfNull(container);
                    }

                    public sealed class Container
                    {
                        public IReadOnlyList<int> Values { get; } =
                            new[] { 1, 2 };
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
        /// Verifies that a write occurring later in the foreach source
        /// invalidates an earlier receiver dereference from that same source.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertReceiverReassignedInsideForEachSourceDoesNotProveReceiverNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class TestClass
                {
                    /// <summary>Validates a receiver reassigned in the foreach source.</summary>
                    public void M(Container? container)
                    {
                        foreach (int value in KeepValues(
                                     container.Values,
                                     container = null))
                        {
                            _ = value;
                            Validate(container);
                        }
                    }

                    private static IReadOnlyList<int> KeepValues(
                        IReadOnlyList<int> values,
                        Container? ignored)
                    {
                        _ = ignored;
                        return values;
                    }

                    private static void Validate(Container? container)
                    {
                        ArgumentNullException.ThrowIfNull(container);
                    }

                    public sealed class Container
                    {
                        public IReadOnlyList<int> Values { get; } =
                            new[] { 1 };
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
