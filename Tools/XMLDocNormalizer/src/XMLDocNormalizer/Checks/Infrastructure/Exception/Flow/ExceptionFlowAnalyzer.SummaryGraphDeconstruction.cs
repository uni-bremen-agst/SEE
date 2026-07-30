using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for implicit Deconstruct calls and
    /// terminal conversions in assignments and deconstructing foreach
    /// variables.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects compiler-selected <c>Deconstruct</c> methods and
        /// user-defined terminal conversions in the current callable.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for deconstruction binding information.
        /// </param>
        /// <param name="graph">
        /// The graph receiving compiler-selected callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryDeconstructions(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            HashSet<string> collectedConversionCallKeys =
                new(StringComparer.Ordinal);

            foreach (AssignmentExpressionSyntax assignment
                     in GetSummaryDescendantsAndSelf
                         <AssignmentExpressionSyntax>(node))
            {
                if (!IsSummaryDeconstructionAssignment(
                        assignment))
                {
                    continue;
                }

                DeconstructionInfo deconstructionInfo =
                    semanticModel.GetDeconstructionInfo(
                        assignment);

                AnalyzeSummaryDeconstructionTree(
                    deconstructionInfo,
                    assignment,
                    assignment.Right,
                    semanticModel,
                    graph,
                    fragment,
                    callContext,
                    collectedConversionCallKeys);
            }

            foreach (ForEachVariableStatementSyntax forEachStatement
                     in GetSummaryDescendantsAndSelf
                         <ForEachVariableStatementSyntax>(node))
            {
                DeconstructionInfo deconstructionInfo =
                    semanticModel.GetDeconstructionInfo(
                        forEachStatement);

                AnalyzeSummaryDeconstructionTree(
                    deconstructionInfo,
                    forEachStatement,
                    receiverExpression: null,
                    semanticModel,
                    graph,
                    fragment,
                    callContext,
                    collectedConversionCallKeys);
            }
        }

        /// <summary>
        /// Traverses one Roslyn deconstruction-information tree and records
        /// its selected <c>Deconstruct</c> methods and terminal user-defined
        /// conversions.
        /// </summary>
        /// <param name="deconstructionInfo">
        /// The current deconstruction-information node.
        /// </param>
        /// <param name="sourceNode">
        /// The source assignment or foreach statement.
        /// </param>
        /// <param name="receiverExpression">
        /// The source expression whose value receives the top-level
        /// <c>Deconstruct</c> call, or <see langword="null"/> for nested or
        /// compiler-generated receivers.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver value facts.
        /// </param>
        /// <param name="graph">
        /// The graph receiving callable targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="collectedConversionCallKeys">
        /// The conversion call keys already collected for the current syntax
        /// fragment.
        /// </param>
        private static void AnalyzeSummaryDeconstructionTree(
            DeconstructionInfo deconstructionInfo,
            SyntaxNode sourceNode,
            ExpressionSyntax? receiverExpression,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedConversionCallKeys)
        {
            if (deconstructionInfo.Method
                is IMethodSymbol deconstructMethod)
            {
                AddSummaryImplicitMethodEdge(
                    deconstructMethod,
                    ExceptionFlowPathStepKind.DeconstructCall,
                    sourceNode,
                    receiverExpression,
                    semanticModel,
                    graph,
                    fragment,
                    callContext);
            }

            AddSummaryDeconstructionConversionEdge(
                deconstructionInfo,
                sourceNode,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedConversionCallKeys);

            if (deconstructionInfo.Nested.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (DeconstructionInfo nestedInfo
                     in deconstructionInfo.Nested)
            {
                AnalyzeSummaryDeconstructionTree(
                    nestedInfo,
                    sourceNode,
                    receiverExpression: null,
                    semanticModel,
                    graph,
                    fragment,
                    callContext,
                    collectedConversionCallKeys);
            }
        }

        /// <summary>
        /// Adds the user-defined conversion associated with one terminal
        /// position in a deconstruction-information tree.
        /// </summary>
        /// <param name="deconstructionInfo">
        /// The deconstruction-information node to inspect.
        /// </param>
        /// <param name="sourceNode">
        /// The assignment or foreach statement representing the conversion.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the source node.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the conversion target.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the conversion edge.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="collectedConversionCallKeys">
        /// The conversion call keys already collected for the current syntax
        /// fragment.
        /// </param>
        private static void AddSummaryDeconstructionConversionEdge(
            DeconstructionInfo deconstructionInfo,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext,
            HashSet<string> collectedConversionCallKeys)
        {
            if (deconstructionInfo.Conversion
                    is not Conversion conversion ||
                !conversion.IsUserDefined ||
                conversion.MethodSymbol
                    is not IMethodSymbol conversionMethod)
            {
                return;
            }

            AddSummaryOperationCallEdge(
                conversionMethod,
                ExceptionFlowPathStepKind.ConversionOperatorCall,
                [
                    null
                ],
                sourceNode,
                semanticModel,
                graph,
                fragment,
                callContext,
                collectedConversionCallKeys);
        }

        /// <summary>
        /// Determines whether an assignment syntax represents deconstruction
        /// rather than an ordinary assignment.
        /// </summary>
        /// <param name="assignment">
        /// The assignment to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for tuple-shaped or declaration-shaped
        /// deconstruction assignments; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSummaryDeconstructionAssignment(
            AssignmentExpressionSyntax assignment)
        {
            if (!assignment.IsKind(
                    SyntaxKind.SimpleAssignmentExpression))
            {
                return false;
            }

            ExpressionSyntax leftExpression =
                assignment.Left;

            while (leftExpression
                   is ParenthesizedExpressionSyntax parenthesized)
            {
                leftExpression =
                    parenthesized.Expression;
            }

            if (leftExpression is TupleExpressionSyntax)
            {
                return true;
            }

            return leftExpression
                       is DeclarationExpressionSyntax declaration &&
                   declaration.Designation
                       is ParenthesizedVariableDesignationSyntax;
        }
    }
}
