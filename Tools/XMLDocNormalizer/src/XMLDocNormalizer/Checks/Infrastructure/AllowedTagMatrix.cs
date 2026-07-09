using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides the mapping of allowed XML documentation tags per syntax node.
    /// </summary>
    /// <remarks>
    /// This class is used by the DOC143 detector to ensure that only tags valid
    /// for a given declaration kind are applied. It does not perform semantic
    /// validation, such as checking whether a paramref tag references an existing
    /// parameter.
    /// </remarks>
    internal static class AllowedTagMatrix
    {
        /// <summary>
        /// Determines whether a given XML tag is allowed on the specified syntax node.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <param name="tagName">The XML tag name without angle brackets.</param>
        /// <returns>
        /// True if the tag is allowed on the node; otherwise false.
        /// </returns>
        public static bool IsTagAllowed(SyntaxNode node, string tagName)
        {
            if (node == null)
            {
                return false;
            }

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

            return true;
        }

        /// <summary>
        /// Determines whether a tag is handled by a specialized detector and should therefore
        /// be skipped by the generic invalid-member tag detector.
        /// </summary>
        /// <param name="tagName">The XML tag name without angle brackets.</param>
        /// <returns>
        /// True if the tag is handled by a specialized detector; otherwise false.
        /// </returns>
        public static bool IsHandledBySpecializedDetector(string tagName)
        {
            return tagName == "value";
        }

        /// <summary>
        /// Determines whether the node can have param tags.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if param tags are valid for the node; otherwise false.
        /// </returns>
        private static bool SupportsParameters(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax ||
                node is ConstructorDeclarationSyntax ||
                node is DelegateDeclarationSyntax ||
                node is IndexerDeclarationSyntax ||
                node is OperatorDeclarationSyntax ||
                node is ConversionOperatorDeclarationSyntax)
            {
                return true;
            }

            if (node is ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.ParameterList != null;
            }

            if (node is StructDeclarationSyntax structDeclaration)
            {
                return structDeclaration.ParameterList != null;
            }

            if (node is RecordDeclarationSyntax recordDeclaration)
            {
                return recordDeclaration.ParameterList != null;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the node can have typeparam tags.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if typeparam tags are valid for the node; otherwise false.
        /// </returns>
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
        /// Determines whether the node can have a returns tag.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if returns tags are valid for the node; otherwise false.
        /// </returns>
        private static bool SupportsReturns(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax methodDeclaration)
            {
                return IsNonVoidReturnType(methodDeclaration.ReturnType);
            }

            if (node is DelegateDeclarationSyntax delegateDeclaration)
            {
                return IsNonVoidReturnType(delegateDeclaration.ReturnType);
            }

            if (node is OperatorDeclarationSyntax operatorDeclaration)
            {
                return IsNonVoidReturnType(operatorDeclaration.ReturnType);
            }

            if (node is ConversionOperatorDeclarationSyntax)
            {
                return true;
            }

            if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                return propertyDeclaration.AccessorList?.Accessors.Any(accessor => accessor.Keyword.Text == "get") == true;
            }

            if (node is IndexerDeclarationSyntax)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the provided return type is not void.
        /// </summary>
        /// <param name="returnType">The return type syntax.</param>
        /// <returns>
        /// True if the return type is not void; otherwise false.
        /// </returns>
        private static bool IsNonVoidReturnType(TypeSyntax returnType)
        {
            if (returnType is PredefinedTypeSyntax predefined)
            {
                return predefined.Keyword.Text != "void";
            }

            return true;
        }

        /// <summary>
        /// Determines whether the node has an executable body and can use exception tags.
        /// </summary>
        /// <param name="node">The syntax node being documented.</param>
        /// <returns>
        /// True if the node has an executable body; otherwise false.
        /// </returns>
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

            if (node is OperatorDeclarationSyntax operatorDeclaration)
            {
                return operatorDeclaration.Body != null || operatorDeclaration.ExpressionBody != null;
            }

            if (node is ConversionOperatorDeclarationSyntax conversionOperatorDeclaration)
            {
                return conversionOperatorDeclaration.Body != null || conversionOperatorDeclaration.ExpressionBody != null;
            }

            return false;
        }
    }
}
