using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains non-null reasoning used during exception-flow analysis.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether an expression is proven to evaluate to a
        /// non-null value without relying only on nullable reference-type
        /// annotations.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDefinitelyNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            HashSet<ISymbol> inspectedReturnSymbols =
                new(SymbolEqualityComparer.Default);

            return IsDefinitelyNonNull(
                expression,
                semanticModel,
                callContext,
                inspectedReturnSymbols);
        }

        /// <summary>
        /// Determines whether an expression is proven to evaluate to a
        /// non-null value while preventing recursive return-value analysis.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being
        /// inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the expression is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDefinitelyNonNull(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            Optional<object?> constantValue =
                semanticModel.GetConstantValue(expression);

            if (constantValue.HasValue &&
                constantValue.Value != null)
            {
                return true;
            }

            TypeInfo typeInfo =
                semanticModel.GetTypeInfo(expression);

            ITypeSymbol? expressionType =
                typeInfo.ConvertedType ?? typeInfo.Type;

            if (expressionType != null &&
                expressionType.IsValueType &&
                !IsNullableValueType(expressionType))
            {
                return true;
            }

            switch (expression)
            {
                case ParenthesizedExpressionSyntax
                    parenthesizedExpression:
                    return IsDefinitelyNonNull(
                        parenthesizedExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case CastExpressionSyntax castExpression:
                    return IsDefinitelyNonNull(
                        castExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case CheckedExpressionSyntax checkedExpression:
                    return IsDefinitelyNonNull(
                        checkedExpression.Expression,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case ObjectCreationExpressionSyntax:
                case ImplicitObjectCreationExpressionSyntax:
                case AnonymousObjectCreationExpressionSyntax:
                case ArrayCreationExpressionSyntax:
                case ImplicitArrayCreationExpressionSyntax:
                case StackAllocArrayCreationExpressionSyntax:
                case ThisExpressionSyntax:
                case BaseExpressionSyntax:
                case TypeOfExpressionSyntax:
                case InterpolatedStringExpressionSyntax:
                case AnonymousFunctionExpressionSyntax:
                    return true;

                case ConditionalExpressionSyntax conditionalExpression:
                    return IsDefinitelyNonNull(
                               conditionalExpression.WhenTrue,
                               semanticModel,
                               callContext,
                               inspectedReturnSymbols) &&
                           IsDefinitelyNonNull(
                               conditionalExpression.WhenFalse,
                               semanticModel,
                               callContext,
                               inspectedReturnSymbols);

                case BinaryExpressionSyntax binaryExpression
                    when binaryExpression.IsKind(
                        SyntaxKind.CoalesceExpression):
                    return IsDefinitelyNonNull(
                        binaryExpression.Right,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);

                case InvocationExpressionSyntax invocation:
                    return IsInvocationResultDefinitelyNonNull(
                        invocation,
                        semanticModel,
                        callContext,
                        inspectedReturnSymbols);
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(expression);

            if (symbolInfo.Symbol is IParameterSymbol parameterSymbol)
            {
                if (callContext.IsParameterKnownNonNull(
                        parameterSymbol))
                {
                    return true;
                }

                ExceptionFlowValueFacts guardFacts =
                    GetFactsProvenByPrecedingGuard(
                        expression,
                        parameterSymbol,
                        semanticModel);

                if (guardFacts.ContainsAll(
                        ExceptionFlowValueFacts.NonNull))
                {
                    return true;
                }

                ExceptionFlowValueFacts dereferenceFacts =
                    GetFactsProvenByPrecedingSuccessfulDereference(
                        expression,
                        parameterSymbol,
                        semanticModel);

                return dereferenceFacts.ContainsAll(
                    ExceptionFlowValueFacts.NonNull);
            }

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                return IsLocalGuaranteedNonNull(
                    expression,
                    localSymbol,
                    semanticModel,
                    callContext,
                    inspectedReturnSymbols);
            }

            if (symbolInfo.Symbol is IFieldSymbol fieldSymbol &&
                fieldSymbol.IsStatic &&
                fieldSymbol.Name == nameof(string.Empty) &&
                fieldSymbol.ContainingType.SpecialType ==
                SpecialType.System_String)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified type is a nullable value type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the type is a nullable value type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNullableValueType(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol
                       is INamedTypeSymbol namedType &&
                   namedType.OriginalDefinition.SpecialType ==
                   SpecialType.System_Nullable_T;
        }

        /// <summary>
        /// Determines whether a local variable is guaranteed to contain a
        /// non-null value because it was introduced by a non-null pattern,
        /// initialized with a value proven to be non-null, obtained from a
        /// type-filtered sequence, or protected by an earlier terminating
        /// null guard.
        /// </summary>
        /// <param name="expression">
        /// The local-variable expression being evaluated.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for expression analysis.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being
        /// inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local variable is proven to be
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalGuaranteedNonNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
            if (IsPatternLocalGuaranteedNonNull(
                    localSymbol,
                    semanticModel))
            {
                return true;
            }

            if (IsForeachIterationVariableProvenNonNullByCallContext(
                    expression,
                    localSymbol,
                    semanticModel,
                    callContext))
            {
                return true;
            }

            if (IsForeachIterationVariableProvenNonNull(
                    expression,
                    localSymbol,
                    semanticModel))
            {
                return true;
            }

            if (IsLocalProvenNonNullByPrecedingGuard(
                    expression,
                    localSymbol,
                    semanticModel))
            {
                return true;
            }

            foreach (SyntaxReference syntaxReference
                     in localSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode =
                    syntaxReference.GetSyntax();

                if (IsForeachLocalProvenNonNull(
                        declarationNode,
                        semanticModel))
                {
                    return true;
                }

                if (declarationNode
                        is not VariableDeclaratorSyntax
                            variableDeclarator ||
                    variableDeclarator.Initializer == null)
                {
                    continue;
                }

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        variableDeclarator.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    continue;
                }

                if (IsLocalInitializerStillCurrent(
                        expression,
                        localSymbol,
                        variableDeclarator,
                        declarationSemanticModel) &&
                    IsDefinitelyNonNull(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel,
                        callContext,
                        inspectedReturnSymbols))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a local variable is introduced by a pattern
        /// that guarantees a non-null value whenever the local is definitely
        /// assigned.
        /// </summary>
        /// <param name="localSymbol">
        /// The local symbol to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the declaring designation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local is declared by a pattern that
        /// excludes <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsPatternLocalGuaranteedNonNull(
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            foreach (SyntaxReference syntaxReference
                     in localSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode =
                    syntaxReference.GetSyntax();

                SemanticModel? declarationSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        declarationNode.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    continue;
                }

                IEnumerable<SingleVariableDesignationSyntax> designations =
                    declarationNode
                        .DescendantNodesAndSelf()
                        .OfType<SingleVariableDesignationSyntax>();

                foreach (SingleVariableDesignationSyntax designation
                         in designations)
                {
                    ISymbol? declaredSymbol =
                        declarationSemanticModel.GetDeclaredSymbol(
                            designation);

                    if (!SymbolEqualityComparer.Default.Equals(
                            declaredSymbol,
                            localSymbol))
                    {
                        continue;
                    }

                    PatternSyntax? declaringPattern =
                        designation.Ancestors()
                            .OfType<PatternSyntax>()
                            .FirstOrDefault();

                    return declaringPattern
                        is DeclarationPatternSyntax or
                            RecursivePatternSyntax or
                            ListPatternSyntax;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a local variable usage refers to the iteration
        /// variable of an enclosing foreach statement whose source sequence
        /// is proven to exclude <see langword="null"/> elements.
        /// </summary>
        /// <param name="expression">
        /// The local-variable usage being evaluated.
        /// </param>
        /// <param name="localSymbol">
        /// The local symbol represented by the expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and sequence analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local is the iteration variable of
        /// a sequence proven to exclude <see langword="null"/> elements;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsForeachIterationVariableProvenNonNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
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

                HashSet<ISymbol> inspectedSequenceSources =
                    new(SymbolEqualityComparer.Default);

                return IsSequenceExpressionProvenToExcludeNullElements(
                    foreachStatement.Expression,
                    semanticModel,
                    inspectedSequenceSources);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a local declared by a foreach statement is
        /// proven non-null because the enumerated sequence filters its
        /// elements by type.
        /// </summary>
        /// <param name="declarationNode">
        /// The syntax node declaring the local symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for sequence analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the foreach source is proven to exclude
        /// <see langword="null"/> elements; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsForeachLocalProvenNonNull(
            SyntaxNode declarationNode,
            SemanticModel semanticModel)
        {
            ForEachStatementSyntax? foreachStatement =
                declarationNode as ForEachStatementSyntax ??
                declarationNode.AncestorsAndSelf()
                    .OfType<ForEachStatementSyntax>()
                    .FirstOrDefault();

            if (foreachStatement == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    foreachStatement.SyntaxTree);

            if (declarationSemanticModel == null)
            {
                return false;
            }

            HashSet<ISymbol> inspectedSequenceSources =
                new(SymbolEqualityComparer.Default);

            return IsSequenceExpressionProvenToExcludeNullElements(
                foreachStatement.Expression,
                declarationSemanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a sequence expression is proven to exclude
        /// <see langword="null"/> elements.
        /// </summary>
        /// <param name="expression">
        /// The sequence expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence-producing methods, locals, and collection symbols
        /// currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every element produced by the expression
        /// is proven to exclude <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
            IsSequenceExpressionProvenToExcludeNullElements(
                ExpressionSyntax expression,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression
                is CastExpressionSyntax castExpression)
            {
                return IsSequenceExpressionProvenToExcludeNullElements(
                    castExpression.Expression,
                    semanticModel,
                    inspectedSequenceSources);
            }

            if (unwrappedExpression
                is CheckedExpressionSyntax checkedExpression)
            {
                return IsSequenceExpressionProvenToExcludeNullElements(
                    checkedExpression.Expression,
                    semanticModel,
                    inspectedSequenceSources);
            }

            SymbolInfo expressionSymbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (expressionSymbolInfo.Symbol
                    is ILocalSymbol localSymbol &&
                IsLocalSequenceExpressionProvenToExcludeNullElements(
                    unwrappedExpression,
                    localSymbol,
                    semanticModel,
                    inspectedSequenceSources))
            {
                return true;
            }

            if (unwrappedExpression
                    is MemberAccessExpressionSyntax memberAccess &&
                IsDictionaryValuesExpressionProvenToExcludeNullElements(
                    memberAccess,
                    semanticModel,
                    inspectedSequenceSources))
            {
                return true;
            }

            if (unwrappedExpression
                is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol
                is not IMethodSymbol methodSymbol)
            {
                return false;
            }

            IMethodSymbol originalMethod =
                methodSymbol.ReducedFrom?.OriginalDefinition ??
                methodSymbol.OriginalDefinition;

            if (IsOfTypeSequenceMethod(originalMethod))
            {
                return true;
            }

            if (TryGetElementPreservingSequenceSource(
                    invocation,
                    methodSymbol,
                    originalMethod,
                    out ExpressionSyntax? sourceExpression) &&
                sourceExpression != null)
            {
                return IsSequenceExpressionProvenToExcludeNullElements(
                    sourceExpression,
                    semanticModel,
                    inspectedSequenceSources);
            }

            if (originalMethod.DeclaringSyntaxReferences.Length == 0 ||
                !inspectedSequenceSources.Add(originalMethod))
            {
                return false;
            }

            bool foundReturnExpression = false;

            try
            {
                foreach (SyntaxReference syntaxReference
                         in originalMethod.DeclaringSyntaxReferences)
                {
                    SyntaxNode declarationNode =
                        syntaxReference.GetSyntax();

                    SemanticModel? declarationSemanticModel =
                        GetSemanticModelForSyntaxTree(
                            semanticModel,
                            declarationNode.SyntaxTree);

                    if (declarationSemanticModel == null)
                    {
                        return false;
                    }

                    IEnumerable<ExpressionSyntax> returnExpressions =
                        GetSequenceReturnExpressions(
                            declarationNode);

                    foreach (ExpressionSyntax returnExpression
                             in returnExpressions)
                    {
                        foundReturnExpression = true;

                        if (!IsSequenceExpressionProvenToExcludeNullElements(
                                returnExpression,
                                declarationSemanticModel,
                                inspectedSequenceSources))
                        {
                            return false;
                        }
                    }
                }

                return foundReturnExpression;
            }
            finally
            {
                inspectedSequenceSources.Remove(originalMethod);
            }
        }

        /// <summary>
        /// Gets the expressions returned by a method or local-function
        /// declaration without descending into nested callables.
        /// </summary>
        /// <param name="declarationNode">
        /// The callable declaration to inspect.
        /// </param>
        /// <returns>The returned sequence expressions.</returns>
        private static IEnumerable<ExpressionSyntax>
            GetSequenceReturnExpressions(
                SyntaxNode declarationNode)
        {
            switch (declarationNode)
            {
                case MethodDeclarationSyntax methodDeclaration
                    when methodDeclaration.ExpressionBody != null:
                    return
                    [
                        methodDeclaration.ExpressionBody.Expression
                    ];

                case MethodDeclarationSyntax methodDeclaration
                    when methodDeclaration.Body != null:
                    return methodDeclaration.Body
                        .DescendantNodes(
                            static node =>
                                node
                                    is not AnonymousFunctionExpressionSyntax &&
                                node
                                    is not LocalFunctionStatementSyntax)
                        .OfType<ReturnStatementSyntax>()
                        .Where(
                            static statement =>
                                statement.Expression != null)
                        .Select(
                            static statement =>
                                statement.Expression!);

                case LocalFunctionStatementSyntax localFunction
                    when localFunction.ExpressionBody != null:
                    return
                    [
                        localFunction.ExpressionBody.Expression
                    ];

                case LocalFunctionStatementSyntax localFunction
                    when localFunction.Body != null:
                    return localFunction.Body
                        .DescendantNodes(
                            static node =>
                                node
                                    is not AnonymousFunctionExpressionSyntax &&
                                node
                                    is not LocalFunctionStatementSyntax)
                        .OfType<ReturnStatementSyntax>()
                        .Where(
                            static statement =>
                                statement.Expression != null)
                        .Select(
                            static statement =>
                                statement.Expression!);

                default:
                    return Array.Empty<ExpressionSyntax>();
            }
        }

        /// <summary>
        /// Determines whether a method is LINQ's runtime type-filtering
        /// <c>OfType&lt;T&gt;</c> operation.
        /// </summary>
        /// <param name="methodSymbol">
        /// The method symbol to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the method filters elements by runtime
        /// type; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsOfTypeSequenceMethod(
            IMethodSymbol methodSymbol)
        {
            if (!methodSymbol.IsStatic ||
                methodSymbol.Name != "OfType" ||
                methodSymbol.Arity != 1 ||
                methodSymbol.Parameters.Length != 1)
            {
                return false;
            }

            string containingTypeName =
                methodSymbol.ContainingType.ToDisplayString();

            return containingTypeName ==
                       "System.Linq.Enumerable" ||
                   containingTypeName ==
                       "System.Linq.Queryable";
        }

        /// <summary>
        /// Gets a semantic model for a syntax tree if the tree belongs to
        /// the same compilation as the supplied semantic model.
        /// </summary>
        /// <param name="semanticModel">
        /// The currently available semantic model.
        /// </param>
        /// <param name="syntaxTree">
        /// The syntax tree whose semantic model is required.
        /// </param>
        /// <returns>
        /// The semantic model for <paramref name="syntaxTree"/>, or
        /// <see langword="null"/> if the tree does not belong to the
        /// compilation.
        /// </returns>
        private static SemanticModel? GetSemanticModelForSyntaxTree(
            SemanticModel semanticModel,
            SyntaxTree syntaxTree)
        {
            if (semanticModel.SyntaxTree == syntaxTree)
            {
                return semanticModel;
            }

            if (!semanticModel.Compilation.SyntaxTrees.Contains(
                    syntaxTree))
            {
                return null;
            }

            return semanticModel.Compilation.GetSemanticModel(
                syntaxTree);
        }
    }
}
