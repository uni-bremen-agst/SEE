using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Utils;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSummaryTargetRegistrar;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Analyzes executable callable declarations into summary fragments.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
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
        internal static bool AnalyzeSummarySymbolDeclarations(
            ISymbol symbol,
            ExceptionFlowSemanticEnvironment semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (symbol is IMethodSymbol implicitConstructor &&
                implicitConstructor.MethodKind ==
                    MethodKind.Constructor &&
                implicitConstructor.IsImplicitlyDeclared)
            {
                return ExceptionFlowAnalyzer.AnalyzeSummaryImplicitConstructor(
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
                    is MethodDeclarationSyntax)
                {
                    if (TryGetSummaryInvocationBody(
                            declarationNode,
                            out SyntaxNode? methodBody)
                        && methodBody != null)
                    {
                        ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
                            ExceptionFlowAnalyzer.AnalyzeSummaryInstanceConstructor(
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
                        ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
                    ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
                    ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
                        ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
                        ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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

            if (analyzedAnyBody ||
                symbol is not IMethodSymbol getterSymbol ||
                getterSymbol.MethodKind !=
                    MethodKind.PropertyGet ||
                getterSymbol.AssociatedSymbol
                    is not IPropertySymbol propertySymbol)
            {
                return analyzedAnyBody;
            }

            Dictionary<int, ExceptionFlowValueFacts>
                propertyParameterFacts =
                    new();

            int mappedParameterCount =
                Math.Min(
                    getterSymbol.Parameters.Length,
                    propertySymbol.Parameters.Length);

            for (int parameterIndex = 0;
                 parameterIndex < mappedParameterCount;
                 parameterIndex++)
            {
                ExceptionFlowValueFacts facts =
                    callContext.GetParameterFacts(
                        parameterIndex);

                if (facts ==
                    ExceptionFlowValueFacts.None)
                {
                    continue;
                }

                propertyParameterFacts.Add(
                    parameterIndex,
                    facts);
            }

            ExceptionFlowCallContext propertyBodyContext =
                new(
                    propertySymbol,
                    propertyParameterFacts);

            foreach (SyntaxReference syntaxReference
                     in propertySymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode =
                    syntaxReference.GetSyntax();

                ExpressionSyntax? expressionBody =
                    declarationNode switch
                    {
                        PropertyDeclarationSyntax property
                            when property.ExpressionBody != null =>
                                property.ExpressionBody.Expression,

                        IndexerDeclarationSyntax indexer
                            when indexer.ExpressionBody != null =>
                                indexer.ExpressionBody.Expression,

                        _ => null
                    };

                if (expressionBody == null)
                {
                    continue;
                }

                if (!semanticContext.TryGetSemanticModel(
                        declarationNode.SyntaxTree,
                        out SemanticModel semanticModel) ||
                    semanticModel == null)
                {
                    continue;
                }

                ExceptionFlowAnalyzer.AnalyzeSummaryNode(
                    expressionBody,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    propertyBodyContext);

                analyzedAnyBody =
                    true;
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
            ExceptionFlowSemanticEnvironment semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            if (TryGetSummaryInvocationBody(
                    localFunction,
                    out SyntaxNode? body)
                && body != null)
            {
                ExceptionFlowAnalyzer.AnalyzeSummaryNode(
                    body,
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
            ExceptionFlowSemanticEnvironment semanticContext,
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

            ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
            ExceptionFlowSemanticEnvironment semanticContext,
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
                ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
                ExceptionFlowAnalyzer.AnalyzeSummaryNode(
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
