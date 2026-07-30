using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains uncertainty collection for operations whose callable or
    /// operator target is selected through C# dynamic binding at runtime.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects uncertainty for dynamically bound invocations, member and
        /// indexer accesses, object creations, operators, and conversions.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to obtain operation trees.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving uncertainty.
        /// </param>
        private static void AnalyzeSummaryDynamicBindings(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryFragment fragment)
        {
            foreach (IOperation operationRoot
                     in GetSummaryOperationRoots(
                         node,
                         semanticModel))
            {
                AnalyzeSummaryDynamicOperationTree(
                    operationRoot,
                    fragment);
            }
        }

        /// <summary>
        /// Traverses one operation tree and records each distinct kind of
        /// runtime dynamic binding represented in the current fragment.
        /// </summary>
        /// <param name="operationRoot">
        /// The operation-tree root.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving uncertainty.
        /// </param>
        private static void AnalyzeSummaryDynamicOperationTree(
            IOperation operationRoot,
            ExceptionFlowSummaryFragment fragment)
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

                AddSummaryDynamicBindingUncertainty(
                    operation,
                    fragment);

                PushSummaryOperationChildren(
                    operation,
                    pendingOperations);
            }
        }

        /// <summary>
        /// Adds uncertainty for one operation when its runtime behavior
        /// requires dynamic binding.
        /// </summary>
        /// <param name="operation">
        /// The operation to classify.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving uncertainty.
        /// </param>
        private static void AddSummaryDynamicBindingUncertainty(
            IOperation operation,
            ExceptionFlowSummaryFragment fragment)
        {
            switch (operation)
            {
                case IDynamicInvocationOperation:
                    fragment.AddUncertainTarget(
                        "Dynamic invocation binding");
                    return;

                case IDynamicObjectCreationOperation:
                    fragment.AddUncertainTarget(
                        "Dynamic object-creation binding");
                    return;

                case IDynamicIndexerAccessOperation:
                    fragment.AddUncertainTarget(
                        "Dynamic indexer binding");
                    return;

                case IDynamicMemberReferenceOperation
                    when !IsSummaryDynamicInvocationTarget(
                        operation):
                    fragment.AddUncertainTarget(
                        "Dynamic member binding");
                    return;

                case ICompoundAssignmentOperation compoundAssignment
                    when UsesSummaryDynamicBinding(
                        compoundAssignment):
                    fragment.AddUncertainTarget(
                        "Dynamic compound-assignment binding");
                    return;

                case IIncrementOrDecrementOperation incrementOperation
                    when IsSummaryDynamicType(
                        incrementOperation.Target.Type):
                    fragment.AddUncertainTarget(
                        "Dynamic increment or decrement binding");
                    return;

                case IBinaryOperation binaryOperation
                    when UsesSummaryDynamicBinding(
                        binaryOperation):
                    fragment.AddUncertainTarget(
                        "Dynamic binary-operator binding");
                    return;

                case IUnaryOperation unaryOperation
                    when IsSummaryDynamicType(
                        unaryOperation.Operand.Type):
                    fragment.AddUncertainTarget(
                        "Dynamic unary-operator binding");
                    return;

                case IConversionOperation conversionOperation
                    when UsesSummaryDynamicBinding(
                        conversionOperation):
                    fragment.AddUncertainTarget(
                        "Dynamic conversion binding");
                    return;
            }
        }

        /// <summary>
        /// Determines whether a dynamic member reference is only the target
        /// descriptor of an enclosing dynamic invocation.
        /// </summary>
        /// <param name="operation">
        /// The candidate dynamic member-reference operation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the enclosing invocation already
        /// represents the runtime binding; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDynamicInvocationTarget(
            IOperation operation)
        {
            return operation.Parent
                       is IDynamicInvocationOperation invocation &&
                   ReferenceEquals(
                       invocation.Operation,
                       operation);
        }

        /// <summary>
        /// Determines whether a compound assignment requires runtime dynamic
        /// binding.
        /// </summary>
        /// <param name="operation">
        /// The compound assignment to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an operand or result is dynamic;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool UsesSummaryDynamicBinding(
            ICompoundAssignmentOperation operation)
        {
            return IsSummaryDynamicType(
                       operation.Target.Type) ||
                   IsSummaryDynamicType(
                       operation.Value.Type) ||
                   IsSummaryDynamicType(
                       operation.Type);
        }

        /// <summary>
        /// Determines whether a binary operation requires runtime dynamic
        /// binding.
        /// </summary>
        /// <param name="operation">
        /// The binary operation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when an operand is dynamic; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool UsesSummaryDynamicBinding(
            IBinaryOperation operation)
        {
            return IsSummaryDynamicType(
                       operation.LeftOperand.Type) ||
                   IsSummaryDynamicType(
                       operation.RightOperand.Type);
        }

        /// <summary>
        /// Determines whether a conversion from a dynamic value requires
        /// runtime binding that is not already represented by its operand.
        /// </summary>
        /// <param name="operation">
        /// The conversion operation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a direct dynamic value is converted
        /// to a runtime-checked non-object type; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool UsesSummaryDynamicBinding(
            IConversionOperation operation)
        {
            if (!IsSummaryDynamicType(
                    operation.Operand.Type) ||
                IsSummaryDynamicType(
                    operation.Type) ||
                operation.Type?.SpecialType ==
                    SpecialType.System_Object)
            {
                return false;
            }

            return !IsSummaryDynamicBindingOperation(
                operation.Operand);
        }

        /// <summary>
        /// Determines whether an operation already represents a dynamic
        /// runtime binder action whose result conversion must not add a second
        /// uncertainty category.
        /// </summary>
        /// <param name="operation">
        /// The operation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for explicit dynamic binding operations and
        /// dynamic operators; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDynamicBindingOperation(
            IOperation operation)
        {
            return operation switch
            {
                IDynamicInvocationOperation => true,
                IDynamicObjectCreationOperation => true,
                IDynamicIndexerAccessOperation => true,
                IDynamicMemberReferenceOperation => true,
                ICompoundAssignmentOperation compoundAssignment =>
                    UsesSummaryDynamicBinding(
                        compoundAssignment),
                IIncrementOrDecrementOperation incrementOperation =>
                    IsSummaryDynamicType(
                        incrementOperation.Target.Type),
                IBinaryOperation binaryOperation =>
                    UsesSummaryDynamicBinding(
                        binaryOperation),
                IUnaryOperation unaryOperation =>
                    IsSummaryDynamicType(
                        unaryOperation.Operand.Type),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether a type symbol represents C#
        /// <see langword="dynamic"/>.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for the dynamic type; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDynamicType(
            ITypeSymbol? typeSymbol)
        {
            return typeSymbol?.TypeKind ==
                TypeKind.Dynamic;
        }
    }
}
