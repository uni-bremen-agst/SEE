using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains exact, semantic-model-scoped caching for Roslyn data-flow
    /// facts used by exception-flow provenance checks.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Reuses immutable data-flow facts without strongly retaining semantic
        /// models after their Roslyn semantic worlds become unreachable.
        /// </summary>
        private static readonly DataFlowFactCache dataFlowFactCache =
            new();

        /// <summary>
        /// Gets the cached data-flow facts for one exact statement region.
        /// </summary>
        /// <param name="statement">The exact statement region.</param>
        /// <param name="semanticModel">
        /// The semantic model defining the region's semantic world.
        /// </param>
        /// <returns>The immutable facts observed by exception-flow analysis.</returns>
        internal static ExceptionFlowDataFlowFacts GetDataFlowFacts(
            StatementSyntax statement,
            SemanticModel semanticModel)
        {
            return dataFlowFactCache.GetFacts(statement, semanticModel);
        }

        /// <summary>
        /// Gets the cached data-flow facts for one exact expression region.
        /// </summary>
        /// <param name="expression">The exact expression region.</param>
        /// <param name="semanticModel">
        /// The semantic model defining the region's semantic world.
        /// </param>
        /// <returns>The immutable facts observed by exception-flow analysis.</returns>
        internal static ExceptionFlowDataFlowFacts GetDataFlowFacts(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            return dataFlowFactCache.GetFacts(expression, semanticModel);
        }

        /// <summary>
        /// Computes the immutable facts required from one Roslyn data-flow
        /// analysis without consulting the cache.
        /// </summary>
        /// <param name="key">The exact region and overload kind.</param>
        /// <param name="semanticModel">
        /// The semantic model defining the region's semantic world.
        /// </param>
        /// <returns>
        /// The successful analysis and its written symbols, or an unsuccessful
        /// snapshot when Roslyn cannot analyze the region.
        /// </returns>
        private static ExceptionFlowDataFlowFacts ComputeDataFlowFacts(
            DataFlowRegionKey key,
            SemanticModel semanticModel)
        {
            DataFlowAnalysis? analysis =
                key.Kind switch
                {
                    DataFlowRegionKind.Statement =>
                        semanticModel.AnalyzeDataFlow((StatementSyntax)key.Region),
                    DataFlowRegionKind.Expression =>
                        semanticModel.AnalyzeDataFlow((ExpressionSyntax)key.Region),
                    _ => null
                };

            if (analysis?.Succeeded != true)
            {
                return ExceptionFlowDataFlowFacts.Unsuccessful;
            }

            return new ExceptionFlowDataFlowFacts(
                succeeded: true,
                analysis.WrittenInside.ToImmutableArray());
        }

        /// <summary>
        /// Stores weak semantic-model partitions for exact data-flow regions.
        /// </summary>
        internal sealed class DataFlowFactCache
        {
            /// <summary>
            /// Weakly partitions entries by their complete Roslyn semantic
            /// world.
            /// </summary>
            private readonly ConditionalWeakTable<
                SemanticModel,
                DataFlowFactCachePartition> partitions = new();

            /// <summary>
            /// Computes uncached facts for one exact region.
            /// </summary>
            private readonly DataFlowFactCalculator calculator;

            /// <summary>
            /// Creates partitions once for their exact semantic models.
            /// </summary>
            private readonly ConditionalWeakTable<
                SemanticModel,
                DataFlowFactCachePartition>.CreateValueCallback
                    partitionFactory;

            /// <summary>
            /// Initializes a production cache that computes the exact Roslyn
            /// facts consumed by exception-flow analysis.
            /// </summary>
            internal DataFlowFactCache()
                : this(ComputeDataFlowFacts)
            {
            }

            /// <summary>
            /// Initializes a cache with the supplied uncached fact calculator.
            /// </summary>
            /// <param name="calculator">
            /// The calculator invoked once for every cache miss.
            /// </param>
            internal DataFlowFactCache(DataFlowFactCalculator calculator)
            {
                this.calculator = calculator;
                partitionFactory = CreatePartition;
            }

            /// <summary>
            /// Gets facts for one exact statement region.
            /// </summary>
            /// <param name="statement">The exact statement object.</param>
            /// <param name="semanticModel">
            /// The semantic model defining the statement's semantic world.
            /// </param>
            /// <returns>The cached or newly computed immutable facts.</returns>
            internal ExceptionFlowDataFlowFacts GetFacts(
                StatementSyntax statement,
                SemanticModel semanticModel)
            {
                return GetPartition(semanticModel).GetFacts(statement);
            }

            /// <summary>
            /// Gets facts for one exact expression region.
            /// </summary>
            /// <param name="expression">The exact expression object.</param>
            /// <param name="semanticModel">
            /// The semantic model defining the expression's semantic world.
            /// </param>
            /// <returns>The cached or newly computed immutable facts.</returns>
            internal ExceptionFlowDataFlowFacts GetFacts(
                ExpressionSyntax expression,
                SemanticModel semanticModel)
            {
                return GetPartition(semanticModel).GetFacts(expression);
            }

            /// <summary>
            /// Gets the number of entries stored for one semantic model.
            /// </summary>
            /// <param name="semanticModel">The semantic model to inspect.</param>
            /// <returns>
            /// The partition entry count, or zero when no partition exists.
            /// </returns>
            internal int GetEntryCount(SemanticModel semanticModel)
            {
                return partitions.TryGetValue(
                    semanticModel,
                    out DataFlowFactCachePartition? partition)
                        ? partition.Count
                        : 0;
            }

            /// <summary>
            /// Gets the partition bound to one exact semantic model.
            /// </summary>
            /// <param name="semanticModel">
            /// The semantic model owning the weak cache partition.
            /// </param>
            /// <returns>The existing or newly created partition.</returns>
            private DataFlowFactCachePartition GetPartition(
                SemanticModel semanticModel)
            {
                return partitions.GetValue(
                    semanticModel,
                    partitionFactory);
            }

            /// <summary>
            /// Creates a partition permanently bound to its semantic model and
            /// this cache's immutable calculator.
            /// </summary>
            /// <param name="semanticModel">
            /// The semantic model supplied as the weak-table key.
            /// </param>
            /// <returns>The new semantic-model partition.</returns>
            private DataFlowFactCachePartition CreatePartition(
                SemanticModel semanticModel)
            {
                return new DataFlowFactCachePartition(
                    semanticModel,
                    calculator);
            }
        }

        /// <summary>
        /// Stores immutable data-flow facts for one semantic model.
        /// </summary>
        private sealed class DataFlowFactCachePartition
        {
            /// <summary>
            /// The semantic model defining every region in this partition.
            /// </summary>
            private readonly SemanticModel semanticModel;

            /// <summary>
            /// Computes uncached facts for this partition.
            /// </summary>
            private readonly DataFlowFactCalculator calculator;

            /// <summary>
            /// Synchronizes lookup and first computation for a partition.
            /// </summary>
            private readonly object gate = new();

            /// <summary>
            /// Stores facts by exact syntax identity and Roslyn overload kind.
            /// </summary>
            private readonly Dictionary<
                DataFlowRegionKey,
                ExceptionFlowDataFlowFacts> entries =
                    new(DataFlowRegionKeyComparer.Instance);

            /// <summary>
            /// Initializes a partition bound to one semantic model and one
            /// immutable calculator.
            /// </summary>
            /// <param name="semanticModel">
            /// The semantic model defining every region in the partition.
            /// </param>
            /// <param name="calculator">
            /// The uncached calculator for the partition.
            /// </param>
            internal DataFlowFactCachePartition(
                SemanticModel semanticModel,
                DataFlowFactCalculator calculator)
            {
                this.semanticModel = semanticModel;
                this.calculator = calculator;
            }

            /// <summary>
            /// Gets the current number of cached regions.
            /// </summary>
            /// <value>The number of exact region entries.</value>
            internal int Count
            {
                get
                {
                    lock (gate)
                    {
                        return entries.Count;
                    }
                }
            }

            /// <summary>
            /// Gets or atomically computes facts for one statement region.
            /// </summary>
            /// <param name="statement">The exact statement object.</param>
            /// <returns>The cached or newly computed facts.</returns>
            internal ExceptionFlowDataFlowFacts GetFacts(
                StatementSyntax statement)
            {
                return GetOrAdd(DataFlowRegionKey.ForStatement(statement));
            }

            /// <summary>
            /// Gets or atomically computes facts for one expression region.
            /// </summary>
            /// <param name="expression">The exact expression object.</param>
            /// <returns>The cached or newly computed facts.</returns>
            internal ExceptionFlowDataFlowFacts GetFacts(
                ExpressionSyntax expression)
            {
                return GetOrAdd(DataFlowRegionKey.ForExpression(expression));
            }

            /// <summary>
            /// Gets or atomically computes immutable facts for one region.
            /// </summary>
            /// <param name="key">The exact region and overload kind.</param>
            /// <returns>The cached or newly computed facts.</returns>
            private ExceptionFlowDataFlowFacts GetOrAdd(
                DataFlowRegionKey key)
            {
                lock (gate)
                {
                    if (entries.TryGetValue(
                            key,
                            out ExceptionFlowDataFlowFacts facts))
                    {
                        return facts;
                    }

                    facts = calculator(key, semanticModel);
                    entries.Add(key, facts);
                    return facts;
                }
            }
        }

        /// <summary>
        /// Identifies one exact Roslyn data-flow region within a semantic-model
        /// partition.
        /// </summary>
        internal readonly struct DataFlowRegionKey
        {
            /// <summary>
            /// Initializes a key whose region and overload kind were selected
            /// by a typed factory.
            /// </summary>
            /// <param name="region">The exact syntax object.</param>
            /// <param name="kind">The matching Roslyn overload kind.</param>
            private DataFlowRegionKey(
                SyntaxNode region,
                DataFlowRegionKind kind)
            {
                Region = region;
                Kind = kind;
            }

            /// <summary>
            /// Gets the exact syntax object.
            /// </summary>
            /// <value>The syntax object identified by reference.</value>
            internal SyntaxNode Region { get; }

            /// <summary>
            /// Gets the selected Roslyn overload kind.
            /// </summary>
            /// <value>The statement or expression overload kind.</value>
            internal DataFlowRegionKind Kind { get; }

            /// <summary>
            /// Creates a key for the single-statement Roslyn overload.
            /// </summary>
            /// <param name="statement">The exact statement object.</param>
            /// <returns>The exact statement-region key.</returns>
            internal static DataFlowRegionKey ForStatement(
                StatementSyntax statement)
            {
                return new DataFlowRegionKey(
                    statement,
                    DataFlowRegionKind.Statement);
            }

            /// <summary>
            /// Creates a key for the single-expression Roslyn overload.
            /// </summary>
            /// <param name="expression">The exact expression object.</param>
            /// <returns>The exact expression-region key.</returns>
            internal static DataFlowRegionKey ForExpression(
                ExpressionSyntax expression)
            {
                return new DataFlowRegionKey(
                    expression,
                    DataFlowRegionKind.Expression);
            }
        }

        /// <summary>
        /// Compares data-flow region keys by syntax object identity and overload
        /// kind.
        /// </summary>
        private sealed class DataFlowRegionKeyComparer :
            IEqualityComparer<DataFlowRegionKey>
        {
            /// <summary>
            /// Gets the shared stateless comparer.
            /// </summary>
            /// <value>The shared stateless comparer.</value>
            internal static DataFlowRegionKeyComparer Instance { get; } = new();

            /// <inheritdoc/>
            public bool Equals(DataFlowRegionKey x, DataFlowRegionKey y)
            {
                return x.Kind == y.Kind
                    && ReferenceEquals(x.Region, y.Region);
            }

            /// <inheritdoc/>
            public int GetHashCode(DataFlowRegionKey key)
            {
                return HashCode.Combine(
                    RuntimeHelpers.GetHashCode(key.Region),
                    key.Kind);
            }
        }

        /// <summary>
        /// Distinguishes Roslyn statement and expression data-flow overloads.
        /// </summary>
        internal enum DataFlowRegionKind
        {
            /// <summary>
            /// A single statement region.
            /// </summary>
            Statement,

            /// <summary>
            /// A single expression region.
            /// </summary>
            Expression
        }

        /// <summary>
        /// Computes uncached facts for one complete data-flow region key.
        /// </summary>
        /// <param name="key">The exact region and overload kind.</param>
        /// <param name="semanticModel">
        /// The semantic model defining the region's semantic world.
        /// </param>
        /// <returns>The immutable data-flow facts.</returns>
        internal delegate ExceptionFlowDataFlowFacts DataFlowFactCalculator(
            DataFlowRegionKey key,
            SemanticModel semanticModel);

        /// <summary>
        /// Contains the exact immutable Roslyn data-flow facts consumed by
        /// exception-flow analysis.
        /// </summary>
        internal readonly struct ExceptionFlowDataFlowFacts
        {
            /// <summary>
            /// Initializes an immutable data-flow fact snapshot.
            /// </summary>
            /// <param name="succeeded">
            /// Whether Roslyn successfully analyzed the region.
            /// </param>
            /// <param name="writtenInside">
            /// The written symbols in their original Roslyn order.
            /// </param>
            internal ExceptionFlowDataFlowFacts(
                bool succeeded,
                ImmutableArray<ISymbol> writtenInside)
            {
                Succeeded = succeeded;
                WrittenInside = writtenInside;
            }

            /// <summary>
            /// Gets an unsuccessful snapshot with no observed symbols.
            /// </summary>
            /// <value>The shared unsuccessful value.</value>
            internal static ExceptionFlowDataFlowFacts Unsuccessful { get; } =
                new(
                    succeeded: false,
                    ImmutableArray<ISymbol>.Empty);

            /// <summary>
            /// Gets whether Roslyn successfully analyzed the region.
            /// </summary>
            /// <value>
            /// <see langword="true"/> for a successful analysis; otherwise
            /// <see langword="false"/>.
            /// </value>
            internal bool Succeeded { get; }

            /// <summary>
            /// Gets the symbols written inside the exact region.
            /// </summary>
            /// <value>
            /// The symbols in their original Roslyn enumeration order, or an
            /// empty array when analysis was unsuccessful.
            /// </value>
            internal ImmutableArray<ISymbol> WrittenInside { get; }
        }
    }
}
