using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests propagation of stable parameter-member facts across callable
    /// boundaries.
    /// </summary>
    public sealed class DOC611_ParameterMemberCallContextTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that a stable property fact is
        /// propagated to a directly called helper.
        /// </summary>
        [Fact]
        public void StablePropertyFactAcrossCall_ProjectTransitive_DoesNotProduceFinding()
        {
            AssertStablePropertyFactAcrossCallDoesNotProduceFinding(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a stable property fact is
        /// propagated to a directly called helper.
        /// </summary>
        [Fact]
        public void StablePropertyFactAcrossCall_SolutionTransitive_DoesNotProduceFinding()
        {
            AssertStablePropertyFactAcrossCallDoesNotProduceFinding(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures that a stable property fact can cross a foreach boundary
        /// before being transferred to a helper parameter.
        /// </summary>
        [Fact]
        public void StablePropertyFactAcrossForEachAndCall_DoesNotProduceFinding()
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
                    /// Passes a previously dereferenced receiver from a loop.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        foreach (int item in new[] { 1 })
                        {
                            _ = item;
                            ValidateHolder(holder);
                        }
                    }

                    private static void ValidateHolder(Holder holder)
                    {
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
        /// Ensures that replacing the receiver before a helper call invalidates
        /// an earlier stable property fact.
        /// </summary>
        [Fact]
        public void ReceiverReassignedBeforeCall_StillProducesFinding()
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
                    /// Replaces a receiver before passing it to a helper.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        holder = new Holder(null);

                        ValidateHolder(holder);
                    }

                    private static void ValidateHolder(Holder holder)
                    {
                        Validate(holder.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that replacing a parameter inside the callee invalidates a
        /// stable member fact received at method entry.
        /// </summary>
        [Fact]
        public void CalleeParameterReassignedBeforePropertyUse_StillProducesFinding()
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
                    /// Passes a proven receiver to a helper that replaces it.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        ValidateHolder(holder);
                    }

                    private static void ValidateHolder(Holder holder)
                    {
                        holder = new Holder(null);

                        Validate(holder.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that a calculated get-only property is not propagated as a
        /// stable parameter-member fact.
        /// </summary>
        [Fact]
        public void CalculatedPropertyAcrossCall_StillProducesFinding()
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

                            return reads == 1
                                ? new object()
                                : null;
                        }
                    }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Passes a receiver whose property value can change.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        ValidateHolder(holder);
                    }

                    private static void ValidateHolder(Holder holder)
                    {
                        Validate(holder.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Verifies stable property fact propagation across one direct method
        /// boundary.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertStablePropertyFactAcrossCallDoesNotProduceFinding(
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
                    /// Passes a receiver after successfully dereferencing a
                    /// stable property.
                    /// </summary>
                    public static void M(Holder holder)
                    {
                        _ = holder.Value.ToString();

                        ValidateHolder(holder);
                    }

                    private static void ValidateHolder(Holder holder)
                    {
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
                mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that an ArgumentNullException finding remains present.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        private static void AssertArgumentNullFinding(string source)
        {
            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
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
