using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies matching value-fact and root semantics for documented private
    /// helpers in project- and solution-transitive exception analysis.
    /// </summary>
    public sealed class DOC611_PrivateHelperCallContextTests
    {
        /// <summary>
        /// Ensures that both transitive modes report the same reachable
        /// <see cref="ArgumentNullException"/> when a documented private
        /// helper forwards an independently unknown parameter to a guarded
        /// constructor without first proving the parameter to be non-null.
        /// </summary>
        [Fact]
        public void DocumentedPrivateHelperForwardingParameter_IsReportedByBothModes()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Processes a matching value.</summary>\n" +
                "    public void M(object? candidate)\n" +
                "    {\n" +
                "        if (candidate is not string value)\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        Add(value);\n" +
                "    }\n" +
                "\n" +
                "    /// <summary>Adds a proven value.</summary>\n" +
                "    private static void Add(string value)\n" +
                "    {\n" +
                "        _ = new Key(value);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(string? value)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(value);\n" +
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

            Finding projectFinding =
                Assert.Single(projectFindings);

            Finding solutionFinding =
                Assert.Single(solutionFindings);

            AssertArgumentNullFinding(
                projectFinding);

            AssertArgumentNullFinding(
                solutionFinding);
        }

        /// <summary>
        /// Ensures that a successful dereference proves a parameter to be
        /// non-null on the continuing execution path before the same value is
        /// passed to a guarded constructor.
        /// </summary>
        [Fact]
        public void DocumentedPrivateHelperAfterSuccessfulDereference_DoesNotReportLaterArgumentNullGuard()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Processes the supplied item.</summary>\n" +
                "    private static void Add(Item? item)\n" +
                "    {\n" +
                "        _ = item.Name;\n" +
                "        _ = new Key(item);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Item\n" +
                "    {\n" +
                "        public string Name => string.Empty;\n" +
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

            Assert.True(
                projectFindings.Count == 0,
                "ProjectTransitive unexpectedly reported: " +
                FormatFindings(projectFindings));

            Assert.True(
                solutionFindings.Count == 0,
                "SolutionTransitive unexpectedly reported: " +
                FormatFindings(solutionFindings));
        }

        /// <summary>
        /// Asserts that a finding reports DOC611 for
        /// <see cref="ArgumentNullException"/> on the private helper.
        /// </summary>
        /// <param name="finding">
        /// The finding to inspect.
        /// </param>
        private static void AssertArgumentNullFinding(
            Finding finding)
        {
            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID);

            Assert.Equal(
                "Add",
                finding.Context.SymbolName);

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
        /// A compact textual representation of the supplied findings.
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
