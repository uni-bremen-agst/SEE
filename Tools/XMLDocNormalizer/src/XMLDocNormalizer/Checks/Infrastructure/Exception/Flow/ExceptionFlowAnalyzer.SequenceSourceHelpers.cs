using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains conservative source-level reasoning for helper calls that
    /// observe a materialized sequence without mutating or exposing its
    /// contents.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether a local sequence reference is passed to one
        /// statically bound source helper whose corresponding parameter is
        /// only observed in ways that preserve the sequence contents.
        /// </summary>
        /// <param name="reference">
        /// The local sequence reference to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the helper is source-available,
        /// statically bound, and every use of the corresponding parameter is
        /// proven read-only for the supplied materialized sequence; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool
            IsSourceHelperArgumentProvenToPreserveSequenceContents(
                IdentifierNameSyntax reference,
                SemanticModel semanticModel)
        {
            if (reference.Parent
                    is not ArgumentSyntax argument ||
                !ReferenceEquals(
                    argument.Expression,
                    reference) ||
                argument.Parent?.Parent
                    is not InvocationExpressionSyntax invocation ||
                !argument.RefKindKeyword.IsKind(
                    SyntaxKind.None))
            {
                return false;
            }

            ITypeSymbol? sourceType =
                semanticModel.GetTypeInfo(reference).Type;

            Conversion conversion =
                semanticModel.GetConversion(reference);

            if (sourceType == null ||
                conversion.IsUserDefined ||
                !IsKnownMaterializedSequenceType(sourceType))
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.ReducedFrom != null ||
                selectedMethod.IsAbstract ||
                selectedMethod.IsExtern ||
                RequiresSummaryRuntimeDispatch(selectedMethod) ||
                selectedMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            int fallbackIndex =
                invocation.ArgumentList.Arguments.IndexOf(argument);

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
                parameterIndex >= selectedMethod.Parameters.Length)
            {
                return false;
            }

            IParameterSymbol parameterSymbol =
                selectedMethod.Parameters[parameterIndex];

            return parameterSymbol.RefKind == RefKind.None &&
                   DoesSourceParameterPreserveSequenceContents(
                       parameterSymbol,
                       semanticModel);
        }

        /// <summary>
        /// Determines whether a sequence value has a concrete framework type
        /// whose ordinary enumeration does not mutate its contents.
        /// </summary>
        /// <param name="typeSymbol">
        /// The compile-time source type at the helper call site.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for arrays and framework
        /// <see cref="List{T}"/> values; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsKnownMaterializedSequenceType(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol is IArrayTypeSymbol ||
                   IsListType(typeSymbol);
        }

        /// <summary>
        /// Determines whether every source-level use of one helper parameter
        /// preserves the contents of a materialized sequence passed to it.
        /// </summary>
        /// <param name="parameterSymbol">
        /// The helper parameter receiving the sequence.
        /// </param>
        /// <param name="semanticModel">
        /// A semantic model from the caller compilation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every parameter reference is a null
        /// comparison, a supported collection-count read, or direct foreach
        /// enumeration; otherwise <see langword="false"/>.
        /// </returns>
        private static bool DoesSourceParameterPreserveSequenceContents(
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            if (parameterSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

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

            foreach (IdentifierNameSyntax reference in references)
            {
                if (IsInsideNestedCallable(
                        reference,
                        containingCallable) ||
                    (!IsSupportedReadOnlySequenceObservation(
                         reference,
                         declarationSemanticModel) &&
                     !IsSupportedSequenceNullObservation(reference) &&
                     !IsDirectForeachSequenceObservation(reference)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a parameter reference is nested inside another
        /// callable and therefore represents a captured sequence reference.
        /// </summary>
        /// <param name="reference">
        /// The parameter reference to inspect.
        /// </param>
        /// <param name="containingCallable">
        /// The source helper whose parameter is being analyzed.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is located inside a
        /// nested anonymous or local function; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsInsideNestedCallable(
            IdentifierNameSyntax reference,
            SyntaxNode containingCallable)
        {
            foreach (SyntaxNode ancestor in reference.Ancestors())
            {
                if (ReferenceEquals(
                        ancestor,
                        containingCallable))
                {
                    return false;
                }

                if (ancestor is AnonymousFunctionExpressionSyntax ||
                    ancestor is LocalFunctionStatementSyntax)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a sequence reference is used directly as the
        /// source of a foreach statement.
        /// </summary>
        /// <param name="reference">
        /// The sequence reference to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the reference is the foreach source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDirectForeachSequenceObservation(
            IdentifierNameSyntax reference)
        {
            if (reference.Parent
                    is ForEachStatementSyntax forEachStatement)
            {
                return ReferenceEquals(
                    forEachStatement.Expression,
                    reference);
            }

            return reference.Parent
                       is ForEachVariableStatementSyntax
                           forEachVariableStatement &&
                   ReferenceEquals(
                       forEachVariableStatement.Expression,
                       reference);
        }
    }
}
