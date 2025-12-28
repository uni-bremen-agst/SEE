using SEE.Utils;
using System;
using System.IO;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Strategy for normalizing Java code element identifiers to logical Identifiers for node lookup in <see cref="MetricApplier"/>.
    /// Handles Java-specific conventions including package separators, inner classes, and methods.
    /// </summary>
    public sealed class JavaIndexNodeStrategy : IIndexNodeStrategy
    {
        // Java conventions

        /// <summary>
        /// The separator used for Java packages and classes (dot).
        /// </summary>
        private const char nodeIdSeparator = '.';

        /// <summary>
        /// The delimiter used for inner classes in compiled Java bytecode (dollar sign).
        /// </summary>
        private const char innerClassDelimiter = '$';

        /// <summary>
        /// The delimiter used to separate method names (hash).
        /// </summary>
        private const char methodDelimiter = '#';

        /// <summary>
        /// The character indicating a compiler-generated artifact (less-than sign).
        /// </summary>
        private const char compilerGeneratedIndicator = '<';

        /// <summary>
        /// The configuration used for parsing paths (e.g., source root).
        /// </summary>
        private readonly ParsingConfig parsingConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaIndexNodeStrategy"/> class.
        /// </summary>
        /// <param name="parsingConfig">The configuration options for parsing paths.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsingConfig"/> is null.</exception>
        public JavaIndexNodeStrategy(ParsingConfig parsingConfig)
        {
            this.parsingConfig = parsingConfig ?? throw new ArgumentNullException(nameof(parsingConfig));
        }

        /// <summary>
        /// Converts raw path to the logical identifier that matches the output for <see cref="ToLogicalIdentifier(Node)"/> for every node.
        /// Removes methods, and replaces Delimiters with <see cref="nodeIdSeparator"/>.
        /// </summary>
        /// <param name="fullPath">Raw path (e.g., "package/Class$Inner#method").</param>
        /// <returns>Logical identifier (e.g., "package.Class.Inner") or null if invalid.</returns>
        public string ToLogicalIdentifier(string fullPath)
        {
            string normalized = ToFullIdentifier(fullPath);
            return normalized == null ? null : RemoveMethod(normalized);
        }

        /// <summary>
        /// Determines the logical identifier for a graph node.
        /// For methods, recursively looks up the parent type.
        /// </summary>
        /// <param name="node">The graph node to analyze.</param>
        /// <returns>The logical identifier, or null if the node type is not indexed.</returns>
        public string ToLogicalIdentifier(Node node)
        {
            if (node == null)
            {
                return null;
            }
            // For packages and Namespaces: Replace path separators with NodeIdSeparator
            if (node.Type == NodeTypes.Package || node.Type == NodeTypes.Namespace)
            {
                return Filenames.ReplaceDirectorySeparators(node.ID, nodeIdSeparator);
            }
            // For methods: recurse to parent type
            if (node.Type == NodeTypes.Method)
            {
                return node.Parent != null ? ToLogicalIdentifier(node.Parent) : null;
            }
            if (NodeTypeExtensions.IsTypeNode(node.Type))
            {
                return ResolveLogicalIdentifier(node.ID);
            }
            // Other node types are not indexed
            return null;
        }

        #region Helper Methods

        /// <summary>
        /// Removes a method suffixes from a qualified name.
        /// Example: "pkg.Outer$Inner#method" → "pkg.Outer$Inner".
        /// </summary>
        /// <param name="qualifiedName">The qualified name containing a method delimiter.</param>
        /// <returns>The name without the method part.</returns>
        private static string RemoveMethod(string qualifiedName)
        {
            int idx = qualifiedName.IndexOf(methodDelimiter);
            return idx < 0 ? qualifiedName : qualifiedName.Substring(0, idx);
        }

        /// <summary>
        /// Handles the path normalization to logical identifier for nodes with type <see cref="TypeNodeTypes"/>.
        /// </summary>
        /// <param name="nodeId">The raw ID of the node.</param>
        /// <returns>
        /// Returns the logical identifier from <see cref="Node.ID"/> by cutting methods and replacing every separator by <see cref="nodeIdSeparator"/>.
        /// </returns>
        private string ResolveLogicalIdentifier(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return nodeId;
            }

            string sourceRootRelative = parsingConfig.SourceRootRelativePath(nodeId);

            // Step 1: Remove method suffix if present
            string logicalId = RemoveMethod(sourceRootRelative);

            // Step 2: Replace directory separators with '.'
            logicalId = Filenames.ReplaceDirectorySeparators(logicalId, nodeIdSeparator);

            // Step 3: Replace inner method suffix with '.'
            return logicalId.Replace(innerClassDelimiter, nodeIdSeparator);
        }

        /// <summary>
        /// Normalizes a raw full path into a full logical identifier.
        /// This processes source root relativity and replaces separators, but does not strip methods.
        /// </summary>
        /// <param name="fullPath">The raw full path to normalize.</param>
        /// <returns>The normalized identifier, or null if empty or compiler-generated.</returns>
        public string ToFullIdentifier(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) ||
                fullPath.Contains(compilerGeneratedIndicator))
            {
                return null;
            }

            // Step 1: Get source root relative path
            string normalized = parsingConfig.SourceRootRelativePath(fullPath);

            normalized = Path.ChangeExtension(normalized, null);
            // Step 2: Convert path separators: '/' → '.' and '\\' -> '.'
            normalized = Filenames.ReplaceDirectorySeparators(normalized, nodeIdSeparator);

            // Step 3: Replace '$' -> '.' 
            return normalized.Replace(innerClassDelimiter, nodeIdSeparator);
        }

        #endregion
    }
}