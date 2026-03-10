using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        /// <param name="body">The body node to inspect for throw statements and throw expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        private static void AnalyzeThrows(
            SyntaxNode body,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions)
        {
            foreach (ThrowStatementSyntax throwStatement in body.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>())
            {
                AddThrownExceptionType(exceptions, semanticModel, throwStatement.Expression);
            }

            foreach (ThrowExpressionSyntax throwExpression in body.DescendantNodesAndSelf().OfType<ThrowExpressionSyntax>())
            {
                AddThrownExceptionType(exceptions, semanticModel, throwExpression.Expression);
            }
        }

        /// <summary>
        /// Resolves the exception type from a thrown expression and adds it to the target set
        /// if it represents an object creation of a named type.
        /// </summary>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="expression">The thrown expression to inspect.</param>
        private static void AddThrownExceptionType(
            HashSet<INamedTypeSymbol> exceptions,
            SemanticModel semanticModel,
            ExpressionSyntax? expression)
        {
            if (expression is not ObjectCreationExpressionSyntax creation)
            {
                return;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation.Type);

            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                exceptions.Add(typeSymbol);
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
            foreach (InvocationExpressionSyntax invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
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
            foreach (ObjectCreationExpressionSyntax creation in body.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
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
        /// analyzes the bodies of the accessed members.
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
            foreach (MemberAccessExpressionSyntax memberAccess in body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    AnalyzePropertyLikeSymbol(propertySymbol, semanticModel, exceptions, visited);
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess in body.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol is IPropertySymbol indexerSymbol)
                {
                    AnalyzePropertyLikeSymbol(indexerSymbol, semanticModel, exceptions, visited);
                }
            }
        }

        /// <summary>
        /// Analyzes a property-like symbol by first trying its getter symbol and
        /// then falling back to the property or indexer declaration itself.
        /// </summary>
        /// <param name="propertySymbol">The property or indexer symbol to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="exceptions">The target set collecting discovered exception types.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzePropertyLikeSymbol(
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            bool analyzedGetter = false;

            if (propertySymbol.GetMethod != null && visited.Add(propertySymbol.GetMethod))
            {
                analyzedGetter = AnalyzeSymbol(propertySymbol.GetMethod, semanticModel, exceptions, visited);
            }

            if (!analyzedGetter && visited.Add(propertySymbol))
            {
                AnalyzeSymbol(propertySymbol, semanticModel, exceptions, visited);
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
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSymbol(
            ISymbol symbol,
            SemanticModel semanticModel,
            HashSet<INamedTypeSymbol> exceptions,
            HashSet<ISymbol> visited)
        {
            bool analyzedAnyBody = false;

            foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(method, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(constructor, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is PropertyDeclarationSyntax property)
                {
                    if (SyntaxUtils.TryGetMemberBody(property, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (getter.Body != null)
                        {
                            AnalyzeBody(getter.Body, semanticModel, exceptions, visited);
                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeBody(getter.ExpressionBody.Expression, semanticModel, exceptions, visited);
                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is IndexerDeclarationSyntax indexer)
                {
                    if (SyntaxUtils.TryGetMemberBody(indexer, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, semanticModel, exceptions, visited);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = indexer.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (getter.Body != null)
                        {
                            AnalyzeBody(getter.Body, semanticModel, exceptions, visited);
                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeBody(getter.ExpressionBody.Expression, semanticModel, exceptions, visited);
                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeBody(accessor.Body, semanticModel, exceptions, visited);
                        analyzedAnyBody = true;
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeBody(accessor.ExpressionBody.Expression, semanticModel, exceptions, visited);
                        analyzedAnyBody = true;
                    }
                }
            }

            return analyzedAnyBody;
        }
    }
}
