using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains sequence-element non-null reasoning used by exception-flow
    /// analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether a local sequence expression still represents an
        /// initializer whose elements are proven to exclude
        /// <see langword="null"/>.
        /// </summary>
        /// <param name="expression">
        /// The local sequence expression being inspected.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol represented by the expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the use site.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the local still contains an initializer
        /// whose sequence excludes <see langword="null"/> elements; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
     IsLocalSequenceExpressionProvenToExcludeNullElements(
         ExpressionSyntax expression,
         ILocalSymbol localSymbol,
         SemanticModel semanticModel,
         HashSet<ISymbol> inspectedSequenceSources)
        {
            if (localSymbol.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedSequenceSources.Add(localSymbol))
            {
                return false;
            }

            try
            {
                SyntaxNode declarationNode =
                    localSymbol.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        declarationNode.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    return false;
                }

                if (IsForeachGroupingLocalProvenToExcludeNullElements(
                        localSymbol,
                        declarationNode,
                        declarationSemanticModel,
                        inspectedSequenceSources))
                {
                    return true;
                }

                if (declarationNode
                        is not VariableDeclaratorSyntax variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    return false;
                }

                if (IsLocalListProvenToExcludeNullElements(
                        expression,
                        localSymbol,
                        variableDeclarator,
                        declarationSemanticModel))
                {
                    return true;
                }

                if (!IsLocalSequenceInitializerStillCurrent(
                        expression,
                        localSymbol,
                        variableDeclarator,
                        semanticModel))
                {
                    return false;
                }

                return IsSequenceExpressionProvenToExcludeNullElements(
                    variableDeclarator.Initializer.Value,
                    declarationSemanticModel,
                    inspectedSequenceSources);
            }
            finally
            {
                inspectedSequenceSources.Remove(localSymbol);
            }
        }

        /// <summary>
        /// Determines whether a local sequence has remained unchanged between
        /// its declaration and the current use site.
        /// </summary>
        /// <param name="expression">
        /// The current local-variable use.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol whose writes, mutations, and escapes are inspected.
        /// </param>
        /// <param name="variableDeclarator">
        /// The declaration containing the sequence initializer.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow and symbol analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when declaration and use occur in the same
        /// block and no intervening statement can replace, mutate, or expose
        /// the sequence; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalSequenceInitializerStillCurrent(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            VariableDeclaratorSyntax variableDeclarator,
            SemanticModel semanticModel)
        {
            if (variableDeclarator.Parent?.Parent
                    is not LocalDeclarationStatementSyntax declarationStatement ||
                declarationStatement.Parent
                    is not BlockSyntax declarationBlock)
            {
                return false;
            }

            StatementSyntax? useStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (useStatement?.Parent
                    is not BlockSyntax useBlock ||
                useBlock.SyntaxTree !=
                    declarationBlock.SyntaxTree ||
                useBlock.Span !=
                    declarationBlock.Span ||
                useStatement.SpanStart <=
                    declarationStatement.SpanStart)
            {
                return false;
            }

            foreach (StatementSyntax statement
                     in useBlock.Statements)
            {
                if (statement.SpanStart <=
                        declarationStatement.SpanStart ||
                    statement.SpanStart >=
                        useStatement.SpanStart)
                {
                    continue;
                }

                ExceptionFlowDataFlowFacts dataFlow =
                    GetDataFlowFacts(statement, semanticModel);

                if (!dataFlow.Succeeded ||
                    dataFlow.WrittenInside.Any(
                        writtenSymbol =>
                            SymbolEqualityComparer.Default.Equals(
                                writtenSymbol,
                                localSymbol)) ||
                    !DoesStatementPreserveLocalSequenceContents(
                        statement,
                        localSymbol,
                        semanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether an intervening statement preserves the contents
        /// and ownership of a local sequence whose element facts are being
        /// reused.
        /// </summary>
        /// <param name="statement">
        /// The intervening statement to inspect.
        /// </param>
        /// <param name="localSymbol">
        /// The sequence local whose contents must remain unchanged.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the statement does not reference the
        /// sequence or only performs a supported read-only observation;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool DoesStatementPreserveLocalSequenceContents(
            StatementSyntax statement,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            IEnumerable<IdentifierNameSyntax> references =
                statement.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            ExpressionReferencesSymbol(
                                identifier,
                                localSymbol,
                                semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsSupportedReadOnlySequenceObservation(reference, semanticModel)
                    || IsSourceHelperArgumentProvenToPreserveSequenceContents(reference, semanticModel))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a local sequence reference is a supported
        /// read-only observation that cannot explicitly replace, mutate, or
        /// expose the sequence contents.
        /// </summary>
        /// <param name="reference">
        /// The local sequence reference to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for member resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference only reads a known
        /// framework collection count or an array length; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsSupportedReadOnlySequenceObservation(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel)
        {
            if (reference.Parent
                    is not MemberAccessExpressionSyntax memberAccess ||
                !ReferenceEquals(
                    memberAccess.Expression,
                    reference))
            {
                return false;
            }

            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess);

            if (memberSymbolInfo.Symbol
                    is not IPropertySymbol propertySymbol)
            {
                return false;
            }

            if (IsFrameworkCollectionCountProperty(
                    propertySymbol))
            {
                return true;
            }

            if (!string.Equals(
                    propertySymbol.Name,
                    "Length",
                    StringComparison.Ordinal) ||
                propertySymbol.GetMethod == null ||
                propertySymbol.SetMethod != null ||
                propertySymbol.Parameters.Length != 0)
            {
                return false;
            }

            TypeInfo receiverTypeInfo =
                semanticModel.GetTypeInfo(
                    reference);

            return receiverTypeInfo.Type
                is IArrayTypeSymbol;
        }

        /// <summary>
        /// Determines whether a property is the read-only <c>Count</c>
        /// property of a supported framework collection abstraction.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the property is a supported framework
        /// collection count observation; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsFrameworkCollectionCountProperty(
            IPropertySymbol propertySymbol)
        {
            if (!string.Equals(
                    propertySymbol.Name,
                    "Count",
                    StringComparison.Ordinal) ||
                propertySymbol.GetMethod == null ||
                propertySymbol.SetMethod != null ||
                propertySymbol.Parameters.Length != 0)
            {
                return false;
            }

            INamedTypeSymbol containingType =
                propertySymbol.ContainingType.OriginalDefinition;

            string namespaceName =
                containingType.ContainingNamespace
                    .ToDisplayString();

            if (string.Equals(
                    namespaceName,
                    "System.Collections.Generic",
                    StringComparison.Ordinal))
            {
                return string.Equals(
                           containingType.Name,
                           "IReadOnlyCollection",
                           StringComparison.Ordinal) ||
                       string.Equals(
                           containingType.Name,
                           "IReadOnlyList",
                           StringComparison.Ordinal) ||
                       string.Equals(
                           containingType.Name,
                           "ICollection",
                           StringComparison.Ordinal) ||
                       string.Equals(
                           containingType.Name,
                           "IList",
                           StringComparison.Ordinal) ||
                       string.Equals(
                           containingType.Name,
                           "List",
                           StringComparison.Ordinal);
            }

            return string.Equals(
                       namespaceName,
                       "System.Collections",
                       StringComparison.Ordinal) &&
                   string.Equals(
                       containingType.Name,
                       "ICollection",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the source sequence of a supported element-preserving LINQ
        /// invocation.
        /// </summary>
        /// <param name="invocation">
        /// The invocation expression.
        /// </param>
        /// <param name="methodSymbol">
        /// The method selected at the invocation site.
        /// </param>
        /// <param name="originalMethod">
        /// The original definition of the selected method.
        /// </param>
        /// <param name="sourceExpression">
        /// The resolved source sequence expression.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the invocation is a supported
        /// element-preserving sequence operation and its source was resolved;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetElementPreservingSequenceSource(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            IMethodSymbol originalMethod,
            out ExpressionSyntax? sourceExpression)
        {
            sourceExpression = null;

            if (!IsElementPreservingSequenceMethod(
                    originalMethod))
            {
                return false;
            }

            return TryGetSequenceSourceExpression(
                invocation,
                methodSymbol,
                out sourceExpression);
        }

        /// <summary>
        /// Determines whether a framework sequence operation only filters, reorders,
        /// or materializes its input elements and therefore preserves an existing
        /// non-null element guarantee.
        /// </summary>
        /// <param name="methodSymbol">
        /// The original framework method definition.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the method preserves the identity of its input
        /// elements; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsElementPreservingSequenceMethod(
            IMethodSymbol methodSymbol)
        {
            return string.Equals(
                       methodSymbol.Name,
                       "Where",
                       StringComparison.Ordinal) ||
                   string.Equals(
                       methodSymbol.Name,
                       "OrderBy",
                       StringComparison.Ordinal) ||
                   string.Equals(
                       methodSymbol.Name,
                       "ToArray",
                       StringComparison.Ordinal) ||
                   string.Equals(
                       methodSymbol.Name,
                       "ToList",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a dictionary <c>Values</c> expression is proven
        /// to contain no <see langword="null"/> elements because the local
        /// dictionary starts empty and every insertion supplies a value proven
        /// to be non-null.
        /// </summary>
        /// <param name="memberAccess">
        /// The possible dictionary <c>Values</c> access.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value analysis.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence and collection symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the values are proven to exclude
        /// <see langword="null"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            IsDictionaryValuesExpressionProvenToExcludeNullElements(
                MemberAccessExpressionSyntax memberAccess,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess);

            if (memberSymbolInfo.Symbol
                    is not IPropertySymbol propertySymbol ||
                !IsDictionaryValuesProperty(
                    propertySymbol))
            {
                return false;
            }

            ExpressionSyntax dictionaryExpression =
                UnwrapParenthesizedExpression(
                    memberAccess.Expression);

            SymbolInfo dictionarySymbolInfo =
                semanticModel.GetSymbolInfo(
                    dictionaryExpression);

            if (dictionarySymbolInfo.Symbol
                is not ILocalSymbol dictionaryLocal)
            {
                return false;
            }

            return IsLocalDictionaryProvenToExcludeNullValues(
                dictionaryLocal,
                memberAccess,
                semanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a property is the framework dictionary
        /// <c>Values</c> property.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for the framework dictionary values
        /// property; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryValuesProperty(
            IPropertySymbol propertySymbol)
        {
            return string.Equals(
                       propertySymbol.Name,
                       "Values",
                       StringComparison.Ordinal) &&
                   propertySymbol.Parameters.Length == 0 &&
                   IsDictionaryType(
                       propertySymbol.ContainingType);
        }

        /// <summary>
        /// Determines whether a local dictionary is proven to exclude null
        /// values when its <c>Values</c> collection is consumed.
        /// </summary>
        /// <param name="dictionaryLocal">
        /// The dictionary local to inspect.
        /// </param>
        /// <param name="valuesAccess">
        /// The <c>Values</c> access whose dictionary is being analyzed.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the access.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence and collection symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the local starts empty, does not escape
        /// through an unsupported use, and every insertion is proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalDictionaryProvenToExcludeNullValues(
            ILocalSymbol dictionaryLocal,
            MemberAccessExpressionSyntax valuesAccess,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            if (!IsDictionaryType(
                    dictionaryLocal.Type) ||
                dictionaryLocal.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedSequenceSources.Add(
                    dictionaryLocal))
            {
                return false;
            }

            try
            {
                SyntaxNode declarationNode =
                    dictionaryLocal.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                if (declarationNode
                        is not VariableDeclaratorSyntax variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    return false;
                }

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null ||
                    !IsKnownEmptyDictionaryCreation(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel))
                {
                    return false;
                }

                SyntaxNode? containingCallable =
                    variableDeclarator.Ancestors()
                        .FirstOrDefault(
                            static node =>
                                node is MethodDeclarationSyntax ||
                                node is LocalFunctionStatementSyntax);

                if (containingCallable == null ||
                    containingCallable.SyntaxTree !=
                        valuesAccess.SyntaxTree)
                {
                    return false;
                }

                IEnumerable<IdentifierNameSyntax> references =
                    containingCallable.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(
                            identifier =>
                                identifier.SpanStart >
                                    variableDeclarator.Span.End &&
                                ExpressionReferencesSymbol(
                                    identifier,
                                    dictionaryLocal,
                                    declarationSemanticModel));

                foreach (IdentifierNameSyntax reference
                         in references)
                {
                    if (!IsDictionaryReferenceSafeForNonNullValues(
                            reference,
                            declarationSemanticModel,
                            inspectedSequenceSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedSequenceSources.Remove(
                    dictionaryLocal);
            }
        }

        /// <summary>
        /// Determines whether an expression creates an empty framework
        /// dictionary without a collection initializer or source collection.
        /// </summary>
        /// <param name="creationExpression">
        /// The dictionary creation expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constructor resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the selected constructor cannot seed
        /// dictionary entries; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsKnownEmptyDictionaryCreation(
            ExpressionSyntax creationExpression,
            SemanticModel semanticModel)
        {
            if (creationExpression
                    is not ObjectCreationExpressionSyntax &&
                creationExpression
                    is not ImplicitObjectCreationExpressionSyntax)
            {
                return false;
            }

            InitializerExpressionSyntax? initializer =
                creationExpression switch
                {
                    ObjectCreationExpressionSyntax objectCreation =>
                        objectCreation.Initializer,
                    ImplicitObjectCreationExpressionSyntax implicitCreation =>
                        implicitCreation.Initializer,
                    _ => null
                };

            if (initializer != null &&
                initializer.Expressions.Count != 0)
            {
                return false;
            }

            SymbolInfo constructorSymbolInfo =
                semanticModel.GetSymbolInfo(
                    creationExpression);

            if (constructorSymbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol ||
                constructorSymbol.MethodKind !=
                    MethodKind.Constructor ||
                !IsDictionaryType(
                    constructorSymbol.ContainingType))
            {
                return false;
            }

            foreach (IParameterSymbol parameter
                     in constructorSymbol.Parameters)
            {
                if (parameter.Type.SpecialType ==
                    SpecialType.System_Int32 ||
                    IsEqualityComparerType(
                        parameter.Type))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a type is the framework generic dictionary type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for the framework generic dictionary type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryType(
            ITypeSymbol typeSymbol)
        {
            if (typeSymbol
                is not INamedTypeSymbol namedType)
            {
                return false;
            }

            INamedTypeSymbol originalType =
                namedType.OriginalDefinition;

            return string.Equals(
                       originalType.Name,
                       "Dictionary",
                       StringComparison.Ordinal) &&
                   originalType.Arity == 2 &&
                   string.Equals(
                       originalType.ContainingNamespace
                           .ToDisplayString(),
                       "System.Collections.Generic",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a type is the framework generic equality
        /// comparer interface.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for the framework generic equality comparer;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsEqualityComparerType(
            ITypeSymbol typeSymbol)
        {
            if (typeSymbol
                is not INamedTypeSymbol namedType)
            {
                return false;
            }

            INamedTypeSymbol originalType =
                namedType.OriginalDefinition;

            return string.Equals(
                       originalType.Name,
                       "IEqualityComparer",
                       StringComparison.Ordinal) &&
                   originalType.Arity == 1 &&
                   string.Equals(
                       originalType.ContainingNamespace
                           .ToDisplayString(),
                       "System.Collections.Generic",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether one dictionary reference preserves the invariant
        /// that no null value is inserted or escapes through an unsupported
        /// mutation path.
        /// </summary>
        /// <param name="reference">
        /// The dictionary local or parameter reference.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the reference.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The collection symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is a safe values read, a
        /// proven non-null insertion, or a supported source-helper argument;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryReferenceSafeForNonNullValues(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSequenceSources)
        {
            if (reference.Parent is CommonForEachStatementSyntax foreachStatement
                && ReferenceEquals(foreachStatement.Expression, reference))
            {
                return true;
            }

            if (reference.Parent is MemberAccessExpressionSyntax memberAccess
                && ReferenceEquals(memberAccess.Expression, reference))
            {
                SymbolInfo memberSymbolInfo =
                    semanticModel.GetSymbolInfo(
                        memberAccess);

                if (memberSymbolInfo.Symbol
                        is IPropertySymbol propertySymbol &&
                    IsDictionaryValuesProperty(
                        propertySymbol))
                {
                    return true;
                }

                if (memberAccess.Parent
                        is InvocationExpressionSyntax invocation &&
                    ReferenceEquals(
                        invocation.Expression,
                        memberAccess))
                {
                    return IsDictionaryMemberInvocationSafeForNonNullValues(
                        invocation,
                        semanticModel);
                }

                return false;
            }

            if (reference.Parent
                    is ArgumentSyntax argument &&
                ReferenceEquals(
                    argument.Expression,
                    reference) &&
                argument.Parent?.Parent
                    is InvocationExpressionSyntax helperInvocation)
            {
                return IsDictionarySourceHelperArgumentSafeForNonNullValues(
                    argument,
                    helperInvocation,
                    semanticModel,
                    inspectedSequenceSources);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a direct framework dictionary invocation
        /// preserves the non-null value invariant.
        /// </summary>
        /// <param name="invocation">
        /// The dictionary invocation to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method and value analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the invocation removes entries or
        /// inserts a value proven to be non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsDictionaryMemberInvocationSafeForNonNullValues(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol
                || !IsDictionaryType(methodSymbol.ContainingType))
            {
                return false;
            }

            if (string.Equals(
                    methodSymbol.Name,
                    "Clear",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "Remove",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "TryGetValue",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "ContainsKey",
                    StringComparison.Ordinal)
                || string.Equals(
                    methodSymbol.Name,
                    "ContainsValue",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(
                    methodSymbol.Name,
                    "Add",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    methodSymbol.Name,
                    "TryAdd",
                    StringComparison.Ordinal))
            {
                return false;
            }

            for (int argumentIndex = 0;
                 argumentIndex <
                    invocation.ArgumentList.Arguments.Count;
                 argumentIndex++)
            {
                ArgumentSyntax argument =
                    invocation.ArgumentList.Arguments[argumentIndex];

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        argumentIndex,
                        methodSymbol);

                if (parameterIndex != 1)
                {
                    continue;
                }

                return IsDictionaryInsertionValueProvenNonNull(
                    argument.Expression,
                    semanticModel);
            }

            return false;
        }

        /// <summary>
        /// Determines whether passing a dictionary to one statically bound
        /// source helper preserves the absence of null values.
        /// </summary>
        /// <param name="argument">
        /// The dictionary argument.
        /// </param>
        /// <param name="invocation">
        /// The helper invocation containing the argument.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The collection symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the helper has a single analyzable
        /// source declaration and every use of the corresponding dictionary
        /// parameter preserves the invariant; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
            IsDictionarySourceHelperArgumentSafeForNonNullValues(
                ArgumentSyntax argument,
                InvocationExpressionSyntax invocation,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            if (!argument.RefKindKeyword.IsKind(
                    SyntaxKind.None))
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.ReducedFrom != null ||
                !selectedMethod.IsStatic ||
                selectedMethod.IsAbstract ||
                selectedMethod.IsExtern ||
                RequiresSummaryRuntimeDispatch(
                    selectedMethod) ||
                selectedMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            int fallbackIndex =
                invocation.ArgumentList.Arguments.IndexOf(
                    argument);

            if (fallbackIndex < 0)
            {
                return false;
            }

            int parameterIndex =
                GetParameterIndexForArgument(
                    argument,
                    fallbackIndex,
                    selectedMethod);

            if (parameterIndex < 0 ||
                parameterIndex >=
                    selectedMethod.Parameters.Length)
            {
                return false;
            }

            IParameterSymbol parameterSymbol =
                selectedMethod.Parameters[parameterIndex];

            if (parameterSymbol.RefKind !=
                    RefKind.None ||
                !IsDictionaryType(
                    parameterSymbol.Type))
            {
                return false;
            }

            return DoesSourceDictionaryParameterPreserveNonNullValues(
                parameterSymbol,
                semanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether all uses of one source-helper dictionary
        /// parameter preserve the invariant that existing non-null values stay
        /// free of null insertions.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The dictionary parameter to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// A semantic model from the caller compilation.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The collection symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every parameter use is a supported safe
        /// operation; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            DoesSourceDictionaryParameterPreserveNonNullValues(
                IParameterSymbol parameterSymbol,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            if (parameterSymbol.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedSequenceSources.Add(
                    parameterSymbol))
            {
                return false;
            }

            try
            {
                SyntaxNode parameterDeclaration =
                    parameterSymbol.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                SyntaxNode? containingCallable =
                    parameterDeclaration.Ancestors()
                        .FirstOrDefault(
                            static node =>
                                node is MethodDeclarationSyntax ||
                                node is LocalFunctionStatementSyntax);

                if (containingCallable == null)
                {
                    return false;
                }

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        containingCallable.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    return false;
                }

                IEnumerable<IdentifierNameSyntax> references =
                    containingCallable.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(
                            identifier =>
                                ExpressionReferencesSymbol(
                                    identifier,
                                    parameterSymbol,
                                    declarationSemanticModel));

                foreach (IdentifierNameSyntax reference
                         in references)
                {
                    if (!IsDictionaryReferenceSafeForNonNullValues(
                            reference,
                            declarationSemanticModel,
                            inspectedSequenceSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedSequenceSources.Remove(
                    parameterSymbol);
            }
        }
    }
}
