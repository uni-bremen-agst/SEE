using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides extension methods for syntax nodes used by XML documentation analysis.
    /// </summary>
    internal static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Determines whether the syntax node represents a declaration that supports XML documentation.
        /// </summary>
        /// <param name="node">
        /// The syntax node to classify.
        /// </param>
        /// <returns>
        /// True if the syntax node represents a supported documentable declaration; otherwise, false.
        /// </returns>
        public static bool SupportsXmlDocumentation(this SyntaxNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return node is ClassDeclarationSyntax
                or StructDeclarationSyntax
                or InterfaceDeclarationSyntax
                or EnumDeclarationSyntax
                or DelegateDeclarationSyntax
                or RecordDeclarationSyntax
                or ConstructorDeclarationSyntax
                or MethodDeclarationSyntax
                or PropertyDeclarationSyntax
                or IndexerDeclarationSyntax
                or FieldDeclarationSyntax
                or EventDeclarationSyntax
                or EventFieldDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax
                or DestructorDeclarationSyntax
                or EnumMemberDeclarationSyntax;
        }

        /// <summary>
        /// Gets a human-readable declaration kind for missing-documentation messages.
        /// </summary>
        /// <param name="node">
        /// The syntax node to classify.
        /// </param>
        /// <returns>
        /// A lower-case declaration kind suitable for report messages.
        /// </returns>
        public static string GetDocumentationDeclarationKind(this SyntaxNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node is ClassDeclarationSyntax)
            {
                return "class";
            }

            if (node is StructDeclarationSyntax)
            {
                return "struct";
            }

            if (node is InterfaceDeclarationSyntax)
            {
                return "interface";
            }

            if (node is EnumDeclarationSyntax)
            {
                return "enum";
            }

            if (node is DelegateDeclarationSyntax)
            {
                return "delegate";
            }

            if (node is RecordDeclarationSyntax recordDeclaration)
            {
                if (recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
                {
                    return "record struct";
                }

                return "record";
            }

            if (node is ConstructorDeclarationSyntax)
            {
                return "constructor";
            }

            if (node is MethodDeclarationSyntax)
            {
                return "method";
            }

            if (node is PropertyDeclarationSyntax)
            {
                return "property";
            }

            if (node is IndexerDeclarationSyntax)
            {
                return "indexer";
            }

            if (node is FieldDeclarationSyntax)
            {
                return "field";
            }

            if (node is EventDeclarationSyntax || node is EventFieldDeclarationSyntax)
            {
                return "event";
            }

            if (node is OperatorDeclarationSyntax)
            {
                return "operator";
            }

            if (node is ConversionOperatorDeclarationSyntax)
            {
                return "conversion operator";
            }

            if (node is DestructorDeclarationSyntax)
            {
                return "destructor";
            }

            if (node is EnumMemberDeclarationSyntax)
            {
                return "enum member";
            }

            return "declaration";
        }

        /// <summary>
        /// Gets the declaration name used in missing-documentation messages.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <returns>
        /// The best available declaration name.
        /// </returns>
        public static string GetDocumentationDeclarationName(this SyntaxNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node is BaseTypeDeclarationSyntax baseTypeDeclaration)
            {
                return baseTypeDeclaration.Identifier.ValueText;
            }

            if (node is DelegateDeclarationSyntax delegateDeclaration)
            {
                return delegateDeclaration.Identifier.ValueText;
            }

            if (node is ConstructorDeclarationSyntax constructorDeclaration)
            {
                return constructorDeclaration.Identifier.ValueText;
            }

            if (node is MethodDeclarationSyntax methodDeclaration)
            {
                return methodDeclaration.Identifier.ValueText;
            }

            if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                return propertyDeclaration.Identifier.ValueText;
            }

            if (node is IndexerDeclarationSyntax)
            {
                return "this[]";
            }

            if (node is FieldDeclarationSyntax fieldDeclaration)
            {
                return GetFirstVariableName(fieldDeclaration.Declaration);
            }

            if (node is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                return GetFirstVariableName(eventFieldDeclaration.Declaration);
            }

            if (node is EventDeclarationSyntax eventDeclaration)
            {
                return eventDeclaration.Identifier.ValueText;
            }

            if (node is OperatorDeclarationSyntax operatorDeclaration)
            {
                return "operator " + operatorDeclaration.OperatorToken.Text;
            }

            if (node is ConversionOperatorDeclarationSyntax conversionOperatorDeclaration)
            {
                return conversionOperatorDeclaration.ImplicitOrExplicitKeyword.Text
                    + " operator "
                    + conversionOperatorDeclaration.Type.ToString();
            }

            if (node is DestructorDeclarationSyntax destructorDeclaration)
            {
                return "~" + destructorDeclaration.Identifier.ValueText;
            }

            if (node is EnumMemberDeclarationSyntax enumMemberDeclaration)
            {
                return enumMemberDeclaration.Identifier.ValueText;
            }

            return node.Kind().ToString();
        }

        /// <summary>
        /// Gets the first variable name from a variable declaration.
        /// </summary>
        /// <param name="declaration">
        /// The variable declaration to inspect.
        /// </param>
        /// <returns>
        /// The first variable name, or Unknown when no variable exists.
        /// </returns>
        private static string GetFirstVariableName(VariableDeclarationSyntax declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);

            VariableDeclaratorSyntax? variable = declaration.Variables.FirstOrDefault();

            if (variable == null)
            {
                return "Unknown";
            }

            return variable.Identifier.ValueText;
        }
    }
}
