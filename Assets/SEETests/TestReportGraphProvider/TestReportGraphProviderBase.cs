using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assets.SEE.DataModel.DG.IO;
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
    /// and a set of expected findings), while this base class provides reusable
    /// arrange/act/assert logic and helpful diagnostics for failures.
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
    public abstract class TestReportGraphProviderBase
    {
        // ---- Template methods (must be implemented by derived classes) ----

        /// <summary>
        /// Returns the report path relative to <see cref="Application.streamingAssetsPath"/>.
        /// Leading slashes/backslashes are ignored.
        /// </summary>
        protected abstract string GetRelativeReportPath();

        /// <summary>
        /// Returns the set of node identifiers/contexts the test should consider during counting.
        /// </summary>
        protected abstract string[] GetNodesToParse();

        /// <summary>
        /// Returns the parsing configuration used to instantiate the corresponding parser.
        /// Must not return null; <see cref="SetUp"/> and the tests assert this.
        /// </summary>
        protected abstract ParsingConfig GetParsingConfig();

        /// <summary>
        /// Returns a node counter that can compute expected counts per context
        /// for the given report and node filter.
        /// </summary>
        protected abstract ICountReportNodes GetNodeCounter();

        /// <summary>
        /// Returns a set of hand-picked findings (keyed by <see cref="Finding.FullPath"/>)
        /// that should be present in the parsed schema, including expected metadata.
        /// </summary>
        protected abstract Dictionary<string, Finding> GetTestFindings();

        // ---- Shared state for a single test run ----
        private MetricSchema metricSchema;   // Parsed results under test
        private string fullReportPath;       // Absolute path to the report file

        /// <summary>
        /// The name of the hierarchical edge type we use for emitting the parent-child
        /// relation among nodes.
        /// </summary>
        private const string hierarchicalEdgeType = "Enclosing";

        /// <summary>
        /// Load Graph from GXL file <paramref name="path"/>.
        /// </summary>
        /// <param name="path">data path of GXL file</param>
        /// <returns>loaded graph</returns>
        private static async UniTask<Graph> LoadGraphAsync(DataPath path)
        {
            return await GraphReader.LoadAsync(path, new HashSet<string> { hierarchicalEdgeType }, basePath: "");
        }

        /// <summary>
        /// The graph that was loaded by <see cref="SetUpAsync"/> before each test case is executed.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// Resolves the absolute path to the test report before each test executes.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Resolve the absolute report path once, based on the relative path from the subclass.
            string relativeReportPath = GetRelativeReportPath();
            fullReportPath = Path.Combine(
                Application.streamingAssetsPath,
                relativeReportPath.TrimStart('/', '\\')
            );
        }

        /// <summary>
        /// Clears cached state between tests to avoid cross-test pollution.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            // Avoid leaking state across tests.
            metricSchema = null;
        }

        /// <summary>
        /// Verifies that the number of parsed nodes matches the expected per-context counts
        /// as well as the expected total across all contexts.
        /// </summary>
        [Test]
        public async Task TestNodeCount_ShouldMatchExpectedCountsAsync()
        {
            // Arrange
            metricSchema = await BuildMetricSchemaAsync();
            Assert.NotNull(metricSchema, "MetricSchema is null");

            ICountReportNodes nodeCounter = GetNodeCounter();
            Assert.NotNull(nodeCounter, "NodeCounter is null");

            // Expected counts per context, computed by the provided counter
            Dictionary<string, int> expectedNodeCounts =
                nodeCounter.Count(GetRelativeReportPath(), GetNodesToParse());

            int expectedTotalNodes = expectedNodeCounts.Values.Sum();

            // Act & Assert
            AssertNodeCountsMatch(expectedNodeCounts);

            Assert.AreEqual(
                expectedTotalNodes,
                metricSchema.findings.Count,
                $"Total node count mismatch. Expected: {expectedTotalNodes}, Actual: {metricSchema.findings.Count}"
            );
        }

        /// <summary>
        /// Parses the configured report file into a <see cref="MetricSchema"/>.
        /// 
        /// Validates that:
        /// - The parsing config is provided.
        /// - The config can create a parser instance.
        /// </summary>
        private async UniTask<MetricSchema> BuildMetricSchemaAsync()
        {
            DataPath reportDataPath = new(fullReportPath);

            ParsingConfig config = GetParsingConfig();
            Assert.NotNull(config, "GetParsingConfig() returned null");

            IReportParser parser = config.CreateParser();
            Assert.NotNull(parser, "CreateParser() returned null (IReportParser)");

            return await parser.ParseAsync(reportDataPath);
        }

        /// <summary>
        /// Asserts that actual counts (grouped by <see cref="Finding.Context"/>) match the
        /// expected per-context counts exactly, and that there are no unexpected contexts.
        /// Matching is case-insensitive on context keys.
        /// </summary>
        private void AssertNodeCountsMatch(Dictionary<string, int> expectedNodeCounts)
        {
            Assert.IsNotNull(metricSchema, "metricSchema has not been initialized.");
            Assert.IsNotNull(metricSchema.findings, "metricSchema.findings is null.");

            // Group actual findings by Context (case-insensitive) and count them.
            var actualNodeCounts = metricSchema.findings
                .Where(f => !string.IsNullOrEmpty(f.Context))
                .GroupBy(f => f.Context, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(),
                    StringComparer.OrdinalIgnoreCase
                );

            // 1) Every expected context must appear with the expected count
            foreach (var expectedEntry in expectedNodeCounts)
            {
                string XmlTagAsContext = GetParsingConfig().XPathMapping.MapContext[expectedEntry.Key];
                int actualCount = actualNodeCounts.TryGetValue(XmlTagAsContext , out int count) ? count : 0;

                Assert.AreEqual(
                    expectedEntry.Value,
                    actualCount,
                    $"Context '{expectedEntry.Key}': expected {expectedEntry.Value}, got {actualCount}"
                );
            }

        }

        /// <summary>
        /// Verifies that a curated set of expected findings exists and matches on:
        /// - Context (if provided),
        /// - Location (line/column spans if provided),
        /// - Metric dictionary (exact keys and values if provided).
        /// </summary>
        [Test]
        public async Task TestSpecificFindingsAsync()
        {
            metricSchema = await BuildMetricSchemaAsync();

            foreach (var finding in metricSchema.findings)
            {
                Debug.LogWarning($"FullPath of Finding: {finding.FullPath}");
            }
            Dictionary<string,Finding> testFindings = GetTestFindings();

            foreach (KeyValuePair<string,Finding> expected in testFindings)
            {
                Finding actual = metricSchema.findings.FirstOrDefault(f => f.FullPath == expected.Key);
                Assert.NotNull(actual, $"Finding '{expected.Key}' not found");
                AssertFindingMatch(actual, expected.Value);
            }
        }

        /// <summary>
        /// Asserts that two findings are equivalent for the properties that the expected
        /// instance specifies (context, location members, metrics).
        /// 
        /// The comparison is opt-in per field: if an expected sub-value is null or missing,
        /// it is not asserted.
        /// </summary>
        private void AssertFindingMatch(Finding actual, Finding expected)
        {
            Assert.IsNotNull(actual);
            Assert.IsNotNull(expected);

            // Context (optional)
            if (!string.IsNullOrEmpty(expected.Context))
            {
                Assert.That(actual.Context, Is.EqualTo(expected.Context), "Context");
            }

            // Location (optional, individual fields are optional too)
            MetricLocation expectedLocation = expected.Location;
            MetricLocation actualLocation = actual.Location;

            if (expectedLocation is not null)
            {
                if (expectedLocation.StartLine.HasValue)
                    Assert.That(actualLocation?.StartLine, Is.EqualTo(expectedLocation.StartLine), "StartLine");

                if (expectedLocation.EndLine.HasValue)
                    Assert.That(actualLocation?.EndLine, Is.EqualTo(expectedLocation.EndLine), "EndLine");

                if (expectedLocation.StartColumn.HasValue)
                    Assert.That(actualLocation?.StartColumn, Is.EqualTo(expectedLocation.StartColumn), "StartColumn");

                if (expectedLocation.EndColumn.HasValue)
                    Assert.That(actualLocation?.EndColumn, Is.EqualTo(expectedLocation.EndColumn), "EndColumn");
            }

            // Metrics (optional)
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
        private void AssertFindingMetricsMatch(Finding actual, Dictionary<string, string> expectedMetrics)
        {
            Assert.NotNull(actual, "Finding is null");

            Dictionary<string, string> metrics = actual.Metrics;

            Assert.NotNull(actual.Metrics, $"Metrics are null for node {actual.FullPath}");

            expectedMetrics ??= new Dictionary<string, string>();

            if (expectedMetrics.Count == 0)
            {
                Assert.IsEmpty(
                    actual.Metrics,
                    $"Expected no metrics but found {actual.Metrics.Count} for {actual.FullPath}"
                );
                return;
            }

            // 1) Every expected metric must be present and match its value
            foreach (KeyValuePair<string, string> testMetric in expectedMetrics)
            {
                Assert.IsTrue(
                    metrics.TryGetValue(testMetric.Key, out string actualValue),
                    $"Missing metric '{testMetric.Key}'. Expected '{testMetric.Value}'."
                );

                Assert.AreEqual(
                    testMetric.Value,
                    actualValue,
                    $"Metric '{testMetric.Key}' mismatch. Expected '{testMetric.Value}', got '{actualValue}'."
                );
            }

            // 2) No unexpected metrics allowed
            var unexpected = metrics.Keys.Except(expectedMetrics.Keys).ToList();
            Assert.IsTrue(
                unexpected.Count == 0,
                $"Unexpected metric(s) present: {string.Join(", ", unexpected)}"
            );
        }

        /// <summary>
        /// Verifies that a curated set of expected findings exists and matches on:
        /// - Context (if provided),
        /// - Location (line/column spans if provided),
        /// - Metric dictionary (exact keys and values if provided).
        /// </summary>
        [Test]
        public async Task TestMetricAppliedToGraphAsync()
        {
            // Parse report → MetricSchema
            metricSchema = await BuildMetricSchemaAsync();
            Dictionary<string, Finding> expectedFindings = GetTestFindings();

            // Load graph
            DataPath gxlPath = new(Application.streamingAssetsPath + "/JLGExample/CodeFacts.gxl.xz");
            graph = await LoadGraphAsync(gxlPath);

            IIndexNodeStrategy indexNodeStrategy = GetParsingConfig().CreateIndexNodeStrategy();

            // Index required for method-level lookups
            SourceRangeIndex index = new(graph, indexNodeStrategy.NodeIdToMainType);

            // Apply metrics to graph nodes
            MetricApplier.ApplyMetrics(graph, metricSchema, GetParsingConfig());

            string Prefix = Metrics.Prefix + GetParsingConfig().ToolId + ".";

            // Now verify each expected finding
            foreach (var kv in expectedFindings)
            {
                string fullPath = kv.Key;
                Finding expected = kv.Value;

                // Normalize node id
                string findingPathAsMainType = indexNodeStrategy.FindingPathToMainType(fullPath, expected.FileName);

                string findingPathAsNodeId = indexNodeStrategy.FindingPathToNodeId(fullPath);

                // Resolve node depending on context
                Node node = null;


                if (expected.Context.Equals("root")) 
                {
                    node = graph.GetRoots()[0];

                }
                else if (expected.Context.Equals("method", StringComparison.OrdinalIgnoreCase))
                {
                    int startLine = expected.Location?.StartLine ?? -1;
                    index.TryGetValue(findingPathAsMainType, startLine, out node);
                }
                else
                {
                    graph.TryGetNode(findingPathAsNodeId, out node);
                }

                Assert.NotNull(node, $"Node '{findingPathAsNodeId}' with FullPath {fullPath} with FindingPathAsMainType: {findingPathAsMainType} not found in graph.");


                // 1) Expected metrics must all exist and match
                foreach (var m in expected.Metrics)
                {
                    string metricKey = Prefix + m.Key;
                    int value = node.GetInt(metricKey);

                    Assert.NotNull(value,
                        $"Node '{findingPathAsNodeId}' is missing expected metric '{metricKey}'"
                    );

                    Assert.AreEqual(
                        m.Value, value.ToString(),
                        $"Metric '{m.Key}' mismatch in node '{findingPathAsNodeId}'. Expected '{m.Value}', got '{value}'."
                    );
                }
               
            }
        }

    }
}

