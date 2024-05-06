﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
using SEE.Tools;
using UnityEngine;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json.Linq;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Imports node metrics from CSV files into the graph.
    /// </summary>
    public class MetricImporter : MetricsIO
    {
        /// <summary>
        /// Loads metrics and issues from the Axivion dashboard and imports them to the graph.
        /// Issues are also aggregated along the node decomposition tree as a sum
        /// using the <see cref="MetricAggregator"/>.
        /// </summary>
        /// <param name="graph">The graph whose nodes' metrics shall be set</param>
        /// <param name="override">Whether any existing metrics present in the graph's nodes shall be updated</param>
        /// <param name="addedFrom">If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.</param>
        /// <returns>The graph with the updated metrics and issues</returns>
        public static async UniTask<Graph> LoadDashboardAsync(Graph graph, bool @override = true, string addedFrom = "")
        {
            IDictionary<(string path, string entity), List<MetricValueTableRow>> metrics = await DashboardRetriever.Instance.GetAllMetricRowsAsync();
            IDictionary<string, List<Issue>> issues = await LoadIssueMetrics(string.IsNullOrWhiteSpace(addedFrom) ? null : addedFrom);
            string projectFolder = DataPath.ProjectFolder();

            await UniTask.SwitchToThreadPool();

            HashSet<Node> encounteredIssueNodes = new();
            int updatedMetrics = 0;
            // Go through all nodes, checking whether any metric in the dashboard matches it.
            foreach (Node node in graph.Nodes())
            {
                string nodePath = $"{node.RelativeDirectory(projectFolder)}{node.Filename ?? string.Empty}";
                if (metrics.TryGetValue((nodePath, node.SourceName), out List<MetricValueTableRow> metricValues))
                {
                    foreach (MetricValueTableRow metricValue in metricValues)
                    {
                        // Only set if value doesn't already exist, or if we're supposed to override and the value differs
                        if (!node.TryGetFloat(metricValue.Metric, out float value) || @override && !Mathf.Approximately(metricValue.Value, value))
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
                                node.SetFloat(issue.AttributeName.Name(), value+1);
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
            Debug.Log($"Updated {updatedMetrics} metric values and {encounteredIssueNodes.Count} issues "
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

        /// <summary>
        /// Loads node metric values from given CSV file with given separator.
        /// The file must contain a header with the column names. The first column
        /// name must be the Node.ID. Values must be either integers or
        /// floats. All numerics will be added as float attributes to the node.
        /// Floats must use . to separate the digits. The ID is used to
        /// identify a node.
        ///
        /// The following errors may occur:
        /// ) The file cannot be read => default Exception
        /// ) The file is empty => IOException
        /// ) The first row does not contain the ID attribute in its first column => IOException
        /// ) There is a row that has either too many or to few entries (the length of header and data rows do not match)
        /// ) A node with given ID does not exist in the graph
        /// ) The data entry in a column cannot be parsed as float
        ///
        /// In the latter three situations, an error message is emitted and the error counter
        /// in increased.
        /// </summary>
        /// <param name="graph">graph for which node metrics are to be imported</param>
        /// <param name="filename">CSV file from which to import node metrics</param>
        /// <param name="separator">used to separate column entries</param>
        /// <returns>the number of errors</returns>
        public static int LoadCsv(Graph graph, string filename, char separator = ';')
        {
            if (!File.Exists(filename))
            {
                Debug.LogWarning($"Metric file {filename} does not exist. CSV Metrics will not be available.\n");
                return 0;
            }

            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                Delimiter = separator.ToString(),
            };

            using StreamReader reader = new(filename);
            using CsvReader csv = new(reader, config);

            int numberOfErrors = 0;
            int lineCount = 1;

            csv.Read();
            if (csv.ReadHeader())
            {
                string[] header = csv.HeaderRecord;
                if (header.Length == 0)
                {
                    throw new IOException($"Header must not be empty. It must include at least column {IDColumnName}.\n");
                }
                if (header[0] != IDColumnName)
                {
                    throw new IOException($"First header column in file {filename} is not {IDColumnName}.");
                }

                string[] columns = header[1..];
                if (columns.Length == 0)
                {
                    Debug.LogWarning($"There are no data columns in {filename}.\n");
                    return 0;
                }
                while (csv.Read())
                {
                    lineCount++;
                    string id = csv.GetField<string>(IDColumnName);

                    if (graph.TryGetNode(id, out Node node))
                    {
                        // Process the remaining data columns of this row starting at index 1
                        foreach (string column in columns)
                        {
                            string entry = csv.GetField<string>(column);

                            try
                            {
                                if (entry.Contains("."))
                                {
                                    node.SetFloat(column, (float)float.Parse(entry, CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    node.SetInt(column, int.Parse(entry));
                                }
                            }
                            catch (CsvHelper.MissingFieldException)
                            {
                                Debug.LogError($"{SourceLocation()} Missing value.\n");
                                numberOfErrors++;
                            }
                            catch (FormatException)
                            {
                                Debug.LogError($"{SourceLocation()} Value {entry} does not represent a number in a valid format.\n");
                                numberOfErrors++;
                            }
                            catch (OverflowException)
                            {
                                Debug.LogError($"{SourceLocation()} Value {entry} represents a number less than minimum or greater than maximum.\n");
                                numberOfErrors++;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{SourceLocation()} Unknown node id '{id}'.\n");
                        numberOfErrors++;
                    }
                }
            }
            else
            {
                string errorMessage = "There is no header.";
                Debug.LogError(errorMessage + "\n");
                throw new IOException(errorMessage);

            }

            return numberOfErrors;

            string SourceLocation()
            {
                return $"{filename}:{lineCount}: ";
            }
        }
    }
}
