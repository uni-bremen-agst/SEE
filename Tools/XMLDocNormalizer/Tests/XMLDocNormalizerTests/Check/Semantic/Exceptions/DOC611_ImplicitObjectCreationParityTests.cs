using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Compares exception-flow handling of explicit and target-typed object
    /// creation expressions across transitive analysis modes.
    /// </summary>
    public sealed class DOC611_ImplicitObjectCreationParityTests
    {
        /// <summary>
        /// Ensures that an explicit object creation propagates a transitively
        /// thrown <see cref="ArgumentNullException"/> in both transitive modes.
        /// </summary>
        [Fact]
        public void ExplicitObjectCreation_IsReportedByBothModes()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key.</summary>\n" +
                "    private static void M(string? value)\n" +
                "    {\n" +
                "        Key key = new Key(value);\n" +
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

            AssertSingleArgumentNullFinding(
                projectFindings);

            AssertSingleArgumentNullFinding(
                solutionFindings);
        }

        /// <summary>
        /// Ensures that a target-typed object creation propagates the same
        /// transitively thrown <see cref="ArgumentNullException"/> as an
        /// explicit object creation.
        /// </summary>
        [Fact]
        public void TargetTypedObjectCreation_IsReportedByBothModes()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key.</summary>\n" +
                "    private static void M(string? value)\n" +
                "    {\n" +
                "        Key key = new(value);\n" +
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

            Assert.True(
                projectFindings.Count == 1 &&
                solutionFindings.Count == 1,
                "Expected one ArgumentNullException finding in both modes. " +
                $"ProjectTransitive: {FormatFindings(projectFindings)}; " +
                $"SolutionTransitive: {FormatFindings(solutionFindings)}.");

            AssertSingleArgumentNullFinding(
                projectFindings);

            AssertSingleArgumentNullFinding(
                solutionFindings);
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
        /// Formats findings for a diagnostic assertion message.
        /// </summary>
        /// <param name="findings">
        /// The findings to format.
        /// </param>
        /// <returns>
        /// A compact description of the supplied findings.
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
