using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests exception contracts obtained from external XML documentation.
    /// </summary>
    public sealed class ExternalDocumentationExceptionModelTests
    {
        /// <summary>
        /// Ensures that all documented exception types of an external method
        /// are resolved.
        /// </summary>
        [Fact]
        public void DocumentedExceptions_AreResolved()
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            (CSharpCompilation compilation, IMethodSymbol methodSymbol) =
                ResolveExternalMethod(
                    externalReference,
                    "Execute");

            IReadOnlyList<INamedTypeSymbol> exceptionTypes =
                ExternalDocumentationExceptionModel.GetDocumentedExceptionTypes(
                    methodSymbol, compilation);

            string[] actualTypes =
                exceptionTypes
                    .Select(
                        static type =>
                            type.ToDisplayString())
                    .OrderBy(
                        static type =>
                            type,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "System.IO.IOException",
                    "System.InvalidOperationException"
                },
                actualTypes);
        }

        /// <summary>
        /// Ensures that a method without exception documentation contributes
        /// no exception contract.
        /// </summary>
        [Fact]
        public void MissingExceptionDocumentation_ReturnsNoContract()
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            (CSharpCompilation compilation, IMethodSymbol methodSymbol) =
                ResolveExternalMethod(
                    externalReference,
                    "WithoutContract");

            IReadOnlyList<INamedTypeSymbol> exceptionTypes =
                ExternalDocumentationExceptionModel.GetDocumentedExceptionTypes(
                    methodSymbol, compilation);

            Assert.Empty(exceptionTypes);
        }

        /// <summary>
        /// Ensures that a cref referring to a non-exception type is not
        /// accepted as an exception contract.
        /// </summary>
        [Fact]
        public void NonExceptionCref_IsIgnored()
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            (CSharpCompilation compilation, IMethodSymbol methodSymbol) =
                ResolveExternalMethod(
                    externalReference,
                    "WithNonExceptionContract");

            IReadOnlyList<INamedTypeSymbol> exceptionTypes =
                ExternalDocumentationExceptionModel
                    .GetDocumentedExceptionTypes(
                        methodSymbol,
                        compilation);

            Assert.Empty(exceptionTypes);
        }

        /// <summary>
        /// Creates the deterministic external test assembly.
        /// </summary>
        /// <returns>
        /// The external metadata reference with attached XML documentation.
        /// </returns>
        private static PortableExecutableReference
            CreateExternalReference()
        {
            const string source =
                """
                namespace ExternalContracts
                {
                    public static class ExternalApi
                    {
                        /// <summary>Executes external work.</summary>
                        /// <exception cref="System.IO.IOException">
                        /// Thrown when an I/O operation fails.
                        /// </exception>
                        /// <exception cref="System.InvalidOperationException">
                        /// Thrown when the operation is invalid.
                        /// </exception>
                        public static void Execute()
                        {
                        }

                        /// <summary>Executes external work.</summary>
                        public static void WithoutContract()
                        {
                        }

                        /// <summary>Executes external work.</summary>
                        /// <exception cref="string">
                        /// This deliberately references a non-exception type.
                        /// </exception>
                        public static void WithNonExceptionContract()
                        {
                        }
                    }
                }
                """;

            return ExternalDocumentationReferenceTestHelper
                .Create(
                    source);
        }

        /// <summary>
        /// Creates a consumer compilation and resolves one invocation of the
        /// requested external method.
        /// </summary>
        /// <param name="externalReference">
        /// The external metadata reference.
        /// </param>
        /// <param name="methodName">
        /// The external method to invoke and resolve.
        /// </param>
        /// <returns>
        /// The consumer compilation and resolved external method symbol.
        /// </returns>
        private static
            (CSharpCompilation Compilation, IMethodSymbol MethodSymbol)
            ResolveExternalMethod(
                PortableExecutableReference externalReference,
                string methodName)
        {
            string source =
                $$"""
                using ExternalContracts;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        ExternalApi.{{methodName}}();
                    }
                }
                """;

            SyntaxTree syntaxTree =
                CSharpSyntaxTree.ParseText(
                    source);

            List<MetadataReference> references =
                new(MetadataReferences.Default)
                {
                    externalReference
                };

            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    assemblyName:
                        "ExternalDocumentationConsumer",
                    syntaxTrees:
                    [
                        syntaxTree
                    ],
                    references:
                        references,
                    options:
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

            Diagnostic[] errors =
                compilation.GetDiagnostics()
                    .Where(
                        static diagnostic =>
                            diagnostic.Severity ==
                            DiagnosticSeverity.Error)
                    .ToArray();

            Assert.Empty(errors);

            InvocationExpressionSyntax invocation =
                Assert.Single(
                    syntaxTree.GetRoot()
                        .DescendantNodes()
                        .OfType<InvocationExpressionSyntax>());

            SemanticModel semanticModel =
                compilation.GetSemanticModel(
                    syntaxTree);

            IMethodSymbol methodSymbol =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    semanticModel
                        .GetSymbolInfo(invocation)
                        .Symbol);

            return (
                compilation,
                methodSymbol);
        }
    }
}
