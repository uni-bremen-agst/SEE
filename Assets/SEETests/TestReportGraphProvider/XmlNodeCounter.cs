using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using SEE.GraphProviders.NodeCounting;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Counts nodes in XML reports.
    /// </summary>
    internal class XmlNodeCounter : ICountReportNodes
    {
        /// <summary>
        /// Counts how many times each tag in <paramref name="tagNames"/> occurs inside the report.
        /// Preconditions: <paramref name="reportPath"/> must not be null or empty and
        /// <paramref name="tagNames"/> must not be null.
        /// </summary>
        /// <param name="reportPath">
        /// Path relative to <see cref="Application.streamingAssetsPath"/>. Must not be null or empty.
        /// </param>
        /// <param name="tagNames">Set of XML element names to tally. Must not be null.</param>
        /// <returns>Case-insensitive dictionary with counts for every requested tag.</returns>
        public Dictionary<string, int> Count(string reportPath, IEnumerable<string> tagNames)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                throw new ArgumentException("Report path cannot be null or empty.", nameof(reportPath));
            }

            if (tagNames == null)
            {
                throw new ArgumentNullException(nameof(tagNames));
            }

            string fullPath = Path.Combine(
                Application.streamingAssetsPath,
                reportPath.TrimStart('/', '\\'));

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Report file not found: {fullPath}");
            }

            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<string> tagNamesList = tagNames.ToList();

            try
            {
                XDocument doc = XDocument.Load(fullPath);

                foreach (string tagName in tagNamesList)
                {
                    int count = doc
                        .Descendants()
                        .Count(element =>
                            element.Name.LocalName.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                    result[tagName] = count;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse XML file: {fullPath}", ex);
            }

            return result;
        }
    }
}
