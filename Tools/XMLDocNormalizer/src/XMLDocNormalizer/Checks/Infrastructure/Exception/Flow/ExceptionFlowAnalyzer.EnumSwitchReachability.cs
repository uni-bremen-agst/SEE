using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains reachability reasoning for fallback arms of exhaustive enum
    /// switch expressions.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether a throw expression belongs to a discard fallback
        /// arm that cannot be selected for a value proven to be one of the
        /// declared values of its enum type.
        /// </summary>
        /// <param name="throwExpression">
        /// The throw expression to inspect.
        /// </param>
        /// <param name="switchArm">
        /// The switch-expression arm containing the throw.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for type and constant resolution.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the fallback throw is unreachable for
        /// every declared enum value; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsThrowExpressionInExhaustiveDefinedEnumFallback(
            ThrowExpressionSyntax throwExpression,
            SwitchExpressionArmSyntax switchArm,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
        {
            if (switchArm.Pattern is not DiscardPatternSyntax
                || switchArm.WhenClause != null
                || !switchArm.Expression.Span.Contains(
                    throwExpression.Span)
                || switchArm.Parent is not SwitchExpressionSyntax switchExpression)
            {
                return false;
            }

            ExceptionFlowValueFacts governingFacts =
                GetExpressionValueFacts(
                    switchExpression.GoverningExpression,
                    semanticModel,
                    callContext);

            if (!governingFacts.ContainsAll(
                    ExceptionFlowValueFacts.DefinedEnumValue))
            {
                return false;
            }

            TypeInfo governingTypeInfo =
                semanticModel.GetTypeInfo(
                    switchExpression.GoverningExpression);

            ITypeSymbol? governingType =
                governingTypeInfo.ConvertedType
                ?? governingTypeInfo.Type;

            if (governingType is not INamedTypeSymbol enumType
                || enumType.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            HashSet<object> declaredValues =
                GetDeclaredEnumConstantValues(
                    enumType);

            if (declaredValues.Count == 0)
            {
                return false;
            }

            HashSet<object> coveredValues =
                new();

            foreach (SwitchExpressionArmSyntax candidateArm
                     in switchExpression.Arms)
            {
                if (ReferenceEquals(
                        candidateArm,
                        switchArm))
                {
                    break;
                }

                if (candidateArm.WhenClause != null
                    || candidateArm.Pattern
                        is not ConstantPatternSyntax constantPattern)
                {
                    continue;
                }

                Optional<object?> constantValue =
                    semanticModel.GetConstantValue(
                        constantPattern.Expression);

                if (constantValue.HasValue
                    && constantValue.Value != null)
                {
                    coveredValues.Add(
                        constantValue.Value);
                }
            }

            return declaredValues.All(
                coveredValues.Contains);
        }

        /// <summary>
        /// Gets the distinct constant values explicitly declared by an enum.
        /// </summary>
        /// <param name="enumType">
        /// The enum type to inspect.
        /// </param>
        /// <returns>
        /// The distinct non-null values declared by enum members.
        /// </returns>
        private static HashSet<object> GetDeclaredEnumConstantValues(
            INamedTypeSymbol enumType)
        {
            HashSet<object> values =
                new();

            foreach (IFieldSymbol fieldSymbol
                     in enumType.GetMembers()
                         .OfType<IFieldSymbol>())
            {
                if (!fieldSymbol.HasConstantValue
                    || fieldSymbol.ConstantValue == null)
                {
                    continue;
                }

                values.Add(
                    fieldSymbol.ConstantValue);
            }

            return values;
        }
    }
}
