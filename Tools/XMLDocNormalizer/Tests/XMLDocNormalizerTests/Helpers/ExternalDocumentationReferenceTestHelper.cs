using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Creates in-memory metadata references with deterministic XML
    /// documentation for external-contract tests.
    /// </summary>
    internal static class ExternalDocumentationReferenceTestHelper
    {
        /// <summary>
        /// Creates an external metadata reference from the supplied C# source
        /// and attaches the XML documentation emitted for that assembly.
        /// </summary>
        /// <param name="source">
        /// The complete source of the external test assembly.
        /// </param>
        /// <returns>
        /// A metadata reference containing the compiled assembly and its XML
        /// documentation.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="source"/> is null, empty, or consists
        /// only of white-space characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the external test assembly cannot be compiled.
        /// </exception>
        public static PortableExecutableReference Create(
            string source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                source);

            SyntaxTree syntaxTree =
                CSharpSyntaxTree.ParseText(
                    source,
                    CSharpParseOptions.Default
                        .WithDocumentationMode(
                            DocumentationMode.Diagnose),
                    path: "ExternalDocumentationContracts.cs");

            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    assemblyName:
                        "ExternalDocumentationContracts",
                    syntaxTrees:
                    [
                        syntaxTree
                    ],
                    references:
                        MetadataReferences.Default,
                    options:
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary,
                            nullableContextOptions:
                                NullableContextOptions.Enable));

            using MemoryStream peStream =
                new();

            using MemoryStream documentationStream =
                new();

            EmitResult emitResult =
                compilation.Emit(
                    peStream,
                    xmlDocumentationStream:
                        documentationStream);

            if (!emitResult.Success)
            {
                Diagnostic[] errors =
                    emitResult.Diagnostics
                        .Where(
                            static diagnostic =>
                                diagnostic.Severity ==
                                DiagnosticSeverity.Error)
                        .ToArray();

                throw new InvalidOperationException(
                    "The external documentation test assembly could not " +
                    "be compiled:" +
                    Environment.NewLine +
                    string.Join(
                        Environment.NewLine,
                        errors.Select(
                            static error =>
                                error.ToString())));
            }

            XmlDocumentationProvider documentationProvider =
                XmlDocumentationProvider.CreateFromBytes(
                    documentationStream.ToArray());

            return MetadataReference.CreateFromImage(
                peStream.ToArray(),
                documentation:
                    documentationProvider,
                filePath:
                    "ExternalDocumentationContracts.dll");
        }
    }
}
