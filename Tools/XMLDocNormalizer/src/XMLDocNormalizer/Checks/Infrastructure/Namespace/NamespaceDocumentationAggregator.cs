using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Namespace
{
    /// <summary>
    /// Aggregates namespace documentation information across a run to avoid redundant namespace findings.
    /// </summary>
    /// <remarks>
    /// If namespace documentation is required and a namespace appears in many files, emitting a missing-documentation
    /// finding per file creates redundant results for the same underlying issue.
    ///
    /// This aggregator ensures:
    /// - exactly one DOC100 finding per directory and namespace when no central namespace documentation exists
    /// - central namespace documentation is only accepted in preferred dedicated namespace documentation files
    ///
    /// Preferred file name conventions:
    /// - LastNamespaceSegment.cs, for example EdgeLayouts.cs
    /// - singular LastNamespaceSegment.cs when the last segment ends with s, for example EdgeLayout.cs
    /// - NamespaceDoc.cs
    /// - Namespace.cs
    ///
    /// Reporting strategy:
    /// - the first encountered undocumented namespace declaration is stored as the reporting anchor location
    /// - if a central namespace documentation declaration is found later, no finding is emitted for that namespace
    /// </remarks>
    /// <param name="enabled">
    /// Indicates whether namespace aggregation is enabled.
    /// If false, all registration methods are no-ops and CreateMissingCentralNamespaceFindings returns an empty list.
    /// </param>
    internal sealed class NamespaceDocumentationAggregator(bool enabled)
    {
        /// <summary>
        /// Stores aggregated namespace documentation states keyed by directory and namespace.
        /// </summary>
        /// <remarks>
        /// Case-insensitive keys allow consistent behavior on common file systems and typical repository usage.
        /// </remarks>
        private readonly Dictionary<string, NamespaceDocState> states = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Indicates whether the aggregator is active for the current run.
        /// </summary>
        /// <remarks>
        /// When disabled, registration methods return without storing information and no findings are produced.
        /// </remarks>
        private readonly bool enabled = enabled;

        /// <summary>
        /// Tracks unique directory and fully qualified namespace combinations observed during the run.
        /// </summary>
        /// <remarks>
        /// Keys follow the same format as the state dictionary.
        /// This enables correct coverage denominators for DOC100, which represents missing central namespace documentation.
        /// </remarks>
        private readonly HashSet<string> observedKeys = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the number of unique directory and fully qualified namespace combinations observed during the run.
        /// </summary>
        public int UniqueNamespaceKeyCount
        {
            get { return observedKeys.Count; }
        }

        /// <summary>
        /// Registers a documented namespace declaration.
        /// </summary>
        /// <param name="filePath">The file path containing the documented namespace declaration.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <remarks>
        /// Documentation counts as central namespace documentation only if it is located in a preferred namespace
        /// documentation file. This enforces the convention that namespace documentation should live in a dedicated file,
        /// not in arbitrary files.
        /// </remarks>
        public void RegisterDocumentedNamespace(string filePath, string namespaceName)
        {
            if (!enabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(filePath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string key = BuildKey(directory, namespaceName);
            observedKeys.Add(key);

            if (!IsPreferredNamespaceDocFile(filePath, namespaceName))
            {
                return;
            }

            NamespaceDocState state = GetOrCreateState(key);
            state.HasCentralDocumentation = true;
        }

        /// <summary>
        /// Registers a missing namespace documentation occurrence.
        /// </summary>
        /// <param name="tree">The syntax tree used for line and column mapping and anchor calculations.</param>
        /// <param name="filePath">The file path containing the missing documentation occurrence.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <param name="anchorPosition">The absolute anchor position used for reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when tree is null.</exception>
        /// <remarks>
        /// Only the first missing location is stored to ensure a single stable reporting point per namespace.
        /// Even if the namespace appears undocumented in many files, only one DOC100 finding is emitted.
        /// </remarks>
        public void RegisterMissingNamespaceDocumentation(
            SyntaxTree tree,
            string filePath,
            string namespaceName,
            int anchorPosition)
        {
            if (!enabled)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(tree);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(filePath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string key = BuildKey(directory, namespaceName);
            observedKeys.Add(key);

            NamespaceDocState state = GetOrCreateState(key);

            if (state.FirstMissingLocation == null)
            {
                state.FirstMissingLocation = new NamespaceMissingLocation(tree, filePath, anchorPosition);
            }
        }

        /// <summary>
        /// Creates aggregated DOC100 findings for namespaces that have no central namespace documentation.
        /// </summary>
        /// <returns>
        /// A list of findings.
        /// Each directory and namespace combination produces at most one finding.
        /// Returns an empty list when the aggregator is disabled.
        /// </returns>
        /// <remarks>
        /// Findings are emitted at the first missing location that was recorded for that directory and namespace.
        /// The message parameters provide suggested file names for where the namespace should be documented.
        /// </remarks>
        public List<Finding> CreateMissingCentralNamespaceFindings()
        {
            List<Finding> findings = new();

            if (!enabled)
            {
                return findings;
            }

            foreach (KeyValuePair<string, NamespaceDocState> pair in states)
            {
                NamespaceDocState state = pair.Value;

                if (state.HasCentralDocumentation)
                {
                    continue;
                }

                if (state.FirstMissingLocation == null)
                {
                    continue;
                }

                NamespaceMissingLocation location = state.FirstMissingLocation;

                string namespaceName = ExtractNamespaceFromKey(pair.Key);
                IReadOnlyList<string> suggestions = GetSuggestedFileNames(namespaceName);

                string suggestionText = string.Join("' or '", suggestions);

                Finding finding = FindingFactory.AtPosition(
                    location.Tree,
                    location.FilePath,
                    tagName: "namespace",
                    XmlDocSmells.MissingCentralNamespaceDocumentation,
                    location.AnchorPosition,
                    FindingContextBuilder.ForNamespace(
                        namespaceName,
                        filePath: location.FilePath),
                    snippet: "",
                    suggestionText,
                    namespaceName);

                findings.Add(finding);
            }

            return findings;
        }

        /// <summary>
        /// Determines whether the given file path is a preferred namespace documentation file for the specified namespace.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// True if the file name matches a preferred naming convention for the namespace; otherwise false.
        /// </returns>
        /// <remarks>
        /// This method restricts what counts as central namespace documentation so that users are guided to place
        /// namespace documentation in a dedicated file, for example EdgeLayouts.cs, instead of arbitrary namespace declarations.
        /// </remarks>
        public bool IsPreferredNamespaceDocFile(string filePath, string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            string fileName = Path.GetFileName(filePath);

            return IsPreferredNamespaceDocFileName(fileName, namespaceName);
        }

        /// <summary>
        /// Gets an existing namespace documentation state for a key or creates a new one if it is missing.
        /// </summary>
        /// <param name="key">The internal state key.</param>
        /// <returns>
        /// The state for the specified key.
        /// </returns>
        private NamespaceDocState GetOrCreateState(string key)
        {
            if (states.TryGetValue(key, out NamespaceDocState? existing))
            {
                return existing;
            }

            NamespaceDocState created = new NamespaceDocState();
            states[key] = created;

            return created;
        }

        /// <summary>
        /// Builds the internal key for the aggregated state map.
        /// </summary>
        /// <param name="directory">The directory path.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// A stable key that combines the directory and namespace name.
        /// </returns>
        private static string BuildKey(string directory, string namespaceName)
        {
            return directory + "|" + namespaceName;
        }

        /// <summary>
        /// Extracts the namespace portion from an internal state key.
        /// </summary>
        /// <param name="key">The internal state key.</param>
        /// <returns>
        /// The extracted namespace name.
        /// If the key cannot be split, the original key is returned.
        /// </returns>
        private static string ExtractNamespaceFromKey(string key)
        {
            int index = key.IndexOf('|');

            if (index < 0 || index >= key.Length - 1)
            {
                return key;
            }

            return key.Substring(index + 1);
        }

        /// <summary>
        /// Computes the suggested file names for a dedicated namespace documentation file.
        /// </summary>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// A stable, de-duplicated list of suggested file names using case-insensitive comparison.
        /// The first entry is the primary suggestion.
        /// </returns>
        /// <remarks>
        /// Suggestions follow these conventions:
        /// - LastNamespaceSegment.cs as the primary suggestion
        /// - the singular form when the last segment ends with s
        /// - NamespaceDoc.cs as a fallback
        ///
        /// Duplicate suggestions are removed.
        /// </remarks>
        private static IReadOnlyList<string> GetSuggestedFileNames(string namespaceName)
        {
            List<string> suggestions = new List<string>();

            string lastSegment = GetLastNamespaceSegment(namespaceName);

            suggestions.Add(lastSegment + ".cs");

            if (lastSegment.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                && lastSegment.Length > 1)
            {
                string singular = lastSegment.Substring(0, lastSegment.Length - 1);
                suggestions.Add(singular + ".cs");
            }

            suggestions.Add("NamespaceDoc.cs");

            return suggestions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Determines whether the file name is considered a preferred namespace documentation file name for the namespace.
        /// </summary>
        /// <param name="fileName">The file name without a directory path.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// True if the file name matches one of the preferred conventions; otherwise false.
        /// </returns>
        /// <remarks>
        /// Supported conventions:
        /// - LastNamespaceSegment.cs
        /// - the singular form when the last segment ends with s
        /// - NamespaceDoc.cs
        /// - Namespace.cs
        /// </remarks>
        private static bool IsPreferredNamespaceDocFileName(string fileName, string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            if (!fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string lastSegment = GetLastNamespaceSegment(namespaceName);

            if (string.Equals(fileName, lastSegment + ".cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (lastSegment.EndsWith("s", StringComparison.OrdinalIgnoreCase) && lastSegment.Length > 1)
            {
                string singular = lastSegment.Substring(0, lastSegment.Length - 1);

                if (string.Equals(fileName, singular + ".cs", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (string.Equals(fileName, "NamespaceDoc.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileName, "Namespace.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the last segment of a fully qualified namespace name.
        /// </summary>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// The last namespace segment.
        /// For example, EdgeLayouts for SEE.Layout.EdgeLayouts.
        /// </returns>
        private static string GetLastNamespaceSegment(string namespaceName)
        {
            int lastDot = namespaceName.LastIndexOf('.');

            if (lastDot < 0)
            {
                return namespaceName;
            }

            if (lastDot >= namespaceName.Length - 1)
            {
                return namespaceName;
            }

            return namespaceName.Substring(lastDot + 1);
        }
    }
}
