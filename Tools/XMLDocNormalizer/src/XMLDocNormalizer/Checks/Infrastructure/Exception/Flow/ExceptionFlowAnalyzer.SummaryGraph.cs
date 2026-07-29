using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains nonrecursive construction of context-sensitive callable
    /// summary graphs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Attempts to construct the complete context-sensitive summary graph
        /// reachable from one source-level member.
        /// </summary>
        /// <param name="member">
        /// The root member whose reachable callables should be summarized.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The constructed summary graph.
        /// </param>
        /// <param name="rootKey">
        /// The context-sensitive key of the analyzed root member.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the root symbol and its semantic model
        /// could be resolved; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryBuildTransitiveSummaryGraph(
            MemberDeclarationSyntax member,
            ProjectClosureSemanticContext semanticContext,
            out ExceptionFlowSummaryGraph graph,
            out ExceptionFlowCallableKey rootKey)
        {
            graph = new ExceptionFlowSummaryGraph();
            rootKey = default;

            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return false;
            }

            if (semanticModel.GetDeclaredSymbol(member)
                is not ISymbol rootSymbol)
            {
                return false;
            }

            ExceptionFlowCallContext rootContext =
                new(rootSymbol);

            rootKey =
                new ExceptionFlowCallableKey(
                    rootSymbol,
                    rootContext.Key);

            graph.GetOrAdd(
                rootKey,
                rootContext);

            BuildPendingSummaryNodes(
                graph,
                semanticContext);

            return true;
        }

        /// <summary>
        /// Processes every pending graph node through a nonrecursive work
        /// queue.
        /// </summary>
        /// <param name="graph">
        /// The graph containing pending callable keys.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        private static void BuildPendingSummaryNodes(
            ExceptionFlowSummaryGraph graph,
            ProjectClosureSemanticContext semanticContext)
        {
            while (graph.TryDequeuePending(
                       out ExceptionFlowCallableKey key))
            {
                if (!graph.TryGetSummary(
                        key,
                        out ExceptionFlowSummary? summary) ||
                    summary == null ||
                    !graph.TryGetCallContext(
                        key,
                        out ExceptionFlowCallContext? callContext) ||
                    callContext == null)
                {
                    continue;
                }

                ExceptionFlowSummaryFragment fragment =
                    new();

                bool analyzedAnyBody =
                    AnalyzeSummarySymbolDeclarations(
                        key.Symbol,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);

                if (analyzedAnyBody)
                {
                    summary.MarkExecutableBodyAnalyzed();
                }

                summary.Merge(
                    fragment);
            }
        }

        /// <summary>
        /// Analyzes all executable declarations belonging to one callable
        /// symbol without recursively entering referenced callables.
        /// </summary>
        /// <param name="symbol">
        /// The callable symbol represented by the current graph node.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The summary graph receiving newly discovered target nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving sources and call edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one executable body was
        /// analyzed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummarySymbolDeclarations(
            ISymbol symbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (symbol is IMethodSymbol implicitConstructor &&
                implicitConstructor.MethodKind ==
                    MethodKind.Constructor &&
                implicitConstructor.IsImplicitlyDeclared)
            {
                return AnalyzeSummaryImplicitConstructor(
                    implicitConstructor,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }

            bool analyzedAnyBody =
                false;

            foreach (SyntaxReference syntaxReference
                     in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode =
                    syntaxReference.GetSyntax();

                if (!semanticContext.TryGetSemanticModel(
                        declarationNode.SyntaxTree,
                        out SemanticModel semanticModel) ||
                    semanticModel == null)
                {
                    continue;
                }

                if (declarationNode
                    is MethodDeclarationSyntax method)
                {
                    if (SyntaxUtils.TryGetMemberBody(
                            method,
                            out SyntaxNode? methodBody) &&
                        methodBody != null)
                    {
                        AnalyzeSummaryNode(
                            methodBody,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                        analyzedAnyBody =
                            true;
                    }

                    continue;
                }

                if (declarationNode
                    is LocalFunctionStatementSyntax localFunction)
                {
                    analyzedAnyBody |=
                        AnalyzeSummaryLocalFunction(
                            localFunction,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                    continue;
                }

                if (declarationNode
                    is AnonymousFunctionExpressionSyntax
                        anonymousFunction)
                {
                    analyzedAnyBody |=
                        AnalyzeSummaryAnonymousFunction(
                            anonymousFunction,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                    continue;
                }

                if (declarationNode
                    is ConstructorDeclarationSyntax constructor)
                {
                    if (symbol is IMethodSymbol constructorSymbol &&
                        constructorSymbol.MethodKind ==
                            MethodKind.Constructor)
                    {
                        analyzedAnyBody |=
                            AnalyzeSummaryInstanceConstructor(
                                constructor,
                                constructorSymbol,
                                semanticModel,
                                semanticContext,
                                graph,
                                fragment,
                                callContext);
                    }
                    else if (SyntaxUtils.TryGetMemberBody(
                                 constructor,
                                 out SyntaxNode? constructorBody) &&
                             constructorBody != null)
                    {
                        AnalyzeSummaryNode(
                            constructorBody,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                        analyzedAnyBody =
                            true;
                    }

                    continue;
                }

                if (declarationNode
                        is OperatorDeclarationSyntax
                            operatorDeclaration &&
                    SyntaxUtils.TryGetMemberBody(
                        operatorDeclaration,
                        out SyntaxNode? operatorBody) &&
                    operatorBody != null)
                {
                    AnalyzeSummaryNode(
                        operatorBody,
                        semanticModel,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);

                    analyzedAnyBody =
                        true;

                    continue;
                }

                if (declarationNode
                        is ConversionOperatorDeclarationSyntax
                            conversionDeclaration &&
                    SyntaxUtils.TryGetMemberBody(
                        conversionDeclaration,
                        out SyntaxNode? conversionBody) &&
                    conversionBody != null)
                {
                    AnalyzeSummaryNode(
                        conversionBody,
                        semanticModel,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);

                    analyzedAnyBody =
                        true;

                    continue;
                }

                if (declarationNode
                    is PropertyDeclarationSyntax property)
                {
                    if (property.ExpressionBody != null)
                    {
                        AnalyzeSummaryNode(
                            property.ExpressionBody.Expression,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                        analyzedAnyBody =
                            true;
                    }

                    if (property.AccessorList != null)
                    {
                        foreach (AccessorDeclarationSyntax accessor
                                 in property.AccessorList.Accessors)
                        {
                            analyzedAnyBody |=
                                AnalyzeSummaryAccessor(
                                    accessor,
                                    semanticContext,
                                    graph,
                                    fragment,
                                    callContext);
                        }
                    }

                    continue;
                }

                if (declarationNode
                    is IndexerDeclarationSyntax indexer)
                {
                    if (indexer.ExpressionBody != null)
                    {
                        AnalyzeSummaryNode(
                            indexer.ExpressionBody.Expression,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                        analyzedAnyBody =
                            true;
                    }

                    if (indexer.AccessorList != null)
                    {
                        foreach (AccessorDeclarationSyntax accessor
                                 in indexer.AccessorList.Accessors)
                        {
                            analyzedAnyBody |=
                                AnalyzeSummaryAccessor(
                                    accessor,
                                    semanticContext,
                                    graph,
                                    fragment,
                                    callContext);
                        }
                    }

                    continue;
                }

                if (declarationNode
                        is EventDeclarationSyntax eventDeclaration &&
                    eventDeclaration.AccessorList
                        is AccessorListSyntax eventAccessorList)
                {
                    foreach (AccessorDeclarationSyntax accessor
                             in eventAccessorList.Accessors)
                    {
                        analyzedAnyBody |=
                            AnalyzeSummaryAccessor(
                                accessor,
                                semanticContext,
                                graph,
                                fragment,
                                callContext);
                    }

                    continue;
                }

                if (declarationNode
                    is AccessorDeclarationSyntax accessorDeclaration)
                {
                    analyzedAnyBody |=
                        AnalyzeSummaryAccessor(
                            accessorDeclaration,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);
                }
            }

            return analyzedAnyBody;
        }

        /// <summary>
        /// Analyzes one local-function body.
        /// </summary>
        /// <param name="localFunction">
        /// The local-function declaration to analyze.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for the local function.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving nested callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local-function summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the local-function parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local function has an executable
        /// body; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummaryLocalFunction(
            LocalFunctionStatementSyntax localFunction,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (localFunction.Body != null)
            {
                AnalyzeSummaryNode(
                    localFunction.Body,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                return true;
            }

            if (localFunction.ExpressionBody != null)
            {
                AnalyzeSummaryNode(
                    localFunction.ExpressionBody.Expression,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Analyzes one lambda or anonymous-method body.
        /// </summary>
        /// <param name="anonymousFunction">
        /// The lambda or anonymous-method declaration.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for the anonymous function.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving nested callable targets.
        /// </param>
        /// <param name="fragment">
        /// The anonymous-function summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the anonymous-function parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an executable anonymous-function body
        /// was found; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummaryAnonymousFunction(
            AnonymousFunctionExpressionSyntax anonymousFunction,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            CSharpSyntaxNode? body =
                anonymousFunction switch
                {
                    ParenthesizedLambdaExpressionSyntax lambda =>
                        lambda.Body,

                    SimpleLambdaExpressionSyntax lambda =>
                        lambda.Body,

                    AnonymousMethodExpressionSyntax anonymousMethod =>
                        anonymousMethod.Block,

                    _ => null
                };

            if (body == null)
            {
                return false;
            }

            AnalyzeSummaryNode(
                body,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            return true;
        }

        /// <summary>
        /// Analyzes one property, indexer, event, or init accessor body.
        /// </summary>
        /// <param name="accessor">
        /// The accessor declaration to analyze.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The summary graph receiving newly discovered targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the accessor.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an executable accessor body was
        /// analyzed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummaryAccessor(
            AccessorDeclarationSyntax accessor,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (!semanticContext.TryGetSemanticModel(
                    accessor.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return false;
            }

            if (accessor.Body != null)
            {
                AnalyzeSummaryNode(
                    accessor.Body,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                return true;
            }

            if (accessor.ExpressionBody != null)
            {
                AnalyzeSummaryNode(
                    accessor.ExpressionBody.Expression,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                return true;
            }

            return false;
        }
    }
}
