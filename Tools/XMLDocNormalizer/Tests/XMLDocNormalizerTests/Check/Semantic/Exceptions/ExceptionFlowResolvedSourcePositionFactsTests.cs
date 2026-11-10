using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception;

/// <summary>Protects local binding reuse without sharing flow-sensitive value facts.</summary>
public sealed class ExceptionFlowResolvedSourcePositionFactsTests
{
    private const string Source = """
        using Microsoft.CodeAnalysis;
        using Microsoft.CodeAnalysis.Text;
        public static class Example
        {
            public static void M(SyntaxTree tree, int position)
            {
                TextSpan span = new(position, length: 1);
                FileLinePositionSpan lineSpan = tree.GetLineSpan(span);
                int line = lineSpan.StartLinePosition.Line + 1;
                Use((line));
                line = position;
                Use(line);
            }
            private static void Use(int value) { }
        }
        """;
    private sealed record Fixture(CSharpCompilation Compilation, SemanticModel Model, IMethodSymbol Method, ExpressionSyntax[] Arguments);

    /// <summary>Retains the unique original four-argument reflection entry.</summary>
    [Fact]
    public void OriginalEntry_RemainsUniqueWithFourParameters()
    {
        MethodInfo method = typeof(ExceptionFlowSourcePositionValueFactsProvider).GetMethod(
            "GetOneBasedSourcePositionValueFacts", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.NotNull(method);
        Assert.Equal(4, method.GetParameters().Length);
    }

    /// <summary>Preserves positive source evidence and invalidation after a write.</summary>
    [Fact]
    public void ResolvedBinding_PreservesSourcePositionAndSubsequentWrite()
    {
        Fixture fixture = Create(Source);
        ExceptionFlowCallContext context = new(fixture.Method);
        for (int index = 0; index < fixture.Arguments.Length; index++)
        {
            ExpressionSyntax argument = fixture.Arguments[index];
            SymbolInfo binding = fixture.Model.GetSymbolInfo(Unwrap(argument));
            HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
            ExceptionFlowValueFacts resolved = ExceptionFlowSourcePositionValueFactsProvider
                .GetOneBasedSourcePositionValueFactsForResolvedSymbol(argument, fixture.Model, context, guard, binding);
            ExceptionFlowValueFacts original = ExceptionFlowSourcePositionValueFactsProvider
                .GetOneBasedSourcePositionValueFacts(argument, fixture.Model, context, guard);
            Assert.Equal(index == 0 ? ExceptionFlowValueFacts.PositiveInt32 : ExceptionFlowValueFacts.None, resolved);
            Assert.Equal(original, resolved);
            Assert.Empty(guard);
        }
    }

    /// <summary>Does not bypass the mutable recursion guard when a symbol was already inspected.</summary>
    [Fact]
    public void PreexistingGuard_IsHonoredAndUnchanged()
    {
        Fixture fixture = Create(Source);
        ExpressionSyntax argument = fixture.Arguments[0];
        SymbolInfo binding = fixture.Model.GetSymbolInfo(Unwrap(argument));
        ISymbol local = binding.Symbol!;
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default) { local };
        ExceptionFlowValueFacts value = ExceptionFlowSourcePositionValueFactsProvider
            .GetOneBasedSourcePositionValueFactsForResolvedSymbol(argument, fixture.Model, new(fixture.Method), guard, binding);
        Assert.Equal(ExceptionFlowValueFacts.None, value);
        Assert.Same(local, Assert.Single(guard));
    }

    /// <summary>Keeps null, constants and unresolved bindings negative.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("null")]
    [InlineData("Missing")]
    public void NonSourcePositionBinding_RemainsNegative(string expression)
    {
        Fixture fixture = Create("public static class Example { public static void M() { Use(" + expression + "); } private static void Use(object value) { } }");
        ExpressionSyntax argument = fixture.Arguments.Single();
        SymbolInfo binding = fixture.Model.GetSymbolInfo(argument);
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        ExceptionFlowValueFacts value = ExceptionFlowSourcePositionValueFactsProvider
            .GetOneBasedSourcePositionValueFactsForResolvedSymbol(argument, fixture.Model, new(fixture.Method), guard, binding);
        Assert.Equal(ExceptionFlowValueFacts.None, value);
        Assert.Empty(guard);
    }

    /// <summary>Resolves and consumes symbols independently in each model and compiler world.</summary>
    [Fact]
    public void SameSyntaxInDifferentModelsAndCompilations_PreservesOwnership()
    {
        Fixture fixture = Create(Source);
        SemanticModel otherModel = fixture.Compilation.GetSemanticModel(fixture.Model.SyntaxTree, ignoreAccessibility: true);
        CSharpCompilation otherCompilation = fixture.Compilation.WithAssemblyName("OtherOwner");
        SemanticModel otherWorld = otherCompilation.GetSemanticModel(fixture.Model.SyntaxTree);
        ISymbol? previous = null;
        foreach (SemanticModel model in new[] { fixture.Model, otherModel, otherWorld })
        {
            ExpressionSyntax argument = fixture.Arguments[0];
            SymbolInfo binding = model.GetSymbolInfo(Unwrap(argument));
            IMethodSymbol method = model.Compilation.GetTypeByMetadataName("Example")!.GetMembers("M").OfType<IMethodSymbol>().Single();
            ExceptionFlowValueFacts value = ExceptionFlowSourcePositionValueFactsProvider
                .GetOneBasedSourcePositionValueFactsForResolvedSymbol(argument, model, new(method), new(SymbolEqualityComparer.Default), binding);
            Assert.Equal(ExceptionFlowValueFacts.PositiveInt32, value);
            Assert.True(SymbolEqualityComparer.Default.Equals(binding.Symbol!.ContainingAssembly, model.Compilation.Assembly));
            if (previous != null && ReferenceEquals(model, otherWorld))
            {
                Assert.False(SymbolEqualityComparer.Default.Equals(previous, binding.Symbol));
            }
            previous = binding.Symbol;
        }
    }

    /// <summary>Never reuses the final value facts across different call contexts.</summary>
    [Fact]
    public void SameExpression_DifferentCallContextsRetainDifferentFacts()
    {
        Fixture fixture = Create("#nullable enable\npublic static class Example { public static void M(object? input) { Use(input); } private static void Use(object? value) { } }");
        ExpressionSyntax argument = fixture.Arguments.Single();
        ExceptionFlowCallContext unknown = new(fixture.Method);
        ExceptionFlowCallContext nonNull = new(fixture.Method, new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull });
        Assert.False(ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(argument, fixture.Model, unknown).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.True(ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(argument, fixture.Model, nonNull).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.False(ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(argument, fixture.Model, unknown).ContainsAll(ExceptionFlowValueFacts.NonNull));
    }

    /// <summary>Preserves the original model-mismatch exception instead of swallowing it.</summary>
    [Fact]
    public void OriginalEntry_WithWrongModelStillThrows()
    {
        Fixture first = Create(Source), second = Create(Source);
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        Assert.Throws<ArgumentException>(() => ExceptionFlowSourcePositionValueFactsProvider
            .GetOneBasedSourcePositionValueFacts(first.Arguments[0], second.Model, new(second.Method), guard));
        Assert.Throws<ArgumentException>(() => ExceptionFlowContextualFactEvaluator
            .GetExpressionValueFacts(first.Arguments[0], second.Model, new(second.Method)));
        Assert.Empty(guard);
    }

    /// <summary>Parallel activations keep call-context-sensitive results separate.</summary>
    [Fact]
    public async Task ParallelActivations_DoNotShareValueFacts()
    {
        Fixture fixture = Create("#nullable enable\npublic static class Example { public static void M(object? input) { Use(input); } private static void Use(object? value) { } }");
        Task<bool>[] calls = Enumerable.Range(0, 16).Select(index => Task.Run(() =>
        {
            ExceptionFlowCallContext context = index % 2 == 0
                ? new(fixture.Method)
                : new(fixture.Method, new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull });
            return ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(fixture.Arguments.Single(), fixture.Model, context)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
        })).ToArray();
        bool[] facts = await Task.WhenAll(calls);
        for (int index = 0; index < facts.Length; index++) { Assert.Equal(index % 2 != 0, facts[index]); }
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized) { expression = parenthesized.Expression; }
        return expression;
    }
    private static Fixture Create(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "BindingReuse.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("Owner" + Guid.NewGuid().ToString("N"), [tree],
            MetadataReferences.Default.Concat([MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location)]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        IMethodSymbol method = compilation.GetTypeByMetadataName("Example")!.GetMembers("M").OfType<IMethodSymbol>().Single();
        ExpressionSyntax[] arguments = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is IdentifierNameSyntax { Identifier.ValueText: "Use" })
            .Select(call => call.ArgumentList.Arguments.Single().Expression).ToArray();
        return new(compilation, model, method, arguments);
    }
}
