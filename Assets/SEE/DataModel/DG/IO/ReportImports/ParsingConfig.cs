using SEE.DataModel.DG.GraphIndex;
using SEE.Utils;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Base configuration that describes how a specific tool's report should be interpreted.
    /// </summary>
    [Serializable]
    public abstract class ParsingConfig
    {
        /// <summary>
        /// Identifier that ties parsed metrics to their origin (for example, "JaCoCo").
        /// This value must not be null when a parser uses this configuration.
        /// </summary>
        /// <remarks>This is not a user setting, yet it must be saved to a configuration file.
        /// It will be used to identify what type of <see cref="ParsingConfig"/> must
        /// instantiated when reading a configuration file. It depends solely on the type
        /// of report data and will be set by the subclasses appropriately.</remarks>
        [HideInInspector]
        public string ToolId = string.Empty;

        /// <summary>
        /// Optional marker used to normalize file paths between the internal graph
        /// and the external tool report.
        ///
        /// Some tools emit absolute paths or paths rooted differently than the
        /// internal graph. When this value is set, <see cref="SourceRootRelativePath(string)"/>
        /// tries to cut off everything up to the last occurrence of this marker and
        /// returns the remaining path relative to that "source root".
        ///
        /// Example:
        /// <code>
        /// SourceRootMarker = "src/main/java"
        /// fullPath = "C:/work/proj/src/main/java/com/acme/Foo.java"
        /// result  = "com/acme/Foo.java"
        /// </code>
        ///
        /// Leave this empty if report paths and paths in the graph already match.
        /// </summary>
        /// <remarks>This is a user setting. It must be saved to a configuration file.</remarks>
        [Tooltip("Marks the root of external paths of the import. "
            + "If set, it will be used to normalize imported paths to match the paths in the the current graph. "
            + "For instance, if an external path is 'C:/work/proj/src/main/java/com/acme/Foo.java' "
            + "and this setting is 'src/main/java', the normalized path will be 'com/acme/Foo.java'. "
            + "Leave empty if the paths match already.")]
        public string SourceRootMarker = string.Empty;

        /// <summary>
        /// Creates the concrete parser that can process this configuration.
        /// The returned parser instance must not be null.
        /// </summary>
        /// <returns>
        /// A concrete <see cref="IReportParser"/> that can interpret reports described by this configuration.
        /// </returns>
        internal abstract IReportParser CreateParser();

        /// <summary>
        /// Converts the given full path into a relative path based on the <see cref="SourceRootMarker"/>.
        /// </summary>
        /// <param name="fullPath">The absolute path to be normalized.</param>
        /// <returns>The normalized relative path, or the normalized full path if the marker is not found.</returns>
        public string SourceRootRelativePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return string.Empty;
            }

            // 1) Normalize path separators to a single canonical separator.
            string normalized = fullPath.Replace(Filenames.WindowsDirectorySeparator, Filenames.UnixDirectorySeparator);

            // 2) Cut off everything before the configured source root marker, if available.
            if (!string.IsNullOrWhiteSpace(SourceRootMarker))
            {
                // Normalize the marker itself to use '/' and no leading/trailing slashes.
                string marker = SourceRootMarker.Replace('\\', '/').Trim('/');
                string needle = "/" + marker + "/";

                // Prefer a clean boundary match ("/marker/") from the end of the path (LastIndexOf).
                int idx = normalized.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    normalized = normalized.Substring(idx + needle.Length);
                }
                else
                {
                    // Fallback: fuzzy match on the marker substring.
                    // This is less precise but may still yield acceptable results for unusual path layouts.
                    idx = normalized.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        normalized = normalized.Substring(idx + marker.Length).TrimStart('/');
                    }
                }
            }
            return normalized;
        }

        /// <summary>
        /// Creates the concrete index strategy that is used to find nodes in a <see cref="SourceRangeIndex"/>.
        /// The returned strategy instance must not be null.
        /// </summary>
        /// <returns>
        /// An <see cref="IIndexNodeStrategy"/> used to locate nodes in a <see cref="SourceRangeIndex"/>.
        /// </returns>
        public abstract IIndexNodeStrategy CreateIndexNodeStrategy();

        #region Config I/O

        /// <summary>
        /// Label of <see cref="ToolId"/> in the configuration file.
        /// </summary>
        private const string toolIdLabel = "ToolId";

        /// <summary>
        /// Label of <see cref="SourceRootMarker"/> in the configuration file.
        /// </summary>
        private const string sourceRootMarkerLabel = "SourceRootMarker";

        /// <summary>
        /// Saves the attributes to the configuration file under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">The configuration writer used to persist the attributes.</param>
        /// <param name="label">The label under which the attributes are saved in the configuration file.</param>
        public virtual void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(ToolId, toolIdLabel);
            writer.Save(SourceRootMarker, sourceRootMarkerLabel);
            SaveAdditional(writer);
            writer.EndGroup();
        }
        /// <summary>
        /// Derived classes can extend <see cref="Save(ConfigWriter, string)"/>
        /// </summary>
        /// <param name="writer">>The configuration writer used to persist the attributes.</param>
        protected virtual void SaveAdditional(ConfigWriter writer) { }

        /// <summary>
        /// Restores the attributes from the configuration file.
        /// </summary>
        /// <param name="attributes">The dictionary containing saved configuration attributes.</param>
        /// <param name="label">The label identifying the group of attributes to restore.</param>
        /// <param name="parsingConfig">The resulting <see cref="ParsingConfig"/> instance, or null if restoration fails.</param>
        public static void Restore(Dictionary<string, object> attributes, string label, out ParsingConfig parsingConfig)
        {
            parsingConfig = null;

            if (!attributes.TryGetValue(label, out object groupObj) || groupObj is not Dictionary<string, object> groupDict)
            {
                return;
            }

            string toolId = string.Empty;
            ConfigIO.Restore(groupDict, toolIdLabel, ref toolId);

            string sourceRootMarker = string.Empty;
            ConfigIO.Restore(groupDict, sourceRootMarkerLabel, ref sourceRootMarker);

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
            // Extension for derived classes
            parsingConfig.RestoreAdditional(groupDict);
        }
        /// <summary>
        /// Derived classes can extend <see cref="Restore(Dictionary{string, object}, string, out ParsingConfig)"/>
        /// </summary>
        /// <param name="groupDict">The dictionary containing the group of attributes to restore additional values from.</param>
        protected virtual void RestoreAdditional(Dictionary<string, object> groupDict) { }


        #endregion
    }
}
