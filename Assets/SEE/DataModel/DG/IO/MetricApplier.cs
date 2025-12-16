using SEE.DataModel.DG.GraphIndex;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Translates parsed findings into node metrics on a <see cref="Graph"/> instance.
    /// Preconditions: <see cref="ApplyMetrics"/> must be called with a non-null graph, schema, and parsing configuration.
    /// </summary>
    internal class MetricApplier
    {
        /// <summary>
        /// Cache for the metric key prefix (for example, <c>Metric.JaCoCo.</c>) used during one application run.
        /// Preconditions: Must be initialized before metrics are applied via <see cref="ApplyMetrics"/>.
        /// </summary>
        private static string prefix;

        /// <summary>
        /// Adds all metrics from <paramref name="schema"/> to the provided <paramref name="graph"/>
        /// using the lookup helpers defined in <paramref name="parsingConfig"/>.
        /// Preconditions: <paramref name="graph"/> and <paramref name="parsingConfig"/> must not be null.
        /// </summary>
        /// <param name="graph">Target graph that receives the parsed metrics.</param>
        /// <param name="schema">Parsed findings including node identifiers and metric dictionaries.</param>
        /// <param name="parsingConfig">Configuration that knows how to normalize node identifiers.</param>
        public static void ApplyMetrics(Graph graph, MetricSchema schema, ParsingConfig parsingConfig)
        {
            if (graph == null)
            {
                Debug.LogError("[MetricApplier] Graph is null – cannot apply metrics.");
                return;
            }

            if (parsingConfig == null)
            {
                Debug.LogError("[MetricApplier] Parsing configuration is null – cannot apply metrics.");
                return;
            }

            if (schema == null || schema.Findings == null)
            {
                Debug.LogError("[MetricApplier] Schema or findings is null – nothing to apply.");
                return;
            }

            prefix = Metrics.Prefix + parsingConfig.ToolId + ".";
            IIndexNodeStrategy indexNodeStrategy = parsingConfig.CreateIndexNodeStrategy();
            SourceRangeIndex index = new(graph, indexNodeStrategy.NodeIdToMainType);

            foreach (Finding finding in schema.Findings)
            {
                if (finding == null)
                {
                    continue;
                }

                string findingPathAsMainType = indexNodeStrategy.FindingPathToMainType(finding.FullPath, finding.FileName);
                string findingPathAsNodeId = indexNodeStrategy.FindingPathToNodeId(finding.FullPath);

                if (string.IsNullOrWhiteSpace(findingPathAsMainType))
                {
                    Debug.LogWarning(
                        $"[MetricApplier] Encountered null nodeId for finding with path: {finding.FullPath} {finding.FileName} – skipping.");
                    continue;
                }

                int startLine = finding.Location?.StartLine ?? -1;
                if (startLine != -1 && index.TryGetValue(findingPathAsMainType, startLine, out Node node))
                {
                    foreach (KeyValuePair<string, string> metric in finding.Metrics)
                    {
                        SetMetric(node, metric);
                    }
                }
                else if (startLine == -1 && graph.TryGetNode(findingPathAsNodeId, out Node classOrPackageNode))
                {
                    foreach (KeyValuePair<string, string> metric in finding.Metrics)
                    {
                        SetMetric(classOrPackageNode, metric);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[MetricApplier] Could not resolve node for Path={finding.FullPath}, nodeId={findingPathAsNodeId}, line={startLine}");
                }
            }
        }

        /// <summary>
        /// Writes a single metric value to the node, trying int, float, and string conversions.
        /// Preconditions: <paramref name="node"/> must not be null.
        /// </summary>
        /// <param name="node">Graph node that should receive the metric.</param>
        /// <param name="metric">Key/value pair taken from the parsed finding.</param>
        private static void SetMetric(Node node, KeyValuePair<string, string> metric)
        {
            string key = prefix + metric.Key;

            if (node.TryGetAny(key, out object existingValue))
            {
                key = computeKey(node, key);
            }

            string stringValue = metric.Value;

            if (int.TryParse(
                    stringValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int intValue))
            {
                node.SetInt(key, intValue);
            }
            else if (float.TryParse(
                         stringValue,
                         NumberStyles.Float | NumberStyles.AllowThousands,
                         CultureInfo.InvariantCulture,
                         out float floatValue))
            {
                node.SetFloat(key, floatValue);
            }
            else
            {
                node.SetString(key, stringValue);
            }
        }

        /// <summary>
        /// Computes a new key with a suffix based on the amount of keys with a specific base key.
        /// if key "example" exist -> generated key is "example [1]"
        /// /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string computeKey(Node node, string key)
        {
            int i = 1;
            while (node.TryGetAny(key + " [" + i.ToString() + "]", out object value)) {
                i++;
            }
            return key + " [" + i.ToString() +"]";
        }
    }
}
