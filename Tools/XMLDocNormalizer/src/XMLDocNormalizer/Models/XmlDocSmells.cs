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
        public static readonly XmlDocSmell MissingDocumentation = new(
            "DOC100",
            "XML documentation is missing.",
            Severity.Error
        );

        /// <summary>
        /// DOC110 – Unknown or misspelled XML documentation tag.
        /// </summary>
        public static readonly XmlDocSmell UnknownTag = new(
            "DOC110",
            "Unknown XML documentation tag <{0}>.",
            Severity.Warning
        );

        /// <summary>
        /// DOC115 – XML documentation tag is syntactically invalid (no valid tag name).
        /// </summary>
        public static readonly XmlDocSmell InvalidXmlTag = new(
            "DOC115",
            "Invalid XML documentation tag '{0}'.",
            Severity.Error
        );

        /// <summary>
        /// DOC120 – Missing end tag (unclosed XML element).
        /// </summary>
        public static readonly XmlDocSmell MissingEndTag = new(
            "DOC120",
            "Missing end tag (unclosed XML element).",
            Severity.Error
        );

        #endregion

        #region summary / remarks / value
        /// <summary>
        /// DOC200 – The summary-tag is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingSummary = new(
            "DOC200",
            "<summary> is missing.",
            Severity.Error
        );

        /// <summary>
        /// DOC210 – The summary-tag exists but contains no meaningful content.
        /// </summary>
        public static readonly XmlDocSmell EmptySummary = new(
            "DOC210",
            "<summary> is empty.",
            Severity.Error
        );

        /// <summary>
        /// DOC220 – Multiple summary tags exist.
        /// Only one summary element is allowed per member.
        /// </summary>
        public static readonly XmlDocSmell DuplicateSummaryTag = new(
            "DOC220",
            "Duplicate <summary> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC230 – Multiple remarks tags exist.
        /// Consider consolidating remarks into a single remarks section.
        /// </summary>
        public static readonly XmlDocSmell DuplicateRemarksTag = new(
            "DOC230",
            "Duplicate <remarks> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC240 – remarks tag exists but contains no meaningful content.
        /// </summary>
        public static readonly XmlDocSmell EmptyRemarks = new(
            "DOC240",
            "<remarks> is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC250 – Multiple value tags exist on a property.
        /// Only one <value> element is allowed.
        /// </summary>
        public static readonly XmlDocSmell DuplicateValueTag = new(
            "DOC250",
            "Duplicate <value> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC260 – value tag exists but the documented member is not a property.
        /// The <value> element should only be used for properties.
        /// </summary>
        public static readonly XmlDocSmell ValueTagOnNonProperty = new(
            "DOC260",
            "<value> should only be used on properties.",
            Severity.Warning
        );

        #endregion

        #region param/paramref/typeparamref
        /// <summary>
        /// DOC300 – param-tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingName = new(
            "DOC300",
            "<param> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC310 – A parameter has no corresponding param-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTag = new(
            "DOC310",
            "Missing <param> documentation for parameter '{0}'.",
            Severity.Error
        );

        /// <summary>
        /// DOC320 – A param-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescription = new(
            "DOC320",
            "<param> documentation for parameter '{0}' is empty.",
            Severity.Error
        );

        /// <summary>
        /// DOC330 – A param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTag = new(
            "DOC330",
            "<param> references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC340 – paramref-tag contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmpty = new(
            "DOC340",
            "<paramref> should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC350 – Multiple param tags exist for the same parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTag = new(
            "DOC350",
            "Duplicate <param> documentation for parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC360 – param tags are not ordered according to the parameter list.
        /// Consider aligning documentation order with the method signature.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatch = new(
            "DOC360",
            "<param> tags should follow the parameter order of the member.",
            Severity.Suggestion
        );

        #endregion

        #region typeparam
        /// <summary>
        /// DOC400 – typeparam-tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingName = new(
            "DOC400",
            "<typeparam> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC410 – A generic type parameter has no corresponding typeparam-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTag = new(
            "DOC410",
            "Missing <typeparam> documentation for type parameter '{0}'.",
            Severity.Error
        );

        /// <summary>
        /// DOC420 – A typeparam-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescription = new(
            "DOC420",
            "<typeparam> documentation for type parameter '{0}' is empty.",
            Severity.Error
        );

        /// <summary>
        /// DOC430 – A typeparam-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamTag = new(
            "DOC430",
            "<typeparam> references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC440 - typeparamref-tag contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefNotEmpty = new(
            "DOC440",
            "<typeparamref> should be an empty element, e.g. <typeparamref name=\"T\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC450 – Multiple typeparam tags exist for the same parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateTypeParamTag = new(
            "DOC450",
            "Duplicate <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        #endregion

        #region returns
        /// <summary>
        /// DOC500 – A non-void member has no returns documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingReturns = new(
            "DOC500",
            "<returns> is missing.",
            Severity.Error
        );

        /// <summary>
        /// DOC510 – The returns-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturns = new(
            "DOC510",
            "<returns> is empty.",
            Severity.Error
        );

        /// <summary>
        /// DOC520 – A void member contains a returns-tag, which is inconsistent with its return type.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnVoidMember = new(
            "DOC520",
            "<returns> must not be used for void members.",
            Severity.Warning
        );

        /// <summary>
        /// DOC530 – Multiple returns tags exist.
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsTag = new(
            "DOC530",
            "Duplicate <returns> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC540 – returns is used on a property without a getter.
        /// A property without a getter does not produce a return value.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnWriteOnlyProperty = new(
            "DOC540",
            "<returns> must not be used on write-only properties.",
            Severity.Warning
        );

        #endregion

        #region exception
        /// <summary>
        /// DOC600 – exception-tag missing required 'cref' attribute.
        /// </summary>
        public static readonly XmlDocSmell ExceptionMissingCref = new(
            "DOC600",
            "<exception> tag is missing required 'cref' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC610 – An exception is directly thrown but not documented with an exception-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingExceptionTag = new(
            "DOC610",
            "Missing <exception> documentation for '{0}'.",
            Severity.Error
        );

        /// <summary>
        /// DOC620 – An exception-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyExceptionDescription = new(
            "DOC620",
            "<exception> documentation for '{0}' is empty.",
            Severity.Error
        );

        /// <summary>
        /// DOC630 – An exception tag documents an exception that is not directly thrown by the member.
        /// </summary>
        /// <remarks>
        /// This is a best-effort semantic check and only applies to members with an executable body
        /// (block body or expression-bodied members). The member may still throw the exception indirectly
        /// via calls to other members.
        /// </remarks>
        public static readonly XmlDocSmell ExceptionTagWithoutDirectThrow = new(
            "DOC630",
            "<exception> documents '{0}', but no direct throw was detected.",
            Severity.Warning
        );

        /// <summary>
        /// DOC640 – A rethrow statement ('throw;') was detected and the exception type cannot be reliably inferred.
        /// </summary>
        public static readonly XmlDocSmell RethrowCannotInferException = new(
            "DOC640",
            "Rethrow detected; cannot infer exception type reliably.",
            Severity.Warning
        );

        /// <summary>
        /// DOC650 – Multiple <exception> tags exist for the same exception cref.
        /// </summary>
        public static readonly XmlDocSmell DuplicateExceptionTag = new(
            "DOC650",
            "Duplicate <exception> documentation for exception cref '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC660 – exception cref could not be resolved to a known type.
        /// </summary>
        /// <remarks>
        /// This is a semantic check that requires type resolution. The detector attempts to resolve the cref
        /// to a type symbol and reports this smell if resolution fails.
        /// </remarks>
        public static readonly XmlDocSmell InvalidExceptionCref = new(
            "DOC660",
            "<exception> cref '{0}' could not be resolved to a type.",
            Severity.Warning
        );

        /// <summary>
        /// DOC670 – exception cref does not reference an exception type.
        /// </summary>
        /// <remarks>
        /// This is a semantic check. The cref can be resolved to a type, but the referenced type is not derived
        /// from <see cref="System.Exception"/>.
        /// </remarks>
        public static readonly XmlDocSmell ExceptionCrefNotExceptionType = new(
            "DOC670",
            "<exception> cref '{0}' does not reference an exception type.",
            Severity.Warning
        );

        /// <summary>
        /// DOC680 – exception tag exists on a member without an executable body.
        /// Exception documentation should only be used when the member can throw exceptions directly.
        /// </summary>
        /// <remarks>
        /// This applies to interface members, abstract members, or extern declarations.
        /// </remarks>
        public static readonly XmlDocSmell ExceptionTagOnNonExecutableMember = new(
            "DOC680",
            "<exception> should not be used on members without an executable body.",
            Severity.Warning
        );

        #endregion

        #region inheritdoc / Overrides / Interface Implementations
        /// <summary>
        /// DOC700 – Documentation consists only of inheritdoc without any additional content.
        /// Even when inheriting documentation, consider documenting behavioral differences
        /// or implementation details specific to this member.
        /// </summary>
        public static readonly XmlDocSmell InheritdocOnly = new(
            "DOC700",
            "Documentation uses only <inheritdoc/>. Consider documenting differences.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC710 – Override or explicit interface implementation should use inheritdoc
        /// or provide full documentation.
        /// Missing documentation on overrides may result in incomplete API documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingInheritdocOnOverride = new(
            "DOC710",
            "Override/implementation should use <inheritdoc/> or provide full documentation.",
            Severity.Warning
        );

        /// <summary>
        /// DOC720 – inheritdoc is used but no valid inheritance source exists.
        /// No base member, implemented interface member, or cref target could be found.
        /// This usually indicates incorrect usage or copy-paste documentation.
        /// </summary>
        public static readonly XmlDocSmell InheritdocNoSource = new(
            "DOC720",
            "<inheritdoc/> used but no inheritance source found.",
            Severity.Warning
        );

        /// <summary>
        /// DOC721 – inheritdoc cref="..." references a member that cannot be resolved.
        /// The cref target does not exist or is not accessible, resulting in broken documentation inheritance.
        /// </summary>
        public static readonly XmlDocSmell InvalidInheritdocCref = new(
            "DOC721",
            "<inheritdoc cref> target cannot be resolved.",
            Severity.Error
        );

        /// <summary>
        /// DOC730 – inheritdoc is redundant because the inherited member
        /// does not contain any XML documentation.
        /// Inheriting empty documentation provides no value.
        /// </summary>
        public static readonly XmlDocSmell RedundantInheritdoc = new(
            "DOC730",
            "<inheritdoc/> is redundant because the base member has no documentation.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC740 – inheritdoc is combined with additional documentation
        /// that may conflict with the inherited content.
        /// This can lead to inconsistent or misleading API documentation.
        /// </summary>
        public static readonly XmlDocSmell ConflictingInheritdoc = new(
            "DOC740",
            "<inheritdoc/> combined with potentially conflicting documentation.",
            Severity.Warning
        );

        /// <summary>
        /// DOC750 – inheritdoc or inheritdoc cref="..." is used,
        /// but the current member defines additional parameters that are not documented.
        /// New parameters must be explicitly documented to ensure complete API documentation.
        /// </summary>
        public static readonly XmlDocSmell InheritdocMissingNewParams = new(
            "DOC750",
            "New parameters are not documented when using <inheritdoc/>.",
            Severity.Warning
        );

        /// <summary>
        /// DOC760 – inheritdoc is used on a sealed override.
        /// Since the member cannot be overridden further, consider providing
        /// explicit documentation instead of relying solely on inheritance.
        /// </summary>
        public static readonly XmlDocSmell SealedOverrideInheritdoc = new(
            "DOC760",
            "Sealed override uses <inheritdoc/>. Consider explicit documentation.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC770 – Multiple possible inheritance sources exist for inheritdoc.
        /// When a member implements multiple interface members with identical signatures,
        /// the documentation source may be ambiguous.
        /// </summary>
        public static readonly XmlDocSmell AmbiguousInheritdocSource = new(
            "DOC770",
            "Multiple possible inheritance sources for <inheritdoc/>.",
            Severity.Warning
        );
        #endregion
    }
}
