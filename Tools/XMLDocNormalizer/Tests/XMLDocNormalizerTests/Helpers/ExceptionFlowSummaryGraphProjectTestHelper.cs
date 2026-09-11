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
            return BuildCore(
                dependencySource,
                consumerSource,
                methodName,
                registerAsSupportingSource: false,
                includeExternalDocumentation: false,
                includeDependencyAnalysisScope: true);
        }

        /// <summary>
        /// Builds a summary graph rooted in a consumer compilation and
        /// registers the referenced dependency compilation as supporting source.
        /// </summary>
        /// <param name="dependencySource">The complete dependency source.</param>
        /// <param name="consumerSource">The complete consumer source.</param>
        /// <param name="methodName">The uniquely occurring consumer root method.</param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation, emission, root resolution, registration,
        /// or graph construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun BuildWithSupportingSource(
            string dependencySource,
            string consumerSource,
            string methodName)
        {
            return BuildCore(
                dependencySource,
                consumerSource,
                methodName,
                registerAsSupportingSource: true,
                includeExternalDocumentation: false,
                includeDependencyAnalysisScope: true);
        }

        /// <summary>
        /// Builds a metadata-only dependency graph with emitted external XML
        /// documentation.
        /// </summary>
        /// <param name="dependencySource">The complete dependency source.</param>
        /// <param name="consumerSource">The complete consumer source.</param>
        /// <param name="methodName">The uniquely occurring consumer root method.</param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation, emission, root resolution, or graph
        /// construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun BuildWithExternalDocumentation(
            string dependencySource,
            string consumerSource,
            string methodName)
        {
            return BuildCore(
                dependencySource,
                consumerSource,
                methodName,
                registerAsSupportingSource: false,
                includeExternalDocumentation: true,
                includeDependencyAnalysisScope: false);
        }

        /// <summary>
        /// Builds a graph with both external XML documentation and the exact
        /// dependency compilation registered as supporting source.
        /// </summary>
        /// <param name="dependencySource">The complete dependency source.</param>
        /// <param name="consumerSource">The complete consumer source.</param>
        /// <param name="methodName">The uniquely occurring consumer root method.</param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation, emission, root resolution, registration,
        /// or graph construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun BuildWithSupportingSourceAndExternalDocumentation(
            string dependencySource,
            string consumerSource,
            string methodName)
        {
            return BuildCore(
                dependencySource,
                consumerSource,
                methodName,
                registerAsSupportingSource: true,
                includeExternalDocumentation: true,
                includeDependencyAnalysisScope: true);
        }

        /// <summary>
        /// Builds a graph in which one registered supporting source
        /// compilation calls another registered supporting source compilation.
        /// </summary>
        /// <param name="targetDependencySource">
        /// The source of the downstream supporting dependency.
        /// </param>
        /// <param name="callingDependencySource">
        /// The source of the supporting dependency that references the
        /// downstream dependency.
        /// </param>
        /// <param name="consumerSource">The complete consumer source.</param>
        /// <param name="methodName">The uniquely occurring consumer root method.</param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation, emission, root resolution, registration,
        /// or graph construction fails.
        /// </exception>
        public static ExceptionFlowSummaryGraphTestRun BuildWithSupportingSourceChain(
            string targetDependencySource,
            string callingDependencySource,
            string consumerSource,
            string methodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetDependencySource);
            ArgumentException.ThrowIfNullOrWhiteSpace(callingDependencySource);
            ArgumentException.ThrowIfNullOrWhiteSpace(consumerSource);
            ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

            SyntaxTree targetDependencyTree = CSharpSyntaxTree.ParseText(
                targetDependencySource, path: "TargetDependency.cs");
            CSharpCompilation targetDependencyCompilation = CSharpCompilation.Create(
                "ExceptionFlowSummaryGraphTargetDependency",
                new[] { targetDependencyTree },
                MetadataReferences.Default,
                CreateCompilationOptions());
            ThrowForCompilationErrors(targetDependencyCompilation, "target dependency");
            MetadataReference targetDependencyReference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(EmitCompilation(targetDependencyCompilation)));

            SyntaxTree callingDependencyTree = CSharpSyntaxTree.ParseText(
                callingDependencySource, path: "CallingDependency.cs");
            CSharpCompilation callingDependencyCompilation = CSharpCompilation.Create(
                "ExceptionFlowSummaryGraphCallingDependency",
                new[] { callingDependencyTree },
                MetadataReferences.Default.Append(targetDependencyReference),
                CreateCompilationOptions());
            ThrowForCompilationErrors(callingDependencyCompilation, "calling dependency");
            MetadataReference callingDependencyReference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(EmitCompilation(callingDependencyCompilation)));

            SyntaxTree consumerTree = CSharpSyntaxTree.ParseText(
                consumerSource, path: ExceptionFlowAnalyzerTestHelper.SourcePath);
            CSharpCompilation consumerCompilation = CSharpCompilation.Create(
                "ExceptionFlowSummaryGraphConsumer",
                new[] { consumerTree },
                MetadataReferences.Default.Concat(
                    new[] { targetDependencyReference, callingDependencyReference }),
                CreateCompilationOptions());
            ThrowForCompilationErrors(consumerCompilation, "consumer");

            MethodDeclarationSyntax[] rootMethods = consumerTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Identifier.ValueText == methodName)
                .ToArray();

            if (rootMethods.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one MethodDeclarationSyntax named " +
                    $"'{methodName}', but found {rootMethods.Length}.");
            }

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    consumerTree, consumerCompilation);
            semanticContext.RegisterSupportingSource(targetDependencyCompilation);
            semanticContext.RegisterSupportingSource(callingDependencyCompilation);

            bool built = ExceptionFlowAnalyzer.TryBuildTransitiveSummaryGraph(
                rootMethods[0],
                semanticContext,
                out ExceptionFlowSummaryGraph graph,
                out ExceptionFlowCallableKey? rootKey);

            if (!built || rootKey == null)
            {
                throw new InvalidOperationException(
                    "The exception-flow summary graph could not be built.");
            }

            return new ExceptionFlowSummaryGraphTestRun(
                graph, rootKey, consumerCompilation);
        }

        /// <summary>
        /// Builds the shared two-compilation summary-graph test arrangement.
        /// </summary>
        /// <param name="dependencySource">The complete dependency source.</param>
        /// <param name="consumerSource">The complete consumer source.</param>
        /// <param name="methodName">The uniquely occurring consumer root method.</param>
        /// <param name="registerAsSupportingSource">
        /// Whether the dependency participates as supporting source instead
        /// of a referenced project.
        /// </param>
        /// <param name="includeExternalDocumentation">
        /// Whether emitted XML documentation is attached to the metadata
        /// reference.
        /// </param>
        /// <param name="includeDependencyAnalysisScope">
        /// Whether the dependency compilation participates as a referenced
        /// project when it is not registered as supporting source.
        /// </param>
        /// <returns>The completed graph test run.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an input string is null, empty, or white-space.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when compilation, emission, root resolution, registration,
        /// or graph construction fails.
        /// </exception>
        private static ExceptionFlowSummaryGraphTestRun BuildCore(
            string dependencySource,
            string consumerSource,
            string methodName,
            bool registerAsSupportingSource,
            bool includeExternalDocumentation,
            bool includeDependencyAnalysisScope)
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
                    includeExternalDocumentation
                        ? CSharpParseOptions.Default.WithDocumentationMode(
                            DocumentationMode.Diagnose)
                        : CSharpParseOptions.Default,
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

            byte[] dependencyImage;
            XmlDocumentationProvider? documentationProvider;

            if (includeExternalDocumentation)
            {
                dependencyImage = EmitCompilationWithDocumentation(
                    dependencyCompilation,
                    out documentationProvider);
            }
            else
            {
                dependencyImage = EmitCompilation(dependencyCompilation);
                documentationProvider = null;
            }

            SyntaxTree consumerTree =
                CSharpSyntaxTree.ParseText(
                    consumerSource,
                    path:
                        ExceptionFlowAnalyzerTestHelper.SourcePath);

            MetadataReference dependencyReference =
                MetadataReference.CreateFromImage(
                    ImmutableArray.CreateRange(
                        dependencyImage),
                    documentation: documentationProvider);

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

            ProjectClosureSemanticContext semanticContext;

            if (registerAsSupportingSource)
            {
                semanticContext = ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    consumerTree, consumerCompilation);
                semanticContext.RegisterSupportingSource(dependencyCompilation);
            }
            else if (includeDependencyAnalysisScope)
            {
                ProjectId consumerProjectId = ProjectId.CreateNewId();
                ProjectId dependencyProjectId = ProjectId.CreateNewId();
                SemanticCompilationScope consumerScope =
                    SemanticCompilationScope.CreateAnalysisTarget(consumerCompilation, consumerProjectId);
                SemanticCompilationScope dependencyScope =
                    SemanticCompilationScope.CreateReferencedProject(dependencyCompilation, dependencyProjectId);
                Dictionary<SyntaxTree, SemanticCompilationScope> scopesBySyntaxTree =
                    new()
                    {
                        [consumerTree] = consumerScope,
                        [dependencyTree] = dependencyScope
                    };

                semanticContext = new ProjectClosureSemanticContext(
                    new[] { consumerScope, dependencyScope },
                    scopesBySyntaxTree);
            }
            else
            {
                semanticContext = ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    consumerTree, consumerCompilation);
            }

            bool built =
                ExceptionFlowAnalyzer
                    .TryBuildTransitiveSummaryGraph(
                        rootMethods[0],
                        semanticContext,
                        out ExceptionFlowSummaryGraph graph,
                        out ExceptionFlowCallableKey? rootKey);

            if (!built || rootKey == null)
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
        /// Emits one compilation and its XML documentation to in-memory data.
        /// </summary>
        /// <param name="compilation">The compilation to emit.</param>
        /// <param name="documentationProvider">
        /// The provider backed by the emitted XML documentation.
        /// </param>
        /// <returns>The emitted portable executable image.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the compilation cannot be emitted.
        /// </exception>
        private static byte[] EmitCompilationWithDocumentation(
            CSharpCompilation compilation,
            out XmlDocumentationProvider documentationProvider)
        {
            using MemoryStream peStream = new();
            using MemoryStream documentationStream = new();
            EmitResult emitResult = compilation.Emit(
                peStream,
                xmlDocumentationStream: documentationStream);

            if (!emitResult.Success)
            {
                throw new InvalidOperationException(
                    "The dependency compilation could not be emitted:" +
                    Environment.NewLine +
                    string.Join(
                        Environment.NewLine,
                        emitResult.Diagnostics.Select(
                            static diagnostic => diagnostic.ToString())));
            }

            documentationProvider = XmlDocumentationProvider.CreateFromBytes(
                documentationStream.ToArray());
            return peStream.ToArray();
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
