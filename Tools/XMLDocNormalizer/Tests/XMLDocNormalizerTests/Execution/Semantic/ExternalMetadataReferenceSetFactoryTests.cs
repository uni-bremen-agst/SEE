using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using P5BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceCandidateDescriptorFactoryTests;
using P5DTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests complete ordinal reconstruction of the metadata-reference set
    /// recorded in one external compilation provenance descriptor.
    /// </summary>
    public sealed class ExternalMetadataReferenceSetFactoryTests
    {
        /// <summary>
        /// Reconstructs a real P5A sequence containing three controlled
        /// dependencies, preserves its order, and binds all dependency types.
        /// </summary>
        [Fact]
        public void MultipleAssemblies_RealP5ASequenceReconstructsAndBinds()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                (string Name, string TypeName, byte[] Image)[] dependencies =
                [
                    ("ZetaDependency.dll", "ZetaDependencyType", EmitAssembly(
                        "ZetaDependency", "public sealed class ZetaDependencyType { }")),
                    ("AlphaDependency.dll", "AlphaDependencyType", EmitAssembly(
                        "AlphaDependency", "public sealed class AlphaDependencyType { }")),
                    ("MiddleDependency.dll", "MiddleDependencyType", EmitAssembly(
                        "MiddleDependency", "public sealed class MiddleDependencyType { }"))
                ];
                Dictionary<string, string> candidatePaths = new(StringComparer.Ordinal);
                List<MetadataReference> originalReferences =
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                ];
                candidatePaths.Add(
                    Path.GetFileName(typeof(object).Assembly.Location),
                    typeof(object).Assembly.Location);

                foreach ((string name, _, byte[] image) in dependencies)
                {
                    string path = Path.Combine(directory, name);
                    File.WriteAllBytes(path, image);
                    candidatePaths.Add(name, path);
                    originalReferences.Add(MetadataReference.CreateFromFile(path));
                }

                P5BTests.EmittedPortablePdb consumer = P5BTests.EmitPortablePdb(
                    "ReferenceSetConsumer",
                    "public sealed class Consumer { public ZetaDependencyType Z = null!; public AlphaDependencyType A = null!; public MiddleDependencyType M = null!; }",
                    originalReferences);
                ExternalCompilationProvenanceDescriptor provenance =
                    P5BTests.ReadCompilationProvenance(consumer);
                ExternalCompilationMetadataReferencesDescriptor expected = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                Assert.Equal(
                    dependencies.Select(dependency => dependency.Name),
                    expected.References
                        .Where(reference => candidatePaths.ContainsKey(reference.Name))
                        .Select(reference => reference.Name)
                        .Where(name => dependencies.Any(dependency => dependency.Name == name)));
                ValidatedExternalMetadataReferenceMaterial[] materials = expected.References
                    .Select(reference => CreateFileMaterial(reference, candidatePaths[reference.Name]))
                    .ToArray();

                ExternalMetadataReferenceSet referenceSet = CreateSet(provenance, materials);

                Assert.Equal(expected.References.Length, referenceSet.References.Length);
                Assert.Equal(
                    expected.References.Select(reference => reference.ModuleVersionId),
                    referenceSet.References.Select(P5DTests.ReadMvid));

                for (int index = 0; index < expected.References.Length; index++)
                {
                    AssertProperties(referenceSet.References[index], expected.References[index]);
                }

                CSharpCompilation bindingCompilation = CreateCompilation(
                    "ReferenceSetBinding",
                    "public sealed class Binding { public ZetaDependencyType Z = null!; public AlphaDependencyType A = null!; public MiddleDependencyType M = null!; }",
                    referenceSet.References);
                AssertNoErrors(bindingCompilation);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects a material sequence shorter than the expected sequence.
        /// </summary>
        [Fact]
        public void MissingMaterial_FailsWithoutPartialSet()
        {
            ReferenceFixture fixture = CreateFixture("MissingA", "MissingB", "MissingC");

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                fixture.Provenance,
                fixture.Materials.Take(2).ToArray(),
                out ExternalMetadataReferenceSet referenceSet));
            Assert.Null(referenceSet);
        }

        /// <summary>
        /// Rejects a material sequence longer than the expected sequence.
        /// </summary>
        [Fact]
        public void ExtraMaterial_FailsWithoutAcceptingUndocumentedReference()
        {
            ReferenceFixture fixture = CreateFixture("ExtraA", "ExtraB", "ExtraC");
            ExternalCompilationProvenanceDescriptor shorter = CreateProvenance(
                fixture.Expected.Take(2));

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                shorter,
                fixture.Materials,
                out ExternalMetadataReferenceSet referenceSet));
            Assert.Null(referenceSet);
        }

        /// <summary>
        /// Rejects swapped materials instead of searching for matching MVIDs.
        /// </summary>
        [Fact]
        public void SwappedMaterials_FailWithoutAutomaticReordering()
        {
            ReferenceFixture fixture = CreateFixture("SwapA", "SwapB");
            ValidatedExternalMetadataReferenceMaterial[] swapped =
            [
                fixture.Materials[1],
                fixture.Materials[0]
            ];

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                fixture.Provenance,
                swapped,
                out _));
        }

        /// <summary>
        /// Rejects the same binary when its material was validated against a
        /// descriptor with different reference properties.
        /// </summary>
        [Fact]
        public void SameBinaryWithWrongExpectedDescriptor_Fails()
        {
            byte[] image = EmitAssembly("WrongExpected", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                image,
                "WrongExpected.dll");
            ExternalCompilationMetadataReferenceDescriptor different = P5BTests.CopyExpected(
                expected,
                aliases: ImmutableArray.Create("different"));

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                CreateProvenance(expected),
                new[] { CreateMaterial(different, image) },
                out _));
        }

        /// <summary>
        /// Accepts separately allocated descriptors whose complete field
        /// values are semantically identical.
        /// </summary>
        [Fact]
        public void SeparateSemanticallyIdenticalDescriptor_Succeeds()
        {
            byte[] image = EmitAssembly("EquivalentExpected", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                image,
                "EquivalentExpected.dll",
                ImmutableArray.Create("zeta", "alpha"),
                embedInteropTypes: true);
            ExternalCompilationMetadataReferenceDescriptor separate = P5BTests.CopyExpected(
                expected);
            Assert.NotSame(expected, separate);

            ExternalMetadataReferenceSet set = CreateSet(
                CreateProvenance(expected),
                new[] { CreateMaterial(separate, image) });

            Assert.Single(set.References);
            AssertProperties(set.References[0], expected);
        }

        /// <summary>
        /// Treats the ordinal name as expected provenance even though it is
        /// not binary identity.
        /// </summary>
        [Fact]
        public void NameMismatch_Fails()
        {
            AssertDescriptorMismatch(
                (expected, _) => P5BTests.CopyExpected(expected),
                expected => new ExternalCompilationMetadataReferenceDescriptor(
                    "OtherName.dll",
                    expected.Aliases,
                    expected.Kind,
                    expected.EmbedInteropTypes,
                    expected.Timestamp,
                    expected.ImageSize,
                    expected.ModuleVersionId));
        }

        /// <summary>
        /// Treats alias order as exact original reference provenance.
        /// </summary>
        [Fact]
        public void AliasOrderMismatch_Fails()
        {
            byte[] image = EmitAssembly("AliasOrder", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                image,
                "AliasOrder.dll",
                ImmutableArray.Create("zeta", "alpha"));
            ExternalCompilationMetadataReferenceDescriptor reversed = P5BTests.CopyExpected(
                expected,
                aliases: ImmutableArray.Create("alpha", "zeta"));

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                CreateProvenance(expected),
                new[] { CreateMaterial(reversed, image) },
                out _));
        }

        /// <summary>
        /// Rejects an EmbedInteropTypes mismatch at its ordinal.
        /// </summary>
        [Fact]
        public void EmbedInteropTypesMismatch_Fails()
        {
            AssertDescriptorMismatch(
                (expected, _) => P5BTests.CopyExpected(expected),
                expected => P5BTests.CopyExpected(
                    expected,
                    embedInteropTypes: !expected.EmbedInteropTypes));
        }

        /// <summary>
        /// Rejects a metadata-image-kind mismatch at its ordinal.
        /// </summary>
        [Fact]
        public void KindMismatch_Fails()
        {
            AssertDescriptorMismatch(
                (expected, _) => P5BTests.CopyExpected(expected),
                expected => P5BTests.CopyExpected(
                    expected,
                    kind: MetadataImageKind.Module));
        }

        /// <summary>
        /// Identifies each concrete PE provenance field as part of ordinal
        /// expected-reference equality.
        /// </summary>
        /// <param name="field">The provenance field to change.</param>
        [Theory]
        [InlineData("Timestamp")]
        [InlineData("ImageSize")]
        [InlineData("Mvid")]
        public void BinaryProvenanceFieldMismatch_Fails(string field)
        {
            AssertDescriptorMismatch(
                (expected, _) => P5BTests.CopyExpected(expected),
                expected => field switch
                {
                    "Timestamp" => P5BTests.CopyExpected(
                        expected,
                        timestamp: unchecked(expected.Timestamp + 1)),
                    "ImageSize" => P5BTests.CopyExpected(
                        expected,
                        imageSize: unchecked(expected.ImageSize + 1)),
                    "Mvid" => P5BTests.CopyExpected(
                        expected,
                        moduleVersionId: Guid.NewGuid()),
                    _ => throw new InvalidOperationException("Unexpected test field.")
                });
        }

        /// <summary>
        /// Reconstructs identical duplicate ordinals independently and keeps
        /// both references.
        /// </summary>
        [Fact]
        public void IdenticalDuplicateReferences_RemainDuplicated()
        {
            byte[] image = EmitAssembly("Duplicate", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                image,
                "Duplicate.dll");

            ExternalMetadataReferenceSet set = CreateSet(
                CreateProvenance(expected, expected),
                new[]
                {
                    CreateMaterial(expected, image),
                    CreateMaterial(expected, image)
                });

            Assert.Equal(2, set.References.Length);
            Assert.NotSame(set.References[0], set.References[1]);
            Assert.Equal(
                P5DTests.ReadMvid(set.References[0]),
                P5DTests.ReadMvid(set.References[1]));
        }

        /// <summary>
        /// Reconstructs the same binary twice with distinct alias properties.
        /// </summary>
        [Fact]
        public void SameBinaryWithDifferentAliases_RemainsTwoReferences()
        {
            byte[] image = EmitAssembly("AliasVariants", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor first = CreateExpected(
                image,
                "AliasVariants.dll",
                ImmutableArray.Create("global"));
            ExternalCompilationMetadataReferenceDescriptor second = P5BTests.CopyExpected(
                first,
                aliases: ImmutableArray.Create("foo"));

            ExternalMetadataReferenceSet set = CreateSet(
                CreateProvenance(first, second),
                new[] { CreateMaterial(first, image), CreateMaterial(second, image) });

            Assert.Equal(new[] { "global" }, set.References[0].Properties.Aliases);
            Assert.Equal(new[] { "foo" }, set.References[1].Properties.Aliases);
            Assert.Equal(
                P5DTests.ReadMvid(set.References[0]),
                P5DTests.ReadMvid(set.References[1]));
        }

        /// <summary>
        /// Reconstructs the same binary twice with distinct interop-embedding
        /// properties.
        /// </summary>
        [Fact]
        public void SameBinaryWithDifferentEmbedInteropTypes_RemainsTwoReferences()
        {
            byte[] image = EmitAssembly("InteropVariants", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor first = CreateExpected(
                image,
                "InteropVariants.dll");
            ExternalCompilationMetadataReferenceDescriptor second = P5BTests.CopyExpected(
                first,
                embedInteropTypes: true);

            ExternalMetadataReferenceSet set = CreateSet(
                CreateProvenance(first, second),
                new[] { CreateMaterial(first, image), CreateMaterial(second, image) });

            Assert.False(set.References[0].Properties.EmbedInteropTypes);
            Assert.True(set.References[1].Properties.EmbedInteropTypes);
            Assert.Equal(
                P5DTests.ReadMvid(set.References[0]),
                P5DTests.ReadMvid(set.References[1]));
        }

        /// <summary>
        /// Produces a real immutable empty sequence when the reference CDI is
        /// present and empty.
        /// </summary>
        [Fact]
        public void PresentEmptyMetadataReferences_SucceedsWithEmptySet()
        {
            ExternalMetadataReferenceSet set = CreateSet(
                CreateProvenance(Array.Empty<ExternalCompilationMetadataReferenceDescriptor>()),
                Array.Empty<ValidatedExternalMetadataReferenceMaterial>());

            Assert.False(set.References.IsDefault);
            Assert.Empty(set.References);
        }

        /// <summary>
        /// Rejects absent metadata-reference provenance instead of inventing
        /// an empty set.
        /// </summary>
        [Fact]
        public void MissingMetadataReferences_FailsForEmptyMaterials()
        {
            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                CreateProvenance(
                    (IEnumerable<ExternalCompilationMetadataReferenceDescriptor>?)null),
                Array.Empty<ValidatedExternalMetadataReferenceMaterial>(),
                out ExternalMetadataReferenceSet set));
            Assert.Null(set);
        }

        /// <summary>
        /// Rejects a null material slot before invoking the individual
        /// reconstruction pipeline.
        /// </summary>
        [Fact]
        public void NullMaterialElement_FailsClosed()
        {
            byte[] image = EmitAssembly("NullSlot", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                image,
                "NullSlot.dll");

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                CreateProvenance(expected),
                new ValidatedExternalMetadataReferenceMaterial[] { null! },
                out _));
        }

        /// <summary>
        /// Propagates one P5D multi-module failure to the entire set without
        /// exposing an earlier successfully reconstructed reference.
        /// </summary>
        [Fact]
        public void P5DFailure_InvalidatesEntireSet()
        {
            byte[] normalImage = EmitAssembly("AtomicNormal", "public sealed class Normal { }");
            byte[] multiModuleImage = P5DTests.EmitMultiModuleAssembly();
            ExternalCompilationMetadataReferenceDescriptor normal = CreateExpected(
                normalImage,
                "AtomicNormal.dll");
            ExternalCompilationMetadataReferenceDescriptor multiModule = CreateExpected(
                multiModuleImage,
                "MultiModuleAssembly.dll");

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                CreateProvenance(normal, multiModule),
                new[]
                {
                    CreateMaterial(normal, normalImage),
                    CreateMaterial(multiModule, multiModuleImage)
                },
                out ExternalMetadataReferenceSet set));
            Assert.Null(set);
        }

        /// <summary>
        /// Preserves a mixed assembly and standalone-netmodule ordinal
        /// sequence.
        /// </summary>
        [Fact]
        public void AssemblyAndStandaloneNetModule_SucceedInOrder()
        {
            byte[] assemblyImage = EmitAssembly("MixedAssembly", "public sealed class AssemblyType { }");
            byte[] moduleImage = P5BTests.EmitPe(
                "MixedModule",
                "public sealed class ModuleType { }",
                OutputKind.NetModule);
            ExternalCompilationMetadataReferenceDescriptor assembly = CreateExpected(
                assemblyImage,
                "MixedAssembly.dll");
            ExternalCompilationMetadataReferenceDescriptor module = CreateExpected(
                moduleImage,
                "MixedModule.netmodule");

            ExternalMetadataReferenceSet set = CreateSet(
                CreateProvenance(assembly, module),
                new[]
                {
                    CreateMaterial(assembly, assemblyImage),
                    CreateMaterial(module, moduleImage)
                });

            Assert.Equal(MetadataImageKind.Assembly, set.References[0].Properties.Kind);
            Assert.Equal(MetadataImageKind.Module, set.References[1].Properties.Kind);
        }

        /// <summary>
        /// Reconstructs file-backed material after its candidate file was
        /// deleted, proving that P5E performs no candidate file access.
        /// </summary>
        [Fact]
        public void DeletedCandidateFile_IsNeverReopened()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly("NoReopen", "public sealed class Type { }");
                ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                    image,
                    "ExpectedName.dll");
                string path = Path.Combine(directory, "Renamed.bin");
                File.WriteAllBytes(path, image);
                ValidatedExternalMetadataReferenceMaterial material =
                    CreateFileMaterial(expected, path);
                File.Delete(path);

                ExternalMetadataReferenceSet set = CreateSet(
                    CreateProvenance(expected),
                    new[] { material });

                Assert.False(File.Exists(path));
                Assert.Equal(path, set.References[0].FilePath);
                Assert.Equal(expected.ModuleVersionId, P5DTests.ReadMvid(set.References[0]));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Applies the normal argument-null convention to both required
        /// top-level inputs.
        /// </summary>
        [Fact]
        public void NullInputs_ThrowArgumentNullException()
        {
            ExternalCompilationProvenanceDescriptor provenance = CreateProvenance(
                Array.Empty<ExternalCompilationMetadataReferenceDescriptor>());

            Assert.Throws<ArgumentNullException>(
                () => ExternalMetadataReferenceSetFactory.TryCreate(
                    null!,
                    Array.Empty<ValidatedExternalMetadataReferenceMaterial>(),
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    null!,
                    out _));
        }

        /// <summary>
        /// Exercises a descriptor mismatch while keeping the material itself
        /// P5C-valid against its own expected descriptor.
        /// </summary>
        private static void AssertDescriptorMismatch(
            Func<ExternalCompilationMetadataReferenceDescriptor, byte[],
                ExternalCompilationMetadataReferenceDescriptor> materialExpectedFactory,
            Func<ExternalCompilationMetadataReferenceDescriptor,
                ExternalCompilationMetadataReferenceDescriptor> provenanceExpectedFactory)
        {
            byte[] image = EmitAssembly("DescriptorMismatch", "public sealed class Type { }");
            ExternalCompilationMetadataReferenceDescriptor baseline = CreateExpected(
                image,
                "DescriptorMismatch.dll");
            ExternalCompilationMetadataReferenceDescriptor materialExpected =
                materialExpectedFactory(baseline, image);
            ExternalCompilationMetadataReferenceDescriptor provenanceExpected =
                provenanceExpectedFactory(baseline);

            Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                CreateProvenance(provenanceExpected),
                new[] { CreateMaterial(materialExpected, image) },
                out _));
        }

        /// <summary>
        /// Creates a small valid top-level provenance shell with present or
        /// absent metadata-reference provenance.
        /// </summary>
        internal static ExternalCompilationProvenanceDescriptor CreateProvenance(
            IEnumerable<ExternalCompilationMetadataReferenceDescriptor>? expected)
        {
            ExternalPortablePdbDescriptor portablePdb = new(
                default,
                PortablePdbValidationKind.Identity,
                ImmutableArray<ExternalSourceDocumentDescriptor>.Empty,
                sourceLink: null);
            ExternalCompilationMetadataReferencesDescriptor? metadataReferences = expected == null
                ? null
                : new ExternalCompilationMetadataReferencesDescriptor(expected.ToImmutableArray());
            return new ExternalCompilationProvenanceDescriptor(
                portablePdb,
                compilationOptions: null,
                metadataReferences);
        }

        /// <summary>
        /// Creates a top-level provenance descriptor from explicit expected
        /// ordinals.
        /// </summary>
        internal static ExternalCompilationProvenanceDescriptor CreateProvenance(
            params ExternalCompilationMetadataReferenceDescriptor[] expected)
        {
            return CreateProvenance(
                (IEnumerable<ExternalCompilationMetadataReferenceDescriptor>)expected);
        }

        /// <summary>
        /// Creates several independent assembly descriptors and their aligned
        /// validated materials.
        /// </summary>
        private static ReferenceFixture CreateFixture(params string[] names)
        {
            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor>.Builder expected =
                ImmutableArray.CreateBuilder<ExternalCompilationMetadataReferenceDescriptor>(names.Length);
            ImmutableArray<ValidatedExternalMetadataReferenceMaterial>.Builder materials =
                ImmutableArray.CreateBuilder<ValidatedExternalMetadataReferenceMaterial>(names.Length);

            foreach (string name in names)
            {
                byte[] image = EmitAssembly(name, $"public sealed class {name}Type {{ }}");
                ExternalCompilationMetadataReferenceDescriptor descriptor = CreateExpected(
                    image,
                    $"{name}.dll");
                expected.Add(descriptor);
                materials.Add(CreateMaterial(descriptor, image));
            }

            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> expectedArray =
                expected.MoveToImmutable();
            return new ReferenceFixture(
                CreateProvenance(expectedArray),
                expectedArray,
                materials.MoveToImmutable());
        }

        /// <summary>
        /// Creates one expected descriptor from controlled PE bytes.
        /// </summary>
        internal static ExternalCompilationMetadataReferenceDescriptor CreateExpected(
            byte[] image,
            string name,
            ImmutableArray<string> aliases = default,
            bool embedInteropTypes = false)
        {
            return P5BTests.CreateExpectedReference(
                image,
                name,
                aliases,
                embedInteropTypes);
        }

        /// <summary>
        /// Creates validated in-memory material through the existing P5C test
        /// helper.
        /// </summary>
        private static ValidatedExternalMetadataReferenceMaterial CreateMaterial(
            ExternalCompilationMetadataReferenceDescriptor expected,
            byte[] image)
        {
            return P5DTests.CreateMaterial(expected, image);
        }

        /// <summary>
        /// Creates validated file-backed material through P5C.
        /// </summary>
        private static ValidatedExternalMetadataReferenceMaterial CreateFileMaterial(
            ExternalCompilationMetadataReferenceDescriptor expected,
            string path)
        {
            Assert.True(ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                expected,
                path,
                out ValidatedExternalMetadataReferenceMaterial material));
            return material;
        }

        /// <summary>
        /// Creates a required successful P5E result.
        /// </summary>
        private static ExternalMetadataReferenceSet CreateSet(
            ExternalCompilationProvenanceDescriptor provenance,
            IReadOnlyList<ValidatedExternalMetadataReferenceMaterial> materials)
        {
            Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                provenance,
                materials,
                out ExternalMetadataReferenceSet set));
            return set;
        }

        /// <summary>
        /// Emits a controlled deterministic assembly using the existing P5B
        /// test infrastructure.
        /// </summary>
        internal static byte[] EmitAssembly(string assemblyName, string source)
        {
            return P5BTests.EmitPe(
                assemblyName,
                source,
                OutputKind.DynamicallyLinkedLibrary);
        }

        /// <summary>
        /// Creates a controlled compilation for reference-set binding only.
        /// </summary>
        internal static CSharpCompilation CreateCompilation(
            string assemblyName,
            string source,
            IEnumerable<MetadataReference> references)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                Microsoft.CodeAnalysis.Text.SourceText.From(source, Encoding.UTF8),
                path: "/_/Source.cs");
            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    deterministic: true));
        }

        /// <summary>
        /// Requires a controlled binding compilation to contain no errors.
        /// </summary>
        internal static void AssertNoErrors(CSharpCompilation compilation)
        {
            Assert.DoesNotContain(
                compilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Requires all P5D reference properties to equal their expected
        /// descriptor values.
        /// </summary>
        private static void AssertProperties(
            PortableExecutableReference reference,
            ExternalCompilationMetadataReferenceDescriptor expected)
        {
            Assert.Equal(expected.Kind, reference.Properties.Kind);
            Assert.Equal(expected.EmbedInteropTypes, reference.Properties.EmbedInteropTypes);
            Assert.Equal(expected.Aliases, reference.Properties.Aliases);
        }

        /// <summary>
        /// Stores aligned expected descriptors and validated materials for
        /// concise completeness tests.
        /// </summary>
        private sealed record ReferenceFixture(
            ExternalCompilationProvenanceDescriptor Provenance,
            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> Expected,
            ImmutableArray<ValidatedExternalMetadataReferenceMaterial> Materials);
    }
}
