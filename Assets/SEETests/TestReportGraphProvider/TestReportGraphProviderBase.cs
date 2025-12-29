using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphIndex;
using SEE.DataModel.DG.IO;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private DataPath fullReportPath;

        /// <summary>
        /// Absolute path to the GLX file for the current test run (computed in <see cref="SetUp"/>).
        /// </summary>
        private DataPath fullGlxPath;

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
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string dataBasePath = Path.Combine(projectRoot, "Data");

            string relativeReportPath = GetRelativeReportPath();
            fullReportPath = new DataPath(Path.Combine(
                dataBasePath,
                relativeReportPath.TrimStart('/', '\\')));

            string relativeGlxPath = GetRelativeGlxPath();
            fullGlxPath = new DataPath(Path.Combine(
                dataBasePath,
                relativeGlxPath.TrimStart('/', '\\')));
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

            ParsingConfig config = GetParsingConfig();
            Assert.NotNull(config, "GetParsingConfig() returned null.");

            IReportParser parser = config.CreateParser();
            Assert.NotNull(parser, "CreateParser() returned null (IReportParser).");

            return await parser.ParseAsync(fullReportPath);
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

            Dictionary<string, Finding> testFindings = GetTestFindings();

            foreach (KeyValuePair<string, Finding> expected in testFindings)
            {
                Finding actual = FindActualNode(expected.Value);
                Assert.NotNull(actual, $"Finding '{expected.Key}' not found.");
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
        /// 4) REPLICATE the "Fallback Index" logic from MetricApplier to be able to find nodes by their clean logical ID.
        /// 5) Apply all metrics onto graph nodes via <see cref="MetricApplier.ApplyMetrics"/>.
        /// 6) For each expected finding, resolve the target node (by line index or by fallback type index) and assert that
        ///    all expected metrics exist with exact values.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
        [Test]
        public async Task TestMetricAppliedToGraphAsync()
        {
            metricSchema = await BuildMetricSchemaAsync();
            Dictionary<string, Finding> expectedFindings = GetTestFindings();

            graph = await LoadGraphAsync(fullGlxPath);

            ParsingConfig parsingConfig = GetParsingConfig();
            IIndexNodeStrategy indexNodeStrategy = parsingConfig.CreateIndexNodeStrategy();

            // 1. Build Range Index for exact line lookups (replicates MetricApplier logic)
            SourceRangeIndex rangeIndex = new (graph, indexNodeStrategy.ToLogicalIdentifier);

            // 2. Build Fallback Index (Type/File Map) - REPLICATION of MetricApplier logic.
            // We need to build this index manually in the test to ensure we can locate the "Container" node
            // when we only possess the "Clean Logical ID" from the report, but the graph uses "Technical IDs".
            Dictionary<string, Node> typeIndex = new Dictionary<string, Node>();
            foreach (Node node in graph.Nodes())
            {
                string logicalId = indexNodeStrategy.ToLogicalIdentifier(node);
                if (!string.IsNullOrEmpty(logicalId) && NodeTypeExtensions.IsContainer(node.Type))
                {
                    // In case of partial classes/duplicates, we take the first one, just like the Applier.
                    if (!typeIndex.ContainsKey(logicalId))
                    {
                        typeIndex[logicalId] = node;
                    }
                }
            }

            // Apply all parsed metrics to the graph nodes.
            MetricApplier.ApplyMetrics(graph, metricSchema, parsingConfig);

            // Metric keys written by MetricApplier are prefixed with this tool id namespace.
            string prefix = Metrics.Prefix + parsingConfig.ToolId + ".";

            foreach (KeyValuePair<string, Finding> findingEntry in expectedFindings)
            {
                Finding expected = findingEntry.Value;
                string fullPath = expected.FullPath;

                // We primarily use the MainType (Logical ID) for both lookup strategies now.
                string findingPathAsMainType = indexNodeStrategy.ToLogicalIdentifier(fullPath);

                Node node = null;
                int startLine = expected.Location?.StartLine ?? -1;

                // Strategy 1: Try exact match via Range Index (e.g. Method)
                if (startLine != -1)
                {
                    rangeIndex.TryGetValue(findingPathAsMainType, startLine, out node);
                }

                // Strategy 2: Fallback Lookup (Class/File)
                // If the range lookup failed (e.g. because the graph has no methods) or no line was provided,
                // we look up the node in our manually built TypeIndex.
                if (node == null)
                {
                    string fullIdentifier = indexNodeStrategy.ToFullIdentifier(fullPath);
                    typeIndex.TryGetValue(fullIdentifier, out node);
                }

                Assert.NotNull(
                    node,
                    $"Node for path '{fullPath}' (Logical ID: {findingPathAsMainType}) not found in graph via Range or Type index.");


                foreach (KeyValuePair<string, string> metricEntry in expected.Metrics)
                {
                    // Construct the fully-qualified metric key as it is written to the graph.
                    string metricKey = prefix + metricEntry.Key;
                    string value;

                    // Because MetricApplier may rename duplicates to "Key [i]",
                    // we search both the base key and any indexed variants.
                    if (IsValueInMetrics(node, metricKey, metricEntry.Value, out string foundValue))
                    {
                        value = foundValue;
                    }
                    else
                    {
                        Assert.Fail($"Metric '{metricKey}' not found on node '{node.ID}' (Expected Logical ID '{findingPathAsMainType}').");
                        return;
                    }

                    Assert.AreEqual(
                        metricEntry.Value,
                        value,
                        $"Metric '{metricKey}' mismatch in node '{node.ID}'. Expected '{metricEntry.Value}', got '{value}'.");
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
        private bool IsValueInMetrics(Node node, string metricKey, string expectedValue, out string found)
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
