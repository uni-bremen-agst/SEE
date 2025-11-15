using SEE.GraphProviders.NodeCounting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Xml.Linq;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Counts nodes in XML reports.
    /// </summary>
    public class XmlNodeCounter : ICountReportNodes
    {
        /// <summary>
        /// Counts how many times each tag in <paramref name="tagNames"/> occurs inside the report.
        /// </summary>
        /// <param name="reportPath">path relative to <see cref="Application.streamingAssetsPath"/></param>
        /// <param name="tagNames">set of XML element names to tally</param>
        /// <returns>case-insensitive dictionary with counts for every requested tag</returns>
        public Dictionary<string, int> Count(string reportPath, IEnumerable<string> tagNames)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
                throw new ArgumentException("Report path cannot be null or empty", nameof(reportPath));

            if (tagNames == null)
                throw new ArgumentNullException(nameof(tagNames));

            string fullPath = Path.Combine(
                Application.streamingAssetsPath,
                reportPath.TrimStart('/', '\\')
            );

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Report file not found: {fullPath}");

            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var tagNamesList = tagNames.ToList();

            try
            {
                XDocument doc = XDocument.Load(fullPath);

                foreach (string tagName in tagNamesList)
                {
                    // Count all elements with this tag name (case-insensitive using LocalName)
                    int count = doc.Descendants()
                        .Count(e => e.Name.LocalName.Equals(tagName, StringComparison.OrdinalIgnoreCase));

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
