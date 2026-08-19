using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies non-null facts established by successful completion of
    /// source-level helper calls whose execution necessarily dereferences
    /// an argument.
    /// </summary>
    public sealed class DOC611_SuccessfulCalleeDereferenceTests
    {
        /// <summary>
        /// Ensures that successful completion of a helper that directly
        /// dereferences an argument proves that argument to be non-null for
        /// subsequent statements in the caller.
        /// </summary>
        [Fact]
        public void SuccessfulDirectHelperDereference_ProvesArgumentNonNullAfterCall()
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key after preparing an item.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        object context = Prepare(item);\n" +
                "        _ = context;\n" +
                "        _ = new Key(item);\n" +
                "    }\n" +
                "\n" +
                "    private static object Prepare(Item? item)\n" +
                "    {\n" +
                "        _ = item.Name;\n" +
                "        return new object();\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public string Name => string.Empty;\n" +
                "    }\n" +
                "}\n";

            AssertNoArgumentNullFindingInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that successful completion of a helper also proves an
        /// argument non-null when the required dereference occurs in another
        /// source-level helper that must complete before the first helper can
        /// return.
        /// </summary>
        [Fact]
        public void SuccessfulTransitiveHelperDereference_ProvesArgumentNonNullAfterCall()
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key after preparing an item.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        object context = Prepare(item);\n" +
                "        _ = context;\n" +
                "        _ = new Key(item);\n" +
                "    }\n" +
                "\n" +
                "    private static object Prepare(Item? item)\n" +
                "    {\n" +
                "        Touch(item);\n" +
                "        return new object();\n" +
                "    }\n" +
                "\n" +
                "    private static void Touch(Item? item)\n" +
                "    {\n" +
                "        _ = item.Name;\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public string Name => string.Empty;\n" +
                "    }\n" +
                "}\n";

            AssertNoArgumentNullFindingInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that a helper does not establish a non-null fact when a
        /// null argument can return normally without reaching its
        /// dereference.
        /// </summary>
        [Fact]
        public void ConditionalHelperDereference_DoesNotProveArgumentNonNullAfterCall()
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key after observing an item.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        object context = Prepare(item);\n" +
                "        _ = context;\n" +
                "        _ = new Key(item);\n" +
                "    }\n" +
                "\n" +
                "    private static object Prepare(Item? item)\n" +
                "    {\n" +
                "        if (item != null)\n" +
                "        {\n" +
                "            _ = item.Name;\n" +
                "        }\n" +
                "\n" +
                "        return new object();\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public string Name => string.Empty;\n" +
                "    }\n" +
                "}\n";

            AssertArgumentNullFindingInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that a dereference inside a handled
        /// <see cref="NullReferenceException"/> path does not establish a
        /// non-null argument fact because the helper can still return
        /// normally for a null argument.
        /// </summary>
        [Fact]
        public void CaughtNullReferenceDereference_DoesNotProveArgumentNonNullAfterCall()
        {
            const string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key after observing an item.</summary>\n" +
                "    public void M(Item? item)\n" +
                "    {\n" +
                "        object context = Prepare(item);\n" +
                "        _ = context;\n" +
                "        _ = new Key(item);\n" +
                "    }\n" +
                "\n" +
                "    private static object Prepare(Item? item)\n" +
                "    {\n" +
                "        try\n" +
                "        {\n" +
                "            _ = item.Name;\n" +
                "        }\n" +
                "        catch (System.NullReferenceException)\n" +
                "        {\n" +
                "        }\n" +
                "\n" +
                "        return new object();\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    public sealed class Item\n" +
                "    {\n" +
                "        public string Name => string.Empty;\n" +
                "    }\n" +
                "}\n";

            AssertArgumentNullFindingInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Asserts that neither transitive mode reports an
        /// <see cref="ArgumentNullException"/> documentation finding for the
        /// supplied source.
        /// </summary>
        /// <param name="source">
        /// The complete test source.
        /// </param>
        private static void AssertNoArgumentNullFindingInBothTransitiveModes(
            string source)
        {
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
        /// Asserts that both transitive modes report exactly one missing
        /// transitive <see cref="ArgumentNullException"/> documentation
        /// finding for the documented root member.
        /// </summary>
        /// <param name="source">
        /// The complete test source.
        /// </param>
        private static void AssertArgumentNullFindingInBothTransitiveModes(
            string source)
        {
            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            AssertArgumentNullFinding(
                projectFindings);

            AssertArgumentNullFinding(
                solutionFindings);
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
