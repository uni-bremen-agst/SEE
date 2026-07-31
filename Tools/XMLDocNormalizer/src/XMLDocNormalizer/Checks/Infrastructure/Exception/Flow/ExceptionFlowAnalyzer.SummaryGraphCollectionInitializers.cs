using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for nested initializer receivers
    /// and classic collection initializers.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects getter edges required by nested initializers and implicit
        /// <c>Add</c> calls produced by classic collection initializers.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for initializer binding information.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// getter and <c>Add</c> implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter and <c>Add</c> targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryCollectionInitializers(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            AnalyzeSummaryNestedInitializerReceivers(
                node,
                semanticModel,
                semanticContext,
                graph,
                fragment,
                callContext);

            foreach (InitializerExpressionSyntax initializer
                     in GetSummaryDescendantsAndSelf
                         <InitializerExpressionSyntax>(node))
            {
                if (!initializer.IsKind(
                        SyntaxKind.CollectionInitializerExpression))
                {
                    continue;
                }

                ExpressionSyntax? receiverExpression =
                    GetSummaryCollectionInitializerReceiver(
                        initializer);

                foreach (ExpressionSyntax element
                         in initializer.Expressions)
                {
                    SymbolInfo symbolInfo =
                        semanticModel
                            .GetCollectionInitializerSymbolInfo(
                                element);

                    if (symbolInfo.Symbol
                        is IMethodSymbol selectedAddMethod)
                    {
                        AddSummaryCollectionInitializerEdges(
                            selectedAddMethod,
                            element,
                            receiverExpression,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                        continue;
                    }

                    AddSummaryUnresolvedCollectionInitializerTarget(
                        receiverExpression,
                        semanticModel,
                        fragment);
                }
            }
        }

        /// <summary>
        /// Collects property and indexer getters used as receivers of nested
        /// object or collection initializers.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// getter implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving getter targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving getter edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryNestedInitializerReceivers(
            SyntaxNode node,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (AssignmentExpressionSyntax assignment
                     in GetSummaryDescendantsAndSelf
                         <AssignmentExpressionSyntax>(node))
            {
                if (!IsSummaryNestedInitializerAssignment(
                        assignment))
                {
                    continue;
                }

                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        assignment.Left);

                if (symbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
                {
                    continue;
                }

                IPropertyReferenceOperation? propertyOperation =
                    semanticModel.GetOperation(
                        assignment.Left)
                    as IPropertyReferenceOperation;

                AddSummaryPropertyGetterEdge(
                    propertySymbol,
                    assignment.Left,
                    GetSummaryIndexerArguments(
                        assignment.Left),
                    propertyOperation,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);
            }
        }

        /// <summary>
        /// Adds one direct compiler-selected collection-initializer
        /// <c>Add</c> call or one edge for every known compatible runtime
        /// implementation.
        /// </summary>
        /// <param name="selectedAddMethod">
        /// The method selected by collection-initializer overload resolution.
        /// </param>
        /// <param name="element">
        /// The collection element responsible for the call.
        /// </param>
        /// <param name="receiverExpression">
        /// The created collection or nested initializer receiver, or
        /// <see langword="null"/> when no source receiver is available.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for argument and receiver facts.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context used to resolve known runtime
        /// <c>Add</c> implementations.
        /// </param>
        /// <param name="graph">
        /// The graph receiving target callables.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryCollectionInitializerEdges(
            IMethodSymbol selectedAddMethod,
            ExpressionSyntax element,
            ExpressionSyntax? receiverExpression,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callerContext)
        {
            IMethodSymbol targetMethod =
                selectedAddMethod.ReducedFrom ??
                selectedAddMethod;

            ExpressionSyntax[] argumentExpressions =
                GetSummaryCollectionElementArguments(
                    element);

            ExceptionFlowCallContext selectedContext =
                CreateSummaryCollectionInitializerCallContext(
                    targetMethod,
                    receiverExpression,
                    argumentExpressions,
                    semanticModel,
                    callerContext);

            if (selectedAddMethod.ReducedFrom != null ||
                !RequiresSummaryRuntimeDispatch(
                    selectedAddMethod))
            {
                AddSummaryCollectionInitializerTargetEdge(
                    targetMethod,
                    selectedContext,
                    element,
                    graph,
                    fragment);

                return;
            }

            INamedTypeSymbol? receiverType =
                GetSummaryCollectionInitializerReceiverType(
                    receiverExpression,
                    selectedAddMethod,
                    semanticModel);

            INamedTypeSymbol? exactReceiverType =
                GetSummaryCollectionInitializerExactReceiverType(
                    receiverExpression,
                    semanticModel);

            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryRuntimeTargets(
                    selectedAddMethod,
                    receiverType,
                    exactReceiverType,
                    semanticContext);

            if (runtimeTargets.Count == 0)
            {
                AddSummaryCollectionInitializerTargetEdge(
                    targetMethod,
                    selectedContext,
                    element,
                    graph,
                    fragment);

                return;
            }

            foreach (IMethodSymbol runtimeTarget
                     in runtimeTargets)
            {
                ExceptionFlowCallContext targetContext =
                    CreateDispatchTargetContext(
                        selectedAddMethod,
                        runtimeTarget,
                        selectedContext);

                AddSummaryCollectionInitializerTargetEdge(
                    runtimeTarget,
                    targetContext,
                    element,
                    graph,
                    fragment);
            }
        }

        /// <summary>
        /// Gets the static receiver type of one collection-initializer
        /// <c>Add</c> call.
        /// </summary>
        /// <param name="receiverExpression">
        /// The collection receiver expression, or <see langword="null"/>.
        /// </param>
        /// <param name="selectedAddMethod">
        /// The method selected by overload resolution.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver-type resolution.
        /// </param>
        /// <returns>
        /// The named static receiver type, falling back to the selected
        /// method's containing type when no source receiver is available.
        /// </returns>
        private static INamedTypeSymbol?
            GetSummaryCollectionInitializerReceiverType(
                ExpressionSyntax? receiverExpression,
                IMethodSymbol selectedAddMethod,
                SemanticModel semanticModel)
        {
            if (receiverExpression != null)
            {
                TypeInfo receiverTypeInfo =
                    semanticModel.GetTypeInfo(
                        receiverExpression);

                if (receiverTypeInfo.Type
                        is INamedTypeSymbol receiverType)
                {
                    return receiverType;
                }

                if (receiverTypeInfo.ConvertedType
                        is INamedTypeSymbol convertedReceiverType)
                {
                    return convertedReceiverType;
                }
            }

            return selectedAddMethod.ContainingType;
        }

        /// <summary>
        /// Gets an exact runtime receiver type from a directly created
        /// collection initializer receiver.
        /// </summary>
        /// <param name="receiverExpression">
        /// The collection receiver expression, or <see langword="null"/>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain the receiver operation.
        /// </param>
        /// <returns>
        /// The exactly created named type, or <see langword="null"/> when no
        /// exact type is proven.
        /// </returns>
        private static INamedTypeSymbol?
            GetSummaryCollectionInitializerExactReceiverType(
                ExpressionSyntax? receiverExpression,
                SemanticModel semanticModel)
        {
            if (receiverExpression == null)
            {
                return null;
            }

            IOperation? receiverOperation =
                semanticModel.GetOperation(
                    receiverExpression);

            return GetSummaryExactReceiverType(
                receiverOperation);
        }

        /// <summary>
        /// Adds one resolved collection-initializer <c>Add</c> target edge.
        /// </summary>
        /// <param name="targetMethod">
        /// The concrete source method represented by the edge target.
        /// </param>
        /// <param name="targetContext">
        /// The call context associated with the target method.
        /// </param>
        /// <param name="element">
        /// The collection element responsible for the call.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target summary.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving the call edge.
        /// </param>
        private static void AddSummaryCollectionInitializerTargetEdge(
            IMethodSymbol targetMethod,
            ExceptionFlowCallContext targetContext,
            ExpressionSyntax element,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
        {
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
                        ExceptionFlowPathStepKind.CollectionAddCall,
                        targetMethod,
                        element)));
        }

        /// <summary>
        /// Creates the context for one collection-initializer <c>Add</c>
        /// invocation.
        /// </summary>
        /// <param name="targetMethod">
        /// The unreduced source-level <c>Add</c> target.
        /// </param>
        /// <param name="receiverExpression">
        /// The source receiver of an extension <c>Add</c>, or
        /// <see langword="null"/> when unavailable.
        /// </param>
        /// <param name="argumentExpressions">
        /// The element expressions supplied to <c>Add</c>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// A context containing safe receiver, argument, optional-parameter,
        /// and expanded-<c>params</c> facts.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateSummaryCollectionInitializerCallContext(
                IMethodSymbol targetMethod,
                ExpressionSyntax? receiverExpression,
                IReadOnlyList<ExpressionSyntax> argumentExpressions,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts =
                    new();

            HashSet<int> suppliedParameterIndexes =
                new();

            int parameterOffset =
                targetMethod.IsExtensionMethod &&
                targetMethod.Parameters.Length > 0
                    ? 1
                    : 0;

            if (parameterOffset == 1)
            {
                suppliedParameterIndexes.Add(0);

                if (receiverExpression != null)
                {
                    ExceptionFlowValueFacts receiverFacts =
                        GetExpressionValueFacts(
                            receiverExpression,
                            semanticModel,
                            callerContext);

                    if (receiverFacts !=
                        ExceptionFlowValueFacts.None)
                    {
                        knownParameterFacts[0] =
                            receiverFacts;
                    }
                }
            }

            for (int argumentIndex = 0;
                 argumentIndex < argumentExpressions.Count;
                 argumentIndex++)
            {
                int parameterIndex =
                    parameterOffset +
                    argumentIndex;

                if (parameterIndex >=
                    targetMethod.Parameters.Length)
                {
                    parameterIndex =
                        targetMethod.Parameters.Length - 1;
                }

                if (parameterIndex < parameterOffset ||
                    parameterIndex >=
                        targetMethod.Parameters.Length)
                {
                    continue;
                }

                IParameterSymbol parameter =
                    targetMethod.Parameters[parameterIndex];

                suppliedParameterIndexes.Add(
                    parameterIndex);

                ExpressionSyntax argumentExpression =
                    argumentExpressions[argumentIndex];

                if (parameter.IsParams &&
                    !IsSummaryDirectParamsArrayArgument(
                        targetMethod,
                        parameterOffset,
                        argumentExpressions,
                        argumentIndex,
                        argumentExpression,
                        semanticModel))
                {
                    knownParameterFacts[parameterIndex] =
                        ExceptionFlowValueFacts.NonNull;

                    continue;
                }

                ExceptionFlowValueFacts argumentFacts =
                    GetSummaryCollectionArgumentFacts(
                        argumentExpression,
                        semanticModel,
                        callerContext);

                if (argumentFacts !=
                    ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[parameterIndex] =
                        argumentFacts;
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

        /// <summary>
        /// Determines whether one expression is supplied directly as the
        /// array value of a <c>params</c> parameter rather than as an expanded
        /// element.
        /// </summary>
        /// <param name="targetMethod">
        /// The selected <c>Add</c> method.
        /// </param>
        /// <param name="parameterOffset">
        /// The number of implicit receiver parameters.
        /// </param>
        /// <param name="argumentExpressions">
        /// All element expressions supplied by the initializer entry.
        /// </param>
        /// <param name="argumentIndex">
        /// The current element-expression index.
        /// </param>
        /// <param name="argumentExpression">
        /// The current element expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for converted-type resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when overload resolution supplies the
        /// expression as the parameter array itself; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDirectParamsArrayArgument(
            IMethodSymbol targetMethod,
            int parameterOffset,
            IReadOnlyList<ExpressionSyntax> argumentExpressions,
            int argumentIndex,
            ExpressionSyntax argumentExpression,
            SemanticModel semanticModel)
        {
            int explicitParameterCount =
                targetMethod.Parameters.Length -
                parameterOffset;

            if (explicitParameterCount <= 0 ||
                argumentExpressions.Count !=
                    explicitParameterCount ||
                argumentIndex !=
                    argumentExpressions.Count - 1)
            {
                return false;
            }

            IParameterSymbol paramsParameter =
                targetMethod.Parameters[
                    targetMethod.Parameters.Length - 1];

            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(
                    argumentExpression);

            return SymbolEqualityComparer.Default.Equals(
                typeInfo.ConvertedType,
                paramsParameter.Type);
        }

        /// <summary>
        /// Gets value facts that remain valid after collection-initializer
        /// argument conversion.
        /// </summary>
        /// <param name="argumentExpression">
        /// The supplied element expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for conversion and value analysis.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// Safe value facts, or <see cref="ExceptionFlowValueFacts.None"/>
        /// when a user-defined conversion may change the value.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetSummaryCollectionArgumentFacts(
                ExpressionSyntax argumentExpression,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            Conversion conversion =
                semanticModel.GetConversion(
                    argumentExpression);

            if (conversion.IsUserDefined)
            {
                return ExceptionFlowValueFacts.None;
            }

            return GetExpressionValueFacts(
                argumentExpression,
                semanticModel,
                callerContext);
        }

        /// <summary>
        /// Gets the explicit argument expressions represented by one simple
        /// or complex collection-initializer element.
        /// </summary>
        /// <param name="element">
        /// The collection-initializer element.
        /// </param>
        /// <returns>
        /// One expression for a simple element or every expression contained
        /// in a complex element initializer.
        /// </returns>
        private static ExpressionSyntax[]
            GetSummaryCollectionElementArguments(
                ExpressionSyntax element)
        {
            if (element
                    is InitializerExpressionSyntax complexElement &&
                complexElement.IsKind(
                    SyntaxKind.ComplexElementInitializerExpression))
            {
                return complexElement.Expressions
                    .ToArray();
            }

            return
            [
                element
            ];
        }

        /// <summary>
        /// Gets the source expression that receives collection-initializer
        /// <c>Add</c> calls.
        /// </summary>
        /// <param name="initializer">
        /// The collection initializer.
        /// </param>
        /// <returns>
        /// The object creation or nested member expression, or
        /// <see langword="null"/> when no receiver expression is available.
        /// </returns>
        private static ExpressionSyntax?
            GetSummaryCollectionInitializerReceiver(
                InitializerExpressionSyntax initializer)
        {
            switch (initializer.Parent)
            {
                case ObjectCreationExpressionSyntax creation:
                    return creation;

                case ImplicitObjectCreationExpressionSyntax
                    implicitCreation:
                    return implicitCreation;

                case AssignmentExpressionSyntax assignment
                    when ReferenceEquals(
                        assignment.Right,
                        initializer):
                    return assignment.Left;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Determines whether an assignment represents a nested object or
        /// collection initializer and therefore reads its left-side member
        /// instead of invoking a setter.
        /// </summary>
        /// <param name="assignment">
        /// The assignment syntax to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for nested initializer member syntax;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryNestedInitializerAssignment(
            AssignmentExpressionSyntax assignment)
        {
            return assignment.IsKind(
                       SyntaxKind.SimpleAssignmentExpression) &&
                   assignment.Right
                       is InitializerExpressionSyntax;
        }

        /// <summary>
        /// Adds uncertainty for a collection-initializer element whose
        /// selected <c>Add</c> target is unavailable.
        /// </summary>
        /// <param name="receiverExpression">
        /// The collection receiver, or <see langword="null"/> when
        /// unavailable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver-type resolution.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving uncertainty.
        /// </param>
        private static void AddSummaryUnresolvedCollectionInitializerTarget(
            ExpressionSyntax? receiverExpression,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment fragment)
        {
            string typeName =
                "<unknown collection type>";

            if (receiverExpression != null)
            {
                TypeInfo typeInfo =
                    semanticModel.GetTypeInfo(
                        receiverExpression);

                ITypeSymbol? receiverType =
                    typeInfo.Type ??
                    typeInfo.ConvertedType;

                string? resolvedTypeName =
                    receiverType?.ToDisplayString(
                        SymbolDisplayFormat.CSharpErrorMessageFormat);

                if (!string.IsNullOrWhiteSpace(
                        resolvedTypeName))
                {
                    typeName =
                        resolvedTypeName;
                }
            }

            fragment.AddUncertainTarget(
                $"Collection initializer Add target for '{typeName}' could not be resolved.");
        }
    }
}
