namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Central registry of all XML documentation smells.
    /// </summary>
    internal static class XmlDocSmells
    {
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
        /// DOC126 – Event documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingEventFieldDocumentation = new(
            "DOC126",
            "XML documentation for event '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC127 – Operator documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingOperatorDocumentation = new(
            "DOC127",
            "XML documentation for operator '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC128 – Conversion operator documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingConversionOperatorDocumentation = new(
            "DOC128",
            "XML documentation for conversion operator '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC129 – Destructor documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingDestructorDocumentation = new(
            "DOC129",
            "XML documentation for destructor '{0}' is missing.",
            Severity.Warning
        );

        /// <summary>
        /// DOC130 – Enum member documentation is missing.
        /// </summary>
        public static readonly XmlDocSmell MissingEnumMemberDocumentation = new(
            "DOC130",
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
        /// DOC3000 – param-tag missing required 'name' attribute.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell ParamMissingName = new(
            "DOC3000",
            "<param> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC300 – param-tag on a method is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingNameOnMethod = new(
            "DOC300",
            "<param> tag on a method is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC301 – param-tag on a constructor is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingNameOnConstructor = new(
            "DOC301",
            "<param> tag on a constructor is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC302 – param-tag on a delegate is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingNameOnDelegate = new(
            "DOC302",
            "<param> tag on a delegate is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC303 – param-tag on an indexer is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingNameOnIndexer = new(
            "DOC303",
            "<param> tag on an indexer is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC304 – param-tag on an operator is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingNameOnOperator = new(
            "DOC304",
            "<param> tag on an operator is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC305 – param-tag on a conversion operator is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingNameOnConversionOperator = new(
            "DOC305",
            "<param> tag on a conversion operator is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC3010 – A parameter has no corresponding param-tag.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell MissingParamTag = new(
            "DOC3010",
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
        /// DOC3020 – A param-tag exists but its description is empty.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell EmptyParamDescription = new(
            "DOC3020",
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
        /// DOC3300 – A param-tag references a parameter name that does not exist.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTag = new(
            "DOC3300",
            "<param> references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC330 – A method param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTagOnMethod = new(
            "DOC330",
            "<param> on method references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC331 – A constructor param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTagOnConstructor = new(
            "DOC331",
            "<param> on constructor references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC332 – A delegate param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTagOnDelegate = new(
            "DOC332",
            "<param> on delegate references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC333 – An indexer param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTagOnIndexer = new(
            "DOC333",
            "<param> on indexer references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC334 – An operator param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTagOnOperator = new(
            "DOC334",
            "<param> on operator references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC335 – A conversion operator param-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamTagOnConversionOperator = new(
            "DOC335",
            "<param> on conversion operator references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC3400 – paramref-tag contains content and should be empty.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmpty = new(
            "DOC3400",
            "<paramref> should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC340 – paramref-tag on a method contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmptyOnMethod = new(
            "DOC340",
            "<paramref> on method should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC341 – paramref-tag on a constructor contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmptyOnConstructor = new(
            "DOC341",
            "<paramref> on constructor should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC342 – paramref-tag on a delegate contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmptyOnDelegate = new(
            "DOC342",
            "<paramref> on delegate should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC343 – paramref-tag on an indexer contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmptyOnIndexer = new(
            "DOC343",
            "<paramref> on indexer should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC344 – paramref-tag on an operator contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmptyOnOperator = new(
            "DOC344",
            "<paramref> on operator should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC345 – paramref-tag on a conversion operator contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmptyOnConversionOperator = new(
            "DOC345",
            "<paramref> on conversion operator should be an empty element, e.g. <paramref name=\"x\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC3500 – Multiple param tags exist for the same parameter name.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTag = new(
            "DOC3500",
            "Duplicate <param> documentation for parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC350 – Multiple param tags exist for the same method parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTagOnMethod = new(
            "DOC350",
            "Duplicate <param> documentation for method parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC351 – Multiple param tags exist for the same constructor parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTagOnConstructor = new(
            "DOC351",
            "Duplicate <param> documentation for constructor parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC352 – Multiple param tags exist for the same delegate parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTagOnDelegate = new(
            "DOC352",
            "Duplicate <param> documentation for delegate parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC353 – Multiple param tags exist for the same indexer parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTagOnIndexer = new(
            "DOC353",
            "Duplicate <param> documentation for indexer parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC354 – Multiple param tags exist for the same operator parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTagOnOperator = new(
            "DOC354",
            "Duplicate <param> documentation for operator parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC355 – Multiple param tags exist for the same conversion operator parameter name.
        /// </summary>
        public static readonly XmlDocSmell DuplicateParamTagOnConversionOperator = new(
            "DOC355",
            "Duplicate <param> documentation for conversion operator parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC360 – param tags on a method are not ordered according to the parameter list.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatchOnMethod = new(
            "DOC360",
            "<param> tags should follow the parameter order of the method.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC361 – param tags on a constructor are not ordered according to the parameter list.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatchOnConstructor = new(
            "DOC361",
            "<param> tags should follow the parameter order of the constructor.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC362 – param tags on a delegate are not ordered according to the parameter list.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatchOnDelegate = new(
            "DOC362",
            "<param> tags should follow the parameter order of the delegate.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC363 – param tags on an indexer are not ordered according to the parameter list.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatchOnIndexer = new(
            "DOC363",
            "<param> tags should follow the parameter order of the indexer.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC364 – param tags on an operator are not ordered according to the parameter list.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatchOnOperator = new(
            "DOC364",
            "<param> tags should follow the parameter order of the operator.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC365 – param tags on a conversion operator are not ordered according to the parameter list.
        /// </summary>
        public static readonly XmlDocSmell ParamOrderMismatchOnConversionOperator = new(
            "DOC365",
            "<param> tags should follow the parameter order of the conversion operator.",
            Severity.Suggestion
        );

        #endregion

        #region typeparam / typeparamref
        /// <summary>
        /// DOC4000 – typeparam-tag missing required 'name' attribute.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingName = new(
            "DOC4000",
            "<typeparam> tag is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC400 – typeparam-tag on a type is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingNameOnType = new(
            "DOC400",
            "<typeparam> tag on a type is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC401 – typeparam-tag on a method is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingNameOnMethod = new(
            "DOC401",
            "<typeparam> tag on a method is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC402 – typeparam-tag on a delegate is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamMissingNameOnDelegate = new(
            "DOC402",
            "<typeparam> tag on a delegate is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC4010 – A generic type parameter has no corresponding typeparam-tag.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell MissingTypeParamTag = new(
            "DOC4010",
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
        /// DOC4020 – A typeparam-tag exists but its description is empty.
        /// TODO: remove this generic smell.
        /// </summary>
        public static readonly XmlDocSmell EmptyTypeParamDescription = new(
            "DOC4020",
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
        /// DOC4300 – A typeparam-tag references a type parameter name that does not exist.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamTag = new(
            "DOC4300",
            "<typeparam> references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC430 – A type typeparam-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamTagOnType = new(
            "DOC430",
            "<typeparam> on type references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC431 – A method typeparam-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamTagOnMethod = new(
            "DOC431",
            "<typeparam> on method references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC432 – A delegate typeparam-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamTagOnDelegate = new(
            "DOC432",
            "<typeparam> on delegate references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC4400 - typeparamref-tag contains content and should be empty.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefNotEmpty = new(
            "DOC4400",
            "<typeparamref> should be an empty element, e.g. <typeparamref name=\"T\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC440 – typeparamref-tag on a type contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefNotEmptyOnType = new(
            "DOC440",
            "<typeparamref> on type should be an empty element, e.g. <typeparamref name=\"T\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC441 – typeparamref-tag on a method contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefNotEmptyOnMethod = new(
            "DOC441",
            "<typeparamref> on method should be an empty element, e.g. <typeparamref name=\"T\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC442 – typeparamref-tag on a delegate contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefNotEmptyOnDelegate = new(
            "DOC442",
            "<typeparamref> on delegate should be an empty element, e.g. <typeparamref name=\"T\"/>.",
            Severity.Error
        );

        /// <summary>
        /// DOC4500 – Multiple typeparam tags exist for the same parameter name.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell DuplicateTypeParamTag = new(
            "DOC4500",
            "Duplicate <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC450 – Multiple typeparam tags exist for the same type parameter name on a type.
        /// </summary>
        public static readonly XmlDocSmell DuplicateTypeParamTagOnType = new(
            "DOC450",
            "Duplicate <typeparam> documentation for type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC451 – Multiple typeparam tags exist for the same type parameter name on a method.
        /// </summary>
        public static readonly XmlDocSmell DuplicateTypeParamTagOnMethod = new(
            "DOC451",
            "Duplicate <typeparam> documentation for method type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC452 – Multiple typeparam tags exist for the same type parameter name on a delegate.
        /// </summary>
        public static readonly XmlDocSmell DuplicateTypeParamTagOnDelegate = new(
            "DOC452",
            "Duplicate <typeparam> documentation for delegate type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC460 – typeparam tags on a type are not ordered according to the type parameter list.
        /// </summary>
        public static readonly XmlDocSmell TypeParamOrderMismatchOnType = new(
            "DOC460",
            "<typeparam> tags should follow the type parameter order of the type.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC461 – typeparam tags on a method are not ordered according to the type parameter list.
        /// </summary>
        public static readonly XmlDocSmell TypeParamOrderMismatchOnMethod = new(
            "DOC461",
            "<typeparam> tags should follow the type parameter order of the method.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC462 – typeparam tags on a delegate are not ordered according to the type parameter list.
        /// </summary>
        public static readonly XmlDocSmell TypeParamOrderMismatchOnDelegate = new(
            "DOC462",
            "<typeparam> tags should follow the type parameter order of the delegate.",
            Severity.Suggestion
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
        /// DOC5200 – A void member contains a returns-tag, which is inconsistent with its return type.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnVoidMember = new(
            "DOC5200",
            "<returns> must not be used for void members.",
            Severity.Warning
        );

        /// <summary>
        /// DOC520 – A void method contains a returns-tag.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnVoidMethod = new(
            "DOC520",
            "<returns> must not be used on void method '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC521 – A void delegate contains a returns-tag.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnVoidDelegate = new(
            "DOC521",
            "<returns> must not be used on void delegate '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC522 – A void operator contains a returns-tag.
        /// </summary>
        public static readonly XmlDocSmell ReturnsOnVoidOperator = new(
            "DOC522",
            "<returns> must not be used on void operator '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC5300 – Multiple returns tags exist.
        /// TODO: remove this generic smell 
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsTag = new(
            "DOC5300",
            "Duplicate <returns> tag.",
            Severity.Warning
        );

        /// <summary>
        /// DOC530 – Multiple returns tags exist on a method.
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsOnMethod = new(
            "DOC530",
            "Duplicate <returns> tag on method '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC531 – Multiple returns tags exist on a delegate.
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsOnDelegate = new(
            "DOC531",
            "Duplicate <returns> tag on delegate '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC532 – Multiple returns tags exist on an operator.
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsOnOperator = new(
            "DOC532",
            "Duplicate <returns> tag on operator '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC533 – Multiple returns tags exist on a conversion operator.
        /// </summary>
        public static readonly XmlDocSmell DuplicateReturnsOnConversionOperator = new(
            "DOC533",
            "Duplicate <returns> tag on conversion operator '{0}'.",
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
        /// DOC800 – A property has no <value> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingValueOnProperty = new(
            "DOC800",
            "<value> is missing on property '{0}'.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC801 – An indexer has no <value> documentation.
        /// </summary>
        public static readonly XmlDocSmell MissingValueOnIndexer = new(
            "DOC801",
            "<value> is missing on indexer '{0}'.",
            Severity.Suggestion
        );

        /// <summary>
        /// DOC810 – A property <value>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyValueOnProperty = new(
            "DOC810",
            "<value> is empty on property '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC811 – An indexer <value>-tag exists but its description is empty.
        /// </summary>
        public static readonly XmlDocSmell EmptyValueOnIndexer = new(
            "DOC811",
            "<value> is empty on indexer '{0}'.",
            Severity.Warning
        );
        /// <summary>
        /// DOC820 – Multiple value tags exist on a property.
        /// </summary>
        public static readonly XmlDocSmell DuplicateValueOnProperty = new(
            "DOC820",
            "Duplicate <value> tag on property '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC821 – Multiple value tags exist on an indexer.
        /// </summary>
        public static readonly XmlDocSmell DuplicateValueOnIndexer = new(
            "DOC821",
            "Duplicate <value> tag on indexer '{0}'.",
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
    }
}
