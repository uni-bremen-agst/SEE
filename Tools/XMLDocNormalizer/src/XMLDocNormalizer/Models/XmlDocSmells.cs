namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Central registry of all XML documentation smells.
    /// </summary>
    internal static class XmlDocSmells
    {
        #region General / Structure
        /// <summary>
        /// DOC100 – No XML documentation comment is present for the member or type.
        /// </summary>
        public static readonly XmlDocSmell MissingDocumentation =
            new("DOC100", "XML documentation is missing.", Severity.Error);

        /// <summary>
        /// DOC110 – Unknown or misspelled XML documentation tag.
        /// </summary>
        public static readonly XmlDocSmell UnknownTag =
            new("DOC110", "Unknown XML documentation tag <{0}>.", Severity.Warning);

        /// <summary>
        /// DOC120 – Missing end tag (unclosed XML element).
        /// </summary>
        public static readonly XmlDocSmell MissingEndTag =
            new("DOC120", "Missing end tag (unclosed XML element).", Severity.Error);
        #endregion

        #region summary
        /// <summary>
        /// DOC200 – The summary-tag is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingSummary =
            new("DOC200", "<summary> is missing.", Severity.Error);

        /// <summary>
        /// DOC210 – The summary-tag exists but contains no meaningful content.
        /// </summary>
        public static readonly XmlDocSmell EmptySummary =
            new("DOC210", "<summary> is empty.", Severity.Error);
        #endregion

        #region param/paramref/typeparamref
        /// <summary>
        /// DOC300 – param-tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingName =
            new("DOC300", "<param> tag is missing required 'name' attribute.", Severity.Error);

        /// <summary>
        /// DOC310 – A parameter has no corresponding param-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTag =
            new("DOC310", "Missing <param> documentation for parameter '{0}'.", Severity.Error);

        /// <summary>
        /// DOC320 – A param-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescription =
            new("DOC320", "<param> documentation for parameter '{0}' is empty.", Severity.Error);

        /// <summary>
        /// DOC330 – A param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTag =
            new("DOC330", "<param> references unknown parameter '{0}'.", Severity.Warning);

        /// <summary>
        /// DOC340 – paramref-tag contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmpty =
            new("DOC340", "<paramref> should be an empty element, e.g. <paramref name=\"x\"/>.", Severity.Error);

        /// <summary>
        /// DOC350 – Multiple <param> tags exist for the same parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTag =
            new(
                "DOC350",
                "Duplicate <param> documentation for parameter '{0}'.",
                Severity.Warning);
        #endregion

        #region typeparam
        /// <summary>
        /// DOC400 – typeparam-tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingName =
            new("DOC400", "<typeparam> tag is missing required 'name' attribute.", Severity.Error);

        /// <summary>
        /// DOC410 – A generic type parameter has no corresponding typeparam-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTag =
            new("DOC410", "Missing <typeparam> documentation for type parameter '{0}'.", Severity.Error);

        /// <summary>
        /// DOC420 – A typeparam-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescription =
            new("DOC420", "<typeparam> documentation for type parameter '{0}' is empty.", Severity.Error);

        /// <summary>
        /// DOC430 – A typeparam-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamTag =
            new("DOC430", "<typeparam> references unknown type parameter '{0}'.", Severity.Warning);

        /// <summary>
        /// DOC440 - typeparamref-tag contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefNotEmpty =
            new("DOC440", "<typeparamref> should be an empty element, e.g. <typeparamref name=\"T\"/>.", Severity.Error);

        /// <summary>
        /// DOC450 – Multiple <typeparam> tags exist for the same parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateTypeParamTag =
            new(
                "DOC450",
                "Duplicate <typeparam> documentation for type parameter '{0}'.",
                Severity.Warning);

        #endregion

        #region returns
        /// <summary>
        /// DOC500 – A non-void member has no returns documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingReturns =
            new("DOC500", "<returns> is missing.", Severity.Error);

        /// <summary>
        /// DOC510 – The returns-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturns =
            new("DOC510", "<returns> is empty.", Severity.Error);

        /// <summary>
        /// DOC520 – A void member contains a returns-tag, which is inconsistent with its return type.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnVoidMember =
            new("DOC520", "<returns> must not be used for void members.", Severity.Warning);

        /// <summary>
        /// DOC530 – Multiple <returns> tags exist.
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsTag =
            new("DOC530", "Duplicate <returns> tag.", Severity.Warning);
        #endregion

        #region exception
        /// <summary>
        /// DOC600 – exception-tag missing required 'cref' attribute.
        /// </summary>
        public static readonly XmlDocSmell ExceptionMissingCref =
            new("DOC600", "<exception> tag is missing required 'cref' attribute.", Severity.Error);

        /// <summary>
        /// DOC610 – An exception is directly thrown but not documented with an exception-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingExceptionTag =
            new("DOC610", "Missing <exception> documentation for '{0}'.", Severity.Error);

        /// <summary>
        /// DOC620 – An exception-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyExceptionDescription =
            new("DOC620", "<exception> documentation for '{0}' is empty.", Severity.Error);

        /// <summary>
        /// DOC630 – An exception-tag documents an exception that is not directly thrown by the member.
        /// </summary>
        /// <remarks>
        /// This is a best-effort semantic check. The member may still throw the exception indirectly via calls to other members.
        /// </remarks>
        public static readonly XmlDocSmell ExceptionTagWithoutDirectThrow =
            new("DOC630", "<exception> documents '{0}', but no direct throw was detected.", Severity.Warning);

        /// <summary>
        /// DOC640 – A rethrow statement ('throw;') was detected and the exception type cannot be reliably inferred.
        /// </summary>
        public static readonly XmlDocSmell RethrowCannotInferException =
            new("DOC640", "Rethrow detected; cannot infer exception type reliably.", Severity.Warning);

        /// <summary>
        /// DOC650 – Multiple <exception> tags exist for the same exception cref.
        /// </summary>
        public static readonly XmlDocSmell DuplicateExceptionTag =
            new(
                "DOC450",
                "Duplicate <exception> documentation for exception cref '{0}'.",
                Severity.Warning);
        #endregion

        #region inheritdoc / Overrides / Interface Implementations
        /// <summary>
        /// DOC700 – Documentation consists only of &lt;inheritdoc/&gt; without any additional content.
        /// </summary>
        public static readonly XmlDocSmell InheritdocOnly =
            new("DOC700", "Documentation uses only <inheritdoc/>. Consider documenting differences.", Severity.Warning);

        /// <summary>
        /// DOC710 – Override or explicit interface implementation should use &lt;inheritdoc/&gt; or provide full documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingInheritdocOnOverride =
            new("DOC710", "Override/implementation should use <inheritdoc/> or provide full documentation.", Severity.Warning);
        #endregion
    }
}
