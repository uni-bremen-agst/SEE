using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Compares value-fact propagation between project-transitive analysis
    /// and the productive solution-transitive summary-graph analysis.
    /// </summary>
    public sealed class DOC611_SolutionTransitiveValueFactRegressionTests
    {
        /// <summary>
        /// Ensures that a value introduced by a successful type pattern remains
        /// non-null across a private helper boundary before reaching a guarded
        /// constructor.
        /// </summary>
        [Fact]
        public void PatternValuePassedThroughPrivateHelper_RemainsNonNullInBothModes()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key for a matching value.</summary>\n" +
                "    public void M(object? candidate)\n" +
                "    {\n" +
                "        if (candidate is not string symbol)\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        Add(symbol);\n" +
                "    }\n" +
                "\n" +
                "    private static void Add(string? symbol)\n" +
                "    {\n" +
                "        _ = new Key(symbol);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(string? symbol)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(symbol);\n" +
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }

        /// <summary>
        /// Ensures that a foreach iteration variable produced by
        /// <c>Enumerable.OfType&lt;T&gt;</c> remains known to be non-null across
        /// a private helper boundary in both transitive modes.
        /// </summary>
        [Fact]
        public void OfTypeForeachValuePassedThroughPrivateHelper_RemainsNonNullInBothModes()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates keys for string values.</summary>\n" +
                "    public void M(IEnumerable<object?> candidates)\n" +
                "    {\n" +
                "        foreach (string symbol in candidates.OfType<string>())\n" +
                "        {\n" +
                "            Add(symbol);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Add(string? symbol)\n" +
                "    {\n" +
                "        _ = new Key(symbol);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(string? symbol)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(symbol);\n" +
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }

        /// <summary>
        /// Reproduces the runtime-target collection shape used by summary
        /// dispatch resolution: only pattern-proven non-null values are inserted
        /// into a local dictionary, its values are returned as an array, and a
        /// caller later enumerates those values before invoking a guarded helper.
        /// </summary>
        [Fact]
        public void GuardedDictionaryValuesReturnedAndEnumerated_DoNotProduceArgumentNullFinding()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates keys for resolved values.</summary>\n" +
                "    public void M(object? candidate)\n" +
                "    {\n" +
                "        IReadOnlyList<string> runtimeTargets =\n" +
                "            ResolveRuntimeTargets(candidate);\n" +
                "\n" +
                "        foreach (string runtimeTarget in runtimeTargets)\n" +
                "        {\n" +
                "            Add(runtimeTarget);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static IReadOnlyList<string> ResolveRuntimeTargets(\n" +
                "        object? candidate)\n" +
                "    {\n" +
                "        Dictionary<int, string> runtimeTargets = new();\n" +
                "\n" +
                "        if (candidate is string runtimeTarget)\n" +
                "        {\n" +
                "            runtimeTargets.TryAdd(0, runtimeTarget);\n" +
                "        }\n" +
                "\n" +
                "        return runtimeTargets.Values.ToArray();\n" +
                "    }\n" +
                "\n" +
                "    private static void Add(string? runtimeTarget)\n" +
                "    {\n" +
                "        _ = new Key(runtimeTarget);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(string? runtimeTarget)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(runtimeTarget);\n" +
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
                projectFindings.Count == 0 &&
                solutionFindings.Count == 0,
                $"Expected no findings. ProjectTransitive: {projectFindings.Count}; " +
                $"SolutionTransitive: {solutionFindings.Count}.");
        }

        /// <summary>
        /// Ensures that the comparison still reports a genuinely reachable
        /// <see cref="ArgumentNullException"/> when no non-null fact exists at
        /// the guarded constructor call site.
        /// </summary>
        [Fact]
        public void PossiblyNullValuePassedToGuardedConstructor_IsReportedByBothModes()
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Creates a key for a value.</summary>\n" +
                "    public void M(string? symbol)\n" +
                "    {\n" +
                "        _ = new Key(symbol);\n" +
                "    }\n" +
                "\n" +
                "    private sealed class Key\n" +
                "    {\n" +
                "        public Key(string? symbol)\n" +
                "        {\n" +
                "            System.ArgumentNullException.ThrowIfNull(symbol);\n" +
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

            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                projectFinding.Smell.ID);

            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                solutionFinding.Smell.ID);

            Assert.Contains(
                "System.ArgumentNullException",
                projectFinding.Message,
                StringComparison.Ordinal);

            Assert.Contains(
                "System.ArgumentNullException",
                solutionFinding.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public void OfTypeElementsThroughOrderByAndToArray_RemainNonNull()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates filtered values.</summary>\n" +
                "    public void M()\n" +
                "    {\n" +
                "        IEnumerable<object?> values =\n" +
                "            new object?[] { \"value\", null };\n" +
                "\n" +
                "        foreach (string value in values\n" +
                "                     .OfType<string>()\n" +
                "                     .OrderBy(static item => item)\n" +
                "                     .ToArray())\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }

        [Fact]
        public void GuardedDictionaryValues_RemainNonNull()
        {
            string source =
                "using System.Collections.Generic;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates resolved values.</summary>\n" +
                "    public void M(object? candidate)\n" +
                "    {\n" +
                "        Dictionary<int, string> values = new();\n" +
                "\n" +
                "        if (candidate is string value)\n" +
                "        {\n" +
                "            values.TryAdd(0, value);\n" +
                "        }\n" +
                "\n" +
                "        foreach (string value in values.Values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        System.ArgumentNullException.ThrowIfNull(value);\n" +
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }

        /// <summary>
        /// Reproduces the productive runtime-target resolver shape in which an empty
        /// dictionary is populated only through a private helper before its values
        /// pass through element-preserving LINQ operations.
        /// </summary>
        [Fact]
        public void DictionaryValuesPopulatedThroughPrivateHelper_RemainNonNull()
        {
            string source =
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    /// <summary>Validates resolved values.</summary>\n" +
                "    public void M(object? candidate)\n" +
                "    {\n" +
                "        IReadOnlyList<string> values = Resolve(candidate);\n" +
                "\n" +
                "        foreach (string value in values)\n" +
                "        {\n" +
                "            Validate(value);\n" +
                "        }\n" +
                "    }\n" +
                "\n" +
                "    private static IReadOnlyList<string> Resolve(object? candidate)\n" +
                "    {\n" +
                "        Dictionary<string, string> values =\n" +
                "            new(StringComparer.Ordinal);\n" +
                "\n" +
                "        TryAdd(candidate, values);\n" +
                "\n" +
                "        return values.Values\n" +
                "            .OrderBy(static value => value, StringComparer.Ordinal)\n" +
                "            .ToArray();\n" +
                "    }\n" +
                "\n" +
                "    private static void TryAdd(\n" +
                "        object? candidate,\n" +
                "        Dictionary<string, string> values)\n" +
                "    {\n" +
                "        if (candidate is not string value)\n" +
                "        {\n" +
                "            return;\n" +
                "        }\n" +
                "\n" +
                "        values.TryAdd(value, value);\n" +
                "    }\n" +
                "\n" +
                "    private static void Validate(string? value)\n" +
                "    {\n" +
                "        ArgumentNullException.ThrowIfNull(value);\n" +
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }

        /// <summary>
        /// Demonstrates that a documented private helper is analyzed independently
        /// even when every real call site supplies a value proven to be non-null.
        /// </summary>
        [Fact]
        public void DocumentedPrivateHelperWithOnlyNonNullCallSites_DoesNotInventNullFlow()
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
                "    private static void Add(string? value)\n" +
                "    {\n" +
                "        ArgumentNullException.ThrowIfNull(value);\n" +
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

            Assert.Empty(projectFindings);
            Assert.Empty(solutionFindings);
        }
    }
}
