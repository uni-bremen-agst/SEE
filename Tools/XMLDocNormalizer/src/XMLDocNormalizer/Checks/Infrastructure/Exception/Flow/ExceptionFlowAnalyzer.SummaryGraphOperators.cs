using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for user-defined operators and
    /// conversions.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Stores the metadata name of the user-defined Boolean true
        /// operator.
        /// </summary>
        private const string TrueOperatorMetadataName =
            "op_True";

        /// <summary>
        /// Stores the metadata name of the user-defined Boolean false
        /// operator.
        /// </summary>
        private const string FalseOperatorMetadataName =
            "op_False";

        /// <summary>
        /// Collects user-defined operator and conversion calls represented by
        /// one executable syntax fragment.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain operation trees and value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving operator and conversion target nodes.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryOperatorsAndConversions(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            HashSet<string> collectedCallKeys =
                new(StringComparer.Ordinal);

            foreach (IOperation operationRoot
                     in GetSummaryOperationRoots(
                         node,
                         semanticModel))
            {
                AnalyzeSummaryOperationTree(
                    operationRoot,
                    semanticModel,
                    graph,
                    fragment,
                    callContext,
                    collectedCallKeys);
            }
        }

        /// <summary>
        /// Gets the largest non-overlapping operation roots represented by an
        /// executable syntax node.
        /// </summary>
        /// <param name="node">
        /// The syntax node whose executable operations should be returned.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain operations.
        /// </param>
        /// <returns>
        /// Non-overlapping operation roots that belong to the current
        /// callable.
        /// </returns>
        private static IEnumerable<IOperation>
            GetSummaryOperationRoots(
                SyntaxNode node,
                SemanticModel semanticModel)
        {
            IOperation? rootOperation =
                semanticModel.GetOperation(
                    node);

            if (rootOperation != null)
            {
                yield return rootOperation;
                yield break;
            }

            Stack<SyntaxNode> pendingNodes =
                new();

            PushSummarySyntaxChildren(
                node,
                pendingNodes);

            while (pendingNodes.Count > 0)
            {
                SyntaxNode currentNode =
                    pendingNodes.Pop();

                if (IsSummarySyntaxExecutionBoundary(
                        currentNode))
                {
                    continue;
                }

                IOperation? currentOperation =
                    semanticModel.GetOperation(
                        currentNode);

                if (currentOperation != null)
                {
                    yield return currentOperation;
                    continue;
                }

                PushSummarySyntaxChildren(
                    currentNode,
                    pendingNodes);
            }
        }

        /// <summary>
        /// Pushes child syntax nodes in reverse order so they are processed in
        /// source order.
        /// </summary>
        /// <param name="node">
        /// The node whose children should be pushed.
        /// </param>
        /// <param name="pendingNodes">
        /// The destination syntax stack.
        /// </param>
        private static void PushSummarySyntaxChildren(
            SyntaxNode node,
            Stack<SyntaxNode> pendingNodes)
        {
            SyntaxNode[] children =
                node.ChildNodes()
                    .ToArray();

            for (int index = children.Length - 1;
                 index >= 0;
                 index--)
            {
                pendingNodes.Push(
                    children[index]);
            }
        }

        /// <summary>
        /// Determines whether a syntax node starts executable code owned by a
        /// different summary or by a separately processed try-statement.
        /// </summary>
        /// <param name="node">
        /// The syntax node to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if traversal must stop at the node;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummarySyntaxExecutionBoundary(
            SyntaxNode node)
        {
            return node is TryStatementSyntax ||
                   node is LocalFunctionStatementSyntax ||
                   node is AnonymousFunctionExpressionSyntax;
        }

        /// <summary>
        /// Traverses one operation tree and records user-defined operator and
        /// conversion calls.
        /// </summary>
        /// <param name="operationRoot">
        /// The operation-tree root.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for operand value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving edges.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The deduplication keys already collected for the current syntax
        /// fragment.
        /// </param>
        private static void AnalyzeSummaryOperationTree(
            IOperation operationRoot,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            Stack<IOperation> pendingOperations =
                new();

            pendingOperations.Push(
                operationRoot);

            while (pendingOperations.Count > 0)
            {
                IOperation operation =
                    pendingOperations.Pop();

                if (IsSummaryOperationExecutionBoundary(
                        operation))
                {
                    continue;
                }

                switch (operation)
                {
                    case ICompoundAssignmentOperation compoundAssignment:
                        AnalyzeSummaryCompoundAssignment(
                            compoundAssignment,
                            semanticModel,
                            graph,
                            fragment,
                            callContext,
                            collectedCallKeys);
                        break;

                    case IIncrementOrDecrementOperation incrementOperation:
                        AnalyzeSummaryIncrementOrDecrement(
                            incrementOperation,
                            semanticModel,
                            graph,
                            fragment,
                            callContext,
                            collectedCallKeys);
                        break;

                    case IBinaryOperation binaryOperation:
                        AnalyzeSummaryBinaryOperator(
                            binaryOperation,
                            semanticModel,
                            graph,
                            fragment,
                            callContext,
                            collectedCallKeys);
                        break;

                    case IUnaryOperation unaryOperation:
                        AnalyzeSummaryUnaryOperator(
                            unaryOperation,
                            semanticModel,
                            graph,
                            fragment,
                            callContext,
                            collectedCallKeys);
                        break;

                    case IConversionOperation conversionOperation:
                        AnalyzeSummaryConversion(
                            conversionOperation,
                            semanticModel,
                            graph,
                            fragment,
                            callContext,
                            collectedCallKeys);
                        break;
                }

                PushSummaryOperationChildren(
                    operation,
                    pendingOperations);
            }
        }

        /// <summary>
        /// Pushes child operations in reverse order so they are processed in
        /// semantic evaluation order.
        /// </summary>
        /// <param name="operation">
        /// The operation whose children should be pushed.
        /// </param>
        /// <param name="pendingOperations">
        /// The destination operation stack.
        /// </param>
        private static void PushSummaryOperationChildren(
            IOperation operation,
            Stack<IOperation> pendingOperations)
        {
            IOperation[] children =
                operation.ChildOperations
                    .ToArray();

            for (int index = children.Length - 1;
                 index >= 0;
                 index--)
            {
                pendingOperations.Push(
                    children[index]);
            }
        }

        /// <summary>
        /// Determines whether an operation belongs to executable code handled
        /// by another summary fragment.
        /// </summary>
        /// <param name="operation">
        /// The operation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for nested try-statements, local functions,
        /// and anonymous functions; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryOperationExecutionBoundary(
            IOperation operation)
        {
            return operation is ITryOperation ||
                   operation is ILocalFunctionOperation ||
                   operation is IAnonymousFunctionOperation;
        }

        /// <summary>
        /// Records the operator and conversion calls belonging to a compound
        /// assignment.
        /// </summary>
        /// <param name="operation">
        /// The compound-assignment operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AnalyzeSummaryCompoundAssignment(
            ICompoundAssignmentOperation operation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            AddSummaryCommonConversionEdge(
                operation.InConversion,
                operation.Target,
                GetSummaryOperatorOperandExpression(
                    operation.Target),
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);

            if (!operation.IsLifted ||
                !IsSummaryKnownNullOperation(
                    operation.Target) &&
                !IsSummaryKnownNullOperation(
                    operation.Value))
            {
                ExpressionSyntax? leftOperand =
                    operation.InConversion.IsIdentity
                        ? GetSummaryOperatorOperandExpression(
                            operation.Target)
                        : null;

                ExpressionSyntax? rightOperand =
                    GetSummaryOperatorOperandExpression(
                        operation.Value);

                AddSummaryOperationCallEdge(
                    operation.OperatorMethod,
                    ExceptionFlowPathStepKind.BinaryOperatorCall,
                    [
                        leftOperand,
                        rightOperand
                    ],
                    operation.Syntax,
                    semanticModel,
                    graph,
                    fragment,
                    callContext,
                    collectedCallKeys);
            }

            AddSummaryCommonConversionEdge(
                operation.OutConversion,
                sourceOperation: null,
                sourceExpression: null,
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Records one user-defined increment or decrement operation.
        /// </summary>
        /// <param name="operation">
        /// The increment or decrement operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the operator target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AnalyzeSummaryIncrementOrDecrement(
            IIncrementOrDecrementOperation operation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            if (operation.IsLifted &&
                IsSummaryKnownNullOperation(
                    operation.Target))
            {
                return;
            }

            AddSummaryOperationCallEdge(
                operation.OperatorMethod,
                ExceptionFlowPathStepKind.UnaryOperatorCall,
                [
                    GetSummaryOperatorOperandExpression(
                        operation.Target)
                ],
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Records one user-defined binary operation and any Boolean
        /// short-circuit operator associated with it.
        /// </summary>
        /// <param name="operation">
        /// The binary operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving operator targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AnalyzeSummaryBinaryOperator(
            IBinaryOperation operation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            if (operation.IsLifted &&
                (IsSummaryKnownNullOperation(
                     operation.LeftOperand) ||
                 IsSummaryKnownNullOperation(
                     operation.RightOperand)))
            {
                return;
            }

            AddSummaryConditionalBooleanOperatorEdge(
                operation,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);

            AddSummaryOperationCallEdge(
                operation.OperatorMethod,
                ExceptionFlowPathStepKind.BinaryOperatorCall,
                [
                    GetSummaryOperatorOperandExpression(
                        operation.LeftOperand),
                    GetSummaryOperatorOperandExpression(
                        operation.RightOperand)
                ],
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Records one user-defined unary operation.
        /// </summary>
        /// <param name="operation">
        /// The unary operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the operator target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AnalyzeSummaryUnaryOperator(
            IUnaryOperation operation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            if (operation.IsLifted &&
                IsSummaryKnownNullOperation(
                    operation.Operand))
            {
                return;
            }

            AddSummaryOperationCallEdge(
                operation.OperatorMethod,
                ExceptionFlowPathStepKind.UnaryOperatorCall,
                [
                    GetSummaryOperatorOperandExpression(
                        operation.Operand)
                ],
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Records one user-defined conversion operation.
        /// </summary>
        /// <param name="operation">
        /// The conversion operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the conversion target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AnalyzeSummaryConversion(
            IConversionOperation operation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            AddSummaryCommonConversionEdge(
                operation.Conversion,
                operation.Operand,
                GetSummaryOperatorOperandExpression(
                    operation.Operand),
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Adds the <c>true</c> or <c>false</c> operator required by a
        /// user-defined conditional-and or conditional-or operation.
        /// </summary>
        /// <param name="operation">
        /// The conditional binary operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the Boolean operator target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AddSummaryConditionalBooleanOperatorEdge(
            IBinaryOperation operation,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            string? requiredOperatorName =
                operation.OperatorKind switch
                {
                    BinaryOperatorKind.ConditionalAnd =>
                        FalseOperatorMetadataName,

                    BinaryOperatorKind.ConditionalOr =>
                        TrueOperatorMetadataName,

                    _ => null
                };

            if (requiredOperatorName == null ||
                operation.OperatorMethod
                    is not IMethodSymbol binaryOperator ||
                binaryOperator.Parameters.Length == 0)
            {
                return;
            }

            IMethodSymbol[] candidates =
                binaryOperator.ContainingType
                    .GetMembers(
                        requiredOperatorName)
                    .OfType<IMethodSymbol>()
                    .Where(
                        candidate =>
                            candidate.MethodKind ==
                                MethodKind.UserDefinedOperator &&
                            candidate.Parameters.Length == 1 &&
                            candidate.ReturnType.SpecialType ==
                                SpecialType.System_Boolean &&
                            SymbolEqualityComparer.Default.Equals(
                                candidate.Parameters[0].Type,
                                binaryOperator.Parameters[0].Type))
                    .ToArray();

            if (candidates.Length != 1)
            {
                return;
            }

            IMethodSymbol booleanOperator =
                candidates[0];

            if (ContainsSummaryOperationMethod(
                    operation,
                    booleanOperator))
            {
                return;
            }

            AddSummaryOperationCallEdge(
                booleanOperator,
                ExceptionFlowPathStepKind.UnaryOperatorCall,
                [
                    GetSummaryOperatorOperandExpression(
                        operation.LeftOperand)
                ],
                operation.Syntax,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Determines whether an operation subtree already exposes a specific
        /// method as an operator or conversion operation.
        /// </summary>
        /// <param name="operation">
        /// The operation subtree to inspect.
        /// </param>
        /// <param name="expectedMethod">
        /// The operator method to locate.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the method is already represented;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsSummaryOperationMethod(
            IOperation operation,
            IMethodSymbol expectedMethod)
        {
            Stack<IOperation> pendingOperations =
                new();

            PushSummaryOperationChildren(
                operation,
                pendingOperations);

            while (pendingOperations.Count > 0)
            {
                IOperation currentOperation =
                    pendingOperations.Pop();

                if (IsSummaryOperationExecutionBoundary(
                        currentOperation))
                {
                    continue;
                }

                IMethodSymbol? currentMethod =
                    currentOperation switch
                    {
                        IBinaryOperation binary =>
                            binary.OperatorMethod,

                        IUnaryOperation unary =>
                            unary.OperatorMethod,

                        IIncrementOrDecrementOperation increment =>
                            increment.OperatorMethod,

                        ICompoundAssignmentOperation compound =>
                            compound.OperatorMethod,

                        IConversionOperation conversion =>
                            conversion.Conversion.MethodSymbol,

                        _ => null
                    };

                if (currentMethod != null &&
                    SymbolEqualityComparer.Default.Equals(
                        currentMethod.OriginalDefinition,
                        expectedMethod.OriginalDefinition))
                {
                    return true;
                }

                PushSummaryOperationChildren(
                    currentOperation,
                    pendingOperations);
            }

            return false;
        }

        /// <summary>
        /// Adds one user-defined conversion represented by common conversion
        /// information.
        /// </summary>
        /// <param name="conversion">
        /// The conversion information.
        /// </param>
        /// <param name="sourceOperation">
        /// The operation producing the value to convert, if available.
        /// </param>
        /// <param name="sourceExpression">
        /// The expression supplying the conversion parameter, if suitable for
        /// value-fact propagation.
        /// </param>
        /// <param name="sourceNode">
        /// The source node representing the conversion.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the conversion target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AddSummaryCommonConversionEdge(
            CommonConversion conversion,
            IOperation? sourceOperation,
            ExpressionSyntax? sourceExpression,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            if (!conversion.IsUserDefined ||
                conversion.MethodSymbol
                    is not IMethodSymbol conversionMethod)
            {
                return;
            }

            if (conversion.IsNullable &&
                sourceOperation != null &&
                IsSummaryKnownNullOperation(
                    sourceOperation))
            {
                return;
            }

            AddSummaryOperationCallEdge(
                conversionMethod,
                ExceptionFlowPathStepKind.ConversionOperatorCall,
                [
                    sourceExpression
                ],
                sourceNode,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedCallKeys);
        }

        /// <summary>
        /// Adds one context-sensitive user-defined operator or conversion edge.
        /// </summary>
        /// <param name="targetMethod">
        /// The selected operator or conversion method.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the call.
        /// </param>
        /// <param name="operandExpressions">
        /// The expressions whose facts can safely be mapped to target
        /// parameters.
        /// </param>
        /// <param name="sourceNode">
        /// The source node representing the operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target node.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the edge.
        /// </param>
        /// <param name="callContext">
        /// The caller's known value facts.
        /// </param>
        /// <param name="collectedCallKeys">
        /// The current fragment's deduplication keys.
        /// </param>
        private static void AddSummaryOperationCallEdge(
            IMethodSymbol? targetMethod,
            ExceptionFlowPathStepKind stepKind,
            IReadOnlyList<ExpressionSyntax?> operandExpressions,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedCallKeys)
        {
            if (targetMethod == null ||
                !IsSupportedSummaryOperationMethod(
                    targetMethod,
                    stepKind))
            {
                return;
            }

            string callKey =
                CreateSummaryOperationCallKey(
                    targetMethod,
                    stepKind,
                    sourceNode);

            if (!collectedCallKeys.Add(
                    callKey))
            {
                return;
            }

            ExceptionFlowCallContext targetContext =
                CreateSummaryOperationCallContext(
                    targetMethod,
                    operandExpressions,
                    semanticModel,
                    callContext);

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
        /// Determines whether a method symbol is appropriate for an operator
        /// or conversion path step.
        /// </summary>
        /// <param name="targetMethod">
        /// The method to inspect.
        /// </param>
        /// <param name="stepKind">
        /// The requested path-step kind.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a matching user-defined method;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSupportedSummaryOperationMethod(
            IMethodSymbol targetMethod,
            ExceptionFlowPathStepKind stepKind)
        {
            if (stepKind ==
                ExceptionFlowPathStepKind.ConversionOperatorCall)
            {
                return targetMethod.MethodKind ==
                       MethodKind.Conversion;
            }

            return targetMethod.MethodKind ==
                   MethodKind.UserDefinedOperator;
        }

        /// <summary>
        /// Creates the call context for an operator or conversion method.
        /// </summary>
        /// <param name="targetMethod">
        /// The selected target method.
        /// </param>
        /// <param name="operandExpressions">
        /// The safely mapped operand expressions.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for value facts.
        /// </param>
        /// <param name="callerContext">
        /// The caller's known value facts.
        /// </param>
        /// <returns>
        /// The context containing facts proven for operator parameters.
        /// </returns>
        private static ExceptionFlowCallContext
            CreateSummaryOperationCallContext(
                IMethodSymbol targetMethod,
                IReadOnlyList<ExpressionSyntax?> operandExpressions,
                SemanticModel semanticModel,
                ExceptionFlowCallContext callerContext)
        {
            Dictionary<int, ExceptionFlowValueFacts>
                knownParameterFacts =
                    new();

            int mappedParameterCount =
                Math.Min(
                    targetMethod.Parameters.Length,
                    operandExpressions.Count);

            for (int index = 0;
                 index < mappedParameterCount;
                 index++)
            {
                ExpressionSyntax? operandExpression =
                    operandExpressions[index];

                if (operandExpression == null)
                {
                    continue;
                }

                ExceptionFlowValueFacts facts =
                    GetExpressionValueFacts(
                        operandExpression,
                        semanticModel,
                        callerContext);

                if (facts != ExceptionFlowValueFacts.None)
                {
                    knownParameterFacts[index] =
                        facts;
                }
            }

            return new ExceptionFlowCallContext(
                targetMethod,
                knownParameterFacts);
        }

        /// <summary>
        /// Gets an expression whose value facts can safely represent the value
        /// received by an operator parameter.
        /// </summary>
        /// <param name="operation">
        /// The operand operation.
        /// </param>
        /// <returns>
        /// The operand expression when no value-changing conversion separates
        /// it from the target parameter; otherwise <see langword="null"/>.
        /// </returns>
        private static ExpressionSyntax?
            GetSummaryOperatorOperandExpression(
                IOperation operation)
        {
            if (operation is IConversionOperation conversion &&
                !conversion.Conversion.IsIdentity)
            {
                return null;
            }

            return operation.Syntax
                as ExpressionSyntax;
        }

        /// <summary>
        /// Determines whether an operation is proven to produce the constant
        /// null value.
        /// </summary>
        /// <param name="operation">
        /// The operation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for a constant null operation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryKnownNullOperation(
            IOperation operation)
        {
            return operation.ConstantValue.HasValue &&
                   operation.ConstantValue.Value == null;
        }

        /// <summary>
        /// Creates a deterministic key for one collected operator or
        /// conversion call site.
        /// </summary>
        /// <param name="targetMethod">
        /// The selected method.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind.
        /// </param>
        /// <param name="sourceNode">
        /// The operation's source node.
        /// </param>
        /// <returns>
        /// A key that distinguishes target, operation kind, and source
        /// position.
        /// </returns>
        private static string CreateSummaryOperationCallKey(
            IMethodSymbol targetMethod,
            ExceptionFlowPathStepKind stepKind,
            SyntaxNode sourceNode)
        {
            string symbolName =
                targetMethod.OriginalDefinition
                    .ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat);

            return
                $"{(int)stepKind}|" +
                $"{sourceNode.SyntaxTree.FilePath}|" +
                $"{sourceNode.SpanStart}|" +
                $"{sourceNode.Span.Length}|" +
                symbolName;
        }
    }
}
