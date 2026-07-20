using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests the deterministic exception models for known framework throw helpers.
    /// </summary>
    public sealed class KnownFrameworkExceptionModelTests
    {
        /// <summary>
        /// Gets all supported framework throw-helper cases.
        /// </summary>
        /// <value>
        /// The source snippets and expected exception types for all modeled helpers.
        /// </value>
        public static TheoryData<string, string[]> KnownFrameworkThrowHelpers { get; } =
            new()
            {
                {
                    "public void M(object? value) { System.ArgumentNullException.ThrowIfNull(value); }",
                    new[] { "System.ArgumentNullException" }
                },
                {
                    "public void M(string? value) { System.ArgumentException.ThrowIfNullOrEmpty(value); }",
                    new[]
                    {
                        "System.ArgumentException",
                        "System.ArgumentNullException"
                    }
                },
                {
                    "public void M(string? value) { System.ArgumentException.ThrowIfNullOrWhiteSpace(value); }",
                    new[]
                    {
                        "System.ArgumentException",
                        "System.ArgumentNullException"
                    }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfZero(value); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfNegative(value); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfEqual(value, 1); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfNotEqual(value, 1); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, 1); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfLessThan(value, 1); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(int value) { System.ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 1); }",
                    new[] { "System.ArgumentOutOfRangeException" }
                },
                {
                    "public void M(bool disposed) { System.ObjectDisposedException.ThrowIf(disposed, this); }",
                    new[] { "System.ObjectDisposedException" }
                },
                {
                    "public void M(bool disposed) { System.ObjectDisposedException.ThrowIf(disposed, typeof(TestClass)); }",
                    new[] { "System.ObjectDisposedException" }
                },
                {
                    "public void M(System.Threading.CancellationToken token) { token.ThrowIfCancellationRequested(); }",
                    new[] { "System.OperationCanceledException" }
                }
            };

        /// <summary>
        /// Ensures that every supported framework throw helper contributes
        /// the expected exception types.
        /// </summary>
        /// <param name="memberSource">The member containing the framework invocation.</param>
        /// <param name="expectedExceptionTypes">The expected exception type names.</param>
        [Theory]
        [MemberData(nameof(KnownFrameworkThrowHelpers))]
        public void KnownFrameworkThrowHelper_AddsExpectedExceptionTypes(
            string memberSource,
            string[] expectedExceptionTypes)
        {
            string source =
                "public sealed class TestClass\n" +
                "{\n" +
                memberSource +
                "\n}\n";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: new[] { tree },
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(static diagnostic =>
                    diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();

            Assert.Empty(errors);

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            InvocationExpressionSyntax invocation = tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Single();

            IMethodSymbol methodSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    semanticModel.GetSymbolInfo(invocation).Symbol);

            HashSet<INamedTypeSymbol> thrownExceptions =
                new(SymbolEqualityComparer.Default);

            bool recognized =
                KnownFrameworkExceptionModel.TryAddThrownExceptionTypes(
                    methodSymbol,
                    compilation,
                    thrownExceptions);

            Assert.True(recognized);

            string[] actualExceptionTypes = thrownExceptions
                .Select(static exceptionType => exceptionType.ToDisplayString())
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            string[] orderedExpectedTypes = expectedExceptionTypes
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(orderedExpectedTypes, actualExceptionTypes);
        }

        /// <summary>
        /// Ensures that a user-defined method with the same name is not treated
        /// as a framework throw helper.
        /// </summary>
        [Fact]
        public void UserDefinedSameNamedMethod_IsNotRecognized()
        {
            string source =
                "public sealed class MyArgumentNullException : System.ArgumentNullException\n" +
                "{\n" +
                "    public static new void ThrowIfNull(object? value) { }\n" +
                "}\n" +
                "\n" +
                "public sealed class TestClass\n" +
                "{\n" +
                "    public void M(object? value)\n" +
                "    {\n" +
                "        MyArgumentNullException.ThrowIfNull(value);\n" +
                "    }\n" +
                "}\n";

            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: "InMemoryAssembly",
                syntaxTrees: new[] { tree },
                references: MetadataReferences.Default,
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            InvocationExpressionSyntax invocation = tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Single();

            IMethodSymbol methodSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    semanticModel.GetSymbolInfo(invocation).Symbol);

            HashSet<INamedTypeSymbol> thrownExceptions =
                new(SymbolEqualityComparer.Default);

            bool recognized =
                KnownFrameworkExceptionModel.TryAddThrownExceptionTypes(
                    methodSymbol,
                    compilation,
                    thrownExceptions);

            Assert.False(recognized);
            Assert.Empty(thrownExceptions);
        }
    }
}
