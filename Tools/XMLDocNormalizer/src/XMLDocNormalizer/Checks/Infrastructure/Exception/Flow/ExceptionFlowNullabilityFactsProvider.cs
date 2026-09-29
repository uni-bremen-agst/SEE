using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides stateless type- and declaration-based nullability facts.
    /// </summary>
    internal static class ExceptionFlowNullabilityFactsProvider
    {
        /// <summary>
        /// Determines whether the specified type is a nullable value type.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type is a nullable value type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsNullableValueType(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol
                       is INamedTypeSymbol namedType
                   && namedType.OriginalDefinition.SpecialType ==
                       SpecialType.System_Nullable_T;
        }

        /// <summary>
        /// Determines whether a local variable is introduced by a pattern
        /// that guarantees a non-null value whenever the local is definitely
        /// assigned.
        /// </summary>
        /// <param name="localSymbol">The local symbol to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the declaring designation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local is declared by a pattern that
        /// excludes <see langword="null"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsPatternLocalGuaranteedNonNull(
            ILocalSymbol localSymbol,
            SemanticModel semanticModel)
        {
            foreach (SyntaxReference syntaxReference
                     in localSymbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declarationNode =
                    syntaxReference.GetSyntax();

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
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
        /// Determines whether a known framework method returns a sequence
        /// whose elements are guaranteed to be non-null after normal return.
        /// </summary>
        /// <param name="methodSymbol">The original method definition.</param>
        /// <param name="compilation">
        /// The compilation used to resolve trusted framework types.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the exact framework signature has a
        /// modeled non-null element guarantee; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool IsKnownFrameworkSequenceWithNonNullElements(
            IMethodSymbol methodSymbol,
            Compilation compilation)
        {
            if (!methodSymbol.IsStatic
                || methodSymbol.MethodKind != MethodKind.Ordinary
                || methodSymbol.Arity != 0
                || !string.Equals(
                    methodSymbol.Name,
                    nameof(Directory.EnumerateFiles),
                    StringComparison.Ordinal)
                || methodSymbol.ContainingType is not INamedTypeSymbol containingType
                || !IsFrameworkType(
                    containingType,
                    compilation,
                    "System.IO.Directory")
                || methodSymbol.Parameters.Length != 3
                || methodSymbol.Parameters[0].Type.SpecialType != SpecialType.System_String
                || methodSymbol.Parameters[1].Type.SpecialType != SpecialType.System_String
                || methodSymbol.Parameters[2].Type is not INamedTypeSymbol searchOptionType
                || !IsFrameworkType(
                    searchOptionType,
                    compilation,
                    "System.IO.SearchOption")
                || methodSymbol.ReturnType is not INamedTypeSymbol returnType
                || !IsFrameworkType(
                    returnType,
                    compilation,
                    "System.Collections.Generic.IEnumerable`1")
                || returnType.TypeArguments.Length != 1
                || returnType.TypeArguments[0].SpecialType != SpecialType.System_String)
            {
                return false;
            }

            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a method is LINQ's runtime type-filtering
        /// <c>OfType&lt;T&gt;</c> operation.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the method filters elements by runtime
        /// type; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsOfTypeSequenceMethod(
            IMethodSymbol methodSymbol)
        {
            if (!methodSymbol.IsStatic
                || methodSymbol.Name != "OfType"
                || methodSymbol.Arity != 1
                || methodSymbol.Parameters.Length != 1)
            {
                return false;
            }

            string containingTypeName =
                methodSymbol.ContainingType.ToDisplayString();

            return containingTypeName ==
                       "System.Linq.Enumerable"
                   || containingTypeName ==
                       "System.Linq.Queryable";
        }

        /// <summary>
        /// Determines whether a type symbol represents the specified
        /// framework type.
        /// </summary>
        /// <param name="actualType">The actual type symbol.</param>
        /// <param name="compilation">
        /// The compilation used for type resolution.
        /// </param>
        /// <param name="metadataName">The expected metadata name.</param>
        /// <returns>
        /// <see langword="true"/> if the symbols represent the same type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsFrameworkType(
            INamedTypeSymbol actualType,
            Compilation compilation,
            string metadataName)
        {
            INamedTypeSymbol? expectedType =
                compilation.GetTypeByMetadataName(metadataName);

            return expectedType != null
                && SymbolEqualityComparer.Default.Equals(
                    actualType.OriginalDefinition,
                    expectedType.OriginalDefinition);
        }
    }
}
