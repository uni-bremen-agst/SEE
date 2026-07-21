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
        /// Determines whether an <see cref="ArgumentNullException"/>
        /// <c>ThrowIfNull</c> invocation is proven not to throw at its current
        /// call site.
        /// </summary>
        /// <param name="invocation">The framework helper invocation.</param>
        /// <param name="methodSymbol">The resolved framework helper symbol.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <returns>
        /// <see langword="true"/> if the guarded argument is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNonThrowingArgumentNullGuard(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (!KnownFrameworkExceptionModel.IsArgumentNullThrowIfNull(
                    methodSymbol,
                    semanticModel.Compilation))
            {
                return false;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments =
                invocation.ArgumentList.Arguments;

            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentSyntax argument = arguments[i];

                int parameterIndex =
                    GetParameterIndexForArgument(
                        argument,
                        i,
                        methodSymbol);

                if (parameterIndex != 0)
                {
                    continue;
                }

                return IsDefinitelyNonNull(
                    argument.Expression,
                    semanticModel,
                    callContext);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an expression is proven to evaluate to a non-null value without
        /// relying only on nullable reference-type annotations.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol and constant resolution.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
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
        /// Determines whether an expression is proven to evaluate to a non-null value while
        /// preventing recursive return-value analysis.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="semanticModel">The semantic model used for symbol and constant resolution.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
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
                case ParenthesizedExpressionSyntax parenthesizedExpression:
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
                    when binaryExpression.IsKind(SyntaxKind.CoalesceExpression):
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
                return callContext.IsParameterKnownNonNull(parameterSymbol);
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

            return false;
        }

        /// <summary>
        /// Determines whether the specified type is a nullable value type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a nullable value type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNullableValueType(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol namedType &&
                   namedType.OriginalDefinition.SpecialType ==
                   SpecialType.System_Nullable_T;
        }

        /// <summary>
        /// Determines whether a local variable is guaranteed to contain a non-null value
        /// because it was introduced by a non-null pattern, initialized with a value
        /// proven to be non-null, or protected by an earlier terminating null guard.
        /// </summary>
        /// <param name="expression">The local-variable expression being evaluated.</param>
        /// <param name="localSymbol">The local symbol to inspect.</param>
        /// <param name="semanticModel">The semantic model used for expression analysis.</param>
        /// <param name="callContext">The call-site facts known for the current callable.</param>
        /// <param name="inspectedReturnSymbols">
        /// The method symbols whose return values are currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local variable is proven to be non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalGuaranteedNonNull(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedReturnSymbols)
        {
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

                if (declarationNode is SingleVariableDesignationSyntax)
                {
                    PatternSyntax? declaringPattern =
                        declarationNode.Ancestors()
                            .OfType<PatternSyntax>()
                            .FirstOrDefault();

                    if (declaringPattern is DeclarationPatternSyntax or
                        RecursivePatternSyntax or
                        ListPatternSyntax)
                    {
                        return true;
                    }

                    continue;
                }

                if (declarationNode is not VariableDeclaratorSyntax variableDeclarator ||
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

                if (IsDefinitelyNonNull(
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
        /// Gets a semantic model for a syntax tree if the tree belongs to the same
        /// compilation as the supplied semantic model.
        /// </summary>
        /// <param name="semanticModel">The currently available semantic model.</param>
        /// <param name="syntaxTree">The syntax tree whose semantic model is required.</param>
        /// <returns>
        /// The semantic model for <paramref name="syntaxTree"/>, or
        /// <see langword="null"/> if the tree does not belong to the compilation.
        /// </returns>
        private static SemanticModel? GetSemanticModelForSyntaxTree(
            SemanticModel semanticModel,
            SyntaxTree syntaxTree)
        {
            if (semanticModel.SyntaxTree == syntaxTree)
            {
                return semanticModel;
            }

            if (!semanticModel.Compilation.SyntaxTrees.Contains(syntaxTree))
            {
                return null;
            }

            return semanticModel.Compilation.GetSemanticModel(syntaxTree);
        }
    }
}
