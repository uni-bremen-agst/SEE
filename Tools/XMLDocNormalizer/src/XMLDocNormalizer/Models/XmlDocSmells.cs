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

        /// <summary>
        /// DOC370 – paramref-tag on a method is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamRefMissingNameOnMethod = new(
            "DOC370",
            "<paramref> on method is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC371 – paramref-tag on a constructor is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamRefMissingNameOnConstructor = new(
            "DOC371",
            "<paramref> on constructor is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC372 – paramref-tag on a delegate is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamRefMissingNameOnDelegate = new(
            "DOC372",
            "<paramref> on delegate is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC373 – paramref-tag on an indexer is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamRefMissingNameOnIndexer = new(
            "DOC373",
            "<paramref> on indexer is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC374 – paramref-tag on an operator is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamRefMissingNameOnOperator = new(
            "DOC374",
            "<paramref> on operator is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC375 – paramref-tag on a conversion operator is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamRefMissingNameOnConversionOperator = new(
            "DOC375",
            "<paramref> on conversion operator is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC380 – A method paramref-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamRefOnMethod = new(
            "DOC380",
            "<paramref> on method references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC381 – A constructor paramref-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamRefOnConstructor = new(
            "DOC381",
            "<paramref> on constructor references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC382 – A delegate paramref-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamRefOnDelegate = new(
            "DOC382",
            "<paramref> on delegate references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC383 – An indexer paramref-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamRefOnIndexer = new(
            "DOC383",
            "<paramref> on indexer references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC384 – An operator paramref-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamRefOnOperator = new(
            "DOC384",
            "<paramref> on operator references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC385 – A conversion operator paramref-tag references a parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownParamRefOnConversionOperator = new(
            "DOC385",
            "<paramref> on conversion operator references unknown parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC390 – paramref-tag on a method contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidParamRefAttributeOnMethod = new(
            "DOC390",
            "<paramref> on method contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC391 – paramref-tag on a constructor contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidParamRefAttributeOnConstructor = new(
            "DOC391",
            "<paramref> on constructor contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC392 – paramref-tag on a delegate contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidParamRefAttributeOnDelegate = new(
            "DOC392",
            "<paramref> on delegate contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC393 – paramref-tag on an indexer contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidParamRefAttributeOnIndexer = new(
            "DOC393",
            "<paramref> on indexer contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC394 – paramref-tag on an operator contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidParamRefAttributeOnOperator = new(
            "DOC394",
            "<paramref> on operator contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC395 – paramref-tag on a conversion operator contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidParamRefAttributeOnConversionOperator = new(
            "DOC395",
            "<paramref> on conversion operator contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
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

        /// <summary>
        /// DOC470 – typeparamref-tag on a type is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefMissingNameOnType = new(
            "DOC470",
            "<typeparamref> on type is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC471 – typeparamref-tag on a method is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefMissingNameOnMethod = new(
            "DOC471",
            "<typeparamref> on method is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC472 – typeparamref-tag on a delegate is missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell TypeParamRefMissingNameOnDelegate = new(
            "DOC472",
            "<typeparamref> on delegate is missing required 'name' attribute.",
            Severity.Error
        );

        /// <summary>
        /// DOC480 – A type typeparamref-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamRefOnType = new(
            "DOC480",
            "<typeparamref> on type references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC481 – A method typeparamref-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamRefOnMethod = new(
            "DOC481",
            "<typeparamref> on method references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC482 – A delegate typeparamref-tag references a type parameter name that does not exist.
        /// </summary>
        public static readonly XmlDocSmell UnknownTypeParamRefOnDelegate = new(
            "DOC482",
            "<typeparamref> on delegate references unknown type parameter '{0}'.",
            Severity.Warning
        );

        /// <summary>
        /// DOC490 – typeparamref-tag on a type contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidTypeParamRefAttributeOnType = new(
            "DOC490",
            "<typeparamref> on type contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC491 – typeparamref-tag on a method contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidTypeParamRefAttributeOnMethod = new(
            "DOC491",
            "<typeparamref> on method contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
        );

        /// <summary>
        /// DOC492 – typeparamref-tag on a delegate contains an attribute that is not allowed.
        /// </summary>
        public static readonly XmlDocSmell InvalidTypeParamRefAttributeOnDelegate = new(
            "DOC492",
            "<typeparamref> on delegate contains invalid attribute '{0}'. Only 'name' is allowed.",
            Severity.Error
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
        /// DOC631 – Exception flow could not be decided completely, therefore DOC630 was suppressed.
        /// </summary>
        public static readonly XmlDocSmell ExceptionFlowNotDecidable = new(
            "DOC631",
            "Exception flow for documented exception '{0}' could not be decided completely; DOC630 was suppressed because these targets could not be analyzed: {1}.",
            Severity.Suggestion
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
