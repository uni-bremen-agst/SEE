using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides a mapping of allowed XML documentation tags per syntax node.
    /// 
    /// This class is used by the DOC140 detector to ensure that only tags
    /// valid for a given member kind are applied. It does **not** perform
    /// semantic validation (e.g., whether a <paramref> references an existing parameter).
    /// </summary>
    internal static class AllowedTagMatrix
    {
        /// <summary>
        /// Determines whether a given XML tag is allowed on the specified syntax node.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <param name="tagName">The XML tag name (without angle brackets).</param>
        /// <returns>
        /// <c>true</c> if the tag is allowed on the node; otherwise <c>false</c>.
        /// </returns>
        public static bool IsTagAllowed(SyntaxNode node, string tagName)
        {
            if (node == null)
            {
                return false;
            }

            // Always allowed tags
            switch (tagName)
            {
                case "summary":
                case "remarks":
                case "example":
                case "see":
                case "seealso":
                case "inheritdoc":
                    {
                        return true;
                    }
            }

            // Conditional tags based on member capabilities
            if (tagName == "param")
            {
                return SupportsParameters(node);
            }

            if (tagName == "typeparam")
            {
                return SupportsTypeParameters(node);
            }

            if (tagName == "returns")
            {
                return SupportsReturns(node);
            }

            if (tagName == "value")
            {
                return node is PropertyDeclarationSyntax || node is IndexerDeclarationSyntax;
            }

            if (tagName == "exception")
            {
                return SupportsExecutableBody(node);
            }

            // Unknown tags: allow here, other detectors handle unknown/misspelled tags
            return true;
        }

        /// <summary>
        /// Determines whether the node can have <param> tags.
        /// </summary>
        private static bool SupportsParameters(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax
                or ConstructorDeclarationSyntax
                or DelegateDeclarationSyntax
                or RecordDeclarationSyntax;
        }

        /// <summary>
        /// Determines whether the node can have <typeparam> tags.
        /// </summary>
        private static bool SupportsTypeParameters(SyntaxNode node)
        {
            if (node is TypeDeclarationSyntax typeDeclaration)
            {
                return typeDeclaration.TypeParameterList != null;
            }

            if (node is MethodDeclarationSyntax methodDeclaration)
            {
                return methodDeclaration.TypeParameterList != null;
            }

            if (node is DelegateDeclarationSyntax delegateDeclaration)
            {
                return delegateDeclaration.TypeParameterList != null;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the node can have a <returns> tag.
        /// </summary>
        private static bool SupportsReturns(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax methodDeclaration)
            {
                if (methodDeclaration.ReturnType is PredefinedTypeSyntax predefined)
                {
                    return predefined.Keyword.Text != "void";
                }

                return true;
            }

            if (node is DelegateDeclarationSyntax delegateDeclaration)
            {
                if (delegateDeclaration.ReturnType is PredefinedTypeSyntax predefined)
                {
                    return predefined.Keyword.Text != "void";
                }

                return true;
            }

            if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                return propertyDeclaration.AccessorList?.Accessors.Any(a => a.Keyword.Text == "get") == true;
            }

            if (node is IndexerDeclarationSyntax)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the node has an executable body and can use <exception> tags.
        /// </summary>
        private static bool SupportsExecutableBody(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax method)
            {
                return method.Body != null || method.ExpressionBody != null;
            }

            if (node is ConstructorDeclarationSyntax constructor)
            {
                return constructor.Body != null || constructor.ExpressionBody != null;
            }

            return false;
        }
    }
}
