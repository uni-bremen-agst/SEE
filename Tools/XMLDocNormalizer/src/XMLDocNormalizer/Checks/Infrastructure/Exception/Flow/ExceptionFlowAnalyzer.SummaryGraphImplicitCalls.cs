using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains shared summary-graph construction for compiler-selected
    /// implicit calls.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Stores the await-pattern method name used to obtain an awaiter.
        /// </summary>
        private const string GetAwaiterMethodName =
            "GetAwaiter";

        /// <summary>
        /// Stores the await-pattern completion property name.
        /// </summary>
        private const string IsCompletedPropertyName =
            "IsCompleted";

        /// <summary>
        /// Stores the await-pattern result method name.
        /// </summary>
        private const string GetResultMethodName =
            "GetResult";

        /// <summary>
        /// Adds one compiler-selected implicit method call to the summary
        /// graph.
        /// </summary>
        /// <param name="selectedMethod">
        /// The method selected by Roslyn, or <see langword="null"/> when no
        /// method was selected.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the implicit call.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the implicit call.
        /// </param>
        /// <param name="reducedExtensionReceiver">
        /// The source expression supplied as the receiver of a reduced
        /// extension method, or <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target callable.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryImplicitMethodEdge(
            IMethodSymbol? selectedMethod,
            ExceptionFlowPathStepKind stepKind,
            SyntaxNode sourceNode,
            ExpressionSyntax? reducedExtensionReceiver,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (selectedMethod == null)
            {
                return;
            }

            IMethodSymbol targetMethod =
                selectedMethod.ReducedFrom ??
                selectedMethod;

            ExceptionFlowCallContext targetContext =
                CreateSummaryImplicitCallContext(
                    selectedMethod,
                    targetMethod,
                    reducedExtensionReceiver,
                    semanticModel,
                    callerContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    targetMethod,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        targetMethod,
                        sourceNode)));
        }

        /// <summary>
        /// Adds one compiler-selected implicit property getter to the summary
        /// graph.
        /// </summary>
        /// <param name="selectedProperty">
        /// The property selected by Roslyn, or <see langword="null"/> when no
        /// property was selected.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the getter access.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the getter access.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the source node.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the getter target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a getter edge was added; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryAddSummaryImplicitGetterEdge(
            IPropertySymbol? selectedProperty,
            ExceptionFlowPathStepKind stepKind,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (selectedProperty?.GetMethod
                is not IMethodSymbol getterMethod)
            {
                return false;
            }

            AddSummaryImplicitMethodEdge(
                getterMethod,
                stepKind,
                sourceNode,
                reducedExtensionReceiver: null,
                semanticModel,
                graph,
                fragment,
                callerContext);

            return true;
        }

        /// <summary>
        /// Adds the call chain selected by Roslyn for one explicit await
        /// expression.
        /// </summary>
        /// <param name="awaitInfo">
        /// Roslyn's semantic information for the await operation.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the await operation.
        /// </param>
        /// <param name="awaitedExpression">
        /// The expression receiving a reduced extension
        /// <c>GetAwaiter</c> call.
        /// </param>
        /// <param name="description">
        /// A fixed description used when the await target is unresolved.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the source node.
        /// </param>
        /// <param name="graph">
        /// The graph receiving awaiter targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving awaiter edges or uncertainty.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryExplicitAwaitEdges(
            AwaitExpressionInfo awaitInfo,
            SyntaxNode sourceNode,
            ExpressionSyntax awaitedExpression,
            string description,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (awaitInfo.IsDynamic)
            {
                fragment.AddUncertainTarget(
                    description +
                    " uses dynamic await binding.");

                return;
            }

            if (awaitInfo.RuntimeAwaitMethod != null)
            {
                AddSummaryImplicitMethodEdge(
                    awaitInfo.RuntimeAwaitMethod,
                    ExceptionFlowPathStepKind.RuntimeAwaitCall,
                    sourceNode,
                    reducedExtensionReceiver: null,
                    semanticModel,
                    graph,
                    fragment,
                    callerContext);

                return;
            }

            bool completeAwaitPattern =
                awaitInfo.GetAwaiterMethod != null &&
                awaitInfo.IsCompletedProperty?.GetMethod != null &&
                awaitInfo.GetResultMethod != null;

            if (!completeAwaitPattern)
            {
                fragment.AddUncertainTarget(
                    description +
                    " awaiter pattern could not be resolved completely.");
            }

            AddSummaryImplicitMethodEdge(
                awaitInfo.GetAwaiterMethod,
                ExceptionFlowPathStepKind.AwaitGetAwaiterCall,
                sourceNode,
                awaitedExpression,
                semanticModel,
                graph,
                fragment,
                callerContext);

            TryAddSummaryImplicitGetterEdge(
                awaitInfo.IsCompletedProperty,
                ExceptionFlowPathStepKind.AwaitIsCompletedGetter,
                sourceNode,
                semanticModel,
                graph,
                fragment,
                callerContext);

            AddSummaryImplicitMethodEdge(
                awaitInfo.GetResultMethod,
                ExceptionFlowPathStepKind.AwaitGetResultCall,
                sourceNode,
                reducedExtensionReceiver: null,
                semanticModel,
                graph,
                fragment,
                callerContext);
        }

        /// <summary>
        /// Resolves and adds the awaiter chain for a compiler-generated await
        /// whose awaitable exists only as the result of another implicit
        /// operation.
        /// </summary>
        /// <param name="awaitableType">
        /// The static type of the implicitly awaited value.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the implicit await.
        /// </param>
        /// <param name="description">
        /// A fixed description used when awaiter binding cannot be resolved.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative awaiter binding.
        /// </param>
        /// <param name="graph">
        /// The graph receiving awaiter targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving awaiter edges or uncertainty.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryImplicitAwaitEdges(
            ITypeSymbol? awaitableType,
            SyntaxNode sourceNode,
            string description,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            if (awaitableType == null)
            {
                fragment.AddUncertainTarget(
                    description +
                    " awaitable type could not be resolved.");

                return;
            }

            if (awaitableType.TypeKind ==
                TypeKind.Dynamic)
            {
                fragment.AddUncertainTarget(
                    description +
                    " uses dynamic await binding.");

                return;
            }

            bool resolved =
                TryResolveSummaryAwaitableMembers(
                    awaitableType,
                    sourceNode,
                    semanticModel,
                    out IMethodSymbol? getAwaiterMethod,
                    out IPropertySymbol? isCompletedProperty,
                    out IMethodSymbol? getResultMethod);

            if (!resolved)
            {
                string typeName =
                    awaitableType.ToDisplayString(
                        SymbolDisplayFormat
                            .CSharpErrorMessageFormat);

                if (string.IsNullOrWhiteSpace(
                        typeName))
                {
                    typeName =
                        "<unknown awaitable type>";
                }

                fragment.AddUncertainTarget(
                    description +
                    $" awaiter pattern for '{typeName}' could not be resolved.");

                return;
            }

            AddSummaryImplicitMethodEdge(
                getAwaiterMethod,
                ExceptionFlowPathStepKind.AwaitGetAwaiterCall,
                sourceNode,
                reducedExtensionReceiver: null,
                semanticModel,
                graph,
                fragment,
                callerContext);

            TryAddSummaryImplicitGetterEdge(
                isCompletedProperty,
                ExceptionFlowPathStepKind.AwaitIsCompletedGetter,
                sourceNode,
                semanticModel,
                graph,
                fragment,
                callerContext);

            AddSummaryImplicitMethodEdge(
                getResultMethod,
                ExceptionFlowPathStepKind.AwaitGetResultCall,
                sourceNode,
                reducedExtensionReceiver: null,
                semanticModel,
                graph,
                fragment,
                callerContext);
        }

        /// <summary>
        /// Resolves the ordinary awaiter pattern for one static awaitable
        /// type using speculative binding at the source location.
        /// </summary>
        /// <param name="awaitableType">
        /// The static awaitable type.
        /// </param>
        /// <param name="sourceNode">
        /// The source node supplying the speculative binding position.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative binding.
        /// </param>
        /// <param name="getAwaiterMethod">
        /// The selected <c>GetAwaiter</c> method.
        /// </param>
        /// <param name="isCompletedProperty">
        /// The selected Boolean <c>IsCompleted</c> property.
        /// </param>
        /// <param name="getResultMethod">
        /// The selected parameterless <c>GetResult</c> method.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the complete ordinary awaiter pattern
        /// is resolved; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveSummaryAwaitableMembers(
            ITypeSymbol awaitableType,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            out IMethodSymbol? getAwaiterMethod,
            out IPropertySymbol? isCompletedProperty,
            out IMethodSymbol? getResultMethod)
        {
            getAwaiterMethod =
                ResolveSummarySpeculativeInvocation(
                    awaitableType,
                    GetAwaiterMethodName,
                    sourceNode,
                    semanticModel,
                    allowReducedExtension: true,
                    requireDeclaredParameterlessMethod: false);

            isCompletedProperty =
                null;

            getResultMethod =
                null;

            if (getAwaiterMethod == null)
            {
                return false;
            }

            ITypeSymbol awaiterType =
                getAwaiterMethod.ReturnType;

            isCompletedProperty =
                ResolveSummarySpeculativeProperty(
                    awaiterType,
                    IsCompletedPropertyName,
                    sourceNode,
                    semanticModel);

            getResultMethod =
                ResolveSummarySpeculativeInvocation(
                    awaiterType,
                    GetResultMethodName,
                    sourceNode,
                    semanticModel,
                    allowReducedExtension: false,
                    requireDeclaredParameterlessMethod: true);

            return isCompletedProperty != null &&
                   getResultMethod != null;
        }

        /// <summary>
        /// Resolves a zero-explicit-argument member invocation on a synthetic
        /// value of one type.
        /// </summary>
        /// <param name="receiverType">
        /// The synthetic receiver type.
        /// </param>
        /// <param name="methodName">
        /// The method name to bind.
        /// </param>
        /// <param name="sourceNode">
        /// The source node supplying the speculative binding position.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative binding.
        /// </param>
        /// <param name="allowReducedExtension">
        /// Whether a reduced extension method is an acceptable result.
        /// </param>
        /// <param name="requireDeclaredParameterlessMethod">
        /// Whether the selected method must declare no parameters rather than
        /// merely being invocable without explicit arguments.
        /// </param>
        /// <returns>
        /// The selected method, or <see langword="null"/> when binding does
        /// not produce a structurally valid target.
        /// </returns>
        private static IMethodSymbol? ResolveSummarySpeculativeInvocation(
            ITypeSymbol receiverType,
            string methodName,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            bool allowReducedExtension,
            bool requireDeclaredParameterlessMethod)
        {
            InvocationExpressionSyntax invocation =
                CreateSummaryImplicitMemberInvocation(
                    receiverType,
                    methodName);

            SymbolInfo symbolInfo =
                semanticModel.GetSpeculativeSymbolInfo(
                    sourceNode.SpanStart,
                    invocation,
                    SpeculativeBindingOption.BindAsExpression);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.Arity != 0 ||
                selectedMethod.IsStatic &&
                selectedMethod.ReducedFrom == null ||
                selectedMethod.ReducedFrom != null &&
                !allowReducedExtension ||
                requireDeclaredParameterlessMethod &&
                selectedMethod.Parameters.Length != 0)
            {
                return null;
            }

            return selectedMethod;
        }

        /// <summary>
        /// Resolves the readable instance Boolean property used by the awaiter
        /// pattern.
        /// </summary>
        /// <param name="receiverType">
        /// The synthetic awaiter type.
        /// </param>
        /// <param name="propertyName">
        /// The property name to bind.
        /// </param>
        /// <param name="sourceNode">
        /// The source node supplying the speculative binding position.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for speculative binding.
        /// </param>
        /// <returns>
        /// The selected property, or <see langword="null"/> when no valid
        /// awaiter property is resolved.
        /// </returns>
        private static IPropertySymbol? ResolveSummarySpeculativeProperty(
            ITypeSymbol receiverType,
            string propertyName,
            SyntaxNode sourceNode,
            SemanticModel semanticModel)
        {
            MemberAccessExpressionSyntax propertyAccess =
                CreateSummaryImplicitMemberAccess(
                    receiverType,
                    propertyName);

            SymbolInfo symbolInfo =
                semanticModel.GetSpeculativeSymbolInfo(
                    sourceNode.SpanStart,
                    propertyAccess,
                    SpeculativeBindingOption.BindAsExpression);

            if (symbolInfo.Symbol
                    is not IPropertySymbol selectedProperty ||
                selectedProperty.IsStatic ||
                selectedProperty.GetMethod == null ||
                selectedProperty.Type.SpecialType !=
                    SpecialType.System_Boolean)
            {
                return null;
            }

            return selectedProperty;
        }

        /// <summary>
        /// Creates a synthetic zero-argument member invocation for
        /// speculative binding.
        /// </summary>
        /// <param name="receiverType">
        /// The receiver type.
        /// </param>
        /// <param name="methodName">
        /// The invoked method name.
        /// </param>
        /// <returns>
        /// A syntax-only invocation equivalent to
        /// <c>default(ReceiverType).Method()</c>.
        /// </returns>
        private static InvocationExpressionSyntax
            CreateSummaryImplicitMemberInvocation(
                ITypeSymbol receiverType,
                string methodName)
        {
            return SyntaxFactory.InvocationExpression(
                CreateSummaryImplicitMemberAccess(
                    receiverType,
                    methodName),
                SyntaxFactory.ArgumentList());
        }

        /// <summary>
        /// Creates a synthetic member access for speculative binding.
        /// </summary>
        /// <param name="receiverType">
        /// The receiver type.
        /// </param>
        /// <param name="memberName">
        /// The accessed member name.
        /// </param>
        /// <returns>
        /// A syntax-only member access equivalent to
        /// <c>default(ReceiverType).Member</c>.
        /// </returns>
        private static MemberAccessExpressionSyntax
            CreateSummaryImplicitMemberAccess(
                ITypeSymbol receiverType,
                string memberName)
        {
            string typeName =
                receiverType.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat);

            TypeSyntax receiverTypeSyntax =
                SyntaxFactory.ParseTypeName(
                    typeName);

            DefaultExpressionSyntax receiver =
                SyntaxFactory.DefaultExpression(
                    receiverTypeSyntax);

            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParenthesizedExpression(
                    receiver),
                SyntaxFactory.IdentifierName(
                    memberName));
        }

        /// <summary>
        /// Creates the context for one compiler-selected implicit call.
        /// </summary>
        /// <param name="selectedMethod">
        /// The method symbol selected at the source location.
        /// </param>
        /// <param name="targetMethod">
        /// The source-level target method. For a reduced extension method,
        /// this is its unreduced method.
        /// </param>
        /// <param name="reducedExtensionReceiver">
        /// The receiver expression of an implicit extension-method call, or
        /// <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver value facts.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// A context containing receiver facts and facts implied by omitted
        /// optional or <c>params</c> arguments.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateSummaryImplicitCallContext(
                IMethodSymbol selectedMethod,
                IMethodSymbol targetMethod,
                ExpressionSyntax? reducedExtensionReceiver,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            bool hasImplicitExtensionReceiver =
                targetMethod.IsExtensionMethod &&
                targetMethod.Parameters.Length > 0;

            if (!hasImplicitExtensionReceiver)
            {
                return CreateCallContext(
                    targetMethod,
                    default,
                    semanticModel,
                    callerContext);
            }

            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts =
                    new();

            HashSet<int> suppliedParameterIndexes =
                new()
                {
            0
                };

            if (reducedExtensionReceiver != null)
            {
                ExceptionFlowValueFacts receiverFacts =
                    GetExpressionValueFacts(
                        reducedExtensionReceiver,
                        semanticModel,
                        callerContext);

                if (receiverFacts !=
                    ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[0] =
                        receiverFacts;
                }
            }

            AddDefaultParameterFacts(
                targetMethod,
                knownParameterFacts,
                suppliedParameterIndexes);

            return new ExceptionFlowCallContext(
                targetMethod,
                knownParameterFacts);
        }
    }
}
