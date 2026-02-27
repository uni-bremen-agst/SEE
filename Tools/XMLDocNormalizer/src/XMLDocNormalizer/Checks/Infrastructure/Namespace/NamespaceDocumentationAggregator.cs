using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;

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
    /// - exactly one DOC101 per (directory, namespace) when no central namespace documentation exists, and
    /// - "central documentation" is only accepted in preferred dedicated namespace documentation files.
    ///
    /// Preferred file name conventions:
    /// - &lt;LastNamespaceSegment&gt;.cs (e.g. EdgeLayouts.cs)
    /// - if last segment ends with 's': &lt;Singular&gt;.cs (e.g. EdgeLayout.cs)
    /// - NamespaceDoc.cs
    /// - Namespace.cs
    ///
    /// Reporting strategy:
    /// - the first encountered undocumented namespace declaration is stored as the reporting anchor location
    /// - if later a central namespace documentation declaration is found, no finding is emitted for that namespace
    /// </remarks>
    internal sealed class NamespaceDocumentationAggregator
    {
        /// <summary>
        /// Aggregated states keyed by <c>{directory}|{namespace}</c>.
        /// </summary>
        /// <remarks>
        /// Case-insensitive keys allow consistent behavior on common file systems and typical repository usage.
        /// </remarks>
        private readonly Dictionary<string, NamespaceDocState> states;

        /// <summary>
        /// Indicates whether the aggregator is active for the current run.
        /// </summary>
        /// <remarks>
        /// When disabled, registration methods become no-ops and no findings are produced.
        /// </remarks>
        private readonly bool enabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceDocumentationAggregator"/> class.
        /// </summary>
        /// <param name="enabled">
        /// Whether namespace aggregation is enabled. If <c>false</c>, all registration methods are no-ops and
        /// <see cref="CreateMissingCentralNamespaceFindings"/> returns an empty list.
        /// </param>
        public NamespaceDocumentationAggregator(bool enabled)
        {
            this.enabled = enabled;
            states = new Dictionary<string, NamespaceDocState>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a documented namespace declaration.
        /// </summary>
        /// <param name="filePath">The file path containing the documented namespace declaration.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <remarks>
        /// Documentation counts as "central namespace documentation" only if it is located in a preferred
        /// namespace documentation file (see <see cref="IsPreferredNamespaceDocFile"/>).
        /// This enforces the convention that namespace documentation should live in the dedicated file,
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

            if (!IsPreferredNamespaceDocFile(filePath, namespaceName))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string key = BuildKey(directory, namespaceName);
            NamespaceDocState state = GetOrCreateState(key);
            state.HasCentralDocumentation = true;
        }

        /// <summary>
        /// Registers a missing namespace documentation occurrence.
        /// </summary>
        /// <param name="tree">The syntax tree used for line/column mapping and anchor calculations.</param>
        /// <param name="filePath">The file path containing the missing documentation occurrence.</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <param name="anchorPosition">The absolute anchor position used for reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tree"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Only the first missing location is stored to ensure a single stable reporting point per namespace.
        /// Even if the namespace appears undocumented in many files, only one DOC101 will be emitted.
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
            NamespaceDocState state = GetOrCreateState(key);

            if (state.FirstMissingLocation == null)
            {
                state.FirstMissingLocation = new NamespaceMissingLocation(tree, filePath, anchorPosition);
            }
        }

        /// <summary>
        /// Creates aggregated DOC101 findings for namespaces that have no central namespace documentation.
        /// </summary>
        /// <returns>
        /// A list of findings. Each (directory, namespace) produces at most one finding.
        /// Returns an empty list when the aggregator is disabled.
        /// </returns>
        /// <remarks>
        /// Findings are emitted at the first missing location that was recorded for that (directory, namespace).
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
                string primaryFile = GetPrimarySuggestedFileName(namespaceName);
                string secondaryFile = GetSecondarySuggestedFileName(namespaceName);

                Finding finding = FindingFactory.AtPosition(
                    location.Tree,
                    location.FilePath,
                    tagName: "namespace",
                    XmlDocSmells.MissingCentralNamespaceDocumentation,
                    location.AnchorPosition,
                    snippet: "",
                    primaryFile,
                    secondaryFile,
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
        /// <c>true</c> if the file name matches a preferred naming convention for the namespace; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is used to restrict what counts as "central namespace documentation" so that users are guided
        /// to place documentation in the dedicated file (e.g. EdgeLayouts.cs) and not at arbitrary namespace declarations.
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
        /// Gets an existing state for a key or creates a new one if missing.
        /// </summary>
        /// <param name="key">The internal state key.</param>
        /// <returns>The state for the specified key.</returns>
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
        /// <returns>A key in the form <c>{directory}|{namespaceName}</c>.</returns>
        private static string BuildKey(string directory, string namespaceName)
        {
            return directory + "|" + namespaceName;
        }

        /// <summary>
        /// Extracts the namespace portion from an internal state key.
        /// </summary>
        /// <param name="key">The key in the form <c>{directory}|{namespaceName}</c>.</param>
        /// <returns>The extracted namespace name.</returns>
        private static string ExtractNamespaceFromKey(string key)
        {
            int idx = key.IndexOf('|');
            if (idx < 0 || idx >= key.Length - 1)
            {
                return key;
            }

            return key.Substring(idx + 1);
        }

        /// <summary>
        /// Computes the primary suggested namespace documentation file name for a namespace.
        /// </summary>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>The primary suggestion (e.g. EdgeLayouts.cs).</returns>
        private static string GetPrimarySuggestedFileName(string namespaceName)
        {
            string lastSegment = GetLastNamespaceSegment(namespaceName);
            return lastSegment + ".cs";
        }

        /// <summary>
        /// Computes the secondary suggested namespace documentation file name for a namespace.
        /// </summary>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// If the last segment ends with 's', returns the singular form (e.g. EdgeLayout.cs).
        /// Otherwise returns <c>NamespaceDoc.cs</c>.
        /// </returns>
        private static string GetSecondarySuggestedFileName(string namespaceName)
        {
            string lastSegment = GetLastNamespaceSegment(namespaceName);

            if (lastSegment.EndsWith("s", StringComparison.OrdinalIgnoreCase) && lastSegment.Length > 1)
            {
                string singular = lastSegment.Substring(0, lastSegment.Length - 1);
                return singular + ".cs";
            }

            return "NamespaceDoc.cs";
        }

        /// <summary>
        /// Determines whether the file name is considered a preferred namespace documentation file name for the namespace.
        /// </summary>
        /// <param name="fileName">The file name (not a full path).</param>
        /// <param name="namespaceName">The fully qualified namespace name.</param>
        /// <returns>
        /// <c>true</c> if the file name matches one of the preferred conventions; otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Supported conventions:
        /// - &lt;LastNamespaceSegment&gt;.cs
        /// - if last segment ends with 's': &lt;Singular&gt;.cs
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
        /// <returns>The last namespace segment (e.g. EdgeLayouts).</returns>
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