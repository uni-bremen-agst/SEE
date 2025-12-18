using System;
using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG.GraphIndex;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Translates parsed findings into node metrics on a <see cref="Graph"/> instance.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// <list type="bullet">
    /// <item><description><see cref="ApplyMetrics"/> must be called with non-null arguments.</description></item>
    /// <item><description>The supplied <see cref="ParsingConfig"/> must be compatible with the schema's findings.</description></item>
    /// </list>
    /// </remarks>
    internal static class MetricApplier
    {
        /// <summary>
        /// Adds all metrics from <paramref name="schema"/> to the provided <paramref name="graph"/>
        /// using the lookup helpers defined in <paramref name="parsingConfig"/>.
        /// </summary>
        /// <param name="graph">Target graph that receives the parsed metrics. Must not be null.</param>
        /// <param name="schema">Parsed findings including node identifiers and metric dictionaries. Must not be null.</param>
        /// <param name="parsingConfig">Configuration that knows how to normalize node identifiers. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
        public static void ApplyMetrics(Graph graph, MetricSchema schema, ParsingConfig parsingConfig)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph),
                                               $"[{nameof(MetricApplier)}] Graph is null – cannot apply metrics.");
            }

            if (parsingConfig == null)
            {
                throw new ArgumentNullException(nameof(parsingConfig),
                                               $"[{nameof(MetricApplier)}] Parsing configuration is null – cannot apply metrics.");
            }

            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema),
                                               $"[{nameof(MetricApplier)}] Schema is null – cannot apply metrics.");
            }

            if (schema.Findings == null)
            {
                throw new ArgumentNullException(nameof(schema.Findings),
                                               $"[{nameof(MetricApplier)}] Schema findings is null – cannot apply metrics.");
            }

            string prefix = Metrics.Prefix + parsingConfig.ToolId + ".";
            IIndexNodeStrategy indexNodeStrategy = parsingConfig.CreateIndexNodeStrategy();
            SourceRangeIndex index = new (graph, indexNodeStrategy.NodeIdToMainType);

            foreach (Finding finding in schema.Findings)
            {
                if (finding == null)
                {
                    continue;
                }

                string findingPathAsMainType =
                    indexNodeStrategy.FindingPathToMainType(finding.FullPath, finding.FileName);

                string findingPathAsNodeId = indexNodeStrategy.FindingPathToNodeId(finding.FullPath);

                if (string.IsNullOrWhiteSpace(findingPathAsMainType))
                {
                    Debug.LogWarning(
                        $"[{nameof(MetricApplier)}] Could not resolve main type for finding with path: {finding.FullPath} {finding.FileName} – skipping.\n");
                    continue;
                }

                int startLine = finding.Location?.StartLine ?? -1;

                if (startLine != -1 && index.TryGetValue(findingPathAsMainType, startLine, out Node node))
                {
                    foreach (KeyValuePair<string, string> metric in finding.Metrics)
                    {
                        SetMetric(node, metric, prefix);
                    }
                }
                else if (startLine == -1 && graph.TryGetNode(findingPathAsNodeId, out Node classOrPackageNode))
                {
                    foreach (KeyValuePair<string, string> metric in finding.Metrics)
                    {
                        SetMetric(classOrPackageNode, metric, prefix);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[{nameof(MetricApplier)}] Could not resolve node for Path={finding.FullPath}, nodeId={findingPathAsNodeId}, line={startLine}.\n");
                }
            }
        }

        /// <summary>
        /// Writes a single metric value to the node, trying int, float, and string conversions.
        /// </summary>
        /// <param name="node">Graph node that should receive the metric. Must not be null.</param>
        /// <param name="metric">Key/value pair taken from the parsed finding.</param>
        /// <param name="prefix">Prefix to prepend to <paramref name="metric"/> keys (including tool id and separator).</param>
        private static void SetMetric(Node node, KeyValuePair<string, string> metric, string prefix)
        {
            string key = prefix + metric.Key;

            if (node.TryGetAny(key, out _))
            {
                key = ComputeKey(node, key);
            }

            string stringValue = metric.Value;

            if (int.TryParse(stringValue,
                             NumberStyles.Integer,
                             CultureInfo.InvariantCulture,
                             out int intValue))
            {
                node.SetInt(key, intValue);
            }
            else if (float.TryParse(stringValue,
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
        /// Computes a unique key by appending a numeric suffix if the base key already exists on <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node whose current keys should be inspected. Must not be null.</param>
        /// <param name="baseKey">Base key that should be made unique.</param>
        /// <returns>A unique key derived from <paramref name="baseKey"/>.</returns>
        private static string ComputeKey(Node node, string baseKey)
        {
            int i = 1;

            while (node.TryGetAny(baseKey + " [" + i + "]", out _))
            {
                i++;
            }

            return baseKey + " [" + i + "]";
        }
    }
}
