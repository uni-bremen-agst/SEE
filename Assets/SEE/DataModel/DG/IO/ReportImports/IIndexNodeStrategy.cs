namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Strategy interface for normalizing code element identifiers from external tools
    /// (e.g. static analyzers, coverage tools) into a unified logical identifier space
    /// that is shared by both findings and graph nodes.
    ///
    /// The central idea is that the same normalization logic is applied on:
    /// - the <see cref="Finding.FullPath"/> coming from a report, and
    /// - the identifiers of <see cref="Node"/> instances in the graph.
    ///
    /// Both sides must yield exactly the same logical identifier for the same
    /// conceptual code container (typically a class or file-level type).
    ///
    /// Method-level information is intentionally normalized to the enclosing
    /// type (class / inner class), because methods cannot be identified
    /// reliably by name alone (e.g. overloads).
    /// Precise method attribution is handled separately via source-range indexing.
    /// </summary>

    /// Typical workflow:
    /// 1. An external tool reports a finding with a tool-specific path
    ///    (possibly including packages, classes, inner classes, and methods).
    /// 2. <see cref="ToLogicalIdentifier(string)"/> maps this path to a logical
    ///    container identifier (usually the enclosing class or main type).
    /// 3. <see cref="ToLogicalIdentifier(Node)"/> maps graph nodes to the same
    ///    logical container identifier.
    /// 4. Both identifiers are used as keys for lookup and source-range indexing
    ///    in <see cref="MetricApplier"/>.
    ///
    /// As a result, findings referring to methods are first associated with
    /// their enclosing type, and only then resolved to a concrete method
    /// using source-range information (if available).
    /// </remarks>
    public interface IIndexNodeStrategy
    {
        /// <summary>
        /// Resolves a tool-specific finding path to a logical identifier.
        ///
        /// The returned identifier represents the enclosing code container
        /// (e.g. class or file-level type) and must be identical to the identifier
        /// produced by <see cref="ToLogicalIdentifier(Node)"/> for the corresponding
        /// graph node.
        ///
        /// Method names and signatures are intentionally removed or normalized,
        /// because methods cannot be matched safely by name alone.
        /// Method-level precision is achieved later via source-range lookup.
        /// </summary>
        /// <param name="fullPath">
        /// The raw path from the report (e.g. file path, qualified class name,
        /// or tool-specific notation including methods).
        /// </param>
        /// <returns>
        /// A fully qualified logical identifier representing the enclosing type.
        ///
        /// Examples:
        /// - "pkg/Class.java"            → "pkg.Class"
        /// - "pkg/Outer$Inner#method"   → "pkg.Outer.Inner"
        ///
        /// The returned value must match the result of
        /// <see cref="ToLogicalIdentifier(Node)"/> for the corresponding graph node.
        /// </returns>
        string ToLogicalIdentifier(string fullPath);

        /// <summary>
        /// Resolves a graph node to its logical identifier.
        ///
        /// This method applies the same normalization rules as
        /// <see cref="ToLogicalIdentifier(string)"/>, ensuring that
        /// findings and nodes are mapped into the same identifier space.
        ///
        /// Method nodes are intentionally mapped to their enclosing type,
        /// because method names alone are insufficient for reliable identification.
        /// Actual method matching is deferred to source-range indexing.
        /// </summary>
        /// <param name="node">
        /// A graph node representing a code element (e.g. class, method, inner class).
        /// </param>
        /// <returns>
        /// A fully qualified logical identifier representing the enclosing type,
        /// or null if the node should not participate in indexing.
        ///
        /// Examples:
        /// - Method node "com.example.MyClass.myMethod()" → "com.example.MyClass"
        /// - Inner class node "com.example.Outer$Inner"   → "com.example.Outer.Inner"
        /// </returns>

        /// Typical behavior:
        /// 1. Method nodes are resolved recursively to their parent type.
        /// 2. Inner classes are collapsed to their enclosing top-level type
        ///    if required by the language or tool conventions.
        /// 3. Non-code nodes (e.g. folders, namespaces) usually return null.
        ///
        /// The result must be consistent with
        /// <see cref="ToLogicalIdentifier(string)"/> to allow reliable
        /// source-range-based resolution in <see cref="MetricApplier"/>.
        /// </remarks>
        string ToLogicalIdentifier(Node node);


        /// <summary>
        /// Converts a <see cref="Finding.FullPath"/> into a method-aware logical identifier
        /// by normalizing separators to '.' while preserving method-level information.
        ///
        /// This identifier is used exclusively as a fallback lookup key in
        /// <see cref="MetricApplier"/> when source-range-based resolution fails.
        ///
        /// The index (typeIndex) only stores container nodes (e.g., classes, files),
        /// because method nodes are not uniquely identifiable by name due to overloads.
        /// <see cref="ToLogicalIdentifier(Node)"/> therefore intentionally strips method information
        /// and maps method nodes to their enclosing container.
        /// When a finding refers to a method but cannot be resolved via
        /// <see cref="SourceRangeIndex"/> (e.g., because no start line is provided),
        /// this method allows performing a safe fallback lookup without silently
        /// assigning the metric to an unrelated method node.
        /// This approach guarantees that:
        /// Metrics are never assigned to an incorrect method due to ambiguous identifiers.
        /// Method-level metrics are only applied when a precise source-range match exists.
        /// <param name="fullPath">
        /// The raw path emitted by the analysis tool, potentially including method information.
        /// </param>
        /// </summary>
        /// <returns>
        /// A normalized identifier suitable for container-level lookup in typeIndex.
        /// </returns>
        string ToFullIdentifier(string fullPath);

    }
}
