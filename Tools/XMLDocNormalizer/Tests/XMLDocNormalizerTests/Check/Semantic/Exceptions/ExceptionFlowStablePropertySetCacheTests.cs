using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception;

/// <summary>Protects exact identity, immutable results and bounded weak-model property-set memoization.</summary>
public sealed class ExceptionFlowStablePropertySetCacheTests
{
    private const string Source = """
        #nullable enable
        public sealed class Holder { public object? Value { get; } }
        public static class Example
        {
            public static void M(Holder first, Holder second)
            {
                _ = first.Value.ToString();
                Use(first);
                Use(second);
            }
            private static void Use(Holder value) { }
        }
        """;
    private sealed record Context(CSharpCompilation Compilation, SemanticModel Model, ExpressionSyntax[] Receivers);

    /// <summary>Reuses the same immutable positive result for the exact query.</summary>
    [Fact]
    public void IdenticalPositiveQuery_ReusesSnapshotAndMatchesUncachedOrder()
    {
        Context context = Create(Source);
        IReadOnlyCollection<IPropertySymbol> expected = Uncached(context.Receivers[0], context.Model);
        IReadOnlyCollection<IPropertySymbol> first = Query(context.Receivers[0], context.Model);
        IReadOnlyCollection<IPropertySymbol> second = Query(context.Receivers[0], context.Model);
        Assert.Single(first);
        Assert.Equal(expected.ToArray(), first.ToArray(), SymbolEqualityComparer.Default);
        Assert.Same(first, second);
        Assert.Single(Entries(context.Model));
    }

    /// <summary>Publishes an immutable collection instead of a shared mutable HashSet.</summary>
    [Fact]
    public void PositiveSnapshot_RejectsCallerMutation()
    {
        Context context = Create(Source);
        IReadOnlyCollection<IPropertySymbol> result = Query(context.Receivers[0], context.Model);
        ICollection<IPropertySymbol> collection = Assert.IsAssignableFrom<ICollection<IPropertySymbol>>(result);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
        Assert.Single(Query(context.Receivers[0], context.Model));
    }

    /// <summary>Caches negative results without conflating two receiver syntax objects.</summary>
    [Fact]
    public void DifferentReceivers_PositiveAndNegativeQueriesHaveSeparateEntries()
    {
        Context context = Create(Source);
        Assert.Single(Query(context.Receivers[0], context.Model));
        IReadOnlyCollection<IPropertySymbol> empty = Query(context.Receivers[1], context.Model);
        Assert.Empty(empty);
        Assert.Same(empty, Query(context.Receivers[1], context.Model));
        Assert.Equal(2, Entries(context.Model).Count);
    }

    /// <summary>Uses syntax object identity even for two occurrences of the same local.</summary>
    [Fact]
    public void SameReceiverTextAtDifferentPositions_UsesDistinctEntries()
    {
        Context context = Create(Source.Replace("Use(second);", "Use(first);", StringComparison.Ordinal));
        Assert.NotSame(context.Receivers[0], context.Receivers[1]);
        Assert.Single(Query(context.Receivers[0], context.Model));
        Assert.Single(Query(context.Receivers[1], context.Model));
        Assert.Equal(2, Entries(context.Model).Count);
    }

    /// <summary>Does not share positive snapshots between semantic-model instances.</summary>
    [Fact]
    public void SameTreeInDifferentModels_HasSeparatePartitions()
    {
        Context context = Create(Source);
        SemanticModel other = context.Compilation.GetSemanticModel(context.Model.SyntaxTree, ignoreAccessibility: true);
        Assert.NotSame(context.Model, other);
        Assert.NotSame(Query(context.Receivers[0], context.Model), Query(context.Receivers[0], other));
        Assert.NotSame(Entries(context.Model), Entries(other));
        Assert.Single(Entries(context.Model));
        Assert.Single(Entries(other));
    }

    /// <summary>Keeps symbol ownership separate for the same tree in different compilations.</summary>
    [Fact]
    public void SharedTreeInDifferentCompilations_PreservesSymbolOwners()
    {
        Context context = Create(Source);
        CSharpCompilation otherCompilation = Compilation("OtherOwner", context.Model.SyntaxTree);
        SemanticModel other = otherCompilation.GetSemanticModel(context.Model.SyntaxTree);
        IPropertySymbol first = Assert.Single(Query(context.Receivers[0], context.Model));
        IPropertySymbol second = Assert.Single(Query(context.Receivers[0], other));
        Assert.True(SymbolEqualityComparer.Default.Equals(first.ContainingAssembly, context.Compilation.Assembly));
        Assert.True(SymbolEqualityComparer.Default.Equals(second.ContainingAssembly, otherCompilation.Assembly));
        Assert.False(SymbolEqualityComparer.Default.Equals(first, second));
        Assert.NotSame(Entries(context.Model), Entries(other));
    }

    /// <summary>Preserves receiver reassignment invalidation.</summary>
    [Fact]
    public void ReceiverReassignment_RemainsUnproven()
    {
        Context context = Create(Source.Replace("Use(first);", "first = second; Use(first);", StringComparison.Ordinal));
        Assert.Empty(Query(context.Receivers[0], context.Model));
        Assert.Empty(Uncached(context.Receivers[0], context.Model));
    }

    /// <summary>Does not transfer a different alias's preceding dereference.</summary>
    [Fact]
    public void AliasDereference_DoesNotInventReceiverProof()
    {
        Context context = Create(Source.Replace("_ = first.Value.ToString();", "Holder alias = first; _ = alias.Value.ToString();", StringComparison.Ordinal));
        Assert.Empty(Query(context.Receivers[0], context.Model));
        Assert.Empty(Uncached(context.Receivers[0], context.Model));
    }

    /// <summary>Preserves writes and nested control-flow results at each position.</summary>
    [Fact]
    public void NestedBlocksAndWrites_MatchUncachedSets()
    {
        string source = Source.Replace("Use(first);", "if (first != null) { { Use(first); } } first = second; Use(first);", StringComparison.Ordinal);
        Context context = Create(source);
        foreach (ExpressionSyntax receiver in context.Receivers)
        {
            IReadOnlyCollection<IPropertySymbol> expected = Uncached(receiver, context.Model);
            IReadOnlyCollection<IPropertySymbol> actual = Query(receiver, context.Model);
            Assert.Equal(expected.ToArray(), actual.ToArray(), SymbolEqualityComparer.Default);
            Assert.Equal(expected.ToArray(), Query(receiver, context.Model).ToArray(), SymbolEqualityComparer.Default);
        }
        Assert.Single(Query(context.Receivers[0], context.Model));
        Assert.Empty(Query(context.Receivers[1], context.Model));
    }

    /// <summary>Rejects mutable or overridable properties exactly as the original computation does.</summary>
    [Theory]
    [InlineData("public object? Value { get; set; }")]
    [InlineData("public object? Value => new object();")]
    public void NonStableProperty_RemainsUnproven(string declaration)
    {
        Context context = Create(Source.Replace("public object? Value { get; }", declaration, StringComparison.Ordinal));
        Assert.Empty(Query(context.Receivers[0], context.Model));
        Assert.Empty(Uncached(context.Receivers[0], context.Model));
    }

    /// <summary>Does not cache exceptions from a mismatched semantic model.</summary>
    [Fact]
    public void MismatchedSyntaxAndModel_DoesNotPublishAnEntry()
    {
        Context first = Create(Source); Context second = Create(Source);
        Assert.Throws<ArgumentException>(() => Query(first.Receivers[0], second.Model));
        Assert.Throws<ArgumentException>(() => Query(first.Receivers[0], second.Model));
        Assert.Empty(Entries(second.Model));
    }

    /// <summary>Allows concurrent computations but publishes one consistent immutable result.</summary>
    [Fact]
    public async Task ParallelExactQueries_PublishOneSnapshot()
    {
        Context context = Create(Source);
        Task<IReadOnlyCollection<IPropertySymbol>>[] tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => Query(context.Receivers[0], context.Model))).ToArray();
        IReadOnlyCollection<IPropertySymbol>[] values = await Task.WhenAll(tasks);
        Assert.All(values, value => { Assert.Single(value); Assert.Same(values[0], value); });
        Assert.Single(Entries(context.Model));
    }

    /// <summary>Bounds stored receiver entries while preserving results after the bound.</summary>
    [Fact]
    public void ReceiverEntryLimit_DoesNotChangeUncachedQueries()
    {
        Context context = Create(Source.Replace("Use(first);", string.Concat(Enumerable.Repeat("Use(first);", 258)), StringComparison.Ordinal));
        foreach (ExpressionSyntax receiver in context.Receivers) { Query(receiver, context.Model); }
        Assert.Equal(256, Entries(context.Model).Count);
        Assert.Single(Query(context.Receivers[^2], context.Model));
        Assert.Equal(256, Entries(context.Model).Count);
    }

    /// <summary>Does not retain unbounded symbol arrays for a single receiver.</summary>
    [Fact]
    public void OversizedPropertySet_IsNotCached()
    {
        string properties = string.Join(" ", Enumerable.Range(0, 65).Select(index => "public object? P" + index + " { get; }"));
        string uses = string.Join(" ", Enumerable.Range(0, 65).Select(index => "_ = first.P" + index + ".ToString();"));
        string source = Source.Replace("public object? Value { get; }", properties, StringComparison.Ordinal)
            .Replace("_ = first.Value.ToString();", uses, StringComparison.Ordinal);
        Context context = Create(source);
        Assert.Equal(65, Query(context.Receivers[0], context.Model).Count);
        Assert.Empty(Entries(context.Model));
        Assert.Equal(65, Query(context.Receivers[0], context.Model).Count);
    }

    /// <summary>Does not keep an otherwise unreachable semantic world alive.</summary>
    [Fact]
    public void WeakPartition_DoesNotRootReleasedSemanticModel()
    {
        WeakReference reference = ReleasedModel();
        for (int attempt = 0; attempt < 8 && reference.IsAlive; attempt++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReleasedModel()
    {
        Context context = Create(Source); Query(context.Receivers[0], context.Model);
        return new WeakReference(context.Model);
    }
    private static Context Create(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "StablePropertySet.cs");
        CSharpCompilation compilation = Compilation("Owner" + Guid.NewGuid().ToString("N"), tree);
        SemanticModel model = compilation.GetSemanticModel(tree);
        ExpressionSyntax[] receivers = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "Use" })
            .Select(invocation => invocation.ArgumentList.Arguments.Single().Expression).ToArray();
        return new(compilation, model, receivers);
    }
    private static CSharpCompilation Compilation(string name, SyntaxTree tree)
        => CSharpCompilation.Create(name, [tree], MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    private static IReadOnlyCollection<IPropertySymbol> Query(ExpressionSyntax expression, SemanticModel model)
        => ExceptionFlowDereferenceFactDiscovery.GetStablePropertiesProvenNonNullByPrecedingSuccessfulDereference(expression, model);
    private static IReadOnlyCollection<IPropertySymbol> Uncached(ExpressionSyntax expression, SemanticModel model)
        => (IReadOnlyCollection<IPropertySymbol>)typeof(ExceptionFlowDereferenceFactDiscovery)
            .GetMethod("ComputeStablePropertiesProvenNonNullByPrecedingSuccessfulDereference", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [expression, model])!;
    private static IDictionary Entries(SemanticModel model)
    {
        object table = typeof(ExceptionFlowDereferenceFactDiscovery).GetField("successfulDereferenceCaches", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        object?[] arguments = [model, null]; Assert.True((bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, arguments)!);
        return (IDictionary)arguments[1]!.GetType().GetField("stablePropertySets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(arguments[1])!;
    }
}
