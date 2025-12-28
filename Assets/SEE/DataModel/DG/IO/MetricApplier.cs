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

            // ---------------------------------------------------------
            // 1. Build Indices
            // ---------------------------------------------------------

            // Range Index for exact line lookups (e.g., finding methods or statements).
            // We pass the strategy to ensure the index works with "Clean IDs".
            SourceRangeIndex rangeIndex = new (graph, indexNodeStrategy.ToLogicalIdentifier);

            // Fallback index:
            // Maps logical container identifiers to graph nodes.
            //
            // Only nodes that are uniquely identifiable by their logical identifier
            // (e.g., classes, files, packages) are stored.
            // Method nodes are intentionally excluded because overloads make them ambiguous
            // without source-range information.
            Dictionary<string, Node> typeIndex = new Dictionary<string, Node>();

            foreach (Node node in graph.Nodes())
            {
                string logicalId = indexNodeStrategy.ToLogicalIdentifier(node);

                if (!string.IsNullOrEmpty(logicalId))
                {
                    if (NodeTypeExtensions.IsContainer(node.Type))
                    {
                        if (!typeIndex.ContainsKey(logicalId))
                        {
                            typeIndex[logicalId] = node;
                        }
                    }
                }
            }

            // ---------------------------------------------------------
            // 2. Process Findings
            // ---------------------------------------------------------
            int matchedCount = 0;

            foreach (Finding finding in schema.Findings)
            {
                if (finding == null)
                {
                    continue;
                }

                // Convert the finding path to the "Logical ID".
                // This strips extensions, matches the namespace structure, etc.
                string findingPathAsLogicalId =
                    indexNodeStrategy.ToLogicalIdentifier(finding.FullPath);

                if (string.IsNullOrWhiteSpace(findingPathAsLogicalId))
                {
                    Debug.LogWarning(
                        $"[{nameof(MetricApplier)}] Could not resolve main type for finding with path: {finding.FullPath} {finding.FileName} – skipping.\n");
                    continue;
                }

                int startLine = finding.Location?.StartLine ?? -1;
                Node targetNode = null;

                // Strategy 1: Try exact match via Range Index (e.g., finding a method at a specific line).
                if (startLine != -1)
                {
                    if (rangeIndex.TryGetValue(findingPathAsLogicalId, startLine, out Node hit))
                    {
                        targetNode = hit;
                    }
                }

                // Strategy 2: Container-level fallback lookup.
                //
                // This strategy applies if:
                // a) No start line was provided by the tool (e.g., class-level metrics).
                // b) A start line was provided, but no matching method node exists in the graph.
                // c) The start line does not fall within any known source range.
                //
                // IMPORTANT:
                // Method nodes are not stored in the fallback index because they are not uniquely
                // identifiable by name alone (e.g., method overloads).
                // Therefore, method nodes can only be resolved via the SourceRangeIndex.
                if (targetNode == null)
                {
                    string fullIdentifier = indexNodeStrategy.ToFullIdentifier(finding.FullPath);
                    if (!string.IsNullOrWhiteSpace(fullIdentifier) && typeIndex.TryGetValue(fullIdentifier, out Node fallbackNode))
                    {
                        targetNode = fallbackNode;
                    }
                }

                // Apply metrics if a node was found
                if (targetNode != null)
                {
                    matchedCount++;
                    foreach (KeyValuePair<string, string> metric in finding.Metrics)
                    {
                        SetMetric(targetNode, metric, prefix);
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"[{nameof(MetricApplier)}] Could not resolve node for Path={finding.FullPath}, MainType={findingPathAsLogicalId}, line={startLine}.\n");
                }
            }

            Debug.Log($"[{nameof(MetricApplier)}] Finished applying metrics. Matched {matchedCount} out of {schema.Findings.Count} findings.");
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
