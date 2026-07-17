namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Describes the source declaration and documentation subject a finding belongs to.
    /// </summary>
    /// <param name="OwnerKind">
    /// The kind of source declaration that owns the XML documentation comment.
    /// Examples are Method, Constructor, Property, Class, Struct, Interface, Enum, Delegate, Field, Event, or Namespace.
    /// </param>
    /// <param name="SubjectKind">
    /// The concrete documentation subject affected by the finding.
    /// Examples are Declaration, Parameter, TypeParameter, ReturnValue, SummaryTag, RemarksTag, TagOrder, or NamespaceDocumentation.
    /// </param>
    /// <param name="Accessibility">
    /// The declared or inferred accessibility of the owner declaration.
    /// Examples are Public, Private, Internal, Protected, ProtectedInternal, PrivateProtected, NotApplicable, or Unknown.
    /// </param>
    /// <param name="SymbolName">
    /// The source symbol name of the owner declaration.
    /// Examples are a method name, property name, type name, field name, event name, or namespace name.
    /// </param>
    /// <param name="ContainingType">
    /// The containing type name.
    /// Nested types are represented as a dotted path.
    /// If no containing type exists, the value is None.
    /// </param>
    /// <param name="ContainingNamespace">
    /// The containing namespace name.
    /// If no namespace declaration exists, the value is GlobalNamespace.
    /// </param>
    /// <param name="TargetName">
    /// The concrete affected target name, if one exists.
    /// For example, this can be the name of a parameter, type parameter, XML tag, or namespace.
    /// </param>
    /// <param name="ProjectName">
    /// The analyzed project name, if available.
    /// </param>
    /// <param name="IsGenerated">
    /// Indicates whether the source file appears to be generated code, if this can be inferred.
    /// </param>
    /// <param name="IsTestFile">
    /// Indicates whether the source file appears to be test code, if this can be inferred.
    /// </param>
    internal sealed record FindingContext(
        string OwnerKind,
        string SubjectKind,
        string Accessibility,
        string SymbolName,
        string ContainingType,
        string ContainingNamespace,
        string? TargetName = null,
        string? ProjectName = null,
        bool? IsGenerated = null,
        bool? IsTestFile = null)
    {
        /// <summary>
        /// Gets a fallback context for findings where no source declaration context is available yet.
        /// </summary>
        /// <value>
        /// The fallback context used when no source declaration context is available.
        /// </value>
        /// <remarks>
        /// This value keeps existing detectors compatible while detector-specific context is added incrementally.
        /// </remarks>
        public static FindingContext Unknown { get; } = new(
            OwnerKind: "Unknown",
            SubjectKind: "Unknown",
            Accessibility: "Unknown",
            SymbolName: "Unknown",
            ContainingType: "Unknown",
            ContainingNamespace: "Unknown");
    }
}
