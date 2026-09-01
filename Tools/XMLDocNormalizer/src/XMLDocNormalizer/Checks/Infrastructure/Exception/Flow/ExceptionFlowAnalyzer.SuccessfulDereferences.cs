using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact reasoning based on earlier successful runtime
    /// dereferences.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Stores successful-dereference results in weak semantic-model
        /// partitions so cached syntax and symbols cannot outlive their Roslyn
        /// semantic world.
        /// </summary>
        private static readonly ConditionalWeakTable<
            SemanticModel,
            SuccessfulDereferenceCachePartition> successfulDereferenceCaches =
                new();

        /// <summary>
        /// Gets facts proven for a local or parameter because execution has
        /// already continued past an earlier statement or entered a nested
        /// construct after successful evaluation that necessarily dereferenced
        /// the same symbol.
        /// </summary>
        /// <param name="expression">
        /// The later symbol expression being evaluated.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.NonNull"/> if an earlier
        /// successful dereference proves the symbol non-null; otherwise
        /// <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetFactsProvenByPrecedingSuccessfulDereference(
                ExpressionSyntax expression,
                ISymbol symbol,
                SemanticModel semanticModel)
        {
            SuccessfulDereferenceCachePartition cache =
                successfulDereferenceCaches.GetValue(
                    semanticModel,
                    static _ => new SuccessfulDereferenceCachePartition());

            SuccessfulDereferenceCacheKey key =
                new(
                    expression,
                    symbol,
                    SuccessfulDereferenceQueryMode.Symbol);

            if (cache.TryGetValue(key, out ExceptionFlowValueFacts cachedFacts))
            {
                return cachedFacts;
            }

            ExceptionFlowValueFacts facts =
                ComputeFactsProvenByPrecedingSuccessfulDereference(
                    expression,
                    symbol,
                    semanticModel);

            cache.Store(key, facts);
            return facts;
        }

        /// <summary>
        /// Computes facts proven by preceding successful dereferences of a
        /// local or parameter without consulting the memoization cache.
        /// </summary>
        /// <param name="expression">
        /// The later symbol expression being evaluated.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.NonNull"/> if an earlier
        /// successful dereference proves the symbol non-null; otherwise
        /// <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts ComputeFactsProvenByPrecedingSuccessfulDereference(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            StatementSyntax? currentStatement =
                expression.AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .FirstOrDefault();

            if (currentStatement == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            while (currentStatement.Parent
                   is BlockSyntax containingBlock)
            {
                int currentStatementIndex =
                    containingBlock.Statements.IndexOf(
                        currentStatement);

                if (currentStatementIndex < 0)
                {
                    break;
                }

                bool earlierFactsInvalidated =
                    false;

                for (int index = currentStatementIndex - 1;
                     index >= 0;
                     index--)
                {
                    StatementSyntax precedingStatement =
                        containingBlock.Statements[index];

                    if (StatementMayWriteSymbolForDereferenceFacts(
                            precedingStatement,
                            symbol,
                            semanticModel))
                    {
                        earlierFactsInvalidated =
                            true;
                        break;
                    }

                    if (StatementSuccessfulCompletionProvesSymbolNonNull(
                        precedingStatement,
                        symbol,
                        semanticModel))
                    {
                        return ExceptionFlowValueFacts.NonNull;
                    }
                }

                if (earlierFactsInvalidated)
                {
                    break;
                }

                if (EnclosingConditionProvesSuccessfulDereference(
                        containingBlock,
                        symbol,
                        semanticModel))
                {
                    return ExceptionFlowValueFacts.NonNull;
                }

                if (containingBlock.Parent
                        is CommonForEachStatementSyntax forEachStatement &&
                    ExpressionDefinitelyDereferencesSymbol(
                        forEachStatement.Expression,
                        symbol,
                        semanticModel))
                {
                    bool sourceMayWriteSymbol =
                        !TryGetWrittenSymbolsForDereferenceFacts(
                            forEachStatement.Expression,
                            semanticModel,
                            out IReadOnlyCollection<ISymbol> sourceWrittenSymbols)
                        || sourceWrittenSymbols.Any(
                            writtenSymbol =>
                                SymbolEqualityComparer.Default.Equals(
                                    writtenSymbol,
                                    symbol));

                    if (!sourceMayWriteSymbol &&
                        !StatementMayWriteSymbolForDereferenceFacts(
                            forEachStatement.Statement,
                            symbol,
                            semanticModel))
                    {
                        return ExceptionFlowValueFacts.NonNull;
                    }
                }

                currentStatement =
                    GetSafeContainingStatement(
                        containingBlock,
                        symbol,
                        semanticModel);

                if (currentStatement == null)
                {
                    break;
                }
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Gets facts proven for a stable get-only auto-property because execution
        /// has already continued past an earlier statement that necessarily
        /// dereferenced the same property value on the same receiver.
        /// </summary>
        /// <param name="expression">
        /// The later property expression being evaluated.
        /// </param>
        /// <param name="propertySymbol">
        /// The property whose value facts are requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.NonNull"/> when an earlier successful
        /// dereference proves the stable property value non-null for the unchanged
        /// receiver; otherwise <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts GetFactsProvenByPrecedingSuccessfulStablePropertyDereference(
            ExpressionSyntax expression,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel)
        {
            SuccessfulDereferenceCachePartition cache =
                successfulDereferenceCaches.GetValue(
                    semanticModel,
                    static _ => new SuccessfulDereferenceCachePartition());

            SuccessfulDereferenceCacheKey key =
                new(
                    expression,
                    propertySymbol,
                    SuccessfulDereferenceQueryMode.StableProperty);

            if (cache.TryGetValue(key, out ExceptionFlowValueFacts cachedFacts))
            {
                return cachedFacts;
            }

            ExceptionFlowValueFacts facts =
                ComputeFactsProvenByPrecedingSuccessfulStablePropertyDereference(
                    expression,
                    propertySymbol,
                    semanticModel);

            cache.Store(key, facts);
            return facts;
        }

        /// <summary>
        /// Computes facts proven by preceding successful dereferences of a
        /// stable property without consulting the memoization cache.
        /// </summary>
        /// <param name="expression">
        /// The later property expression being evaluated.
        /// </param>
        /// <param name="propertySymbol">
        /// The property whose value facts are requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see cref="ExceptionFlowValueFacts.NonNull"/> when an earlier
        /// successful dereference proves the stable property value non-null for
        /// the unchanged receiver; otherwise
        /// <see cref="ExceptionFlowValueFacts.None"/>.
        /// </returns>
        private static ExceptionFlowValueFacts ComputeFactsProvenByPrecedingSuccessfulStablePropertyDereference(
            ExpressionSyntax expression,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel)
        {
            if (!TryGetStableGetOnlyAutoPropertyReceiverSymbol(
                    expression,
                    propertySymbol,
                    semanticModel,
                    out ISymbol? receiverSymbol)
                || receiverSymbol == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            StatementSyntax? currentStatement = expression.AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault();

            if (currentStatement == null)
            {
                return ExceptionFlowValueFacts.None;
            }

            if (StatementMayWriteSymbolForDereferenceFacts(
                    currentStatement,
                    receiverSymbol,
                    semanticModel))
            {
                return ExceptionFlowValueFacts.None;
            }

            while (currentStatement.Parent is BlockSyntax containingBlock)
            {
                int currentStatementIndex = containingBlock.Statements.IndexOf(currentStatement);

                if (currentStatementIndex < 0)
                {
                    break;
                }

                bool earlierFactsInvalidated = false;

                for (int index = currentStatementIndex - 1; index >= 0; index--)
                {
                    StatementSyntax precedingStatement = containingBlock.Statements[index];

                    if (StatementMayWriteSymbolForDereferenceFacts(
                            precedingStatement,
                            receiverSymbol,
                            semanticModel))
                    {
                        earlierFactsInvalidated = true;
                        break;
                    }

                    if (StatementPropertyReferencesUseReceiver(
                            precedingStatement,
                            propertySymbol,
                            receiverSymbol,
                            semanticModel)
                        && StatementSuccessfulCompletionProvesSymbolNonNull(
                            precedingStatement,
                            propertySymbol,
                            semanticModel))
                    {
                        return ExceptionFlowValueFacts.NonNull;
                    }
                }

                if (earlierFactsInvalidated)
                {
                    break;
                }

                currentStatement = GetSafeContainingStatement(
                    containingBlock,
                    receiverSymbol,
                    semanticModel);

                if (currentStatement == null)
                {
                    break;
                }
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Stores immutable successful-dereference facts for one semantic model.
        /// </summary>
        private sealed class SuccessfulDereferenceCachePartition
        {
            /// <summary>
            /// Synchronizes cache access without holding the lock while an
            /// uncached Roslyn computation is performed.
            /// </summary>
            private readonly object gate = new();

            /// <summary>
            /// Stores results keyed by exact syntax identity, Roslyn symbol
            /// identity, and query mode.
            /// </summary>
            private readonly Dictionary<
                SuccessfulDereferenceCacheKey,
                ExceptionFlowValueFacts> entries =
                    new(SuccessfulDereferenceCacheKeyComparer.Instance);

            /// <summary>
            /// Attempts to get a previously computed immutable fact value.
            /// </summary>
            /// <param name="key">The complete query key.</param>
            /// <param name="facts">
            /// The cached facts when the key exists; otherwise the default
            /// value.
            /// </param>
            /// <returns>
            /// <see langword="true"/> when the cache contains the key;
            /// otherwise <see langword="false"/>.
            /// </returns>
            public bool TryGetValue(SuccessfulDereferenceCacheKey key, out ExceptionFlowValueFacts facts)
            {
                lock (gate)
                {
                    return entries.TryGetValue(key, out facts);
                }
            }

            /// <summary>
            /// Stores one immutable fact value unless another thread already
            /// stored the same complete query key.
            /// </summary>
            /// <param name="key">The complete query key.</param>
            /// <param name="facts">The immutable facts to store.</param>
            public void Store(SuccessfulDereferenceCacheKey key, ExceptionFlowValueFacts facts)
            {
                lock (gate)
                {
                    entries.TryAdd(key, facts);
                }
            }
        }

        /// <summary>
        /// Identifies one successful-dereference query within a semantic-model
        /// partition.
        /// </summary>
        /// <param name="Expression">
        /// The exact later-use expression object.
        /// </param>
        /// <param name="Symbol">
        /// The Roslyn symbol whose successful dereference is inspected.
        /// </param>
        /// <param name="Mode">
        /// The normal-symbol or stable-property query mode.
        /// </param>
        private readonly record struct SuccessfulDereferenceCacheKey(
            ExpressionSyntax Expression,
            ISymbol Symbol,
            SuccessfulDereferenceQueryMode Mode);

        /// <summary>
        /// Compares cache keys by exact syntax identity, Roslyn symbol equality,
        /// and query mode.
        /// </summary>
        private sealed class SuccessfulDereferenceCacheKeyComparer :
            IEqualityComparer<SuccessfulDereferenceCacheKey>
        {
            /// <summary>
            /// Gets the shared stateless comparer instance.
            /// </summary>
            /// <value>The shared stateless comparer instance.</value>
            public static SuccessfulDereferenceCacheKeyComparer Instance { get; } =
                new();

            /// <inheritdoc/>
            public bool Equals(
                SuccessfulDereferenceCacheKey x,
                SuccessfulDereferenceCacheKey y)
            {
                return x.Mode == y.Mode
                    && ReferenceEquals(x.Expression, y.Expression)
                    && SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol);
            }

            /// <inheritdoc/>
            public int GetHashCode(SuccessfulDereferenceCacheKey key)
            {
                return HashCode.Combine(
                    RuntimeHelpers.GetHashCode(key.Expression),
                    SymbolEqualityComparer.Default.GetHashCode(key.Symbol),
                    key.Mode);
            }
        }

        /// <summary>
        /// Distinguishes normal-symbol and stable-property provenance queries.
        /// </summary>
        private enum SuccessfulDereferenceQueryMode
        {
            /// <summary>
            /// A query for a local or parameter symbol.
            /// </summary>
            Symbol,

            /// <summary>
            /// A receiver-sensitive query for a stable property.
            /// </summary>
            StableProperty
        }

        /// <summary>
        /// Determines whether every reference to a specified property in a statement
        /// uses the expected receiver.
        /// </summary>
        /// <param name="statement">
        /// The statement whose property references are inspected.
        /// </param>
        /// <param name="propertySymbol">
        /// The property whose references are inspected.
        /// </param>
        /// <param name="receiverSymbol">
        /// The receiver symbol that every relevant property access must use.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the statement contains at least one reference
        /// to the property and every such reference uses the expected receiver;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool StatementPropertyReferencesUseReceiver(
            StatementSyntax statement,
            IPropertySymbol propertySymbol,
            ISymbol receiverSymbol,
            SemanticModel semanticModel)
        {
            bool foundPropertyReference = false;

            foreach (SimpleNameSyntax name in statement.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(name);

                if (symbolInfo.Symbol is not IPropertySymbol referencedProperty
                    || !SymbolEqualityComparer.Default.Equals(
                        referencedProperty.OriginalDefinition,
                        propertySymbol.OriginalDefinition))
                {
                    continue;
                }

                foundPropertyReference = true;

                if (name.Parent is not MemberAccessExpressionSyntax memberAccess
                    || !ReferenceEquals(memberAccess.Name, name)
                    || !ExpressionReferencesSymbol(
                        memberAccess.Expression,
                        receiverSymbol,
                        semanticModel))
                {
                    return false;
                }
            }

            return foundPropertyReference;
        }

        /// <summary>
        /// Gets stable get-only properties proven non-null for a receiver by earlier
        /// successful dereferences at the current control-flow position.
        /// </summary>
        /// <param name="receiverExpression">
        /// The receiver expression passed to another callable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// The stable properties whose values are proven non-null for the unchanged
        /// receiver.
        /// </returns>
        private static IReadOnlyCollection<IPropertySymbol>
            GetStablePropertiesProvenNonNullByPrecedingSuccessfulDereference(
                ExpressionSyntax receiverExpression,
                SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedReceiver =
                UnwrapParenthesizedExpression(receiverExpression);

            SymbolInfo receiverSymbolInfo = semanticModel.GetSymbolInfo(unwrappedReceiver);

            if (receiverSymbolInfo.Symbol is not ILocalSymbol
                && receiverSymbolInfo.Symbol is not IParameterSymbol)
            {
                return Array.Empty<IPropertySymbol>();
            }

            ISymbol receiverSymbol = receiverSymbolInfo.Symbol;

            StatementSyntax? currentStatement = receiverExpression.AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault();

            if (currentStatement == null
                || StatementMayWriteSymbolForDereferenceFacts(
                    currentStatement,
                    receiverSymbol,
                    semanticModel))
            {
                return Array.Empty<IPropertySymbol>();
            }

            HashSet<IPropertySymbol> provenProperties =
                new(SymbolEqualityComparer.Default);

            while (currentStatement.Parent is BlockSyntax containingBlock)
            {
                int currentStatementIndex = containingBlock.Statements.IndexOf(currentStatement);

                if (currentStatementIndex < 0)
                {
                    break;
                }

                for (int index = currentStatementIndex - 1; index >= 0; index--)
                {
                    StatementSyntax precedingStatement = containingBlock.Statements[index];

                    if (StatementMayWriteSymbolForDereferenceFacts(
                            precedingStatement,
                            receiverSymbol,
                            semanticModel))
                    {
                        return provenProperties;
                    }

                    IEnumerable<MemberAccessExpressionSyntax> propertyAccesses =
                        precedingStatement.DescendantNodes()
                            .OfType<MemberAccessExpressionSyntax>();

                    foreach (MemberAccessExpressionSyntax propertyAccess in propertyAccesses)
                    {
                        SymbolInfo propertySymbolInfo = semanticModel.GetSymbolInfo(
                            propertyAccess.Name);

                        if (propertySymbolInfo.Symbol is not IPropertySymbol propertySymbol)
                        {
                            continue;
                        }

                        if (!TryGetStableGetOnlyAutoPropertyReceiverSymbol(
                                propertyAccess,
                                propertySymbol,
                                semanticModel,
                                out ISymbol? propertyReceiver)
                            || propertyReceiver == null
                            || !SymbolEqualityComparer.Default.Equals(
                                propertyReceiver,
                                receiverSymbol))
                        {
                            continue;
                        }

                        if (!StatementPropertyReferencesUseReceiver(
                                precedingStatement,
                                propertySymbol,
                                receiverSymbol,
                                semanticModel)
                            || !StatementSuccessfulCompletionProvesSymbolNonNull(
                                precedingStatement,
                                propertySymbol,
                                semanticModel))
                        {
                            continue;
                        }

                        provenProperties.Add(propertySymbol.OriginalDefinition);
                    }
                }

                currentStatement = GetSafeContainingStatement(
                    containingBlock,
                    receiverSymbol,
                    semanticModel);

                if (currentStatement == null)
                {
                    break;
                }
            }

            return provenProperties;
        }

        /// <summary>
        /// Determines whether a parameter still refers to the value that was supplied
        /// when the current callable was entered.
        /// </summary>
        /// <param name="expression">
        /// The current parameter use.
        /// </param>
        /// <param name="parameterSymbol">
        /// The parameter whose receiver identity must remain unchanged.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for write analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when no write can replace the parameter value between
        /// callable entry and the current expression; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsParameterValueStillCurrentSinceEntry(
            ExpressionSyntax expression,
            IParameterSymbol parameterSymbol,
            SemanticModel semanticModel)
        {
            StatementSyntax? currentStatement = expression.AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault();

            if (currentStatement == null
                || StatementMayWriteSymbolForDereferenceFacts(
                    currentStatement,
                    parameterSymbol,
                    semanticModel))
            {
                return false;
            }

            while (currentStatement.Parent is BlockSyntax containingBlock)
            {
                int currentStatementIndex = containingBlock.Statements.IndexOf(currentStatement);

                if (currentStatementIndex < 0)
                {
                    return false;
                }

                for (int index = 0; index < currentStatementIndex; index++)
                {
                    if (StatementMayWriteSymbolForDereferenceFacts(
                            containingBlock.Statements[index],
                            parameterSymbol,
                            semanticModel))
                    {
                        return false;
                    }
                }

                if (containingBlock.Parent is BaseMethodDeclarationSyntax
                    || containingBlock.Parent is AccessorDeclarationSyntax
                    || containingBlock.Parent is LocalFunctionStatementSyntax
                    || containingBlock.Parent is AnonymousFunctionExpressionSyntax)
                {
                    return true;
                }

                currentStatement = GetSafeContainingStatement(
                    containingBlock,
                    parameterSymbol,
                    semanticModel);

                if (currentStatement == null)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to resolve the receiver of a stable instance get-only
        /// auto-property access.
        /// </summary>
        /// <param name="expression">
        /// The property-access expression.
        /// </param>
        /// <param name="propertySymbol">
        /// The resolved property symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver resolution.
        /// </param>
        /// <param name="receiverSymbol">
        /// The local or parameter receiver when the access is supported; otherwise
        /// <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the expression accesses a stable get-only
        /// auto-property through a directly trackable local or parameter receiver;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetStableGetOnlyAutoPropertyReceiverSymbol(
            ExpressionSyntax expression,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            out ISymbol? receiverSymbol)
        {
            receiverSymbol = null;

            if (propertySymbol.IsStatic
                || propertySymbol.IsIndexer
                || propertySymbol.SetMethod != null
                || propertySymbol.ReturnsByRef
                || propertySymbol.ReturnsByRefReadonly
                || propertySymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            SyntaxNode propertyNode = propertySymbol.DeclaringSyntaxReferences[0].GetSyntax();

            if (propertyNode is not PropertyDeclarationSyntax propertyDeclaration
                || !IsSupportedGetOnlyAutoProperty(propertyDeclaration))
            {
                return false;
            }

            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            ExpressionSyntax receiverExpression =
                UnwrapParenthesizedExpression(memberAccess.Expression);

            SymbolInfo receiverSymbolInfo = semanticModel.GetSymbolInfo(receiverExpression);

            if (receiverSymbolInfo.Symbol is not ILocalSymbol
                && receiverSymbolInfo.Symbol is not IParameterSymbol)
            {
                return false;
            }

            receiverSymbol = receiverSymbolInfo.Symbol;
            return true;
        }

        /// <summary>
        /// Attempts to resolve a parameter receiver for a stable get-only or
        /// init-only auto-property.
        /// </summary>
        /// <param name="expression">
        /// The property-access expression.
        /// </param>
        /// <param name="propertySymbol">
        /// The accessed property.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for receiver resolution.
        /// </param>
        /// <param name="receiverParameter">
        /// The parameter receiver when the access is supported; otherwise
        /// <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the property is stable and uses a directly
        /// trackable parameter receiver; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetStableCallContextPropertyReceiverParameter(
            ExpressionSyntax expression,
            IPropertySymbol propertySymbol,
            SemanticModel semanticModel,
            out IParameterSymbol? receiverParameter)
        {
            receiverParameter = null;

            if (!IsSupportedStableAutoProperty(propertySymbol))
            {
                return false;
            }

            ExpressionSyntax unwrappedExpression = UnwrapParenthesizedExpression(expression);

            if (unwrappedExpression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            ExpressionSyntax receiverExpression =
                UnwrapParenthesizedExpression(memberAccess.Expression);

            SymbolInfo receiverSymbolInfo = semanticModel.GetSymbolInfo(receiverExpression);

            if (receiverSymbolInfo.Symbol is not IParameterSymbol parameterSymbol)
            {
                return false;
            }

            receiverParameter = parameterSymbol;
            return true;
        }

        /// <summary>
        /// Determines whether entering the supplied block proves a symbol to be
        /// non-null because the enclosing branch condition necessarily
        /// dereferenced that symbol while being evaluated successfully.
        /// </summary>
        /// <param name="block">
        /// The branch body containing the later symbol use.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose non-null fact is requested.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the enclosing condition necessarily
        /// dereferences <paramref name="symbol"/> without writing it; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool EnclosingConditionProvesSuccessfulDereference(
            BlockSyntax block,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax? condition = null;

            if (block.Parent is IfStatementSyntax ifStatement)
            {
                condition =
                    ifStatement.Condition;
            }
            else if (block.Parent is ElseClauseSyntax elseClause &&
                     elseClause.Parent
                         is IfStatementSyntax elseIfStatement)
            {
                condition =
                    elseIfStatement.Condition;
            }

            if (condition == null)
            {
                return false;
            }

            if (!TryGetWrittenSymbolsForDereferenceFacts(
                    condition,
                    semanticModel,
                    out IReadOnlyCollection<ISymbol> writtenSymbols))
            {
                return false;
            }

            if (writtenSymbols.Any(
                    writtenSymbol =>
                        SymbolEqualityComparer.Default.Equals(
                            writtenSymbol,
                            symbol)))
            {
                return false;
            }

            return ExpressionDefinitelyDereferencesSymbol(
                condition,
                symbol,
                semanticModel);
        }

        /// <summary>
        /// Determines conservatively whether a statement may write a
        /// specified symbol.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <param name="symbol">
        /// The symbol whose writes are detected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the statement writes the symbol or the
        /// data-flow analysis is unavailable; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool StatementMayWriteSymbolForDereferenceFacts(
            StatementSyntax statement,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (!TryGetWrittenSymbolsForDereferenceFacts(
                    statement,
                    semanticModel,
                    out IReadOnlyCollection<ISymbol> writtenSymbols))
            {
                return true;
            }

            return writtenSymbols.Any(
                writtenSymbol =>
                    SymbolEqualityComparer.Default.Equals(
                        writtenSymbol,
                        symbol));
        }

        /// <summary>
        /// Tries to obtain the symbols written inside a statement or expression
        /// while treating unavailable or failed Roslyn data-flow analysis
        /// conservatively.
        /// </summary>
        /// <param name="node">
        /// The statement or expression whose data flow is inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for data-flow analysis.
        /// </param>
        /// <param name="writtenSymbols">
        /// The symbols written inside the node when analysis succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when Roslyn successfully produced data-flow
        /// information; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetWrittenSymbolsForDereferenceFacts(
            SyntaxNode node,
            SemanticModel semanticModel,
            out IReadOnlyCollection<ISymbol> writtenSymbols)
        {
            writtenSymbols = Array.Empty<ISymbol>();

            try
            {
                DataFlowAnalysis? dataFlow =
                    node switch
                    {
                        StatementSyntax statement =>
                            semanticModel.AnalyzeDataFlow(statement),
                        ExpressionSyntax expression =>
                            semanticModel.AnalyzeDataFlow(expression),
                        _ => null
                    };

                if (dataFlow?.Succeeded != true)
                {
                    return false;
                }

                writtenSymbols =
                    dataFlow.WrittenInside.ToArray();

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether successful completion of a statement requires a
        /// runtime dereference of a specified symbol.
        /// </summary>
        /// <param name="statement">
        /// The statement to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if normal completion of the statement
        /// requires the symbol to have been non-null; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool StatementDefinitelyDereferencesSymbol(
            StatementSyntax statement,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDeclaration:
                    foreach (VariableDeclaratorSyntax variable
                             in localDeclaration.Declaration.Variables)
                    {
                        if (variable.Initializer != null &&
                            ExpressionDefinitelyDereferencesSymbol(
                                variable.Initializer.Value,
                                symbol,
                                semanticModel))
                        {
                            return true;
                        }
                    }

                    return false;

                case ExpressionStatementSyntax expressionStatement:
                    return ExpressionDefinitelyDereferencesSymbol(
                        expressionStatement.Expression,
                        symbol,
                        semanticModel);

                case IfStatementSyntax ifStatement:
                    return ExpressionDefinitelyDereferencesSymbol(
                        ifStatement.Condition,
                        symbol,
                        semanticModel);

                case SwitchStatementSyntax switchStatement:
                    return ExpressionDefinitelyDereferencesSymbol(
                        switchStatement.Expression,
                        symbol,
                        semanticModel);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether evaluating an expression to completion
        /// necessarily performs a runtime dereference of a specified symbol.
        /// </summary>
        /// <param name="expression">
        /// The expression to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if evaluating the expression necessarily
        /// dereferences the symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ExpressionDefinitelyDereferencesSymbol(
            ExpressionSyntax expression,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(
                    expression);

            switch (unwrappedExpression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    if (IsDirectRuntimeDereference(
                            memberAccess,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                        memberAccess.Expression,
                        symbol,
                        semanticModel);

                case ElementAccessExpressionSyntax elementAccess:
                    if (ExpressionReferencesSymbol(
                            elementAccess.Expression,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    if (ExpressionDefinitelyDereferencesSymbol(
                            elementAccess.Expression,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    return elementAccess.ArgumentList.Arguments.Any(
                        argument =>
                            ExpressionDefinitelyDereferencesSymbol(
                                argument.Expression,
                                symbol,
                                semanticModel));

                case InvocationExpressionSyntax invocation:
                    return InvocationDefinitelyDereferencesSymbol(
                        invocation,
                        symbol,
                        semanticModel);

                case ObjectCreationExpressionSyntax creation:
                    return creation.ArgumentList?.Arguments.Any(
                               argument =>
                                   ExpressionDefinitelyDereferencesSymbol(
                                       argument.Expression,
                                       symbol,
                                       semanticModel)) ==
                           true;

                case ImplicitObjectCreationExpressionSyntax creation:
                    return creation.ArgumentList.Arguments.Any(
                        argument =>
                            ExpressionDefinitelyDereferencesSymbol(
                                argument.Expression,
                                symbol,
                                semanticModel));

                case AssignmentExpressionSyntax assignment:
                    if (assignment.IsKind(
                            SyntaxKind.CoalesceAssignmentExpression))
                    {
                        return ExpressionDefinitelyDereferencesSymbol(
                            assignment.Left,
                            symbol,
                            semanticModel);
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                               assignment.Left,
                               symbol,
                               semanticModel) ||
                           ExpressionDefinitelyDereferencesSymbol(
                               assignment.Right,
                               symbol,
                               semanticModel);

                case BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.IsKind(
                            SyntaxKind.LogicalAndExpression) ||
                        binaryExpression.IsKind(
                            SyntaxKind.LogicalOrExpression) ||
                        binaryExpression.IsKind(
                            SyntaxKind.CoalesceExpression))
                    {
                        return ExpressionDefinitelyDereferencesSymbol(
                            binaryExpression.Left,
                            symbol,
                            semanticModel);
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                               binaryExpression.Left,
                               symbol,
                               semanticModel) ||
                           ExpressionDefinitelyDereferencesSymbol(
                               binaryExpression.Right,
                               symbol,
                               semanticModel);

                case ConditionalExpressionSyntax conditionalExpression:
                    if (ExpressionDefinitelyDereferencesSymbol(
                            conditionalExpression.Condition,
                            symbol,
                            semanticModel))
                    {
                        return true;
                    }

                    return ExpressionDefinitelyDereferencesSymbol(
                               conditionalExpression.WhenTrue,
                               symbol,
                               semanticModel) &&
                           ExpressionDefinitelyDereferencesSymbol(
                               conditionalExpression.WhenFalse,
                               symbol,
                               semanticModel);

                case IsPatternExpressionSyntax isPatternExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        isPatternExpression.Expression,
                        symbol,
                        semanticModel);

                case CastExpressionSyntax castExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        castExpression.Expression,
                        symbol,
                        semanticModel);

                case CheckedExpressionSyntax checkedExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        checkedExpression.Expression,
                        symbol,
                        semanticModel);

                case PrefixUnaryExpressionSyntax prefixExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        prefixExpression.Operand,
                        symbol,
                        semanticModel);

                case PostfixUnaryExpressionSyntax postfixExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        postfixExpression.Operand,
                        symbol,
                        semanticModel);

                case AwaitExpressionSyntax awaitExpression:
                    return ExpressionDefinitelyDereferencesSymbol(
                        awaitExpression.Expression,
                        symbol,
                        semanticModel);

                case InterpolatedStringExpressionSyntax interpolatedString:
                    return interpolatedString.Contents
                        .OfType<InterpolationSyntax>()
                        .Any(
                            interpolation =>
                                ExpressionDefinitelyDereferencesSymbol(
                                    interpolation.Expression,
                                    symbol,
                                    semanticModel));

                case ConditionalAccessExpressionSyntax conditionalAccess:
                    return ExpressionDefinitelyDereferencesSymbol(
                        conditionalAccess.Expression,
                        symbol,
                        semanticModel);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether evaluating an invocation necessarily
        /// dereferences a specified symbol.
        /// </summary>
        /// <param name="invocation">
        /// The invocation to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for method resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the invocation necessarily dereferences
        /// the symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool InvocationDefinitelyDereferencesSymbol(
            InvocationExpressionSyntax invocation,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (invocation.Expression
                    is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText ==
                    "nameof")
            {
                return false;
            }

            if (invocation.Expression
                    is MemberAccessExpressionSyntax memberAccess)
            {
                if (IsDirectRuntimeDereference(
                        memberAccess,
                        symbol,
                        semanticModel))
                {
                    return true;
                }

                if (ExpressionDefinitelyDereferencesSymbol(
                        memberAccess.Expression,
                        symbol,
                        semanticModel))
                {
                    return true;
                }
            }
            else if (ExpressionReferencesSymbol(
                         invocation.Expression,
                         symbol,
                         semanticModel) &&
                     semanticModel.GetTypeInfo(
                         invocation.Expression).Type?.TypeKind ==
                         TypeKind.Delegate)
            {
                return true;
            }

            return invocation.ArgumentList.Arguments.Any(
                argument =>
                    ExpressionDefinitelyDereferencesSymbol(
                        argument.Expression,
                        symbol,
                        semanticModel));
        }

        /// <summary>
        /// Determines whether one member-access expression performs an
        /// instance dereference of a specified symbol.
        /// </summary>
        /// <param name="memberAccess">
        /// The member access to inspect.
        /// </param>
        /// <param name="symbol">
        /// The local or parameter symbol whose dereference is sought.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for member resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for an instance member access on the
        /// symbol; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsDirectRuntimeDereference(
            MemberAccessExpressionSyntax memberAccess,
            ISymbol symbol,
            SemanticModel semanticModel)
        {
            if (!ExpressionReferencesSymbol(
                    memberAccess.Expression,
                    symbol,
                    semanticModel))
            {
                return false;
            }

            SymbolInfo memberSymbolInfo =
                semanticModel.GetSymbolInfo(
                    memberAccess);

            return memberSymbolInfo.Symbol switch
            {
                IMethodSymbol methodSymbol =>
                    !methodSymbol.IsStatic &&
                    methodSymbol.ReducedFrom == null,

                IPropertySymbol propertySymbol =>
                    !propertySymbol.IsStatic,

                IFieldSymbol fieldSymbol =>
                    !fieldSymbol.IsStatic,

                IEventSymbol eventSymbol =>
                    !eventSymbol.IsStatic,

                _ => false
            };
        }
    }
}
