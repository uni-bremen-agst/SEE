using System;
using System.Collections.Generic;
using System.IO;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Strategy for normalizing Java code element identifiers to main type paths.
    /// Handles JaCoCo-specific conventions including package separators, inner classes, and methods.
    /// </summary>
    public sealed class JaCoCoIndexNodeStrategy : IIndexNodeStrategy
    {
        // JaCoCo and Java conventions
        private const char LinuxPathSeparator = '/';
        private const char WindowsPathSeparator = '\\';
        private const char NodeIdSeparator = '.';
        private const char InnerClassDelimiter = '$';
        private const char MethodDelimiter = '#';
        private const char CompilerGeneratedIndicator = '<';

        /// <summary>
        /// Converts JaCoCo's slash-separated package paths to dot-separated format.
        /// Example: "org/example/MyClass" → "org.example.MyClass"
        /// </summary>
        public string FindingPathToNodeId(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            // 1. replacing path separators
            fullPath = fullPath.Replace(LinuxPathSeparator, NodeIdSeparator).Replace(WindowsPathSeparator, NodeIdSeparator);

            bool isMethod = fullPath.IndexOf(MethodDelimiter) > -1;

            // 2. Replacing methodDelimiter
            if (isMethod)
            {
                fullPath = fullPath.Replace(MethodDelimiter.ToString(), ".~") + "()";

            }
            return fullPath;
        }

        /// <summary>
        /// Converts JaCoCo's raw path to the main type identifier that matches graph node IDs.
        /// Removes inner classes, methods, and uses filename to identify the main type.
        /// </summary>
        /// <param name="fullPath">Raw path from JaCoCo (e.g., "package/Class$Inner#method")</param>
        /// <param name="fileName">Source filename (e.g., "Class.java")</param>
        /// <returns>Main type identifier (e.g., "package.Class") or null if invalid</returns>
        public string FindingPathToMainType(string fullPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fullPath) ||
                fullPath.Contains(CompilerGeneratedIndicator))
            {
                return null;
            }

            // Step 1: Convert path separators: '/' → '.'
            string normalized = fullPath.Replace(LinuxPathSeparator, NodeIdSeparator).Replace(WindowsPathSeparator, NodeIdSeparator);

            // Step 2: Remove everything from first inner class or method delimiter onwards
            normalized = RemoveInnerTypesAndMethods(normalized);

            // Step 3: If no filename given, return as-is
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return normalized;
            }

            // Step 4: Replace the simple class name with the filename-derived main type
            return ReplaceWithMainType(normalized, fileName);
        }

        /// <summary>
        /// Determines the main type path for a graph node.
        /// For methods, recursively looks up the parent type.
        /// For types, resolves to the main type using filename information.
        /// </summary>
        public string NodeIdToMainType(Node node)
        {
            if (node == null)
            {
                return null;
            }

            // For methods: recurse to parent type
            if (node.Type == "Method")
            {
                return node.Parent != null ? NodeIdToMainType(node.Parent) : null;
            }

            // For type nodes: resolve to main type
            if (IsTypeNode(node.Type))
            {
                return ResolveMainType(node.ID, node.Filename);
            }

            // Other node types are not indexed
            return null;
        }

        #region Helper Methods

        /// <summary>
        /// Java type node types in GLX graphs.
        /// </summary>
        private static readonly HashSet<string> TypeNodeTypes = new()
        {
            "Class",
            "Interface",
            "Class_Template",
            "Interface_Template"
        };

        private static bool IsTypeNode(string nodeType)
        {
            return TypeNodeTypes.Contains(nodeType);
        }

        /// <summary>
        /// Removes inner class and method suffixes from a qualified name.
        /// Example: "pkg.Outer$Inner#method" → "pkg.Outer"
        /// </summary>
        private static string RemoveInnerTypesAndMethods(string qualifiedName)
        {
            int innerClassIndex = qualifiedName.IndexOf(InnerClassDelimiter);
            int methodIndex = qualifiedName.IndexOf(MethodDelimiter);

            // Find the first delimiter (if any)
            int cutIndex = qualifiedName.Length;
            if (innerClassIndex >= 0) cutIndex = Math.Min(cutIndex, innerClassIndex);
            if (methodIndex >= 0) cutIndex = Math.Min(cutIndex, methodIndex);

            return qualifiedName.Substring(0, cutIndex);
        }

        /// <summary>
        /// Replaces the simple class name in a qualified name with the main type from the filename.
        /// Example: "pkg.NonMain" + "Main.java" → "pkg.Main"
        /// </summary>
        private static string ReplaceWithMainType(string qualifiedName, string fileName)
        {
            string mainClassName = Path.GetFileNameWithoutExtension(fileName);

            int lastDotIndex = qualifiedName.LastIndexOf(NodeIdSeparator);

            if (lastDotIndex < 0)
            {
                // No package, just return the main class name
                return mainClassName;
            }

            string packageName = qualifiedName.Substring(0, lastDotIndex);
            return packageName + NodeIdSeparator + mainClassName;
        }

        /// <summary>
        /// Resolves a node's ID to its main type identifier.
        /// Handles inner classes and non-main top-level types.
        /// </summary>
        /// <remarks>
        /// Java allows multiple top-level types in one file, but only one can be public (the "main type").
        /// The filename must match the main type name (e.g., MyClass.java contains public class MyClass).
        ///
        /// Examples:
        /// - "pkg.Main" in Main.java → "pkg.Main" (already main type)
        /// - "pkg.NonMain" in Main.java → "pkg.Main" (non-main top-level type)
        /// - "pkg.Main$Inner" in Main.java → "pkg.Main" (inner class)
        /// </remarks>
        private static string ResolveMainType(string nodeId, string filename)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(filename))
            {
                return nodeId;
            }

            // Step 1: Remove inner class suffix if present
            string outerType = RemoveInnerTypesAndMethods(nodeId);

            // Step 2: Check if the simple type name matches the filename
            string simpleTypeName = GetSimpleName(outerType);
            string mainTypeFromFile = Path.GetFileNameWithoutExtension(filename);

            if (simpleTypeName == mainTypeFromFile)
            {
                // This is already the main type
                return outerType;
            }

            // Step 3: Non-main type - replace with main type from filename
            return ReplaceWithMainType(outerType, filename);
        }

        /// <summary>
        /// Extracts the simple name (last component) from a qualified name.
        /// Example: "org.example.MyClass" → "MyClass"
        /// </summary>
        private static string GetSimpleName(string qualifiedName)
        {
            int lastDotIndex = qualifiedName.LastIndexOf(NodeIdSeparator);
            return lastDotIndex < 0
                ? qualifiedName
                : qualifiedName.Substring(lastDotIndex + 1);
        }

        #endregion
    }
}
