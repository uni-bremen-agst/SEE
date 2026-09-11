using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests exact supporting source binding for metadata method symbols.
    /// </summary>
    public sealed class SupportingSourceSymbolResolverTests
    {
        /// <summary>
        /// Resolves a metadata method through the exact registered supporting
        /// assembly identity.
        /// </summary>
        [Fact]
        public void ResolveMethod_ExactRegisteredSupportingIdentity_ReturnsSourceMethod()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(CreateDependencySource("1.0.0.0"));
            ProjectClosureSemanticContext context = CreateConsumerContext(consumerCompilation);
            context.RegisterSupportingSource(sourceCompilation);
            IMethodSymbol metadataMethod = GetRequiredMethod(consumerCompilation);
            IMethodSymbol sourceMethod = GetRequiredMethod(sourceCompilation);

            IMethodSymbol? resolvedMethod =
                SupportingSourceSymbolResolver.ResolveMethod(metadataMethod, context);

            Assert.NotNull(resolvedMethod);
            Assert.True(SymbolEqualityComparer.Default.Equals(sourceMethod, resolvedMethod));
            Assert.False(resolvedMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
        }

        /// <summary>
        /// Leaves an already source-backed method outside metadata-to-source
        /// binding.
        /// </summary>
        [Fact]
        public void ResolveMethod_SourceMethod_ReturnsNull()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(CreateDependencySource("1.0.0.0"));
            ProjectClosureSemanticContext context = CreateConsumerContext(consumerCompilation);
            context.RegisterSupportingSource(sourceCompilation);

            Assert.Null(SupportingSourceSymbolResolver.ResolveMethod(
                GetRequiredMethod(sourceCompilation), context));
        }

        /// <summary>
        /// Rejects a registered source compilation with the same simple name
        /// but a different full assembly identity.
        /// </summary>
        [Fact]
        public void ResolveMethod_DifferentAssemblyVersion_ReturnsNull()
        {
            (CSharpCompilation referencedCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(CreateDependencySource("1.0.0.0"));
            CSharpCompilation mismatchedSource = CreateSourceCompilation(
                CreateDependencySource("2.0.0.0"));
            ProjectClosureSemanticContext context = CreateConsumerContext(consumerCompilation);
            context.RegisterSupportingSource(mismatchedSource);

            Assert.Equal(referencedCompilation.Assembly.Identity.Name, mismatchedSource.Assembly.Identity.Name);
            Assert.NotEqual(referencedCompilation.Assembly.Identity, mismatchedSource.Assembly.Identity);
            Assert.Null(SupportingSourceSymbolResolver.ResolveMethod(
                GetRequiredMethod(consumerCompilation), context));
        }

        /// <summary>
        /// Returns no binding when no supporting source identity is registered.
        /// </summary>
        [Fact]
        public void ResolveMethod_UnregisteredSupportingIdentity_ReturnsNull()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(CreateDependencySource("1.0.0.0"));
            ProjectClosureSemanticContext context = CreateConsumerContext(consumerCompilation);

            Assert.NotNull(sourceCompilation);
            Assert.Null(SupportingSourceSymbolResolver.ResolveMethod(
                GetRequiredMethod(consumerCompilation), context));
        }

        /// <summary>
        /// Stores source-rebound stable property facts in the canonical graph
        /// target context.
        /// </summary>
        [Fact]
        public void RegisterSummaryMethodTarget_MetadataPropertyFact_StoresSourcePropertyFact()
        {
            const string source =
                "#nullable enable\nnamespace Dependency { " +
                "public sealed class Holder { public object? Value { get; } } " +
                "public static class Validator { public static void Validate(Holder renamed) { } } }";
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(source);
            ProjectClosureSemanticContext context = CreateConsumerContext(consumerCompilation);
            context.RegisterSupportingSource(sourceCompilation);
            IMethodSymbol metadataMethod = consumerCompilation.GetTypeByMetadataName("Dependency.Validator")!
                .GetMembers("Validate")
                .OfType<IMethodSymbol>()
                .Single();
            IMethodSymbol sourceMethod = sourceCompilation.GetTypeByMetadataName("Dependency.Validator")!
                .GetMembers("Validate")
                .OfType<IMethodSymbol>()
                .Single();
            IPropertySymbol metadataProperty = metadataMethod.Parameters[0].Type
                .GetMembers("Value")
                .OfType<IPropertySymbol>()
                .Single();
            IPropertySymbol sourceProperty = sourceMethod.Parameters[0].Type
                .GetMembers("Value")
                .OfType<IPropertySymbol>()
                .Single();
            ExceptionFlowCallContext metadataContext = new(
                metadataMethod,
                Array.Empty<KeyValuePair<int, ExceptionFlowValueFacts>>(),
                new[] { new KeyValuePair<int, ISymbol>(0, metadataProperty) });
            ExceptionFlowSummaryGraph graph = new();

            ExceptionFlowCallableKey targetKey = ExceptionFlowAnalyzer.RegisterSummaryMethodTarget(
                metadataMethod, metadataContext, context, graph);

            Assert.True(SymbolEqualityComparer.Default.Equals(sourceMethod, targetKey.Symbol));
            Assert.True(graph.TryGetCallContext(
                targetKey, out ExceptionFlowCallContext? sourceContext));
            Assert.NotNull(sourceContext);
            Assert.True(sourceContext.IsParameterMemberKnownNonNull(
                sourceMethod.Parameters[0], sourceProperty));
        }

        /// <summary>
        /// Creates dependency source with a controlled assembly version.
        /// </summary>
        /// <param name="version">The assembly version.</param>
        /// <returns>The complete dependency source.</returns>
        private static string CreateDependencySource(string version)
        {
            return
                "using System.Reflection;\n" +
                $"[assembly: AssemblyVersion(\"{version}\")]\n" +
                "namespace Dependency\n" +
                "{\n" +
                "    public static class Service\n" +
                "    {\n" +
                "        public static void Execute(string value) { }\n" +
                "    }\n" +
                "}\n";
        }

        /// <summary>
        /// Creates source and consumer compilations separated by emitted metadata.
        /// </summary>
        /// <param name="source">The complete dependency source.</param>
        /// <returns>The dependency and consumer compilations.</returns>
        private static (CSharpCompilation SourceCompilation, CSharpCompilation ConsumerCompilation) CreateCompilationPair(
            string source)
        {
            CSharpCompilation sourceCompilation = CreateSourceCompilation(source);
            using MemoryStream imageStream = new();
            EmitResult emitResult = sourceCompilation.Emit(imageStream);
            Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
            MetadataReference dependencyReference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(imageStream.ToArray()));
            CSharpCompilation consumerCompilation = CSharpCompilation.Create(
                "SupportingSourceResolverConsumer",
                references: MetadataReferences.Default.Append(dependencyReference),
                options: CreateCompilationOptions());

            return (sourceCompilation, consumerCompilation);
        }

        /// <summary>
        /// Creates one source-backed dependency compilation.
        /// </summary>
        /// <param name="source">The complete dependency source.</param>
        /// <returns>The source compilation.</returns>
        private static CSharpCompilation CreateSourceCompilation(string source)
        {
            return CSharpCompilation.Create(
                "SupportingSourceResolverDependency",
                new[] { CSharpSyntaxTree.ParseText(source, path: "Dependency.cs") },
                MetadataReferences.Default,
                CreateCompilationOptions());
        }

        /// <summary>
        /// Creates the project semantic context for a consumer compilation.
        /// </summary>
        /// <param name="consumerCompilation">The consumer compilation.</param>
        /// <returns>The consumer semantic context.</returns>
        private static ProjectClosureSemanticContext CreateConsumerContext(
            CSharpCompilation consumerCompilation)
        {
            SyntaxTree consumerTree = CSharpSyntaxTree.ParseText(
                "public sealed class Consumer { }",
                path: "Consumer.cs");
            CSharpCompilation compilation = consumerCompilation.AddSyntaxTrees(consumerTree);
            return ProjectClosureSemanticContext.CreateSingleCompilationContext(
                consumerTree, compilation);
        }

        /// <summary>
        /// Gets the unique dependency method from a compilation.
        /// </summary>
        /// <param name="compilation">The compilation to inspect.</param>
        /// <returns>The required method.</returns>
        private static IMethodSymbol GetRequiredMethod(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName("Dependency.Service")!
                .GetMembers("Execute")
                .OfType<IMethodSymbol>()
                .Single();
        }

        /// <summary>
        /// Creates shared library compilation options.
        /// </summary>
        /// <returns>The shared options.</returns>
        private static CSharpCompilationOptions CreateCompilationOptions()
        {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable);
        }
    }
}
