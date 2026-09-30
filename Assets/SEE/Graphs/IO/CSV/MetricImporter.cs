using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using SEE.Utils;
using SEE.Utils.Paths;

namespace SEE.Graphs.IO.CSV
{
    /// <summary>
    /// Imports node metrics from CSV files into the graph.
    /// </summary>
    public class MetricImporter : MetricsIO
    {
        /// <summary>
        /// Analogous to <see cref="LoadCsv(Graph, string, char)"/> except that the
        /// data are read from the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="graph">Graph for which node metrics are to be imported.</param>
        /// <param name="path">Path to a data file containing CSV data from which to import node metrics.</param>
        /// <param name="separator">Used to separate column entries.</param>
        /// <param name="token">The token to cancel the loading.</param>
        /// <returns>The number of errors that occurred.</returns>
        /// <returns>The number of errors.</returns>
        public static async UniTask<int> LoadCsvAsync(Graph graph, DataPath path, char separator = ';',
                                                      CancellationToken token = default)
        {
            Stream stream = await path.LoadAsync();
            using StreamReader reader = new(stream);
            return await LoadCsvAsync(graph, separator, reader, path.Path, token);
        }

        /// <summary>
        /// Does the actual CSV import.
        /// </summary>
        /// <param name="graph">Graph for which node metrics are to be imported.</param>
        /// <param name="separator">Used to separate column entries.</param>
        /// <param name="reader">A reader yielding the CSV data.</param>
        /// <param name="filename">The name of the CSV; will be used only for
        /// error messages; can be empty.</param>
        /// <param name="token">The token to cancel the loading.</param>
        /// <returns>The number of errors that occurred.</returns>
        /// <exception cref="IOException">If the file is malformed, i.e., does not conform
        /// to the expected CSV format.</exception>
        private static async UniTask<int> LoadCsvAsync(Graph graph, char separator, StreamReader reader,
                                                       string filename, CancellationToken token)
        {
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                Delimiter = separator.ToString(),
            };
            CsvReader csv = new(reader, config);
            int numberOfErrors = 0;
            int lineCount = 1;

            await csv.ReadAsync();
            token.ThrowIfCancellationRequested();

            if (csv.ReadHeader())
            {
                string[] header = csv.HeaderRecord;
                if (header.Length == 0)
                {
                    throw new IOException($"Header must not be empty. It must include at least column {IDColumnName}.\n");
                }
                if (header[0] != IDColumnName)
                {
                    throw new IOException($"First header column in {Input()} is not {IDColumnName}.");
                }

                string[] columns = header[1..];
                if (columns.Length == 0)
                {
                    Logging.Logger.LogWarning($"There are no data columns in {Input()}.\n");
                    return 0;
                }
                while (await csv.ReadAsync())
                {
                    token.ThrowIfCancellationRequested();

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
                                    node.SetFloat(column, float.Parse(entry, CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    node.SetInt(column, int.Parse(entry));
                                }
                            }
                            catch (CsvHelper.MissingFieldException)
                            {
                                Logging.Logger.LogError($"{SourceLocation()} Missing value.\n");
                                numberOfErrors++;
                            }
                            catch (FormatException)
                            {
                                Logging.Logger.LogError($"{SourceLocation()} Value {entry} does not represent a number in a valid format.\n");
                                numberOfErrors++;
                            }
                            catch (OverflowException)
                            {
                                Logging.Logger.LogError($"{SourceLocation()} Value {entry} represents a number less than minimum or greater than maximum.\n");
                                numberOfErrors++;
                            }
                        }
                    }
                    else
                    {
                        Logging.Logger.LogWarning($"{SourceLocation()} Unknown node id '{id}'.\n");
                        numberOfErrors++;
                    }
                }
            }
            else
            {
                const string errorMessage = "There is no header.";
                Logging.Logger.LogError(errorMessage + "\n");
                throw new IOException(errorMessage);

            }

            return numberOfErrors;

            string Input()
            {
                return string.IsNullOrWhiteSpace(filename) ? "CSV data" : filename;
            }

            string SourceLocation()
            {
                return string.IsNullOrWhiteSpace(filename) ?
                          $"{lineCount}: "
                       :  $"{filename}:{lineCount}: ";
            }
        }
    }
}
