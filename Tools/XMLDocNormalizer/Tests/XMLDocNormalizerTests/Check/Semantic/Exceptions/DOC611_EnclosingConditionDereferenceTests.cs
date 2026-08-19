using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies non-null value facts established by successful evaluation of
    /// enclosing branch conditions.
    /// </summary>
    public sealed class DOC611_EnclosingConditionDereferenceTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that entering an <c>if</c>
        /// branch after a condition that necessarily dereferences a parameter
        /// proves the parameter to be non-null inside that branch.
        /// </summary>
        [Fact]
        public void DirectDereferenceInIfCondition_ProjectTransitive_ProvesReceiverNonNullInsideThenBranch()
        {
            AssertDirectDereferenceInIfConditionProvesReceiverNonNullInsideThenBranch(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that entering an <c>if</c>
        /// branch after a condition that necessarily dereferences a parameter
        /// proves the parameter to be non-null inside that branch.
        /// </summary>
        [Fact]
        public void DirectDereferenceInIfCondition_SolutionTransitive_ProvesReceiverNonNullInsideThenBranch()
        {
            AssertDirectDereferenceInIfConditionProvesReceiverNonNullInsideThenBranch(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that entering an <c>else</c>
        /// branch retains the non-null fact established by successful
        /// evaluation of the shared condition.
        /// </summary>
        [Fact]
        public void DirectDereferenceInIfCondition_ProjectTransitive_ProvesReceiverNonNullInsideElseBranch()
        {
            AssertDirectDereferenceInIfConditionProvesReceiverNonNullInsideElseBranch(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that entering an <c>else</c>
        /// branch retains the non-null fact established by successful
        /// evaluation of the shared condition.
        /// </summary>
        [Fact]
        public void DirectDereferenceInIfCondition_SolutionTransitive_ProvesReceiverNonNullInsideElseBranch()
        {
            AssertDirectDereferenceInIfConditionProvesReceiverNonNullInsideElseBranch(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that conditional access does not
        /// establish a non-null receiver fact because it can complete normally
        /// for a null receiver.
        /// </summary>
        [Fact]
        public void ConditionalAccessInIfCondition_ProjectTransitive_DoesNotProveReceiverNonNull()
        {
            AssertConditionalAccessInIfConditionDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that conditional access does
        /// not establish a non-null receiver fact because it can complete
        /// normally for a null receiver.
        /// </summary>
        [Fact]
        public void ConditionalAccessInIfCondition_SolutionTransitive_DoesNotProveReceiverNonNull()
        {
            AssertConditionalAccessInIfConditionDoesNotProveReceiverNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures in project-transitive mode that a later write in the
        /// condition invalidates the earlier successful-dereference fact.
        /// </summary>
        [Fact]
        public void ConditionWritingReceiverAfterDereference_ProjectTransitive_DoesNotProveFinalValueNonNull()
        {
            AssertConditionWritingReceiverAfterDereferenceDoesNotProveFinalValueNonNull(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a later write in the
        /// condition invalidates the earlier successful-dereference fact.
        /// </summary>
        [Fact]
        public void ConditionWritingReceiverAfterDereference_SolutionTransitive_DoesNotProveFinalValueNonNull()
        {
            AssertConditionWritingReceiverAfterDereferenceDoesNotProveFinalValueNonNull(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies the direct-dereference condition scenario for one
        /// transitive exception-analysis mode.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertDirectDereferenceInIfConditionProvesReceiverNonNullInsideThenBranch(
            ExceptionAnalysisMode mode)
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an item after evaluating its member.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        if (item.Value == null)\n" +
                "        {\n" +
                "            Validate(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(Item? item)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(item);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public object? Value { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies the else-branch dereference scenario for one transitive
        /// exception-analysis mode.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertDirectDereferenceInIfConditionProvesReceiverNonNullInsideElseBranch(
            ExceptionAnalysisMode mode)
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an item in the alternative branch.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        if (item.Value != null)\n" +
                "        {\n" +
                "            _ = item.Value;\n" +
                "        }\n" +
                "        else\n" +
                "        {\n" +
                "            Validate(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(Item? item)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(item);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public object? Value { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies the conditional-access scenario for one transitive
        /// exception-analysis mode.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertConditionalAccessInIfConditionDoesNotProveReceiverNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates an optionally accessed item.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        if (item?.Value == null)\n" +
                "        {\n" +
                "            Validate(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(Item? item)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(item);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public object? Value { get; set; }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode);

            AssertArgumentNullFinding(
                findings);
        }

        /// <summary>
        /// Verifies the condition-write scenario for one transitive
        /// exception-analysis mode.
        /// </summary>
        /// <param name="mode">
        /// The transitive exception-analysis mode to verify.
        /// </param>
        private static void AssertConditionWritingReceiverAfterDereferenceDoesNotProveFinalValueNonNull(
            ExceptionAnalysisMode mode)
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates the value left by the condition.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        if (item.Value == null &&\n" +
                "            (item = null) == null)\n" +
                "        {\n" +
                "            Validate(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(Item? item)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(item);\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public object? Value { get; set; }\n" +
                "    }\n" +
                "}\n";

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
                "M",
                finding.Context.SymbolName);

            Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName);
        }
    }
}
