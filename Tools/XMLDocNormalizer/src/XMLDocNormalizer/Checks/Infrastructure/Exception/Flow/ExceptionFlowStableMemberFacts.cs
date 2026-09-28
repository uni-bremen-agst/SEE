using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Classifies immutable auto-properties without discovering their values.
    /// </summary>
    internal static class ExceptionFlowStableMemberFacts
    {
        /// <summary>
        /// Determines whether a declaration is a get-only auto-property
        /// without a custom getter body.
        /// </summary>
        /// <param name="propertyDeclaration">
        /// The property declaration to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the declaration is a supported get-only
        /// auto-property; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsSupportedGetOnlyAutoProperty(
            PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.ExpressionBody != null
                || propertyDeclaration.AccessorList == null
                || propertyDeclaration.AccessorList.Accessors.Count != 1)
            {
                return false;
            }

            AccessorDeclarationSyntax accessor =
                propertyDeclaration.AccessorList.Accessors[0];

            return accessor.IsKind(SyntaxKind.GetAccessorDeclaration)
                && accessor.Body == null
                && accessor.ExpressionBody == null
                && accessor.SemicolonToken.IsKind(SyntaxKind.SemicolonToken);
        }

        /// <summary>
        /// Determines whether a property is an auto-property whose value cannot
        /// be replaced after object initialization.
        /// </summary>
        /// <param name="propertySymbol">The property to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for a supported get-only or init-only
        /// auto-property; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsSupportedStableAutoProperty(
            IPropertySymbol propertySymbol)
        {
            if (propertySymbol.IsStatic
                || propertySymbol.IsIndexer
                || propertySymbol.ReturnsByRef
                || propertySymbol.ReturnsByRefReadonly
                || propertySymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode propertyNode =
                propertySymbol.DeclaringSyntaxReferences[0].GetSyntax();

            if (propertyNode is not PropertyDeclarationSyntax propertyDeclaration)
            {
                return false;
            }

            if (propertySymbol.SetMethod == null)
            {
                return IsSupportedGetOnlyAutoProperty(propertyDeclaration);
            }

            return propertySymbol.SetMethod.IsInitOnly
                && IsSupportedInitOnlyAutoProperty(propertyDeclaration);
        }

        /// <summary>
        /// Determines whether a declaration is an init-only auto-property
        /// without custom accessor bodies.
        /// </summary>
        /// <param name="propertyDeclaration">
        /// The property declaration to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the declaration consists of automatic
        /// <c>get</c> and <c>init</c> accessors; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSupportedInitOnlyAutoProperty(
            PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration.ExpressionBody != null
                || propertyDeclaration.AccessorList == null
                || propertyDeclaration.AccessorList.Accessors.Count != 2)
            {
                return false;
            }

            AccessorDeclarationSyntax? getter =
                propertyDeclaration.AccessorList.Accessors.FirstOrDefault(
                    static accessor =>
                        accessor.IsKind(SyntaxKind.GetAccessorDeclaration));

            AccessorDeclarationSyntax? initializer =
                propertyDeclaration.AccessorList.Accessors.FirstOrDefault(
                    static accessor =>
                        accessor.IsKind(SyntaxKind.InitAccessorDeclaration));

            return getter != null
                && getter.Body == null
                && getter.ExpressionBody == null
                && getter.SemicolonToken.IsKind(SyntaxKind.SemicolonToken)
                && initializer != null
                && initializer.Body == null
                && initializer.ExpressionBody == null
                && initializer.SemicolonToken.IsKind(SyntaxKind.SemicolonToken);
        }
    }
}
