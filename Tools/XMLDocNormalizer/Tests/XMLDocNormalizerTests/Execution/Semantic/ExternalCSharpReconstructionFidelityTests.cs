using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Compares controlled original compilations with their complete
    /// P3/P4/P5 reconstructed counterparts at exception-analysis boundaries.
    /// </summary>
    public sealed class ExternalCSharpReconstructionFidelityTests
    {
        /// <summary>
        /// Compares own-source and external calls across overloads, generics,
        /// inheritance, interfaces, overrides, extensions, optional arguments,
        /// params arguments, and named arguments.
        /// </summary>
        [Fact]
        public void SymbolBindingSurface_MatchesOriginalCompilation()
        {
            using FidelityWorkspace workspace = new();
            PortableExecutableReference dependency = workspace.CreateReference(
                "BindingDependency",
                """
                namespace Alpha
                {
                    public interface IService { string Execute(); }
                    public class Base
                    {
                        public virtual string Virtual() => "base";
                        protected static int ProtectedValue() => 7;
                    }
                    public sealed class Derived : Base, IService
                    {
                        public override string Virtual() => "derived";
                        public string Execute() => "service";
                    }
                    public static class ExternalApi
                    {
                        public static string M(int value) => "int";
                        public static string M(string value) => "string";
                        public static string M(object value) => "object";
                        public static T Identity<T>(T value) => value;
                        public static T StructIdentity<T>(T value) where T : struct => value;
                        public static void Optional(int value = 42) { }
                        public static void Many(params int[] values) { }
                    }
                    public sealed class Container<T>
                    {
                        public T Echo(T value) => value;
                    }
                    public static class Extensions
                    {
                        public static int WordCount(this string value) => value.Length;
                    }
                }
                """);
            SourceInput[] sources =
            [
                new(
                    "/_/OwnA.cs",
                    """
                    public static class OwnA
                    {
                        public static void Call() => OwnB.ThrowingMethod();
                        private static void Hidden() { }
                        public static void CallHidden() => Hidden();
                    }
                    """),
                new(
                    "/_/OwnB.cs",
                    """
                    public static class OwnB
                    {
                        public static void ThrowingMethod() =>
                            throw new System.InvalidOperationException();
                    }
                    """),
                new(
                    "/_/Bindings.cs",
                    """
                    using Alpha;
                    public sealed class BindingConsumer : Base
                    {
                        public void Run()
                        {
                            ExternalApi.M(1);
                            ExternalApi.M("value");
                            ExternalApi.M((object)"value");
                            ExternalApi.Identity("value");
                            ExternalApi.StructIdentity(1);
                            new Container<string>().Echo("value");
                            IService service = new Derived();
                            service.Execute();
                            new Derived().Virtual();
                            "two words".WordCount();
                            ExternalApi.Optional();
                            ExternalApi.Many(1, 2, 3);
                            ExternalApi.Optional(value: 7);
                            _ = ProtectedValue();
                        }
                    }
                    """)
            ];

            ReconstructionPair pair = workspace.Reconstruct(
                "Binding.Target",
                sources,
                additionalReferences: [dependency]);

            AssertNoErrors(pair.Original);
            AssertNoErrors(pair.Reconstructed);
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));

            IMethodSymbol originalInterfaceImplementation = ReadInterfaceImplementation(
                pair.Original,
                "Alpha.IService",
                "Alpha.Derived");
            IMethodSymbol reconstructedInterfaceImplementation = ReadInterfaceImplementation(
                pair.Reconstructed,
                "Alpha.IService",
                "Alpha.Derived");
            Assert.Equal(
                MethodSignature(originalInterfaceImplementation),
                MethodSignature(reconstructedInterfaceImplementation));

            IMethodSymbol originalOverride = ReadMethod(
                pair.Original,
                "Alpha.Derived",
                "Virtual");
            IMethodSymbol reconstructedOverride = ReadMethod(
                pair.Reconstructed,
                "Alpha.Derived",
                "Virtual");
            Assert.Equal(
                MethodSignature(originalOverride.OverriddenMethod!),
                MethodSignature(reconstructedOverride.OverriddenMethod!));

            IMethodSymbol extension = FindInvocation(
                pair.Reconstructed,
                invocation => invocation.Expression.ToString().EndsWith("WordCount", StringComparison.Ordinal));
            Assert.NotNull(extension.ReducedFrom);
            Assert.Equal("Extensions", extension.ReducedFrom.ContainingType.Name);

            IMethodSymbol optional = FindInvocation(
                pair.Reconstructed,
                invocation => invocation.ToString() == "ExternalApi.Optional()");
            Assert.True(optional.Parameters[0].HasExplicitDefaultValue);
            Assert.Equal(42, optional.Parameters[0].ExplicitDefaultValue);

            IMethodSymbol paramsMethod = FindInvocation(
                pair.Reconstructed,
                invocation => invocation.Expression.ToString().EndsWith("Many", StringComparison.Ordinal));
            Assert.True(paramsMethod.Parameters[0].IsParams);

            IMethodSymbol generic = FindInvocation(
                pair.Reconstructed,
                invocation => invocation.Expression.ToString().EndsWith("Identity", StringComparison.Ordinal)
                    && !invocation.Expression.ToString().Contains("Struct", StringComparison.Ordinal));
            Assert.Equal("string", generic.TypeArguments[0].ToDisplayString());
            Assert.Equal("T", generic.OriginalDefinition.TypeParameters[0].Name);

            IMethodSymbol constructedTypeMethod = FindInvocation(
                pair.Reconstructed,
                invocation => invocation.Expression.ToString().EndsWith("Echo", StringComparison.Ordinal));
            Assert.Equal("string", constructedTypeMethod.ContainingType.TypeArguments[0].ToDisplayString());
        }

        /// <summary>
        /// Compares public, protected, private, and friend-assembly access.
        /// </summary>
        [Fact]
        public void AccessibilityAndInternalsVisibleTo_MatchOriginalCompilation()
        {
            using FidelityWorkspace workspace = new();
            PortableExecutableReference dependency = workspace.CreateReference(
                "FriendDependency",
                """
                using System.Runtime.CompilerServices;
                [assembly: InternalsVisibleTo("Friend.Target")]
                namespace Friend
                {
                    internal static class InternalApi
                    {
                        internal static void Execute() { }
                    }
                    public class Base
                    {
                        protected static void ProtectedCall() { }
                    }
                }
                """);
            ReconstructionPair pair = workspace.Reconstruct(
                "Friend.Target",
                [
                    new SourceInput(
                        "/_/Friend.cs",
                        """
                        public sealed class Consumer : Friend.Base
                        {
                            private static void PrivateCall() { }
                            public static void Run()
                            {
                                Friend.InternalApi.Execute();
                                PrivateCall();
                                ProtectedCall();
                            }
                        }
                        """)
                ],
                additionalReferences: [dependency]);

            AssertNoErrors(pair.Original);
            AssertNoErrors(pair.Reconstructed);
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
            Assert.Contains(
                ReadInvocationMethods(pair.Reconstructed),
                method => method.Name == "Execute"
                    && method.DeclaredAccessibility == Accessibility.Internal);
            Assert.Contains(
                ReadInvocationMethods(pair.Reconstructed),
                method => method.Name == "PrivateCall"
                    && method.DeclaredAccessibility == Accessibility.Private);
            Assert.Contains(
                ReadInvocationMethods(pair.Reconstructed),
                method => method.Name == "ProtectedCall"
                    && method.DeclaredAccessibility == Accessibility.Protected);
        }

        /// <summary>
        /// Compares namespace aliases and two distinct aliases over the same
        /// dependency binary without deduplication.
        /// </summary>
        [Fact]
        public void NamespaceAndExternAliasBinding_MatchesOriginalCompilation()
        {
            using FidelityWorkspace workspace = new();
            PortableExecutableReference dependency = workspace.CreateReference(
                "AliasDependency",
                """
                namespace One { public sealed class Shared { public static void Execute() { } } }
                namespace Two { public sealed class Shared { public static void Execute() { } } }
                """);
            PortableExecutableReference left = dependency.WithProperties(
                MetadataReferenceProperties.Assembly.WithAliases(["left"]));
            PortableExecutableReference right = dependency.WithProperties(
                MetadataReferenceProperties.Assembly.WithAliases(["right"]));
            ReconstructionPair pair = workspace.Reconstruct(
                "Alias.Target",
                [
                    new SourceInput(
                        "/_/Aliases.cs",
                        """
                        extern alias left;
                        extern alias right;
                        using OneType = left::One.Shared;
                        using TwoType = right::Two.Shared;
                        public static class AliasConsumer
                        {
                            public static void Run()
                            {
                                OneType.Execute();
                                TwoType.Execute();
                            }
                        }
                        """)
                ],
                additionalReferences: [left, right]);

            AssertNoErrors(pair.Original);
            AssertNoErrors(pair.Reconstructed);
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
            PortableExecutableReference reconstructedAliasReference = Assert.Single(
                pair.Reconstructed.References
                    .OfType<PortableExecutableReference>()
                    .Where(reference => reference.Display == dependency.FilePath));
            Assert.Equal(["left", "right"], reconstructedAliasReference.Properties.Aliases);
            Assert.Equal(
                new[] { "One.Shared", "Two.Shared" },
                ReadInvocationMethods(pair.Reconstructed)
                    .Select(method => method.ContainingType.ToDisplayString()));
        }

        /// <summary>
        /// Confirms that a real linked-module target fails closed when Roslyn
        /// omits that module from P5A metadata-reference provenance.
        /// </summary>
        [Fact]
        public void RealPipelineNetModuleReferenceWithoutP5AOrdinal_FailsClosed()
        {
            using FidelityWorkspace workspace = new();
            PortableExecutableReference module = workspace.CreateReference(
                "ModuleDependency",
                "public static class ModuleApi { public static void Execute() { } }",
                OutputKind.NetModule);
            ReconstructionPair pair = workspace.Reconstruct(
                "Module.Target",
                [
                    new SourceInput(
                        "/_/Module.cs",
                        "public static class Consumer { public static void Run() => ModuleApi.Execute(); }")
                ],
                additionalReferences: [module],
                expectCompositionSuccess: false);

            AssertNoErrors(pair.Original);
            Assert.Null(pair.Reconstructed);
        }

        /// <summary>
        /// Compares all four nullable modes and their symbol annotations.
        /// </summary>
        [Theory]
        [InlineData(NullableContextOptions.Disable)]
        [InlineData(NullableContextOptions.Warnings)]
        [InlineData(NullableContextOptions.Annotations)]
        [InlineData(NullableContextOptions.Enable)]
        public void NullableAnnotations_MatchOriginalCompilation(
            NullableContextOptions nullableContext)
        {
            using FidelityWorkspace workspace = new();
            CSharpCompilationOptions options = DefaultCompilationOptions()
                .WithNullableContextOptions(nullableContext);
            ReconstructionPair pair = workspace.Reconstruct(
                "Nullable.Target." + nullableContext,
                [
                    new SourceInput(
                        "/_/Nullable.cs",
                        """
                        public static class NullableApi
                        {
                            public static string? Maybe(string? value) => value;
                            public static string Sure(string value) => value;
                        }
                        """)
                ],
                compilationOptions: options);

            Assert.Equal(
                nullableContext,
                pair.Reconstructed.Options.NullableContextOptions);
            Assert.Equal(
                MethodSignature(ReadMethod(pair.Original, "NullableApi", "Maybe")),
                MethodSignature(ReadMethod(pair.Reconstructed, "NullableApi", "Maybe")));
            Assert.Equal(
                MethodSignature(ReadMethod(pair.Original, "NullableApi", "Sure")),
                MethodSignature(ReadMethod(pair.Reconstructed, "NullableApi", "Sure")));
        }

        /// <summary>
        /// Compares checked operations, unsafe symbols, defines, and concrete
        /// language versions through the complete pipeline.
        /// </summary>
        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp12)]
        public void ParseAndBodySemantics_MatchOriginalCompilation(
            LanguageVersion languageVersion)
        {
            using FidelityWorkspace workspace = new();
            CSharpParseOptions parseOptions = new(
                languageVersion,
                DocumentationMode.Parse,
                SourceCodeKind.Regular,
                ["FEATURE"]);
            CSharpCompilationOptions compilationOptions = DefaultCompilationOptions()
                .WithOverflowChecks(true)
                .WithAllowUnsafe(true);
            ReconstructionPair pair = workspace.Reconstruct(
                "BodySemantics.Target." + languageVersion,
                [
                    new SourceInput(
                        "/_/Body.cs",
                        """
                        #if FEATURE
                        public sealed class ActiveFeature { }
                        #else
                        public sealed class InactiveFeature { }
                        #endif
                        public static unsafe class BodySemantics
                        {
                            public static int Add(int left, int right) => left + right;
                            public static int* Identity(int* value) => value;
                        }
                        """)
                ],
                parseOptions,
                compilationOptions);

            AssertNoErrors(pair.Original);
            AssertNoErrors(pair.Reconstructed);
            Assert.Equal(
                languageVersion,
                ((CSharpParseOptions)pair.Reconstructed.SyntaxTrees[0].Options).LanguageVersion);
            Assert.Equal(
                parseOptions.PreprocessorSymbolNames,
                ((CSharpParseOptions)pair.Reconstructed.SyntaxTrees[0].Options)
                    .PreprocessorSymbolNames);
            Assert.NotNull(pair.Reconstructed.GetTypeByMetadataName("ActiveFeature"));
            Assert.Null(pair.Reconstructed.GetTypeByMetadataName("InactiveFeature"));
            Assert.True(pair.Reconstructed.Options.CheckOverflow);
            Assert.True(pair.Reconstructed.Options.AllowUnsafe);
            Assert.False(string.IsNullOrWhiteSpace(pair.Configuration.CompilerVersion));
            Assert.False(string.IsNullOrWhiteSpace(pair.Configuration.RuntimeVersion));
            Assert.Equal(
                new Version(5, 0, 0, 0),
                typeof(CSharpCompilation).Assembly.GetName().Version);
            Assert.Equal(ReadBinaryIsChecked(pair.Original), ReadBinaryIsChecked(pair.Reconstructed));
            Assert.True(ReadBinaryIsChecked(pair.Reconstructed));
            Assert.Equal(
                MethodSignature(ReadMethod(pair.Original, "BodySemantics", "Identity")),
                MethodSignature(ReadMethod(pair.Reconstructed, "BodySemantics", "Identity")));
        }

        /// <summary>
        /// Compares supported optimization, platform, and assembly output-kind
        /// combinations without attributing IL-level fidelity to P5.
        /// </summary>
        [Theory]
        [InlineData(OptimizationLevel.Debug, Platform.AnyCpu, OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OptimizationLevel.Release, Platform.X86, OutputKind.ConsoleApplication)]
        [InlineData(OptimizationLevel.Release, Platform.X64, OutputKind.WindowsApplication)]
        public void SupportedCompilationOptions_MatchAndPreserveBinding(
            OptimizationLevel optimization,
            Platform platform,
            OutputKind outputKind)
        {
            using FidelityWorkspace workspace = new();
            CSharpCompilationOptions options = new(
                outputKind,
                optimizationLevel: optimization,
                platform: platform,
                deterministic: true);
            ReconstructionPair pair = workspace.Reconstruct(
                "Options.Target." + outputKind,
                [
                    new SourceInput(
                        "/_/Program.cs",
                        """
                        public static class Program
                        {
                            public static void Main() => Helper.Execute();
                        }
                        public static class Helper
                        {
                            public static void Execute() { }
                        }
                        """)
                ],
                compilationOptions: options);

            Assert.Equal(optimization, pair.Reconstructed.Options.OptimizationLevel);
            Assert.Equal(platform, pair.Reconstructed.Options.Platform);
            Assert.Equal(outputKind, pair.Reconstructed.Options.OutputKind);
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
        }

        /// <summary>
        /// Demonstrates that warning and deterministic settings are omitted
        /// while ordinary symbol binding remains semantically sufficient.
        /// </summary>
        [Fact]
        public void WarningAndDeterministicDifferences_DoNotChangeBinding()
        {
            using FidelityWorkspace workspace = new();
            CSharpCompilationOptions options = DefaultCompilationOptions()
                .WithWarningLevel(0)
                .WithSpecificDiagnosticOptions(
                    ImmutableDictionary<string, ReportDiagnostic>.Empty.Add(
                        "CS0168",
                        ReportDiagnostic.Suppress))
                .WithDeterministic(true);
            ReconstructionPair pair = workspace.Reconstruct(
                "Diagnostics.Target",
                [
                    new SourceInput(
                        "/_/Diagnostics.cs",
                        """
                        public static class DiagnosticsTarget
                        {
                            public static void Run()
                            {
                                int unused;
                                Helper.Execute();
                            }
                        }
                        public static class Helper { public static void Execute() { } }
                        """)
                ],
                compilationOptions: options);

            Assert.Equal(0, pair.Original.Options.WarningLevel);
            Assert.Equal(4, pair.Reconstructed.Options.WarningLevel);
            Assert.Contains("CS0168", pair.Original.Options.SpecificDiagnosticOptions.Keys);
            Assert.Empty(pair.Reconstructed.Options.SpecificDiagnosticOptions);
            Assert.True(pair.Original.Options.Deterministic);
            Assert.False(pair.Reconstructed.Options.Deterministic);
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
        }

        /// <summary>
        /// Demonstrates that MainType omission changes entry-point selection,
        /// but not normal method binding inside the reconstructed sources.
        /// </summary>
        [Fact]
        public void MainTypeIsNotReconstructed_NormalBindingStillMatches()
        {
            using FidelityWorkspace workspace = new();
            CSharpCompilationOptions options = new(
                OutputKind.ConsoleApplication,
                mainTypeName: "First",
                deterministic: true);
            ReconstructionPair pair = workspace.Reconstruct(
                "MainType.Target",
                [
                    new SourceInput(
                        "/_/Program.cs",
                        """
                        public static class First
                        {
                            public static void Main() => Helper.Execute();
                        }
                        public static class Second { public static void Main() { } }
                        public static class Helper { public static void Execute() { } }
                        """)
                ],
                compilationOptions: options);

            Assert.Equal("First", pair.Original.Options.MainTypeName);
            Assert.Null(pair.Reconstructed.Options.MainTypeName);
            Assert.Equal("First", pair.Original.GetEntryPoint(default)!.ContainingType.Name);
            Assert.Null(pair.Reconstructed.GetEntryPoint(default));
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
        }

        /// <summary>
        /// Demonstrates that MetadataImportOptions is not reconstructed and
        /// can change visibility of non-public metadata members.
        /// </summary>
        [Fact]
        public void MetadataImportOptionsAffectNonpublicMetadataSurface()
        {
            using FidelityWorkspace workspace = new();
            PortableExecutableReference dependency = workspace.CreateReference(
                "ImportDependency",
                """
                public static class ImportApi
                {
                    public static void PublicCall() { }
                    internal static void InternalHidden() { }
                    private static void PrivateHidden() { }
                }
                """);
            CSharpCompilationOptions options = DefaultCompilationOptions()
                .WithMetadataImportOptions(MetadataImportOptions.All);
            ReconstructionPair pair = workspace.Reconstruct(
                "Import.Target",
                [
                    new SourceInput(
                        "/_/Import.cs",
                        "public static class Consumer { public static void Run() => ImportApi.PublicCall(); }")
                ],
                compilationOptions: options,
                additionalReferences: [dependency]);

            Assert.Equal(MetadataImportOptions.All, pair.Original.Options.MetadataImportOptions);
            Assert.Equal(MetadataImportOptions.Public, pair.Reconstructed.Options.MetadataImportOptions);
            Assert.Single(pair.Original.GetTypeByMetadataName("ImportApi")!.GetMembers("PrivateHidden"));
            Assert.Empty(pair.Reconstructed.GetTypeByMetadataName("ImportApi")!.GetMembers("PrivateHidden"));
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
        }

        /// <summary>
        /// Confirms the deliberate documentation-mode analysis policy while
        /// retaining ordinary source symbol binding and regular source kind.
        /// </summary>
        [Fact]
        public void DocumentationModeIsAnalysisPolicy_NormalBindingMatches()
        {
            using FidelityWorkspace workspace = new();
            CSharpParseOptions originalParseOptions = new(
                LanguageVersion.CSharp12,
                DocumentationMode.Diagnose,
                SourceCodeKind.Regular);
            ReconstructionPair pair = workspace.Reconstruct(
                "DocumentationMode.Target",
                [
                    new SourceInput(
                        "/_/Documentation.cs",
                        """
                        /// <summary>Calls the helper.</summary>
                        public static class Documented
                        {
                            public static void Run() => Helper.Execute();
                        }
                        public static class Helper { public static void Execute() { } }
                        """)
                ],
                originalParseOptions);

            Assert.Equal(DocumentationMode.Diagnose, pair.Original.SyntaxTrees[0].Options.DocumentationMode);
            Assert.Equal(DocumentationMode.Parse, pair.Reconstructed.SyntaxTrees[0].Options.DocumentationMode);
            Assert.Equal(SourceCodeKind.Regular, pair.Reconstructed.SyntaxTrees[0].Options.Kind);
            Assert.Equal(
                ReadInvocationSignatures(pair.Original),
                ReadInvocationSignatures(pair.Reconstructed));
        }

        /// <summary>
        /// Proves that reconstructed source-backed dependency bodies expose
        /// cross-tree symbols, syntax references, locations, semantic models,
        /// and the throw syntax needed by later exception-flow analysis.
        /// </summary>
        [Fact]
        public void SourceBackedDependency_ProvidesExceptionFlowPrerequisites()
        {
            using FidelityWorkspace workspace = new();
            ReconstructionPair dependency = workspace.Reconstruct(
                "SourceBacked.Dependency",
                [
                    new SourceInput(
                        "/_/DependencyA.cs",
                        """
                        public static class Dependency
                        {
                            public static void A() => DependencyBody.B();
                        }
                        """),
                    new SourceInput(
                        "/_/DependencyB.cs",
                        """
                        public static class DependencyBody
                        {
                            public static void B() =>
                                throw new System.InvalidOperationException();
                        }
                        """)
                ]);
            PortableExecutableReference dependencyReference = workspace.CreateReferenceFromImage(
                "SourceBacked.Dependency.dll",
                dependency.PeImage);
            ReconstructionPair target = workspace.Reconstruct(
                "SourceBacked.Target",
                [
                    new SourceInput(
                        "/_/Target.cs",
                        "public static class Target { public static void Run() => Dependency.A(); }")
                ],
                additionalReferences: [dependencyReference]);

            IMethodSymbol metadataA = Assert.Single(ReadInvocationMethods(target.Reconstructed));
            IMethodSymbol sourceA = Assert.IsAssignableFrom<IMethodSymbol>(
                ExceptionFlowCrossCompilationResolver.ResolveMethod(
                    metadataA,
                    dependency.Reconstructed));
            Assert.Equal("A", sourceA.Name);
            SyntaxReference sourceAReference = Assert.Single(sourceA.DeclaringSyntaxReferences);
            Assert.Equal("/_/DependencyA.cs", sourceAReference.SyntaxTree.FilePath);
            Assert.Contains(sourceA.Locations, location => location.IsInSource);

            MethodDeclarationSyntax sourceADeclaration = Assert.IsType<MethodDeclarationSyntax>(
                sourceAReference.GetSyntax());
            InvocationExpressionSyntax invocationB = Assert.Single(
                sourceADeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>());
            SemanticModel modelA = dependency.Reconstructed.GetSemanticModel(
                sourceAReference.SyntaxTree);
            IMethodSymbol sourceB = Assert.IsAssignableFrom<IMethodSymbol>(
                modelA.GetSymbolInfo(invocationB).Symbol);
            SyntaxReference sourceBReference = Assert.Single(sourceB.DeclaringSyntaxReferences);
            Assert.Equal("/_/DependencyB.cs", sourceBReference.SyntaxTree.FilePath);

            MethodDeclarationSyntax sourceBDeclaration = Assert.IsType<MethodDeclarationSyntax>(
                sourceBReference.GetSyntax());
            ThrowExpressionSyntax throwExpression = Assert.Single(
                sourceBDeclaration.DescendantNodes().OfType<ThrowExpressionSyntax>());
            ObjectCreationExpressionSyntax exceptionCreation = Assert.IsType<
                ObjectCreationExpressionSyntax>(throwExpression.Expression);
            SemanticModel modelB = dependency.Reconstructed.GetSemanticModel(
                sourceBReference.SyntaxTree);
            IMethodSymbol exceptionConstructor = Assert.IsAssignableFrom<IMethodSymbol>(
                modelB.GetSymbolInfo(exceptionCreation).Symbol);

            Assert.Equal("System.InvalidOperationException", exceptionConstructor.ContainingType.ToDisplayString());
            Assert.Equal(
                MethodSignature(FindInvocation(dependency.Original, _ => true)),
                MethodSignature(sourceB));
        }

        /// <summary>
        /// Confirms that unsupported DebugPlus and nonzero portability-policy
        /// values remain fail-closed boundaries.
        /// </summary>
        [Theory]
        [InlineData("optimization", "debug-plus")]
        [InlineData("optimization", "release-debug-plus")]
        [InlineData("portability-policy", "1")]
        [InlineData("portability-policy", "2")]
        [InlineData("portability-policy", "3")]
        public void UnsupportedP5GValues_RemainFailClosed(string key, string value)
        {
            ExternalCompilationProvenanceDescriptor provenance = CreateOptionsProvenance(
                (key, value));

            Assert.False(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                provenance,
                out ExternalCSharpCompilationConfiguration configuration));
            Assert.Null(configuration);
        }

        /// <summary>
        /// Reads whether the controlled addition is checked in the semantic
        /// operation tree.
        /// </summary>
        private static bool ReadBinaryIsChecked(CSharpCompilation compilation)
        {
            BinaryExpressionSyntax binary = Assert.Single(
                compilation.SyntaxTrees
                    .SelectMany(tree => tree.GetRoot().DescendantNodes())
                    .OfType<BinaryExpressionSyntax>()
                    .Where(node => node.IsKind(SyntaxKind.AddExpression)));
            IBinaryOperation operation = Assert.IsAssignableFrom<IBinaryOperation>(
                compilation.GetSemanticModel(binary.SyntaxTree).GetOperation(binary));
            return operation.IsChecked;
        }

        /// <summary>
        /// Reads ordered invocation target signatures from a compilation.
        /// </summary>
        private static ImmutableArray<string> ReadInvocationSignatures(
            CSharpCompilation compilation)
        {
            return ReadInvocationMethods(compilation)
                .Select(MethodSignature)
                .ToImmutableArray();
        }

        /// <summary>
        /// Resolves every invocation target in tree and source order.
        /// </summary>
        private static ImmutableArray<IMethodSymbol> ReadInvocationMethods(
            CSharpCompilation compilation)
        {
            ImmutableArray<IMethodSymbol>.Builder methods = ImmutableArray.CreateBuilder<IMethodSymbol>();

            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(tree);

                foreach (InvocationExpressionSyntax invocation in tree.GetRoot()
                             .DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    methods.Add(Assert.IsAssignableFrom<IMethodSymbol>(
                        model.GetSymbolInfo(invocation).Symbol));
                }
            }

            return methods.ToImmutable();
        }

        /// <summary>
        /// Finds one invocation method satisfying a syntax predicate.
        /// </summary>
        private static IMethodSymbol FindInvocation(
            CSharpCompilation compilation,
            Func<InvocationExpressionSyntax, bool> predicate)
        {
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(tree);
                InvocationExpressionSyntax? invocation = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(predicate);

                if (invocation != null)
                {
                    return Assert.IsAssignableFrom<IMethodSymbol>(
                        model.GetSymbolInfo(invocation).Symbol);
                }
            }

            throw new InvalidOperationException("The expected invocation was not found.");
        }

        /// <summary>
        /// Creates a stable semantic signature without comparing symbol object
        /// identity between compilations.
        /// </summary>
        private static string MethodSignature(IMethodSymbol method)
        {
            SymbolDisplayFormat format = SymbolDisplayFormat.CSharpErrorMessageFormat
                .WithMiscellaneousOptions(
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                    | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            return string.Join(
                "|",
                method.Kind,
                method.MethodKind,
                method.DeclaredAccessibility,
                method.MetadataName,
                method.Arity,
                method.ContainingNamespace.ToDisplayString(),
                method.ContainingType.ToDisplayString(format),
                method.ReturnType.ToDisplayString(format),
                method.ReturnNullableAnnotation,
                string.Join(
                    ";",
                    method.Parameters.Select(parameter => string.Join(
                        ":",
                        parameter.RefKind,
                        parameter.Type.ToDisplayString(format),
                        parameter.NullableAnnotation,
                        parameter.IsParams))));
        }

        /// <summary>
        /// Reads one named method from a metadata or source type.
        /// </summary>
        private static IMethodSymbol ReadMethod(
            CSharpCompilation compilation,
            string metadataTypeName,
            string methodName)
        {
            INamedTypeSymbol type = Assert.IsAssignableFrom<INamedTypeSymbol>(
                compilation.GetTypeByMetadataName(metadataTypeName));
            return Assert.Single(type.GetMembers(methodName).OfType<IMethodSymbol>());
        }

        /// <summary>
        /// Resolves one interface member to its implementing method.
        /// </summary>
        private static IMethodSymbol ReadInterfaceImplementation(
            CSharpCompilation compilation,
            string interfaceName,
            string implementationName)
        {
            INamedTypeSymbol interfaceType = Assert.IsAssignableFrom<INamedTypeSymbol>(
                compilation.GetTypeByMetadataName(interfaceName));
            INamedTypeSymbol implementationType = Assert.IsAssignableFrom<INamedTypeSymbol>(
                compilation.GetTypeByMetadataName(implementationName));
            IMethodSymbol member = Assert.Single(
                interfaceType.GetMembers().OfType<IMethodSymbol>());
            return Assert.IsAssignableFrom<IMethodSymbol>(
                implementationType.FindImplementationForInterfaceMember(member));
        }

        /// <summary>
        /// Requires a compilation to contain no error diagnostics.
        /// </summary>
        private static void AssertNoErrors(CSharpCompilation compilation)
        {
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Creates the default options used by controlled fidelity builds.
        /// </summary>
        private static CSharpCompilationOptions DefaultCompilationOptions()
        {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                deterministic: true);
        }

        /// <summary>
        /// Creates valid option provenance with one keyed replacement.
        /// </summary>
        private static ExternalCompilationProvenanceDescriptor CreateOptionsProvenance(
            (string Key, string Value) replacement)
        {
            List<ExternalCompilationOption> options =
            [
                new("version", "2"),
                new("compiler-version", "5.0.0-test"),
                new("language", "C#"),
                new("source-file-count", "1"),
                new("output-kind", "DynamicallyLinkedLibrary"),
                new("platform", "AnyCpu"),
                new("runtime-version", "test-runtime"),
                new("language-version", "12.0")
            ];
            options.RemoveAll(option => option.Key == replacement.Key);
            options.Add(new ExternalCompilationOption(replacement.Key, replacement.Value));
            ExternalPeDebugDirectoryDescriptor debug = new(
                new ExternalModuleIdentity("synthetic.dll", Guid.Empty),
                isDeterministic: false,
                ImmutableArray<ExternalCodeViewPdbReference>.Empty,
                ImmutableArray<System.Reflection.Metadata.BlobContentId>.Empty,
                ImmutableArray<ExternalPdbChecksum>.Empty);
            ExternalPortablePdbDescriptor pdb = new(
                default,
                PortablePdbValidationKind.Identity,
                ImmutableArray<ExternalSourceDocumentDescriptor>.Empty,
                sourceLink: null);
            return new ExternalCompilationProvenanceDescriptor(
                debug,
                pdb,
                new ExternalCompilationOptionsDescriptor(options.ToImmutableArray()),
                metadataReferences: null);
        }

        /// <summary>
        /// One controlled source path and content snapshot.
        /// </summary>
        internal sealed record SourceInput(string Path, string Source);

        /// <summary>
        /// Stores the original and completely reconstructed compilations.
        /// </summary>
        internal sealed record ReconstructionPair(
            CSharpCompilation Original,
            CSharpCompilation Reconstructed,
            ExternalCSharpCompilationConfiguration Configuration,
            byte[] PeImage,
            ExternalSupportingSourceCompilation? SupportingSource,
            ExternalSupportingSourceCompilation? ConflictingSupportingSource);

        /// <summary>
        /// Owns explicit candidate files and executes the complete P3/P4/P5
        /// reconstruction pipeline for controlled test compilations.
        /// </summary>
        internal sealed class FidelityWorkspace : IDisposable
        {
            /// <summary>
            /// Retains controlled candidate images for in-memory multi-module
            /// target metadata composition in tests.
            /// </summary>
            private readonly Dictionary<string, byte[]> imagesByPath =
                new(StringComparer.Ordinal);

            /// <summary>
            /// Initializes one isolated explicit-candidate directory.
            /// </summary>
            public FidelityWorkspace()
            {
                DirectoryPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "XMLDocNormalizerTests",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(DirectoryPath);
            }

            /// <summary>
            /// Gets the isolated candidate directory.
            /// </summary>
            public string DirectoryPath { get; }

            /// <summary>
            /// Emits one controlled assembly or module candidate.
            /// </summary>
            public PortableExecutableReference CreateReference(
                string assemblyName,
                string source,
                OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
                MetadataReferenceProperties? properties = null)
            {
                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    [CSharpSyntaxTree.ParseText(source, path: "/_/Dependency.cs")],
                    MetadataReferences.Default,
                    new CSharpCompilationOptions(outputKind));
                using MemoryStream stream = new();
                EmitResult result = compilation.Emit(stream);
                Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
                string extension = outputKind == OutputKind.NetModule ? ".netmodule" : ".dll";
                string path = System.IO.Path.Combine(DirectoryPath, assemblyName + extension);
                byte[] image = stream.ToArray();
                File.WriteAllBytes(path, image);
                imagesByPath.Add(path, image);
                return MetadataReference.CreateFromImage(
                    ImmutableArray.Create(image),
                    properties ?? (outputKind == OutputKind.NetModule
                        ? MetadataReferenceProperties.Module
                        : MetadataReferenceProperties.Assembly),
                    filePath: path);
            }

            /// <summary>
            /// Creates a file-backed reference over an existing emitted image.
            /// </summary>
            public PortableExecutableReference CreateReferenceFromImage(
                string fileName,
                byte[] image)
            {
                string path = System.IO.Path.Combine(DirectoryPath, fileName);
                File.WriteAllBytes(path, image);
                imagesByPath.Add(path, image);
                return MetadataReference.CreateFromImage(
                    ImmutableArray.Create(image),
                    filePath: path);
            }

            /// <summary>
            /// Emits one original compilation and reconstructs it through all
            /// production P3/P4/P5 stages ending at P5K.
            /// </summary>
            public ReconstructionPair Reconstruct(
                string assemblyName,
                IReadOnlyList<SourceInput> sources,
                CSharpParseOptions? parseOptions = null,
                CSharpCompilationOptions? compilationOptions = null,
                IReadOnlyList<PortableExecutableReference>? additionalReferences = null,
                bool expectCompositionSuccess = true,
                bool createConflictingSupportingSource = false)
            {
                CSharpParseOptions actualParseOptions = parseOptions
                    ?? new CSharpParseOptions(
                        LanguageVersion.CSharp12,
                        DocumentationMode.Parse,
                        SourceCodeKind.Regular);
                CSharpCompilationOptions actualCompilationOptions = compilationOptions
                    ?? DefaultCompilationOptions();
                ImmutableArray<SyntaxTree> originalTrees = sources
                    .Select(source => CreateTree(source, actualParseOptions))
                    .ToImmutableArray();
                ImmutableArray<PortableExecutableReference> references =
                    MetadataReferences.Default
                        .Select(reference => Assert.IsAssignableFrom<PortableExecutableReference>(reference))
                        .Concat(additionalReferences ?? Array.Empty<PortableExecutableReference>())
                        .ToImmutableArray();
                CSharpCompilation original = CSharpCompilation.Create(
                    assemblyName,
                    originalTrees,
                    references,
                    actualCompilationOptions);
                AssertNoErrors(original);
                using MemoryStream peStream = new();
                using MemoryStream pdbStream = new();
                EmitResult emit = original.Emit(
                    peStream,
                    pdbStream,
                    options: new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: System.IO.Path.Combine(
                            DirectoryPath,
                            Guid.NewGuid().ToString("N") + ".pdb")));
                Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
                byte[] peImage = peStream.ToArray();

                PortableExecutableReference targetReference;

                if (references.Any(reference =>
                        reference.Properties.Kind == MetadataImageKind.Module))
                {
                    ImmutableArray<ModuleMetadata>.Builder modules =
                        ImmutableArray.CreateBuilder<ModuleMetadata>();
                    modules.Add(ModuleMetadata.CreateFromImage(peImage));

                    foreach (PortableExecutableReference moduleReference in references.Where(
                                 reference => reference.Properties.Kind == MetadataImageKind.Module))
                    {
                        modules.Add(ModuleMetadata.CreateFromImage(
                            imagesByPath[moduleReference.FilePath!]));
                    }

                    targetReference = AssemblyMetadata.Create(modules.ToArray()).GetReference(
                        filePath: System.IO.Path.Combine(DirectoryPath, assemblyName + ".dll"));
                }
                else
                {
                    targetReference = MetadataReference.CreateFromImage(
                        ImmutableArray.Create(peImage),
                        filePath: System.IO.Path.Combine(DirectoryPath, "renamed-target.bin"));
                }
                CSharpCompilation host = CSharpCompilation.Create(
                    "Fidelity.Descriptor.Host",
                    references: MetadataReferences.Default.Append(targetReference),
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                IAssemblySymbol targetSymbol = Assert.IsAssignableFrom<IAssemblySymbol>(
                    host.GetAssemblyOrModuleSymbol(targetReference));
                Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                    host,
                    targetSymbol,
                    out ExternalAssemblyReferenceDescriptor targetDescriptor));
                peStream.Position = 0;
                Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(
                    targetDescriptor,
                    peStream,
                    out ExternalPeDebugDirectoryDescriptor debugDescriptor));
                pdbStream.Position = 0;
                Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(
                    debugDescriptor,
                    pdbStream,
                    out ExternalCompilationProvenanceDescriptor provenance));
                Assert.True(ExternalCSharpCompilationConfigurationFactory.TryCreate(
                    provenance,
                    out ExternalCSharpCompilationConfiguration configuration));

                List<ExternalSourceDocumentDescriptor> documents = new(sources.Count);
                List<ExternalCSharpSyntaxTree> reconstructedSources = new(sources.Count);

                foreach (SourceInput source in sources)
                {
                    ExternalSourceDocumentDescriptor document = Assert.Single(
                        provenance.PortablePdb.Documents.Where(item => item.Name == source.Path));
                    using MemoryStream sourceStream = new(
                        Encoding.UTF8.GetBytes(source.Source),
                        writable: false);
                    Assert.True(ValidatedExternalSourceMaterialFactory.TryCreate(
                        document,
                        sourceStream,
                        out ValidatedExternalSourceMaterial material));
                    Assert.True(ExternalCSharpSyntaxTreeFactory.TryCreate(
                        material,
                        configuration,
                        out ExternalCSharpSyntaxTree reconstructed));
                    documents.Add(document);
                    reconstructedSources.Add(reconstructed);
                }

                Assert.True(ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                    configuration,
                    documents,
                    reconstructedSources,
                    out ExternalCSharpSyntaxTreeSet treeSet));
                ExternalCompilationMetadataReferencesDescriptor expectedReferences = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                string[] candidatePaths = expectedReferences.References
                    .Select(expected => references.First(reference => string.Equals(
                        System.IO.Path.GetFileName(reference.FilePath),
                        expected.Name,
                        StringComparison.Ordinal)).FilePath!)
                    .ToArray();
                bool materialSetCreated = ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    provenance,
                    candidatePaths,
                    out ExternalMetadataReferenceMaterialSet materialSet);
                Assert.True(
                    materialSetCreated,
                    string.Join(
                        Environment.NewLine,
                        expectedReferences.References
                            .Select((expected, index) =>
                                $"{index}: expected={expected.Name}; "
                                + $"candidate={candidatePaths[index]}; "
                                + $"aliases={string.Join(',', expected.Aliases)}")));
                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                bool compositionSucceeded = ExternalCSharpCompilationFactory.TryCreate(
                    targetDescriptor,
                    provenance,
                    configuration,
                    treeSet,
                    referenceSet,
                    out CSharpCompilation reconstructedCompilation);
                Assert.Equal(expectCompositionSuccess, compositionSucceeded);

                ExternalSupportingSourceCompilation? supportingSource = null;
                ExternalSupportingSourceCompilation? conflictingSupportingSource = null;

                if (compositionSucceeded)
                {
                    Assert.True(ExternalSupportingSourceCompilation.TryCreate(
                        targetDescriptor,
                        provenance,
                        configuration,
                        treeSet,
                        referenceSet,
                        reconstructedCompilation,
                        out supportingSource));

                    if (createConflictingSupportingSource)
                    {
                        Assert.True(ExternalCSharpCompilationFactory.TryCreate(
                            targetDescriptor,
                            provenance,
                            configuration,
                            treeSet,
                            referenceSet,
                            out CSharpCompilation conflictingCompilation));
                        Assert.NotSame(reconstructedCompilation, conflictingCompilation);
                        Assert.True(ExternalSupportingSourceCompilation.TryCreate(
                            targetDescriptor,
                            provenance,
                            configuration,
                            treeSet,
                            referenceSet,
                            conflictingCompilation,
                            out conflictingSupportingSource));
                    }
                }

                return new ReconstructionPair(
                    original,
                    compositionSucceeded ? reconstructedCompilation : null!,
                    configuration,
                    peImage,
                    supportingSource,
                    conflictingSupportingSource);
            }

            /// <summary>
            /// Deletes only this workspace's isolated candidate directory.
            /// </summary>
            public void Dispose()
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }

            /// <summary>
            /// Creates a checksum-stable syntax tree over exact UTF-8 bytes.
            /// </summary>
            private static SyntaxTree CreateTree(
                SourceInput source,
                CSharpParseOptions parseOptions)
            {
                byte[] image = Encoding.UTF8.GetBytes(source.Source);
                SourceText text = SourceText.From(
                    image,
                    image.Length,
                    Encoding.UTF8,
                    SourceHashAlgorithm.Sha256,
                    throwIfBinaryDetected: false,
                    canBeEmbedded: true);
                return CSharpSyntaxTree.ParseText(text, parseOptions, source.Path);
            }
        }
    }
}
