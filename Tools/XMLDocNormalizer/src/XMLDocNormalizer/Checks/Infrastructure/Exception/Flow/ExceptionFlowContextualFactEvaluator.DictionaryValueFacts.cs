using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Evaluates contextual value, symbol, sequence, and call facts.
    /// </summary>
    internal static partial class ExceptionFlowContextualFactEvaluator
    {
        /// <summary>
        /// Determines whether every value stored in a dictionary expression is
        /// proven non-null.
        /// </summary>
        /// <param name="expression">
        /// The dictionary expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the dictionary is proven to contain no
        /// null values; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreDictionaryValuesProvenNonNull(
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

            if (symbolInfo.Symbol
                    is IParameterSymbol parameterSymbol
                && callerContext.GetParameterFacts(
                        parameterSymbol)
                    .ContainsAll(
                        ExceptionFlowValueFacts.NonNullDictionaryValues))
            {
                return true;
            }

            if (symbolInfo.Symbol
                    is not IFieldSymbol fieldSymbol)
            {
                return false;
            }

            HashSet<ISymbol> inspectedDictionarySources =
                new(SymbolEqualityComparer.Default);

            return IsPrivateReadonlyDictionaryFieldProvenToExcludeNullValues(
                fieldSymbol,
                semanticModel,
                inspectedDictionarySources);
        }

        /// <summary>
        /// Determines whether a private readonly dictionary field starts empty
        /// and every use preserves a non-null-value invariant.
        /// </summary>
        /// <param name="fieldSymbol">
        /// The dictionary field to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to inspect the field and its references.
        /// </param>
        /// <param name="inspectedDictionarySources">
        /// The dictionary sources already being inspected, used to avoid
        /// recursive provenance cycles.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every stored dictionary value is proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPrivateReadonlyDictionaryFieldProvenToExcludeNullValues(
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedDictionarySources)
        {
            IFieldSymbol normalizedField =
                fieldSymbol.OriginalDefinition;

            if (normalizedField.DeclaredAccessibility != Accessibility.Private
                || !normalizedField.IsReadOnly
                || normalizedField.IsStatic
                || !ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryType(normalizedField.Type)
                || normalizedField.DeclaringSyntaxReferences.Length != 1
                || normalizedField.ContainingType
                    .DeclaringSyntaxReferences.Length != 1
                || !inspectedDictionarySources.Add(normalizedField))
            {
                return false;
            }

            try
            {
                if (normalizedField.DeclaringSyntaxReferences[0].GetSyntax()
                        is not VariableDeclaratorSyntax variableDeclarator
                    || variableDeclarator.Initializer == null)
                {
                    return false;
                }

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null
                    || !ExceptionFlowSequenceCollectionFactsProvider.IsKnownEmptyDictionaryCreation(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel))
                {
                    return false;
                }

                if (normalizedField.ContainingType
                        .DeclaringSyntaxReferences[0].GetSyntax()
                        is not TypeDeclarationSyntax containingTypeDeclaration)
                {
                    return false;
                }

                IEnumerable<IdentifierNameSyntax> references =
                    containingTypeDeclaration
                        .DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(
                            identifier =>
                                ExpressionReferencesSymbol(
                                    identifier,
                                    normalizedField,
                                    declarationSemanticModel));

                foreach (IdentifierNameSyntax reference in references)
                {
                    if (!IsPrivateDictionaryFieldReferenceSafeForNonNullValues(
                            reference,
                            declarationSemanticModel,
                            inspectedDictionarySources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedDictionarySources.Remove(normalizedField);
            }
        }

        /// <summary>
        /// Determines whether one reference to a private dictionary field can
        /// preserve its non-null-value invariant.
        /// </summary>
        /// <param name="reference">
        /// The dictionary field reference to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the reference.
        /// </param>
        /// <param name="inspectedDictionarySources">
        /// The dictionary sources already being inspected, used to avoid
        /// recursive provenance cycles.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference preserves the non-null
        /// value invariant; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPrivateDictionaryFieldReferenceSafeForNonNullValues(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedDictionarySources)
        {
            ExpressionSyntax fieldExpression =
                reference;

            if (reference.Parent
                    is MemberAccessExpressionSyntax receiverAccess
                && ReferenceEquals(
                    receiverAccess.Name,
                    reference))
            {
                fieldExpression = receiverAccess;
            }

            if (fieldExpression.Parent
                    is MemberAccessExpressionSyntax memberAccess
                && ReferenceEquals(
                    memberAccess.Expression,
                    fieldExpression))
            {
                if (memberAccess.Parent
                        is InvocationExpressionSyntax invocation
                    && ReferenceEquals(
                        invocation.Expression,
                        memberAccess))
                {
                    return IsDictionaryMemberInvocationSafeForNonNullValues(
                        invocation,
                        semanticModel);
                }

                return false;
            }

            if (fieldExpression.Parent
                    is not ArgumentSyntax argument
                || !ReferenceEquals(
                    argument.Expression,
                    fieldExpression))
            {
                return false;
            }

            if (argument.Parent?.Parent
                    is InvocationExpressionSyntax helperInvocation)
            {
                return IsDictionarySourceHelperArgumentSafeForNonNullValues(
                    argument,
                    helperInvocation,
                    semanticModel,
                    inspectedDictionarySources);
            }

            if (argument.Parent?.Parent
                    is ObjectCreationExpressionSyntax objectCreation)
            {
                return ExceptionFlowSequenceCollectionFactsProvider.IsReadOnlyDictionaryWrapperConstruction(
                    argument,
                    objectCreation,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a dictionary insertion value is proven non-null,
        /// including a value established by the nearest straight-line local
        /// assignment.
        /// </summary>
        /// <param name="expression">
        /// The value expression being inserted into the dictionary.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the insertion.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the inserted value is proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryInsertionValueProvenNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            ISymbol? enclosingSymbol =
                semanticModel.GetEnclosingSymbol(
                    expression.SpanStart);

            ExceptionFlowCallContext context =
                new(enclosingSymbol);

            ExceptionFlowValueFacts facts =
                GetExpressionValueFacts(
                    expression,
                    semanticModel,
                    context);

            if (facts.ContainsAll(ExceptionFlowValueFacts.NonNull))
            {
                return true;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(expression);

            if (symbolInfo.Symbol
                    is not ILocalSymbol localSymbol
                || !ExceptionFlowSymbolUsageFacts.TryGetPrecedingSimpleLocalAssignment(
                    expression,
                    localSymbol,
                    semanticModel,
                    out ExpressionSyntax? assignedExpression)
                || assignedExpression == null)
            {
                return false;
            }

            ExceptionFlowValueFacts assignedFacts =
                GetExpressionValueFacts(
                    assignedExpression,
                    semanticModel,
                    context);

            return assignedFacts.ContainsAll(
                ExceptionFlowValueFacts.NonNull);
        }
    }
}
