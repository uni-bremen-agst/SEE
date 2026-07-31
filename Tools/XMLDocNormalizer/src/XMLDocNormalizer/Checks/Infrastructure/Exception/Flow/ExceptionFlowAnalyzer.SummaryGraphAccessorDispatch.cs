using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains runtime-target expansion for property, indexer, and event
    /// accessors.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Adds a direct accessor edge or one edge for every known compatible
        /// runtime implementation.
        /// </summary>
        /// <param name="selectedAccessor">
        /// The accessor selected by compile-time binding.
        /// </param>
        /// <param name="selectedContext">
        /// The call context created for the selected accessor.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the accessor operation.
        /// </param>
        /// <param name="accessedSymbol">
        /// The property, indexer, or event represented by the source syntax.
        /// </param>
        /// <param name="sourceNode">
        /// The syntax responsible for the accessor operation.
        /// </param>
        /// <param name="receiverOperation">
        /// The receiver operation, or <see langword="null"/> for static
        /// accessors.
        /// </param>
        /// <param name="exactReceiverType">
        /// An exact receiver type proven from the source, or
        /// <see langword="null"/>.
        /// </param>
        /// <param name="staticallyBound">
        /// Whether the source explicitly uses <see langword="base"/>.
        /// </param>
        /// <param name="omitImplicitTargets">
        /// Whether compiler-generated accessor implementations without user
        /// code should be omitted.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving target summaries.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving call edges.
        /// </param>
        private static void AddSummaryAccessorCallEdges(
            IMethodSymbol selectedAccessor,
            ExceptionFlowCallContext selectedContext,
            ExceptionFlowPathStepKind stepKind,
            ISymbol accessedSymbol,
            SyntaxNode sourceNode,
            IOperation? receiverOperation,
            INamedTypeSymbol? exactReceiverType,
            bool staticallyBound,
            bool omitImplicitTargets,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
        {
            if (staticallyBound ||
                !RequiresSummaryRuntimeDispatch(
                    selectedAccessor))
            {
                if (omitImplicitTargets &&
                    selectedAccessor.IsImplicitlyDeclared)
                {
                    return;
                }

                AddSummaryAccessorTargetEdge(
                    selectedAccessor,
                    selectedContext,
                    stepKind,
                    accessedSymbol,
                    sourceNode,
                    graph,
                    fragment);

                return;
            }

            INamedTypeSymbol? receiverType =
                receiverOperation?.Type
                    as INamedTypeSymbol;

            IReadOnlyList<IMethodSymbol> resolvedTargets =
                ResolveSummaryRuntimeTargets(
                    selectedAccessor,
                    receiverType,
                    exactReceiverType,
                    semanticContext);

            IMethodSymbol[] runtimeTargets =
                resolvedTargets
                    .Where(
                        target =>
                            !omitImplicitTargets ||
                            !target.IsImplicitlyDeclared)
                    .ToArray();

            if (runtimeTargets.Length == 0)
            {
                if (omitImplicitTargets &&
                    selectedAccessor.IsImplicitlyDeclared)
                {
                    return;
                }

                AddSummaryAccessorTargetEdge(
                    selectedAccessor,
                    selectedContext,
                    stepKind,
                    accessedSymbol,
                    sourceNode,
                    graph,
                    fragment);

                return;
            }

            foreach (IMethodSymbol runtimeTarget
                     in runtimeTargets)
            {
                ExceptionFlowCallContext targetContext =
                    CreateDispatchTargetContext(
                        selectedAccessor,
                        runtimeTarget,
                        selectedContext);

                AddSummaryAccessorTargetEdge(
                    runtimeTarget,
                    targetContext,
                    stepKind,
                    accessedSymbol,
                    sourceNode,
                    graph,
                    fragment);
            }
        }

        /// <summary>
        /// Adds one resolved accessor target edge.
        /// </summary>
        /// <param name="targetAccessor">
        /// The resolved target accessor.
        /// </param>
        /// <param name="targetContext">
        /// The target call context.
        /// </param>
        /// <param name="stepKind">
        /// The path-step kind representing the access.
        /// </param>
        /// <param name="accessedSymbol">
        /// The statically accessed property, indexer, or event.
        /// </param>
        /// <param name="sourceNode">
        /// The source syntax responsible for the access.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target summary.
        /// </param>
        /// <param name="fragment">
        /// The local fragment receiving the edge.
        /// </param>
        private static void AddSummaryAccessorTargetEdge(
            IMethodSymbol targetAccessor,
            ExceptionFlowCallContext targetContext,
            ExceptionFlowPathStepKind stepKind,
            ISymbol accessedSymbol,
            SyntaxNode sourceNode,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment)
        {
            ExceptionFlowCallableKey targetKey =
                new(
                    targetAccessor,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        stepKind,
                        accessedSymbol,
                        sourceNode)));
        }

        /// <summary>
        /// Determines whether an accessor can select a different runtime
        /// implementation.
        /// </summary>
        /// <param name="accessor">
        /// The accessor to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for dispatchable class and interface
        /// accessors; otherwise <see langword="false"/>.
        /// </returns>
        private static bool RequiresSummaryRuntimeDispatch(
            IMethodSymbol accessor)
        {
            if (accessor.IsStatic ||
                accessor.IsSealed)
            {
                return false;
            }

            if (accessor.ContainingType.TypeKind ==
                TypeKind.Interface)
            {
                return true;
            }

            return accessor.IsAbstract ||
                   accessor.IsVirtual ||
                   accessor.IsOverride;
        }

        /// <summary>
        /// Determines whether an accessor source explicitly uses
        /// <see langword="base"/> and is therefore statically bound.
        /// </summary>
        /// <param name="accessNode">
        /// The property, indexer, or event access syntax.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for an explicit base access; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryBaseAccessorAccess(
            SyntaxNode accessNode)
        {
            return accessNode switch
            {
                MemberAccessExpressionSyntax memberAccess =>
                    memberAccess.Expression
                        is BaseExpressionSyntax,

                ElementAccessExpressionSyntax elementAccess =>
                    elementAccess.Expression
                        is BaseExpressionSyntax,

                AssignmentExpressionSyntax assignment =>
                    IsSummaryBaseAccessorAccess(
                        assignment.Left),

                PrefixUnaryExpressionSyntax prefixExpression =>
                    IsSummaryBaseAccessorAccess(
                        prefixExpression.Operand),

                PostfixUnaryExpressionSyntax postfixExpression =>
                    IsSummaryBaseAccessorAccess(
                        postfixExpression.Operand),

                _ => false
            };
        }

        /// <summary>
        /// Gets an exact accessor receiver type from a direct object creation
        /// or a direct object-initializer entry.
        /// </summary>
        /// <param name="sourceNode">
        /// The complete source access or initializer entry.
        /// </param>
        /// <param name="receiverOperation">
        /// The receiver operation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for object-creation type resolution.
        /// </param>
        /// <returns>
        /// The exact receiver type, or <see langword="null"/> when it cannot be
        /// proven.
        /// </returns>
        private static INamedTypeSymbol?
            GetSummaryAccessorExactReceiverType(
                SyntaxNode sourceNode,
                IOperation? receiverOperation,
                SemanticModel semanticModel)
        {
            INamedTypeSymbol? operationReceiverType =
                GetSummaryExactReceiverType(
                    receiverOperation);

            if (operationReceiverType != null)
            {
                return operationReceiverType;
            }

            SyntaxNode initializerEntry =
                sourceNode;

            if (sourceNode.Parent
                    is AssignmentExpressionSyntax assignment &&
                ReferenceEquals(
                    assignment.Left,
                    sourceNode))
            {
                initializerEntry =
                    assignment;
            }

            if (initializerEntry.Parent
                    is not InitializerExpressionSyntax initializer ||
                !initializer.IsKind(
                    SyntaxKind.ObjectInitializerExpression))
            {
                return null;
            }

            ExpressionSyntax? creationExpression =
                initializer.Parent switch
                {
                    ObjectCreationExpressionSyntax objectCreation =>
                        objectCreation,

                    ImplicitObjectCreationExpressionSyntax
                        implicitObjectCreation =>
                            implicitObjectCreation,

                    _ => null
                };

            if (creationExpression == null)
            {
                return null;
            }

            return semanticModel.GetTypeInfo(
                    creationExpression)
                .Type as INamedTypeSymbol;
        }
    }
}
