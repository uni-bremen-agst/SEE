using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains propagation and validation of sequence-element facts across
    /// callable boundaries.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an argument expression is proven to produce only
        /// non-null sequence elements.
        /// </summary>
        /// <param name="expression">
        /// The supplied argument expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the sequence elements are proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreSequenceElementsProvenNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            Conversion conversion =
                semanticModel.GetConversion(expression);

            if (conversion.IsUserDefined)
            {
                return false;
            }

            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol
                && callerContext.GetParameterFacts(parameterSymbol)
                    .ContainsAll(ExceptionFlowValueFacts.NonNullElements))
            {
                return true;
            }

            ISymbol? sequenceSymbol =
                symbolInfo.Symbol;

            if (sequenceSymbol is ILocalSymbol localSymbol)
            {
                if (IsDictionaryTryGetValueOutSequenceProvenNonNullElements(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }

                if (IsLocalListWithRangeAddsProvenToExcludeNullElements(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel,
                        callerContext))
                {
                    return true;
                }
            }

            if ((sequenceSymbol is ILocalSymbol
                    || sequenceSymbol is IParameterSymbol)
                && IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper(
                    unwrappedExpression,
                    sequenceSymbol,
                    semanticModel))
            {
                return true;
            }

            HashSet<ISymbol> inspectedSequenceSources =
                new(SymbolEqualityComparer.Default);

            return IsSequenceExpressionProvenToExcludeNullElements(
                unwrappedExpression,
                semanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a foreach iteration variable is non-null because
        /// its source parameter received a sequence-element fact at the call
        /// site.
        /// </summary>
        /// <param name="expression">
        /// The iteration-variable usage being analyzed.
        /// </param>
        /// <param name="localSymbol">
        /// The iteration-variable symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the callable body.
        /// </param>
        /// <param name="callContext">
        /// The parameter facts known for the callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the iteration variable is proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsForeachIterationVariableProvenNonNullByCallContext(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            IEnumerable<ForEachStatementSyntax> enclosingStatements =
                expression.Ancestors()
                    .OfType<ForEachStatementSyntax>();

            foreach (ForEachStatementSyntax foreachStatement
                     in enclosingStatements)
            {
                ISymbol? iterationVariable =
                    semanticModel.GetDeclaredSymbol(
                        foreachStatement);

                if (!SymbolEqualityComparer.Default.Equals(
                        iterationVariable,
                        localSymbol))
                {
                    continue;
                }

                ExpressionSyntax sourceExpression =
                    UnwrapParenthesizedExpression(
                        foreachStatement.Expression);

                SymbolInfo sourceSymbolInfo =
                    semanticModel.GetSymbolInfo(
                        sourceExpression);

                if (sourceSymbolInfo.Symbol
                        is not IParameterSymbol parameterSymbol
                    || !callContext.GetParameterFacts(
                            parameterSymbol)
                        .ContainsAll(
                            ExceptionFlowValueFacts.NonNullElements))
                {
                    return false;
                }

                return IsSequenceParameterFactStillCurrent(
                    foreachStatement,
                    parameterSymbol,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a non-null element fact received for a sequence
        /// parameter is still valid at a foreach statement.
        /// </summary>
        /// <param name="foreachStatement">
        /// The foreach statement consuming the parameter.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter carrying the fact.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no preceding operation or loop-body use
        /// can mutate or expose the sequence; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSequenceParameterFactStillCurrent(
            ForEachStatementSyntax foreachStatement,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            if (foreachStatement.Parent
                is not BlockSyntax block)
            {
                return false;
            }

            foreach (StatementSyntax statement in block.Statements)
            {
                if (statement.SpanStart >=
                    foreachStatement.SpanStart)
                {
                    break;
                }

                if (!DoesStatementPreserveSequenceParameterContents(
                        statement,
                        parameterSymbol,
                        semanticModel))
                {
                    return false;
                }
            }

            IEnumerable<IdentifierNameSyntax> bodyReferences =
                foreachStatement.Statement
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                parameterSymbol,
                                semanticModel));

            return !bodyReferences.Any();
        }

        /// <summary>
        /// Determines whether a statement before a foreach loop only observes
        /// a sequence parameter without mutating or exposing it.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <param name="parameterSymbol">
        /// The sequence parameter whose element fact must remain valid.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every parameter reference is a
        /// supported read-only observation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool DoesStatementPreserveSequenceParameterContents(
            StatementSyntax statement,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            IEnumerable<IdentifierNameSyntax> references =
                statement.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                parameterSymbol,
                                semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsSupportedReadOnlySequenceObservation(
                        reference,
                        semanticModel)
                    || IsSupportedSequenceNullObservation(reference))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a sequence reference is used only for a direct
        /// comparison with <see langword="null"/>.
        /// </summary>
        /// <param name="reference">
        /// The sequence reference to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for supported equality or inequality
        /// comparisons with <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSupportedSequenceNullObservation(
            IdentifierNameSyntax reference)
        {
            if (reference.Parent
                is not BinaryExpressionSyntax comparison)
            {
                return false;
            }

            if (!comparison.IsKind(
                    SyntaxKind.EqualsExpression)
                && !comparison.IsKind(
                    SyntaxKind.NotEqualsExpression))
            {
                return false;
            }

            ExpressionSyntax otherExpression;

            if (ReferenceEquals(
                    comparison.Left,
                    reference))
            {
                otherExpression =
                    comparison.Right;
            }
            else if (ReferenceEquals(
                         comparison.Right,
                         reference))
            {
                otherExpression =
                    comparison.Left;
            }
            else
            {
                return false;
            }

            return otherExpression.IsKind(
                SyntaxKind.NullLiteralExpression);
        }
    }
}
