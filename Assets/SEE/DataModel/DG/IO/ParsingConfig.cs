using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Contains data model types for parsing and interpreting external tool reports in a <see cref="Graph"/>.
/// </summary>
namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Base configuration that describes how a specific tool's report should be interpreted.
    /// Implementations must provide a non-null tool identifier and a valid XPath mapping before use.
    /// </summary>
    public abstract class ParsingConfig
    {
        /// <summary>
        /// Windows path separator (<c>\</c>), used to detect and normalize Windows-style paths.
        /// </summary>
        private const char WindowsPathSeparator = '\\';

        /// <summary>
        /// Linux/Unix path separator (<c>/</c>), used as the normalized internal separator.
        /// </summary>
        private const char LinuxPathSeparator = '/';

        /// <summary>
        /// Identifier that ties parsed metrics to their origin (for example, "JaCoCo").
        /// This value must not be null when a parser uses this configuration.
        /// </summary>
        public string ToolId = string.Empty;

        /// <summary>
        /// Source root marker of the project
        /// </summary>
        public string SourceRootMarker = string.Empty;

        /// <summary>
        /// Describes which XML nodes to visit and how to interpret them.
        /// This value must not be null when a parser uses this configuration.
        /// </summary>
        public XPathMapping XPathMapping;

        /// <summary>
        /// Creates the concrete parser that can process this configuration.
        /// The returned parser instance must not be null.
        /// </summary>
        /// <returns>
        /// A concrete <see cref="IReportParser"/> that can interpret reports described by this configuration.
        /// </returns>
        internal abstract IReportParser CreateParser();

        public string SourceRootRelativePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return string.Empty;
            }

            // 1) Normalize path separators to a single canonical separator.
            string normalized = fullPath.Replace(WindowsPathSeparator, LinuxPathSeparator);

            // 2) Cut off everything before the configured source root marker, if available.
            if (!string.IsNullOrWhiteSpace(SourceRootMarker))
            {
                // Normalize the marker itself to use '/' and no leading/trailing slashes.
                string marker = SourceRootMarker.Replace('\\', '/').Trim('/');
                string needle = "/" + marker + "/";

                // Prefer a clean boundary match ("/marker/") from the end of the path (LastIndexOf).
                int idx = normalized.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    normalized = normalized.Substring(idx + needle.Length);
                else
                {
                    // Fallback: fuzzy match on the marker substring.
                    // This is less precise but may still yield acceptable results for unusual path layouts.
                    idx = normalized.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        normalized = normalized.Substring(idx + marker.Length).TrimStart('/');
                }
            }
            return normalized;
        }

        /// <summary>
        /// Creates the concrete index strategy that is used to find nodes in a <c>SourceRangeIndex</c>.
        /// The returned strategy instance must not be null.
        /// </summary>
        /// <returns>
        /// An <see cref="IIndexNodeStrategy"/> used to locate nodes in a <c>SourceRangeIndex</c>.
        /// </returns>
        public abstract IIndexNodeStrategy CreateIndexNodeStrategy();

        #region Config I/O

        /// <summary>
        /// Label of <see cref="ToolId"/> in the configuration file.
        /// </summary>
        private const string ToolIdLabel = "ToolId";

        /// <summary>
        /// Label of <see cref="SourceRootMarker"/> in the configuration file.
        /// </summary>
        private const string SourceRootMarkerLabel = "SourceRootMarker";

        /// <summary>
        /// Saves the attributes to the configuration file under the given <paramref name="label"/>.
        /// </summary>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(ToolId, ToolIdLabel);
            writer.Save(SourceRootMarker, SourceRootMarkerLabel);
            SaveAdditional(writer);
            writer.EndGroup();
        }
        /// <summary>
        /// Derived classes can extend <see cref="Save(ConfigWriter, string)"/>
        /// </summary>
        /// <param name="writer"></param>
        protected virtual void SaveAdditional(ConfigWriter writer) { }

        /// <summary>
        /// Restores the attributes from the configuration file.
        /// </summary>
        public static void Restore(Dictionary<string, object> attributes, string label, out ParsingConfig parsingConfig)
        {
            parsingConfig = null;

            if (!attributes.TryGetValue(label, out object groupObj) || groupObj is not Dictionary<string, object> groupDict)
            {
                return;
            }

            string toolId = string.Empty;
            ConfigIO.Restore(groupDict, ToolIdLabel, ref toolId);

            string sourceRootMarker = string.Empty;
            ConfigIO.Restore(groupDict, SourceRootMarkerLabel, ref sourceRootMarker);

            if (string.IsNullOrWhiteSpace(toolId))
            {
                return;
            }

            parsingConfig = ParsingConfigFactory.Create(toolId);
            if (parsingConfig == null)
            {
                return;
            }

            // Apply restored values to the created instance.
            parsingConfig.ToolId = toolId;
            parsingConfig.SourceRootMarker = sourceRootMarker;
            // Extention for derived classes
            parsingConfig.RestoreAdditional(groupDict);
        }
        /// <summary>
        /// Derived classes can extend <see cref="Restore(Dictionary{string, object}, string, out ParsingConfig)"/>
        /// </summary>
        /// <param name="groupDict"></param>
        protected virtual void RestoreAdditional(Dictionary<string, object> groupDict) { }


        #endregion
    }
}
