using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Exports node metrics of a graph to CSV files.
    /// </summary>
    public class MetricExporter : MetricsIO
    {
        /// <summary>
        /// Saves all node metrics of given <paramref name="graph"/> to a file named 
        /// <paramref name="filename"/> in CSV format where <paramref name="separator"/> 
        /// is used to separate columns.
        /// </summary>
        /// <param name="graph">the graph whose node metrics are to be stored</param>
        /// <param name="filename">the name of the CSV file in which to save the metrics</param>
        /// <param name="separator">the separator inbetween columns</param>
        public static void Save(Graph graph, string filename, char separator = ';')
        {
            List<string> intAttributes = graph.AllIntNodeAttributes();
            List<string> floatAttributes = graph.AllFloatNodeAttributes();

            if (intAttributes.Count + floatAttributes.Count > 0)
            {
                using (StreamWriter outputFile = new StreamWriter(filename))
                {
                    WriteHeader(outputFile, intAttributes, floatAttributes, separator);
                    foreach (Node node in graph.Nodes())
                    {
                        WriteAttributes(outputFile, node, intAttributes, floatAttributes, separator);
                    }
                }
            }
            else
            {
                Debug.LogWarning("The graph has no node attributes. No CSV file will be written.\n");
            }
        }

        /// <summary>
        /// Writes the header row with the column names.
        /// </summary>
        /// <param name="outputFile">where to write</param>
        /// <param name="intAttributes">integer attribute names</param>
        /// <param name="floatAttributes">float attribute names</param>
        /// <param name="separator">the character used to separate columns</param>
        private static void WriteHeader
            (StreamWriter outputFile, 
            List<string> intAttributes, 
            List<string> floatAttributes, 
            char separator)
        {
            StringBuilder sb = new StringBuilder();
            // The IDColumnName must be the first column.
            sb.Append(IDColumnName);
            sb.Append(separator);
            // Integer attributes
            foreach (string name in intAttributes)
            {
                sb.Append(name);
                sb.Append(separator);
            }
            // Float attributes
            foreach (string name in floatAttributes)
            {
                sb.Append(name);
                sb.Append(separator);
            }
            sb.Length--; // Remove last separator again
            outputFile.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Writes the attribute values of <paramref name="node"/> for all <paramref name="intAttributes"/> 
        /// and <paramref name="floatAttributes"/>. If <paramref name="node"/> does not have a value
        /// for any of these, 0 will be written.
        /// </summary>
        /// <param name="outputFile">where to write</param>
        /// <param name="node">node whose attributes are to be written</param>
        /// <param name="intAttributes">integer attribute names</param>
        /// <param name="floatAttributes">float attribute names</param>
        /// <param name="separator">the character used to separate columns</param>
        private static void WriteAttributes(StreamWriter outputFile, Node node, List<string> intAttributes, List<string> floatAttributes, char separator)
        {
            StringBuilder sb = new StringBuilder();
            // The ID must be in the first column.
            sb.Append(node.ID);
            sb.Append(separator);
            // Integer attributes
            foreach (string name in intAttributes)
            {
                if (node.TryGetInt(name, out int value))
                {
                    sb.Append(value);
                }
                else
                {
                    sb.Append(0);
                }
                sb.Append(separator);
            }
            // Float attributes
            foreach (string name in floatAttributes)
            {
                if (node.TryGetFloat(name, out float value))
                {
                    sb.Append(value);
                }
                else
                {
                    sb.Append(0);
                }
                sb.Append(separator);
            }
            sb.Length--; // Remove last separator again
            outputFile.WriteLine(sb.ToString());
        }
    }
}