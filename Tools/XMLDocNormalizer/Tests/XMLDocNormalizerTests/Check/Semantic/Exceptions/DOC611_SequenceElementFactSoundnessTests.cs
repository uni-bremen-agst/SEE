using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests sound preservation of non-null sequence-element facts.
    /// </summary>
    public sealed class DOC611_SequenceElementFactSoundnessTests
    {
        /// <summary>
        /// Ensures that a local variable preserves the non-null element
        /// guarantee established by <c>OfType&lt;T&gt;</c>.
        /// </summary>
        [Fact]
        public void OfTypeSequenceStoredInLocal_RemainsNonNull()
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
                "        IEnumerable<string> values =\n" +
                "            candidates.OfType<string>();\n" +
                "\n" +
                "        foreach (string value in values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
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

            AssertNoFindingsInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that potentially null elements produced before
        /// <c>OfType&lt;T&gt;</c> are filtered before a sequence is stored in
        /// a local variable.
        /// </summary>
        [Fact]
        public void SelectThenOfTypeSequenceStoredInLocal_RemainsNonNull()
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
                "        IEnumerable<string> values = candidates\n" +
                "            .Select(static candidate => candidate)\n" +
                "            .OfType<string>();\n" +
                "\n" +
                "        foreach (string value in values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
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

            AssertNoFindingsInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that a transformation capable of introducing null values
        /// does not inherit the non-null fact from an earlier
        /// <c>OfType&lt;T&gt;</c> operation.
        /// </summary>
        [Fact]
        public void SelectAfterOfTypeCanIntroduceNull_StillProducesFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates transformed values.</summary>\n" +
                "    public void M(IEnumerable<object?> candidates)\n" +
                "    {\n" +
                "        IEnumerable<string?> values = candidates\n" +
                "            .OfType<string>()\n" +
                "            .Select(static _ => (string?)null);\n" +
                "\n" +
                "        foreach (string? value in values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
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

            AssertSingleArgumentNullFindingInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that dictionary values are not considered non-null when an
        /// insertion can supply a null value.
        /// </summary>
        [Fact]
        public void PossiblyNullDictionaryInsertion_StillProducesFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates dictionary values.</summary>\n" +
                "    public void M(string? candidate)\n" +
                "    {\n" +
                "        Dictionary<int, string?> values = new();\n" +
                "        values.TryAdd(0, candidate);\n" +
                "\n" +
                "        foreach (string? value in values.Values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
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

            AssertSingleArgumentNullFindingInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that an initializer-based sequence fact is discarded after
        /// the local variable is reassigned.
        /// </summary>
        [Fact]
        public void ReassignedSequenceLocal_StillProducesFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates reassigned values.</summary>\n" +
                "    public void M(IEnumerable<object?> candidates)\n" +
                "    {\n" +
                "        IEnumerable<string?> values =\n" +
                "            candidates.OfType<string>();\n" +
                "\n" +
                "        values = new string?[] { null };\n" +
                "\n" +
                "        foreach (string? value in values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
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

            AssertSingleArgumentNullFindingInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that mutating a materialized sequence element to null
        /// invalidates an earlier non-null element guarantee.
        /// </summary>
        [Fact]
        public void MaterializedArrayElementMutation_StillProducesFinding()
        {
            string source =
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates mutated values.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        string?[] values = new object?[] { \"value\" }\n" +
                "            .OfType<string>()\n" +
                "            .ToArray();\n" +
                "\n" +
                "        values[0] = null;\n" +
                "\n" +
                "        foreach (string? value in values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
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

            AssertSingleArgumentNullFindingInBothTransitiveModes(source);
        }

        /// <summary>
        /// Asserts that both transitive modes produce no exception
        /// documentation findings.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        private static void AssertNoFindingsInBothTransitiveModes(
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }

        /// <summary>
        /// Asserts that both transitive modes produce one DOC611 finding for
        /// <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        private static void AssertSingleArgumentNullFindingInBothTransitiveModes(
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

            Finding projectFinding =
                Assert.Single(projectFindings);

            Finding solutionFinding =
                Assert.Single(solutionFindings);

            AssertArgumentNullFinding(projectFinding);
            AssertArgumentNullFinding(solutionFinding);
        }

        /// <summary>
        /// Asserts that one finding is DOC611 for an
        /// <see cref="ArgumentNullException"/>.
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

            Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }
    }
}
