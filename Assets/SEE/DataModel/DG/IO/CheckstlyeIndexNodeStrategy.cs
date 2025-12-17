using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Index strategy for Checkstyle reports.
    ///
    /// This strategy translates file-oriented paths emitted by Checkstyle (often absolute OS paths)
    /// into the fully qualified class names (FQCNs) used as identifiers in the Java GLX graphs.
    ///
    /// Typical input is an absolute file path (Windows or Linux style), which is normalized and then
    /// mapped to a dotted package name plus class name.
    ///
    /// Example:
    /// <c>C:\...\src\main\java\com\medical\services\auth\CustomUserDetailsService.java</c>
    /// becomes
    /// <c>com.medical.services.auth.CustomUserDetailsService</c>.
    ///
    /// Notes:
    /// - This implementation assumes a Java source layout where a configurable source root marker
    ///   (e.g., <c>src/main/java</c>) can be used to cut off the leading path.
    /// - For already-normalized identifiers (no slashes/backslashes, but contains a dot), the input is
    ///   returned unchanged.
    /// </summary>
    public sealed class CheckstyleIndexNodeStrategy : IIndexNodeStrategy
    {
        /// <summary>
        /// Separator used in Java package/class notation (e.g., <c>com.example.ClassName</c>).
        /// </summary>
        private const char PackageSeparator = '.';

        /// <summary>
        /// Windows path separator (<c>\</c>), used to detect and normalize Windows-style paths.
        /// </summary>
        private const char WindowsPathSeparator = '\\';

        /// <summary>
        /// Linux/Unix path separator (<c>/</c>), used as the normalized internal separator.
        /// </summary>
        private const char LinuxPathSeparator = '/';

        /// <summary>
        /// Parsing configuration that provides path normalization helpers (most importantly
        /// <see cref="ParsingConfig.SourceRootRelativePath(string)"/>).
        ///
        /// This allows the strategy to normalize absolute paths from both GLX and Checkstyle reports
        /// to the same stable, source-root-relative form before converting them into an FQCN.
        ///
        /// Preconditions:
        /// - Must not be null.
        /// </summary>
        private readonly ParsingConfig config;

        /// <summary>
        /// Creates a new Checkstyle index strategy.
        ///
        /// The strategy relies on the provided <see cref="ParsingConfig"/> to normalize absolute or
        /// platform-specific file paths into stable, source-root-relative paths, and finally into
        /// fully qualified Java class names (FQCNs).
        /// </summary>
        /// <param name="config">
        /// The parsing configuration that provides path normalization settings (most importantly the
        /// source-root marker used by <see cref="ParsingConfig.SourceRootRelativePath(string)"/>).
        ///
        /// The configured source-root marker (e.g., <c>src/main/java</c>) allows this strategy to
        /// ignore differences in absolute prefixes between the GLX graph and the Checkstyle report,
        /// as long as both paths share the same suffix starting at the marker.
        ///
        /// Preconditions:
        /// - Must not be null.
        /// - <see cref="ParsingConfig.SourceRootMarker"/> may be empty; in that case normalization is
        ///   best-effort and depends on the input path layout.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
        internal CheckstyleIndexNodeStrategy(ParsingConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Produces the node identifier that can be used with <c>graph.TryGetNode(...)</c>.
        ///
        /// For Checkstyle, file findings are aggregated at the class/file level, so we use the
        /// fully qualified class name as the node id.
        ///
        /// Preconditions:
        /// - <paramref name="fullPath"/> should be a path or identifier that can be converted to an FQCN.
        /// </summary>
        /// <param name="fullPath">Absolute or relative file path (or already normalized FQCN).</param>
        /// <returns>
        /// The fully qualified class name for the file, or null if <paramref name="fullPath"/> is empty/invalid.
        /// </returns>
        public string FindingPathToNodeId(string fullPath)
        {
            return ToQualifiedClassName(fullPath);
        }

        /// <summary>
        /// Computes the "main type" for a given finding path.
        ///
        /// Because Checkstyle reports are effectively file-level findings (as opposed to method-level),
        /// this is identical to the node id in this strategy.
        ///
        /// Preconditions:
        /// - <paramref name="fullPath"/> should be a path or identifier that can be converted to an FQCN.
        /// </summary>
        /// <param name="fullPath">Absolute or relative file path (or already normalized FQCN).</param>
        /// <param name="fileName">File name part; not used for Checkstyle main-type resolution here.</param>
        /// <returns>
        /// The fully qualified class name for the file, or null if <paramref name="fullPath"/> is empty/invalid.
        /// </returns>
        public string FindingPathToMainType(string fullPath, string fileName)
        {
            return ToQualifiedClassName(fullPath);
        }

        /// <summary>
        /// Maps an existing graph node to its "main type" (i.e., the class that should be indexed for source ranges).
        ///
        /// This is primarily used by indexing infrastructure such as <c>SourceRangeIndex</c>.
        ///
        /// Resolution rules:
        /// - Method nodes: walk up to the parent until a type node (or null) is reached.
        /// - Type nodes (Class/Interface/templates): resolve potential inner type suffixes (<c>$Inner</c>)
        ///   and ensure the type name matches the file's main type.
        /// - File nodes: interpret the node ID as a path or FQCN and normalize via <see cref="ToQualifiedClassName"/>.
        /// - Other node types: not indexed and therefore return null.
        ///
        /// Preconditions:
        /// - <paramref name="node"/> may be null (returns null).
        /// </summary>
        /// <param name="node">The graph node to translate into a main-type identifier.</param>
        /// <returns>
        /// A string identifier representing the main type for indexing, or null if the node is not indexable.
        /// </returns>
        public string NodeIdToMainType(Node node)
        {
            if (node == null)
            {
                return null;
            }

            // Methods -> climb to the parent type.
            // This ensures that a method finding is attributed to the surrounding class/interface.
            if (node.Type == "Method")
            {
                return node.Parent != null ? NodeIdToMainType(node.Parent) : null;
            }

            // Type nodes -> normalize to the main type of the file (e.g., replace non-main type names).
            if (IsTypeNode(node.Type))
            {
                return ResolveMainType(node.ID, node.Filename);
            }

            if (node.Type == "File")
            {
                // For file nodes, we explicitly apply path normalization logic.
                // In some graphs, File node IDs may be absolute paths.
                string res = ToQualifiedClassName(node.ID);

                return res;
            }

            // Other node types are not indexable for Checkstyle.
            return null;
        }

        #region Helper für Typ-Erkennung

        /// <summary>
        /// Set of node types that represent Java type declarations in the GLX graphs.
        ///
        /// This includes concrete and template variants for classes and interfaces.
        /// </summary>
        private static readonly HashSet<string> TypeNodeTypes = new()
        {
            "Class",
            "Interface",
            "Class_Template",
            "Interface_Template"
        };

        /// <summary>
        /// Determines whether a given node type string represents a Java type node.
        /// </summary>
        /// <param name="nodeType">Type label from the graph (e.g., "Class", "Method").</param>
        /// <returns>True if the node type is considered a type declaration node; otherwise false.</returns>
        private static bool IsTypeNode(string nodeType)
        {
            return TypeNodeTypes.Contains(nodeType);
        }

        #endregion

        #region Helper für Pfad → FQCN

        /// <summary>
        /// Converts a Checkstyle-reported file path into a fully qualified class name (FQCN).
        ///
        /// Steps:
        /// 1) Return early if the input looks already normalized (no slashes/backslashes, but contains dots).
        /// 2) Normalize the path into a stable, source-root-relative path via <see cref="ParsingConfig.SourceRootRelativePath(string)"/>.
        /// 3) Strip the ".java" extension if present.
        /// 4) Replace '/' with '.' and trim leading/trailing dots.
        ///
        /// Preconditions:
        /// - <paramref name="fullPath"/> may be an absolute path, a relative path, or an already normalized FQCN.
        /// - If <see cref="ParsingConfig.SourceRootMarker"/> is empty, normalization is best-effort and may yield
        ///   incorrect results for unusual directory layouts.
        /// </summary>
        /// <param name="fullPath">The file path or identifier to normalize.</param>
        /// <returns>The normalized fully qualified class name, or null if <paramref name="fullPath"/> is empty.</returns>
        private string ToQualifiedClassName(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            // Return if already normalized:
            // - no OS path separators found
            // - contains package separator dots
            // This avoids double-normalizing values that are already FQCNs.
            if (!fullPath.Contains(WindowsPathSeparator) && !fullPath.Contains(LinuxPathSeparator) && fullPath.Contains(PackageSeparator))
            {
                return fullPath;
            }

            string normalized = config.SourceRootRelativePath(fullPath);

            // Remove ".java" extension (Checkstyle typically refers to Java source files).
            if (normalized.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 5);
            }

            // Convert remaining path separators to package separators to form the FQCN.
            normalized = normalized.Replace(LinuxPathSeparator, PackageSeparator).Trim('.');

            return normalized;
        }

        #endregion

        #region Helper für Main-Type-Auflösung

        /// <summary>
        /// Removes an inner-class suffix from a qualified type name (e.g., <c>com.example.Outer$Inner</c>).
        ///
        /// This is necessary because some graph IDs encode inner types using '$' (JVM-style),
        /// while file/main-type identification should be based on the outer (top-level) type.
        /// </summary>
        /// <param name="qualifiedName">A potentially inner-type qualified name.</param>
        /// <returns>The outer type name without '$' suffix, or the original string if none is present.</returns>
        private static string RemoveInnerTypeSuffix(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                return qualifiedName;
            }

            int idx = qualifiedName.IndexOf('$');
            return idx < 0 ? qualifiedName : qualifiedName.Substring(0, idx);
        }

        /// <summary>
        /// Replaces the simple type name portion of an FQCN with the "main type" derived from the given file name.
        ///
        /// Example:
        /// <c>com.example.Helper</c> with file <c>Main.java</c> becomes <c>com.example.Main</c>.
        ///
        /// Preconditions:
        /// - <paramref name="fileName"/> should be a file name that contains a meaningful type name
        ///   (usually the main Java type for that file).
        /// </summary>
        /// <param name="qualifiedName">An FQCN whose simple type name should be replaced.</param>
        /// <param name="fileName">The file name used to derive the main type (extension is removed).</param>
        /// <returns>The FQCN with its simple type name replaced by the file's main type name.</returns>
        private static string ReplaceWithMainType(string qualifiedName, string fileName)
        {
            string mainClassName = Path.GetFileNameWithoutExtension(fileName);
            int lastDotIndex = qualifiedName.LastIndexOf(PackageSeparator);

            if (lastDotIndex < 0)
            {
                // No package component -> return only the class name.
                return mainClassName;
            }

            string packageName = qualifiedName.Substring(0, lastDotIndex);
            return packageName + PackageSeparator + mainClassName;
        }

        /// <summary>
        /// Resolves a graph node id to the corresponding main type identifier.
        ///
        /// This handles:
        /// - Inner-class notation (<c>$</c>) by collapsing to the outer type.
        /// - Non-main types by replacing the simple type name with the file-derived main type name.
        ///
        /// Preconditions:
        /// - <paramref name="nodeId"/> and <paramref name="filename"/> should be present for accurate resolution.
        /// - If either is missing, the original <paramref name="nodeId"/> is returned unchanged.
        /// </summary>
        /// <param name="nodeId">The node identifier from the graph (often an FQCN, possibly with '$').</param>
        /// <param name="filename">The file name associated with the node.</param>
        /// <returns>A normalized identifier representing the file's main type.</returns>
        private static string ResolveMainType(string nodeId, string filename)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(filename))
            {
                return nodeId;
            }

            // 1) Remove inner-class suffix, if any.
            string outerType = RemoveInnerTypeSuffix(nodeId);

            // 2) If the simple type name already matches the file name, this is the main type.
            string simpleTypeName = GetSimpleName(outerType);
            string mainTypeFromFile = Path.GetFileNameWithoutExtension(filename);

            if (string.Equals(simpleTypeName, mainTypeFromFile, StringComparison.Ordinal))
            {
                // Already the main type.
                return outerType;
            }

            // 3) Otherwise, map to the main type name derived from the file.
            return ReplaceWithMainType(outerType, filename);
        }

        /// <summary>
        /// Extracts the simple (unqualified) type name from a fully qualified name.
        ///
        /// Example:
        /// <c>com.example.MyType</c> -> <c>MyType</c>
        /// </summary>
        /// <param name="qualifiedName">A fully qualified name using '.' as separator.</param>
        /// <returns>The simple type name, or the original input if no separator is present.</returns>
        private static string GetSimpleName(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                return qualifiedName;
            }

            int lastDotIndex = qualifiedName.LastIndexOf(PackageSeparator);
            return lastDotIndex < 0
                ? qualifiedName
                : qualifiedName.Substring(lastDotIndex + 1);
        }

        #endregion
    }
}
