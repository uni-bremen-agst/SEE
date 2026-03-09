using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Inheritdoc
{
    /// <summary>
    /// Resolves the primary declared symbol for documented syntax nodes.
    /// </summary>
    internal static class InheritdocDeclaredSymbolResolver
    {
        /// <summary>
        /// Gets the primary declared symbol for a documented syntax node.
        /// </summary>
        /// <param name="node">The documented syntax node.</param>
        /// <param name="semanticModel">The semantic model used to resolve symbols.</param>
        /// <returns>
        /// The declared symbol for the node if available; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Some declarations such as <see cref="EventFieldDeclarationSyntax"/> and
        /// <see cref="FieldDeclarationSyntax"/> declare their symbols on variable
        /// declarators rather than on the declaration node itself.
        /// </remarks>
        internal static ISymbol? GetDeclaredSymbol(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(semanticModel);

            if (node is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                VariableDeclaratorSyntax? variable =
                    eventFieldDeclaration.Declaration?.Variables.FirstOrDefault();

                return variable != null
                    ? semanticModel.GetDeclaredSymbol(variable)
                    : null;
            }

            if (node is FieldDeclarationSyntax fieldDeclaration)
            {
                VariableDeclaratorSyntax? variable =
                    fieldDeclaration.Declaration?.Variables.FirstOrDefault();

                return variable != null
                    ? semanticModel.GetDeclaredSymbol(variable)
                    : null;
            }

            return semanticModel.GetDeclaredSymbol(node);
        }
    }
}
