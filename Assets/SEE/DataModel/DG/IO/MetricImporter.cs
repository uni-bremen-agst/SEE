using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Net.Dashboard;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
using SEE.Tools;
using Sirenix.Utilities;
using UnityEngine;

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
        public static async UniTask LoadDashboard(Graph graph, bool @override = true, string addedFrom = "")
        {
            IDictionary<(string path, string entity), List<MetricValueTableRow>> metrics = await DashboardRetriever.Instance.GetAllMetricRows();
            IDictionary<string, List<Issue>> issues = await LoadIssueMetrics(addedFrom.IsNullOrWhitespace() ? null : addedFrom, null);
            string projectFolder = DataPath.ProjectFolder();

            await UniTask.SwitchToThreadPool();

            HashSet<Node> encounteredIssueNodes = new HashSet<Node>();
            int updatedMetrics = 0;
            // Go through all nodes, checking whether any metric in the dashboard matches it.
            foreach (Node node in graph.Nodes())
            {
                string nodePath = $"{node.RelativePath(projectFolder)}{node.Filename() ?? string.Empty}";
                if (metrics.TryGetValue((nodePath, node.SourceName), out List<MetricValueTableRow> metricValues))
                {
                    foreach (MetricValueTableRow metricValue in metricValues)
                    {
                        // Only set if value doesn't already exist, or if we're supposed to override and the value differs
                        if (!node.TryGetFloat(metricValue.metric, out float value) || @override && !Mathf.Approximately(metricValue.value, value))
                        {
                            node.SetFloat(metricValue.metric, metricValue.value);
                            updatedMetrics++;
                        }
                    }
                }

                if (issues.TryGetValue(nodePath, out List<Issue> issueList))
                {
                    int? line = node.SourceLine();
                    IEnumerable<Issue> relevantIssues;
                    if (!line.HasValue)
                    {
                        // Relevant issues are those which are contained in this file, so all issues
                        relevantIssues = issueList;
                    }
                    else
                    {
                        int? length = node.SourceLength();
                        HashSet<int> lineRange = Enumerable.Range(line.Value, length ?? 1).ToHashSet();
                        // Relevant issues are those which are entirely contained by the source region of this node
                        relevantIssues = issueList.Where(
                            x => x.Entities.Any(e => lineRange.Contains(e.line) &&
                                                     (!e.endLine.HasValue || lineRange.Contains(e.endLine.Value))));
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
                NumericAttributeNames.Architecture_Violations, NumericAttributeNames.Dead_Code
            };
            //FIXME: Aggregation from lower levels to classes doesn't work due to issues spanning multiple lines
            // Maybe simply ignore aggregated value when a non-aggregated value is present (which it would be)
            MetricAggregator.AggregateSum(graph, issueNames.Select(x => x.Name()));

            await UniTask.SwitchToMainThread();
            Debug.Log($"Updated {updatedMetrics} metric values and {encounteredIssueNodes.Count} issues "
                      + "using the Axivion dashboard.\n");


            static async UniTask<IDictionary<string, List<Issue>>> LoadIssueMetrics(string start, string end = null)
            {
                IDictionary<string, List<Issue>> issues = new Dictionary<string, List<Issue>>();
                IList<Issue> allIssues = await DashboardRetriever.Instance.GetConfiguredIssues(start, end, Issue.IssueState.added);
                foreach (Issue issue in allIssues)
                {
                    foreach (SourceCodeEntity entity in issue.Entities)
                    {
                        if (!issues.ContainsKey(entity.path))
                        {
                            issues[entity.path] = new List<Issue>();
                        }
                        issues[entity.path].Add(issue);
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
            int numberOfErrors = 0;
            try
            {
                using StreamReader reader = new StreamReader(filename);
                if (reader.EndOfStream)
                {
                    Debug.LogError($"Empty file: {filename}.\n");
                }
                else
                {
                    // The line number in the CSV file currently processed.
                    int lineCount = 1;
                    // Header row
                    string headerLine = reader.ReadLine();
                    // The names of the columns
                    string[] columnNames = headerLine?.Split(separator);
                    lineCount++;
                    // We expect the ID plus at least one metric
                    if (columnNames?.Length > 1)
                    {
                        // The first column must be the ID
                        if (columnNames[0] != IDColumnName)
                        {
                            string errorMessage = $"First header column in file {filename} is not {IDColumnName}.";
                            Debug.LogError(errorMessage + "\n");
                            throw new IOException(errorMessage);
                        }
                        // Process each data row
                        while (!reader.EndOfStream)
                        {
                            // Currently processed data row
                            string line = reader.ReadLine();
                            // The values of the data row
                            string[] values = line.Split(separator);
                            // Number of named columns and data entries must correspond
                            if (columnNames.Length != values.Length)
                            {
                                Debug.LogError($"Unexpected number of entries in file {filename} at line {lineCount}.\n");
                                numberOfErrors++;
                            }
                            // ID is expected to be in the first column. Try to
                            // retrieve the corresponding node from the graph
                            if (graph.TryGetNode(values[0], out Node node))
                            {
                                // Process the remaining data columns of this row starting at index 1
                                for (int i = 1; i < Mathf.Min(columnNames.Length, values.Length); i++)
                                {
                                    try
                                    {
                                        if (values[i].Contains("."))
                                        {
                                            float value = float.Parse(values[i], CultureInfo.InvariantCulture);
                                            node.SetFloat(columnNames[i], value);
                                        }
                                        else
                                        {
                                            int value = int.Parse(values[i]);
                                            node.SetInt(columnNames[i], value);
                                        }
                                    }
                                    catch (ArgumentNullException)
                                    {
                                        Debug.LogError($"Missing value in file {filename} at line {lineCount}.\n");
                                        numberOfErrors++;
                                    }
                                    catch (FormatException)
                                    {
                                        Debug.LogError($"Value {values[i]} does not represent a number in a valid format in file {filename} at line {lineCount}.\n");
                                        numberOfErrors++;
                                    }
                                    catch (OverflowException)
                                    {
                                        Debug.LogError($"Value {values[i]} represents a number less than minimum or greater than maximum in file {filename} at line {lineCount}.\n");
                                        numberOfErrors++;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Unknown node {values[0]} in file {filename} at line {lineCount}.\n");
                                numberOfErrors++;
                            }
                            lineCount++;
                        }
                    }
                    else
                    {
                        string errorMessage = $"Not enough columns in file {filename}.";
                        Debug.LogError(errorMessage + "\n");
                        throw new IOException(errorMessage);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception {e.Message} while loading data from CSV file {filename}.\n");
                throw;
            }
            return numberOfErrors;
        }
    }
}
