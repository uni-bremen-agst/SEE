using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Value;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizer.Utils.Extensions;

namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Detects value-related XML documentation smells.
    /// </summary>
    internal static class XmlDocValueDetector
    {
        /// <summary>
        /// Scans the syntax tree and returns value-related findings.
        /// </summary>
        /// <param name="tree">
        /// The syntax tree to analyze.
        /// </param>
        /// <param name="filePath">
        /// The file path used for reporting.
        /// </param>
        /// <returns>
        /// A list of value-related findings.
        /// </returns>
        public static List<Finding> FindValueSmells(SyntaxTree tree, string filePath)
        {
            return FindValueSmells(tree, filePath, new XmlDocOptions());
        }

        /// <summary>
        /// Scans the syntax tree and returns value-related findings.
        /// </summary>
        /// <param name="tree">
        /// The syntax tree to analyze.
        /// </param>
        /// <param name="filePath">
        /// The file path used for reporting.
        /// </param>
        /// <param name="options">
        /// The XML documentation options used by the analysis.
        /// </param>
        /// <returns>
        /// A list of value-related findings.
        /// </returns>
        public static List<Finding> FindValueSmells(
            SyntaxTree tree,
            string filePath,
            XmlDocOptions options)
        {
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(options);

            List<Finding> findings = new();

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            IEnumerable<MemberDeclarationSyntax> members = root.DescendantNodes().OfType<MemberDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in members)
            {
                ValueAnalysisContext? context = TryCreateContext(member, filePath);

                if (context == null)
                {
                    continue;
                }

                AddMissingValueFindings(
                    findings,
                    tree,
                    filePath,
                    context,
                    options.ValueDocumentationMode);

                AddEmptyValueFindings(findings, tree, filePath, context);
                AddDuplicateValueFindings(findings, tree, filePath, context);
                AddInvalidValueUsageFindings(findings, tree, filePath, context);
            }

            return findings;
        }

        /// <summary>
        /// Creates a value-analysis context for a documented member.
        /// </summary>
        /// <param name="member">
        /// The member to inspect.
        /// </param>
        /// <param name="filePath">
        /// The file path used for generated-file and test-file classification.
        /// </param>
        /// <returns>
        /// A fully prepared analysis context, or null if the member has no XML documentation comment.
        /// </returns>
        private static ValueAnalysisContext? TryCreateContext(MemberDeclarationSyntax member, string filePath)
        {
            ArgumentNullException.ThrowIfNull(member);
            ArgumentNullException.ThrowIfNull(filePath);

            DocumentationCommentTriviaSyntax? doc = XmlDocUtils.TryGetDocComment(member);

            if (doc == null)
            {
                return null;
            }

            string? memberName = GetMemberName(member);

            return new ValueAnalysisContext
            {
                Member = member,
                Doc = doc,
                ValueTags = XmlDocElementQuery.AllByName(doc, "value").ToList(),
                TargetKind = ClassifyMember(member),
                MemberName = memberName,
                FindingContext = FindingContextBuilder.ForDeclaration(
                    member,
                    "ValueTag",
                    targetName: memberName,
                    filePath: filePath)
            };
        }

        /// <summary>
        /// Adds missing-value findings.
        /// </summary>
        /// <param name="findings">
        /// The target finding list.
        /// </param>
        /// <param name="tree">
        /// The syntax tree used for location calculation.
        /// </param>
        /// <param name="filePath">
        /// The file path used for reporting.
        /// </param>
        /// <param name="context">
        /// The prepared member analysis context.
        /// </param>
        /// <param name="valueDocumentationMode">
        /// The configured value-documentation mode.
        /// </param>
        private static void AddMissingValueFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context,
            ValueDocumentationMode valueDocumentationMode)
        {
            ArgumentNullException.ThrowIfNull(findings);
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(context);

            if (context.ValueTags.Count != 0)
            {
                return;
            }

            if (context.Doc.HasInheritdoc())
            {
                return;
            }

            if (!ShouldReportMissingValue(context, valueDocumentationMode))
            {
                return;
            }

            findings.Add(FindingFactory.AtPosition(
                tree,
                filePath,
                tagName: "value",
                XmlDocSmells.MissingValueTag,
                MemberAnchorResolver.GetAnchorPosition(context.Member),
                context.FindingContext,
                snippet: string.Empty,
                GetValueTargetKindForMessage(context),
                GetValueTargetNameForMessage(context)));
        }

        /// <summary>
        /// Determines whether missing value documentation should be reported for the current context.
        /// </summary>
        /// <param name="context">
        /// The prepared value-analysis context.
        /// </param>
        /// <param name="valueDocumentationMode">
        /// The configured value-documentation mode.
        /// </param>
        /// <returns>
        /// True if a missing value finding should be reported; otherwise false.
        /// </returns>
        private static bool ShouldReportMissingValue(
            ValueAnalysisContext context,
            ValueDocumentationMode valueDocumentationMode)
        {
            ArgumentNullException.ThrowIfNull(context);

            return valueDocumentationMode switch
            {
                ValueDocumentationMode.None => false,
                ValueDocumentationMode.AllReadableProperties => IsValueDocumentationTarget(context),
                ValueDocumentationMode.ExcludeDtoLikeTypes =>
                    IsValueDocumentationTarget(context)
                    && !IsDtoLikeContainer(context.Member),
                ValueDocumentationMode.IndexersOnly => context.TargetKind == ValueTargetKind.Indexer,
                _ => IsValueDocumentationTarget(context)
            };
        }

        /// <summary>
        /// Determines whether the value target can require value documentation.
        /// </summary>
        /// <param name="context">
        /// The prepared value-analysis context.
        /// </param>
        /// <returns>
        /// True if the target can require value documentation; otherwise false.
        /// </returns>
        private static bool IsValueDocumentationTarget(ValueAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context.TargetKind == ValueTargetKind.ReadableProperty
                || context.TargetKind == ValueTargetKind.Indexer;
        }

        /// <summary>
        /// Determines whether a member belongs to a DTO-like data container.
        /// </summary>
        /// <param name="member">
        /// The member to inspect.
        /// </param>
        /// <returns>
        /// True if the member belongs to a DTO-like data container; otherwise false.
        /// </returns>
        private static bool IsDtoLikeContainer(MemberDeclarationSyntax member)
        {
            ArgumentNullException.ThrowIfNull(member);

            BaseTypeDeclarationSyntax? containingType = member
                .Ancestors()
                .OfType<BaseTypeDeclarationSyntax>()
                .FirstOrDefault();

            if (containingType == null)
            {
                return false;
            }

            string typeName = containingType.Identifier.ValueText;

            if (HasDtoLikeTypeName(typeName))
            {
                return true;
            }

            string? namespaceName = containingType
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()?
                .Name
                .ToString();

            return HasDtoLikeNamespace(namespaceName);
        }

        /// <summary>
        /// Determines whether a type name represents a DTO-like data container.
        /// </summary>
        /// <param name="typeName">
        /// The type name to inspect.
        /// </param>
        /// <returns>
        /// True if the type name is DTO-like; otherwise false.
        /// </returns>
        private static bool HasDtoLikeTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            return typeName.EndsWith("Dto", StringComparison.Ordinal)
                || typeName.EndsWith("DTO", StringComparison.Ordinal)
                || typeName.EndsWith("Result", StringComparison.Ordinal)
                || typeName.EndsWith("Report", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a namespace represents DTO-like data containers.
        /// </summary>
        /// <param name="namespaceName">
        /// The namespace name to inspect.
        /// </param>
        /// <returns>
        /// True if the namespace is DTO-like; otherwise false.
        /// </returns>
        private static bool HasDtoLikeNamespace(string? namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return false;
            }

            return namespaceName.EndsWith(".Dto", StringComparison.Ordinal)
                || namespaceName.Contains(".Dto.", StringComparison.Ordinal)
                || namespaceName.EndsWith(".DTO", StringComparison.Ordinal)
                || namespaceName.Contains(".DTO.", StringComparison.Ordinal);
        }

        /// <summary>
        /// Adds empty-value findings.
        /// </summary>
        /// <param name="findings">
        /// The target finding list.
        /// </param>
        /// <param name="tree">
        /// The syntax tree used for location calculation.
        /// </param>
        /// <param name="filePath">
        /// The file path used for reporting.
        /// </param>
        /// <param name="context">
        /// The prepared member analysis context.
        /// </param>
        private static void AddEmptyValueFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(findings);
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(context);

            if (context.TargetKind != ValueTargetKind.ReadableProperty
                && context.TargetKind != ValueTargetKind.Indexer)
            {
                return;
            }

            foreach (XmlElementSyntax valueTag in context.ValueTags)
            {
                if (XmlDocUtils.HasMeaningfulContent(valueTag))
                {
                    continue;
                }

                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "value",
                    XmlDocSmells.EmptyValueTag,
                    valueTag.SpanStart,
                    context.FindingContext,
                    snippet: valueTag.ToString(),
                    GetValueTargetKindForMessage(context),
                    GetValueTargetNameForMessage(context)));
            }
        }

        /// <summary>
        /// Adds duplicate-value findings.
        /// </summary>
        /// <param name="findings">
        /// The target finding list.
        /// </param>
        /// <param name="tree">
        /// The syntax tree used for location calculation.
        /// </param>
        /// <param name="filePath">
        /// The file path used for reporting.
        /// </param>
        /// <param name="context">
        /// The prepared member analysis context.
        /// </param>
        private static void AddDuplicateValueFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(findings);
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(context);

            if (context.TargetKind != ValueTargetKind.ReadableProperty
                && context.TargetKind != ValueTargetKind.Indexer)
            {
                return;
            }

            if (context.ValueTags.Count < 2)
            {
                return;
            }

            foreach (XmlElementSyntax duplicateTag in context.ValueTags.Skip(1))
            {
                findings.Add(FindingFactory.AtPosition(
                    tree,
                    filePath,
                    tagName: "value",
                    XmlDocSmells.DuplicateValueTag,
                    duplicateTag.SpanStart,
                    context.FindingContext,
                    snippet: duplicateTag.ToString(),
                    GetValueTargetKindForMessage(context),
                    GetValueTargetNameForMessage(context)));
            }
        }

        /// <summary>
        /// Adds invalid value-usage findings.
        /// </summary>
        /// <param name="findings">
        /// The target finding list.
        /// </param>
        /// <param name="tree">
        /// The syntax tree used for location calculation.
        /// </param>
        /// <param name="filePath">
        /// The file path used for reporting.
        /// </param>
        /// <param name="context">
        /// The prepared member analysis context.
        /// </param>
        private static void AddInvalidValueUsageFindings(
            List<Finding> findings,
            SyntaxTree tree,
            string filePath,
            ValueAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(findings);
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(context);

            foreach (XmlElementSyntax valueTag in context.ValueTags)
            {
                switch (context.TargetKind)
                {
                    case ValueTargetKind.WriteOnlyProperty:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.ValueOnWriteOnlyProperty,
                            valueTag.SpanStart,
                            context.FindingContext,
                            snippet: valueTag.ToString(),
                            GetValueTargetNameForMessage(context)));
                        break;

                    case ValueTargetKind.InvalidMember:
                        findings.Add(FindingFactory.AtPosition(
                            tree,
                            filePath,
                            tagName: "value",
                            XmlDocSmells.ValueOnInvalidMember,
                            valueTag.SpanStart,
                            context.FindingContext,
                            snippet: valueTag.ToString()));
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the declaration kind used for value-tag message formatting.
        /// </summary>
        /// <param name="context">
        /// The prepared value-analysis context.
        /// </param>
        /// <returns>
        /// The lower-case declaration kind used in the formatted finding message.
        /// </returns>
        private static string GetValueTargetKindForMessage(ValueAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.TargetKind == ValueTargetKind.Indexer)
            {
                return "indexer";
            }

            return "property";
        }

        /// <summary>
        /// Gets the declaration name used for value-tag message formatting.
        /// </summary>
        /// <param name="context">
        /// The prepared value-analysis context.
        /// </param>
        /// <returns>
        /// The declaration name used in the formatted finding message.
        /// </returns>
        private static string GetValueTargetNameForMessage(ValueAnalysisContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!string.IsNullOrWhiteSpace(context.MemberName))
            {
                return context.MemberName;
            }

            if (context.TargetKind == ValueTargetKind.Indexer)
            {
                return "this[]";
            }

            return "Unknown";
        }

        /// <summary>
        /// Classifies the member for value-tag analysis.
        /// </summary>
        /// <param name="member">
        /// The member to classify.
        /// </param>
        /// <returns>
        /// The matching value-target kind.
        /// </returns>
        private static ValueTargetKind ClassifyMember(MemberDeclarationSyntax member)
        {
            ArgumentNullException.ThrowIfNull(member);

            if (member is PropertyDeclarationSyntax property)
            {
                PropertyValueKind propertyValueKind = ClassifyProperty(property);

                if (propertyValueKind == PropertyValueKind.Readable)
                {
                    return ValueTargetKind.ReadableProperty;
                }

                if (propertyValueKind == PropertyValueKind.WriteOnly)
                {
                    return ValueTargetKind.WriteOnlyProperty;
                }

                return ValueTargetKind.InvalidMember;
            }

            if (member is IndexerDeclarationSyntax)
            {
                return ValueTargetKind.Indexer;
            }

            return ValueTargetKind.InvalidMember;
        }

        /// <summary>
        /// Gets the member name used for reporting.
        /// </summary>
        /// <param name="member">
        /// The member to inspect.
        /// </param>
        /// <returns>
        /// The member name if available; otherwise null.
        /// </returns>
        private static string? GetMemberName(MemberDeclarationSyntax member)
        {
            ArgumentNullException.ThrowIfNull(member);

            if (member is BaseTypeDeclarationSyntax baseTypeDeclaration)
            {
                return baseTypeDeclaration.Identifier.ValueText;
            }

            if (member is DelegateDeclarationSyntax delegateDeclaration)
            {
                return delegateDeclaration.Identifier.ValueText;
            }

            if (member is ConstructorDeclarationSyntax constructorDeclaration)
            {
                return constructorDeclaration.Identifier.ValueText;
            }

            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                return methodDeclaration.Identifier.ValueText;
            }

            if (member is PropertyDeclarationSyntax propertyDeclaration)
            {
                return propertyDeclaration.Identifier.ValueText;
            }

            if (member is IndexerDeclarationSyntax)
            {
                return "this[]";
            }

            if (member is FieldDeclarationSyntax fieldDeclaration)
            {
                return GetFirstVariableName(fieldDeclaration.Declaration);
            }

            if (member is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                return GetFirstVariableName(eventFieldDeclaration.Declaration);
            }

            if (member is EventDeclarationSyntax eventDeclaration)
            {
                return eventDeclaration.Identifier.ValueText;
            }

            if (member is OperatorDeclarationSyntax operatorDeclaration)
            {
                return "operator " + operatorDeclaration.OperatorToken.Text;
            }

            if (member is ConversionOperatorDeclarationSyntax conversionOperatorDeclaration)
            {
                return conversionOperatorDeclaration.ImplicitOrExplicitKeyword.Text
                    + " operator "
                    + conversionOperatorDeclaration.Type.ToString();
            }

            if (member is DestructorDeclarationSyntax destructorDeclaration)
            {
                return "~" + destructorDeclaration.Identifier.ValueText;
            }

            return null;
        }

        /// <summary>
        /// Gets the first variable name from a variable declaration.
        /// </summary>
        /// <param name="declaration">
        /// The variable declaration to inspect.
        /// </param>
        /// <returns>
        /// The first variable name, or null when no variable exists.
        /// </returns>
        private static string? GetFirstVariableName(VariableDeclarationSyntax declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);

            VariableDeclaratorSyntax? variable = declaration.Variables.FirstOrDefault();

            if (variable == null)
            {
                return null;
            }

            return variable.Identifier.ValueText;
        }

        /// <summary>
        /// Classifies the property for value-tag analysis.
        /// </summary>
        /// <param name="property">
        /// The property to classify.
        /// </param>
        /// <returns>
        /// The matching property value kind.
        /// </returns>
        private static PropertyValueKind ClassifyProperty(PropertyDeclarationSyntax property)
        {
            ArgumentNullException.ThrowIfNull(property);

            if (property.ExpressionBody != null)
            {
                return PropertyValueKind.Readable;
            }

            if (property.AccessorList == null)
            {
                return PropertyValueKind.Other;
            }

            bool hasGetter = property.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.GetAccessorDeclaration);

            bool hasSetter = property.AccessorList.Accessors.Any(
                static accessor => accessor.Kind() == SyntaxKind.SetAccessorDeclaration);

            if (hasGetter)
            {
                return PropertyValueKind.Readable;
            }

            if (hasSetter)
            {
                return PropertyValueKind.WriteOnly;
            }

            return PropertyValueKind.Other;
        }
    }
}
