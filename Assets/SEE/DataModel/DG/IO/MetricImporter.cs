﻿using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Imports node metrics from files into the graph.
    /// </summary>
    public class MetricImporter
    {
        public const string IDColumnName = "ID";

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
        public static int Load(Graph graph, string filename, char separator = ';')
        {
            if (!File.Exists(filename))
            {
                Debug.LogWarningFormat("Metric file {0} does not exist. Metrics will not be available.\n", filename);
                return 0;
            }
            int numberOfErrors = 0;
            try
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    if (reader.EndOfStream)
                    {
                        Debug.LogErrorFormat("Empty file: {0}", filename);
                    }
                    else
                    {
                        // The line number in the CSV file currently processed.
                        int lineCount = 1;
                        // Header row
                        string headerLine = reader.ReadLine();
                        // The names of the columns
                        string[] columnNames = headerLine.Split(separator);
                        lineCount++;
                        // We expect the ID plus at least one metric
                        if (columnNames.Length > 1)
                        {
                            // The first column must be the ID
                            if (columnNames[0] != IDColumnName)
                            {
                                Debug.LogErrorFormat("First header column in file {0} is not {1}.\n", filename, IDColumnName);
                                throw new IOException("First header column does not contain the expected attribute " + IDColumnName);
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
                                    Debug.LogErrorFormat("Unexpected number of entries in file {0} at line {1}.\n", filename, lineCount);
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
                                            Debug.LogErrorFormat("Missing value in file {0} at line {1}.\n", filename, lineCount);
                                            numberOfErrors++;
                                        }
                                        catch (FormatException)
                                        {
                                            Debug.LogErrorFormat("Value {0} does not represent a number in a valid format in file {1} at line {2}.\n", values[i], filename, lineCount);
                                            numberOfErrors++;
                                        }
                                        catch (OverflowException)
                                        {
                                            Debug.LogErrorFormat("Value {0} represents a number less than minimum or greater than maximum in file {1} at line {2}.\n", values[i], filename, lineCount);
                                            numberOfErrors++;
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogErrorFormat("Unknown node {0} in file {1} at line {2}.\n", columnNames[0], filename, lineCount);
                                    numberOfErrors++;
                                }
                                lineCount++;
                            }
                        }
                        else
                        {
                            Debug.LogErrorFormat("Not enough columns in file {0}\n", filename);
                            throw new IOException("Not enough columns.");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("Exception {0} while loading data from CSV file {1}.\n", e.Message, filename);
                throw e;
            }
            return numberOfErrors;
        }
    }

}
