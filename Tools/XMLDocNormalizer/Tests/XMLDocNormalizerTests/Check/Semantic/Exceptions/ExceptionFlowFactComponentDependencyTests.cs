using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Guards the acyclic lower layer shared by context construction and
    /// contextual value-fact discovery.
    /// </summary>
    public sealed class ExceptionFlowFactComponentDependencyTests
    {
        /// <summary>
        /// Ensures all extracted fact components remain closed static
        /// implementation details without interface dispatch.
        /// </summary>
        [Fact]
        public void Components_AreStaticAndImplementNoInterfaces()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowArgumentMapper),
                typeof(ExceptionFlowCallContextFactProjector),
                typeof(ExceptionFlowConditionalWeakTableValueFactsProvider),
                typeof(ExceptionFlowDataFlowFactsProvider),
                typeof(ExceptionFlowDereferenceFactDiscovery),
                typeof(ExceptionFlowEnumValueFactsProvider),
                typeof(ExceptionFlowGuardFactsProvider),
                typeof(ExceptionFlowImmutableMemberValueFactsProvider),
                typeof(ExceptionFlowKnownPropertyValueFactsProvider),
                typeof(ExceptionFlowLocalInitializerFactsProvider),
                typeof(ExceptionFlowNullabilityFactsProvider),
                typeof(ExceptionFlowPrimitiveValueFactsProvider),
                typeof(ExceptionFlowRuntimeDispatchClassifier),
                typeof(ExceptionFlowSequenceCollectionFactsProvider),
                typeof(ExceptionFlowSourcePositionValueFactsProvider),
                typeof(ExceptionFlowStableMemberFacts),
                typeof(ExceptionFlowStableSourceMemberFactsProvider),
                typeof(ExceptionFlowSymbolUsageFacts)
            ];

            Assert.All(componentTypes, type => Assert.True(type.IsAbstract));
            Assert.All(componentTypes, type => Assert.True(type.IsSealed));
            Assert.All(componentTypes, type => Assert.Empty(type.GetInterfaces()));
        }

        /// <summary>
        /// Ensures the lower fact components cannot call back into the
        /// remaining analyzer partials and form a component cycle.
        /// </summary>
        [Fact]
        public void ExtractedComponents_DoNotDependOnAnalyzer()
        {
            string flowDirectory = GetFlowDirectory();
            string[] componentFiles =
            [
                "ExceptionFlowArgumentMapper.cs",
                "ExceptionFlowCallContextFactProjector.cs",
                "ExceptionFlowDataFlowFactsProvider.cs",
                "ExceptionFlowDereferenceFactDiscovery.Callee.cs",
                "ExceptionFlowDereferenceFactDiscovery.cs",
                "ExceptionFlowEnumValueFactsProvider.cs",
                "ExceptionFlowGuardFactsProvider.cs",
                "ExceptionFlowImmutableMemberValueFactsProvider.cs",
                "ExceptionFlowKnownPropertyValueFactsProvider.cs",
                "ExceptionFlowLocalInitializerFactsProvider.cs",
                "ExceptionFlowNullabilityFactsProvider.cs",
                "ExceptionFlowPrimitiveValueFactsProvider.cs",
                "ExceptionFlowRuntimeDispatchClassifier.cs",
                "ExceptionFlowSequenceCollectionFactsProvider.cs",
                "ExceptionFlowSourcePositionValueFactsProvider.cs",
                "ExceptionFlowStableMemberFacts.cs",
                "ExceptionFlowStableSourceMemberFactsProvider.cs",
                "ExceptionFlowSymbolUsageFacts.cs"
            ];

            Assert.All(
                componentFiles,
                file => Assert.DoesNotContain(
                    "ExceptionFlowAnalyzer",
                    File.ReadAllText(Path.Combine(flowDirectory, file)),
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures every partial declaration of the stateless table-fact
        /// provider remains free of Analyzer back references even though the
        /// cache owner remains in the same source file.
        /// </summary>
        [Fact]
        public void ConditionalWeakTableProvider_DoesNotDependOnAnalyzer()
        {
            string source = File.ReadAllText(
                Path.Combine(
                    GetFlowDirectory(),
                    "ExceptionFlowAnalyzer.ConditionalWeakTableValueFacts.cs"));
            CompilationUnitSyntax root =
                CSharpSyntaxTree.ParseText(source)
                    .GetCompilationUnitRoot();
            ClassDeclarationSyntax[] declarations =
                root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(
                        static declaration =>
                            declaration.Identifier.ValueText ==
                                nameof(ExceptionFlowConditionalWeakTableValueFactsProvider))
                    .ToArray();

            Assert.Equal(3, declarations.Length);
            Assert.All(
                declarations,
                declaration => Assert.DoesNotContain(
                    nameof(ExceptionFlowAnalyzer),
                    declaration.ToFullString(),
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures dereference discovery owns the one successful-dereference
        /// cache and that the old analyzer owner no longer has a copy.
        /// </summary>
        [Fact]
        public void DereferenceDiscovery_IsSoleSuccessfulDereferenceCacheOwner()
        {
            const string fieldName = "successfulDereferenceCaches";
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Assert.NotNull(
                typeof(ExceptionFlowDereferenceFactDiscovery).GetField(
                    fieldName,
                    flags));
            Assert.Null(
                typeof(ExceptionFlowAnalyzer).GetField(
                    fieldName,
                    flags));
        }

        /// <summary>
        /// Ensures the extracted lower layer does not acquire summary,
        /// executable-entry, or historical semantic-environment dependencies.
        /// </summary>
        [Fact]
        public void ExtractedComponents_ExcludeForbiddenDependencies()
        {
            string flowDirectory = GetFlowDirectory();
            string source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        flowDirectory,
                        "ExceptionFlow*Provider.cs",
                        SearchOption.TopDirectoryOnly)
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowStableMemberFacts.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowSymbolUsageFacts.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowArgumentMapper.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowCallContextFactProjector.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowRuntimeDispatchClassifier.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDereferenceFactDiscovery.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDereferenceFactDiscovery.Callee.cs"))
                    .Select(File.ReadAllText));

            string[] forbiddenNames =
            [
                "ExceptionFlowSummaryGraphEvaluator",
                "ProjectClosureSemanticContext",
                "SemanticCompilationScope",
                "SupportingSourceSymbolResolver",
                "CrossCompilationSymbolResolver",
                "Program.Main"
            ];

            Assert.All(
                forbiddenNames,
                name => Assert.DoesNotContain(
                    name,
                    source,
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures compilation-local semantic-model resolution cannot acquire
        /// runtime dispatch or implementation interfaces.
        /// </summary>
        [Fact]
        public void SemanticModelResolution_IsConcreteAndNonvirtual()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            MethodInfo[] resolutionMethods =
                typeof(ExceptionFlowSemanticScope)
                    .GetMethods(flags)
                    .Where(
                        static method =>
                            method.Name is
                                "TryGetSemanticModel" or
                                "GetSemanticModelForSyntaxTree")
                    .ToArray();

            Assert.Equal(3, resolutionMethods.Length);
            Assert.All(
                resolutionMethods,
                method => Assert.False(method.IsVirtual));
            Assert.Empty(typeof(ExceptionFlowSemanticScope).GetInterfaces());
        }

        /// <summary>
        /// Locates the exception-flow source directory from the test process.
        /// </summary>
        /// <returns>The absolute exception-flow source directory.</returns>
        private static string GetFlowDirectory()
        {
            string[] startingDirectories =
            [
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            ];

            foreach (string startingDirectory in startingDirectories)
            {
                DirectoryInfo? directory = new(startingDirectory);

                while (directory != null)
                {
                    string solutionPath = Path.Combine(
                        directory.FullName,
                        "XMLDocNormalizer.sln");

                    if (File.Exists(solutionPath))
                    {
                        return Path.Combine(
                            directory.FullName,
                            "src",
                            "XMLDocNormalizer",
                            "Checks",
                            "Infrastructure",
                            "Exception",
                            "Flow");
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException(
                "Could not locate XMLDocNormalizer.sln from the current test execution directories.");
        }
    }
}
