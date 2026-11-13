using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception;

/// <summary>Protects the nested source-provenance chain when each helper consumes its first binding.</summary>
public sealed class ExceptionFlowSourcePositionLocalBindingTests
{
    [Theory]
    [InlineData("Line", "1", false, false, true)]
    [InlineData("Character", "1", true, true, true)]
    [InlineData("Line", "0", true, false, false)]
    [InlineData("Character", "-1", false, true, false)]
    [InlineData("Line", "length", false, false, false)]
    [InlineData("Character", "length", true, true, false)]
    public void InlineChainRetainsCoordinateAndConstructorProvenance(
        string coordinate, string length, bool implicitCreation, bool oneFirst, bool positive)
    {
        string creation = implicitCreation ? $"new(position, length: {length})" : $"new TextSpan(position, length: {length})";
        string value = $"((tree.GetLineSpan({creation})).StartLinePosition).{coordinate}";
        var fixture = Create($"Use({(oneFirst ? $"1 + ({value})" : $"({value}) + 1")});");
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        Assert.Equal(positive ? ExceptionFlowValueFacts.PositiveInt32 : ExceptionFlowValueFacts.None,
            Evaluate(fixture.Arguments.Single(), fixture.Model, new(fixture.Method), guard));
        Assert.Empty(guard);
    }

    [Theory]
    [InlineData("Use(arbitrary.Line + 1);")]
    [InlineData("Use(tree.GetLineSpan(new TextSpan(position, 1)).EndLinePosition.Line + 1);")]
    [InlineData("Use(GetSpan(tree).StartLinePosition.Line + 1);")]
    public void SimilarShapesWithoutTheRequiredOriginRemainNegative(string body)
    {
        var fixture = Create(body);
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
        Assert.Equal(ExceptionFlowValueFacts.None,
            Evaluate(fixture.Arguments.Single(), fixture.Model, new(fixture.Method), guard));
        Assert.Empty(guard);
    }

    [Fact]
    public void InlineChainIsRecomputedInEachModelAndCallContext()
    {
        var fixture = Create("Use(tree.GetLineSpan(new TextSpan(position, 1)).StartLinePosition.Line + 1);");
        ExpressionSyntax expression = fixture.Arguments.Single();
        CSharpCompilation otherCompilation = fixture.Compilation.WithAssemblyName("OtherOwner");
        foreach (SemanticModel model in new[]
        {
            fixture.Model,
            fixture.Compilation.GetSemanticModel(fixture.Model.SyntaxTree, ignoreAccessibility: true),
            otherCompilation.GetSemanticModel(fixture.Model.SyntaxTree)
        })
        {
            IMethodSymbol method = model.Compilation.GetTypeByMetadataName("Example")!.GetMembers("M").OfType<IMethodSymbol>().Single();
            ExceptionFlowCallContext empty = new(method);
            ExceptionFlowCallContext different = new(method,
                new Dictionary<int, ExceptionFlowValueFacts> { [1] = ExceptionFlowValueFacts.PositiveInt32 });
            foreach (ExceptionFlowCallContext context in new[] { empty, different, empty })
            {
                HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
                Assert.Equal(ExceptionFlowValueFacts.PositiveInt32, Evaluate(expression, model, context, guard));
                Assert.Empty(guard);
            }
            Assert.True(SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, model.Compilation.Assembly));
        }
    }

    [Fact]
    public void NestedLocalGuardAndWritesRemainEffectiveAfterBindingReuse()
    {
        var fixture = Create("""
            TextSpan span = new(position, length: 1);
            FileLinePositionSpan lineSpan = tree.GetLineSpan(span);
            Use(lineSpan.StartLinePosition.Line + 1);
            span = new(position, length: length);
            lineSpan = tree.GetLineSpan(span);
            Use(lineSpan.StartLinePosition.Line + 1);
            """);
        VariableDeclaratorSyntax declaration = fixture.Model.SyntaxTree.GetRoot().DescendantNodes()
            .OfType<VariableDeclaratorSyntax>().Single(node => node.Identifier.ValueText == "span");
        ISymbol span = fixture.Model.GetDeclaredSymbol(declaration)!;
        HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default) { span };
        ExceptionFlowCallContext context = new(fixture.Method);
        Assert.Equal(ExceptionFlowValueFacts.None, Evaluate(fixture.Arguments[0], fixture.Model, context, guard));
        Assert.Same(span, Assert.Single(guard));
        guard.Clear();
        Assert.Equal(ExceptionFlowValueFacts.PositiveInt32, Evaluate(fixture.Arguments[0], fixture.Model, context, guard));
        Assert.Empty(guard);
        Assert.Equal(ExceptionFlowValueFacts.None, Evaluate(fixture.Arguments[1], fixture.Model, context, guard));
        Assert.Empty(guard);
    }

    private static ExceptionFlowValueFacts Evaluate(ExpressionSyntax expression, SemanticModel model,
        ExceptionFlowCallContext context, HashSet<ISymbol> guard)
        => ExceptionFlowSourcePositionValueFactsProvider.GetOneBasedSourcePositionValueFacts(expression, model, context, guard);

    private sealed record Fixture(CSharpCompilation Compilation, SemanticModel Model, IMethodSymbol Method, ExpressionSyntax[] Arguments);

    private static Fixture Create(string body)
    {
        string source = $$"""
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Text;
            public static class Example
            {
                public static void M(SyntaxTree tree, int position, int length, LinePosition arbitrary)
                {
                    {{body}}
                }
                private static void Use(int value) { }
                private static FileLinePositionSpan GetSpan(SyntaxTree tree) => tree.GetLineSpan(new TextSpan(0, 1));
            }
            """;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "LocalSourcePosition.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("LocalOwner", [tree],
            MetadataReferences.Default.Concat([MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location)]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        SemanticModel model = compilation.GetSemanticModel(tree);
        IMethodSymbol method = compilation.GetTypeByMetadataName("Example")!.GetMembers("M").OfType<IMethodSymbol>().Single();
        ExpressionSyntax[] arguments = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is IdentifierNameSyntax { Identifier.ValueText: "Use" })
            .Select(call => call.ArgumentList.Arguments.Single().Expression).ToArray();
        return new(compilation, model, method, arguments);
    }
}
