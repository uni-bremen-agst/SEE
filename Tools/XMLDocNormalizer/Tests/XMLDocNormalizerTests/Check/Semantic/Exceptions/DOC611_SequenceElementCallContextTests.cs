using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests propagation of sequence-element facts across method calls.
    /// </summary>
    public sealed class DOC611_SequenceElementCallContextTests
    {
        /// <summary>
        /// Ensures that a sequence proven to contain only non-null elements
        /// retains that fact when passed to another method.
        /// </summary>
        [Fact]
        public void NonNullSequenceElementsPassedToHelper_RemainNonNull()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates filtered values.</summary>\n" +
                "    public void M(IEnumerable<object?> candidates)\n" +
                "    {\n" +
                "        IReadOnlyList<Item> items = candidates\n" +
                "            .OfType<Item>()\n" +
                "            .ToArray();\n" +
                "\n" +
                "        ValidateAll(items);\n" +
                "    }\n" +
                "\n" +
                "    private static void ValidateAll(\n" +
                "        IReadOnlyList<Item> items)\n" +
                "    {\n" +
                "        foreach (Item item in items)\n" +
                "        {\n" +
                "            _ = new Key(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Item\n" +
                "    {\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            AssertNoFindingsInBothModes(source);
        }

        /// <summary>
        /// Ensures that a sequence which may contain null elements continues
        /// to expose the corresponding exception after being passed to another
        /// method.
        /// </summary>
        [Fact]
        public void PossiblyNullSequenceElementsPassedToHelper_StillProduceFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates supplied values.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        IReadOnlyList<Item?> items =\n" +
                "            new Item?[] { null };\n" +
                "\n" +
                "        ValidateAll(items);\n" +
                "    }\n" +
                "\n" +
                "    private static void ValidateAll(\n" +
                "        IReadOnlyList<Item?> items)\n" +
                "    {\n" +
                "        foreach (Item? item in items)\n" +
                "        {\n" +
                "            _ = new Key(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Item\n" +
                "    {\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            AssertSingleArgumentNullFinding(projectFindings);
            AssertSingleArgumentNullFinding(solutionFindings);
        }

        /// <summary>
        /// Ensures that non-null sequence-element facts survive an interface
        /// call when solution-transitive analysis follows the concrete runtime
        /// target.
        /// </summary>
        [Fact]
        public void NonNullSequenceElementsPassedThroughInterfaceDispatch_RemainNonNull()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public interface IReporter\n" +
                "{\n" +
                "    void Report(IReadOnlyList<Item> items);\n" +
                "}\n" +
                "\n" +
                "public sealed class Reporter : IReporter\n" +
                "{\n" +
                "    public void Report(IReadOnlyList<Item> items)\n" +
                "    {\n" +
                "        foreach (Item item in items)\n" +
                "        {\n" +
                "            _ = new Key(item);\n" +
                "        }\n" +
                "    }\n" +
                "}\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Reports filtered items.</summary>\n" +
                "    public void M(\n" +
                "        IEnumerable<object?> candidates,\n" +
                "        IReporter reporter)\n" +
                "    {\n" +
                "        IReadOnlyList<Item> items = candidates\n" +
                "            .OfType<Item>()\n" +
                "            .ToArray();\n" +
                "\n" +
                "        reporter.Report(items);\n" +
                "    }\n" +
                "}\n" +
                "\n" +
                "public sealed class Item\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "public sealed class Key\n" +
                "{\n" +
                "    public Key(Item? item)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(item);\n" +
                "    }\n" +
                "}\n";

            AssertNoFindingsInBothModes(source);
        }

        /// <summary>
        /// Ensures that a call-site element fact is discarded after the
        /// receiving sequence parameter is mutated before enumeration.
        /// </summary>
        [Fact]
        public void SequenceParameterMutatedBeforeForeach_StillProducesFinding()
        {
            string source =
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates filtered values.</summary>\n" +
                "    public void M(object?[] candidates)\n" +
                "    {\n" +
                "        Item[] items = candidates\n" +
                "            .OfType<Item>()\n" +
                "            .ToArray();\n" +
                "\n" +
                "        ValidateAll(items);\n" +
                "    }\n" +
                "\n" +
                "    private static void ValidateAll(Item?[] items)\n" +
                "    {\n" +
                "        items[0] = null;\n" +
                "\n" +
                "        foreach (Item? item in items)\n" +
                "        {\n" +
                "            _ = new Key(item);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Item\n" +
                "    {\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(Item? item)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(item);\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            AssertSingleArgumentNullFinding(projectFindings);
            AssertSingleArgumentNullFinding(solutionFindings);
        }

        /// <summary>
        /// Asserts that neither transitive analysis mode produces a finding for
        /// the supplied source.
        /// </summary>
        /// <param name="source">
        /// The source to analyze.
        /// </param>
        private static void AssertNoFindingsInBothModes(
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

            Assert.True(
                projectFindings.Count == 0 &&
                solutionFindings.Count == 0,
                "Expected no findings. " +
                $"ProjectTransitive: {FormatFindings(projectFindings)}; " +
                $"SolutionTransitive: {FormatFindings(solutionFindings)}.");
        }

        /// <summary>
        /// Asserts that exactly one DOC611 finding for
        /// <see cref="ArgumentNullException"/> was produced.
        /// </summary>
        /// <param name="findings">
        /// The findings to inspect.
        /// </param>
        private static void AssertSingleArgumentNullFinding(
            IReadOnlyList<Finding> findings)
        {
            Finding finding =
                Assert.Single(findings);

            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID);

            Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName);
        }

        /// <summary>
        /// Formats findings for diagnostic assertion messages.
        /// </summary>
        /// <param name="findings">
        /// The findings to format.
        /// </param>
        /// <returns>
        /// A compact description of all supplied findings.
        /// </returns>
        private static string FormatFindings(
            IReadOnlyList<Finding> findings)
        {
            if (findings.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ", ",
                findings.Select(
                    static finding =>
                        finding.Smell.ID +
                        ":" +
                        finding.Context.SymbolName +
                        ":" +
                        finding.Context.TargetName));
        }
    }
}
