using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphIndex;
using SEE.DataModel.DG.IO;
using SEE.GraphProviders.NodeCounting;
using SEE.Utils.Paths;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Abstract base class for testing report parsers and metric application against a graph.
    ///
    /// This fixture follows the Template Method pattern:
    /// concrete subclasses provide report-specific configuration (paths, parsing config, nodes to parse,
    /// node counter, and expected <see cref="Finding"/> instances), while this base class provides the shared
    /// arrange/act/assert workflow and reusable helper assertions.
    ///
    /// Supported test scenarios:
    /// - Parsing validation: Ensure a report parses into a <see cref="MetricSchema"/> with expected findings.
    /// - Count validation: Ensure the number of findings per context matches expected counts.
    /// - Metric validation: Ensure a curated set of findings matches on context, location, and metrics.
    /// - Graph application validation: Ensure parsed metrics are applied onto the correct graph nodes and can be
    ///   read back under the expected metric keys (including potential key disambiguation).
    ///
    /// Subclass contract:
    /// - <see cref="GetRelativeReportPath"/> and <see cref="GetRelativeGlxPath"/> must point to existing assets
    ///   under <see cref="Application.streamingAssetsPath"/>.
    /// - <see cref="GetParsingConfig"/> must return a non-null configuration capable of producing an <see cref="IReportParser"/>.
    /// - <see cref="GetNodeCounter"/> must return a counter that is compatible with the report format and the node filter.
    /// - <see cref="GetTestFindings"/> must return deterministic expected findings (stable paths, locations, and metrics).
    ///
    /// Notes:
    /// - This base class intentionally includes diagnostic logging to aid debugging when a test fails
    ///   (e.g., enumerating parsed finding paths).
    /// - Metric keys are namespaced by tool id via <see cref="Metrics.Prefix"/> + <see cref="ParsingConfig.ToolId"/>.
    /// </summary>
    internal abstract class TestReportGraphProviderBase
    {
        // ---- Template methods (must be implemented by derived classes) ----

        /// <summary>
        /// Returns the report path relative to <see cref="Application.streamingAssetsPath"/>.
        ///
        /// Leading slashes and backslashes are ignored by <see cref="SetUp"/> when combining the path.
        ///
        /// Preconditions:
        /// - The returned relative path must be non-null/non-empty and point to a readable report file.
        /// </summary>
        /// <returns>Report path relative to <see cref="Application.streamingAssetsPath"/>.</returns>
        protected abstract string GetRelativeReportPath();

        /// <summary>
        /// Returns the GLX path relative to <see cref="Application.streamingAssetsPath"/>.
        ///
        /// Leading slashes and backslashes are ignored by <see cref="SetUp"/> when combining the path.
        ///
        /// Preconditions:
        /// - The returned relative path must be non-null/non-empty and point to a readable GLX file.
        /// </summary>
        /// <returns>GLX path relative to <see cref="Application.streamingAssetsPath"/>.</returns>
        protected abstract string GetRelativeGlxPath();

        /// <summary>
        /// Returns the set of node identifiers or contexts the test should consider during counting.
        ///
        /// Interpretation depends on the concrete <see cref="ICountReportNodes"/> implementation. For example:
        /// - For XML-based reports, this may correspond to element/tag names.
        /// - For JSON-based reports, this might correspond to node kinds or categories.
        ///
        /// Preconditions:
        /// - Returned values should align with what the parser emits as <see cref="Finding.Context"/>
        ///   (or with the mapping from parser node types to contexts).
        /// </summary>
        /// <returns>Array of node identifiers or contexts to parse.</returns>
        protected abstract string[] GetNodesToParse();

        /// <summary>
        /// Returns the parsing configuration used to instantiate the corresponding parser.
        ///
        /// Preconditions:
        /// - Must not return null. <see cref="BuildMetricSchemaAsync"/> asserts this explicitly.
        /// - The returned config must be compatible with the report referenced by <see cref="GetRelativeReportPath"/>.
        /// </summary>
        /// <returns>Parsing configuration used to create the report parser. Must not be null.</returns>
        protected abstract ParsingConfig GetParsingConfig();

        /// <summary>
        /// Returns a node counter that can compute expected counts per context for the given report and node filter.
        ///
        /// The counter is used to establish baseline expectations around how many "parseable" nodes exist per context.
        ///
        /// Preconditions:
        /// - Must not return null.
        /// - Must be compatible with the report format.
        /// </summary>
        /// <returns>Node counter used to compute expected counts per context.</returns>
        protected abstract ICountReportNodes GetNodeCounter();

        /// <summary>
        /// Returns a curated set of expected findings (keyed by a stable identifier, typically <see cref="Finding.FullPath"/>)
        /// that should be present in the parsed schema, including expected context/location/metrics.
        ///
        /// Notes:
        /// - The returned set should be small and representative to keep tests stable and readable.
        /// - Keys do not need to be unique file paths if the same file can yield multiple findings; in that case
        ///   derived classes can use composite keys (e.g., add a suffix) as long as the values remain correct.
        /// </summary>
        /// <returns>Dictionary of expected findings keyed by their full path (or a derived stable key).</returns>
        protected abstract Dictionary<string, Finding> GetTestFindings();

        // ---- Shared state for a single test run ----

        /// <summary>
        /// Parsed metric schema under test for the current run.
        ///
        /// This is created by <see cref="BuildMetricSchemaAsync"/> and then used by multiple tests.
        /// </summary>
        private MetricSchema metricSchema;

        /// <summary>
        /// Absolute path to the report file for the current test run (computed in <see cref="SetUp"/>).
        /// </summary>
        private string fullReportPath;

        /// <summary>
        /// Absolute path to the GLX file for the current test run (computed in <see cref="SetUp"/>).
        /// </summary>
        private string fullGlxPath;

        /// <summary>
        /// The name of the hierarchical edge type used when loading graphs to reconstruct parent-child relationships.
        ///
        /// This constant is passed to <see cref="GraphReader.LoadAsync"/> to ensure parent links such as
        /// method-to-class relationships can be traversed when resolving nodes for findings.
        /// </summary>
        private const string hierarchicalEdgeType = "Enclosing";

        /// <summary>
        /// Loads a <see cref="Graph"/> from a GXL/GLX file asynchronously.
        ///
        /// The loader is configured to include hierarchical relations via <see cref="hierarchicalEdgeType"/>.
        ///
        /// Preconditions:
        /// - <paramref name="path"/> must point to a valid graph file.
        /// - The graph file must be readable and compatible with the loader.
        /// </summary>
        /// <param name="path">Data path of the GXL/GLX file.</param>
        /// <returns>
        /// A task that represents the asynchronous load operation.
        /// The task result is the loaded <see cref="Graph"/>.
        /// </returns>
        private static async UniTask<Graph> LoadGraphAsync(DataPath path)
        {
            return await GraphReader.LoadAsync(path, new HashSet<string> { hierarchicalEdgeType }, basePath: string.Empty);
        }

        /// <summary>
        /// The graph used in <see cref="TestMetricAppliedToGraphAsync"/> to validate metric application.
        ///
        /// This is initialized when the metric-to-graph test runs.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// Resolves the absolute report and graph paths before each test executes.
        ///
        /// The returned values from <see cref="GetRelativeReportPath"/> and <see cref="GetRelativeGlxPath"/> are treated
        /// as paths relative to <see cref="Application.streamingAssetsPath"/>. Leading path separators are removed to
        /// avoid accidental absolute path interpretation.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            string relativeReportPath = GetRelativeReportPath();
            fullReportPath = Path.Combine(
                Application.streamingAssetsPath,
                relativeReportPath.TrimStart('/', '\\'));

            string relativeGlxPath = GetRelativeGlxPath();
            fullGlxPath = Path.Combine(
                Application.streamingAssetsPath,
                relativeGlxPath.TrimStart('/', '\\'));
        }

        /// <summary>
        /// Clears cached state between tests to avoid cross-test pollution.
        ///
        /// Note:
        /// Only <see cref="metricSchema"/> is cleared here because it is the main per-test artifact.
        /// Other fields are recomputed in <see cref="SetUp"/>.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            metricSchema = null;
        }

        /// <summary>
        /// Parses the configured report file into a <see cref="MetricSchema"/>.
        ///
        /// Validates that:
        /// - The parsing config is provided by <see cref="GetParsingConfig"/>.
        /// - The config can create a parser instance via <see cref="ParsingConfig.CreateParser"/>.
        ///
        /// Preconditions:
        /// - <see cref="SetUp"/> must have executed to initialize <see cref="fullReportPath"/>.
        /// - The report file must exist and be compatible with the returned parser.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous parse operation.
        /// The task result is the parsed <see cref="MetricSchema"/>.
        /// </returns>
        private async UniTask<MetricSchema> BuildMetricSchemaAsync()
        {
            DataPath reportDataPath = new DataPath(fullReportPath);

            ParsingConfig config = GetParsingConfig();
            Assert.NotNull(config, "GetParsingConfig() returned null.");

            IReportParser parser = config.CreateParser();
            Assert.NotNull(parser, "CreateParser() returned null (IReportParser).");

            return await parser.ParseAsync(reportDataPath);
        }

        /// <summary>
        /// Asserts that actual counts (grouped by <see cref="Finding.Context"/>) match the expected counts.
        ///
        /// Behavior:
        /// - Groups all parsed findings by their <see cref="Finding.Context"/> (case-insensitive).
        /// - For each expected entry, maps from the node key (often an XML tag name) to the parser context
        ///   using <see cref="XPathMapping.MapContext"/>.
        /// - Asserts that the actual count for that context equals the expected count.
        ///
        /// Notes:
        /// - This method does not currently assert that *no other* contexts are present; it focuses on matching
        ///   expected contexts. If you want stricter behavior, you can extend it to compare key sets.
        ///
        /// Preconditions:
        /// - <see cref="metricSchema"/> must have been initialized by parsing.
        /// - <paramref name="expectedNodeCounts"/> must not be null.
        /// </summary>
        /// <param name="expectedNodeCounts">Expected node counts per context key (e.g., per XML tag name).</param>
        private void AssertNodeCountsMatch(Dictionary<string, int> expectedNodeCounts)
        {
            Assert.IsNotNull(metricSchema, "metricSchema has not been initialized.");
            Assert.IsNotNull(metricSchema.Findings, "metricSchema.Findings is null.");

            Dictionary<string, int> actualNodeCounts = metricSchema.Findings
                .Where(finding => !string.IsNullOrEmpty(finding.Context))
                .GroupBy(finding => finding.Context, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, int> expectedEntry in expectedNodeCounts)
            {
                // Convert the "node key" used for counting (e.g., "file") into the parser's output context label.
                string xmlTagAsContext = GetParsingConfig().XPathMapping.MapContext[expectedEntry.Key];

                int actualCount = actualNodeCounts.TryGetValue(xmlTagAsContext, out int count) ? count : 0;

                Assert.AreEqual(
                    expectedEntry.Value,
                    actualCount,
                    $"Context '{expectedEntry.Key}': expected {expectedEntry.Value}, got {actualCount}.");
            }
        }

        /// <summary>
        /// Parses the report and verifies that a curated set of expected findings exists.
        ///
        /// For each expected finding, this test asserts:
        /// - The finding can be located in the parsed schema (see <see cref="FindActualNode"/>).
        /// - The found instance matches expected fields (see <see cref="AssertFindingMatch"/>).
        ///
        /// Diagnostics:
        /// - Logs all FullPath values found in the parsed schema to assist debugging mismatches.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
        [Test]
        public async Task TestSpecificFindingsAsync()
        {
            metricSchema = await BuildMetricSchemaAsync();

            // Helpful for diagnosing "not found" issues caused by path normalization differences.
            foreach (Finding finding in metricSchema.Findings)
            {
                Debug.LogWarning($"FullPath of Finding: {finding.FullPath}");
            }

            Dictionary<string, Finding> testFindings = GetTestFindings();

            foreach (KeyValuePair<string, Finding> expected in testFindings)
            {
                Finding actual = FindActualNode(expected.Value);
                Assert.NotNull(actual, $"Finding '{expected.Key} ' not found.");
                AssertFindingMatch(actual, expected.Value);
            }
        }

        /// <summary>
        /// Searches for an expected <see cref="Finding"/> in the actual <see cref="metricSchema"/>.
        ///
        /// Matching criteria:
        /// - <see cref="Finding.FullPath"/> must match exactly (ordinal, case-sensitive as written here).
        /// - Start line and column must match if present on the expected finding.
        ///
        /// Notes:
        /// - This deliberately uses a strict match on FullPath. If paths can differ by casing or normalization
        ///   in a given environment, consider normalizing paths or using a comparer.
        /// - This method returns the first match, which is sufficient if expected findings are unique under the match criteria.
        ///
        /// Preconditions:
        /// - <see cref="metricSchema"/> must have been initialized.
        /// </summary>
        /// <param name="expected">Expected finding that describes the identity to search for.</param>
        /// <returns>The actual matching <see cref="Finding"/> in the schema, or null if none is found.</returns>
        private Finding FindActualNode(Finding expected)
        {
            return metricSchema.Findings.FirstOrDefault(f =>
                f.FullPath == expected.FullPath &&
                f.Location?.StartLine == expected.Location?.StartLine &&
                f.Location?.StartColumn == expected.Location?.StartColumn);
        }

        /// <summary>
        /// Asserts that two findings are equivalent for the properties that the expected instance specifies.
        ///
        /// Comparison is "opt-in" per field:
        /// - If <see cref="Finding.Context"/> is set on <paramref name="expected"/>, it is asserted.
        /// - If <see cref="Finding.Location"/> is set on <paramref name="expected"/>, each non-null member
        ///   (StartLine, EndLine, StartColumn, EndColumn) is asserted.
        /// - If <see cref="Finding.Metrics"/> contains entries on <paramref name="expected"/>, the metric set
        ///   must match exactly (see <see cref="AssertFindingMetricsMatch"/>).
        ///
        /// Preconditions:
        /// - <paramref name="actual"/> and <paramref name="expected"/> must be non-null.
        /// </summary>
        /// <param name="actual">Actual finding produced by the parser.</param>
        /// <param name="expected">Expected finding that defines the required values.</param>
        private void AssertFindingMatch(Finding actual, Finding expected)
        {
            Assert.IsNotNull(actual);
            Assert.IsNotNull(expected);

            if (!string.IsNullOrEmpty(expected.Context))
            {
                Assert.That(actual.Context, Is.EqualTo(expected.Context), "Context");
            }

            MetricLocation expectedLocation = expected.Location;
            MetricLocation actualLocation = actual.Location;

            if (expectedLocation != null)
            {
                if (expectedLocation.StartLine.HasValue)
                {
                    Assert.That(actualLocation?.StartLine, Is.EqualTo(expectedLocation.StartLine), "StartLine");
                }

                if (expectedLocation.EndLine.HasValue)
                {
                    Assert.That(actualLocation?.EndLine, Is.EqualTo(expectedLocation.EndLine), "EndLine");
                }

                if (expectedLocation.StartColumn.HasValue)
                {
                    Assert.That(actualLocation?.StartColumn, Is.EqualTo(expectedLocation.StartColumn), "StartColumn");
                }

                if (expectedLocation.EndColumn.HasValue)
                {
                    Assert.That(actualLocation?.EndColumn, Is.EqualTo(expectedLocation.EndColumn), "EndColumn");
                }
            }

            if (expected.Metrics is { Count: > 0 })
            {
                AssertFindingMetricsMatch(actual, expected.Metrics);
            }
        }

        /// <summary>
        /// Verifies that a finding's metric dictionary exactly matches the expected metrics.
        ///
        /// Rules:
        /// - Every expected key must exist in <paramref name="actual"/> with the same value.
        /// - No unexpected keys may be present (i.e., actual keys must be a subset equal to expected keys).
        /// - If <paramref name="expectedMetrics"/> is empty, the finding must have no metrics.
        ///
        /// Preconditions:
        /// - <paramref name="actual"/> must not be null.
        /// - <see cref="Finding.Metrics"/> on <paramref name="actual"/> must not be null.
        /// </summary>
        /// <param name="actual">Actual finding under test.</param>
        /// <param name="expectedMetrics">Expected metric key-value pairs.</param>
        private void AssertFindingMetricsMatch(Finding actual, Dictionary<string, string> expectedMetrics)
        {
            Assert.NotNull(actual, "Finding is null.");

            Dictionary<string, string> metrics = actual.Metrics;

            Assert.NotNull(actual.Metrics, $"Metrics are null for node {actual.FullPath}.");

            expectedMetrics ??= new Dictionary<string, string>();

            if (expectedMetrics.Count == 0)
            {
                Assert.IsEmpty(
                    actual.Metrics,
                    $"Expected no metrics but found {actual.Metrics.Count} for {actual.FullPath}.");
                return;
            }

            foreach (KeyValuePair<string, string> testMetric in expectedMetrics)
            {
                Assert.IsTrue(
                    metrics.TryGetValue(testMetric.Key, out string actualValue),
                    $"Missing metric '{testMetric.Key}'. Expected '{testMetric.Value}'.");

                Assert.AreEqual(
                    testMetric.Value,
                    actualValue,
                    $"Metric '{testMetric.Key}' mismatch. Expected '{testMetric.Value}', got '{actualValue}'.");
            }

            // Detect extra metrics that are present on the parsed finding but not expected by the test.
            List<string> unexpected = metrics.Keys.Except(expectedMetrics.Keys).ToList();
            Assert.IsTrue(
                unexpected.Count == 0,
                $"Unexpected metric(s) present: {string.Join(", ", unexpected)}.");
        }

        /// <summary>
        /// Parses the report, applies the parsed metrics to a loaded graph, and verifies that the expected
        /// metric values are present on the correct graph nodes.
        ///
        /// Workflow:
        /// 1) Parse report into <see cref="MetricSchema"/>.
        /// 2) Load graph from the provided GLX path.
        /// 3) Create an index strategy and <see cref="SourceRangeIndex"/> for resolving line-based findings.
        /// 4) Apply all metrics onto graph nodes via <see cref="MetricApplier.ApplyMetrics"/>.
        /// 5) For each expected finding, resolve the target node (by line index or by node id) and assert that
        ///    all expected metrics exist with exact values.
        ///
        /// Key handling:
        /// - Metrics are stored with a tool-specific prefix: <c>Metrics.Prefix + ToolId + "."</c>.
        /// - If duplicate keys occur, the metric applier may create disambiguated keys (e.g., "Key [1]").
        ///   This test accounts for that via <see cref="isValueInMetrics"/>.
        ///
        /// Preconditions:
        /// - Graph loading must succeed and produce nodes that can be resolved by the index strategy.
        /// - The metric applier must use the same prefixing conventions as used here for lookups.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
        [Test]
        public async Task TestMetricAppliedToGraphAsync()
        {
            metricSchema = await BuildMetricSchemaAsync();
            Dictionary<string, Finding> expectedFindings = GetTestFindings();

            DataPath gxlPath = new DataPath(fullGlxPath);
            graph = await LoadGraphAsync(gxlPath);

            ParsingConfig parsingConfig = GetParsingConfig();
            IIndexNodeStrategy indexNodeStrategy = parsingConfig.CreateIndexNodeStrategy();

            // Build an index for mapping (mainType, line) -> node.
            // This is used for findings that carry a source line location.
            SourceRangeIndex index = new SourceRangeIndex(graph, indexNodeStrategy.NodeIdToMainType);

            // Apply all parsed metrics to the graph nodes.
            MetricApplier.ApplyMetrics(graph, metricSchema, parsingConfig);

            // Metric keys written by MetricApplier are prefixed with this tool id namespace.
            string prefix = Metrics.Prefix + parsingConfig.ToolId + ".";

            foreach (KeyValuePair<string, Finding> kv in expectedFindings)
            {
                Finding expected = kv.Value;
                string fullPath = expected.FullPath;

                // Strategy-dependent normalization:
                // - main type is used for line-to-node lookup
                // - node id is used for direct graph.TryGetNode lookup
                string findingPathAsMainType = indexNodeStrategy.FindingPathToMainType(fullPath, expected.FileName);
                string findingPathAsNodeId = indexNodeStrategy.FindingPathToNodeId(fullPath);

                Node node = null;

                int startLine = expected.Location?.StartLine ?? -1;

                if (startLine != -1)
                {
                    // Line-based finding: resolve node via SourceRangeIndex.
                    index.TryGetValue(findingPathAsMainType, startLine, out node);
                }
                else
                {
                    // File/type-level finding: resolve node via node id.
                    graph.TryGetNode(findingPathAsNodeId, out node);
                }

                Assert.NotNull(
                    node,
                    $"Node '{findingPathAsNodeId}' with FullPath {fullPath} with FindingPathAsMainType: {findingPathAsMainType} not found in graph.");

                foreach (KeyValuePair<string, string> m in expected.Metrics)
                {
                    // Construct the fully-qualified metric key as it is written to the graph.
                    string metricKey = prefix + m.Key;
                    string value;

                    // Because MetricApplier may rename duplicates to "Key [i]",
                    // we search both the base key and any indexed variants.
                    if (isValueInMetrics(node, metricKey, m.Value, out string foundValue))
                    {
                        value = foundValue;
                    }
                    else
                    {
                        Assert.Fail($"Metric '{metricKey}' not found on node '{findingPathAsNodeId}' (Context '{expected.Context}').");
                        return;
                    }

                    Assert.AreEqual(
                        m.Value,
                        value,
                        $"Metric '{metricKey}' mismatch in node '{findingPathAsNodeId}'. Expected '{m.Value}', got '{value}'.");
                }
            }
        }

        /// <summary>
        /// Checks whether a node contains a metric with the specified key and expected value.
        ///
        /// This helper accounts for the metric key disambiguation strategy used by <see cref="MetricApplier"/>:
        /// if a metric key already exists on a node, the applier may write the same metric under a new key
        /// of the form <c>"{key} [i]"</c> (i starting at 1).
        ///
        /// Search order:
        /// 1) Try the base key <paramref name="metricKey"/>.
        /// 2) If not equal (or not found), try indexed keys: <c>metricKey + " [1]"</c>, <c>" [2]"</c>, ...
        ///    until the first missing indexed key stops the loop.
        ///
        /// Value handling:
        /// - Node storage may contain ints/floats/strings. We convert to string using invariant culture
        ///   to compare with expected string values coming from the parsing layer.
        ///
        /// Preconditions:
        /// - <paramref name="node"/> must not be null.
        /// - <paramref name="metricKey"/> must not be null or empty.
        /// </summary>
        /// <param name="node">Graph node that should contain the metric.</param>
        /// <param name="metricKey">Fully qualified metric key (including tool prefix).</param>
        /// <param name="expectedValue">Expected metric value (string form).</param>
        /// <param name="found">The value found on the node (converted to string), or null if not found.</param>
        /// <returns>True if a matching key/value pair exists; otherwise false.</returns>
        private bool isValueInMetrics(Node node, string metricKey, string expectedValue, out string found)
        {
            // 1) Check the base key first.
            if (node.TryGetAny(metricKey, out object valueWithBaseKey))
            {
                string convertedValue = Convert.ToString(valueWithBaseKey, CultureInfo.InvariantCulture);
                if (expectedValue.Equals(convertedValue))
                {
                    found = convertedValue;
                    return true;
                }
            }

            // 2) Check indexed keys created by MetricApplier (e.g., "Key [1]", "Key [2]", ...).
            int i = 1;
            while (node.TryGetAny(metricKey + " [" + i + "]", out object value))
            {
                string converted = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (expectedValue.Equals(converted))
                {
                    found = Convert.ToString(value, CultureInfo.InvariantCulture);
                    return true;
                }
                i++;
            }

            found = null;
            return false;
        }
    }
}
