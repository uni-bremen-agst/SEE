using SEE.DataModel.DG;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Strategy interface for normalizing code element identifiers from external tools
    /// (like code coverage analyzers) into a format that matches graph node IDs and enables
    /// efficient source range indexing.
    /// 
    /// Different programming languages and tools use different conventions for representing
    /// code elements (packages, classes, methods). This interface abstracts those differences
    /// so that metrics can be correctly attributed to nodes in the graph regardless of the
    /// source language or tool.
    /// </summary>
    /// <remarks>
    /// Typical workflow:
    /// 1. External tool (e.g., JaCoCo, dotCover) reports findings with tool-specific paths
    /// 2. <see cref="FindingPathToNodeId"/> normalizes these paths to match graph node IDs
    /// 3. <see cref="FindingPathToMainType"/> resolves to the main type for indexing purposes
    /// 4. <see cref="NodeIdToMainType"/> provides consistent main type resolution from graph nodes
    /// 
    /// Example implementations: JavaIndexNodeStrategy, CSharpIndexNodeStrategy
    /// </remarks>
    public interface IIndexNodeStrategy
    {
        /// <summary>
        /// Converts a tool-specific finding path to the main type identifier used for indexing.
        /// This resolves nested types, inner classes, and methods to their containing main type.
        /// </summary>
        /// <param name="fullPath">
        /// The complete path as reported by the external tool (e.g., code coverage analyzer).
        /// May include package/namespace separators, class names, inner classes, and method identifiers
        /// in a tool-specific format.
        /// Examples:
        /// - Java/JaCoCo: "com/example/MyClass$InnerClass#method"
        /// - C#/dotCover: "MyNamespace.MyClass+NestedClass.Method"
        /// </param>
        /// <param name="filename">
        /// The source filename where the code element is declared (e.g., "MyClass.java", "MyClass.cs").
        /// Used to identify the main type in languages where multiple top-level types can exist
        /// in a single file. May be null if not available.
        /// </param>
        /// <returns>
        /// The fully qualified main type identifier suitable for source range indexing.
        /// Returns null if the path is invalid or represents a compiler-generated element.
        /// Examples:
        /// - Java: "com.example.MyClass" (even for inner classes or methods)
        /// - C#: "MyNamespace.MyClass" (even for nested types or methods)
        /// </returns>
        /// <remarks>
        /// The "main type" concept is important for languages like Java where:
        /// - Multiple top-level types can exist in one file, but only one is public (the main type)
        /// - The filename must match the main type name (e.g., MyClass.java contains class MyClass)
        /// - Inner/nested types should resolve to their outermost main type for indexing
        /// 
        /// This ensures that metrics reported at the method or inner class level are correctly
        /// attributed to the primary type that represents the file in the graph.
        /// </remarks>
        string FindingPathToMainType(string fullPath, string filename);

        /// <summary>
        /// Resolves a graph node to its main type identifier for consistent indexing.
        /// This method ensures that nodes representing methods, inner classes, or other
        /// nested code elements can be looked up in the source range index.
        /// </summary>
        /// <param name="node">
        /// A node from the graph that represents a code element (class, method, etc.).
        /// Must not be null and should have an ID and Type property.
        /// </param>
        /// <returns>
        /// The fully qualified main type identifier that corresponds to this node.
        /// Returns null if the node type is not indexable (e.g., not a type or method).
        /// Examples:
        /// - Method node "com.example.MyClass.~myMethod()" → "com.example.MyClass"
        /// - Inner class node "com.example.Outer$Inner" → "com.example.Outer"
        /// - Main class node "com.example.MyClass" → "com.example.MyClass"
        /// </returns>
        /// <remarks>
        /// This method typically:
        /// 1. For method nodes: Recursively resolves to the parent type's main type
        /// 2. For type nodes: Strips inner class indicators and resolves to the main type
        /// 3. For other nodes: Returns null (not indexable)
        /// 
        /// The implementation should use the node's ID, Type, Filename, and Parent properties
        /// to make the determination.
        /// </remarks>
        string NodeIdToMainType(Node node);

        /// <summary>
        /// Converts a tool-specific finding path to a graph node ID format.
        /// This performs basic normalization like replacing path separators and
        /// converting method indicators to match the graph's naming conventions.
        /// </summary>
        /// <param name="fullPath">
        /// The complete path as reported by the external tool.
        /// Examples:
        /// - Java/JaCoCo: "com/example/MyClass#method"
        /// - C#/dotCover: "MyNamespace.MyClass.Method"
        /// </param>
        /// <returns>
        /// A normalized identifier that should match a node ID in the graph.
        /// Returns null if the path is invalid or represents an element that shouldn't be indexed.
        /// Examples:
        /// - Java: "com/example/MyClass#method" → "com.example.MyClass.~method()"
        /// - Java constructor: "com/example/MyClass#&lt;init&gt;" → "com.example.MyClass.~MyClass()"
        /// - C#: "MyNamespace.MyClass.Method" → "MyNamespace.MyClass.Method"
        /// </returns>
        /// <remarks>
        /// This method handles language-specific conventions such as:
        /// - Path separator normalization (/ vs . vs ::)
        /// - Method indicator conversion (# → .~ in Java)
        /// - Constructor special names (&lt;init&gt; → actual class name)
        /// - Inner class delimiters ($ in Java, + in C#)
        /// 
        /// The result should exactly match the <see cref="GraphElement.ID"/> (Linkage.Name)
        /// of the corresponding node in the graph, enabling direct node lookup.
        /// 
        /// Note: This method does NOT resolve to main types - it preserves the full
        /// qualified name including inner classes and method names. Use 
        /// <see cref="FindingPathToMainType"/> for index key generation.
        /// </remarks>
        string FindingPathToNodeId(string fullPath);
    }
}