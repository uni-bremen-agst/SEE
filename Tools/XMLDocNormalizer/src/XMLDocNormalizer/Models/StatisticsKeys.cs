namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Provides stable keys for run-level statistics totals.
    /// </summary>
    /// <remarks>
    /// These keys are used to store denominators for coverage metrics in <c>RunResult.Totals</c>.
    /// Keys are stable and culture-invariant and must not be changed once they are used in reports,
    /// because report consumers may depend on them.
    ///
    /// Naming conventions:
    /// - Keys ending with <c>Total</c> represent absolute counts (denominators).
    /// - Keys use PascalCase and describe the declaration category being counted.
    /// </remarks>
    internal static class StatisticsKeys
    {
        /// <summary>
        /// Total number of namespace declarations (file-scoped and block-scoped).
        /// </summary>
        public const string NamespaceDeclarationsTotal = "NamespaceDeclarationsTotal";

        /// <summary>
        /// Total number of class declarations.
        /// </summary>
        public const string ClassDeclarationsTotal = "ClassDeclarationsTotal";

        /// <summary>
        /// Total number of struct declarations.
        /// </summary>
        public const string StructDeclarationsTotal = "StructDeclarationsTotal";

        /// <summary>
        /// Total number of interface declarations.
        /// </summary>
        public const string InterfaceDeclarationsTotal = "InterfaceDeclarationsTotal";

        /// <summary>
        /// Total number of enum declarations.
        /// </summary>
        public const string EnumDeclarationsTotal = "EnumDeclarationsTotal";

        /// <summary>
        /// Total number of delegate declarations.
        /// </summary>
        public const string DelegateDeclarationsTotal = "DelegateDeclarationsTotal";

        /// <summary>
        /// Total number of record declarations (class records).
        /// </summary>
        public const string RecordDeclarationsTotal = "RecordDeclarationsTotal";

        /// <summary>
        /// Total number of record struct declarations.
        /// </summary>
        public const string RecordStructDeclarationsTotal = "RecordStructDeclarationsTotal";

        /// <summary>
        /// Total number of enum member declarations.
        /// </summary>
        public const string EnumMembersTotal = "EnumMembersTotal";

        /// <summary>
        /// Total number of method declarations.
        /// </summary>
        public const string MethodsTotal = "MethodsTotal";

        /// <summary>
        /// Total number of constructor declarations.
        /// </summary>
        public const string ConstructorsTotal = "ConstructorsTotal";

        /// <summary>
        /// Total number of property declarations.
        /// </summary>
        public const string PropertiesTotal = "PropertiesTotal";

        /// <summary>
        /// Total number of indexer declarations.
        /// </summary>
        public const string IndexersTotal = "IndexersTotal";

        /// <summary>
        /// Total number of event declarations (event and event-field).
        /// </summary>
        public const string EventsTotal = "EventsTotal";

        /// <summary>
        /// Total number of field declarations.
        /// </summary>
        public const string FieldsTotal = "FieldsTotal";

        /// <summary>
        /// Total number of operator declarations.
        /// </summary>
        public const string OperatorsTotal = "OperatorsTotal";

        /// <summary>
        /// Total number of conversion operator declarations.
        /// </summary>
        public const string ConversionsTotal = "ConversionsTotal";

        /// <summary>
        /// Total number of value parameters across all counted members (methods, constructors, indexers, operators, delegates).
        /// </summary>
        public const string ParametersTotal = "ParametersTotal";

        /// <summary>
        /// Total number of type parameters across all counted generic declarations.
        /// </summary>
        public const string TypeParametersTotal = "TypeParametersTotal";

        /// <summary>
        /// Total number of members that require a &lt;returns&gt; documentation tag
        /// (e.g. non-void methods, delegates, operators, conversions).
        /// </summary>
        public const string ReturnsRequiredTotal = "ReturnsRequiredTotal";
    }
}