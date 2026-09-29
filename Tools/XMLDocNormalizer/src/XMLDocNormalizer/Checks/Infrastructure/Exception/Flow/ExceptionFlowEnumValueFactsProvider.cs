using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides stateless syntax, type, and constant facts used by enum value
    /// analysis.
    /// </summary>
    internal static class ExceptionFlowEnumValueFactsProvider
    {
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
        internal static List<ExpressionSyntax> GetSourceReturnExpressions(
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
        /// Determines whether an expression is an array creation whose
        /// initializer can be inspected.
        /// </summary>
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="initializer">
        /// The array initializer when available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression is a supported array
        /// creation with an initializer; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool TryGetArrayInitializer(
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
        /// <param name="expression">The expression to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <param name="enumType">The resolved enum type when successful.</param>
        /// <returns>
        /// <see langword="true"/> when the effective expression type is an
        /// enum; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetEnumType(
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
        /// <param name="expression">The sequence expression to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used for type resolution.
        /// </param>
        /// <param name="enumType">The enum element type when successful.</param>
        /// <returns>
        /// <see langword="true"/> when the expression is an array or generic
        /// enumerable of enum values; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetSequenceEnumElementType(
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
        /// <param name="typeSymbol">The sequence type to inspect.</param>
        /// <param name="enumType">The enum element type when successful.</param>
        /// <returns>
        /// <see langword="true"/> when the type is an array or implements
        /// <see cref="IEnumerable{T}"/> for an enum element type; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool TryGetSequenceEnumElementType(
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
        /// <param name="enumType">The enum type to inspect.</param>
        /// <param name="constantValue">The constant value to compare.</param>
        /// <returns>
        /// <see langword="true"/> when a declared enum member has the supplied
        /// constant value; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDeclaredEnumConstantValue(
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
    }
}
