using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides focused in-memory access to the raw exception-flow analyzer.
    /// </summary>
    internal static class ExceptionFlowAnalyzerTestHelper
    {
        /// <summary>
        /// The source path assigned to in-memory syntax trees.
        /// </summary>
        public const string SourcePath = "InMemory.cs";

        /// <summary>
        /// Analyzes direct exception sources in the specified method.
        /// </summary>
        /// <param name="source">The complete C# source text.</param>
        /// <param name="methodName">The method name to analyze.</param>
        /// <returns>The completed test analysis run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="source"/> or
        /// <paramref name="methodName"/> is null, empty, or consists only
        /// of white-space characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source does not compile or the requested method
        /// cannot be identified uniquely.
        /// </exception>
        public static ExceptionFlowAnalyzerTestRun AnalyzeDirectly(
            string source,
            string methodName)
        {
            return Analyze(
                source,
                methodName,
                analyzeTransitively: false);
        }

        /// <summary>
        /// Analyzes direct and transitive exception sources in the specified
        /// method.
        /// </summary>
        /// <param name="source">The complete C# source text.</param>
        /// <param name="methodName">The method name to analyze.</param>
        /// <returns>The completed test analysis run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="source"/> or
        /// <paramref name="methodName"/> is null, empty, or consists only
        /// of white-space characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source does not compile or the requested method
        /// cannot be identified uniquely.
        /// </exception>
        public static ExceptionFlowAnalyzerTestRun AnalyzeTransitively(
            string source,
            string methodName)
        {
            return Analyze(
                source,
                methodName,
                analyzeTransitively: true);
        }

        /// <summary>
        /// Creates an in-memory compilation and executes the selected
        /// exception-flow analysis mode.
        /// </summary>
        /// <param name="source">The complete C# source text.</param>
        /// <param name="methodName">The method name to analyze.</param>
        /// <param name="analyzeTransitively">
        /// Whether transitive analysis should be used.
        /// </param>
        /// <returns>The completed test analysis run.</returns>
        private static ExceptionFlowAnalyzerTestRun Analyze(
            string source,
            string methodName,
            bool analyzeTransitively)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                source,
                path: SourcePath);

            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    assemblyName: "ExceptionFlowAnalyzerTests",
                    syntaxTrees: [syntaxTree],
                    references: MetadataReferences.Default,
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        nullableContextOptions:
                            NullableContextOptions.Enable));

            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(static diagnostic =>
                    diagnostic.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Length > 0)
            {
                throw new InvalidOperationException(
                    "The exception-flow test source did not compile:" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine,
                        errors.Select(static error => error.ToString())));
            }

            MethodDeclarationSyntax[] matchingMethods =
                syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method =>
                        method.Identifier.ValueText == methodName)
                    .ToArray();

            if (matchingMethods.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one method named '{methodName}', " +
                    $"but found {matchingMethods.Length}.");
            }

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContext
                    .CreateSingleCompilationContext(
                        syntaxTree,
                        compilation);

            ExceptionFlowAnalysisResult result = analyzeTransitively
                ? ExceptionFlowAnalyzer
                    .AnalyzeTransitivelyThrownExceptions(
                        matchingMethods[0],
                        semanticContext)
                : ExceptionFlowAnalyzer
                    .AnalyzeDirectlyThrownExceptions(
                        matchingMethods[0],
                        semanticContext);

            return new ExceptionFlowAnalyzerTestRun(
                result,
                compilation,
                syntaxTree,
                matchingMethods[0]);
        }
    }

    /// <summary>
    /// Contains the raw output and Roslyn objects of one focused analyzer run.
    /// </summary>
    /// <param name="Result">The raw exception-flow analysis result.</param>
    /// <param name="Compilation">The in-memory compilation.</param>
    /// <param name="SyntaxTree">The analyzed syntax tree.</param>
    /// <param name="Method">The analyzed method declaration.</param>
    internal sealed record ExceptionFlowAnalyzerTestRun(
        ExceptionFlowAnalysisResult Result,
        CSharpCompilation Compilation,
        SyntaxTree SyntaxTree,
        MethodDeclarationSyntax Method)
    {
        /// <summary>
        /// Resolves a required named type from the test compilation.
        /// </summary>
        /// <param name="metadataName">The full metadata type name.</param>
        /// <returns>The resolved named type symbol.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="metadataName"/> is null, empty, or
        /// consists only of white-space characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the type cannot be resolved.
        /// </exception>
        public INamedTypeSymbol GetRequiredType(
            string metadataName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(metadataName);

            INamedTypeSymbol? typeSymbol =
                Compilation.GetTypeByMetadataName(metadataName);

            return typeSymbol ??
                   throw new InvalidOperationException(
                       $"Could not resolve type '{metadataName}'.");
        }
    }
}
