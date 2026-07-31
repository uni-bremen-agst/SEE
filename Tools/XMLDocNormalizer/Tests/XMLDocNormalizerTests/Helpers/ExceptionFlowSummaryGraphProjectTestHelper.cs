using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Builds summary graphs for focused tests spanning a project-reference
    /// boundary represented by two Roslyn compilations.
    /// </summary>
    internal static class ExceptionFlowSummaryGraphProjectTestHelper
    {
        /// <summary>
        /// Builds a summary graph rooted in a consumer compilation that
        /// references a separately compiled dependency.
        /// </summary>
        /// <param name="dependencySource">
        /// The complete dependency source.
        /// </param>
        /// <param name="consumerSource">
        /// The complete consumer source containing the root method.
        /// </param>
        /// <param name="methodName">
        /// The uniquely occurring root method name in the consumer source.
        /// </param>
        /// <returns>
        /// The completed graph test run using the consumer compilation for
        /// framework-type lookup.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when either source does not compile, the dependency cannot
        /// be emitted, the root method cannot be resolved uniquely, or graph
        /// construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun Build(
            string dependencySource,
            string consumerSource,
            string methodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                dependencySource);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                consumerSource);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                methodName);

            SyntaxTree dependencyTree =
                CSharpSyntaxTree.ParseText(
                    dependencySource,
                    path:
                        "Dependency.cs");

            CSharpCompilation dependencyCompilation =
                CSharpCompilation.Create(
                    assemblyName:
                        "ExceptionFlowSummaryGraphDependency",
                    syntaxTrees:
                    [
                        dependencyTree
                    ],
                    references:
                        MetadataReferences.Default,
                    options:
                        CreateCompilationOptions());

            ThrowForCompilationErrors(
                dependencyCompilation,
                "dependency");

            byte[] dependencyImage =
                EmitCompilation(
                    dependencyCompilation);

            SyntaxTree consumerTree =
                CSharpSyntaxTree.ParseText(
                    consumerSource,
                    path:
                        ExceptionFlowAnalyzerTestHelper.SourcePath);

            MetadataReference dependencyReference =
                MetadataReference.CreateFromImage(
                    ImmutableArray.CreateRange(
                        dependencyImage));

            CSharpCompilation consumerCompilation =
                CSharpCompilation.Create(
                    assemblyName:
                        "ExceptionFlowSummaryGraphConsumer",
                    syntaxTrees:
                    [
                        consumerTree
                    ],
                    references:
                        MetadataReferences.Default.Concat(
                        [
                            dependencyReference
                        ]),
                    options:
                        CreateCompilationOptions());

            ThrowForCompilationErrors(
                consumerCompilation,
                "consumer");

            MethodDeclarationSyntax[] rootMethods =
                consumerTree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(
                        method =>
                            method.Identifier.ValueText ==
                            methodName)
                    .ToArray();

            if (rootMethods.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one MethodDeclarationSyntax named " +
                    $"'{methodName}', but found {rootMethods.Length}.");
            }

            ProjectId consumerProjectId =
                ProjectId.CreateNewId();

            ProjectId dependencyProjectId =
                ProjectId.CreateNewId();

            HashSet<ProjectId> reportingProjectIds =
                new()
                {
                    consumerProjectId
                };

            HashSet<ProjectId> analysisProjectIds =
                new()
                {
                    consumerProjectId,
                    dependencyProjectId
                };

            Dictionary<ProjectId, Compilation> compilations =
                new()
                {
                    [consumerProjectId] = consumerCompilation,
                    [dependencyProjectId] = dependencyCompilation
                };

            Dictionary<SyntaxTree, ProjectId> syntaxTreeToProjectId =
                new()
                {
                    [consumerTree] = consumerProjectId,
                    [dependencyTree] = dependencyProjectId
                };

            ProjectClosureSemanticContext semanticContext =
                new(
                    reportingProjectIds,
                    analysisProjectIds,
                    compilations,
                    syntaxTreeToProjectId);

            bool built =
                ExceptionFlowAnalyzer
                    .TryBuildTransitiveSummaryGraph(
                        rootMethods[0],
                        semanticContext,
                        out ExceptionFlowSummaryGraph graph,
                        out ExceptionFlowCallableKey rootKey);

            if (!built)
            {
                throw new InvalidOperationException(
                    "The exception-flow summary graph could not be built.");
            }

            return new ExceptionFlowSummaryGraphTestRun(
                graph,
                rootKey,
                consumerCompilation);
        }

        /// <summary>
        /// Creates the shared compilation options used by dependency and
        /// consumer compilations.
        /// </summary>
        /// <returns>The shared compilation options.</returns>
        private static CSharpCompilationOptions CreateCompilationOptions()
        {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions:
                    NullableContextOptions.Enable);
        }

        /// <summary>
        /// Emits one compilation to an in-memory metadata image.
        /// </summary>
        /// <param name="compilation">
        /// The compilation to emit.
        /// </param>
        /// <returns>The emitted portable executable image.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the compilation cannot be emitted.
        /// </exception>
        private static byte[] EmitCompilation(
            CSharpCompilation compilation)
        {
            using MemoryStream stream =
                new();

            EmitResult emitResult =
                compilation.Emit(
                    stream);

            if (!emitResult.Success)
            {
                throw new InvalidOperationException(
                    "The dependency compilation could not be emitted:" +
                    Environment.NewLine +
                    string.Join(
                        Environment.NewLine,
                        emitResult.Diagnostics.Select(
                            static diagnostic =>
                                diagnostic.ToString())));
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Throws when a focused test compilation contains compiler errors.
        /// </summary>
        /// <param name="compilation">
        /// The compilation to inspect.
        /// </param>
        /// <param name="role">
        /// The compilation role used in the error message.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compiler errors are present.
        /// </exception>
        private static void ThrowForCompilationErrors(
            CSharpCompilation compilation,
            string role)
        {
            Diagnostic[] errors =
                compilation.GetDiagnostics()
                    .Where(
                        static diagnostic =>
                            diagnostic.Severity ==
                            DiagnosticSeverity.Error)
                    .ToArray();

            if (errors.Length == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                $"The {role} summary-graph test source did not compile:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    errors.Select(
                        static error =>
                            error.ToString())));
        }
    }
}
