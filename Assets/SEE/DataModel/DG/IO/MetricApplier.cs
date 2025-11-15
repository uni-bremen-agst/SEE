using Assets.SEE.DataModel.DG.IO;
using SEE.DataModel.DG.GraphIndex;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Translates parsed findings into node metrics on a <see cref="Graph"/> instance.
    /// </summary>
    class MetricApplier
    {
        /// <summary>
        /// Cache for the metric key prefix (e.g., <c>Metric.JaCoCo.</c>) used during one application run.
        /// </summary>
        static string Prefix;

        /// <summary>
        /// Context of the root node
        /// </summary>
        private static string RootContext = "root";// <summary>

        /// <summary>
        /// Adds all metrics from <paramref name="schema"/> to the provided <paramref name="graph"/>
        /// using the lookup helpers defined in <paramref name="parsingConfig"/>.
        /// </summary>
        /// <param name="graph">target graph that receives the parsed metrics</param>
        /// <param name="schema">parsed findings including node identifiers and metric dictionaries</param>
        /// <param name="parsingConfig">config that knows how to normalize node identifiers</param>
        public static void ApplyMetrics(Graph graph, MetricSchema schema, ParsingConfig parsingConfig)
        {
            if (graph == null)
            {
                Debug.LogError("[MetricApplier] Graph is null – cannot apply metrics.");
                return;
            }

            if (schema == null || schema.findings == null)
            {
                Debug.LogError("[MetricApplier] Schema or findings is null – nothing to apply.");
                return;
            }

            Prefix = Metrics.Prefix + parsingConfig.ToolId + ".";
            IIndexNodeStrategy indexNodeStrategy = parsingConfig.CreateIndexNodeStrategy();
            SourceRangeIndex index = new(graph, indexNodeStrategy.NodeIdToMainType);

            foreach (Finding finding in schema.findings)
            {
                if (finding == null)
                {
                    Debug.LogWarning("[MetricApplier] Encountered null finding – skipping.");
                    continue;
                }
                // adding metrics to the root node
                if(finding.Context.Equals(RootContext))
                {
                    Debug.Log($"[MetricApplier] Applying metrics to ROOT node {finding.FullPath}");

                    foreach (var metric in finding.Metrics)
                    {
                        SetMetric(graph.GetRoots()[0] , metric);
                    }
                    continue;
                }

                string findingPathAsMainType = indexNodeStrategy.FindingPathToMainType(finding.FullPath, finding.FileName);
                string findingPathAsNodeId = indexNodeStrategy.FindingPathToNodeId(finding.FullPath);

                if (string.IsNullOrWhiteSpace(findingPathAsMainType))
                {
                    Debug.LogWarning($"[MetricApplier] Encountered null nodeId for finding with path: {finding.FullPath} {finding.FileName}  – skipping.");
                    continue;
                }

                int startLine = finding.Location?.StartLine ?? -1;
                if (startLine != -1 && index.TryGetValue(findingPathAsMainType, startLine, out Node node))
                {
                    Debug.Log($"[MetricApplier] Applying metrics to METHOD node {node.ID} at line {startLine}");
                    foreach (var metric in finding.Metrics)
                    {
                        SetMetric(node, metric);
                    }
                }
                else if (startLine == -1 && graph.TryGetNode(findingPathAsNodeId, out Node classOrPackageNode))
                {
                    Debug.Log($"[MetricApplier] Applying metrics to CLASS/PACKAGE node {classOrPackageNode.ID}");
                    foreach (var metric in finding.Metrics)
                    {
                        SetMetric(classOrPackageNode, metric);
                    }
                }
                else
                {
                    Debug.LogWarning($"[MetricApplier] Could not resolve node for Path={finding.FullPath}, nodeId={findingPathAsNodeId}, line={startLine}");
                }
            }
        }

        /// <summary>
        /// Writes a single metric value to the node, trying int, float, and string conversions.
        /// </summary>
        /// <param name="node">graph node that should receive the metric</param>
        /// <param name="metric">key/value pair taken from the parsed finding</param>
        private static void SetMetric(Node node, KeyValuePair<string, string> metric)
        {
            string key = Prefix + metric.Key;
            string stringValue = metric.Value;

            if (int.TryParse(stringValue, NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out int intValue))
            {
                node.SetInt(key, intValue);
            }
            else if (float.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands,
                                    CultureInfo.InvariantCulture, out float floatValue))
            {
                node.SetFloat(key, floatValue);
            }
            else
            {
                node.SetString(key, stringValue);
            }
        }


    }
}
