using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the entry contracts and evaluator-owned weak cache lifetime after extraction.
    /// Existing end-to-end regressions cover the unchanged specialized fact paths.
    /// </summary>
    public sealed class ExceptionFlowContextualFactEvaluatorTests
    {
        /// <summary>
        /// Exercises all four entry targets with positive, negative and unknown argument facts.
        /// </summary>
        [Theory]
        [InlineData("new object()", true)]
        [InlineData("null", false)]
        [InlineData("input", false)]
        public void FourEntries_PreserveFactsDefaultsAndGuardCleanup(string argument, bool nonNull)
        {
            Context fixture = CreateContext($$"""
                public static class TestClass
                {
                    public static void M(object? input) { Sink({{argument}}); }
                    private static void Sink(object? value, int count = 2) { }
                }
                """);
            InvocationExpressionSyntax invocation = fixture.Root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>().Single();
            ExpressionSyntax expression = invocation.ArgumentList.Arguments[0].Expression;
            IMethodSymbol target = (IMethodSymbol)fixture.Model.GetSymbolInfo(invocation).Symbol!;
            ExceptionFlowCallContext caller = new(fixture.Model.GetDeclaredSymbol(fixture.Method));
            HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);

            ExceptionFlowValueFacts facts = ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(
                expression, fixture.Model, caller);
            Assert.Equal(nonNull, facts.ContainsAll(ExceptionFlowValueFacts.NonNull));
            Assert.Equal(nonNull, ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull(
                expression, fixture.Model, caller, guard));
            Assert.Empty(guard);

            Dictionary<int, ExceptionFlowValueFacts> explicitFacts = [];
            HashSet<int> supplied = [];
            ExceptionFlowContextualFactEvaluator.AddExplicitArgumentFacts(
                target, invocation.ArgumentList.Arguments, fixture.Model, caller, explicitFacts, supplied, guard);
            Assert.Equal(new[] { 0 }, supplied);
            Assert.Equal(nonNull, explicitFacts.GetValueOrDefault(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
            Assert.False(explicitFacts.ContainsKey(1));
            Assert.Empty(guard);

            ExceptionFlowCallContext projected = ExceptionFlowContextualFactEvaluator.CreateCallContext(
                target, invocation.ArgumentList.Arguments, fixture.Model, caller, guard);
            Assert.Equal(nonNull, projected.GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
            Assert.Equal(ExceptionFlowValueFacts.NonNull, projected.GetParameterFacts(1));
            Assert.True(SymbolEqualityComparer.Default.Equals(target.OriginalDefinition, projected.CallableSymbol));
            Assert.Empty(guard);
        }

        /// <summary>
        /// Keeps call-context-dependent facts separate across repeated evaluations of one expression.
        /// </summary>
        [Fact]
        public void ParameterFacts_RemainContextDependent()
        {
            Context fixture = CreateContext("public static class TestClass { public static void M(object? input) { Use(input); } private static void Use(object? value) { } }");
            ExpressionSyntax expression = fixture.Method.DescendantNodes()
                .OfType<ArgumentSyntax>().Single().Expression;
            ISymbol? symbol = fixture.Model.GetDeclaredSymbol(fixture.Method);
            ExceptionFlowCallContext unknown = new(symbol);
            ExceptionFlowCallContext known = new(symbol, new Dictionary<int, ExceptionFlowValueFacts>
            {
                [0] = ExceptionFlowValueFacts.NonNull
            });
            foreach (ExceptionFlowCallContext context in new[] { unknown, known, unknown, known })
            {
                Assert.Equal(context == known, ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(
                    expression, fixture.Model, context).ContainsAll(ExceptionFlowValueFacts.NonNull));
            }
        }

        /// <summary>
        /// Recursive source returns fail closed without poisoning a reused guard or later positive query.
        /// </summary>
        [Fact]
        public void RecursiveReturns_TerminateAndRestoreGuard()
        {
            Context fixture = CreateContext("""
                public static class TestClass
                {
                    public static void M() { Use(LoopA()); Use(Ready()); }
                    private static object? LoopA() => LoopB();
                    private static object? LoopB() => LoopA();
                    private static object Ready() => new object();
                    private static void Use(object? value) { }
                }
                """);
            ExpressionSyntax[] expressions = fixture.Method.DescendantNodes().OfType<ArgumentSyntax>()
                .Select(argument => argument.Expression).ToArray();
            HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
            ExceptionFlowCallContext caller = new(fixture.Model.GetDeclaredSymbol(fixture.Method));
            Assert.False(ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull(expressions[0], fixture.Model, caller, guard));
            Assert.Empty(guard);
            Assert.True(ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull(expressions[1], fixture.Model, caller, guard));
            Assert.Empty(guard);
        }

        /// <summary>
        /// Memoizes both truth values once per model using Roslyn symbol equality, with guards checked first.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WeakPartition_ReusesPositiveAndNegativeEntriesWithoutBypassingGuard(bool nonNull)
        {
            Context fixture = CacheContext(nonNull);
            InvocationExpressionSyntax invocation = CacheInvocation(fixture);
            ExceptionFlowCallContext caller = new(fixture.Model.GetDeclaredSymbol(fixture.Method));
            HashSet<ISymbol> guard = new(SymbolEqualityComparer.Default);
            for (int index = 0; index < 3; index++)
            {
                Assert.Equal(nonNull, ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(
                    invocation, fixture.Model, caller).ContainsAll(ExceptionFlowValueFacts.NonNull));
                Assert.Equal(nonNull, ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull(
                    invocation, fixture.Model, caller, guard));
                Assert.Empty(guard);
            }

            Dictionary<ISymbol, bool> entries = Entries(fixture.Model);
            KeyValuePair<ISymbol, bool> entry = Assert.Single(entries);
            Assert.Same(SymbolEqualityComparer.Default, entries.Comparer);
            Assert.Equal(nonNull, entry.Value);
            Assert.True(SymbolEqualityComparer.Default.Equals(entry.Key, entry.Key.OriginalDefinition));
            guard.Add(entry.Key);
            Assert.False(ExceptionFlowContextualFactEvaluator.IsDefinitelyNonNull(invocation, fixture.Model, caller, guard));
            Assert.Single(guard);
            Assert.Equal(nonNull, Assert.Single(entries).Value);
        }

        /// <summary>
        /// Identically named symbols in different semantic worlds do not share cached facts.
        /// </summary>
        [Fact]
        public void WeakPartitions_AreScopedToSemanticModelIdentity()
        {
            Context positive = CacheContext(true);
            Context negative = CacheContext(false);
            Warm(positive);
            Warm(negative);
            Assert.NotSame(Entries(positive.Model), Entries(negative.Model));
            Assert.True(Assert.Single(Entries(positive.Model)).Value);
            Assert.False(Assert.Single(Entries(negative.Model)).Value);
        }

        /// <summary>
        /// A warmed static cache does not strongly retain the model through partition symbols.
        /// </summary>
        [Fact]
        public void StaticWeakCache_DoesNotKeepSemanticWorldAlive()
        {
            WeakReference model = WarmAndReleaseModel();
            for (int attempt = 0; attempt < 5 && model.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(model.IsAlive);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference WarmAndReleaseModel()
        {
            Context fixture = CacheContext(true);
            Warm(fixture);
            Assert.Single(Entries(fixture.Model));
            return new WeakReference(fixture.Model);
        }

        private static void Warm(Context fixture) => ExceptionFlowContextualFactEvaluator.GetExpressionValueFacts(
            CacheInvocation(fixture), fixture.Model, new ExceptionFlowCallContext(fixture.Model.GetDeclaredSymbol(fixture.Method)));

        private static InvocationExpressionSyntax CacheInvocation(Context fixture) => fixture.Method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>().Single(invocation => invocation.Expression.ToString() == "Cache.GetValue");

        private static Context CacheContext(bool nonNull) => CreateContext($$"""
            using System.Runtime.CompilerServices;
            public static class TestClass
            {
                private static readonly ConditionalWeakTable<object, object> Cache = new();
                public static object M() => Cache.GetValue(new object(), static _ => {{(nonNull ? "new object()" : "null!")}});
            }
            """);

        private static Dictionary<ISymbol, bool> Entries(SemanticModel model)
        {
            object table = typeof(ExceptionFlowContextualFactEvaluator).GetField(
                "conditionalWeakTableValueFactCaches", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            object?[] arguments = [model, null];
            Assert.True((bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, arguments)!);
            object partition = arguments[1]!;
            return Assert.IsType<Dictionary<ISymbol, bool>>(partition.GetType().GetField(
                "entries", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(partition));
        }

        private static Context CreateContext(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "Fixture.cs");
            CSharpCompilation compilation = CSharpCompilation.Create("EvaluatorFixture", [tree], MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            return new Context(compilation.GetSemanticModel(tree), root,
                root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "M"));
        }

        private sealed record Context(SemanticModel Model, CompilationUnitSyntax Root, MethodDeclarationSyntax Method);
    }
}
