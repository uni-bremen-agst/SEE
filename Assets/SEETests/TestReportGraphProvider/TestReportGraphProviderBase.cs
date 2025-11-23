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
    /// Abstract base class for testing report parsers.
    /// 
    /// This fixture follows the Template Method pattern: concrete subclasses provide
    /// the specifics (report path, nodes to parse, parsing configuration, node counter,
    /// and a set of expected <see cref="Finding"/> instances), while this base class
    /// provides reusable arrange/act/assert logic and helpful diagnostics for failures.
    /// 
    /// Supported use cases:
    /// - Validating node counts against expectations (per context and total).
    /// - Verifying selected, representative <see cref="Finding"/> instances including
    ///   their location data and metric dictionaries.
    /// 
    /// Contract for subclasses:
    /// - Return consistent relative paths via <see cref="GetRelativeReportPath"/>.
    /// - Provide a matching <see cref="ParsingConfig"/> that can produce an <see cref="IReportParser"/>.
    /// - Supply a <see cref="ICountReportNodes"/> implementation that counts the same
    ///   contexts the parser will produce.
    /// - Provide deterministic expected findings via <see cref="GetTestFindings"/>.
    /// </summary>
    internal abstract class TestReportGraphProviderBase
    {
        // ---- Template methods (must be implemented by derived classes) ----

        /// <summary>
        /// Returns the report path relative to <see cref="Application.streamingAssetsPath"/>.
        /// Leading slashes and backslashes are ignored.
        /// </summary>
        /// <returns>Report path relative to <see cref="Application.streamingAssetsPath"/>.</returns>
        protected abstract string GetRelativeReportPath();

        /// <summary>
        /// Returns the glx path relative to <see cref="Application.streamingAssetsPath"/>.
        /// Leading slashes and backslashes are ignored.
        /// </summary>
        /// <returns>Glx path relative to <see cref="Application.streamingAssetsPath"/>.</returns>
        protected abstract string GetRelativeGlxPath();

        /// <summary>
        /// Returns the set of node identifiers or contexts the test should consider during counting.
        /// </summary>
        /// <returns>Array of node identifiers or contexts to parse.</returns>
        protected abstract string[] GetNodesToParse();

        /// <summary>
        /// Returns the parsing configuration used to instantiate the corresponding parser.
        /// Must not return null; <see cref="SetUp"/> and the tests assert this.
        /// </summary>
        /// <returns>Parsing configuration used to create the report parser. Must not be null.</returns>
        protected abstract ParsingConfig GetParsingConfig();

        /// <summary>
        /// Returns a node counter that can compute expected counts per context
        /// for the given report and node filter.
        /// </summary>
        /// <returns>Node counter used to compute expected counts per context.</returns>
        protected abstract ICountReportNodes GetNodeCounter();

        /// <summary>
        /// Returns a set of hand-picked findings (keyed by <see cref="Finding.FullPath"/>)
        /// that should be present in the parsed schema, including expected metadata.
        /// </summary>
        /// <returns>Dictionary of expected findings keyed by their full path.</returns>
        protected abstract Dictionary<string, Finding> GetTestFindings();

        // ---- Shared state for a single test run ----

        /// <summary>
        /// Parsed metric schema that is under test in the current test run.
        /// </summary>
        private MetricSchema metricSchema;

        /// <summary>
        /// Absolute path to the report file for the current test run.
        /// </summary>
        private string fullReportPath;

        /// <summary>
        /// Absolute path to the Glx file for the current test run
        /// </summary>
        private string fullGlxPath;


        /// <summary>
        /// The name of the hierarchical edge type we use for emitting the parent-child
        /// relation among nodes.
        /// </summary>
        private const string hierarchicalEdgeType = "Enclosing";

        /// <summary>
        /// Load graph from GXL file located at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Data path of the GXL file.</param>
        /// <returns>A task that represents the asynchronous load operation. The task result is the loaded graph.</returns>
        private static async UniTask<Graph> LoadGraphAsync(DataPath path)
        {
            return await GraphReader.LoadAsync(path, new HashSet<string> { hierarchicalEdgeType }, basePath: string.Empty);
        }

        /// <summary>
        /// The graph that was loaded by <see cref="TestMetricAppliedToGraphAsync"/> for metric application tests.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// Resolves the absolute path to the test report before each test executes.
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
        /// - The parsing config is provided.
        /// - The config can create a parser instance.
        /// </summary>
        /// <returns>A task that represents the asynchronous parse operation. The task result is the parsed metric schema.</returns>
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
        /// Asserts that actual counts (grouped by <see cref="Finding.Context"/>) match the
        /// expected per-context counts exactly, and that there are no unexpected contexts.
        /// Matching is case-insensitive on context keys.
        /// </summary>
        /// <param name="expectedNodeCounts">Expected node counts per context.</param>
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
                string xmlTagAsContext = GetParsingConfig().XPathMapping.MapContext[expectedEntry.Key];

                int actualCount = actualNodeCounts.TryGetValue(xmlTagAsContext, out int count) ? count : 0;

                Assert.AreEqual(
                    expectedEntry.Value,
                    actualCount,
                    $"Context '{expectedEntry.Key}': expected {expectedEntry.Value}, got {actualCount}.");
            }
        }

        /// <summary>
        /// Verifies that a curated set of expected findings exists and matches on:
        /// - Context (if provided),
        /// - Location (line and column spans if provided),
        /// - Metric dictionary (exact keys and values if provided).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
        [Test]
        public async Task TestSpecificFindingsAsync()
        {
            metricSchema = await BuildMetricSchemaAsync();

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
        /// Searches for the expected Finding in the actual MetricSchema. 
        /// Findings can be identified by FullPath and StartLine.
        /// </summary>
        /// <param name="expected"></param>
        /// <returns>The actual found <see cref="Finding"/> in the MetricSchema for a expected Finding</returns>
        private Finding FindActualNode(Finding expected)
        {
            return metricSchema.Findings.FirstOrDefault(f =>
                f.FullPath == expected.FullPath &&
                f.Location?.StartLine == expected.Location?.StartLine);
        }


        /// <summary>
        /// Asserts that two findings are equivalent for the properties that the expected
        /// instance specifies (context, location members, metrics).
        /// 
        /// The comparison is opt-in per field: if an expected sub-value is null or missing,
        /// it is not asserted.
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
        /// Verifies that a finding's metric dictionary exactly matches the expected metrics:
        /// - Every expected key exists with the same value.
        /// - No unexpected keys are present.
        /// If no metrics are expected, the finding must have an empty metric dictionary.
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

            List<string> unexpected = metrics.Keys.Except(expectedMetrics.Keys).ToList();
            Assert.IsTrue(
                unexpected.Count == 0,
                $"Unexpected metric(s) present: {string.Join(", ", unexpected)}.");
        }

        /// <summary>
        /// Verifies that a curated set of expected findings exists and matches on:
        /// - Context (if provided),
        /// - Location (line and column spans if provided),
        /// - Metric dictionary (exact keys and values if provided),
        /// after metrics have been applied to the graph.
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

            SourceRangeIndex index = new SourceRangeIndex(graph, indexNodeStrategy.NodeIdToMainType);

            MetricApplier.ApplyMetrics(graph, metricSchema, parsingConfig);

            string prefix = Metrics.Prefix + parsingConfig.ToolId + ".";

            foreach (KeyValuePair<string, Finding> kv in expectedFindings)
            {
                string fullPath = kv.Key;
                Finding expected = kv.Value;

                string findingPathAsMainType = indexNodeStrategy.FindingPathToMainType(fullPath, expected.FileName);
                string findingPathAsNodeId = indexNodeStrategy.FindingPathToNodeId(fullPath);

                Node node = null;

                if (expected.Context.Equals("method", StringComparison.OrdinalIgnoreCase))
                {
                    int startLine = expected.Location?.StartLine ?? -1;
                    index.TryGetValue(findingPathAsMainType, startLine, out node);
                }
                else
                {
                    graph.TryGetNode(findingPathAsNodeId, out node);
                }

                Assert.NotNull(
                    node,
                    $"Node '{findingPathAsNodeId}' with FullPath {fullPath} with FindingPathAsMainType: {findingPathAsMainType} not found in graph.");

                foreach (KeyValuePair<string, string> m in expected.Metrics)
                {
                    string metricKey = prefix + m.Key;
                    string value;

                    if (node.TryGetInt(metricKey, out int intVal))
                    {
                        value = intVal.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (node.TryGetFloat(metricKey, out float floatVal))
                    {
                        value = floatVal.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (node.TryGetString(metricKey, out string strVal))
                    {
                        value = strVal;
                    }
                    else
                    {
                        Assert.Fail(
                            $"Metric '{metricKey}' not found on node '{findingPathAsNodeId}' (Context '{expected.Context}').");
                        return;
                    }

                    Assert.AreEqual(
                        m.Value,
                        value,
                        $"Metric '{metricKey}' mismatch in node '{findingPathAsNodeId}'. Expected '{m.Value}', got '{value}'.");
                }

            }
        }
    }
}
