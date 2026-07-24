using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains transitive traversal of constructors, properties, indexers,
    /// and callable symbols.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Resolves constructor calls within the specified node and
        /// recursively analyzes the bodies of the called constructors.
        /// Object creations that are part of a direct throw are ignored here
        /// because they are already handled by direct throw analysis.
        /// </summary>
        /// <param name="node">
        /// The node to inspect for object creation expressions.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the currently analyzed callable.
        /// </param>
        private static void AnalyzeObjectCreations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            foreach (ObjectCreationExpressionSyntax creation
                     in GetDescendantsAndSelfExcludingNestedTry
                         <ObjectCreationExpressionSyntax>(node))
            {
                if (IsPartOfDirectThrow(creation))
                {
                    continue;
                }

                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(creation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol)
                {
                    continue;
                }

                SeparatedSyntaxList<ArgumentSyntax> arguments =
                    creation.ArgumentList?.Arguments ?? default;

                ExceptionFlowCallContext constructorContext =
                    CreateCallContext(
                        constructorSymbol,
                        arguments,
                        semanticModel,
                        callContext);

                if (!traversalState.TryMarkAnalyzed(
                        constructorSymbol,
                        constructorContext))
                {
                    continue;
                }

                if (!AnalyzeSymbol(
                        constructorSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        constructorContext))
                {
                    MarkUncertain(
                        result,
                        constructorSymbol);
                }
            }
        }

        /// <summary>
        /// Resolves property and indexer accesses within the specified node
        /// and recursively analyzes the bodies of the accessed members.
        /// </summary>
        /// <param name="node">
        /// The node to inspect for property and indexer access.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the currently analyzed callable.
        /// </param>
        private static void AnalyzePropertyAndIndexerAccesses(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            foreach (MemberAccessExpressionSyntax memberAccess
                     in GetDescendantsAndSelfExcludingNestedTry
                         <MemberAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(memberAccess);

                if (symbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
                {
                    continue;
                }

                ISymbol propertyCallable;

                if (propertySymbol.GetMethod
                    is IMethodSymbol propertyGetter)
                {
                    propertyCallable = propertyGetter;
                }
                else
                {
                    propertyCallable = propertySymbol;
                }

                ExceptionFlowCallContext propertyContext =
                    new(propertyCallable);

                if (!AnalyzePropertyLikeSymbol(
                        propertySymbol,
                        semanticContext,
                        result,
                        traversalState,
                        propertyContext))
                {
                    MarkUncertain(
                        result,
                        propertySymbol);
                }
            }

            foreach (ElementAccessExpressionSyntax elementAccess
                     in GetDescendantsAndSelfExcludingNestedTry
                         <ElementAccessExpressionSyntax>(node))
            {
                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(elementAccess);

                if (symbolInfo.Symbol
                    is not IPropertySymbol indexerSymbol)
                {
                    continue;
                }

                IMethodSymbol? indexerGetter =
                    indexerSymbol.GetMethod;

                ExceptionFlowCallContext indexerContext =
                    indexerGetter != null
                        ? CreateCallContext(
                            indexerGetter,
                            elementAccess.ArgumentList.Arguments,
                            semanticModel,
                            callContext)
                        : new ExceptionFlowCallContext(
                            indexerSymbol);

                if (!AnalyzePropertyLikeSymbol(
                        indexerSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        indexerContext))
                {
                    MarkUncertain(
                        result,
                        indexerSymbol);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified object creation is part of a
        /// direct throw statement or throw expression and is therefore
        /// already covered by direct throw analysis.
        /// </summary>
        /// <param name="creation">
        /// The object creation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the object creation is directly thrown;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPartOfDirectThrow(
            ObjectCreationExpressionSyntax creation)
        {
            return creation.Parent
                       is ThrowStatementSyntax ||
                   creation.Parent
                       is ThrowExpressionSyntax;
        }

        /// <summary>
        /// Marks the given callable target as uncertain because its exception
        /// flow could not be analyzed.
        /// </summary>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="symbol">
        /// The symbol whose flow could not be decided.
        /// </param>
        private static void MarkUncertain(
            ExceptionFlowAnalysisResult result,
            ISymbol symbol)
        {
            string display =
                symbol.ToDisplayString(
                    SymbolDisplayFormat.CSharpErrorMessageFormat);

            if (string.IsNullOrWhiteSpace(display))
            {
                display = symbol.Name;
            }

            if (!string.IsNullOrWhiteSpace(display))
            {
                result.UncertainTargets.Add(display);
            }
        }

        /// <summary>
        /// Analyzes a property-like symbol by first trying its getter symbol
        /// and then falling back to the property or indexer declaration
        /// itself.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property or indexer symbol to analyze.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the property getter.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed
        /// for the symbol; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="callContext"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static bool AnalyzePropertyLikeSymbol(
            IPropertySymbol propertySymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            bool analyzedGetter = false;

            if (propertySymbol.GetMethod
                is IMethodSymbol getterSymbol)
            {
                if (!traversalState.TryMarkAnalyzed(
                        getterSymbol,
                        callContext))
                {
                    return true;
                }

                analyzedGetter =
                    AnalyzeSymbol(
                        getterSymbol,
                        semanticContext,
                        result,
                        traversalState,
                        callContext);
            }

            if (analyzedGetter)
            {
                return true;
            }

            ExceptionFlowCallContext propertyContext =
                new(propertySymbol);

            if (!traversalState.TryMarkAnalyzed(
                    propertySymbol,
                    propertyContext))
            {
                return true;
            }

            return AnalyzeSymbol(
                propertySymbol,
                semanticContext,
                result,
                traversalState,
                propertyContext);
        }

        /// <summary>
        /// Analyzes the syntax declarations of a callable symbol and
        /// recursively processes any executable bodies found there.
        /// </summary>
        /// <param name="symbol">
        /// The callable symbol to analyze.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was analyzed
        /// for the symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSymbol(
            ISymbol symbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext)
        {
            bool analyzedAnyBody = false;

            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }

            foreach (SyntaxReference syntaxReference
                     in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode node =
                    syntaxReference.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(
                        node.SyntaxTree,
                        out SemanticModel nodeSemanticModel) ||
                    nodeSemanticModel == null)
                {
                    continue;
                }

                if (node is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            method,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node
                    is ConstructorDeclarationSyntax constructor)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            constructor,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    continue;
                }

                if (node is PropertyDeclarationSyntax property)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            property,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter =
                        property.AccessorList?.Accessors
                            .FirstOrDefault(
                                static accessor =>
                                    accessor.Keyword.IsKind(
                                        SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(
                                getter.SyntaxTree,
                                out SemanticModel getterSemanticModel) ||
                            getterSemanticModel == null)
                        {
                            continue;
                        }

                        if (getter.Body != null)
                        {
                            AnalyzeNode(
                                getter.Body,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeNode(
                                getter.ExpressionBody.Expression,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is IndexerDeclarationSyntax indexer)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            indexer,
                            out SyntaxNode? body) &&
                        body != null)
                    {
                        AnalyzeNode(
                            body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }

                    AccessorDeclarationSyntax? getter =
                        indexer.AccessorList?.Accessors
                            .FirstOrDefault(
                                static accessor =>
                                    accessor.Keyword.IsKind(
                                        SyntaxKind.GetKeyword));

                    if (getter != null)
                    {
                        if (!semanticContext.TryGetSemanticModel(
                                getter.SyntaxTree,
                                out SemanticModel getterSemanticModel) ||
                            getterSemanticModel == null)
                        {
                            continue;
                        }

                        if (getter.Body != null)
                        {
                            AnalyzeNode(
                                getter.Body,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                        else if (getter.ExpressionBody != null)
                        {
                            AnalyzeNode(
                                getter.ExpressionBody.Expression,
                                getterSemanticModel,
                                semanticContext,
                                result,
                                traversalState,
                                ExceptionFlowTraversalMode.Transitive,
                                callContext);

                            analyzedAnyBody = true;
                        }
                    }

                    continue;
                }

                if (node is AccessorDeclarationSyntax accessor)
                {
                    if (accessor.Body != null)
                    {
                        AnalyzeNode(
                            accessor.Body,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }
                    else if (accessor.ExpressionBody != null)
                    {
                        AnalyzeNode(
                            accessor.ExpressionBody.Expression,
                            nodeSemanticModel,
                            semanticContext,
                            result,
                            traversalState,
                            ExceptionFlowTraversalMode.Transitive,
                            callContext);

                        analyzedAnyBody = true;
                    }
                }
            }

            return analyzedAnyBody;
        }
    }
}
