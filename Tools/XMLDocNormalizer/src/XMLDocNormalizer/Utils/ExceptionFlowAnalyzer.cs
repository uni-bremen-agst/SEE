using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Performs transitive analysis of exceptions that may be thrown by a member
    /// by walking executable bodies and recursively inspecting invoked members.
    /// </summary>
    internal static class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Analyzes all exception types that may be thrown directly or transitively
        /// by the specified member.
        /// </summary>
        /// <param name="member">The member whose exception flow should be analyzed.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// A result object containing all proven thrown exception types and a flag
        /// indicating whether at least one relevant transitive path was not decidable.
        /// </returns>
        public static ExceptionFlowAnalysisResult AnalyzeTransitivelyThrownExceptions(
            MemberDeclarationSyntax member,
            SemanticModel semanticModel)
        {
            ExceptionFlowAnalysisResult result = new();

            HashSet<ISymbol> visited =
                new(SymbolEqualityComparer.Default);

            if (!SyntaxUtils.TryGetMemberBody(member, out SyntaxNode? body) || body == null)
            {
                return result;
            }

            AnalyzeBody(body, semanticModel, result, visited);

            return result;
        }

        /// <summary>
        /// Analyzes a body node for directly thrown exceptions and recursively
        /// reachable callable symbols.
        /// </summary>
        /// <param name="body">The body node to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeBody(
            SyntaxNode body,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            SemanticModel? bodySemanticModel = TryGetSemanticModelFor(body, semanticModel);

            if (bodySemanticModel == null)
            {
                result.HasUncertainPaths = true;
                return;
            }

            AnalyzeThrows(body, bodySemanticModel, result);
            AnalyzeInvocations(body, bodySemanticModel, result, visited);
            AnalyzeObjectCreations(body, bodySemanticModel, result, visited);
            AnalyzePropertyAndIndexerAccesses(body, bodySemanticModel, result, visited);
        }

        /// <summary>
        /// Tries to get the semantic model that belongs to the syntax tree of the specified node.
        /// </summary>
        /// <param name="node">The node whose syntax tree should be analyzed.</param>
        /// <param name="fallbackSemanticModel">The fallback semantic model of the current analysis context.</param>
        /// <returns>
        /// The semantic model for the node's syntax tree, or <see langword="null"/> if the tree
        /// does not belong to the current compilation.
        /// </returns>
        private static SemanticModel? TryGetSemanticModelFor(
            SyntaxNode node,
            SemanticModel fallbackSemanticModel)
        {
            if (ReferenceEquals(node.SyntaxTree, fallbackSemanticModel.SyntaxTree))
            {
                return fallbackSemanticModel;
            }

            Compilation compilation = fallbackSemanticModel.Compilation;

            if (!compilation.SyntaxTrees.Contains(node.SyntaxTree))
            {
                return null;
            }

            return compilation.GetSemanticModel(node.SyntaxTree);
        }

        /// <summary>
        /// Collects exception types that are thrown directly within the specified body.
        /// </summary>
        /// <param name="body">The body node to inspect for throw statements and throw expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        private static void AnalyzeThrows(
            SyntaxNode body,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result)
        {
            foreach (ThrowStatementSyntax throwStatement in body.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>())
            {
                AddThrownExceptionType(result, semanticModel, throwStatement.Expression);
            }

            foreach (ThrowExpressionSyntax throwExpression in body.DescendantNodesAndSelf().OfType<ThrowExpressionSyntax>())
            {
                AddThrownExceptionType(result, semanticModel, throwExpression.Expression);
            }
        }

        /// <summary>
        /// Resolves the exception type from a thrown expression and adds it to the result
        /// if it represents an object creation of a named type.
        /// </summary>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="expression">The thrown expression to inspect.</param>
        private static void AddThrownExceptionType(
            ExceptionFlowAnalysisResult result,
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
                result.ThrownExceptions.Add(typeSymbol);
            }
        }

        /// <summary>
        /// Resolves method invocations within the specified body and recursively
        /// analyzes the bodies of the invoked methods.
        /// </summary>
        /// <param name="body">The body node to inspect for invocations.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeInvocations(
            SyntaxNode body,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
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

                if (!AnalyzeSymbol(methodSymbol, semanticModel, result, visited))
                {
                    result.HasUncertainPaths = true;
                }
            }
        }

        /// <summary>
        /// Resolves constructor calls within the specified body and recursively
        /// analyzes the bodies of the called constructors.
        /// Object creations that are part of a direct throw are ignored here because
        /// they are already handled by direct throw analysis.
        /// </summary>
        /// <param name="body">The body node to inspect for object creation expressions.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzeObjectCreations(
            SyntaxNode body,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (ObjectCreationExpressionSyntax creation in body.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>())
            {
                if (IsPartOfDirectThrow(creation))
                {
                    continue;
                }

                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(creation);

                if (symbolInfo.Symbol is not IMethodSymbol constructorSymbol)
                {
                    continue;
                }

                if (!visited.Add(constructorSymbol))
                {
                    continue;
                }

                if (!AnalyzeSymbol(constructorSymbol, semanticModel, result, visited))
                {
                    result.HasUncertainPaths = true;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified object creation is part of a direct throw statement
        /// or throw expression and is therefore already covered by direct throw analysis.
        /// </summary>
        /// <param name="creation">The object creation to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the object creation is directly thrown; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPartOfDirectThrow(ObjectCreationExpressionSyntax creation)
        {
            return creation.Parent is ThrowStatementSyntax
                || creation.Parent is ThrowExpressionSyntax;
        }

        /// <summary>
        /// Resolves property and indexer accesses within the specified body and recursively
        /// analyzes the bodies of the accessed members.
        /// </summary>
        /// <param name="body">The body node to inspect for property and indexer access.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        private static void AnalyzePropertyAndIndexerAccesses(
            SyntaxNode body,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            foreach (MemberAccessExpressionSyntax memberAccess in body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    if (!AnalyzePropertyLikeSymbol(propertySymbol, semanticModel, result, visited))
                    {
                        result.HasUncertainPaths = true;
                    }
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess in body.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol is IPropertySymbol indexerSymbol)
                {
                    if (!AnalyzePropertyLikeSymbol(indexerSymbol, semanticModel, result, visited))
                    {
                        result.HasUncertainPaths = true;
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes a property-like symbol by first trying its getter symbol and
        /// then falling back to the property or indexer declaration itself.
        /// </summary>
        /// <param name="propertySymbol">The property or indexer symbol to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
        /// <param name="visited">
        /// The set of already visited callable symbols used to prevent recursive cycles.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed for the symbol;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzePropertyLikeSymbol(
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            bool analyzedGetter = false;

            if (propertySymbol.GetMethod != null && visited.Add(propertySymbol.GetMethod))
            {
                analyzedGetter = AnalyzeSymbol(propertySymbol.GetMethod, semanticModel, result, visited);
            }

            if (!analyzedGetter && visited.Add(propertySymbol))
            {
                return AnalyzeSymbol(propertySymbol, semanticModel, result, visited);
            }

            return analyzedGetter;
        }

        /// <summary>
        /// Analyzes the syntax declarations of a callable symbol and recursively
        /// processes any executable bodies found there.
        /// </summary>
        /// <param name="symbol">The callable symbol to analyze.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <param name="result">The accumulated exception-flow result.</param>
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
            ExceptionFlowAnalysisResult result,
            HashSet<ISymbol> visited)
        {
            bool analyzedAnyBody = false;

            foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node = syntaxRef.GetSyntax();
                SemanticModel? nodeSemanticModel = TryGetSemanticModelFor(node, semanticModel);

                if (nodeSemanticModel == null)
                {
                    continue;
                }

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(method, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, result, visited);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(constructor, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, result, visited);
                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is PropertyDeclarationSyntax property)
                {
                    if (SyntaxUtils.TryGetMemberBody(property, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, result, visited);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        SemanticModel? getterSemanticModel = TryGetSemanticModelFor(getter, semanticModel);

                        if (getterSemanticModel != null)
                        {
                            if (getter.Body != null)
                            {
                                AnalyzeBody(getter.Body, getterSemanticModel, result, visited);
                                analyzedAnyBody = true;
                            }
                            else if (getter.ExpressionBody != null)
                            {
                                AnalyzeBody(getter.ExpressionBody.Expression, getterSemanticModel, result, visited);
                                analyzedAnyBody = true;
                            }
                        }
                    }

                    continue;
                }

                if (node is IndexerDeclarationSyntax indexer)
                {
                    if (SyntaxUtils.TryGetMemberBody(indexer, out SyntaxNode? body) && body != null)
                    {
                        AnalyzeBody(body, nodeSemanticModel, result, visited);
                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter = indexer.AccessorList?.Accessors
                        .FirstOrDefault(static accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        SemanticModel? getterSemanticModel = TryGetSemanticModelFor(getter, semanticModel);

                        if (getterSemanticModel != null)
                        {
                            if (getter.Body != null)
                            {
                                AnalyzeBody(getter.Body, getterSemanticModel, result, visited);
                                analyzedAnyBody = true;
                            }
                            else if (getter.ExpressionBody != null)
                            {
                                AnalyzeBody(getter.ExpressionBody.Expression, getterSemanticModel, result, visited);
                                analyzedAnyBody = true;
                            }
                        }
                    }

                    continue;
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeBody(accessor.Body, nodeSemanticModel, result, visited);
                        analyzedAnyBody = true;
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeBody(accessor.ExpressionBody.Expression, nodeSemanticModel, result, visited);
                        analyzedAnyBody = true;
                    }
                }
            }

            return analyzedAnyBody;
        }
    }
}
