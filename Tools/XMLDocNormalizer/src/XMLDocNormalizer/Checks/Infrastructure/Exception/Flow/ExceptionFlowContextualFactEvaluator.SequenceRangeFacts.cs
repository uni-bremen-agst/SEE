using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowStableMemberFacts;
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
        /// Determines whether a local list starts empty and every operation
        /// before the current use preserves its non-null element invariant,
        /// including supported <c>AddRange</c> operations.
        /// </summary>
        /// <param name="expression">
        /// The current use of the local list.
        /// </param>
        /// <param name="localSymbol">
        /// The local list symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the use.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every element currently stored in the
        /// list is proven non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalListWithRangeAddsProvenToExcludeNullElements(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            if (!ExceptionFlowSequenceCollectionFactsProvider.IsListType(
                    localSymbol.Type)
                || localSymbol.DeclaringSyntaxReferences.Length != 1
                || localSymbol.DeclaringSyntaxReferences[0].GetSyntax() is not VariableDeclaratorSyntax variableDeclarator
                || variableDeclarator.Initializer == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(semanticModel, variableDeclarator.SyntaxTree);

            if (declarationSemanticModel == null
                || !ExceptionFlowSequenceCollectionFactsProvider.IsKnownEmptyListCreation(
                    variableDeclarator.Initializer.Value,
                    declarationSemanticModel))
            {
                return false;
            }

            SyntaxNode? containingCallable = variableDeclarator.Ancestors().FirstOrDefault(
                static node => node is MethodDeclarationSyntax || node is LocalFunctionStatementSyntax);

            if (containingCallable == null || containingCallable.SyntaxTree != expression.SyntaxTree)
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> references =
                containingCallable.DescendantNodes(
                        static node => node is not AnonymousFunctionExpressionSyntax
                            && node is not LocalFunctionStatementSyntax)
                    .OfType<IdentifierNameSyntax>()
                    .Where(identifier =>
                        identifier.SpanStart > variableDeclarator.Span.End
                        && identifier.SpanStart < expression.SpanStart
                        && ExpressionReferencesSymbol(identifier, localSymbol, declarationSemanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsLocalListReferenceSafeForNonNullElements(
                        reference,
                        declarationSemanticModel,
                        callerContext)
                    || IsListAddRangeReferenceSafeForNonNullElements(
                        reference,
                        declarationSemanticModel,
                        callerContext,
                        inspectedSequenceSources))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a sequence used as a range source is proven to
        /// contain only non-null elements. Foreach grouping variables are
        /// resolved directly through the enclosing foreach statement that
        /// declares them.
        /// </summary>
        /// <param name="expression">
        /// The range-source expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the expression.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every source element is proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRangeSourceProvenToExcludeNullElements(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext)
        {
            HashSet<ISymbol> inspectedSequenceSources =
                new(SymbolEqualityComparer.Default);

            return IsRangeSourceProvenToExcludeNullElements(
                expression,
                semanticModel,
                callerContext,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a sequence used as a range source is proven to
        /// contain only non-null elements while preventing recursive source
        /// analysis.
        /// </summary>
        /// <param name="expression">The range-source expression.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the expression.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every source element is proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRangeSourceProvenToExcludeNullElements(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                foreach (ForEachStatementSyntax foreachStatement
                         in unwrappedExpression.Ancestors().OfType<ForEachStatementSyntax>())
                {
                    ISymbol? iterationVariable = semanticModel.GetDeclaredSymbol(foreachStatement);

                    if (!SymbolEqualityComparer.Default.Equals(iterationVariable, localSymbol))
                    {
                        continue;
                    }

                    return IsGroupingSequenceProvenToContainNonNullElements(
                        foreachStatement.Expression,
                        semanticModel,
                        callerContext,
                        inspectedSequenceSources);
                }
            }

            return AreSequenceElementsProvenNonNull(
                unwrappedExpression,
                semanticModel,
                callerContext,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether one reference to a local list is a supported
        /// <c>AddRange</c> operation whose source sequence contains only
        /// non-null elements.
        /// </summary>
        /// <param name="reference">
        /// The local list reference acting as the destination.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the operation is a supported
        /// <c>List&lt;T&gt;.AddRange</c> with a proven non-null source
        /// sequence; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsListAddRangeReferenceSafeForNonNullElements(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            if (reference.Parent is not MemberAccessExpressionSyntax memberAccess
                || !ReferenceEquals(memberAccess.Expression, reference)
                || memberAccess.Parent is not InvocationExpressionSyntax invocation
                || !ReferenceEquals(invocation.Expression, memberAccess))
            {
                return false;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                || !ExceptionFlowSequenceCollectionFactsProvider.IsListType(
                    methodSymbol.ContainingType)
                || !string.Equals(methodSymbol.Name, "AddRange", StringComparison.Ordinal)
                || invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            return IsRangeSourceProvenToExcludeNullElements(
                invocation.ArgumentList.Arguments[0].Expression,
                semanticModel,
                callerContext,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether an out local produced by a successful dictionary
        /// <c>TryGetValue</c> represents a stored sequence whose elements are
        /// proven non-null.
        /// </summary>
        /// <param name="expression">
        /// The current use of the out local.
        /// </param>
        /// <param name="localSymbol">
        /// The out-local symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the use.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the use is guarded by successful
        /// <c>TryGetValue</c> and every sequence stored in the source
        /// dictionary preserves the non-null element invariant; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryTryGetValueOutSequenceProvenNonNullElements(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            if (localSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode declarationNode = localSymbol.DeclaringSyntaxReferences[0].GetSyntax();

            ArgumentSyntax? outArgument =
                declarationNode.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault();

            if (outArgument == null
                || !outArgument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)
                || outArgument.Parent?.Parent is not InvocationExpressionSyntax invocation
                || !ExceptionFlowSequenceCollectionFactsProvider.TryGetDictionaryReceiverFromTryGetValue(
                    invocation,
                    semanticModel,
                    out ExpressionSyntax? dictionaryExpression,
                    out IMethodSymbol? tryGetValueMethod)
                || dictionaryExpression == null
                || tryGetValueMethod == null)
            {
                return false;
            }

            int argumentIndex = invocation.ArgumentList.Arguments.IndexOf(outArgument);

            if (argumentIndex < 0
                || ExceptionFlowArgumentMapper.GetParameterIndex(outArgument, argumentIndex, tryGetValueMethod) != 1
                || !ExceptionFlowGuardFactsProvider.IsUseGuardedBySuccessfulTryGetValue(
                    expression,
                    invocation))
            {
                return false;
            }

            if (!IsDictionaryOfSequencesProvenToExcludeNullElements(
                    dictionaryExpression,
                    semanticModel))
            {
                return false;
            }

            return ExceptionFlowSequenceContentPreservationFactsProvider.DoesOutSequenceRemainUnchangedBeforeUse(
                expression,
                localSymbol,
                invocation,
                semanticModel);
        }

        /// <summary>
        /// Determines whether a get-only dictionary property owned by a
        /// private nested source type starts empty and every stored list
        /// preserves the non-null element invariant.
        /// </summary>
        /// <param name="dictionaryExpression">
        /// The dictionary expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the use.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every dictionary value is a list whose
        /// elements are proven non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryOfSequencesProvenToExcludeNullElements(
            ExpressionSyntax dictionaryExpression,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(dictionaryExpression);

            if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
            {
                return false;
            }

            IPropertySymbol normalizedProperty = propertySymbol.OriginalDefinition;

            if (!ExceptionFlowSequenceContentPreservationFactsProvider.IsSupportedDictionarySequenceProperty(
                    normalizedProperty,
                    semanticModel,
                    out PropertyDeclarationSyntax? propertyDeclaration,
                    out SemanticModel? declarationSemanticModel)
                || propertyDeclaration == null
                || declarationSemanticModel == null)
            {
                return false;
            }

            HashSet<ISymbol> inspectedDictionaries =
                new(SymbolEqualityComparer.Default);

            return IsDictionarySequencePropertyInvariantPreserved(
                normalizedProperty,
                propertyDeclaration,
                declarationSemanticModel,
                inspectedDictionaries);
        }

        /// <summary>
        /// Determines whether all references to a dictionary-of-lists property
        /// preserve the invariant that each stored list contains only non-null
        /// elements.
        /// </summary>
        /// <param name="propertySymbol">
        /// The dictionary property.
        /// </param>
        /// <param name="propertyDeclaration">
        /// The property declaration.
        /// </param>
        /// <param name="declarationSemanticModel">
        /// The semantic model associated with the declaration.
        /// </param>
        /// <param name="inspectedDictionaries">
        /// Dictionary properties currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every use preserves the invariant;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionarySequencePropertyInvariantPreserved(
            IPropertySymbol propertySymbol,
            PropertyDeclarationSyntax propertyDeclaration,
            SemanticModel declarationSemanticModel,
            HashSet<ISymbol> inspectedDictionaries)
        {
            if (!inspectedDictionaries.Add(propertySymbol))
            {
                return false;
            }

            try
            {
                INamedTypeSymbol containingType = propertySymbol.ContainingType;

                while (containingType.ContainingType != null)
                {
                    containingType = containingType.ContainingType;
                }

                foreach (SyntaxReference typeReference in containingType.DeclaringSyntaxReferences)
                {
                    SyntaxNode typeNode = typeReference.GetSyntax();

                    SemanticModel? typeSemanticModel =
                        ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(declarationSemanticModel, typeNode.SyntaxTree);

                    if (typeSemanticModel == null)
                    {
                        return false;
                    }

                    IEnumerable<IdentifierNameSyntax> references =
                        typeNode.DescendantNodes(
                                static node => node is not AnonymousFunctionExpressionSyntax
                                    && node is not LocalFunctionStatementSyntax)
                            .OfType<IdentifierNameSyntax>()
                            .Where(identifier =>
                                ExpressionReferencesSymbol(
                                    identifier,
                                    propertySymbol,
                                    typeSemanticModel));

                    foreach (IdentifierNameSyntax reference in references)
                    {
                        if (!IsDictionarySequencePropertyReferenceSafe(
                                reference,
                                propertySymbol,
                                typeSemanticModel,
                                inspectedDictionaries))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            finally
            {
                inspectedDictionaries.Remove(propertySymbol);
            }
        }

        /// <summary>
        /// Determines whether one reference to a dictionary-of-lists property
        /// preserves its nested non-null element invariant.
        /// </summary>
        /// <param name="reference">
        /// The property reference.
        /// </param>
        /// <param name="propertySymbol">
        /// The dictionary property.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the reference.
        /// </param>
        /// <param name="inspectedDictionaries">
        /// Dictionary properties currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is a supported insertion
        /// or read; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionarySequencePropertyReferenceSafe(
            IdentifierNameSyntax reference,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedDictionaries)
        {
            ExpressionSyntax dictionaryExpression = reference;

            if (reference.Parent is MemberAccessExpressionSyntax receiverAccess
                && ReferenceEquals(receiverAccess.Name, reference))
            {
                dictionaryExpression = receiverAccess;
            }

            if (dictionaryExpression.Parent is ElementAccessExpressionSyntax elementAccess
                && ReferenceEquals(elementAccess.Expression, dictionaryExpression)
                && elementAccess.Parent is AssignmentExpressionSyntax assignment
                && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                && ReferenceEquals(assignment.Left, elementAccess))
            {
                return IsStoredSequenceExpressionProvenNonNullElements(
                    assignment.Right,
                    semanticModel);
            }

            if (dictionaryExpression.Parent is not MemberAccessExpressionSyntax memberAccess
                || !ReferenceEquals(memberAccess.Expression, dictionaryExpression)
                || memberAccess.Parent is not InvocationExpressionSyntax invocation
                || !ReferenceEquals(invocation.Expression, memberAccess))
            {
                return false;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                || !ExceptionFlowSequenceCollectionFactsProvider.IsDictionaryType(methodSymbol.ContainingType))
            {
                return false;
            }

            if (string.Equals(methodSymbol.Name, "TryGetValue", StringComparison.Ordinal))
            {
                return DoesDictionaryTryGetValueAliasPreserveNonNullElements(
                    invocation,
                    propertySymbol,
                    semanticModel);
            }

            if (!string.Equals(methodSymbol.Name, "Add", StringComparison.Ordinal)
                && !string.Equals(methodSymbol.Name, "TryAdd", StringComparison.Ordinal))
            {
                return false;
            }

            for (int argumentIndex = 0;
                 argumentIndex < invocation.ArgumentList.Arguments.Count;
                 argumentIndex++)
            {
                ArgumentSyntax argument = invocation.ArgumentList.Arguments[argumentIndex];

                int parameterIndex =
                    ExceptionFlowArgumentMapper.GetParameterIndex(argument, argumentIndex, methodSymbol);

                if (parameterIndex == 1)
                {
                    return IsStoredSequenceExpressionProvenNonNullElements(
                        argument.Expression,
                        semanticModel);
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a sequence being stored in a dictionary is
        /// proven to contain only non-null elements.
        /// </summary>
        /// <param name="expression">
        /// The sequence expression being stored.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the storage site.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the sequence is proven safe; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsStoredSequenceExpressionProvenNonNullElements(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            ExceptionFlowCallContext localContext =
                new(semanticModel.GetEnclosingSymbol(expression.SpanStart));

            if (AreSequenceElementsProvenNonNull(expression, semanticModel, localContext)
                || ExceptionFlowSequenceCollectionFactsProvider.IsKnownEmptyListCreation(
                    expression,
                    semanticModel))
            {
                return true;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);

            if (symbolInfo.Symbol is not ILocalSymbol localSymbol
                || !ExceptionFlowSymbolUsageFacts.TryGetPrecedingSimpleLocalAssignment(
                    expression,
                    localSymbol,
                    semanticModel,
                    out ExpressionSyntax? assignedExpression)
                || assignedExpression == null)
            {
                return false;
            }

            if (ExceptionFlowSequenceCollectionFactsProvider.IsKnownEmptyListCreation(
                    assignedExpression,
                    semanticModel))
            {
                return true;
            }

            return AreSequenceElementsProvenNonNull(
                assignedExpression,
                semanticModel,
                localContext);
        }

        /// <summary>
        /// Determines whether every alias produced by a dictionary
        /// <c>TryGetValue</c> preserves the non-null element invariant of the
        /// stored list.
        /// </summary>
        /// <param name="invocation">
        /// The <c>TryGetValue</c> invocation.
        /// </param>
        /// <param name="dictionaryProperty">
        /// The source dictionary property.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every later use of the out local is
        /// proven to preserve the invariant; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool DoesDictionaryTryGetValueAliasPreserveNonNullElements(
            InvocationExpressionSyntax invocation,
            IPropertySymbol dictionaryProperty,
            SemanticModel semanticModel)
        {
            SymbolInfo invocationSymbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (invocationSymbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            ArgumentSyntax? outArgument = null;

            for (int argumentIndex = 0;
                 argumentIndex < invocation.ArgumentList.Arguments.Count;
                 argumentIndex++)
            {
                ArgumentSyntax argument = invocation.ArgumentList.Arguments[argumentIndex];

                if (ExceptionFlowArgumentMapper.GetParameterIndex(argument, argumentIndex, methodSymbol) == 1)
                {
                    outArgument = argument;
                    break;
                }
            }

            if (outArgument == null
                || !outArgument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
            {
                return false;
            }

            ISymbol? aliasSymbol =
                ExceptionFlowSymbolUsageFacts.GetOutArgumentSymbol(
                    outArgument,
                    semanticModel);

            if (aliasSymbol is not ILocalSymbol aliasLocal)
            {
                return false;
            }

            SyntaxNode? containingCallable =
                invocation.Ancestors().FirstOrDefault(
                    static node => node is MethodDeclarationSyntax || node is LocalFunctionStatementSyntax);

            if (containingCallable == null)
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> references =
                containingCallable.DescendantNodes(
                        static node => node is not AnonymousFunctionExpressionSyntax
                            && node is not LocalFunctionStatementSyntax)
                    .OfType<IdentifierNameSyntax>()
                    .Where(identifier =>
                        identifier.SpanStart > outArgument.Span.End
                        && ExpressionReferencesSymbol(identifier, aliasLocal, semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (!IsDictionarySequenceAliasReferenceSafe(
                        reference,
                        aliasLocal,
                        dictionaryProperty,
                        semanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether one use of a list alias obtained from a
        /// dictionary preserves the stored sequence invariant.
        /// </summary>
        /// <param name="reference">
        /// The alias reference.
        /// </param>
        /// <param name="aliasLocal">
        /// The alias local.
        /// </param>
        /// <param name="dictionaryProperty">
        /// The dictionary property from which the alias originated.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the reference.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference preserves the invariant;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionarySequenceAliasReferenceSafe(
            IdentifierNameSyntax reference,
            ILocalSymbol aliasLocal,
            IPropertySymbol dictionaryProperty,
            SemanticModel semanticModel)
        {
            if (reference.Parent is AssignmentExpressionSyntax assignment
                && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                if (ReferenceEquals(assignment.Left, reference))
                {
                    return IsStoredSequenceExpressionProvenNonNullElements(
                        assignment.Right,
                        semanticModel);
                }

                if (ReferenceEquals(assignment.Right, reference)
                    && ExceptionFlowSymbolUsageFacts.AssignmentTargetsDictionaryProperty(
                        assignment.Left,
                        dictionaryProperty,
                        semanticModel))
                {
                    return true;
                }
            }

            if (ExceptionFlowSequenceContentPreservationFactsProvider.IsSupportedReadOnlySequenceObservation(reference, semanticModel)
                || ExceptionFlowSequenceContentPreservationFactsProvider.IsSupportedSequenceNullObservation(reference))
            {
                return true;
            }

            if (reference.Parent is MemberAccessExpressionSyntax memberAccess
                && ReferenceEquals(memberAccess.Expression, reference)
                && memberAccess.Parent is InvocationExpressionSyntax invocation
                && ReferenceEquals(invocation.Expression, memberAccess))
            {
                return IsListAliasMemberInvocationSafe(invocation, semanticModel);
            }

            if (reference.Parent is ArgumentSyntax argument
                && ReferenceEquals(argument.Expression, reference)
                && argument.Parent?.Parent is InvocationExpressionSyntax sourceInvocation)
            {
                if (ExceptionFlowSequenceCollectionFactsProvider.IsListAddRangeSourceArgument(
                        argument,
                        sourceInvocation,
                        semanticModel))
                {
                    return true;
                }

                return ExceptionFlowSequenceContentPreservationFactsProvider.IsSourceHelperArgumentProvenToPreserveSequenceContents(
                    reference,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a list alias member invocation preserves its
        /// non-null element invariant.
        /// </summary>
        /// <param name="invocation">
        /// The list invocation.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for supported removals and proven-safe
        /// additions; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsListAliasMemberInvocationSafe(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                || !ExceptionFlowSequenceCollectionFactsProvider.IsListType(
                    methodSymbol.ContainingType))
            {
                return false;
            }

            if (string.Equals(methodSymbol.Name, "Clear", StringComparison.Ordinal)
                || string.Equals(methodSymbol.Name, "Remove", StringComparison.Ordinal)
                || string.Equals(methodSymbol.Name, "RemoveAt", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(methodSymbol.Name, "AddRange", StringComparison.Ordinal)
                && invocation.ArgumentList.Arguments.Count == 1)
            {
                ArgumentSyntax sourceArgument = invocation.ArgumentList.Arguments[0];
                ExceptionFlowCallContext localContext =
                    new(semanticModel.GetEnclosingSymbol(sourceArgument.Expression.SpanStart));

                return IsRangeSourceProvenToExcludeNullElements(
                    sourceArgument.Expression,
                    semanticModel,
                    localContext);
            }

            if (!string.Equals(methodSymbol.Name, "Add", StringComparison.Ordinal)
                || invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            ExpressionSyntax valueExpression = invocation.ArgumentList.Arguments[0].Expression;
            ExceptionFlowCallContext valueContext =
                new(semanticModel.GetEnclosingSymbol(valueExpression.SpanStart));

            ExceptionFlowValueFacts valueFacts =
                GetExpressionValueFacts(valueExpression, semanticModel, valueContext);

            return valueFacts.ContainsAll(ExceptionFlowValueFacts.NonNull);
        }
    }
}
