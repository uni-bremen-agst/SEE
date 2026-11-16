using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Check.Semantic.Exception;

/// <summary>Exercises source-return facts across exact tree, context and guard boundaries.</summary>
public sealed class ExceptionFlowSourceReturnSemanticModelReuseTests(ITestOutputHelper output)
{
    private delegate bool ReturnQuery(InvocationExpressionSyntax invocation, SemanticModel model,
        ExceptionFlowCallContext context, HashSet<ISymbol> guard, out ExceptionFlowValueFacts facts);

    private static readonly ReturnQuery Query = typeof(ExceptionFlowContextualFactEvaluator)
        .GetMethod("TryGetSourceInvocationReturnValueFacts", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<ReturnQuery>();

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void ForeignTree_PositiveReturnsAndLastNullCounterexampleReachTheEnd(int count)
    {
        Fixture positive = Create(Returns(count));
        Fixture lastNull = Create(Returns(count, nullAt: count - 1));
        Assert.NotSame(positive.Invocation.SyntaxTree, positive.Selected.DeclaringSyntaxReferences.Single().SyntaxTree);
        Assert.True(positive.Compilation.ContainsSyntaxTree(positive.Selected.DeclaringSyntaxReferences.Single().SyntaxTree));
        List<ExpressionSyntax> expressions = ReturnExpressions(positive);
        Assert.Equal(count, expressions.Count);
        Assert.All(expressions, expression => Assert.Same(expressions[0].SyntaxTree, expression.SyntaxTree));
        AssertNonNull(Evaluate(positive));
        // The earlier returns are unchanged and individually positive. Only evaluating
        // the final null can erase their common NonNull fact in the existing equation.
        Assert.All(expressions, expression => Assert.True(ExceptionFlowContextualFactEvaluator
            .GetExpressionValueFacts(expression, positive.Compilation.GetSemanticModel(expression.SyntaxTree),
                new(positive.Selected)).ContainsAll(ExceptionFlowValueFacts.NonNull)));
        AssertUnknown(Evaluate(lastNull));
        output.WriteLine("PERF06B_REACHED " + JsonSerializer.Serialize(new
        {
            returns = count, foreignTree = true, positive = true, lastNullChangesResult = true,
            evidence = "Functional paired fixtures; request counts require the separate source/control-flow proof"
        }));
    }

    [Theory]
    [InlineData("{ return new object(); }")]
    [InlineData("=> new object();")]
    public void SingleReturnAndExpressionBodyRemainPositive(string body)
    {
        AssertNonNull(Evaluate(Create(body)));
    }

    [Fact]
    public void SameTreeUsesTheCallerModelIncludingAccessibilityOptions()
    {
        Fixture fixture = Create(Returns(8), sameTree: true);
        SemanticModel model = fixture.Compilation.GetSemanticModel(fixture.Invocation.SyntaxTree, ignoreAccessibility: true);
        Assert.Same(model, ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(model, ReturnExpressions(fixture)[0].SyntaxTree));
        AssertNonNull(Evaluate(fixture, model: model));
    }

    [Fact]
    public void CallerContextsRemainSeparateForEveryReturn()
    {
        Fixture fixture = Create(Returns(3, expression: "input"));
        ExceptionFlowCallContext unknown = new(fixture.Caller);
        ExceptionFlowCallContext known = new(fixture.Caller,
            new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull });
        AssertUnknown(Evaluate(fixture, unknown));
        AssertNonNull(Evaluate(fixture, known));
        AssertUnknown(Evaluate(fixture, unknown));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void EarlyAndLateNullEraseTheCommonFact(int nullAt)
    {
        AssertUnknown(Evaluate(Create(Returns(4, nullAt))));
    }

    [Theory]
    [InlineData("{ return input; }")]
    [InlineData("{ object? local = input; local = null; return local; }")]
    [InlineData("{ return index == 0 ? new object() : null; }")]
    [InlineData("{ return Make(index, input); }")]
    [InlineData("{ return Other(index, input); } public static object? Other(int index, object? input) { return Make(index, input); }")]
    public void UnknownWritesConditionalAndRecursiveReturnsRemainUnknown(string body)
    {
        Fixture fixture = Create(body);
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        AssertUnknown(Evaluate(fixture, guard: guard));
        Assert.Empty(guard);
    }

    [Fact]
    public void NestedCallableReturnsDoNotEnterTheDeclarationReturnList()
    {
        Fixture fixture = Create("""
            { object? Nested() { return null; } System.Func<object?> nested = () => null;
              if (index == 0) { return new object(); } return new object(); }
            """);
        Assert.Equal(2, ReturnExpressions(fixture).Count);
        AssertNonNull(Evaluate(fixture));
    }

    [Fact]
    public void ExistingGuardEntriesArePreservedAndActiveSourceIsNotReentered()
    {
        Fixture fixture = Create(Returns(3));
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default) { fixture.Selected.OriginalDefinition, fixture.Caller };
        AssertUnknown(Evaluate(fixture, guard: guard));
        Assert.Equal(2, guard.Count);
        guard.Remove(fixture.Selected.OriginalDefinition);
        AssertNonNull(Evaluate(fixture, guard: guard));
        Assert.Same(fixture.Caller, Assert.Single(guard));
    }

    [Fact]
    public void EqualPathsDoNotMakeDistinctTreesTheSameTree()
    {
        Fixture fixture = Create(Returns(2), equalPaths: true);
        SyntaxTree calleeTree = ReturnExpressions(fixture)[0].SyntaxTree;
        Assert.Equal(fixture.Invocation.SyntaxTree.FilePath, calleeTree.FilePath);
        Assert.NotSame(fixture.Invocation.SyntaxTree, calleeTree);
        AssertNonNull(Evaluate(fixture));
    }

    [Fact]
    public void SharedExactTreesInDifferentCompilationsRetainTheirOwners()
    {
        Fixture first = Create(Returns(2, expression: "input"));
        CSharpCompilation secondCompilation = first.Compilation.WithAssemblyName("OtherOwner");
        SemanticModel secondModel = secondCompilation.GetSemanticModel(first.Invocation.SyntaxTree);
        IMethodSymbol secondCaller = (IMethodSymbol)secondModel.GetDeclaredSymbol(first.Caller.DeclaringSyntaxReferences.Single().GetSyntax())!;
        IMethodSymbol secondTarget = (IMethodSymbol)secondModel.GetSymbolInfo(first.Invocation).Symbol!;
        Assert.NotSame(first.Selected.ContainingAssembly, secondTarget.ContainingAssembly);
        Assert.True(SymbolEqualityComparer.Default.Equals(secondTarget.ContainingAssembly, secondCompilation.Assembly));
        AssertNonNull(Evaluate(first, new(first.Caller,
            new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull })));
        AssertUnknown(Evaluate(first, new(secondCaller), model: secondModel));
        AssertNonNull(Evaluate(first, new(secondCaller,
            new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull }), model: secondModel));
    }

    [Fact]
    public void EqualTextAndPathsCannotAuthorizeAForeignCompilationTree()
    {
        Fixture first = Create(Returns(2));
        Fixture second = Create(Returns(2));
        SyntaxTree foreign = ReturnExpressions(second)[0].SyntaxTree;
        Assert.Equal(ReturnExpressions(first)[0].SyntaxTree.ToString(), foreign.ToString());
        Assert.False(first.Compilation.ContainsSyntaxTree(foreign));
        Assert.Null(ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(first.Model, foreign));
        Assert.Throws<ArgumentException>(() => Evaluate(first, model: second.Model));
        AssertNonNull(Evaluate(first));
    }

    [Fact]
    public void CompilationReferenceSourceTreeIsRejectedByTheBindingCompilation()
    {
        Fixture source = Create(Returns(2));
        SyntaxTree callerTree = CSharpSyntaxTree.ParseText("""
            #nullable enable
            public static class ExternalCaller { public static object? Entry(object? input) { return Factory.Make(0, input); } }
            """, path: "/PERF06B/External.cs");
        CSharpCompilation callerCompilation = Compile("ExternalOwner", [callerTree],
            MetadataReferences.Default.Concat([source.Compilation.ToMetadataReference()]));
        SemanticModel model = callerCompilation.GetSemanticModel(callerTree);
        InvocationExpressionSyntax invocation = Assert.Single(callerTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>());
        IMethodSymbol selected = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
        Assert.Single(selected.DeclaringSyntaxReferences);
        Assert.False(callerCompilation.ContainsSyntaxTree(selected.DeclaringSyntaxReferences[0].SyntaxTree));
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        Assert.False(Query(invocation, model, new(null), guard, out ExceptionFlowValueFacts facts));
        Assert.Equal(ExceptionFlowValueFacts.None, facts);
        Assert.Empty(guard);
    }

    [Fact]
    public void EmptyReturnListAbortsAndReleasesTheSourceGuard()
    {
        Fixture fixture = Create("{ throw new System.InvalidOperationException(); }");
        Assert.Empty(ReturnExpressions(fixture));
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        AssertUnknown(Evaluate(fixture, guard: guard));
        Assert.Empty(guard);
    }

    [Fact]
    public void CalleeContextFailurePropagatesAndReleasesOnlyItsOwnGuardEntry()
    {
        Fixture fixture = Create(Returns(2), localArgument: true);
        ThrowOnLocalComparer comparer = new();
        HashSet<ISymbol> guard = new(comparer) { fixture.Caller };
        Assert.Throws<LocalInspectionException>(() => Evaluate(fixture, guard: guard));
        Assert.True(comparer.SawLocal);
        Assert.Same(fixture.Caller, Assert.Single(guard));
    }

    [Fact]
    public void ModelResolutionOccursOnceAfterContextAndBeforeTheUnchangedFactLoop()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        string relative = Path.Combine("src", "XMLDocNormalizer", "Checks", "Infrastructure", "Exception", "Flow",
            "ExceptionFlowContextualFactEvaluator.ReturnValueFacts.cs");
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative)))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        MethodDeclarationSyntax method = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName, relative)))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(candidate => candidate.Identifier.ValueText == "TryGetSourceInvocationReturnValueFacts");
        InvocationExpressionSyntax lookup = Assert.Single(method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is MemberAccessExpressionSyntax access
                && access.Name.Identifier.ValueText == "GetSemanticModelForSyntaxTree"));
        InvocationExpressionSyntax context = Assert.Single(method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() == "CreateCallContext"));
        ForEachStatementSyntax loop = Assert.Single(method.DescendantNodes().OfType<ForEachStatementSyntax>());
        Assert.True(context.SpanStart < lookup.SpanStart && lookup.SpanStart < loop.SpanStart);
        Assert.Empty(lookup.Ancestors().OfType<ForEachStatementSyntax>());
        Assert.Equal("semanticModel", lookup.ArgumentList.Arguments[0].Expression.ToString());
        Assert.Equal("returnExpressions[0].SyntaxTree", lookup.ArgumentList.Arguments[1].Expression.ToString());
        InvocationExpressionSyntax fact = Assert.Single(loop.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() == "GetSourceReturnExpressionValueFacts"));
        Assert.Equal(new[] { "returnExpression", "returnSemanticModel", "calleeContext", "inspectedValueSources" },
            fact.ArgumentList.Arguments.Select(argument => argument.Expression.ToString()));
        Assert.Contains(loop.DescendantNodes().OfType<IfStatementSyntax>(), statement =>
            statement.Condition.ToString() == "commonFacts == ExceptionFlowValueFacts.None");
        FinallyClauseSyntax cleanup = Assert.Single(method.DescendantNodes().OfType<FinallyClauseSyntax>());
        Assert.Contains(cleanup.DescendantNodes().OfType<InvocationExpressionSyntax>(), call =>
            call.Expression.ToString() == "inspectedValueSources.Remove"
            && call.ArgumentList.Arguments.Single().Expression.ToString() == "targetMethod");
    }

    private static List<ExpressionSyntax> ReturnExpressions(Fixture fixture) =>
        ExceptionFlowEnumValueFactsProvider.GetSourceReturnExpressions(fixture.Selected.DeclaringSyntaxReferences.Single().GetSyntax());

    private static (bool Success, ExceptionFlowValueFacts Facts) Evaluate(Fixture fixture,
        ExceptionFlowCallContext? context = null, HashSet<ISymbol>? guard = null, SemanticModel? model = null)
    {
        bool success = Query(fixture.Invocation, model ?? fixture.Model, context ?? new(fixture.Caller),
            guard ?? new(SymbolEqualityComparer.Default), out ExceptionFlowValueFacts facts);
        return (success, facts);
    }

    private static void AssertNonNull((bool Success, ExceptionFlowValueFacts Facts) result)
    {
        Assert.True(result.Success);
        Assert.True(result.Facts.ContainsAll(ExceptionFlowValueFacts.NonNull));
    }

    private static void AssertUnknown((bool Success, ExceptionFlowValueFacts Facts) result)
    {
        Assert.False(result.Success);
        Assert.Equal(ExceptionFlowValueFacts.None, result.Facts);
    }

    private static string Returns(int count, int? nullAt = null, string expression = "new object()")
    {
        return "{ " + string.Join(" ", Enumerable.Range(0, count).Select(index =>
            (index + 1 < count ? "if (index == " + index + ") { " : "") + "return " +
            (index == nullAt ? "null" : expression) + ";" + (index + 1 < count ? " }" : ""))) + " }";
    }

    private static Fixture Create(string body, bool sameTree = false, bool equalPaths = false, bool localArgument = false)
    {
        string caller = "#nullable enable\npublic static class Caller { public static object? Entry(object? input) { " +
            (localArgument ? "object? local = input; " : "") + "return Factory.Make(0, " + (localArgument ? "local" : "input") + "); } }";
        string factory = "\npublic static class Factory { public static object? Make(int index, object? input) " + body + " }";
        SyntaxTree callerTree = CSharpSyntaxTree.ParseText(caller + (sameTree ? factory : ""), path: "/PERF06B/Caller.cs");
        SyntaxTree[] trees = sameTree ? [callerTree] : [callerTree,
            CSharpSyntaxTree.ParseText("#nullable enable" + factory, path: equalPaths ? callerTree.FilePath : "/PERF06B/Factory.cs")];
        CSharpCompilation compilation = Compile("ReturnOwner", trees);
        SemanticModel model = compilation.GetSemanticModel(callerTree);
        MethodDeclarationSyntax entry = callerTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "Entry");
        InvocationExpressionSyntax invocation = Assert.Single(entry.DescendantNodes().OfType<InvocationExpressionSyntax>());
        return new(compilation, model, (IMethodSymbol)model.GetDeclaredSymbol(entry)!, invocation,
            (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!);
    }

    private static CSharpCompilation Compile(string name, IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference>? references = null)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(name, trees, references ?? MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return compilation;
    }

    private sealed record Fixture(CSharpCompilation Compilation, SemanticModel Model, IMethodSymbol Caller,
        InvocationExpressionSyntax Invocation, IMethodSymbol Selected);

    private sealed class LocalInspectionException : System.Exception
    {
    }

    private sealed class ThrowOnLocalComparer : IEqualityComparer<ISymbol>
    {
        internal bool SawLocal { get; private set; }
        public bool Equals(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);
        public int GetHashCode(ISymbol symbol)
        {
            if (symbol is ILocalSymbol)
            {
                SawLocal = true;
                throw new LocalInspectionException();
            }
            return SymbolEqualityComparer.Default.GetHashCode(symbol);
        }
    }
}
