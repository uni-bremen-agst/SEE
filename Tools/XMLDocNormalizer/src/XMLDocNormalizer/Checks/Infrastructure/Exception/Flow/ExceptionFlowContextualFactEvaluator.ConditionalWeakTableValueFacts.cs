using System.Runtime.CompilerServices;
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
        /// Stores cache-invariant results in weak semantic-model partitions so
        /// cached Roslyn symbols cannot outlive their semantic world.
        /// </summary>
        private static readonly ConditionalWeakTable<
            SemanticModel,
            ConditionalWeakTableValueFactCachePartition>
            conditionalWeakTableValueFactCaches = new();

        /// <summary>
        /// Determines whether a supported
        /// <see cref="ConditionalWeakTable{TKey, TValue}.GetValue"/> invocation
        /// is guaranteed to return a non-null value.
        /// </summary>
        /// <param name="invocation">The invocation to inspect.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The value-producing symbols currently being inspected recursively.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the receiver is a private, empty,
        /// source-owned cache and every possible stored value is proven
        /// non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsConditionalWeakTableGetValueResultDefinitelyNonNull(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            if (!ExceptionFlowConditionalWeakTableValueFactsProvider.TryGetConditionalWeakTableGetValueParts(
                    invocation,
                    semanticModel,
                    out ExpressionSyntax? receiver,
                    out ExpressionSyntax? factory)
                || receiver == null
                || factory == null
                || semanticModel.GetSymbolInfo(receiver).Symbol
                    is not IFieldSymbol fieldSymbol
                || fieldSymbol.DeclaredAccessibility != Accessibility.Private
                || !fieldSymbol.IsStatic
                || !fieldSymbol.IsReadOnly)
            {
                return false;
            }

            ISymbol normalizedField = fieldSymbol.OriginalDefinition;

            if (!inspectedValueSources.Add(normalizedField))
            {
                return false;
            }

            try
            {
                ConditionalWeakTableValueFactCachePartition cache =
                    conditionalWeakTableValueFactCaches.GetValue(
                        semanticModel,
                        static _ =>
                            new ConditionalWeakTableValueFactCachePartition());

                if (cache.TryGetValue(normalizedField, out bool cachedResult))
                {
                    return cachedResult;
                }

                HashSet<ISymbol> invariantInspectedValueSources =
                    new(SymbolEqualityComparer.Default)
                    {
                        normalizedField
                    };

                bool result =
                    ExceptionFlowConditionalWeakTableValueFactsProvider.IsConditionalWeakTableFieldInitializedEmpty(
                        fieldSymbol,
                        semanticModel)
                    && AreAllConditionalWeakTableFieldValuesDefinitelyNonNull(
                        fieldSymbol,
                        semanticModel,
                        invariantInspectedValueSources);

                cache.Store(normalizedField, result);
                return result;
            }
            finally
            {
                inspectedValueSources.Remove(normalizedField);
            }
        }
        /// <summary>
        /// Determines whether every source use of a private table field can
        /// store only callback results proven to be non-null.
        /// </summary>
        /// <param name="fieldSymbol">The table field to inspect.</param>
        /// <param name="semanticModel">
        /// A semantic model from the field's compilation.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The active field and callback return-analysis guard.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every field reference is the receiver of
        /// the exact supported <c>GetValue</c> overload and every factory is
        /// proven non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreAllConditionalWeakTableFieldValuesDefinitelyNonNull(
            IFieldSymbol fieldSymbol,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            Compilation compilation = semanticModel.Compilation;
            bool foundReference = false;

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel? treeSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        compilation,
                        syntaxTree);

                if (treeSemanticModel == null)
                {
                    return false;
                }

                foreach (IdentifierNameSyntax identifier
                         in syntaxTree.GetRoot()
                             .DescendantNodes()
                             .OfType<IdentifierNameSyntax>())
                {
                    if (!string.Equals(
                            identifier.Identifier.ValueText,
                            fieldSymbol.Name,
                            StringComparison.Ordinal)
                        || treeSemanticModel.GetSymbolInfo(identifier).Symbol
                            is not IFieldSymbol referencedField
                        || !SymbolEqualityComparer.Default.Equals(
                            referencedField.OriginalDefinition,
                            fieldSymbol.OriginalDefinition))
                    {
                        continue;
                    }

                    foundReference = true;

                    if (!ExceptionFlowConditionalWeakTableValueFactsProvider.TryGetConditionalWeakTableFactoryForFieldReference(
                            identifier,
                            fieldSymbol,
                            treeSemanticModel,
                            out ExpressionSyntax? factory)
                        || factory == null
                        || !IsCallbackReturnDefinitelyNonNull(
                            factory,
                            treeSemanticModel,
                            inspectedValueSources))
                    {
                        return false;
                    }
                }
            }

            return foundReference;
        }
        /// <summary>
        /// Determines whether every normal return of one statically resolved
        /// source callback is proven to be non-null.
        /// </summary>
        /// <param name="factory">The delegate-valued factory expression.</param>
        /// <param name="semanticModel">
        /// The semantic model associated with the factory expression.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The active field and callback return-analysis guard.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when one stable source callback was resolved
        /// and all of its normal returns are non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsCallbackReturnDefinitelyNonNull(
            ExpressionSyntax factory,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedValueSources)
        {
            if (!ExceptionFlowDelegateTargetResolver.TryResolveDelegateTarget(
                    factory,
                    semanticModel,
                    out IMethodSymbol? targetMethod)
                || targetMethod == null
                || targetMethod.ReturnsVoid
                || targetMethod.IsAsync
                || targetMethod.IsExtern
                || targetMethod.IsAbstract
                || targetMethod.IsIterator
                || targetMethod.ReturnsByRef
                || targetMethod.ReturnsByRefReadonly
                || targetMethod.ReducedFrom != null
                || ExceptionFlowRuntimeDispatchClassifier.RequiresRuntimeDispatch(targetMethod)
                || targetMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            ISymbol normalizedTarget = targetMethod.OriginalDefinition;

            if (!inspectedValueSources.Add(normalizedTarget))
            {
                return false;
            }

            try
            {
                SyntaxNode declaration =
                    targetMethod.DeclaringSyntaxReferences[0].GetSyntax();

                SemanticModel? declarationSemanticModel =
                    ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                        semanticModel,
                        declaration.SyntaxTree);

                if (declarationSemanticModel == null)
                {
                    return false;
                }

                List<KeyValuePair<int, ExceptionFlowValueFacts>> parameterFacts =
                    new();

                if (targetMethod.Parameters.Length > 0)
                {
                    parameterFacts.Add(
                        new KeyValuePair<int, ExceptionFlowValueFacts>(
                            0,
                            ExceptionFlowValueFacts.NonNull));
                }

                ExceptionFlowCallContext callbackContext =
                    new(targetMethod, parameterFacts);

                List<ExpressionSyntax> returnExpressions =
                    ExceptionFlowConditionalWeakTableValueFactsProvider.GetCallbackReturnExpressions(
                        declaration);

                if (returnExpressions.Count == 0)
                {
                    return false;
                }

                foreach (ExpressionSyntax returnExpression in returnExpressions)
                {
                    if (!IsDefinitelyNonNull(
                            returnExpression,
                            declarationSemanticModel,
                            callbackContext,
                            inspectedValueSources))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                inspectedValueSources.Remove(normalizedTarget);
            }
        }
        /// <summary>
        /// Stores immutable table-field invariant results for one semantic
        /// model.
        /// </summary>
        private sealed class ConditionalWeakTableValueFactCachePartition
        {
            /// <summary>
            /// Synchronizes cache access without holding the lock during
            /// Roslyn analysis.
            /// </summary>
            private readonly object gate = new();

            /// <summary>
            /// Stores results by Roslyn field identity.
            /// </summary>
            private readonly Dictionary<ISymbol, bool> entries =
                new(SymbolEqualityComparer.Default);

            /// <summary>
            /// Attempts to retrieve one cached invariant result.
            /// </summary>
            /// <param name="field">The normalized table field.</param>
            /// <param name="result">
            /// The cached result when present; otherwise the default value.
            /// </param>
            /// <returns>
            /// <see langword="true"/> when the field has a cached result;
            /// otherwise <see langword="false"/>.
            /// </returns>
            public bool TryGetValue(ISymbol field, out bool result)
            {
                lock (gate)
                {
                    return entries.TryGetValue(field, out result);
                }
            }

            /// <summary>
            /// Stores one immutable field-invariant result.
            /// </summary>
            /// <param name="field">The normalized table field.</param>
            /// <param name="result">The invariant result to store.</param>
            public void Store(ISymbol field, bool result)
            {
                lock (gate)
                {
                    entries.TryAdd(field, result);
                }
            }
        }
    }
}
