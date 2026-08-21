using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning for enum values and sequences whose values
    /// are restricted to explicitly declared enum constants.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets facts proving that an expression contains a declared value of
        /// its enum type.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type, symbol, and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.DefinedEnumValue"/> when the
        /// expression is proven to equal a declared enum constant; otherwise
        /// <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts GetDefinedEnumValueFacts(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (!TryGetEnumType(
                    unwrappedExpression,
                    semanticModel,
                    out INamedTypeSymbol? enumType)
                || enumType == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            Optional<object?> constantValue =
                semanticModel.GetConstantValue(unwrappedExpression);

            if (constantValue.HasValue
                && IsDeclaredEnumConstantValue(
                    enumType,
                    constantValue.Value))
            {
                return ExceptionFlowValueFacts.DefinedEnumValue;
            }

            if (unwrappedExpression is ElementAccessExpressionSyntax elementAccess
                && IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                    elementAccess.Expression,
                    semanticModel,
                    callContext,
                    inspectedValueSources))
            {
                return ExceptionFlowValueFacts.DefinedEnumValue;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                return callContext
                    .GetParameterFacts(parameterSymbol)
                    .ContainsAll(ExceptionFlowValueFacts.DefinedEnumValue)
                        ? ExceptionFlowValueFacts.DefinedEnumValue
                        : ExceptionFlowValueFacts.None;
            }

            if (symbolInfo.Symbol is not ILocalSymbol localSymbol
                || !inspectedValueSources.Add(localSymbol))
            {
                return ExceptionFlowValueFacts.None;
            }

            try
            {
                if (IsForeachIterationVariableProvenDefinedEnumValue(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel,
                        callContext,
                        inspectedValueSources))
                {
                    return ExceptionFlowValueFacts.DefinedEnumValue;
                }

                if (!TryGetCurrentLocalInitializerExpression(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel,
                        out ExpressionSyntax? initializer)
                    || initializer == null)
                {
                    return ExceptionFlowValueFacts.None;
                }

                return GetDefinedEnumValueFacts(
                    initializer,
                    semanticModel,
                    callContext,
                    inspectedValueSources);
            }
            finally
            {
                inspectedValueSources.Remove(localSymbol);
            }
        }

        /// <summary>
        /// Determines whether a foreach iteration variable is restricted to
        /// declared values of its enum type.
        /// </summary>
        /// <param name="expression">
        /// The iteration-variable use being inspected.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol represented by the expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for foreach and symbol analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every possible element of the foreach
        /// source is a declared enum value; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsForeachIterationVariableProvenDefinedEnumValue(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            foreach (ForEachStatementSyntax forEachStatement
                     in expression.Ancestors()
                         .OfType<ForEachStatementSyntax>())
            {
                ISymbol? iterationVariable =
                    semanticModel.GetDeclaredSymbol(forEachStatement);

                if (!SymbolEqualityComparer.Default.Equals(
                        iterationVariable,
                        localSymbol))
                {
                    continue;
                }

                return IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                    forEachStatement.Expression,
                    semanticModel,
                    callContext,
                    inspectedValueSources);
            }

            return false;
        }

        /// <summary>
        /// Determines whether every element produced by a sequence expression
        /// is a declared value of its enum element type.
        /// </summary>
        /// <param name="expression">
        /// The sequence expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and return-value analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The sequence-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when all produced elements are proven
        /// declared enum values; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (!TryGetSequenceEnumElementType(
                    unwrappedExpression,
                    semanticModel,
                    out _))
            {
                return false;
            }

            if (unwrappedExpression is ConditionalExpressionSyntax conditionalExpression)
            {
                return IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                           conditionalExpression.WhenTrue,
                           semanticModel,
                           callContext,
                           inspectedValueSources)
                    && IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                        conditionalExpression.WhenFalse,
                        semanticModel,
                        callContext,
                        inspectedValueSources);
            }

            if (TryGetArrayInitializer(
                    unwrappedExpression,
                    out InitializerExpressionSyntax? initializer)
                && initializer != null)
            {
                foreach (ExpressionSyntax element in initializer.Expressions)
                {
                    ExceptionFlowValueFacts elementFacts =
                        GetDefinedEnumValueFacts(
                            element,
                            semanticModel,
                            callContext,
                            inspectedValueSources);

                    if (!elementFacts.ContainsAll(
                            ExceptionFlowValueFacts.DefinedEnumValue))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (unwrappedExpression is InvocationExpressionSyntax invocation)
            {
                return TryProveSourceInvocationDefinedEnumElements(
                    invocation,
                    semanticModel,
                    callContext,
                    inspectedValueSources);
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                return callContext
                    .GetParameterFacts(parameterSymbol)
                    .ContainsAll(ExceptionFlowValueFacts.DefinedEnumElements);
            }

            if (symbolInfo.Symbol is not ILocalSymbol localSymbol
                || !inspectedValueSources.Add(localSymbol))
            {
                return false;
            }

            try
            {
                if (IsListType(localSymbol.Type))
                {
                    return IsLocalListProvenToContainOnlyDefinedEnumValues(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel,
                        callContext,
                        inspectedValueSources);
                }

                if (!TryGetCurrentLocalInitializerExpression(
                        unwrappedExpression,
                        localSymbol,
                        semanticModel,
                        out ExpressionSyntax? localInitializer)
                    || localInitializer == null)
                {
                    return false;
                }

                return IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                    localInitializer,
                    semanticModel,
                    callContext,
                    inspectedValueSources);
            }
            finally
            {
                inspectedValueSources.Remove(localSymbol);
            }
        }

        /// <summary>
        /// Determines whether a source method invocation returns only declared
        /// enum values on every explicit return path.
        /// </summary>
        /// <param name="invocation">
        /// The source invocation to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <param name="callerContext">
        /// The call-site facts known for the caller.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every supported return expression
        /// produces only declared enum values; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryProveSourceInvocationDefinedEnumElements(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is not IMethodSymbol selectedMethod
                || selectedMethod.ReducedFrom != null
                || selectedMethod.ReturnsVoid
                || selectedMethod.IsAsync
                || selectedMethod.IsExtern
                || selectedMethod.IsAbstract
                || selectedMethod.IsIterator
                || selectedMethod.ReturnsByRef
                || selectedMethod.ReturnsByRefReadonly
                || RequiresSummaryRuntimeDispatch(selectedMethod))
            {
                return false;
            }

            IMethodSymbol targetMethod =
                selectedMethod.OriginalDefinition;

            if (targetMethod.DeclaringSyntaxReferences.Length != 1
                || !inspectedValueSources.Add(targetMethod))
            {
                return false;
            }

            try
            {
                SyntaxNode declaration =
                    targetMethod.DeclaringSyntaxReferences[0].GetSyntax();

                List<ExpressionSyntax> returnExpressions =
                    GetSourceReturnExpressions(declaration);

                if (returnExpressions.Count == 0)
                {
                    return false;
                }

                ExceptionFlowCallContext calleeContext =
                    CreateCallContext(
                        selectedMethod,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callerContext);

                foreach (ExpressionSyntax returnExpression in returnExpressions)
                {
                    SemanticModel? returnSemanticModel =
                        GetSemanticModelForSyntaxTree(
                            semanticModel,
                            returnExpression.SyntaxTree);

                    if (returnSemanticModel == null
                        || !IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                            returnExpression,
                            returnSemanticModel,
                            calleeContext,
                            inspectedValueSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedValueSources.Remove(targetMethod);
            }
        }

        /// <summary>
        /// Gets the explicit return expressions of a supported source method or
        /// local function while excluding nested callables.
        /// </summary>
        /// <param name="declaration">
        /// The callable declaration to inspect.
        /// </param>
        /// <returns>
        /// The return expressions represented by the declaration.
        /// </returns>
        private static List<ExpressionSyntax> GetSourceReturnExpressions(
            SyntaxNode declaration)
        {
            ArrowExpressionClauseSyntax? expressionBody;
            BlockSyntax? body;

            switch (declaration)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    expressionBody = methodDeclaration.ExpressionBody;
                    body = methodDeclaration.Body;
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    expressionBody = localFunction.ExpressionBody;
                    body = localFunction.Body;
                    break;

                default:
                    return new List<ExpressionSyntax>();
            }

            if (expressionBody != null)
            {
                return new List<ExpressionSyntax>
                {
                    expressionBody.Expression
                };
            }

            if (body == null)
            {
                return new List<ExpressionSyntax>();
            }

            return body.DescendantNodesAndSelf(
                    static node =>
                        node is not AnonymousFunctionExpressionSyntax
                        && node is not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>()
                .Where(static statement => statement.Expression != null)
                .Select(static statement => statement.Expression!)
                .ToList();
        }

        /// <summary>
        /// Determines whether a mutable local list starts empty and receives
        /// only declared enum values before the inspected use.
        /// </summary>
        /// <param name="useExpression">
        /// The current use of the local list.
        /// </param>
        /// <param name="localSymbol">
        /// The local list symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for reference and invocation analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every element added before the use is a
        /// declared enum value; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalListProvenToContainOnlyDefinedEnumValues(
            ExpressionSyntax useExpression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            if (localSymbol.DeclaringSyntaxReferences.Length != 1
                || localSymbol.DeclaringSyntaxReferences[0].GetSyntax()
                    is not VariableDeclaratorSyntax variableDeclarator
                || variableDeclarator.Initializer == null
                || !IsKnownEmptyListCreation(
                    variableDeclarator.Initializer.Value,
                    semanticModel))
            {
                return false;
            }

            SyntaxNode? containingCallable =
                variableDeclarator.Ancestors()
                    .FirstOrDefault(
                        static node =>
                            node is MethodDeclarationSyntax
                            || node is LocalFunctionStatementSyntax);

            if (containingCallable == null
                || containingCallable.SyntaxTree != useExpression.SyntaxTree)
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> references =
                containingCallable.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            identifier.SpanStart > variableDeclarator.Span.End
                            && identifier.SpanStart < useExpression.SpanStart
                            && ExpressionReferencesSymbol(
                                identifier,
                                localSymbol,
                                semanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsSupportedReadOnlySequenceObservation(
                        reference,
                        semanticModel))
                {
                    continue;
                }

                if (!IsListAddOfDefinedEnumValue(
                        reference,
                        semanticModel,
                        callContext,
                        inspectedValueSources))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a local list reference represents an
        /// <c>Add</c> call whose supplied element is a declared enum value.
        /// </summary>
        /// <param name="reference">
        /// The local list reference.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method and argument analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is a supported safe
        /// <c>Add</c> call; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsListAddOfDefinedEnumValue(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            if (reference.Parent is not MemberAccessExpressionSyntax memberAccess
                || !ReferenceEquals(memberAccess.Expression, reference)
                || memberAccess.Parent is not InvocationExpressionSyntax invocation
                || !ReferenceEquals(invocation.Expression, memberAccess)
                || invocation.ArgumentList.Arguments.Count != 1
                || semanticModel.GetSymbolInfo(invocation).Symbol
                    is not IMethodSymbol methodSymbol
                || !string.Equals(
                    methodSymbol.Name,
                    "Add",
                    StringComparison.Ordinal)
                || !IsListType(methodSymbol.ContainingType))
            {
                return false;
            }

            ArgumentSyntax argument =
                invocation.ArgumentList.Arguments[0];

            ExceptionFlowValueFacts elementFacts =
                GetDefinedEnumValueFacts(
                    argument.Expression,
                    semanticModel,
                    callContext,
                    inspectedValueSources);

            return elementFacts.ContainsAll(
                ExceptionFlowValueFacts.DefinedEnumValue);
        }

        /// <summary>
        /// Determines whether an expression is an array creation whose
        /// initializer can be inspected.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="initializer">
        /// The array initializer when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression is a supported array
        /// creation with an initializer; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetArrayInitializer(
            ExpressionSyntax expression,
            out InitializerExpressionSyntax? initializer)
        {
            initializer = expression switch
            {
                ArrayCreationExpressionSyntax arrayCreation =>
                    arrayCreation.Initializer,

                ImplicitArrayCreationExpressionSyntax implicitArray =>
                    implicitArray.Initializer,

                _ => null
            };

            return initializer != null;
        }

        /// <summary>
        /// Determines whether an expression has an enum type.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <param name="enumType">
        /// The resolved enum type when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the effective expression type is an
        /// enum; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetEnumType(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out INamedTypeSymbol? enumType)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(expression);

            ITypeSymbol? effectiveType =
                typeInfo.ConvertedType ?? typeInfo.Type;

            enumType =
                effectiveType as INamedTypeSymbol;

            return enumType?.TypeKind == TypeKind.Enum;
        }

        /// <summary>
        /// Determines whether an expression produces a sequence whose element
        /// type is an enum.
        /// </summary>
        /// <param name="expression">
        /// The sequence expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <param name="enumType">
        /// The enum element type when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression is an array or generic
        /// enumerable of enum values; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetSequenceEnumElementType(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out INamedTypeSymbol? enumType)
        {
            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(expression);

            ITypeSymbol? effectiveType =
                typeInfo.ConvertedType ?? typeInfo.Type;

            return TryGetSequenceEnumElementType(
                effectiveType,
                out enumType);
        }

        /// <summary>
        /// Determines whether a type represents a sequence whose element type
        /// is an enum.
        /// </summary>
        /// <param name="typeSymbol">
        /// The sequence type to inspect.
        /// </param>
        /// <param name="enumType">
        /// The enum element type when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type is an array or implements
        /// <see cref="IEnumerable{T}"/> for an enum element type; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetSequenceEnumElementType(
            ITypeSymbol? typeSymbol,
            out INamedTypeSymbol? enumType)
        {
            enumType = null;

            if (typeSymbol is IArrayTypeSymbol arrayType
                && arrayType.ElementType is INamedTypeSymbol arrayElementType
                && arrayElementType.TypeKind == TypeKind.Enum)
            {
                enumType = arrayElementType;
                return true;
            }

            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            IEnumerable<INamedTypeSymbol> candidates =
                namedType.AllInterfaces.Prepend(namedType);

            foreach (INamedTypeSymbol candidate in candidates)
            {
                INamedTypeSymbol originalType =
                    candidate.OriginalDefinition;

                if (originalType.Arity != 1
                    || !string.Equals(
                        originalType.Name,
                        "IEnumerable",
                        StringComparison.Ordinal)
                    || !string.Equals(
                        originalType.ContainingNamespace.ToDisplayString(),
                        "System.Collections.Generic",
                        StringComparison.Ordinal)
                    || candidate.TypeArguments[0]
                        is not INamedTypeSymbol elementType
                    || elementType.TypeKind != TypeKind.Enum)
                {
                    continue;
                }

                enumType = elementType;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a constant value equals one of the explicitly
        /// declared members of an enum type.
        /// </summary>
        /// <param name="enumType">
        /// The enum type whose declared values should be inspected.
        /// </param>
        /// <param name="constantValue">
        /// The constant value to compare.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a declared enum member has the supplied
        /// constant value; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDeclaredEnumConstantValue(
            INamedTypeSymbol enumType,
            object? constantValue)
        {
            foreach (IFieldSymbol fieldSymbol
                     in enumType.GetMembers()
                         .OfType<IFieldSymbol>())
            {
                if (!fieldSymbol.HasConstantValue)
                {
                    continue;
                }

                if (Equals(
                        fieldSymbol.ConstantValue,
                        constantValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether every element of a sequence expression is proven
        /// to be a declared enum value.
        /// </summary>
        /// <param name="expression">
        /// The sequence expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for sequence analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every possible sequence element is a
        /// declared enum value; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreSequenceElementsProvenDefinedEnumValues(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            HashSet<ISymbol> inspectedValueSources =
                new(SymbolEqualityComparer.Default);

            return IsSequenceExpressionProvenToContainOnlyDefinedEnumValues(
                expression,
                semanticModel,
                callContext,
                inspectedValueSources);
        }
    }
}
