using System.Reflection;

namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Central registry of all XML documentation smells.
    /// </summary>
    internal static class XmlDocSmells
    {
        /// <summary>
        /// Returns all registered XML documentation smells declared in this registry.
        /// </summary>
        /// <returns>
        /// A read-only list containing all <see cref="XmlDocSmell"/> instances declared
        /// as public static fields on <see cref="XmlDocSmells"/>, ordered by their identifier.
        /// </returns>
        public static IReadOnlyList<XmlDocSmell> GetAll()
        {
            List<XmlDocSmell> smells = typeof(XmlDocSmells)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.FieldType == typeof(XmlDocSmell))
                .Select(static field => (XmlDocSmell)field.GetValue(null)!)
                .OrderBy(static smell => smell.ID, new Utils.SmellIdComparer())
                .ToList();

            return smells;
        }

        #region General / Structure + Missing documentation
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

        /// <summary>
        /// DOC110 – XML documentation is missing for a supported declaration.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a documentable declaration has no XML documentation comment.
        ///
        /// Message arguments:
        /// {0} = declaration kind, for example class, method, property, field, enum member
        /// {1} = declaration name
        ///
        /// The concrete declaration kind is also stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell MissingDocumentation = new(
            "DOC110",
            "XML documentation for {0} '{1}' is missing.",
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

        /// <summary>
        /// DOC150 – Top-level XML documentation tags are not ordered according to the recommended convention.
        /// </summary>
        /// <remarks>
        /// Recommended order:
        /// summary, typeparam, param, returns/value, exception, remarks, example, seealso.
        /// This is a style-oriented suggestion and not a compiler requirement.
        /// </remarks>
        public static readonly XmlDocSmell TopLevelTagOrderMismatch = new(
            "DOC150",
            "Top-level XML documentation tags should follow the recommended order.",
            Severity.Suggestion
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
        /// <remarks>
        /// This smell is emitted when a param tag does not define the required name attribute.
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell ParamMissingName = new(
            "DOC300",
            "<param> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC310 – A parameter has no corresponding param documentation tag.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a declaration parameter is not documented by a matching param tag.
        ///
        /// Message arguments:
        /// {0} = parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The affected parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell MissingParamTag = new(
            "DOC310",
            "Missing <param> documentation for parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC320 – A param documentation tag exists but its description is empty.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a param tag has a valid name attribute but no meaningful text content.
        ///
        /// Message arguments:
        /// {0} = parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The affected parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell EmptyParamDescription = new(
            "DOC320",
            "<param> documentation for parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC330 – A param documentation tag references a parameter name that does not exist.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a param tag names a parameter that is not declared by the documented member.
        ///
        /// Message arguments:
        /// {0} = unknown parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The unknown parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell UnknownParamTag = new(
            "DOC330",
            "<param> references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC340 – paramref tag contains content and should be empty.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a paramref tag is written with text or nested XML content.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell ParamRefNotEmpty = new(
            "DOC340",
            "<paramref> should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC350 – Multiple param documentation tags exist for the same parameter name.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when more than one param tag documents the same declaration parameter.
        ///
        /// Message arguments:
        /// {0} = duplicated parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The duplicated parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell DuplicateParamTag = new(
            "DOC350",
            "Duplicate <param> documentation for parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC360 – param documentation tags are not ordered according to the parameter list.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when param tags document existing parameters but their order does not match
        /// the parameter order of the documented declaration.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell ParamOrderMismatch = new(
            "DOC360",
            "<param> tags should follow the declaration parameter order.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC370 – paramref tag is missing the required name attribute.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a paramref tag does not define the required name attribute.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell ParamRefMissingName = new(
            "DOC370",
            "<paramref> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC380 – paramref tag references a parameter name that does not exist.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a paramref tag names a parameter that is not declared by the documented member.
        ///
        /// Message arguments:
        /// {0} = unknown parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The unknown parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell UnknownParamRef = new(
            "DOC380",
            "<paramref> references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC390 – paramref tag contains an attribute that is not allowed.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a paramref tag contains an attribute other than name.
        ///
        /// Message arguments:
        /// {0} = invalid attribute name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The invalid attribute name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell InvalidParamRefAttribute = new(
            "DOC390",
            "<paramref> contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );
        #endregion

        #region typeparam / typeparamref
        /// <summary>
        /// DOC400 – typeparam tag is missing the required name attribute.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparam tag does not define the required name attribute.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell TypeParamMissingName = new(
            "DOC400",
            "<typeparam> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC410 – A type parameter has no corresponding typeparam documentation tag.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a declaration type parameter is not documented by a matching typeparam tag.
        ///
        /// Message arguments:
        /// {0} = type parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The affected type parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell MissingTypeParamTag = new(
            "DOC410",
            "Missing <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC420 – A typeparam documentation tag exists but its description is empty.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparam tag has a valid name attribute but no meaningful text content.
        ///
        /// Message arguments:
        /// {0} = type parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The affected type parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell EmptyTypeParamDescription = new(
            "DOC420",
            "<typeparam> documentation for type parameter '{0}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC430 – A typeparam documentation tag references a type parameter name that does not exist.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparam tag names a type parameter that is not declared by the documented member.
        ///
        /// Message arguments:
        /// {0} = unknown type parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The unknown type parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell UnknownTypeParamTag = new(
            "DOC430",
            "<typeparam> references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC440 – typeparamref tag contains content and should be empty.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparamref tag is written with text or nested XML content.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell TypeParamRefNotEmpty = new(
            "DOC440",
            "<typeparamref> should be an empty element, e.g. <typeparamref name=\"T\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC450 – Multiple typeparam documentation tags exist for the same type parameter name.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when more than one typeparam tag documents the same declaration type parameter.
        ///
        /// Message arguments:
        /// {0} = duplicated type parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The duplicated type parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell DuplicateTypeParamTag = new(
            "DOC450",
            "Duplicate <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC460 – typeparam documentation tags are not ordered according to the type parameter list.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when typeparam tags document existing type parameters but their order does not match
        /// the type parameter order of the documented declaration.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell TypeParamOrderMismatch = new(
            "DOC460",
            "<typeparam> tags should follow the declaration type parameter order.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC470 – typeparamref tag is missing the required name attribute.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparamref tag does not define the required name attribute.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell TypeParamRefMissingName = new(
            "DOC470",
            "<typeparamref> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC480 – typeparamref tag references a type parameter name that does not exist.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparamref tag names a type parameter that is not declared by the documented member.
        ///
        /// Message arguments:
        /// {0} = unknown type parameter name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The unknown type parameter name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell UnknownTypeParamRef = new(
            "DOC480",
            "<typeparamref> references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC490 – typeparamref tag contains an attribute that is not allowed.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a typeparamref tag contains an attribute other than name.
        ///
        /// Message arguments:
        /// {0} = invalid attribute name
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// The invalid attribute name is stored in FindingContext.TargetName.
        /// </remarks>
        public static readonly XmlDocSmell InvalidTypeParamRefAttribute = new(
            "DOC490",
            "<typeparamref> contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );
        #endregion

        #region returns
        /// <summary>
        /// DOC500 – A non-void member has no returns documentation.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a member that returns a value has XML documentation,
        /// but no returns documentation.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell MissingReturns = new(
            "DOC500",
            "<returns> is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC510 – The returns tag exists but its description is empty.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a returns tag exists but has no meaningful text content.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell EmptyReturns = new(
            "DOC510",
            "<returns> is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC520 – A void member contains a returns tag.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a void-like member contains returns documentation,
        /// even though no return value exists.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell ReturnsOnVoidMember = new(
            "DOC520",
            "<returns> must not be used for void members.",
            Severity.Warning
        );

        /// <summary>
        /// DOC530 – Multiple returns tags exist.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when more than one returns tag exists on the same documented member.
        ///
        /// The concrete declaration kind is stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell DuplicateReturnsTag = new(
            "DOC530",
            "Duplicate <returns> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC540 – returns is used on a write-only property.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnWriteOnlyProperty = new(
            "DOC540",
            "<returns> must not be used on write-only property '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC541 – returns is used on an indexer.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnIndexer = new(
            "DOC541",
            "<returns> must not be used on indexer '{0}'.",
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
        /// DOC611 - An exception is thrown within the configured transitively analysis scope 
        /// but not documented with an exception-tag.
        /// </summary>
        public static readonly XmlDocSmell MissingTransitiveExceptionDocumentation = new(
            "DOC611",
            "Missing <exception> documentation for transitively thrown '{0}'.",
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
        public static readonly XmlDocSmell ExceptionTagWithoutDirectThrow = new(
            "DOC630",
            "<exception> documents '{0}', but no direct throw was detected.",
            Severity.Warning
        );

        /// <summary> 
        /// DOC631 – Exception flow could not be decided completely, therefore DOC632 was suppressed.
        /// </summary>
        public static readonly XmlDocSmell ExceptionFlowNotDecidable = new(
            "DOC631",
            "Exception flow for documented exception '{0}' could not be decided completely; DOC632 was suppressed because these targets could not be analyzed: {1}.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC632 - An exception tag documents an exception that is not thrown within the configured transitive analysis scope.
        /// </summary>
        public static readonly XmlDocSmell ExceptionTagWithoutTransitiveThrow = new(
            "DOC632",
            "<exception> documents '{0}, but no transitively throw was detected.",
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
        /// DOC700 – inheritdoc is combined with an explicit summary on the same member.
        /// Since inheritdoc already provides summary content through inheritance,
        /// an additional local summary may be misleading, redundant, or semantically conflicting.
        /// </summary>
        public static readonly XmlDocSmell InheritdocWithOwnSummary = new(
            "DOC700",
            "<inheritdoc/> is combined with an explicit <summary>.",
            Severity.Warning
        );

        /// <summary>
        /// DOC710 – inheritdoc cref="..." references a member that cannot be resolved.
        /// The cref target does not exist, is inaccessible, or cannot be bound unambiguously.
        /// As a result, the inherited documentation target is invalid.
        /// </summary>
        public static readonly XmlDocSmell InvalidInheritdocCref = new(
            "DOC710",
            "<inheritdoc cref=\"...\"/> target cannot be resolved.",
            Severity.Warning
        );

        /// <summary>
        /// DOC711 – inheritdoc cref="..." resolves successfully, but the referenced symbol
        /// is not a valid documentation inheritance source for the documented declaration.
        /// For example, the cref target is neither an overridden base member, nor an implemented
        /// interface member, nor a base type or inherited interface of the documented element.
        /// </summary>
        /// <remarks>
        /// This smell indicates that the cref target exists, but it is not connected to the
        /// documented declaration through a valid inheritance or implementation relationship.
        /// </remarks>
        public static readonly XmlDocSmell InheritdocIncompatibleCref = new(
            "DOC711",
            "<inheritdoc cref=\"...\"/> does not refer to a valid inheritance source.",
            Severity.Warning
        );

        /// <summary>
        /// DOC720 – inheritdoc is used but no valid inheritance source exists.
        /// No base member, implemented interface member, or other valid source could be determined.
        /// This indicates that the inherited documentation cannot be resolved meaningfully.
        /// </summary>
        public static readonly XmlDocSmell InheritdocNoSource = new(
            "DOC720",
            "<inheritdoc/> used but no valid inheritance source found.",
            Severity.Warning
        );

        /// <summary>
        /// DOC730 – inheritdoc is redundant because the resolved source does not provide useful XML documentation.
        /// Although a source member exists, inheriting from it adds no documentation value.
        /// This usually indicates unnecessary or ineffective documentation inheritance.
        /// </summary>
        public static readonly XmlDocSmell RedundantInheritdoc = new(
            "DOC730",
            "<inheritdoc/> is redundant because the resolved source has no useful documentation.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC740 – Multiple possible inheritance sources exist for inheritdoc.
        /// The documentation source cannot be determined uniquely, for example when multiple
        /// interface members are plausible candidates.
        /// This may result in ambiguous or tool-dependent inherited documentation.
        /// </summary>
        public static readonly XmlDocSmell AmbiguousInheritdocSource = new(
            "DOC740",
            "Multiple possible inheritance sources for <inheritdoc/>.",
            Severity.Warning
        );

        /// <summary>
        /// DOC750 – Multiple <c>inheritdoc</c> tags are present on the same declaration.
        /// Using more than one inheritdoc tag is ambiguous and may result in confusing
        /// or tool-dependent inherited documentation.
        /// </summary>
        public static readonly XmlDocSmell DuplicateInheritdocTag = new(
            "DOC750",
            "Multiple <inheritdoc> tags are present.",
            Severity.Warning
        );
        #endregion

        #region value
        /// <summary>
        /// DOC800 – A readable property or indexer has no value documentation.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a readable property or indexer has XML documentation,
        /// but no value tag.
        ///
        /// Message arguments:
        /// {0} = declaration kind, for example property or indexer
        /// {1} = declaration name
        ///
        /// The concrete declaration kind is also stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell MissingValueTag = new(
            "DOC800",
            "value documentation is missing on {0} '{1}'.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC810 – A value tag exists but its description is empty.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a readable property or indexer contains a value tag,
        /// but the tag has no meaningful text content.
        ///
        /// Message arguments:
        /// {0} = declaration kind, for example property or indexer
        /// {1} = declaration name
        ///
        /// The concrete declaration kind is also stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell EmptyValueTag = new(
            "DOC810",
            "value documentation on {0} '{1}' is empty.",
            Severity.Warning
        );

        /// <summary>
        /// DOC820 – Multiple value tags exist on the same readable property or indexer.
        /// </summary>
        /// <remarks>
        /// This smell is emitted when a readable property or indexer contains more than one value tag.
        ///
        /// Message arguments:
        /// {0} = declaration kind, for example property or indexer
        /// {1} = declaration name
        ///
        /// The concrete declaration kind is also stored in FindingContext.OwnerKind.
        /// </remarks>
        public static readonly XmlDocSmell DuplicateValueTag = new(
            "DOC820",
            "Duplicate value documentation on {0} '{1}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC830 – value is used on a write-only property.
        /// </summary>
        public static readonly XmlDocSmell ValueOnWriteOnlyProperty = new(
            "DOC830",
            "<value> must not be used on write-only property '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC831 – value is used on a member that is not a property or indexer.
        /// </summary>
        public static readonly XmlDocSmell ValueOnInvalidMember = new(
            "DOC831",
            "<value> must only be used on properties or indexers.",
            Severity.Warning
        );

        #endregion

        #region see / seealso
        /// <summary>
        /// DOC900 – see-tag has no valid target attribute.
        /// A <see> tag must specify exactly one of: cref, href, or langword.
        /// </summary>
        public static readonly XmlDocSmell SeeMissingTarget = new(
            "DOC900",
            "<see> must specify exactly one of 'cref', 'href', or 'langword'.",
            Severity.Error
        );

        /// <summary>
        /// DOC901 – seealso-tag has no valid target attribute.
        /// A <seealso> tag must specify exactly one of: cref or href.
        /// </summary>
        public static readonly XmlDocSmell SeeAlsoMissingTarget = new(
            "DOC901",
            "<seealso> must specify exactly one of 'cref' or 'href'.",
            Severity.Error
        );

        /// <summary>
        /// DOC910 – see-tag uses multiple mutually exclusive target attributes.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeAttributeCombination = new(
            "DOC910",
            "<see> must not combine 'cref', 'href', and 'langword'. Use exactly one target attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC911 – seealso-tag uses multiple mutually exclusive target attributes.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeAlsoAttributeCombination = new(
            "DOC911",
            "<seealso> must not combine 'cref' and 'href'. Use exactly one target attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC912 – seealso-tag uses langword, which is not supported.
        /// </summary>
        public static readonly XmlDocSmell SeeAlsoLangwordNotSupported = new(
            "DOC912",
            "<seealso> does not support the 'langword' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC920 – see-tag contains an attribute that is not allowed.
        /// Only cref, href, and langword are allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeAttribute = new(
            "DOC920",
            "<see> contains invalid attribute '{0}'. Only 'cref', 'href', and 'langword' are allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC921 – seealso-tag contains an attribute that is not allowed.
        /// Only cref and href are allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeAlsoAttribute = new(
            "DOC921",
            "<seealso> contains invalid attribute '{0}'. Only 'cref' and 'href' are allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC930 – see cref could not be resolved to a known symbol.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeCref = new(
            "DOC930",
            "<see> cref '{0}' could not be resolved.",
            Severity.Warning
        );

        /// <summary>
        /// DOC931 – seealso cref could not be resolved to a known symbol.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeAlsoCref = new(
            "DOC931",
            "<seealso> cref '{0}' could not be resolved.",
            Severity.Warning
        );

        /// <summary>
        /// DOC940 – see href is empty or not a valid absolute URI.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeHref = new(
            "DOC940",
            "<see> href '{0}' is invalid.",
            Severity.Warning
        );

        /// <summary>
        /// DOC941 – seealso href is empty or not a valid absolute URI.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeAlsoHref = new(
            "DOC941",
            "<seealso> href '{0}' is invalid.",
            Severity.Warning
        );

        /// <summary>
        /// DOC950 – see langword uses a keyword that is not supported.
        /// </summary>
        public static readonly XmlDocSmell InvalidSeeLangword = new(
            "DOC950",
            "<see> langword '{0}' is not supported.",
            Severity.Warning
        );

        /// <summary>
        /// DOC960 – seealso must not be nested inside another XML documentation tag.
        /// It should appear only at the top level of the documentation comment.
        /// </summary>
        public static readonly XmlDocSmell SeeAlsoNotTopLevel = new(
            "DOC960",
            "<seealso> must be a top-level XML documentation tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC970 – Duplicate seealso tags reference the same target.
        /// </summary>
        public static readonly XmlDocSmell DuplicateSeeAlsoTarget = new(
            "DOC970",
            "Duplicate <seealso> reference to '{0}'.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC980 – A see-tag contains body content although it should normally be an empty element.
        /// </summary>
        public static readonly XmlDocSmell SeeNotEmpty = new(
            "DOC980",
            "<see> should normally be an empty element, e.g. <see cref=\"T:Namespace.Type\"/>.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC981 – A seealso-tag contains body content although it should normally be an empty element.
        /// </summary>
        public static readonly XmlDocSmell SeeAlsoNotEmpty = new(
            "DOC981",
            "<seealso> should normally be an empty element, e.g. <seealso cref=\"T:Namespace.Type\"/>.",
            Severity.Suggestion
        );
        #endregion
    }
}
