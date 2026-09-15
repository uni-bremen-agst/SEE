using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using P5BTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceCandidateDescriptorFactoryTests;
using P5DTests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceFactoryTests;
using P5ETests = XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceSetFactoryTests;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests explicit ordinal acquisition of complete P5C metadata-reference
    /// material sequences from caller-supplied candidate file paths.
    /// </summary>
    public sealed class ExternalMetadataReferenceMaterialSetFactoryTests
    {
        /// <summary>
        /// Acquires the complete real P5A sequence for three controlled
        /// dependencies, preserves order through P5E, and binds every type.
        /// </summary>
        [Fact]
        public void MultipleCandidates_RealP5AThroughP5ESequenceBinds()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                (string Name, string TypeName, byte[] Image)[] dependencies =
                [
                    ("ZetaCandidate.dll", "ZetaCandidateType", EmitAssembly(
                        "ZetaCandidate", "public sealed class ZetaCandidateType { }")),
                    ("AlphaCandidate.dll", "AlphaCandidateType", EmitAssembly(
                        "AlphaCandidate", "public sealed class AlphaCandidateType { }")),
                    ("MiddleCandidate.dll", "MiddleCandidateType", EmitAssembly(
                        "MiddleCandidate", "public sealed class MiddleCandidateType { }"))
                ];
                Dictionary<string, string> pathsByExpectedName = new(StringComparer.Ordinal);
                List<MetadataReference> originalReferences =
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                ];
                pathsByExpectedName.Add(
                    Path.GetFileName(typeof(object).Assembly.Location),
                    typeof(object).Assembly.Location);

                foreach ((string name, _, byte[] image) in dependencies)
                {
                    string path = Path.Combine(directory, name);
                    File.WriteAllBytes(path, image);
                    pathsByExpectedName.Add(name, path);
                    originalReferences.Add(MetadataReference.CreateFromFile(path));
                }

                P5BTests.EmittedPortablePdb consumer = P5BTests.EmitPortablePdb(
                    "MaterialSetConsumer",
                    "public sealed class Consumer { public ZetaCandidateType Z = null!; public AlphaCandidateType A = null!; public MiddleCandidateType M = null!; }",
                    originalReferences);
                ExternalCompilationProvenanceDescriptor provenance =
                    P5BTests.ReadCompilationProvenance(consumer);
                ExternalCompilationMetadataReferencesDescriptor expected = Assert.IsType<
                    ExternalCompilationMetadataReferencesDescriptor>(provenance.MetadataReferences);
                string[] candidatePaths = expected.References
                    .Select(reference => pathsByExpectedName[reference.Name])
                    .ToArray();

                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    provenance,
                    candidatePaths);

                Assert.Equal(expected.References.Length, materialSet.Materials.Length);
                Assert.Equal(
                    expected.References.Select(reference => reference.Name),
                    materialSet.Materials.Select(
                        material => material.Candidate.ExpectedReference.Name));
                Assert.Equal(
                    expected.References.Select(reference => reference.ModuleVersionId),
                    materialSet.Materials.Select(
                        material => material.Candidate.Module.ModuleVersionId));
                Assert.Equal(candidatePaths, materialSet.Materials.Select(
                    material => material.Candidate.FilePath));

                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.Equal(
                    expected.References.Select(reference => reference.ModuleVersionId),
                    referenceSet.References.Select(P5DTests.ReadMvid));
                CSharpCompilation binding = P5ETests.CreateCompilation(
                    "MaterialSetBinding",
                    "public sealed class Binding { public ZetaCandidateType Z = null!; public AlphaCandidateType A = null!; public MiddleCandidateType M = null!; }",
                    referenceSet.References);
                P5ETests.AssertNoErrors(binding);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects a shorter path list before attempting candidate I/O.
        /// </summary>
        [Fact]
        public void MissingCandidateCount_FailsBeforeOpeningCandidates()
        {
            DescriptorFixture fixture = CreateDescriptorFixture("MissingOne", "MissingTwo");

            Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                fixture.Provenance,
                new[] { "does-not-need-to-exist.dll" },
                out ExternalMetadataReferenceMaterialSet materialSet));
            Assert.Null(materialSet);
        }

        /// <summary>
        /// Rejects a longer path list before attempting candidate I/O.
        /// </summary>
        [Fact]
        public void ExtraCandidateCount_FailsBeforeOpeningCandidates()
        {
            DescriptorFixture fixture = CreateDescriptorFixture("ExtraOne");

            Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                fixture.Provenance,
                new[] { "missing-one.dll", "missing-two.dll" },
                out ExternalMetadataReferenceMaterialSet materialSet));
            Assert.Null(materialSet);
        }

        /// <summary>
        /// Rejects swapped candidate paths without searching for a later
        /// matching ordinal.
        /// </summary>
        [Fact]
        public void SwappedCandidatePaths_FailWithoutRemapping()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                FileFixture first = CreateFileFixture(directory, "SwapOne", "one.dll");
                FileFixture second = CreateFileFixture(directory, "SwapTwo", "two.dll");
                ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                    first.Expected,
                    second.Expected);

                Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    provenance,
                    new[] { second.Path, first.Path },
                    out _));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Accepts a renamed candidate because the caller path is location
        /// provenance rather than binary identity.
        /// </summary>
        [Fact]
        public void RenamedCandidate_SucceedsWithoutNameBasedLookup()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly("Original", "public sealed class Type { }");
                ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                    image,
                    "Original.dll");
                string path = WriteImage(directory, "RenamedCandidate.bin", image);

                ExternalMetadataReferenceMaterialSet set = CreateMaterialSet(
                    P5ETests.CreateProvenance(expected),
                    new[] { path });

                Assert.Equal(path, set.Materials[0].Candidate.FilePath);
                Assert.Same(expected, set.Materials[0].Candidate.ExpectedReference);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Materializes the same path twice for two identical expected
        /// ordinals without deduplication.
        /// </summary>
        [Fact]
        public void DuplicateExpectedReferencesAtSamePath_RemainDuplicated()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                FileFixture fixture = CreateFileFixture(directory, "Duplicate", "duplicate.dll");

                ExternalMetadataReferenceMaterialSet set = CreateMaterialSet(
                    P5ETests.CreateProvenance(fixture.Expected, fixture.Expected),
                    new[] { fixture.Path, fixture.Path });

                Assert.Equal(2, set.Materials.Length);
                Assert.NotSame(set.Materials[0], set.Materials[1]);
                Assert.Equal(fixture.Path, set.Materials[0].Candidate.FilePath);
                Assert.Equal(fixture.Path, set.Materials[1].Candidate.FilePath);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Materializes one path independently for distinct alias-bearing
        /// expected ordinals.
        /// </summary>
        [Fact]
        public void SamePathWithDifferentAliases_PreservesEachExpectedDescriptor()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly("AliasPath", "public sealed class Type { }");
                string path = WriteImage(directory, "candidate.dll", image);
                ExternalCompilationMetadataReferenceDescriptor first = CreateExpected(
                    image,
                    "AliasPath.dll",
                    ImmutableArray.Create("first"));
                ExternalCompilationMetadataReferenceDescriptor second = P5BTests.CopyExpected(
                    first,
                    aliases: ImmutableArray.Create("second"));

                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    P5ETests.CreateProvenance(first, second),
                    new[] { path, path });

                Assert.Equal(new[] { "first" }, materialSet.Materials[0]
                    .Candidate.ExpectedReference.Aliases);
                Assert.Equal(new[] { "second" }, materialSet.Materials[1]
                    .Candidate.ExpectedReference.Aliases);
                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    P5ETests.CreateProvenance(first, second),
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.Equal(new[] { "first" }, referenceSet.References[0].Properties.Aliases);
                Assert.Equal(new[] { "second" }, referenceSet.References[1].Properties.Aliases);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Materializes one path independently for distinct interop-embedding
        /// expected ordinals.
        /// </summary>
        [Fact]
        public void SamePathWithDifferentEmbedInteropTypes_PreservesEachExpectedDescriptor()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly("InteropPath", "public sealed class Type { }");
                string path = WriteImage(directory, "candidate.dll", image);
                ExternalCompilationMetadataReferenceDescriptor first = CreateExpected(
                    image,
                    "InteropPath.dll");
                ExternalCompilationMetadataReferenceDescriptor second = P5BTests.CopyExpected(
                    first,
                    embedInteropTypes: true);

                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    P5ETests.CreateProvenance(first, second),
                    new[] { path, path });

                Assert.False(materialSet.Materials[0]
                    .Candidate.ExpectedReference.EmbedInteropTypes);
                Assert.True(materialSet.Materials[1]
                    .Candidate.ExpectedReference.EmbedInteropTypes);
                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    P5ETests.CreateProvenance(first, second),
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.False(referenceSet.References[0].Properties.EmbedInteropTypes);
                Assert.True(referenceSet.References[1].Properties.EmbedInteropTypes);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects absent metadata-reference provenance without performing I/O.
        /// </summary>
        [Fact]
        public void MissingMetadataReferences_FailsForEmptyPaths()
        {
            ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                (IEnumerable<ExternalCompilationMetadataReferenceDescriptor>?)null);

            Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                provenance,
                Array.Empty<string>(),
                out ExternalMetadataReferenceMaterialSet set));
            Assert.Null(set);
        }

        /// <summary>
        /// Produces a real non-default empty immutable material sequence for a
        /// present empty metadata-reference CDI.
        /// </summary>
        [Fact]
        public void PresentEmptyMetadataReferences_SucceedsWithEmptySet()
        {
            ExternalMetadataReferenceMaterialSet set = CreateMaterialSet(
                P5ETests.CreateProvenance(
                    Array.Empty<ExternalCompilationMetadataReferenceDescriptor>()),
                Array.Empty<string>());

            Assert.False(set.Materials.IsDefault);
            Assert.Empty(set.Materials);
        }

        /// <summary>
        /// Preserves P5C's fail-closed behavior for a nonexistent candidate.
        /// </summary>
        [Fact]
        public void NonexistentCandidate_FailsClosed()
        {
            DescriptorFixture fixture = CreateDescriptorFixture("Nonexistent");
            string path = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"),
                "missing.dll");

            Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                fixture.Provenance,
                new[] { path },
                out _));
        }

        /// <summary>
        /// Preserves P5C's fail-closed behavior when a candidate path names a
        /// directory rather than a file.
        /// </summary>
        [Fact]
        public void DirectoryCandidate_FailsClosed()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                DescriptorFixture fixture = CreateDescriptorFixture("DirectoryCandidate");
                Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    fixture.Provenance,
                    new[] { directory },
                    out _));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Propagates P5C rejection of a malformed PE candidate atomically.
        /// </summary>
        [Fact]
        public void MalformedCandidate_FailsWithoutPartialSet()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                FileFixture first = CreateFileFixture(directory, "ValidBeforeMalformed", "valid.dll");
                byte[] expectedImage = EmitAssembly("MalformedExpected", "public sealed class Type { }");
                ExternalCompilationMetadataReferenceDescriptor second = CreateExpected(
                    expectedImage,
                    "malformed.dll");
                string malformedPath = WriteImage(
                    directory,
                    "malformed.dll",
                    new byte[] { 0x4d, 0x5a, 0x00 });

                Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    P5ETests.CreateProvenance(first.Expected, second),
                    new[] { first.Path, malformedPath },
                    out ExternalMetadataReferenceMaterialSet set));
                Assert.Null(set);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects a valid PE from a different concrete build without fallback
        /// candidate search.
        /// </summary>
        [Fact]
        public void WrongBuild_FailsClosed()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] expectedImage = EmitAssembly(
                    "WrongBuild",
                    "public sealed class ExpectedType { }");
                byte[] wrongImage = EmitAssembly(
                    "WrongBuild",
                    "public sealed class WrongType { }");
                ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                    expectedImage,
                    "WrongBuild.dll");
                string path = WriteImage(directory, "candidate.dll", wrongImage);

                Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    P5ETests.CreateProvenance(expected),
                    new[] { path },
                    out _));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Rejects a different build even when its complete assembly identity
        /// equals the expected build identity.
        /// </summary>
        [Fact]
        public void SameAssemblyIdentityDifferentBuild_FailsClosed()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] firstImage = EmitAssembly(
                    "SameIdentity",
                    "public sealed class FirstType { }");
                byte[] secondImage = EmitAssembly(
                    "SameIdentity",
                    "public sealed class SecondType { }");
                ExternalCompilationMetadataReferenceDescriptor first = CreateExpected(
                    firstImage,
                    "SameIdentity.dll");
                ExternalCompilationMetadataReferenceDescriptor second = CreateExpected(
                    secondImage,
                    "SameIdentity.dll");
                ValidatedExternalMetadataReferenceMaterial firstMaterial =
                    P5DTests.CreateMaterial(first, firstImage);
                ValidatedExternalMetadataReferenceMaterial secondMaterial =
                    P5DTests.CreateMaterial(second, secondImage);
                Assert.Equal(
                    firstMaterial.Candidate.AssemblyIdentity,
                    secondMaterial.Candidate.AssemblyIdentity);
                Assert.NotEqual(first.ModuleVersionId, second.ModuleVersionId);
                string secondPath = WriteImage(directory, "candidate.dll", secondImage);

                Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    P5ETests.CreateProvenance(first),
                    new[] { secondPath },
                    out _));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Acquires an assembly, standalone netmodule, and second assembly in
        /// their supplied ordinal order and passes them directly to P5E.
        /// </summary>
        [Fact]
        public void AssemblyNetModuleAssembly_SucceedsInOrderThroughP5E()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                FileFixture first = CreateFileFixture(directory, "MixedFirst", "first.dll");
                byte[] moduleImage = P5BTests.EmitPe(
                    "MixedModule",
                    "public sealed class ModuleType { }",
                    OutputKind.NetModule);
                ExternalCompilationMetadataReferenceDescriptor module = CreateExpected(
                    moduleImage,
                    "MixedModule.netmodule");
                string modulePath = WriteImage(directory, "module.bin", moduleImage);
                FileFixture third = CreateFileFixture(directory, "MixedThird", "third.dll");
                ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                    first.Expected,
                    module,
                    third.Expected);

                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    provenance,
                    new[] { first.Path, modulePath, third.Path });

                Assert.Equal(
                    new[]
                    {
                        MetadataImageKind.Assembly,
                        MetadataImageKind.Module,
                        MetadataImageKind.Assembly
                    },
                    materialSet.Materials.Select(material => material.Candidate.Kind));
                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.Equal(
                    new[]
                    {
                        MetadataImageKind.Assembly,
                        MetadataImageKind.Module,
                        MetadataImageKind.Assembly
                    },
                    referenceSet.References.Select(reference => reference.Properties.Kind));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps all snapshots usable by P5E after every candidate file is
        /// deleted following P5F acquisition.
        /// </summary>
        [Fact]
        public void CandidateFilesDeletedAfterP5F_P5EUsesSnapshots()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                FileFixture first = CreateFileFixture(directory, "DeleteFirst", "first.dll");
                FileFixture second = CreateFileFixture(directory, "DeleteSecond", "second.dll");
                ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                    first.Expected,
                    second.Expected);
                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    provenance,
                    new[] { first.Path, second.Path });

                File.Delete(first.Path);
                File.Delete(second.Path);

                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.Equal(
                    new[] { first.Expected.ModuleVersionId, second.Expected.ModuleVersionId },
                    referenceSet.References.Select(P5DTests.ReadMvid));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Keeps original snapshots when candidate files are replaced by later
        /// builds after P5F acquisition.
        /// </summary>
        [Fact]
        public void CandidateFilesReplacedAfterP5F_P5EUsesOriginalBuilds()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                FileFixture first = CreateFileFixture(directory, "ReplaceFirst", "first.dll");
                FileFixture second = CreateFileFixture(directory, "ReplaceSecond", "second.dll");
                ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                    first.Expected,
                    second.Expected);
                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    provenance,
                    new[] { first.Path, second.Path });
                File.WriteAllBytes(
                    first.Path,
                    EmitAssembly("ReplaceFirst", "public sealed class LaterFirstType { }"));
                File.WriteAllBytes(
                    second.Path,
                    EmitAssembly("ReplaceSecond", "public sealed class LaterSecondType { }"));

                Assert.True(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.Equal(
                    new[] { first.Expected.ModuleVersionId, second.Expected.ModuleVersionId },
                    referenceSet.References.Select(P5DTests.ReadMvid));
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Materializes a multi-module manifest through P5F while preserving
        /// P5D/P5E as the layer that rejects incomplete reconstruction.
        /// </summary>
        [Fact]
        public void MultiModuleManifest_P5FSucceedsAndP5EFailsClosed()
        {
            string directory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = P5DTests.EmitMultiModuleAssembly();
                ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                    image,
                    "MultiModuleAssembly.dll");
                string path = WriteImage(directory, "manifest.bin", image);
                ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                    expected);

                ExternalMetadataReferenceMaterialSet materialSet = CreateMaterialSet(
                    provenance,
                    new[] { path });

                Assert.Single(materialSet.Materials);
                Assert.False(ExternalMetadataReferenceSetFactory.TryCreate(
                    provenance,
                    materialSet.Materials,
                    out ExternalMetadataReferenceSet referenceSet));
                Assert.Null(referenceSet);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(directory);
            }
        }

        /// <summary>
        /// Accepts identical binary content from distinct explicit locations
        /// without treating path as identity.
        /// </summary>
        [Fact]
        public void SameBinaryAtDifferentPaths_PreservesBothLocations()
        {
            string firstDirectory = P5BTests.CreateTempDirectory();
            string secondDirectory = P5BTests.CreateTempDirectory();

            try
            {
                byte[] image = EmitAssembly("DifferentPaths", "public sealed class Type { }");
                ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                    image,
                    "DifferentPaths.dll");
                string firstPath = WriteImage(firstDirectory, "first.bin", image);
                string secondPath = WriteImage(secondDirectory, "second.bin", image);

                ExternalMetadataReferenceMaterialSet set = CreateMaterialSet(
                    P5ETests.CreateProvenance(expected, expected),
                    new[] { firstPath, secondPath });

                Assert.Equal(firstPath, set.Materials[0].Candidate.FilePath);
                Assert.Equal(secondPath, set.Materials[1].Candidate.FilePath);
                Assert.Equal(
                    set.Materials[0].Candidate.Module.ModuleVersionId,
                    set.Materials[1].Candidate.Module.ModuleVersionId);
            }
            finally
            {
                P5BTests.DeleteDirectoryIfExists(firstDirectory);
                P5BTests.DeleteDirectoryIfExists(secondDirectory);
            }
        }

        /// <summary>
        /// Rejects a null path element cleanly at its ordinal.
        /// </summary>
        [Fact]
        public void NullCandidatePathElement_FailsClosed()
        {
            DescriptorFixture fixture = CreateDescriptorFixture("NullPath");

            Assert.False(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                fixture.Provenance,
                new string[] { null! },
                out _));
        }

        /// <summary>
        /// Applies the project argument-null convention to both top-level
        /// inputs.
        /// </summary>
        [Fact]
        public void NullInputs_ThrowArgumentNullException()
        {
            ExternalCompilationProvenanceDescriptor provenance = P5ETests.CreateProvenance(
                Array.Empty<ExternalCompilationMetadataReferenceDescriptor>());

            Assert.Throws<ArgumentNullException>(
                () => ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    null!,
                    Array.Empty<string>(),
                    out _));
            Assert.Throws<ArgumentNullException>(
                () => ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    provenance,
                    null!,
                    out _));
        }

        /// <summary>
        /// Creates a successful material set through the production P5F
        /// factory.
        /// </summary>
        private static ExternalMetadataReferenceMaterialSet CreateMaterialSet(
            ExternalCompilationProvenanceDescriptor provenance,
            IReadOnlyList<string> candidatePaths)
        {
            Assert.True(ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                provenance,
                candidatePaths,
                out ExternalMetadataReferenceMaterialSet materialSet));
            return materialSet;
        }

        /// <summary>
        /// Creates expected descriptors without writing their images.
        /// </summary>
        private static DescriptorFixture CreateDescriptorFixture(params string[] names)
        {
            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> expected = names
                .Select(name => CreateExpected(
                    EmitAssembly(name, $"public sealed class {name}Type {{ }}"),
                    $"{name}.dll"))
                .ToImmutableArray();
            return new DescriptorFixture(P5ETests.CreateProvenance(expected), expected);
        }

        /// <summary>
        /// Emits and writes one controlled assembly candidate.
        /// </summary>
        private static FileFixture CreateFileFixture(
            string directory,
            string assemblyName,
            string fileName)
        {
            byte[] image = EmitAssembly(
                assemblyName,
                $"public sealed class {assemblyName}Type {{ }}");
            ExternalCompilationMetadataReferenceDescriptor expected = CreateExpected(
                image,
                $"{assemblyName}.dll");
            return new FileFixture(
                expected,
                WriteImage(directory, fileName, image));
        }

        /// <summary>
        /// Writes one test image to an explicit candidate path.
        /// </summary>
        private static string WriteImage(string directory, string fileName, byte[] image)
        {
            string path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, image);
            return path;
        }

        /// <summary>
        /// Creates one expected descriptor through the existing P5A/P5B test
        /// helper.
        /// </summary>
        private static ExternalCompilationMetadataReferenceDescriptor CreateExpected(
            byte[] image,
            string name,
            ImmutableArray<string> aliases = default,
            bool embedInteropTypes = false)
        {
            return P5ETests.CreateExpected(image, name, aliases, embedInteropTypes);
        }

        /// <summary>
        /// Emits a controlled assembly through the existing P5B-backed test
        /// helper.
        /// </summary>
        private static byte[] EmitAssembly(string assemblyName, string source)
        {
            return P5ETests.EmitAssembly(assemblyName, source);
        }

        /// <summary>
        /// Stores expected provenance without candidate files.
        /// </summary>
        private sealed record DescriptorFixture(
            ExternalCompilationProvenanceDescriptor Provenance,
            ImmutableArray<ExternalCompilationMetadataReferenceDescriptor> Expected);

        /// <summary>
        /// Stores one written candidate and its expected provenance.
        /// </summary>
        private sealed record FileFixture(
            ExternalCompilationMetadataReferenceDescriptor Expected,
            string Path);
    }
}
