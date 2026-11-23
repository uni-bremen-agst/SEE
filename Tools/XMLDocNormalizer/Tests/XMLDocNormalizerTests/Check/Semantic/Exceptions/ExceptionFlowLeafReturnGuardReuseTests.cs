using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception;

/// <summary>Protects allocation-free unused return guards without sharing contextual facts.</summary>
public sealed class ExceptionFlowLeafReturnGuardReuseTests
{
    private delegate ExceptionFlowValueFacts Evaluate(ExpressionSyntax expression, SemanticModel model,
        ExceptionFlowCallContext context, HashSet<ISymbol> guard);
    private delegate HashSet<ISymbol> Select(ExpressionSyntax expression, SymbolInfo symbol, HashSet<ISymbol> guard);
    private const string Code = """
        class Owner<T>
        {
            public object? Field;
            public object? Property { get; }
            static void Sink(object? input) { }
            static U Identity<U>(U input) => input;
            static object? Cycle() => Cycle();
            void Run(object? value, Owner<T> owner, bool choose, object? other, dynamic dynamicValue)
            {
                object? local = value;
                Sink(value); Sink(owner.Field); Sink(owner.Property); Sink(Field); Sink(Property);
                Sink(local); Sink(Identity(value)); Sink(choose ? value : other); Sink(value ?? other);
                Sink((object?)value); Sink(Cycle()); Sink(new object());
                if (value == null) { return; }
                Sink(value); value = null; Sink(value);
                Sink(MissingName); Sink(dynamicValue.Member); Sink((value)); Sink(string.Empty); Sink("known");
            }
        }
        """;

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    [InlineData(8, false)]
    [InlineData(9, false)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(13, true)]
    [InlineData(14, false)]
    [InlineData(15, false)]
    [InlineData(16, true)]
    [InlineData(17, true)]
    public void GuardSelectionBorrowsOnlyDefaultComparerBoundLeafForms(int index, bool borrow)
    {
        Fixture fixture = Create();
        ExpressionSyntax expression = fixture.Expressions[index];
        while (expression is ParenthesizedExpressionSyntax parentheses)
        {
            expression = parentheses.Expression;
        }
        ISymbol[] seeds = fixture.Compilation.GetSpecialType(SpecialType.System_String).GetMembers().Take(24).ToArray();
        Select select = ActualSelection();
        foreach (int count in new[] { 0, 8, 24 })
        {
            HashSet<ISymbol> guard = new(seeds.Take(count), SymbolEqualityComparer.Default);
            HashSet<ISymbol> selected = select(expression, fixture.Model.GetSymbolInfo(expression), guard);
            Assert.Equal(borrow, ReferenceEquals(guard, selected));
            Assert.Same(SymbolEqualityComparer.Default, selected.Comparer);
            Assert.True(selected.SetEquals(guard));
            Assert.Equal(count, guard.Count);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForeignComparersKeepIndependentDefaultNormalizedGuards(bool populated)
    {
        Fixture fixture = Create();
        ISymbol[] seeds = populated ? fixture.Compilation.GetSpecialType(SpecialType.System_String).GetMembers().Take(8).ToArray() : [];
        HashSet<ISymbol> guard = new(seeds, ReferenceEqualityComparer.Instance);
        HashSet<ISymbol> selected = ActualSelection()(fixture.Expressions[0], fixture.Model.GetSymbolInfo(fixture.Expressions[0]), guard);
        Assert.NotSame(guard, selected);
        Assert.Same(SymbolEqualityComparer.Default, selected.Comparer);
        Assert.True(selected.SetEquals(new HashSet<ISymbol>(guard, SymbolEqualityComparer.Default)));
        Assert.True(guard.SetEquals(seeds));
        Assert.Equal(ExceptionFlowValueFacts.None, FullEvaluate()(fixture.Expressions[0], fixture.Model, new(fixture.Method), guard));
        Assert.True(guard.SetEquals(seeds));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CallerFactsFlowPositionsAndLocalRecursionRemainFresh(bool known)
    {
        Fixture fixture = Create();
        ExceptionFlowCallContext context = known
            ? new(fixture.Method, new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull })
            : new(fixture.Method);
        ISymbol[] seeds = fixture.Compilation.GetSpecialType(SpecialType.System_String).GetMembers().Take(8).ToArray();
        HashSet<ISymbol> guard = new(seeds, SymbolEqualityComparer.Default);
        Evaluate evaluate = FullEvaluate();
        Assert.Equal(known, evaluate(fixture.Expressions[0], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.Equal(known, evaluate(fixture.Expressions[5], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.True(evaluate(fixture.Expressions[12], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        // Preserve the existing treatment of explicit caller facts after a write;
        // the empty-caller case still loses the earlier flow-derived proof.
        Assert.Equal(known, evaluate(fixture.Expressions[13], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.True(guard.SetEquals(seeds));
    }

    [Fact]
    public void ClearedMemberFactsCannotBeReusedDespiteTheSameNodeModelAndKey()
    {
        Fixture fixture = Create();
        IPropertySymbol property = (IPropertySymbol)fixture.Method.ContainingType.GetMembers("Property").Single();
        ExceptionFlowCallContext context = new(fixture.Method, [], [new KeyValuePair<int, ISymbol>(1, property)]);
        string key = context.Key;
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        Evaluate evaluate = FullEvaluate();
        Assert.True(evaluate(fixture.Expressions[2], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        ((HashSet<ISymbol>)context.GetKnownNonNullParameterMembers(1)).Clear();
        Assert.Equal(key, context.Key);
        Assert.False(evaluate(fixture.Expressions[2], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.Empty(guard);
        Assert.Empty(new ExceptionFlowCallContext(fixture.Method).GetKnownNonNullParameterMembers(1));
    }

    [Fact]
    public void AnotherCompilerModelAndGenericInvocationKeepTheirOwnFactsAndFrame()
    {
        Fixture fixture = Create();
        IMethodSymbol constructed = (IMethodSymbol)fixture.Method.ContainingType.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_String))
            .GetMembers("Run").Single();
        ExceptionFlowCallContext context = new ExceptionFlowCallContext(constructed,
            new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull }).WithInvocation(constructed);
        Assert.Same(constructed, context.InvocationSymbol);
        Assert.Same(constructed.OriginalDefinition, context.CallableSymbol);
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        Evaluate evaluate = FullEvaluate();
        Assert.True(evaluate(fixture.Expressions[0], fixture.Model, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        SemanticModel otherModel = fixture.Compilation.Clone().GetSemanticModel(fixture.Tree);
        Assert.NotSame(fixture.Model, otherModel);
        Assert.False(evaluate(fixture.Expressions[0], otherModel, context, guard).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.Empty(guard);
        Assert.Same(constructed, context.InvocationSymbol);
    }

    [Fact]
    public void RecursionActiveGuardsDynamicAndInvalidBindingsRetainConservativeResults()
    {
        Fixture fixture = Create();
        ISymbol identity = fixture.Method.ContainingType.GetMembers("Identity").Single();
        HashSet<ISymbol> guard = new([identity], SymbolEqualityComparer.Default);
        ExceptionFlowCallContext context = new(fixture.Method);
        Evaluate evaluate = FullEvaluate();
        foreach (int index in new[] { 6, 10, 14, 15 })
        {
            Assert.Equal(ExceptionFlowValueFacts.None, evaluate(fixture.Expressions[index], fixture.Model, context, guard));
            Assert.Single(guard);
            Assert.Contains(identity, guard);
        }
    }

    [Fact]
    public void ModelMismatchThrowsAtTheExistingBindingBeforeGuardSelection()
    {
        Fixture fixture = Create();
        SyntaxTree foreign = CSharpSyntaxTree.ParseText("class Other {}", path: "/PERF10E/Other.cs");
        CSharpCompilation other = CSharpCompilation.Create("PERF10E.Other", [foreign], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        HashSet<ISymbol> guard = new([fixture.Method], SymbolEqualityComparer.Default);
        Assert.Throws<ArgumentException>(() => FullEvaluate()(fixture.Expressions[0], other.GetSemanticModel(foreign), new(fixture.Method), guard));
        Assert.Single(guard);
        Assert.Contains(fixture.Method, guard);
    }

    [Fact]
    public void NullGuardStillThrowsFromTheExistingCopyConstructor()
    {
        Fixture fixture = Create();
        ArgumentNullException selected = Assert.Throws<ArgumentNullException>(() =>
            ActualSelection()(fixture.Expressions[0], fixture.Model.GetSymbolInfo(fixture.Expressions[0]), null!));
        ArgumentNullException evaluated = Assert.Throws<ArgumentNullException>(() =>
            FullEvaluate()(fixture.Expressions[0], fixture.Model, new(fixture.Method), null!));
        Assert.Equal("collection", selected.ParamName);
        Assert.Equal(selected.ParamName, evaluated.ParamName);
    }

    [Fact]
    public void ConstantFactsStillReturnBeforeTheUnusedNullGuardIsValidated()
    {
        Fixture fixture = Create();
        ExceptionFlowValueFacts facts = FullEvaluate()(fixture.Expressions[18], fixture.Model, new(fixture.Method), null!);
        Assert.True(facts.ContainsAll(ExceptionFlowValueFacts.NonNull | ExceptionFlowValueFacts.NonEmptyString | ExceptionFlowValueFacts.NonWhiteSpaceString));
    }

    [Fact]
    public async Task IndependentActivationsDoNotShareGuardsOrCallerFacts()
    {
        Fixture fixture = Create();
        ExceptionFlowCallContext empty = new(fixture.Method);
        ExceptionFlowCallContext known = new(fixture.Method, new Dictionary<int, ExceptionFlowValueFacts> { [0] = ExceptionFlowValueFacts.NonNull });
        Evaluate evaluate = FullEvaluate();
        Task<bool>[] tasks = Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            HashSet<ISymbol> guard = new([fixture.Method], SymbolEqualityComparer.Default);
            bool result = evaluate(fixture.Expressions[0], fixture.Model, index % 2 == 0 ? known : empty, guard)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
            Assert.Single(guard);
            Assert.Equal(index % 2 == 0, result);
            return result;
        })).ToArray();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public void BorrowedLeafPathsCannotReadMutateStoreOrForwardTheReturnGuard()
    {
        MethodDeclarationSyntax method = CSharpSyntaxTree.ParseText(File.ReadAllText(Source("ExceptionFlowContextualFactEvaluator.Nullability.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(candidate => candidate.Identifier.ValueText == "IsDefinitelyNonNull" && candidate.ParameterList.Parameters.Count == 4);
        SwitchStatementSyntax dispatch = Assert.Single(method.Body!.Statements.OfType<SwitchStatementSyntax>());
        IfStatementSyntax local = method.Body.Statements.OfType<IfStatementSyntax>()
            .Single(statement => statement.Condition.ToString() == "symbolInfo.Symbol is ILocalSymbol localSymbol");
        Assert.DoesNotContain(local.Condition.DescendantNodes().OfType<IdentifierNameSyntax>(), identifier => identifier.Identifier.ValueText == "inspectedReturnSymbols");
        foreach (IdentifierNameSyntax guard in method.Body.DescendantNodes().OfType<IdentifierNameSyntax>()
                     .Where(identifier => identifier.Identifier.ValueText == "inspectedReturnSymbols"))
        {
            Assert.True(dispatch.Span.Contains(guard.Span) || local.Statement.Span.Contains(guard.Span), "A leaf path now accesses the guard.");
        }
        string[] recursiveDispatchTypes = dispatch.Sections.SelectMany(section => section.Labels)
            .Select(label => label switch
            {
                CasePatternSwitchLabelSyntax { Pattern: DeclarationPatternSyntax declaration } => declaration.Type.ToString(),
                CasePatternSwitchLabelSyntax { Pattern: TypePatternSyntax type } => type.Type.ToString(),
                CaseSwitchLabelSyntax { Value: IdentifierNameSyntax typeName } => typeName.Identifier.ValueText,
                _ => throw new InvalidOperationException("New unqualified dispatch pattern.")
            }).Order(StringComparer.Ordinal).ToArray();
        string[] allowed = ["ParenthesizedExpressionSyntax", "CastExpressionSyntax", "CheckedExpressionSyntax", "ObjectCreationExpressionSyntax",
            "ImplicitObjectCreationExpressionSyntax", "AnonymousObjectCreationExpressionSyntax", "ArrayCreationExpressionSyntax", "ImplicitArrayCreationExpressionSyntax",
            "StackAllocArrayCreationExpressionSyntax", "ThisExpressionSyntax", "BaseExpressionSyntax", "TypeOfExpressionSyntax", "InterpolatedStringExpressionSyntax",
            "AnonymousFunctionExpressionSyntax", "ConditionalExpressionSyntax", "BinaryExpressionSyntax", "InvocationExpressionSyntax"];
        Assert.Equal(allowed.Order(StringComparer.Ordinal), recursiveDispatchTypes);
    }

    private static Evaluate FullEvaluate() => typeof(ExceptionFlowContextualFactEvaluator).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(method => method.Name == "GetExpressionValueFacts" && method.GetParameters().Length == 4).CreateDelegate<Evaluate>();

    private static Select ActualSelection()
    {
        MethodDeclarationSyntax method = CSharpSyntaxTree.ParseText(File.ReadAllText(Source("ExceptionFlowContextualFactEvaluator.ValueFacts.cs")))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(candidate => candidate.Identifier.ValueText == "GetExpressionValueFacts" && candidate.ParameterList.Parameters.Count == 4);
        VariableDeclaratorSyntax condition = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "canBorrowReturnGuard");
        VariableDeclaratorSyntax selected = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "inspectedReturnSymbols");
        ExpressionStatementSyntax nullCheck = method.Body!.Statements.OfType<ExpressionStatementSyntax>()
            .Single(statement => statement.Expression is InvocationExpressionSyntax call && call.Expression.ToString() == "ArgumentNullException.ThrowIfNull");
        string code = "using System; using System.Collections.Generic; using Microsoft.CodeAnalysis; using Microsoft.CodeAnalysis.CSharp.Syntax; public static class Selection { public static HashSet<ISymbol> Run(ExpressionSyntax unwrappedExpression, SymbolInfo symbolInfo, HashSet<ISymbol> inspectedImmutableMembers) { "
            + nullCheck + " bool canBorrowReturnGuard = "
            + condition.Initializer!.Value + "; return " + selected.Initializer!.Value + "; } }";
        CSharpCompilation compilation = CSharpCompilation.Create("PERF10E.Selection." + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(code)],
            MetadataReferences.Default.Concat([MetadataReference.CreateFromFile(typeof(ISymbol).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ExpressionSyntax).Assembly.Location), MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location)]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream image = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(image);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(image.ToArray()).GetType("Selection")!.GetMethod("Run")!.CreateDelegate<Select>();
    }

    private static string Source(string name)
    {
        string root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "XMLDocNormalizer.sln")))
        {
            root = Directory.GetParent(root)?.FullName ?? throw new InvalidOperationException("Tool root missing.");
        }
        return Path.Combine(root, "src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow", name);
    }

    private static Fixture Create()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Code, path: "/PERF10E/Fixture.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("PERF10E.Tests", [tree], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Diagnostic error = Assert.Single(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CS0103", error.Id);
        SemanticModel model = compilation.GetSemanticModel(tree);
        MethodDeclarationSyntax run = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Run");
        ExpressionSyntax[] expressions = run.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(invocation => invocation.Expression.ToString() == "Sink")
            .Select(invocation => invocation.ArgumentList.Arguments[0].Expression).ToArray();
        return new(tree, compilation, model, model.GetDeclaredSymbol(run)!, expressions);
    }

    private sealed record Fixture(SyntaxTree Tree, CSharpCompilation Compilation, SemanticModel Model, IMethodSymbol Method, ExpressionSyntax[] Expressions);
}
