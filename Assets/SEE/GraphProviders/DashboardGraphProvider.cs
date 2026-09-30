using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.Graphs;
using SEE.Graphs.IO.CSV;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Config;
using UnityEngine;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Metric;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Utils.Paths;
using Range = SEE.Graphs.Range;
using SEE.Utils;
using System.Linq;
using SEE.Tools;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Reads metrics from the Axivion Dashboard and adds these to a graph.
    /// </summary>
    [Serializable]
    public class DashboardGraphProvider: SingleGraphProvider
    {
        /// <summary>
        /// Whether metrics retrieved from the dashboard shall override existing metrics.
        /// </summary>
        [Tooltip("Whether metrics retrieved from the dashboard shall override existing metrics."),
            RuntimeTab(GraphProviderFoldoutGroup)]
        public bool OverrideMetrics = true;

        /// <summary>
        /// If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.
        /// </summary>
        [Tooltip("Version for which to retrieve issues. If empty, all issues are loaded."),
            RuntimeTab(GraphProviderFoldoutGroup)]
        public string IssuesAddedFromVersion = "";

        /// <summary>
        /// Loads the metrics available at the Axivion Dashboard into the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded.</param>
        /// <param name="city">This parameter is currently ignored.</param>
        /// <param name="changePercentage">This parameter is currently ignored.</param>
        /// <param name="token">This parameter is currently ignored.</param>
        public override async UniTask<Graph> ProvideAsync(Graph graph, AbstractSEECity city,
                                                          Action<float> changePercentage = null,
                                                          CancellationToken token = default)
        {
            string startVersion = string.IsNullOrEmpty(IssuesAddedFromVersion) ? null : IssuesAddedFromVersion;
            Debug.Log($"Loading metrics and added issues from the Axivion Dashboard for start version {startVersion}.\n");
            return await LoadDashboardAsync(graph, OverrideMetrics, startVersion, changePercentage, token);
        }

        public override SingleGraphProviderKind GetKind()
        {
            return SingleGraphProviderKind.Dashboard;
        }

        /// <summary>
        /// Loads metrics and issues from the Axivion dashboard and imports them to the graph.
        /// Issues are also aggregated along the node decomposition tree as a sum
        /// using the <see cref="MetricAggregator"/>.
        /// </summary>
        /// <param name="graph">The graph whose nodes' metrics shall be set.</param>
        /// <param name="override">Whether any existing metrics present in the graph's nodes shall be updated.</param>
        /// <param name="addedFrom">If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.</param>
        /// <param name="changePercentage">Used to report progress of the operation as a percentage.</param>
        /// <param name="token">Token to cancel the operation.</param>
        /// <returns>The graph with the updated metrics and issues.</returns>
        private static async UniTask<Graph> LoadDashboardAsync(Graph graph, bool @override = true,
                                                              string addedFrom = "",
                                                              Action<float> changePercentage = null,
                                                              CancellationToken token = default)
        {
            IDictionary<(string path, string entity), List<MetricValueTableRow>> metrics
                = await DashboardRetriever.Instance.GetAllMetricRowsAsync();
            IDictionary<string, List<Issue>> issues
                = await LoadIssueMetrics(string.IsNullOrWhiteSpace(addedFrom) ? null : addedFrom);
            string projectFolder = DataPath.ProjectFolder();

            await UniTask.SwitchToThreadPool();

            HashSet<Node> encounteredIssueNodes = new();
            int updatedMetrics = 0;
            IList<Node> nodes = graph.Nodes();
            float i = 0;
            // Go through all nodes, checking whether any metric in the dashboard matches it.
            await foreach (Node node in nodes.BatchPerFrame())
            {
                token.ThrowIfCancellationRequested();

                changePercentage?.Invoke(++i / nodes.Count);
                string nodePath = $"{node.RelativeDirectory(projectFolder)}{node.Filename ?? string.Empty}";
                if (metrics.TryGetValue((nodePath, node.SourceName), out List<MetricValueTableRow> metricValues))
                {
                    foreach (MetricValueTableRow metricValue in metricValues)
                    {
                        // Only set if value doesn't already exist, or if we're supposed to override and the value differs
                        if (!node.TryGetFloat(metricValue.Metric, out float value) || @override && !FloatUtils.Approximately(metricValue.Value, value))
                        {
                            node.SetFloat(metricValue.Metric, metricValue.Value);
                            updatedMetrics++;
                        }
                    }
                }

                if (issues.TryGetValue(nodePath, out List<Issue> issueList))
                {
                    int? line = node.SourceLine;
                    IEnumerable<Issue> relevantIssues;
                    if (!line.HasValue)
                    {
                        // Relevant issues are those which are contained in this file, so all issues
                        relevantIssues = issueList;
                    }
                    else
                    {
                        Range lineRange = node.SourceRange ?? new Range(line.Value, line.Value + 1);
                        // Relevant issues are those which are entirely contained by the source region of this node
                        relevantIssues = issueList.Where(
                            x => x.Entities.Any(e => lineRange.Contains(e.Line, 0) && (!e.EndLine.HasValue || lineRange.Contains(e.EndLine.Value, 0))));
                    }

                    foreach (Issue issue in relevantIssues)
                    {
                        if (node.TryGetFloat(issue.AttributeName.Name(), out float value))
                        {
                            if (!encounteredIssueNodes.Contains(node))
                            {
                                // If the value already exists and it was set from somewhere else,
                                // we override it if the caller wishes to do so.
                                if (@override)
                                {
                                    node.SetFloat(issue.AttributeName.Name(), 1);
                                    encounteredIssueNodes.Add(node);
                                }
                            }
                            else
                            {
                                // We found one more issue here, so we increment the value by one
                                node.SetFloat(issue.AttributeName.Name(), value + 1);
                            }
                        }
                        else
                        {
                            node.SetFloat(issue.AttributeName.Name(), 1);
                            encounteredIssueNodes.Add(node);
                        }
                    }
                }
            }

            // Aggregate metrics
            NumericAttributeNames[] issueNames =
            {
                NumericAttributeNames.Clone, NumericAttributeNames.Complexity, NumericAttributeNames.Cycle,
                NumericAttributeNames.Metric, NumericAttributeNames.Style,
                NumericAttributeNames.ArchitectureViolations, NumericAttributeNames.DeadCode
            };
            //FIXME: Aggregation from lower levels to classes doesn't work due to issues spanning multiple lines
            // Maybe simply ignore aggregated value when a non-aggregated value is present (which it would be)
            MetricAggregator.AggregateSum(graph, issueNames.Select(x => x.Name()));

            await UniTask.SwitchToMainThread();
            Logging.Logger.LogInfo($"Updated {updatedMetrics} metric values and {encounteredIssueNodes.Count} issues "
                      + "using the Axivion dashboard.\n");
            return graph;


            static async UniTask<IDictionary<string, List<Issue>>> LoadIssueMetrics(string start)
            {
                IDictionary<string, List<Issue>> issues = new Dictionary<string, List<Issue>>();
                IList<Issue> allIssues = await DashboardRetriever.Instance.GetConfiguredIssuesAsync(start, end: null, state: Issue.IssueState.added);
                foreach (Issue issue in allIssues)
                {
                    foreach (SourceCodeEntity entity in issue.Entities)
                    {
                        if (!issues.ContainsKey(entity.Path))
                        {
                            issues[entity.Path] = new List<Issue>();
                        }
                        issues[entity.Path].Add(issue);
                    }
                }

                return issues;
            }
        }

        #region Configuration file input/output

        /// <summary>
        /// Label of attribute <see cref="OverrideMetrics"/> in the configuration file.
        /// </summary>
        private const string overrideMetricsLabel = "OverrideMetrics";

        /// <summary>
        /// Label of attribute <see cref="IssuesAddedFromVersion"/> in the configuration file.
        /// </summary>
        private const string issuesAddedFromVersionLabel = "IssuesAddedFromVersion";

        protected override void SaveAttributes(ConfigWriter writer)
        {
            writer.Save(OverrideMetrics, overrideMetricsLabel);
            writer.Save(IssuesAddedFromVersion, issuesAddedFromVersionLabel);
        }

        protected override void RestoreAttributes(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, overrideMetricsLabel, ref OverrideMetrics);
            ConfigIO.Restore(attributes, issuesAddedFromVersionLabel, ref IssuesAddedFromVersion);
        }

        #endregion
    }
}
