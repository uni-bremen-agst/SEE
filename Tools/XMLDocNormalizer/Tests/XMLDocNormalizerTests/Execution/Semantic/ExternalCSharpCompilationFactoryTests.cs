using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Verifies P5K composition and the local Roslyn 5.0 semantics on which
    /// its structural postconditions depend.
    /// </summary>
    public sealed class ExternalCSharpCompilationFactoryTests
    {
        /// <summary>
        /// Establishes that Roslyn retains the exact options, syntax-tree, and
        /// metadata-reference instances supplied to one creation call.
        /// </summary>
        [Fact]
        public void Roslyn50_Create_RetainsExactInputInstancesAndOrder()
        {
            CSharpCompilationOptions options = new(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Enable);
            SyntaxTree first = Tree("public sealed class First { }", "/_/First.cs");
            SyntaxTree second = Tree("public sealed class Second { }", "/_/Second.cs");
            PortableExecutableReference reference = Assert.IsAssignableFrom<PortableExecutableReference>(
                MetadataReferences.Default[0]);

            CSharpCompilation compilation = CSharpCompilation.Create(
                "Observed.Library",
                new[] { first, second },
                new[] { reference },
                options);

            Assert.Same(options, compilation.Options);
            Assert.Equal(new[] { first, second }, compilation.SyntaxTrees);
            Assert.Same(first, compilation.SyntaxTrees[0]);
            Assert.Same(second, compilation.SyntaxTrees[1]);
            Assert.Single(compilation.References);
            Assert.Same(reference, compilation.References.ElementAt(0));
            Assert.Equal("Observed.Library", compilation.AssemblyName);
            Assert.Equal("Observed.Library.dll", compilation.SourceModule.Name);
        }

        /// <summary>
        /// Establishes Roslyn's behavior for a duplicate syntax-tree instance.
        /// </summary>
        [Fact]
        public void Roslyn50_Create_RejectsDuplicateSyntaxTreeInstance()
        {
            SyntaxTree tree = Tree("public sealed class Duplicate { }", "/_/Duplicate.cs");

            Assert.Throws<ArgumentException>(
                () => CSharpCompilation.Create(
                    "DuplicateTrees",
                    new[] { tree, tree },
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary)));
        }

        /// <summary>
        /// Establishes Roslyn's behavior for a duplicate metadata-reference
        /// instance.
        /// </summary>
        [Fact]
        public void Roslyn50_Create_PreservesDuplicateReferenceInstance()
        {
            PortableExecutableReference reference = Assert.IsAssignableFrom<PortableExecutableReference>(
                MetadataReferences.Default[0]);

            CSharpCompilation compilation = CSharpCompilation.Create(
                "DuplicateReferences",
                references: new[] { reference, reference },
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

            Assert.Equal(2, compilation.References.Count());
            Assert.Same(reference, compilation.References.ElementAt(0));
            Assert.Same(reference, compilation.References.ElementAt(1));
        }

        /// <summary>
        /// Captures output-kind name and identity behavior for Roslyn 5.0.
        /// </summary>
        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary, "Observed", "Observed.dll", AssemblyContentType.Default)]
        [InlineData(OutputKind.ConsoleApplication, "Observed", "Observed.exe", AssemblyContentType.Default)]
        [InlineData(OutputKind.WindowsApplication, "Observed", "Observed.exe", AssemblyContentType.Default)]
        [InlineData(OutputKind.WindowsRuntimeMetadata, "Observed", "Observed.winmdobj", AssemblyContentType.Default)]
        [InlineData(OutputKind.WindowsRuntimeApplication, "Observed", "Observed.exe", AssemblyContentType.Default)]
        [InlineData(OutputKind.NetModule, "Observed", "Observed.netmodule", AssemblyContentType.Default)]
        [InlineData(OutputKind.NetModule, null, "?", AssemblyContentType.Default)]
        public void Roslyn50_OutputKinds_HaveObservedTargetSemantics(
            OutputKind outputKind,
            string? assemblyName,
            string expectedModuleName,
            AssemblyContentType expectedContentType)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                options: new CSharpCompilationOptions(outputKind));

            Assert.Equal(assemblyName, compilation.AssemblyName);
            Assert.Equal(expectedModuleName, compilation.SourceModule.Name);
            Assert.Equal(assemblyName ?? "?", compilation.Assembly.Identity.Name);
            Assert.Equal(expectedContentType, compilation.Assembly.Identity.ContentType);
        }

        /// <summary>
        /// Establishes the emitted metadata kind for every P5G-supported
        /// output kind, including Windows Runtime and netmodule outputs.
        /// </summary>
        /// <param name="outputKind">The output kind under study.</param>
        /// <param name="isAssembly">Whether emitted metadata has an Assembly row.</param>
        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary, true)]
        [InlineData(OutputKind.ConsoleApplication, true)]
        [InlineData(OutputKind.WindowsApplication, true)]
        [InlineData(OutputKind.WindowsRuntimeMetadata, true)]
        [InlineData(OutputKind.WindowsRuntimeApplication, true)]
        [InlineData(OutputKind.NetModule, false)]
        public void Roslyn50_OutputKinds_EmitExpectedMetadataKind(
            OutputKind outputKind,
            bool isAssembly)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                "Observed",
                new[]
                {
                    Tree(
                        "public static class Program { public static void Main() { } }",
                        "/_/Program.cs"),
                },
                MetadataReferences.Default,
                new CSharpCompilationOptions(outputKind));
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            stream.Position = 0;
            using PEReader peReader = new(stream);
            MetadataReader reader = peReader.GetMetadataReader();

            Assert.Equal(isAssembly, reader.IsAssembly);
            Assert.Equal(
                compilation.SourceModule.Name,
                reader.GetString(reader.GetModuleDefinition().Name));

            if (isAssembly)
            {
                Assert.True(ExternalPeMetadataIdentityReader.TryReadAssemblyIdentity(
                    reader,
                    out AssemblyIdentity emittedIdentity));
                Assert.Equal(compilation.Assembly.Identity, emittedIdentity);
            }
        }

        /// <summary>
        /// Establishes that a null netmodule creation name produces the
        /// placeholder module name and remains emit-capable in Roslyn 5.0.
        /// </summary>
        [Fact]
        public void Roslyn50_NetModuleWithNullName_EmitsPlaceholderModule()
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                null,
                new[] { Tree("namespace Empty { }", "/_/Empty.cs") },
                options: new CSharpCompilationOptions(OutputKind.NetModule));
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream);

            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            Assert.Null(compilation.AssemblyName);
            Assert.Equal("?", compilation.SourceModule.Name);
            stream.Position = 0;
            using PEReader peReader = new(stream);
            Assert.False(peReader.GetMetadataReader().IsAssembly);
        }

        /// <summary>
        /// Composes one assembly directly from the exact P5G, P5J, and P5E
        /// instances and preserves their order.
        /// </summary>
        [Fact]
        public void ValidAssembly_ComposesExactInputsAndFullIdentity()
        {
            CompositionFixture fixture = CreateFixture(
                sources:
                [
                    "public sealed class First { }",
                    "public sealed class Second { }",
                ],
                references: CreateInMemoryDefaultReferences(2));

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Same(fixture.Configuration.CompilationOptions, compilation.Options);
            Assert.Equal(fixture.TargetAssembly.AssemblyIdentity, compilation.Assembly.Identity);
            Assert.Equal(fixture.Trees.Trees, compilation.SyntaxTrees);
            Assert.Equal(fixture.References.References, compilation.References);

            for (int index = 0; index < fixture.Trees.Trees.Length; index++)
            {
                Assert.Same(fixture.Trees.Trees[index], compilation.SyntaxTrees[index]);
            }

            for (int index = 0; index < fixture.References.References.Length; index++)
            {
                Assert.Same(
                    fixture.References.References[index],
                    compilation.References.ElementAt(index));
            }
        }

        /// <summary>
        /// Uses the validated binary identity rather than source, PE, or PDB
        /// path-like provenance to name the target.
        /// </summary>
        [Fact]
        public void ValidatedAssemblyIdentity_DeterminesNameAndVersionIndependentlyOfPaths()
        {
            CompositionFixture fixture = CreateFixture(
                assemblyName: "Actual.Target.Identity",
                sources:
                [
                    "using System.Reflection; "
                    + "[assembly: AssemblyVersion(\"1.2.3.4\")] "
                    + "public sealed class CompletelyDifferentSourceName { }",
                ],
                references: CreateInMemoryDefaultReferences());

            Assert.Equal("renamed-candidate.unrelated", fixture.TargetAssembly.FilePath);
            Assert.Equal("Actual.Target.Identity", fixture.TargetAssembly.AssemblyIdentity.Name);
            Assert.Equal(new Version(1, 2, 3, 4), fixture.TargetAssembly.AssemblyIdentity.Version);
            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Equal("Actual.Target.Identity", compilation.AssemblyName);
            Assert.Equal("Actual.Target.Identity.dll", compilation.SourceModule.Name);
            Assert.Equal(new Version(1, 2, 3, 4), compilation.Assembly.Identity.Version);
        }

        /// <summary>
        /// Covers every assembly-producing output kind reconstructed by P5G.
        /// </summary>
        [Theory]
        [InlineData(OutputKind.DynamicallyLinkedLibrary)]
        [InlineData(OutputKind.ConsoleApplication)]
        [InlineData(OutputKind.WindowsApplication)]
        [InlineData(OutputKind.WindowsRuntimeMetadata)]
        [InlineData(OutputKind.WindowsRuntimeApplication)]
        public void AssemblyProducingOutputKind_IsSupported(OutputKind outputKind)
        {
            CompositionFixture fixture = CreateFixture(outputKind: outputKind);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Equal(outputKind, compilation.Options.OutputKind);
            Assert.Equal(fixture.TargetAssembly.AssemblyIdentity, compilation.Assembly.Identity);
        }

        /// <summary>
        /// Rejects a target netmodule because P3 supplies assembly provenance
        /// and P5G does not preserve the original module-name option.
        /// </summary>
        [Fact]
        public void NetModuleTarget_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture(outputKind: OutputKind.NetModule);

            Assert.False(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Null(compilation);
        }

        /// <summary>
        /// Rejects target provenance without a manifest module.
        /// </summary>
        [Fact]
        public void TargetWithoutManifestModule_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture();
            ExternalAssemblyReferenceDescriptor target = new(
                fixture.TargetAssembly.AssemblyIdentity,
                ImmutableArray<ExternalModuleIdentity>.Empty,
                fixture.TargetAssembly.FilePath,
                fixture.TargetAssembly.IsReferenceAssembly);

            Assert.False(TryCreate(fixture with { TargetAssembly = target }, out _));
        }

        /// <summary>
        /// Prevents mixing P3 target identity with P4A/P5A provenance from a
        /// different binary.
        /// </summary>
        [Fact]
        public void DifferentTargetManifestMvid_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture();
            ExternalModuleIdentity differentModule = new(
                fixture.TargetAssembly.Modules[0].Name,
                Guid.NewGuid());
            ExternalPeDebugDirectoryDescriptor debug = CreateDebugDirectory(differentModule);

            Assert.False(TryCreate(
                fixture with
                {
                    Provenance = CreateProvenance(
                        debug,
                        fixture.Configuration,
                        fixture.References.References),
                },
                out _));
        }

        /// <summary>
        /// Rejects a reconstructed tree set whose size no longer matches P5G.
        /// </summary>
        [Fact]
        public void SourceFileCountMismatch_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture();
            ExternalCSharpCompilationConfiguration configuration =
                CreateConfiguration(
                    fixture.Configuration.ParseOptions,
                    fixture.Configuration.CompilationOptions,
                    sourceFileCount: fixture.Trees.Trees.Length + 1);

            Assert.False(TryCreate(fixture with { Configuration = configuration }, out _));
        }

        /// <summary>
        /// Rejects trees from a different P5G parse-options instance even
        /// when the options are semantically equal.
        /// </summary>
        [Fact]
        public void DifferentParseOptionsInstance_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture();
            CSharpParseOptions equivalentOptions = new(
                fixture.Configuration.ParseOptions.LanguageVersion,
                fixture.Configuration.ParseOptions.DocumentationMode,
                fixture.Configuration.ParseOptions.Kind,
                fixture.Configuration.ParseOptions.PreprocessorSymbolNames);
            SyntaxTree foreignTree = Tree(
                "public sealed class Foreign { }",
                "/_/Foreign.cs",
                equivalentOptions);
            ExternalCSharpSyntaxTreeSet trees = new([foreignTree]);

            Assert.False(TryCreate(fixture with { Trees = trees }, out _));
        }

        /// <summary>
        /// Rejects a null tree before Roslyn composition.
        /// </summary>
        [Fact]
        public void NullTree_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture();
            ExternalCSharpSyntaxTreeSet trees = new([null!]);

            Assert.False(TryCreate(fixture with { Trees = trees }, out _));
        }

        /// <summary>
        /// Converts Roslyn's duplicate-tree rejection into a false result and
        /// never deduplicates the caller's set.
        /// </summary>
        [Fact]
        public void DuplicateTreeInstance_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture(
                sources: ["public sealed class Duplicate { }"]);
            SyntaxTree tree = fixture.Trees.Trees[0];
            ExternalCSharpSyntaxTreeSet trees = new([tree, tree]);
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                fixture.Configuration.ParseOptions,
                fixture.Configuration.CompilationOptions,
                sourceFileCount: 2);

            Assert.False(TryCreate(
                fixture with { Trees = trees, Configuration = configuration },
                out _));
        }

        /// <summary>
        /// Allows an empty P5J source scope; diagnostics are not a P5K gate.
        /// </summary>
        [Fact]
        public void EmptySourceSet_Composes()
        {
            CompositionFixture fixture = CreateFixture(sources: []);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Empty(compilation.SyntaxTrees);
        }

        /// <summary>
        /// Allows empty P5A/P5E reference provenance.
        /// </summary>
        [Fact]
        public void EmptyReferenceSet_Composes()
        {
            CompositionFixture fixture = CreateFixture(references: []);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Empty(compilation.References);
        }

        /// <summary>
        /// Does not inspect diagnostics as part of composition success.
        /// </summary>
        [Fact]
        public void CompilationErrors_DoNotFailComposition()
        {
            CompositionFixture fixture = CreateFixture(
                sources: ["public sealed class Broken { MissingType Value; }"]);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Contains(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Rejects absent P5A reference provenance even for an empty P5E set.
        /// </summary>
        [Fact]
        public void MissingReferenceProvenance_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture(references: []);
            ExternalCompilationProvenanceDescriptor provenance = new(
                fixture.Provenance.DebugDirectory,
                fixture.Provenance.PortablePdb,
                fixture.Provenance.CompilationOptions,
                metadataReferences: null);

            Assert.False(TryCreate(fixture with { Provenance = provenance }, out _));
        }

        /// <summary>
        /// Rejects P5A/P5E reference-count disagreement.
        /// </summary>
        [Fact]
        public void ReferenceCountMismatch_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture(
                references: CreateInMemoryDefaultReferences(1));
            ExternalMetadataReferenceSet references = new(
                ImmutableArray<PortableExecutableReference>.Empty);

            Assert.False(TryCreate(fixture with { References = references }, out _));
        }

        /// <summary>
        /// Fails closed for malformed synthetic provenance containing a null
        /// reference descriptor.
        /// </summary>
        [Fact]
        public void NullExpectedReferenceDescriptor_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture(
                references: CreateInMemoryDefaultReferences(1));
            ExternalCompilationProvenanceDescriptor provenance = new(
                fixture.Provenance.DebugDirectory,
                fixture.Provenance.PortablePdb,
                fixture.Provenance.CompilationOptions,
                new ExternalCompilationMetadataReferencesDescriptor([null!]));

            Assert.False(TryCreate(fixture with { Provenance = provenance }, out _));
        }

        /// <summary>
        /// Rejects references whose metadata kind, aliases, or embed-interop
        /// flag no longer matches its P5A ordinal.
        /// </summary>
        [Fact]
        public void ReferencePropertiesMismatch_FailsClosed()
        {
            PortableExecutableReference original = CreateInMemoryDefaultReferences(1)[0];
            PortableExecutableReference changed = original.WithProperties(
                original.Properties.WithAliases(["changed"]));
            CompositionFixture fixture = CreateFixture(references: [original]);

            Assert.False(TryCreate(
                fixture with { References = new ExternalMetadataReferenceSet([changed]) },
                out _));
        }

        /// <summary>
        /// Rejects a same-shaped reference whose actual manifest MVID differs
        /// from P5A provenance.
        /// </summary>
        [Fact]
        public void ReferenceMvidMismatch_FailsClosed()
        {
            PortableExecutableReference first = CreateDependencyReference(
                "First.Dependency",
                "public sealed class FirstDependency { }");
            PortableExecutableReference second = CreateDependencyReference(
                "Second.Dependency",
                "public sealed class SecondDependency { }");
            CompositionFixture fixture = CreateFixture(references: [first]);

            Assert.False(TryCreate(
                fixture with { References = new ExternalMetadataReferenceSet([second]) },
                out _));
        }

        /// <summary>
        /// Preserves two ordinals containing the exact same reference object.
        /// </summary>
        [Fact]
        public void DuplicateReferenceInstance_IsNotDeduplicated()
        {
            PortableExecutableReference reference = CreateInMemoryDefaultReferences(1)[0];
            CompositionFixture fixture = CreateFixture(references: [reference, reference]);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Equal(2, compilation.References.Count());
            Assert.Same(reference, compilation.References.ElementAt(0));
            Assert.Same(reference, compilation.References.ElementAt(1));
        }

        /// <summary>
        /// Preserves two distinct references to one binary with independent
        /// extern aliases and verifies semantic binding through both aliases.
        /// </summary>
        [Fact]
        public void SameBinaryDifferentAliases_PreservesBothAndBindsSemantically()
        {
            ImmutableArray<byte> image = EmitImage(
                "Aliased.Dependency",
                "namespace Dependency { public sealed class Shared { } }");
            PortableExecutableReference left = MetadataReference.CreateFromImage(
                image,
                MetadataReferenceProperties.Assembly.WithAliases(["left"]));
            PortableExecutableReference right = MetadataReference.CreateFromImage(
                image,
                MetadataReferenceProperties.Assembly.WithAliases(["right"]));
            ImmutableArray<PortableExecutableReference> references =
                CreateInMemoryDefaultReferences().Add(left).Add(right);
            CompositionFixture fixture = CreateFixture(
                sources:
                [
                    "extern alias left; extern alias right; "
                    + "public sealed class AliasConsumer { "
                    + "public left::Dependency.Shared Left() => new(); "
                    + "public right::Dependency.Shared Right() => new(); }",
                ],
                references: references);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Same(left, compilation.References.ElementAt(references.Length - 2));
            Assert.Same(right, compilation.References.ElementAt(references.Length - 1));
            Assert.Equal(
                2,
                compilation.SyntaxTrees[0].GetRoot()
                    .DescendantNodes()
                    .OfType<AliasQualifiedNameSyntax>()
                    .Select(node => node.Alias.Identifier.ValueText)
                    .Distinct(StringComparer.Ordinal)
                    .Count());
        }

        /// <summary>
        /// Uses the validated target name for friend-assembly access.
        /// </summary>
        [Fact]
        public void InternalsVisibleTo_BindsForValidatedTargetName()
        {
            PortableExecutableReference friend = CreateDependencyReference(
                "Friend.Dependency",
                "using System.Runtime.CompilerServices; "
                + "[assembly: InternalsVisibleTo(\"Actual.Target\")] "
                + "internal static class InternalType { public static int Value => 42; }");
            CompositionFixture fixture = CreateFixture(
                assemblyName: "Actual.Target",
                sources:
                [
                    "public sealed class FriendConsumer { "
                    + "public int Read() => InternalType.Value; }",
                ],
                references: CreateInMemoryDefaultReferences().Add(friend));

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            MemberAccessExpressionSyntax access = compilation.SyntaxTrees[0]
                .GetRoot()
                .DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Single();
            Assert.NotNull(compilation.GetSemanticModel(compilation.SyntaxTrees[0])
                .GetSymbolInfo(access).Symbol);
        }

        /// <summary>
        /// Keeps a P5E netmodule reference on a normal assembly target and
        /// permits Roslyn to bind its type.
        /// </summary>
        [Fact]
        public void NetModuleReference_IsPreservedAndBindable()
        {
            ImmutableArray<byte> image = EmitImage(
                "Referenced.Module",
                "public sealed class ModuleType { public static int Value => 7; }",
                OutputKind.NetModule);
            PortableExecutableReference module = MetadataReference.CreateFromImage(
                image,
                MetadataReferenceProperties.Module);
            ImmutableArray<PortableExecutableReference> references =
                CreateInMemoryDefaultReferences().Add(module);
            CompositionFixture fixture = CreateFixture(
                sources:
                [
                    "public sealed class ModuleConsumer { "
                    + "public int Read() => ModuleType.Value; }",
                ],
                references: references);

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Same(module, compilation.References.Last());
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Fails rather than inventing signing options omitted by P5G.
        /// </summary>
        [Fact]
        public void SignedTargetWithoutSigningOptions_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture(
                references: CreateInMemoryDefaultReferences());
            byte[] publicKey = typeof(object).Assembly.GetName().GetPublicKey()!;
            CSharpCompilation signed = CSharpCompilation.Create(
                fixture.TargetAssembly.AssemblyIdentity.Name,
                fixture.Trees.Trees,
                fixture.References.References,
                fixture.Configuration.CompilationOptions
                    .WithCryptoPublicKey(publicKey.ToImmutableArray())
                    .WithPublicSign(true));
            Assert.True(signed.Assembly.Identity.HasPublicKey);
            ExternalAssemblyReferenceDescriptor target = new(
                signed.Assembly.Identity,
                fixture.TargetAssembly.Modules,
                fixture.TargetAssembly.FilePath,
                isReferenceAssembly: false);

            Assert.False(TryCreate(fixture with { TargetAssembly = target }, out _));
        }

        /// <summary>
        /// Reconstructs the exact signed semantic identity from the complete
        /// public key without configuring a private key, provider, or emit mode.
        /// </summary>
        [Fact]
        public void FullySignedTargetWithExactPublicKey_ReconstructsSemanticIdentity()
        {
            CompositionFixture fixture = CreateFullySignedFixture(CreateFixture(
                sources:
                [
                    "public static class SignedHelper { "
                    + "public static T Echo<T>(T value) => value; }",
                    "public sealed class SignedConsumer { "
                    + "public string Read(string value) => SignedHelper.Echo(value); }",
                ],
                references: CreateInMemoryDefaultReferences()));

            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Equal(fixture.TargetAssembly.AssemblyIdentity, compilation.Assembly.Identity);
            Assert.Equal(
                fixture.TargetAssembly.AssemblyIdentity.PublicKey,
                compilation.Assembly.Identity.PublicKey);
            Assert.Equal(
                fixture.TargetAssembly.AssemblyIdentity.PublicKeyToken,
                compilation.Assembly.Identity.PublicKeyToken);
            Assert.Equal(
                fixture.Configuration.SigningProvenance.PublicKey,
                compilation.Options.CryptoPublicKey);
            Assert.Null(compilation.Options.CryptoKeyFile);
            Assert.Null(compilation.Options.CryptoKeyContainer);
            Assert.Null(compilation.Options.DelaySign);
            Assert.False(compilation.Options.PublicSign);
            Assert.Null(compilation.Options.StrongNameProvider);

            InvocationExpressionSyntax invocation = compilation.SyntaxTrees[1]
                .GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Single();
            IMethodSymbol symbol = Assert.IsAssignableFrom<IMethodSymbol>(
                compilation.GetSemanticModel(compilation.SyntaxTrees[1])
                    .GetSymbolInfo(invocation).Symbol);
            Assert.Equal("Echo", symbol.Name);
            Assert.Equal(SpecialType.System_String, symbol.ReturnType.SpecialType);
            Assert.Same(
                compilation.SyntaxTrees[0],
                Assert.Single(symbol.OriginalDefinition.DeclaringSyntaxReferences).SyntaxTree);
        }

        /// <summary>
        /// Rejects a different full public key even when name, version,
        /// culture, trees, references, and all nonsigning options agree.
        /// </summary>
        [Fact]
        public void FullySignedTargetWithWrongPublicKey_FailsClosed()
        {
            CompositionFixture unsigned = CreateFixture(
                references: CreateInMemoryDefaultReferences());
            CompositionFixture expected = CreateFullySignedFixture(
                unsigned,
                GetSigningPublicKey());
            CompositionFixture wrong = CreateFullySignedFixture(
                unsigned,
                GetDifferentSigningPublicKey());

            Assert.NotEqual(
                expected.TargetAssembly.AssemblyIdentity.PublicKeyToken,
                wrong.TargetAssembly.AssemblyIdentity.PublicKeyToken);
            Assert.False(TryCreate(
                wrong with { TargetAssembly = expected.TargetAssembly },
                out _));
        }

        /// <summary>
        /// Rejects a signed target when P5G omitted its required complete key.
        /// </summary>
        [Fact]
        public void FullySignedTargetWithMissingPublicKey_FailsClosed()
        {
            CompositionFixture unsigned = CreateFixture(
                references: CreateInMemoryDefaultReferences());
            CompositionFixture signed = CreateFullySignedFixture(unsigned);
            ExternalAssemblySigningProvenance signing =
                signed.Configuration.SigningProvenance;
            ExternalCSharpCompilationConfiguration missingKey = CreateConfiguration(
                unsigned.Configuration.ParseOptions,
                unsigned.Configuration.CompilationOptions,
                unsigned.Configuration.SourceFileCount,
                signing);
            ExternalCompilationProvenanceDescriptor provenance = CreateProvenance(
                CreateDebugDirectory(signed.TargetAssembly.Modules[0], signing),
                missingKey,
                signed.References.References);

            Assert.False(TryCreate(
                signed with { Configuration = missingKey, Provenance = provenance },
                out _));
        }

        /// <summary>
        /// Applies the P5L composition postcondition to an already created
        /// compilation and rejects a different signed assembly identity.
        /// </summary>
        [Fact]
        public void P5L_DifferentSignedCompilationIdentity_FailsClosed()
        {
            CompositionFixture fixture = CreateFullySignedFixture(CreateFixture(
                references: CreateInMemoryDefaultReferences()));
            CSharpCompilation wrong = CSharpCompilation.Create(
                fixture.TargetAssembly.AssemblyIdentity.Name,
                fixture.Trees.Trees,
                fixture.References.References,
                fixture.Configuration.CompilationOptions.WithCryptoPublicKey(
                    GetDifferentSigningPublicKey()));

            Assert.NotEqual(
                fixture.TargetAssembly.AssemblyIdentity,
                wrong.Assembly.Identity);
            Assert.False(ExternalCSharpCompilationFactory.IsValidComposition(
                fixture.TargetAssembly,
                fixture.Provenance,
                fixture.Configuration,
                fixture.Trees,
                fixture.References,
                wrong));
        }

        /// <summary>
        /// Demonstrates that the full signed friend identity affects binding:
        /// exact identity grants internal access while missing or wrong keys do not.
        /// Public generic binding remains available in all three compilations.
        /// </summary>
        [Fact]
        public void InternalsVisibleTo_RequiresExactSignedFriendIdentity()
        {
            ImmutableArray<byte> publicKey = GetSigningPublicKey();
            string publicKeyHex = Convert.ToHexString(publicKey.AsSpan());
            CSharpCompilation dependency = CSharpCompilation.Create(
                "Friend.Dependency",
                [Tree(
                    "using System.Runtime.CompilerServices; "
                    + $"[assembly: InternalsVisibleTo(\"Friend.Target, PublicKey={publicKeyHex}\")] "
                    + "namespace FriendDependency { "
                    + "internal static class Hidden { internal static int Read() => 7; } "
                    + "public static class PublicApi { "
                    + "public static T Echo<T>(T value) => value; } }",
                    "/_/Dependency.cs")],
                MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.DoesNotContain(
                dependency.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            MetadataReference dependencyReference = dependency.ToMetadataReference();
            ImmutableArray<MetadataReference> references =
                MetadataReferences.Default.Append(dependencyReference).ToImmutableArray();
            SyntaxTree source = Tree(
                "public static class FriendConsumer { "
                + "public static int Internal() => FriendDependency.Hidden.Read(); "
                + "public static string Public(string value) "
                + "=> FriendDependency.PublicApi.Echo(value); }",
                "/_/FriendConsumer.cs");

            CSharpCompilation exact = CreateFriendCompilation(
                source,
                references,
                publicKey);
            CSharpCompilation missing = CreateFriendCompilation(
                source,
                references,
                ImmutableArray<byte>.Empty);
            CSharpCompilation wrong = CreateFriendCompilation(
                source,
                references,
                GetDifferentSigningPublicKey());

            Assert.DoesNotContain(
                exact.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Contains(
                missing.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Contains(
                wrong.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            AssertInvocationBindings(exact, expectInternalBinding: true);
            AssertInvocationBindings(missing, expectInternalBinding: false);
            AssertInvocationBindings(wrong, expectInternalBinding: false);
        }

        /// <summary>
        /// Carries one exact signed P5K result through P6A registration and
        /// P6B cross-compilation method resolution without emitting a binary.
        /// </summary>
        [Fact]
        public void FullySignedTarget_P6ARegistrationAndP6BResolutionSucceed()
        {
            CompositionFixture fixture = CreateFullySignedFixture(CreateFixture(
                assemblyName: "Signed.Navigation",
                sources:
                [
                    "namespace SignedNavigation { public static class Api { "
                    + "public static void Run() { } } }",
                ],
                references: CreateInMemoryDefaultReferences()));
            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.True(ExternalSupportingSourceCompilation.TryCreate(
                fixture.TargetAssembly,
                fixture.Provenance,
                fixture.Configuration,
                fixture.Trees,
                fixture.References,
                compilation,
                out ExternalSupportingSourceCompilation supportingSource));
            SupportingSourceCatalog catalog = new();
            Assert.True(catalog.TryRegisterExternal(
                supportingSource,
                out SemanticCompilationScope scope));

            CSharpCompilation consumer = CSharpCompilation.Create(
                "Signed.Navigation.Consumer",
                references: MetadataReferences.Default.Append(
                    compilation.ToMetadataReference()),
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));
            IAssemblySymbol metadataAssembly = Assert.IsAssignableFrom<IAssemblySymbol>(
                consumer.GetAssemblyOrModuleSymbol(consumer.References.Last()));
            INamedTypeSymbol metadataType = Assert.IsAssignableFrom<INamedTypeSymbol>(
                metadataAssembly.GetTypeByMetadataName("SignedNavigation.Api"));
            IMethodSymbol metadataMethod = Assert.Single(
                metadataType.GetMembers("Run").OfType<IMethodSymbol>());

            IMethodSymbol sourceMethod = Assert.IsAssignableFrom<IMethodSymbol>(
                CrossCompilationSymbolResolver.ResolveMethod(
                    metadataMethod,
                    scope.Compilation));
            SyntaxReference declaration = Assert.Single(
                sourceMethod.DeclaringSyntaxReferences);
            Assert.Same(compilation.SyntaxTrees.Single(), declaration.SyntaxTree);
            Assert.Equal(
                fixture.TargetAssembly.AssemblyIdentity,
                sourceMethod.ContainingAssembly.Identity);
        }

        /// <summary>
        /// Preserves a culture carried by source-level assembly identity and
        /// validates it as part of the complete identity postcondition.
        /// </summary>
        [Fact]
        public void AssemblyCulture_IsPartOfFullIdentityValidation()
        {
            CompositionFixture fixture = CreateFixture(
                sources:
                [
                    "using System.Reflection; "
                    + "[assembly: AssemblyCulture(\"fr-FR\")] "
                    + "public sealed class CultureCarrier { }",
                ],
                references: CreateInMemoryDefaultReferences());

            Assert.Equal("fr-FR", fixture.TargetAssembly.AssemblyIdentity.CultureName);
            Assert.True(TryCreate(fixture, out CSharpCompilation compilation));
            Assert.Equal("fr-FR", compilation.Assembly.Identity.CultureName);
        }

        /// <summary>
        /// Rejects any full assembly-identity disagreement, including an
        /// otherwise same-named target with a different version.
        /// </summary>
        [Fact]
        public void DifferentAssemblyVersion_FailsClosed()
        {
            ImmutableArray<PortableExecutableReference> references =
                CreateInMemoryDefaultReferences();
            CompositionFixture fixture = CreateFixture(
                assemblyName: "Versioned.Target",
                sources:
                [
                    "using System.Reflection; "
                    + "[assembly: AssemblyVersion(\"1.2.3.4\")] "
                    + "public sealed class VersionOne { }",
                ],
                references: references);
            CompositionFixture other = CreateFixture(
                assemblyName: "Versioned.Target",
                sources:
                [
                    "using System.Reflection; "
                    + "[assembly: AssemblyVersion(\"9.8.7.6\")] "
                    + "public sealed class VersionTwo { }",
                ],
                references: references);
            ExternalAssemblyReferenceDescriptor mismatchedTarget = new(
                other.TargetAssembly.AssemblyIdentity,
                fixture.TargetAssembly.Modules,
                fixture.TargetAssembly.FilePath,
                isReferenceAssembly: false);

            Assert.False(TryCreate(
                fixture with { TargetAssembly = mismatchedTarget },
                out _));
        }

        /// <summary>
        /// Rejects a target module name that the exact P5G options cannot
        /// produce instead of applying WithModuleName.
        /// </summary>
        [Fact]
        public void TargetModuleNameMismatch_FailsClosed()
        {
            CompositionFixture fixture = CreateFixture();
            ExternalModuleIdentity module = new(
                "different-output-name.dll",
                fixture.TargetAssembly.Modules[0].ModuleVersionId);
            ExternalAssemblyReferenceDescriptor target = new(
                fixture.TargetAssembly.AssemblyIdentity,
                [module],
                fixture.TargetAssembly.FilePath,
                isReferenceAssembly: false);
            ExternalCompilationProvenanceDescriptor provenance = CreateProvenance(
                CreateDebugDirectory(module),
                fixture.Configuration,
                fixture.References.References);

            Assert.False(TryCreate(
                fixture with { TargetAssembly = target, Provenance = provenance },
                out _));
        }

        /// <summary>
        /// Validates all public null contracts.
        /// </summary>
        [Fact]
        public void NullInputs_ThrowArgumentNullException()
        {
            CompositionFixture fixture = CreateFixture();

            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpCompilationFactory.TryCreate(
                    null!, fixture.Provenance, fixture.Configuration,
                    fixture.Trees, fixture.References, out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpCompilationFactory.TryCreate(
                    fixture.TargetAssembly, null!, fixture.Configuration,
                    fixture.Trees, fixture.References, out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpCompilationFactory.TryCreate(
                    fixture.TargetAssembly, fixture.Provenance, null!,
                    fixture.Trees, fixture.References, out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpCompilationFactory.TryCreate(
                    fixture.TargetAssembly, fixture.Provenance, fixture.Configuration,
                    null!, fixture.References, out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalCSharpCompilationFactory.TryCreate(
                    fixture.TargetAssembly, fixture.Provenance, fixture.Configuration,
                    fixture.Trees, null!, out _));
        }

        /// <summary>
        /// Runs controlled binaries and three sources through the complete
        /// P3/P4A/P4B/P5A-to-P5K production pipeline and verifies semantic
        /// binding on the composed compilation.
        /// </summary>
        [Fact]
        public void CompletePipeline_MultipleSourcesDependenciesAndOptionsBindSemantically()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                string dependencyAPath = Path.Combine(directory, "DependencyA.dll");
                string dependencyBPath = Path.Combine(directory, "DependencyB.dll");
                File.WriteAllBytes(
                    dependencyAPath,
                    EmitImage(
                        "DependencyA",
                        "namespace DependencyA { public sealed class TypeA { } }").ToArray());
                File.WriteAllBytes(
                    dependencyBPath,
                    EmitImage(
                        "DependencyB",
                        "namespace DependencyB { public static class Helper { "
                        + "public static int GetValue() => 42; } }").ToArray());

                List<PortableExecutableReference> originalReferences =
                    MetadataReferences.Default
                        .Select(reference => Assert.IsAssignableFrom<PortableExecutableReference>(reference))
                        .ToList();
                originalReferences.Add(MetadataReference.CreateFromFile(
                    dependencyAPath,
                    MetadataReferenceProperties.Assembly.WithAliases(["depAlias"])));
                originalReferences.Add(MetadataReference.CreateFromFile(dependencyBPath));

                CSharpParseOptions parseOptions = new(
                    LanguageVersion.CSharp12,
                    DocumentationMode.Parse,
                    SourceCodeKind.Regular,
                    ["FEATURE"]);
                CSharpCompilationOptions compilationOptions = new(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    checkOverflow: true,
                    allowUnsafe: true,
                    platform: Platform.X64,
                    nullableContextOptions: NullableContextOptions.Enable,
                    deterministic: true);
                (string Path, string Source)[] sourceInputs =
                [
                    (
                        "/_/Identity.cs",
                        "using System.Reflection; "
                        + "[assembly: AssemblyVersion(\"1.2.3.4\")]\n"
                        + "#if FEATURE\npublic sealed class FeatureBranch { }\n"
                        + "#else\npublic sealed class FallbackBranch { }\n#endif"),
                    (
                        "/_/AliasConsumer.cs",
                        "extern alias depAlias; public sealed class AliasConsumer { "
                        + "public depAlias::DependencyA.TypeA Create() => new(); }"),
                    (
                        "/_/CallConsumer.cs",
                        "public sealed class CallConsumer { public string? Text { get; set; } "
                        + "public int Read() => DependencyB.Helper.GetValue(); }"),
                ];
                ImmutableArray<SyntaxTree> originalTrees = sourceInputs
                    .Select(source =>
                    {
                        byte[] image = Encoding.UTF8.GetBytes(source.Source);
                        SourceText text = SourceText.From(
                            image,
                            image.Length,
                            Encoding.UTF8,
                            SourceHashAlgorithm.Sha256,
                            throwIfBinaryDetected: false,
                            canBeEmbedded: true);
                        return CSharpSyntaxTree.ParseText(
                            text,
                            parseOptions,
                            source.Path);
                    })
                    .ToImmutableArray();
                CSharpCompilation original = CSharpCompilation.Create(
                    "Actual.Pipeline.Target",
                    originalTrees,
                    originalReferences,
                    compilationOptions);
                Assert.DoesNotContain(
                    original.GetDiagnostics(),
                    diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
                using MemoryStream peStream = new();
                using MemoryStream pdbStream = new();
                EmitResult emitResult = original.Emit(
                    peStream,
                    pdbStream,
                    options: new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: Path.Combine(directory, "UnrelatedDebugName.pdb")));
                Assert.True(
                    emitResult.Success,
                    string.Join(Environment.NewLine, emitResult.Diagnostics));
                byte[] peImage = peStream.ToArray();
                byte[] pdbImage = pdbStream.ToArray();

                PortableExecutableReference targetReference = MetadataReference.CreateFromImage(
                    ImmutableArray.Create(peImage),
                    filePath: Path.Combine(directory, "RenamedTargetCandidate.bin"));
                CSharpCompilation host = CSharpCompilation.Create(
                    "Descriptor.Host",
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

                List<ExternalSourceDocumentDescriptor> documents = new(sourceInputs.Length);
                List<ExternalCSharpSyntaxTree> reconstructedSources = new(sourceInputs.Length);

                foreach ((string path, string source) in sourceInputs)
                {
                    ExternalSourceDocumentDescriptor document = Assert.Single(
                        provenance.PortablePdb.Documents.Where(item => item.Name == path));
                    using MemoryStream sourceStream = new(
                        Encoding.UTF8.GetBytes(source),
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

                Dictionary<string, string> candidatePaths = new(StringComparer.Ordinal);

                foreach (PortableExecutableReference reference in originalReferences)
                {
                    candidatePaths.Add(Path.GetFileName(reference.FilePath!), reference.FilePath!);
                }

                ExternalCompilationMetadataReferencesDescriptor expectedReferences = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                string[] alignedCandidatePaths = expectedReferences.References
                    .Select(reference => candidatePaths[reference.Name])
                    .ToArray();
                Assert.True(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    provenance,
                    alignedCandidatePaths,
                    out ExternalMetadataReferenceMaterialSet materialSet));
                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));

                Assert.True(ExternalCSharpCompilationFactory.TryCreate(
                    targetDescriptor,
                    provenance,
                    configuration,
                    treeSet,
                    referenceSet,
                    out CSharpCompilation reconstructedCompilation));

                Assert.Equal(original.Assembly.Identity, reconstructedCompilation.Assembly.Identity);
                Assert.Equal(new Version(1, 2, 3, 4), reconstructedCompilation.Assembly.Identity.Version);
                Assert.Equal(NullableContextOptions.Enable, reconstructedCompilation.Options.NullableContextOptions);
                Assert.Equal(OptimizationLevel.Release, reconstructedCompilation.Options.OptimizationLevel);
                Assert.Equal(Platform.X64, reconstructedCompilation.Options.Platform);
                Assert.True(reconstructedCompilation.Options.CheckOverflow);
                Assert.True(reconstructedCompilation.Options.AllowUnsafe);
                Assert.Equal(3, reconstructedCompilation.SyntaxTrees.Length);
                Assert.Equal(
                    LanguageVersion.CSharp12,
                    Assert.IsType<CSharpParseOptions>(
                        reconstructedCompilation.SyntaxTrees[0].Options).LanguageVersion);
                Assert.NotNull(reconstructedCompilation.GetTypeByMetadataName("FeatureBranch"));
                Assert.Null(reconstructedCompilation.GetTypeByMetadataName("FallbackBranch"));
                Assert.NotNull(reconstructedCompilation.GetTypeByMetadataName("AliasConsumer"));
                Assert.NotNull(reconstructedCompilation.GetTypeByMetadataName("CallConsumer"));
                Assert.DoesNotContain(
                    reconstructedCompilation.GetDiagnostics(),
                    diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
                InvocationExpressionSyntax invocation = reconstructedCompilation.SyntaxTrees[2]
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Single();
                IMethodSymbol method = Assert.IsAssignableFrom<IMethodSymbol>(
                    reconstructedCompilation.GetSemanticModel(reconstructedCompilation.SyntaxTrees[2])
                        .GetSymbolInfo(invocation).Symbol);
                Assert.Equal("GetValue", method.Name);
                Assert.Equal("DependencyB", method.ContainingAssembly.Name);
                Assert.Equal(
                    ["depAlias"],
                    reconstructedCompilation.References
                        .OfType<PortableExecutableReference>()
                        .Single(reference => reference.FilePath == dependencyAPath)
                        .Properties.Aliases);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Creates aligned synthetic P3/P4A/P5A/P5G/P5J/P5E inputs.
        /// </summary>
        private static CompositionFixture CreateFixture(
            string assemblyName = "Target.Library",
            string[]? sources = null,
            ImmutableArray<PortableExecutableReference>? references = null,
            OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            sources ??= ["public sealed class TargetType { }"];
            ImmutableArray<PortableExecutableReference> actualReferences =
                references ?? ImmutableArray<PortableExecutableReference>.Empty;
            CSharpParseOptions parseOptions = new(
                LanguageVersion.CSharp12,
                DocumentationMode.Parse,
                SourceCodeKind.Regular,
                ["FEATURE"]);
            CSharpCompilationOptions compilationOptions = new(
                outputKind,
                optimizationLevel: OptimizationLevel.Release,
                platform: Platform.X64,
                nullableContextOptions: NullableContextOptions.Enable,
                checkOverflow: true,
                allowUnsafe: true);
            ImmutableArray<SyntaxTree> trees = sources
                .Select((source, index) => Tree(source, $"/_/Source{index}.cs", parseOptions))
                .ToImmutableArray();
            CSharpCompilation expected = CSharpCompilation.Create(
                assemblyName,
                trees,
                actualReferences,
                compilationOptions);
            ExternalModuleIdentity manifestModule = new(
                expected.SourceModule.Name,
                Guid.NewGuid());
            ImmutableArray<ExternalModuleIdentity>.Builder targetModules =
                ImmutableArray.CreateBuilder<ExternalModuleIdentity>();
            targetModules.Add(manifestModule);

            foreach (PortableExecutableReference reference in actualReferences.Where(
                         reference => reference.Properties.Kind == MetadataImageKind.Module))
            {
                ModuleMetadata module = Assert.IsType<ModuleMetadata>(reference.GetMetadata());
                targetModules.Add(new ExternalModuleIdentity(
                    module.Name,
                    module.GetModuleVersionId()));
            }

            ExternalAssemblyReferenceDescriptor target = new(
                expected.Assembly.Identity,
                targetModules.ToImmutable(),
                "renamed-candidate.unrelated",
                isReferenceAssembly: false);
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                parseOptions,
                compilationOptions,
                trees.Length);

            return new CompositionFixture(
                target,
                CreateProvenance(
                    CreateDebugDirectory(manifestModule),
                    configuration,
                    actualReferences),
                configuration,
                new ExternalCSharpSyntaxTreeSet(trees),
                new ExternalMetadataReferenceSet(actualReferences));
        }

        /// <summary>
        /// Creates a minimal configuration around exact Roslyn instances.
        /// </summary>
        private static ExternalCSharpCompilationConfiguration CreateConfiguration(
            CSharpParseOptions parseOptions,
            CSharpCompilationOptions compilationOptions,
            int sourceFileCount,
            ExternalAssemblySigningProvenance? signingProvenance = null)
        {
            return new ExternalCSharpCompilationConfiguration(
                parseOptions,
                compilationOptions,
                signingProvenance ?? ExternalAssemblySigningProvenance.Unsigned,
                "5.0.0-test",
                "test-runtime",
                sourceFileCount,
                defaultEncodingWebName: null,
                fallbackEncodingWebName: null);
        }

        /// <summary>
        /// Creates aligned P4A/P4B/P5A provenance for finished references.
        /// </summary>
        private static ExternalCompilationProvenanceDescriptor CreateProvenance(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            ExternalCSharpCompilationConfiguration configuration,
            ImmutableArray<PortableExecutableReference> references)
        {
            ExternalPortablePdbDescriptor portablePdb = new(
                default,
                PortablePdbValidationKind.Identity,
                ImmutableArray<ExternalSourceDocumentDescriptor>.Empty,
                sourceLink: null);
            ExternalCompilationMetadataReferencesDescriptor metadataReferences = new(
                references.Select(CreateReferenceDescriptor).ToImmutableArray());
            return new ExternalCompilationProvenanceDescriptor(
                debugDirectory,
                portablePdb,
                compilationOptions: null,
                metadataReferences);
        }

        /// <summary>
        /// Creates minimal P4A provenance for one target manifest module.
        /// </summary>
        private static ExternalPeDebugDirectoryDescriptor CreateDebugDirectory(
            ExternalModuleIdentity manifestModule,
            ExternalAssemblySigningProvenance? signingProvenance = null)
        {
            return new ExternalPeDebugDirectoryDescriptor(
                manifestModule,
                signingProvenance ?? ExternalAssemblySigningProvenance.Unsigned,
                isDeterministic: false,
                ImmutableArray<ExternalCodeViewPdbReference>.Empty,
                ImmutableArray<BlobContentId>.Empty,
                ImmutableArray<ExternalPdbChecksum>.Empty);
        }

        /// <summary>Applies exact fully signed semantic provenance to one fixture.</summary>
        private static CompositionFixture CreateFullySignedFixture(
            CompositionFixture fixture,
            ImmutableArray<byte>? requestedPublicKey = null)
        {
            ImmutableArray<byte> publicKey = requestedPublicKey ?? GetSigningPublicKey();
            CSharpCompilationOptions options = fixture.Configuration.CompilationOptions
                .WithCryptoPublicKey(publicKey);
            CSharpCompilation identity = CSharpCompilation.Create(
                fixture.TargetAssembly.AssemblyIdentity.Name,
                fixture.Trees.Trees,
                fixture.References.References,
                options);
            ExternalAssemblySigningProvenance signing =
                new ExternalAssemblySigningProvenance(
                    ExternalAssemblySigningState.FullySigned,
                    AssemblyFlags.PublicKey,
                    CorFlags.ILOnly | CorFlags.StrongNameSigned,
                    publicKey,
                    identity.Assembly.Identity.PublicKeyToken,
                    strongNameSignatureSize: 128,
                    Enumerable.Repeat((byte)1, 32).ToImmutableArray());
            ExternalCSharpCompilationConfiguration configuration = CreateConfiguration(
                fixture.Configuration.ParseOptions,
                options,
                fixture.Configuration.SourceFileCount,
                signing);
            ExternalAssemblyReferenceDescriptor target = new(
                identity.Assembly.Identity,
                fixture.TargetAssembly.Modules,
                fixture.TargetAssembly.FilePath,
                fixture.TargetAssembly.IsReferenceAssembly);
            ExternalCompilationProvenanceDescriptor provenance = CreateProvenance(
                CreateDebugDirectory(target.Modules[0], signing),
                configuration,
                fixture.References.References);
            return fixture with
            {
                TargetAssembly = target,
                Provenance = provenance,
                Configuration = configuration,
            };
        }

        /// <summary>Gets a real complete public key without acquiring its private half.</summary>
        private static ImmutableArray<byte> GetSigningPublicKey()
        {
            return ImmutableArray.Create(
                typeof(object).Assembly.GetName().GetPublicKey()!);
        }

        /// <summary>Gets a distinct real complete public key for negative tests.</summary>
        private static ImmutableArray<byte> GetDifferentSigningPublicKey()
        {
            ImmutableArray<byte> key = ImmutableArray.Create(
                typeof(CSharpCompilation).Assembly.GetName().GetPublicKey()!);
            Assert.NotEqual(GetSigningPublicKey(), key);
            return key;
        }

        /// <summary>Creates one signed friend compilation without an emit key source.</summary>
        private static CSharpCompilation CreateFriendCompilation(
            SyntaxTree source,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<byte> publicKey)
        {
            CSharpCompilationOptions options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary);
            if (!publicKey.IsEmpty)
            {
                options = options.WithCryptoPublicKey(publicKey);
            }

            return CSharpCompilation.Create(
                "Friend.Target",
                [source],
                references,
                options);
        }

        /// <summary>Checks internal and public invocation binding independently.</summary>
        private static void AssertInvocationBindings(
            CSharpCompilation compilation,
            bool expectInternalBinding)
        {
            InvocationExpressionSyntax[] invocations = compilation.SyntaxTrees.Single()
                .GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .ToArray();
            Assert.Equal(2, invocations.Length);
            SemanticModel model = compilation.GetSemanticModel(
                compilation.SyntaxTrees.Single());
            Assert.Equal(
                expectInternalBinding,
                model.GetSymbolInfo(invocations[0]).Symbol != null);
            IMethodSymbol publicSymbol = Assert.IsAssignableFrom<IMethodSymbol>(
                model.GetSymbolInfo(invocations[1]).Symbol);
            Assert.Equal("Echo", publicSymbol.Name);
            Assert.Equal(SpecialType.System_String, publicSymbol.ReturnType.SpecialType);
        }

        /// <summary>
        /// Describes one already materialized reference as P5A would.
        /// </summary>
        private static ExternalCompilationMetadataReferenceDescriptor CreateReferenceDescriptor(
            PortableExecutableReference reference)
        {
            Metadata metadata = reference.GetMetadata();
            ModuleMetadata module = reference.Properties.Kind switch
            {
                MetadataImageKind.Assembly =>
                    Assert.IsType<AssemblyMetadata>(metadata).GetModules()[0],
                MetadataImageKind.Module => Assert.IsType<ModuleMetadata>(metadata),
                _ => throw new InvalidOperationException("Unexpected metadata image kind."),
            };
            return new ExternalCompilationMetadataReferenceDescriptor(
                reference.FilePath ?? "opaque-reference",
                reference.Properties.Aliases,
                reference.Properties.Kind,
                reference.Properties.EmbedInteropTypes,
                timestamp: 0,
                imageSize: 0,
                module.GetModuleVersionId());
        }

        /// <summary>
        /// Copies framework references into memory to model finished P5E
        /// references whose metadata no longer depends on candidate files.
        /// </summary>
        private static ImmutableArray<PortableExecutableReference>
            CreateInMemoryDefaultReferences(int? take = null)
        {
            IEnumerable<MetadataReference> selected = take.HasValue
                ? MetadataReferences.Default.Take(take.Value)
                : MetadataReferences.Default;
            return selected
                .Select(reference => Assert.IsAssignableFrom<PortableExecutableReference>(reference))
                .Select(reference => MetadataReference.CreateFromImage(
                    ImmutableArray.Create(File.ReadAllBytes(reference.FilePath!)),
                    reference.Properties,
                    documentation: null,
                    reference.FilePath))
                .ToImmutableArray();
        }

        /// <summary>
        /// Emits an in-memory dependency assembly reference.
        /// </summary>
        private static PortableExecutableReference CreateDependencyReference(
            string assemblyName,
            string source)
        {
            return MetadataReference.CreateFromImage(EmitImage(assemblyName, source));
        }

        /// <summary>
        /// Emits controlled assembly or module metadata for test inputs.
        /// </summary>
        private static ImmutableArray<byte> EmitImage(
            string assemblyName,
            string source,
            OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                [Tree(source, "/_/Dependency.cs")],
                MetadataReferences.Default,
                new CSharpCompilationOptions(outputKind));
            using MemoryStream stream = new();
            EmitResult result = compilation.Emit(stream);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            return ImmutableArray.Create(stream.ToArray());
        }

        /// <summary>
        /// Invokes P5K for one fixture.
        /// </summary>
        private static bool TryCreate(
            CompositionFixture fixture,
            out CSharpCompilation compilation)
        {
            return ExternalCSharpCompilationFactory.TryCreate(
                fixture.TargetAssembly,
                fixture.Provenance,
                fixture.Configuration,
                fixture.Trees,
                fixture.References,
                out compilation!);
        }

        /// <summary>
        /// Creates one controlled syntax tree.
        /// </summary>
        /// <param name="source">The source text.</param>
        /// <param name="path">The opaque source path.</param>
        /// <returns>The parsed tree.</returns>
        private static SyntaxTree Tree(string source, string path)
        {
            return Tree(source, path, options: null);
        }

        /// <summary>
        /// Creates one controlled syntax tree with explicit parse options.
        /// </summary>
        private static SyntaxTree Tree(
            string source,
            string path,
            CSharpParseOptions? options)
        {
            return CSharpSyntaxTree.ParseText(
                SourceText.From(source, Encoding.UTF8),
                options,
                path: path);
        }

        /// <summary>
        /// Groups already constructed P5K inputs for focused mutations.
        /// </summary>
        private sealed record CompositionFixture(
            ExternalAssemblyReferenceDescriptor TargetAssembly,
            ExternalCompilationProvenanceDescriptor Provenance,
            ExternalCSharpCompilationConfiguration Configuration,
            ExternalCSharpSyntaxTreeSet Trees,
            ExternalMetadataReferenceSet References);
    }
}
