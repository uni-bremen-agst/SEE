using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning for one-based source positions derived
    /// from Roslyn line-span information.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets positive integer facts for a one-based Roslyn source
        /// coordinate.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="callContext">
        /// The current callable value facts.
        /// </param>
        /// <param name="inspectedValueSources">
        /// Symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.PositiveInt32"/> when the
        /// expression is proven to be a one-based source coordinate;
        /// otherwise <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts GetOneBasedSourcePositionValueFacts(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                if (!inspectedValueSources.Add(localSymbol))
                {
                    return ExceptionFlowValueFacts.None;
                }

                try
                {
                    if (!TryGetCurrentLocalInitializerExpression(
                            unwrappedExpression,
                            localSymbol,
                            semanticModel,
                            out ExpressionSyntax? initializer)
                        || initializer == null)
                    {
                        return ExceptionFlowValueFacts.None;
                    }

                    return GetOneBasedSourcePositionValueFacts(
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

            if (unwrappedExpression is not BinaryExpressionSyntax addition
                || !addition.IsKind(SyntaxKind.AddExpression)
                || semanticModel.GetOperation(addition) is not IBinaryOperation binaryOperation
                || binaryOperation.OperatorKind != BinaryOperatorKind.Add
                || binaryOperation.OperatorMethod != null
                || binaryOperation.Type?.SpecialType != SpecialType.System_Int32)
            {
                return ExceptionFlowValueFacts.None;
            }

            ExpressionSyntax? coordinateExpression = null;

            if (TryGetInt32Constant(addition.Left, semanticModel, out int leftConstant)
                && leftConstant == 1)
            {
                coordinateExpression = addition.Right;
            }
            else if (TryGetInt32Constant(addition.Right, semanticModel, out int rightConstant)
                && rightConstant == 1)
            {
                coordinateExpression = addition.Left;
            }

            if (coordinateExpression == null
                || !IsStartSourceCoordinateFromNonEmptyLineSpan(
                    coordinateExpression,
                    semanticModel,
                    inspectedValueSources))
            {
                return ExceptionFlowValueFacts.None;
            }

            return ExceptionFlowValueFacts.PositiveInt32;
        }

        /// <summary>
        /// Determines whether an expression is the line or character component
        /// of a start line position produced for a non-empty source span.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedValueSources">
        /// Symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression is a supported start
        /// source coordinate; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsStartSourceCoordinateFromNonEmptyLineSpan(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression is not MemberAccessExpressionSyntax coordinateAccess
                || semanticModel.GetSymbolInfo(coordinateAccess).Symbol is not IPropertySymbol coordinateProperty
                || (coordinateProperty.Name != "Line" && coordinateProperty.Name != "Character")
                || !HasMetadataIdentity(
                    coordinateProperty.ContainingType,
                    "Microsoft.CodeAnalysis.Text",
                    "LinePosition"))
            {
                return false;
            }

            return IsStartLinePositionFromNonEmptyLineSpan(
                coordinateAccess.Expression,
                semanticModel,
                inspectedValueSources);
        }

        /// <summary>
        /// Determines whether an expression represents the start line position
        /// of a line span derived from a non-empty source span.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedValueSources">
        /// Symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression represents the required
        /// start line position; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsStartLinePositionFromNonEmptyLineSpan(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                if (!inspectedValueSources.Add(localSymbol))
                {
                    return false;
                }

                try
                {
                    if (!TryGetCurrentLocalInitializerExpression(
                            unwrappedExpression,
                            localSymbol,
                            semanticModel,
                            out ExpressionSyntax? initializer)
                        || initializer == null)
                    {
                        return false;
                    }

                    return IsStartLinePositionFromNonEmptyLineSpan(
                        initializer,
                        semanticModel,
                        inspectedValueSources);
                }
                finally
                {
                    inspectedValueSources.Remove(localSymbol);
                }
            }

            if (unwrappedExpression is not MemberAccessExpressionSyntax positionAccess
                || semanticModel.GetSymbolInfo(positionAccess).Symbol is not IPropertySymbol positionProperty
                || positionProperty.Name != "StartLinePosition"
                || !HasMetadataIdentity(
                    positionProperty.ContainingType,
                    "Microsoft.CodeAnalysis",
                    "FileLinePositionSpan"))
            {
                return false;
            }

            return IsLineSpanFromNonEmptySyntaxTreeSpan(
                positionAccess.Expression,
                semanticModel,
                inspectedValueSources);
        }

        /// <summary>
        /// Determines whether an expression is a line span returned by a
        /// Roslyn syntax tree for a non-empty text span.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method and argument resolution.
        /// </param>
        /// <param name="inspectedValueSources">
        /// Symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the line span originates from a
        /// supported non-empty text span; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLineSpanFromNonEmptySyntaxTreeSpan(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                if (!inspectedValueSources.Add(localSymbol))
                {
                    return false;
                }

                try
                {
                    if (!TryGetCurrentLocalInitializerExpression(
                            unwrappedExpression,
                            localSymbol,
                            semanticModel,
                            out ExpressionSyntax? initializer)
                        || initializer == null)
                    {
                        return false;
                    }

                    return IsLineSpanFromNonEmptySyntaxTreeSpan(
                        initializer,
                        semanticModel,
                        inspectedValueSources);
                }
                finally
                {
                    inspectedValueSources.Remove(localSymbol);
                }
            }

            if (unwrappedExpression is not InvocationExpressionSyntax invocation
                || semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol selectedMethod)
            {
                return false;
            }

            IMethodSymbol method = selectedMethod.OriginalDefinition;

            if (method.Name != "GetLineSpan"
                || !HasMetadataIdentity(
                    method.ContainingType,
                    "Microsoft.CodeAnalysis",
                    "SyntaxTree")
                || !HasMetadataIdentity(
                    method.ReturnType,
                    "Microsoft.CodeAnalysis",
                    "FileLinePositionSpan"))
            {
                return false;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

            for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
            {
                ArgumentSyntax argument = arguments[argumentIndex];

                int parameterIndex = GetParameterIndexForArgument(
                    argument,
                    argumentIndex,
                    selectedMethod);

                if (parameterIndex < 0
                    || parameterIndex >= selectedMethod.Parameters.Length
                    || !HasMetadataIdentity(
                        selectedMethod.Parameters[parameterIndex].Type,
                        "Microsoft.CodeAnalysis.Text",
                        "TextSpan"))
                {
                    continue;
                }

                return IsNonEmptyTextSpanExpression(
                    argument.Expression,
                    semanticModel,
                    inspectedValueSources);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an expression creates a Roslyn text span with a
        /// compile-time positive length.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constructor and constant resolution.
        /// </param>
        /// <param name="inspectedValueSources">
        /// Symbols currently inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression produces a text span with
        /// positive compile-time length; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNonEmptyTextSpanExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(unwrappedExpression);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                if (!inspectedValueSources.Add(localSymbol))
                {
                    return false;
                }

                try
                {
                    if (!TryGetCurrentLocalInitializerExpression(
                            unwrappedExpression,
                            localSymbol,
                            semanticModel,
                            out ExpressionSyntax? initializer)
                        || initializer == null)
                    {
                        return false;
                    }

                    return IsNonEmptyTextSpanExpression(
                        initializer,
                        semanticModel,
                        inspectedValueSources);
                }
                finally
                {
                    inspectedValueSources.Remove(localSymbol);
                }
            }

            ArgumentListSyntax? argumentList = unwrappedExpression switch
            {
                ObjectCreationExpressionSyntax objectCreation => objectCreation.ArgumentList,
                ImplicitObjectCreationExpressionSyntax implicitCreation => implicitCreation.ArgumentList,
                _ => null
            };

            if (argumentList == null
                || semanticModel.GetSymbolInfo(unwrappedExpression).Symbol is not IMethodSymbol constructor
                || constructor.MethodKind != MethodKind.Constructor
                || !HasMetadataIdentity(
                    constructor.ContainingType,
                    "Microsoft.CodeAnalysis.Text",
                    "TextSpan"))
            {
                return false;
            }

            for (int argumentIndex = 0; argumentIndex < argumentList.Arguments.Count; argumentIndex++)
            {
                ArgumentSyntax argument = argumentList.Arguments[argumentIndex];

                int parameterIndex = GetParameterIndexForArgument(
                    argument,
                    argumentIndex,
                    constructor);

                if (parameterIndex < 0
                    || parameterIndex >= constructor.Parameters.Length
                    || constructor.Parameters[parameterIndex].Name != "length")
                {
                    continue;
                }

                return TryGetInt32Constant(argument.Expression, semanticModel, out int length)
                    && length > 0;
            }

            return false;
        }

        /// <summary>
        /// Gets the initializer that still determines the current value of a
        /// local variable.
        /// </summary>
        /// <param name="useExpression">
        /// The expression at which the current local value is required.
        /// </param>
        /// <param name="localSymbol">
        /// The local variable whose initializer should be inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to verify whether the initializer is still
        /// current.
        /// </param>
        /// <param name="initializer">
        /// The current initializer expression when the method succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the local has a usable initializer that
        /// still determines its value; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetCurrentLocalInitializerExpression(
            ExpressionSyntax useExpression,
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            out ExpressionSyntax? initializer)
        {
            initializer = null;

            if (localSymbol.DeclaringSyntaxReferences.Length != 1
                || localSymbol.DeclaringSyntaxReferences[0].GetSyntax() is not VariableDeclaratorSyntax declarator
                || declarator.Initializer == null
                || !IsLocalInitializerStillCurrent(
                    useExpression,
                    localSymbol,
                    declarator,
                    semanticModel))
            {
                return false;
            }

            initializer = declarator.Initializer.Value;
            return true;
        }

        /// <summary>
        /// Determines whether a type has the expected namespace and simple
        /// metadata name.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type symbol to inspect.
        /// </param>
        /// <param name="namespaceName">
        /// The expected containing namespace.
        /// </param>
        /// <param name="typeName">
        /// The expected simple type name.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when both namespace and type name match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasMetadataIdentity(
            ITypeSymbol typeSymbol,
            string namespaceName,
            string typeName)
        {
            return string.Equals(typeSymbol.Name, typeName, StringComparison.Ordinal)
                && string.Equals(
                    typeSymbol.ContainingNamespace.ToDisplayString(),
                    namespaceName,
                    StringComparison.Ordinal);
        }
    }
}
