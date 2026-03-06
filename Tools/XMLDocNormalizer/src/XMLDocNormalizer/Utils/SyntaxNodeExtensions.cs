using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides extension methods for <see cref="SyntaxNode"/> related to XML documentation analysis.
    /// </summary>
    internal static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Determines the specific "missing documentation" smell that applies to the given syntax node.
        /// </summary>
        /// <param name="node">
        /// The syntax node representing a documentable declaration.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="XmlDocSmell"/> describing the missing documentation problem,
        /// or <c>null</c> if the node type does not support XML documentation analysis.
        /// </returns>
        /// <remarks>
        /// This method replaces the generic "missing documentation" smell by returning
        /// element-specific smells such as missing documentation for classes, methods,
        /// properties, or other supported member types.
        /// <para>
        /// The classification is based on the concrete Roslyn syntax node type.
        /// </para>
        /// </remarks>
        public static XmlDocSmell? GetMissingDocumentationSmell(this SyntaxNode node)
        {
            return node switch
            {
                ClassDeclarationSyntax => XmlDocSmells.MissingClassDocumentation,
                StructDeclarationSyntax => XmlDocSmells.MissingStructDocumentation,
                InterfaceDeclarationSyntax => XmlDocSmells.MissingInterfaceDocumentation,
                EnumDeclarationSyntax => XmlDocSmells.MissingEnumDocumentation,
                DelegateDeclarationSyntax => XmlDocSmells.MissingDelegateDocumentation,

                RecordDeclarationSyntax recordDecl
                    when recordDecl.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                    => XmlDocSmells.MissingRecordStructDocumentation,

                RecordDeclarationSyntax => XmlDocSmells.MissingRecordDocumentation,

                ConstructorDeclarationSyntax => XmlDocSmells.MissingConstructorDocumentation,
                MethodDeclarationSyntax => XmlDocSmells.MissingMethodDocumentation,
                PropertyDeclarationSyntax => XmlDocSmells.MissingPropertyDocumentation,
                IndexerDeclarationSyntax => XmlDocSmells.MissingIndexerDocumentation,
                FieldDeclarationSyntax => XmlDocSmells.MissingFieldDocumentation,
                EventDeclarationSyntax => XmlDocSmells.MissingEventDocumentation,
                EventFieldDeclarationSyntax => XmlDocSmells.MissingEventFieldDocumentation,
                OperatorDeclarationSyntax => XmlDocSmells.MissingOperatorDocumentation,
                ConversionOperatorDeclarationSyntax => XmlDocSmells.MissingConversionOperatorDocumentation,
                DestructorDeclarationSyntax => XmlDocSmells.MissingDestructorDocumentation,
                EnumMemberDeclarationSyntax => XmlDocSmells.MissingEnumMemberDocumentation,

                _ => null
            };
        }
    }
}