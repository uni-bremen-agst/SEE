using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Builds study-oriented finding context metadata from Roslyn syntax nodes.
    /// </summary>
    /// <remarks>
    /// This builder is syntax-based and can therefore be used by detectors that do not require semantic model access.
    /// </remarks>
    internal static class FindingContextBuilder
    {
        /// <summary>
        /// Creates a finding context for a source declaration.
        /// </summary>
        /// <param name="owner">
        /// The syntax node that owns the XML documentation comment.
        /// If this value is null, an unknown owner context is returned while preserving the provided subject metadata.
        /// </param>
        /// <param name="subjectKind">
        /// The concrete documentation subject affected by the finding.
        /// Examples are Declaration, Parameter, TypeParameter, ReturnValue, SummaryTag, RemarksTag, TagOrder, or NamespaceDocumentation.
        /// </param>
        /// <param name="targetName">
        /// The concrete affected target name, if one exists.
        /// For example, this can be a parameter name, type parameter name, XML tag name, or namespace name.
        /// </param>
        /// <param name="projectName">
        /// The analyzed project name, if available.
        /// </param>
        /// <param name="filePath">
        /// The source file path used to infer generated-code and test-file metadata.
        /// </param>
        /// <returns>
        /// A populated finding context.
        /// </returns>
        public static FindingContext ForDeclaration(
            SyntaxNode? owner,
            string subjectKind,
            string? targetName = null,
            string? projectName = null,
            string? filePath = null)
        {
            if (owner == null)
            {
                return FindingContext.Unknown with
                {
                    SubjectKind = Normalize(subjectKind),
                    TargetName = targetName,
                    ProjectName = projectName,
                    IsGenerated = IsGeneratedFile(filePath),
                    IsTestFile = IsTestFile(filePath)
                };
            }

            return new FindingContext(
                OwnerKind: GetOwnerKind(owner),
                SubjectKind: Normalize(subjectKind),
                Accessibility: GetAccessibility(owner),
                SymbolName: GetSymbolName(owner),
                ContainingType: GetContainingType(owner),
                ContainingNamespace: GetContainingNamespace(owner),
                TargetName: targetName,
                ProjectName: projectName,
                IsGenerated: IsGeneratedFile(filePath),
                IsTestFile: IsTestFile(filePath));
        }

        /// <summary>
        /// Creates a finding context for a documentation comment.
        /// </summary>
        /// <param name="comment">
        /// The documentation comment whose owning declaration should be resolved.
        /// </param>
        /// <param name="subjectKind">
        /// The concrete documentation subject affected by the finding.
        /// Examples are SeeTag, SeeAlsoTag, ReferenceTag, or NamespaceDocumentation.
        /// </param>
        /// <param name="targetName">
        /// The concrete affected target name, if one exists.
        /// For reference tags, this can be a cref, href, or langword target.
        /// </param>
        /// <param name="projectName">
        /// The analyzed project name, if available.
        /// </param>
        /// <param name="filePath">
        /// The source file path used to infer generated-code and test-file metadata.
        /// </param>
        /// <returns>
        /// A populated finding context for the owner declaration of the documentation comment.
        /// If no owner declaration can be resolved, an unknown owner context is returned.
        /// </returns>
        public static FindingContext ForDocumentationComment(
            DocumentationCommentTriviaSyntax comment,
            string subjectKind,
            string? targetName = null,
            string? projectName = null,
            string? filePath = null)
        {
            ArgumentNullException.ThrowIfNull(comment);

            SyntaxNode? owner = FindOwnerDeclaration(comment);

            return ForDeclaration(
                owner,
                subjectKind,
                targetName,
                projectName,
                filePath);
        }

        /// <summary>
        /// Resolves the declaration that owns a documentation comment.
        /// </summary>
        /// <param name="comment">
        /// The documentation comment whose owner should be resolved.
        /// </param>
        /// <returns>
        /// The owning declaration syntax node if it can be resolved; otherwise null.
        /// </returns>
        private static SyntaxNode? FindOwnerDeclaration(DocumentationCommentTriviaSyntax comment)
        {
            SyntaxToken token = comment.ParentTrivia.Token;
            SyntaxNode? current = token.Parent;

            while (current != null)
            {
                if (current is MemberDeclarationSyntax)
                {
                    return current;
                }

                if (current is BaseNamespaceDeclarationSyntax)
                {
                    return current;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets the normalized owner kind for a syntax node.
        /// </summary>
        /// <param name="node">The syntax node to classify.</param>
        /// <returns>
        /// A stable owner kind string.
        /// </returns>
        private static string GetOwnerKind(SyntaxNode node)
        {
            switch (node)
            {
                case ClassDeclarationSyntax:
                    {
                        return "Class";
                    }

                case StructDeclarationSyntax:
                    {
                        return "Struct";
                    }

                case InterfaceDeclarationSyntax:
                    {
                        return "Interface";
                    }

                case EnumDeclarationSyntax:
                    {
                        return "Enum";
                    }

                case DelegateDeclarationSyntax:
                    {
                        return "Delegate";
                    }

                case RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword):
                    {
                        return "RecordStruct";
                    }

                case RecordDeclarationSyntax:
                    {
                        return "Record";
                    }

                case ConstructorDeclarationSyntax:
                    {
                        return "Constructor";
                    }

                case MethodDeclarationSyntax:
                    {
                        return "Method";
                    }

                case PropertyDeclarationSyntax:
                    {
                        return "Property";
                    }

                case IndexerDeclarationSyntax:
                    {
                        return "Indexer";
                    }

                case FieldDeclarationSyntax:
                    {
                        return "Field";
                    }

                case EventDeclarationSyntax:
                    {
                        return "Event";
                    }

                case EventFieldDeclarationSyntax:
                    {
                        return "EventField";
                    }

                case OperatorDeclarationSyntax:
                    {
                        return "Operator";
                    }

                case ConversionOperatorDeclarationSyntax:
                    {
                        return "ConversionOperator";
                    }

                case DestructorDeclarationSyntax:
                    {
                        return "Destructor";
                    }

                case EnumMemberDeclarationSyntax:
                    {
                        return "EnumMember";
                    }

                case BaseNamespaceDeclarationSyntax:
                    {
                        return "Namespace";
                    }

                default:
                    {
                        return node.Kind().ToString();
                    }
            }
        }

        /// <summary>
        /// Gets the declared or inferred accessibility for a syntax node.
        /// </summary>
        /// <param name="node">The syntax node to inspect.</param>
        /// <returns>
        /// A stable accessibility string.
        /// </returns>
        private static string GetAccessibility(SyntaxNode node)
        {
            SyntaxTokenList modifiers = GetModifiers(node);

            bool isPublic = modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
            bool isPrivate = modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword));
            bool isProtected = modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ProtectedKeyword));
            bool isInternal = modifiers.Any(modifier => modifier.IsKind(SyntaxKind.InternalKeyword));

            if (isPrivate && isProtected)
            {
                return "PrivateProtected";
            }

            if (isProtected && isInternal)
            {
                return "ProtectedInternal";
            }

            if (isPublic)
            {
                return "Public";
            }

            if (isProtected)
            {
                return "Protected";
            }

            if (isInternal)
            {
                return "Internal";
            }

            if (isPrivate)
            {
                return "Private";
            }

            if (node is EnumMemberDeclarationSyntax)
            {
                return "Public";
            }

            if (node.Parent is InterfaceDeclarationSyntax)
            {
                return "Public";
            }

            if (node is BaseTypeDeclarationSyntax && IsTopLevelType(node))
            {
                return "Internal";
            }

            if (node is BaseNamespaceDeclarationSyntax)
            {
                return "NotApplicable";
            }

            return "Private";
        }

        /// <summary>
        /// Determines whether a type declaration is declared directly in a compilation unit or namespace.
        /// </summary>
        /// <param name="node">The syntax node to inspect.</param>
        /// <returns>
        /// True if the node is a top-level type declaration; otherwise false.
        /// </returns>
        private static bool IsTopLevelType(SyntaxNode node)
        {
            if (node.Parent is CompilationUnitSyntax)
            {
                return true;
            }

            if (node.Parent is BaseNamespaceDeclarationSyntax)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the modifier token list for a supported declaration node.
        /// </summary>
        /// <param name="node">The syntax node to inspect.</param>
        /// <returns>
        /// The declaration modifiers, or an empty token list if the node has no modifiers.
        /// </returns>
        private static SyntaxTokenList GetModifiers(SyntaxNode node)
        {
            switch (node)
            {
                case BaseTypeDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case DelegateDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case BaseMethodDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case PropertyDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case IndexerDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case FieldDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case EventDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                case EventFieldDeclarationSyntax declaration:
                    {
                        return declaration.Modifiers;
                    }

                default:
                    {
                        return default;
                    }
            }
        }

        /// <summary>
        /// Gets the source symbol name for a syntax node.
        /// </summary>
        /// <param name="node">The syntax node to inspect.</param>
        /// <returns>
        /// The symbol name if it can be derived syntactically; otherwise Unknown.
        /// </returns>
        private static string GetSymbolName(SyntaxNode node)
        {
            switch (node)
            {
                case BaseTypeDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case DelegateDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case ConstructorDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case DestructorDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case MethodDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case PropertyDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case IndexerDeclarationSyntax:
                    {
                        return "this[]";
                    }

                case FieldDeclarationSyntax declaration:
                    {
                        return string.Join(",", declaration.Declaration.Variables.Select(variable => variable.Identifier.ValueText));
                    }

                case EventDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case EventFieldDeclarationSyntax declaration:
                    {
                        return string.Join(",", declaration.Declaration.Variables.Select(variable => variable.Identifier.ValueText));
                    }

                case OperatorDeclarationSyntax declaration:
                    {
                        return "operator " + declaration.OperatorToken.ValueText;
                    }

                case ConversionOperatorDeclarationSyntax declaration:
                    {
                        return "operator " + declaration.Type;
                    }

                case EnumMemberDeclarationSyntax declaration:
                    {
                        return declaration.Identifier.ValueText;
                    }

                case BaseNamespaceDeclarationSyntax declaration:
                    {
                        return declaration.Name.ToString();
                    }

                default:
                    {
                        return "Unknown";
                    }
            }
        }

        /// <summary>
        /// Gets the containing type path for a syntax node.
        /// </summary>
        /// <param name="node">The syntax node to inspect.</param>
        /// <returns>
        /// A dotted containing type path, or None if the node is not inside a type declaration.
        /// </returns>
        private static string GetContainingType(SyntaxNode node)
        {
            List<string> names = node
                .AncestorsAndSelf()
                .OfType<BaseTypeDeclarationSyntax>()
                .Reverse()
                .Select(typeDeclaration => typeDeclaration.Identifier.ValueText)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (names.Count == 0)
            {
                return "None";
            }

            return string.Join(".", names);
        }

        /// <summary>
        /// Gets the containing namespace for a syntax node.
        /// </summary>
        /// <param name="node">The syntax node to inspect.</param>
        /// <returns>
        /// The containing namespace name, or GlobalNamespace if no namespace declaration exists.
        /// </returns>
        private static string GetContainingNamespace(SyntaxNode node)
        {
            BaseNamespaceDeclarationSyntax? namespaceDeclaration = node
                .AncestorsAndSelf()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (namespaceDeclaration == null)
            {
                return "GlobalNamespace";
            }

            return namespaceDeclaration.Name.ToString();
        }

        /// <summary>
        /// Normalizes a string value for stable report output.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <returns>
        /// The original value if it is not empty; otherwise Unknown.
        /// </returns>
        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unknown";
            }

            return value;
        }

        /// <summary>
        /// Determines whether a file path appears to represent generated source code.
        /// </summary>
        /// <param name="filePath">The file path to inspect.</param>
        /// <returns>
        /// True if the file appears generated, false if it appears handwritten, or null if no file path is available.
        /// </returns>
        private static bool? IsGeneratedFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            string normalized = filePath.Replace('\\', '/');
            string fileName = Path.GetFileName(normalized);

            if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.Contains("/Generated/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a file path appears to represent test source code.
        /// </summary>
        /// <param name="filePath">The file path to inspect.</param>
        /// <returns>
        /// True if the file appears to be test code, false if it does not, or null if no file path is available.
        /// </returns>
        private static bool? IsTestFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            string normalized = filePath.Replace('\\', '/');
            string fileName = Path.GetFileName(normalized);

            if (normalized.Contains("/Tests/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.Contains("/Test/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (fileName.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (fileName.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
