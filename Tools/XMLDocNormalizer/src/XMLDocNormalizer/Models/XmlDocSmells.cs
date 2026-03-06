namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Central registry of all XML documentation smells.
    /// </summary>
    internal static class XmlDocSmells
    {
        #region General / Structure + Missing documentation
        /// <summary>
        /// DOC000 – No XML documentation comment is present for the member or type.
        /// TODO: remove this generic smell
        /// </summary>
        public static readonly XmlDocSmell MissingDocumentation = new(
            "DOC000",
            "XML documentation is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC100 – Namespace documentation is missing in the dedicated namespace documentation file.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when namespace documentation is required but should not be duplicated at every
        /// namespace declaration site. Instead, a dedicated file in the namespace directory should contain the
        /// namespace documentation.
        ///
        /// Message arguments:
        /// {0} = primary suggested file name (e.g. EdgeLayouts.cs)
        /// {1} = secondary suggested file name (e.g. EdgeLayout.cs or NamespaceDoc.cs)
        /// {2} = fully qualified namespace name
        /// </remarks>
        public static readonly XmlDocSmell MissingCentralNamespaceDocumentation = new(
            "DOC100",
            "Namespace '{1}' documentation is missing. " +
                "Document the namespace in a dedicated file in this directory " +
                "(e.g. '{0}').",
            Severity.Warning
        );

        /// Types

        /// <summary>
        /// DOC110 – Class documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingClassDocumentation = new(
            "DOC110",
            "XML documentation for class '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC111 – Struct documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingStructDocumentation = new(
            "DOC111",
            "XML documentation for struct '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC112 – Interface documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingInterfaceDocumentation = new(
            "DOC112",
            "XML documentation for interface '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC113 – Enum documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingEnumDocumentation = new(
            "DOC113",
            "XML documentation for enum '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC114 – Delegate documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingDelegateDocumentation = new(
            "DOC114",
            "XML documentation for delegate '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC115 – Record documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingRecordDocumentation = new(
            "DOC115",
            "XML documentation for record '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC116 – Record struct documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingRecordStructDocumentation = new(
            "DOC116",
            "XML documentation for record struct '{0}' is missing.",
            Severity.Warning
        );

        /// Members:

        /// <summary>
        /// DOC120 – Constructor documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingConstructorDocumentation = new(
            "DOC120",
            "XML documentation for constructor '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC121 – Method documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingMethodDocumentation = new(
            "DOC121",
            "XML documentation for method '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC122 – Property documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingPropertyDocumentation = new(
            "DOC122",
            "XML documentation for property '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC123 – Indexer documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingIndexerDocumentation = new(
            "DOC123",
            "XML documentation for indexer '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC124 – Field documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingFieldDocumentation = new(
            "DOC124",
            "XML documentation for field '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC125 – Event documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingEventDocumentation = new(
            "DOC125",
            "XML documentation for event '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC126 – Operator documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingOperatorDocumentation = new(
            "DOC126",
            "XML documentation for operator '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC127 – Conversion operator documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingConversionOperatorDocumentation = new(
            "DOC127",
            "XML documentation for conversion operator '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC128 – Destructor documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingDestructorDocumentation = new(
            "DOC128",
            "XML documentation for destructor '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC129 – Enum member documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingEnumMemberDocumentation = new(
            "DOC129",
            "XML documentation for enum member '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC140 – Unknown or misspelled XML documentation tag.
        /// </summary>
        public static readonly XmlDocSmell UnknownTag = new(
            "DOC140",
            "Unknown XML documentation tag <{0}>.",
            Severity.Warning
        );

        /// <summary>
        /// DOC141 – Missing end tag (unclosed XML element).
        /// </summary>
        public static readonly XmlDocSmell MissingEndTag = new(
            "DOC141",
            "Missing end tag (unclosed XML element).",
            Severity.Error
        );

        /// <summary>
        /// DOC142 – XML documentation tag is syntactically invalid (no valid tag name).
        /// </summary>
        public static readonly XmlDocSmell InvalidXmlTag = new(
            "DOC142",
            "Invalid XML documentation tag '{0}'.",
            Severity.Error
        );

        /// <summary>
        /// DOC143 – This XML documentation tag is not allowed on the member type.
        /// </summary>
        public static readonly XmlDocSmell InvalidTagOnMember = new XmlDocSmell(
            "DOC143",
            "This XML documentation tag is not allowed on this member type.",
            Severity.Warning
        );
        #endregion

        #region summary / remarks / etc.
        /// <summary>
        /// DOC200 – The summary-tag is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingSummary = new(
            "DOC200",
            "<summary> is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC210 – The summary-tag exists but contains no meaningful content.
        /// </summary>
        public static readonly XmlDocSmell EmptySummary = new(
            "DOC210",
            "<summary> is empty.",
            Severity.Warning
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
        #endregion

        #region param / paramref
        /// <summary>
        /// DOC300 – param-tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingName = new(
            "DOC300",
            "<param> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC301 – A parameter has no corresponding param-tag.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell MissingParamTag = new(
            "DOC301",
            "Missing <param> documentation for parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC310 – A method parameter has no corresponding <param>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTagOnMethod = new(
            "DOC310",
            "Missing <param> documentation for method parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC311 – A constructor parameter has no corresponding <param>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTagOnConstructor = new(
            "DOC311",
            "Missing <param> documentation for constructor parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC312 – A delegate parameter has no corresponding <param>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTagOnDelegate = new(
            "DOC312",
            "Missing <param> documentation for delegate parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC313 – An indexer parameter has no corresponding <param>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTagOnIndexer = new(
            "DOC313",
            "Missing <param> documentation for indexer parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC314 – An operator parameter has no corresponding <param>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTagOnOperator = new(
            "DOC314",
            "Missing <param> documentation for operator parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC315 – A conversion operator parameter has no corresponding <param>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingParamTagOnConversionOperator = new(
            "DOC315",
            "Missing <param> documentation for conversion operator parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC302 – A param-tag exists but its description is empty.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescription = new(
            "DOC302",
            "<param> documentation for parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC320 – A method <param>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescriptionOnMethod = new(
            "DOC320",
            "<param> documentation for method parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC321 – A constructor <param>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescriptionOnConstructor = new(
            "DOC321",
            "<param> documentation for constructor parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC322 – A delegate <param>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescriptionOnDelegate = new(
            "DOC322",
            "<param> documentation for delegate parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC323 – An indexer <param>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescriptionOnIndexer = new(
            "DOC323",
            "<param> documentation for indexer parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC324 – An operator <param>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescriptionOnOperator = new(
            "DOC324",
            "<param> documentation for operator parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC325 – A conversion operator <param>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescriptionOnConversionOperator = new(
            "DOC325",
            "<param> documentation for conversion operator parameter '{0}' is empty.",
            Severity.Warning
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

        #region typeparam / typeparamref
        /// <summary>
        /// DOC400 – typeparam-tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingName = new(
            "DOC400",
            "<typeparam> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC401 – A generic type parameter has no corresponding typeparam-tag.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTag = new(
            "DOC401",
            "Missing <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC410 – A type type-parameter has no corresponding <typeparam>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTagOnType = new(
            "DOC410",
            "Missing <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC411 – A method type-parameter has no corresponding <typeparam>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTagOnMethod = new(
            "DOC411",
            "Missing <typeparam> documentation for method type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC412 – A delegate type-parameter has no corresponding <typeparam>-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTagOnDelegate = new(
            "DOC412",
            "Missing <typeparam> documentation for delegate type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC402 – A typeparam-tag exists but its description is empty.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescription = new(
            "DOC402",
            "<typeparam> documentation for type parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC420 – A type <typeparam>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescriptionOnType = new(
            "DOC420",
            "<typeparam> documentation for type parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC421 – A method <typeparam>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescriptionOnMethod = new(
            "DOC421",
            "<typeparam> documentation for method type parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC422 – A delegate <typeparam>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescriptionOnDelegate = new(
            "DOC422",
            "<typeparam> documentation for delegate type parameter '{0}' is empty.",
            Severity.Warning
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
        /// DOC5000 – A non-void member has no returns documentation.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell MissingReturns = new(
            "DOC5000",
            "<returns> is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC500 – A non-void method has no <returns> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingReturnsOnMethod = new(
            "DOC500",
            "<returns> is missing on method '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC501 – A non-void delegate has no <returns> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingReturnsOnDelegate = new(
            "DOC501",
            "<returns> is missing on delegate '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC502 – A non-void operator has no <returns> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingReturnsOnOperator = new(
            "DOC502",
            "<returns> is missing on operator '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC503 – A conversion operator has no <returns> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingReturnsOnConversionOperator = new(
            "DOC503",
            "<returns> is missing on conversion operator '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC5100 – The returns-tag exists but its description is empty.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturns = new(
            "DOC5100",
            "<returns> is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC510 – A method <returns>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturnsOnMethod = new(
            "DOC510",
            "<returns> is empty on method '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC511 – A delegate <returns>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturnsOnDelegate = new(
            "DOC511",
            "<returns> is empty on delegate '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC512 – An operator <returns>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturnsOnOperator = new(
            "DOC512",
            "<returns> is empty on operator '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC513 – A conversion operator <returns>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyReturnsOnConversionOperator = new(
            "DOC513",
            "<returns> is empty on conversion operator '{0}'.",
            Severity.Warning
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
            Severity.Warning
        );

        /// <summary>
        /// DOC620 – An exception-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyExceptionDescription = new(
            "DOC620",
            "<exception> documentation for '{0}' is empty.",
            Severity.Warning
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
            Severity.Warning
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

        #region value

        /// <summary>
        /// DOC800 – A property or indexer has no <value> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingValue = new XmlDocSmell(
            "DOC800",
            "<value> is missing.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC810 – The <value> tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyValue = new XmlDocSmell(
            "DOC810",
            "<value> is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC820 – Multiple <value> tags exist.
        /// </summary>
        public static readonly XmlDocSmell DuplicateValueTag = new XmlDocSmell(
            "DOC820",
            "Duplicate <value> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC830 – <value> used on a write-only property.
        /// </summary>
        public static readonly XmlDocSmell ValueOnWriteOnlyProperty = new XmlDocSmell(
            "DOC830",
            "<value> must not be used on write-only properties.",
            Severity.Warning
        );

        /// <summary>
        /// DOC840 – <value> used on a member that is not a property or indexer.
        /// </summary>
        public static readonly XmlDocSmell ValueOnInvalidMember = new XmlDocSmell(
            "DOC840",
            "<value> must only be used on properties or indexers.",
            Severity.Warning
        );

        #endregion
    }
}
