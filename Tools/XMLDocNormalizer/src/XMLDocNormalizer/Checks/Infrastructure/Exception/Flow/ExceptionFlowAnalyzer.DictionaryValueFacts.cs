using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning for framework dictionary values.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
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
        /// Gets value facts for the <c>Value</c> property of a
        /// <see cref="KeyValuePair{TKey,TValue}"/> produced by a dictionary
        /// parameter whose stored values are known non-null.
        /// </summary>
        /// <param name="expression">
        /// The property access expression to inspect.
        /// </param>
        /// <param name="propertySymbol">
        /// The property symbol represented by <paramref name="expression"/>.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the expression.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the current call.
        /// </param>
        /// <returns>
        /// The facts proven for the dictionary entry value.
        /// </returns>
        private static ExceptionFlowValueFacts GetDictionaryEntryValueFacts(
            ExpressionSyntax expression,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (!IsKeyValuePairValueProperty(propertySymbol))
            {
                return ExceptionFlowValueFacts.None;
            }

            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression
                    is not MemberAccessExpressionSyntax memberAccess)
            {
                return ExceptionFlowValueFacts.None;
            }

            SymbolInfo receiverSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess.Expression);

            if (receiverSymbolInfo.Symbol
                    is not ILocalSymbol iterationLocal)
            {
                return ExceptionFlowValueFacts.None;
            }

            foreach (ForEachStatementSyntax foreachStatement
                     in expression.Ancestors()
                         .OfType<ForEachStatementSyntax>())
            {
                ISymbol? declaredIterationSymbol =
                    semanticModel.GetDeclaredSymbol(
                        foreachStatement);

                if (!SymbolEqualityComparer.Default.Equals(
                        declaredIterationSymbol,
                        iterationLocal))
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
                        is not IParameterSymbol sourceParameter
                    || !callContext.GetParameterFacts(
                            sourceParameter)
                        .ContainsAll(
                            ExceptionFlowValueFacts.NonNullDictionaryValues)
                    || !IsSequenceParameterFactStillCurrent(
                        foreachStatement,
                        sourceParameter,
                        semanticModel))
                {
                    return ExceptionFlowValueFacts.None;
                }

                return ExceptionFlowValueFacts.NonNull;
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether a property is
        /// <see cref="KeyValuePair{TKey,TValue}.Value"/>.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the property is the framework
        /// <c>KeyValuePair&lt;TKey, TValue&gt;.Value</c> property; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsKeyValuePairValueProperty(
            IPropertySymbol propertySymbol)
        {
            INamedTypeSymbol containingType =
                propertySymbol.ContainingType.OriginalDefinition;

            return string.Equals(
                       propertySymbol.Name,
                       "Value",
                       StringComparison.Ordinal)
                && propertySymbol.Parameters.Length == 0
                && string.Equals(
                    containingType.Name,
                    "KeyValuePair",
                    StringComparison.Ordinal)
                && containingType.Arity == 2
                && string.Equals(
                    containingType.ContainingNamespace.ToDisplayString(),
                    "System.Collections.Generic",
                    StringComparison.Ordinal);
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
                || !IsDictionaryType(normalizedField.Type)
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
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null
                    || !IsKnownEmptyDictionaryCreation(
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
                return IsReadOnlyDictionaryWrapperConstruction(
                    argument,
                    objectCreation,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a dictionary field is supplied to a framework
        /// <see cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey,TValue}"/>
        /// constructor.
        /// </summary>
        /// <param name="argument">
        /// The constructor argument containing the dictionary expression.
        /// </param>
        /// <param name="objectCreation">
        /// The object creation expression containing the argument.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the object creation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the dictionary is passed as the wrapped
        /// dictionary argument; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsReadOnlyDictionaryWrapperConstruction(
            ArgumentSyntax argument,
            ObjectCreationExpressionSyntax objectCreation,
            SemanticModel semanticModel)
        {
            SymbolInfo constructorSymbolInfo =
                semanticModel.GetSymbolInfo(
                    objectCreation);

            if (constructorSymbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol
                || constructorSymbol.MethodKind != MethodKind.Constructor)
            {
                return false;
            }

            INamedTypeSymbol containingType =
                constructorSymbol.ContainingType.OriginalDefinition;

            if (!string.Equals(
                    containingType.Name,
                    "ReadOnlyDictionary",
                    StringComparison.Ordinal)
                || containingType.Arity != 2
                || !string.Equals(
                    containingType.ContainingNamespace.ToDisplayString(),
                    "System.Collections.ObjectModel",
                    StringComparison.Ordinal))
            {
                return false;
            }

            int argumentIndex =
                objectCreation.ArgumentList?.Arguments.IndexOf(argument)
                ?? -1;

            return argumentIndex >= 0
                && GetParameterIndexForArgument(
                    argument,
                    argumentIndex,
                    constructorSymbol) == 0;
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
        internal static bool IsDictionaryInsertionValueProvenNonNull(
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
                || !TryGetPrecedingSimpleLocalAssignment(
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

        /// <summary>
        /// Gets the nearest preceding straight-line assignment to a local.
        /// </summary>
        /// <param name="expression">
        /// The expression whose preceding statements are inspected.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol whose assignment is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow and symbol analysis.
        /// </param>
        /// <param name="assignedExpression">
        /// Receives the right-hand expression of the nearest qualifying
        /// assignment when one is found; otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a qualifying preceding assignment is
        /// found; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetPrecedingSimpleLocalAssignment(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? assignedExpression)
        {
            assignedExpression = null;

            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement?.Parent
                    is not BlockSyntax containingBlock)
            {
                return false;
            }

            int currentIndex =
                containingBlock.Statements.IndexOf(
                    currentStatement);

            if (currentIndex < 0)
            {
                return false;
            }

            for (int index = currentIndex - 1;
                 index >= 0;
                 index--)
            {
                StatementSyntax precedingStatement =
                    containingBlock.Statements[index];

                DataFlowAnalysis? dataFlow =
                    semanticModel.AnalyzeDataFlow(
                        precedingStatement);

                if (dataFlow?.Succeeded != true)
                {
                    return false;
                }

                bool writesLocal =
                    dataFlow.WrittenInside.Any(
                        writtenSymbol =>
                            SymbolEqualityComparer.Default.Equals(
                                writtenSymbol,
                                localSymbol));

                if (!writesLocal)
                {
                    continue;
                }

                if (precedingStatement
                        is not ExpressionStatementSyntax expressionStatement
                    || expressionStatement.Expression
                        is not AssignmentExpressionSyntax assignment
                    || !assignment.IsKind(
                        SyntaxKind.SimpleAssignmentExpression)
                    || !ExpressionReferencesSymbol(
                        assignment.Left,
                        localSymbol,
                        semanticModel))
                {
                    return false;
                }

                assignedExpression =
                    assignment.Right;

                return true;
            }

            return false;
        }
    }
}
