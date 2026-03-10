using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Performs transitive analysis of exceptions that may be thrown by a member
    /// by walking executable bodies and recursively inspecting invoked members.
    /// </summary>
    internal static class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects all exception types that may be thrown directly or transitively
        /// by the specified member.
        /// </summary>
        /// <param name="member">The member whose exception flow should be analyzed.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// A set containing all exception types that may be thrown by the member,
        /// including exceptions originating from recursively analyzed invoked members.
        /// </returns>
        public static HashSet<INamedTypeSymbol> CollectTransitivelyThrownExceptions(
            MemberDeclarationSyntax member,
            SemanticModel semanticModel)
        {
            HashSet<INamedTypeSymbol> exceptions =
                new(SymbolEqualityComparer.Default);

            HashSet<ISymbol> visited =
                new(SymbolEqualityComparer.Default);

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) || body == null)
            {
                return exceptions;
            }

            AnalyzeBody(body, semanticModel, exceptions, visited);

            return exceptions;
        }

        /// <summary>
        /// Analyzes a body node for directly thrown exceptions and recursively
        /// reachable callable symbols.
        /// </summary>
        /// <param name="body">The body node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeBody(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            AnalyzeThrows(body, semanticModel, exceptions);
            AnalyzeInvocations(body, semanticModel, exceptions, visited);
            AnalyzeObjectCreations(body, semanticModel, exceptions, visited);
            AnalyzePropertyAndIndexerAccesses(body, semanticModel, exceptions, visited);
        }

        /// <summary>
        /// Collects exception types that are thrown directly within the specified body.
        /// </summary>
        /// <param name="body">The body node to inspect for throw statements.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        private static void AnalyzeThrows(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions)
        {
            foreach (ThrowStatementSyntax throwStatement in body.DescendantNodes().OfType<ThrowStatementSyntax>())
            {
                if (throwStatement.Expression is ObjectCreationExpressionSyntax creation)
                {
                    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation.Type);

                    if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
                    {
                        exceptions.Add(typeSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves method invocations within the specified body and recursively
        /// analyzes the bodies of the invoked methods.
        /// </summary>
        /// <param name="body">The body node to inspect for invocations.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeInvocations(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            foreach (InvocationExpressionSyntax invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

                if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                if (!visited.Add(methodSymbol))
                {
                    continue;
                }

                AnalyzeSymbol(methodSymbol, semanticModel, exceptions, visited);
            }
        }

        /// <summary>
        /// Resolves constructor calls within the specified body and recursively
        /// analyzes the bodies of the called constructors.
        /// </summary>
        /// <param name="body">The body node to inspect for object creation expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeObjectCreations(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            foreach (ObjectCreationExpressionSyntax creation in body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation);

                if (symbolInfo.Symbol is not IMethodSymbol constructorSymbol)
                {
                    continue;
                }

                if (!visited.Add(constructorSymbol))
                {
                    continue;
                }

                AnalyzeSymbol(constructorSymbol, semanticModel, exceptions, visited);
            }
        }

        /// <summary>
        /// Resolves property and indexer accesses within the specified body and recursively
        /// analyzes the bodies of the accessed getters.
        /// </summary>
        /// <param name="body">The body node to inspect for property and indexer access.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzePropertyAndIndexerAccesses(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            foreach (MemberAccessExpressionSyntax memberAccess in body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol is IPropertySymbol propertySymbol &&
                    propertySymbol.GetMethod != null &&
                    visited.Add(propertySymbol.GetMethod))
                {
                    AnalyzeSymbol(propertySymbol.GetMethod, semanticModel, exceptions, visited);
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess in body.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol is IPropertySymbol indexerSymbol &&
                    indexerSymbol.GetMethod != null &&
                    visited.Add(indexerSymbol.GetMethod))
                {
                    AnalyzeSymbol(indexerSymbol.GetMethod, semanticModel, exceptions, visited);
                }
            }
        }

        /// <summary>
        /// Analyzes the syntax declarations of a callable symbol and recursively
        /// processes any executable bodies found there.
        /// </summary>
        /// <param name="symbol">The callable symbol to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeSymbol(
            ISymbol symbol,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(method, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                    }

                    continue;
                }

                if (node is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(constructor, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                    }

                    continue;
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeBody(accessor.Body, semanticModel, exceptions, visited);
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeBody(accessor.ExpressionBody.Expression, semanticModel, exceptions, visited);
                    }
                }
            }
        }
    }
}
