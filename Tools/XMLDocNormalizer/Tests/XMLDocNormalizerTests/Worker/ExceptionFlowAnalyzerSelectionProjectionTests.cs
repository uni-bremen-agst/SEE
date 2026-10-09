using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Execution.Analysis.ExceptionFlowAnalyzerSelectionContext.Projector;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Exercises typed projection with actual existing validator outputs, without executing analysis or a Worker.</summary>
    public sealed class ExceptionFlowAnalyzerSelectionProjectionTests
    {
        [Fact]
        public void CurrentCompilation_ProjectsActualLoadedIdentityWithoutAnalyzingSource()
        {
            CSharpCompilation compilation = CurrentCompilation();
            Assert.Contains(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            ExceptionFlowAnalyzerSelectionProjectionResult projection = Project(new(CurrentCompilation: compilation));
            AssertContext(projection, ExceptionFlowCompilerProvenance.CurrentCompilation, CurrentVersion());
            Assert.Equal(ExceptionFlowAnalyzerSelection.Current, Select(projection));
            AssertNoHistoricalLoaded();
        }

        [Fact]
        public void RealValidatedCurrentPdb_ProjectsAndSelectsCurrent()
        {
            ExternalCompilationProvenanceDescriptor descriptor = CurrentPdb();
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.IsValidated(descriptor));
            Assert.Equal(PortablePdbValidationKind.IdentityAndChecksum, descriptor.PortablePdb.ValidationKind);
            ExceptionFlowAnalyzerSelectionProjectionResult projection = Project(new(PortablePdbProvenance: descriptor));
            AssertContext(projection, ExceptionFlowCompilerProvenance.ValidatedPortablePdb, CurrentVersion());
            Assert.Equal(ExceptionFlowAnalyzerSelection.Current, Select(projection));
        }

        [Fact]
        public async Task RealValidatedHistoricalPdb_ProjectsAndSelectsHistoricalWithoutLoadingOrExecutingIt()
        {
            Guid before = typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId;
            string imagePath = HistoricalImagePath();
            byte[] pe = File.ReadAllBytes(imagePath);
            byte[] pdb = File.ReadAllBytes(Path.ChangeExtension(imagePath, ".pdb"));
            ExternalCompilationProvenanceDescriptor historical = ReadRealProvenance(pe, pdb);
            Assert.Equal(PortablePdbValidationKind.IdentityAndChecksum, historical.PortablePdb.ValidationKind);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.IsValidated(historical));
            ExceptionFlowAnalyzerSelectionProjectionResult historicalProjection = Project(new(PortablePdbProvenance: historical));
            AssertContext(historicalProjection, ExceptionFlowCompilerProvenance.ValidatedPortablePdb,
                ExceptionFlowAnalyzerSelectionPolicy.HistoricalCompilerVersion);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, Select(historicalProjection));
            Assert.True(historical.CompilationOptions!.TryGetValue("compiler-version", out string recorded));
            Assert.Equal(recorded, historicalProjection.Context!.CompilerVersion);
            AssertNoHistoricalLoaded();
            Assert.Equal(before, typeof(CSharpCompilation).Assembly.ManifestModule.ModuleVersionId);
            ExceptionFlowAnalyzerSelectionProjectionResult current = Project(new(CurrentCompilation: CurrentCompilation()));
            ExceptionFlowAnalyzerSelectionProjectionResult currentPdb = Project(new(PortablePdbProvenance: CurrentPdb()));
            string evidence = Path.Combine(Root(), "artifacts", "p5o2b7", "projection-boundary.json");
            Directory.CreateDirectory(Path.GetDirectoryName(evidence)!);
            await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new
            {
                CurrentCompilation = current.Context,
                CurrentPdb = currentPdb.Context,
                HistoricalPdb = historicalProjection.Context,
                CurrentDecision = Select(current).ToString(),
                CurrentPdbDecision = Select(currentPdb).ToString(),
                HistoricalDecision = Select(historicalProjection).ToString(),
                HistoricalImagePath = imagePath,
                HistoricalPeSha256 = Convert.ToHexString(SHA256.HashData(pe)),
                HistoricalPdbSha256 = Convert.ToHexString(SHA256.HashData(pdb)),
                HistoricalPdbId = historical.PortablePdb.Id.Guid,
                HistoricalValidationKind = historical.PortablePdb.ValidationKind.ToString(),
                ExistingFactoryReceiptVerified = true,
                CurrentRuntimeInformationalVersion = CurrentVersion(),
                CurrentRuntimeMvid = before,
                ProjectorSelects = false,
                ProjectorExecutes = false,
                ProjectorStartsWorker = false,
                PolicyExecutes = false,
                HistoricalLoadedInCaller = false
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        [Fact]
        public void AnalyzerBuildPdb_IsNotTheAnalyzerRuntimeAndIsNeverRelabelledAsHistorical()
        {
            string path = Path.Combine(Root(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.ExceptionFlow.Historical", "bin",
                new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "net8.0", "XMLDocNormalizer.ExceptionFlow.Historical.dll");
            ExternalCompilationProvenanceDescriptor descriptor = ReadRealProvenance(File.ReadAllBytes(path), File.ReadAllBytes(Path.ChangeExtension(path, ".pdb")));
            Assert.True(descriptor.CompilationOptions!.TryGetValue("compiler-version", out string recorded));
            ExceptionFlowAnalyzerSelectionProjectionResult projection = Project(new(PortablePdbProvenance: descriptor));
            AssertContext(projection, ExceptionFlowCompilerProvenance.ValidatedPortablePdb, recorded);
            Assert.NotEqual(ExceptionFlowAnalyzerSelectionPolicy.HistoricalCompilerVersion, recorded);
            Assert.NotEqual(ExceptionFlowAnalyzerSelectionPolicy.CurrentCompilerVersion, recorded);
            Assert.Equal(ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion,
                ExceptionFlowAnalyzerSelectionPolicy.Select(projection.Context).Failure!.Code);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void MissingSource_IsTypedFailure(int scenario)
            => AssertFailure(Project(scenario == 0 ? null : scenario == 1 ? new() : new(CurrentCompilation: null)),
                ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingInput);

        [Theory]
        [InlineData("current")]
        [InlineData("historical")]
        [InlineData("unknown")]
        public void RawVersionWrappedInUnvalidatedDescriptor_DoesNotBecomeTrusted(string scenario)
        {
            ExternalCompilationProvenanceDescriptor valid = CurrentPdb();
            string version = scenario switch
            {
                "current" => CurrentVersion(),
                "historical" => ExceptionFlowAnalyzerSelectionPolicy.HistoricalCompilerVersion,
                _ => "unknown"
            };
            ExternalCompilationOptionsDescriptor rawOptions = new([new("language", "C#"), new("version", "2"), new("compiler-version", version)]);
            ExternalCompilationProvenanceDescriptor raw = new(valid.DebugDirectory, valid.PortablePdb, rawOptions, valid.MetadataReferences);
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.IsValidated(raw));
            AssertFailure(Project(new(PortablePdbProvenance: raw)), ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnvalidatedProvenance);
        }

        [Fact]
        public void IdenticalDescriptorCopy_DoesNotInheritReceipt()
        {
            ExternalCompilationProvenanceDescriptor valid = CurrentPdb();
            ExternalCompilationProvenanceDescriptor copy = new(valid.DebugDirectory, valid.PortablePdb, valid.CompilationOptions, valid.MetadataReferences);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.IsValidated(valid));
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.IsValidated(copy));
            AssertFailure(Project(new(PortablePdbProvenance: copy)), ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnvalidatedProvenance);
        }

        [Fact]
        public void ExistingB6ConvenienceEntry_CannotBypassTrustBoundary()
        {
            ExternalCompilationProvenanceDescriptor valid = CurrentPdb();
            ExternalCompilationProvenanceDescriptor copy = new(valid.DebugDirectory, valid.PortablePdb, valid.CompilationOptions, valid.MetadataReferences);
            ExceptionFlowAnalyzerSelectionDecision decision = ExceptionFlowAnalyzerSelectionPolicy.Select(
                ExceptionFlowAnalyzerSelectionContext.FromValidatedPortablePdb(copy));
            Assert.False(decision.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Unspecified, decision.Selection);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MismatchedOrCorruptPdb_IsRejectedByExistingValidator(bool corrupt)
        {
            var expected = EmitCurrent("ExpectedProjectionPdb");
            byte[] candidate = corrupt ? (byte[])expected.PdbImage.Clone()
                : ExternalCompilationProvenanceDescriptorFactoryTests.EmitPortablePdb("OtherProjectionPdb", "public class DifferentSource { public int Value; }",
                    new CSharpParseOptions(LanguageVersion.CSharp12), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).PdbImage;
            if (corrupt) { candidate[^1] ^= 1; }
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.TryCreate(expected.DebugDescriptor, [.. candidate], out ExternalCompilationProvenanceDescriptor descriptor));
            Assert.False(ExternalCompilationProvenanceDescriptorFactory.IsValidated(descriptor));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TwoSources_AreNeverSilentlyPreferredEvenIfVersionsAgree(bool historical)
        {
            ExternalCompilationProvenanceDescriptor pdb = historical
                ? ReadRealProvenance(File.ReadAllBytes(HistoricalImagePath()), File.ReadAllBytes(Path.ChangeExtension(HistoricalImagePath(), ".pdb")))
                : CurrentPdb();
            AssertFailure(Project(new(CurrentCompilation(), pdb)), ExceptionFlowAnalyzerSelectionProjectionFailureCode.ConflictingInputs);
        }

        [Fact]
        public void ValidatedPdbWithoutOptions_IsTypedMissingInformation()
            => AssertFailure(Project(new(PortablePdbProvenance: SyntheticValidatedPdb(null))),
                ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingCompilationOptions);

        [Theory]
        [InlineData("missing-language")]
        [InlineData("wrong-language")]
        [InlineData("missing-schema")]
        [InlineData("wrong-schema")]
        public void ValidatedUnsupportedOptions_FailProjectionWithoutDefaults(string scenario)
        {
            List<string> entries = [];
            if (scenario != "missing-language") { entries.AddRange(["language", scenario == "wrong-language" ? "Visual Basic" : "C#"]); }
            if (scenario != "missing-schema") { entries.AddRange(["version", scenario == "wrong-schema" ? "3" : "2"]); }
            entries.AddRange(["compiler-version", CurrentVersion()]);
            AssertFailure(Project(new(PortablePdbProvenance: SyntheticValidatedPdb([.. entries]))),
                ExceptionFlowAnalyzerSelectionProjectionFailureCode.UnsupportedCompilationOptions);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        public void ValidatedMissingCompilerVersion_IsTypedFailure(string? version)
        {
            string[] entries = version == null ? ["language", "C#", "version", "2"]
                : ["language", "C#", "version", "2", "compiler-version", version];
            AssertFailure(Project(new(PortablePdbProvenance: SyntheticValidatedPdb(entries))),
                ExceptionFlowAnalyzerSelectionProjectionFailureCode.MissingCompilerVersion);
        }

        [Theory]
        [InlineData("unknown")]
        [InlineData("5.0.0.0")]
        [InlineData("5.0.0-2.25567.12+6c4a46a31302167b425d5e0a31ea83c9a9aa1d09 ")]
        public void ValidatedUnsupportedVersion_ProjectsUnchangedAndOnlyB6RejectsIt(string version)
        {
            ExceptionFlowAnalyzerSelectionProjectionResult projection = Project(new(PortablePdbProvenance:
                SyntheticValidatedPdb(["language", "C#", "version", "2", "compiler-version", version])));
            AssertContext(projection, ExceptionFlowCompilerProvenance.ValidatedPortablePdb, version);
            ExceptionFlowAnalyzerSelectionDecision decision = ExceptionFlowAnalyzerSelectionPolicy.Select(projection.Context);
            Assert.False(decision.Succeeded);
            Assert.Equal(ExceptionFlowAnalyzerSelection.Unspecified, decision.Selection);
            Assert.Equal(ExceptionFlowAnalyzerSelectionFailureCode.UnsupportedCompilerVersion, decision.Failure!.Code);
        }

        [Fact]
        public void ContextCannotBeRawConstructedOrChangedAndProjectorAcceptsNoRawMetadata()
        {
            Assert.Empty(typeof(ExceptionFlowAnalyzerSelectionContext).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
            Assert.All(typeof(ExceptionFlowAnalyzerSelectionContext).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic), constructor => Assert.True(constructor.IsPrivate));
            Assert.Null(typeof(ExceptionFlowAnalyzerSelectionContext).GetProperty(nameof(ExceptionFlowAnalyzerSelectionContext.CompilerVersion))!.SetMethod);
            Assert.Null(typeof(ExceptionFlowAnalyzerSelectionContext).GetProperty(nameof(ExceptionFlowAnalyzerSelectionContext.Provenance))!.SetMethod);
            Assert.All(typeof(ExceptionFlowAnalyzerSelectionProjectionInput).GetProperties(), property => Assert.True(
                property.PropertyType == typeof(CSharpCompilation) || property.PropertyType == typeof(ExternalCompilationProvenanceDescriptor)));
            Assert.Single(typeof(ExceptionFlowAnalyzerSelectionContext.Projector).GetMethods(BindingFlags.Static | BindingFlags.NonPublic), method => method.Name == "Project");
        }

        [Fact]
        public void Architecture_ProjectorNeverSelectsRoutesExecutesLoadsOrParses()
        {
            string source = File.ReadAllText(Path.Combine(Root(), "src", "XMLDocNormalizer", "Execution", "Analysis", "ExceptionFlowAnalyzerSelectionContext.Projector.cs"));
            foreach (string forbidden in new[]
            {
                "ExceptionFlowAnalyzerSelectionPolicy", "ExceptionFlowAnalysisRouter", "HistoricalWorkerClient", "Process.Start", "ProcessStartInfo",
                "Assembly.Load", "File.", "Directory.", "MetadataReader", "PEReader", "CSharpCompilation.Create", "GetDiagnostics", "GetSemanticModel", "Analyze(",
                "StartsWith", "Trim(", "Version.Parse", "5.0.0-", "ExceptionFlowAnalyzerSelection.Current", "ExceptionFlowAnalyzerSelection.Historical"
            }) { Assert.DoesNotContain(forbidden, source); }
            Assert.Empty(typeof(ExceptionFlowAnalyzerSelectionContext.Projector).GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzerSelectionContext.Projector).Assembly.GetReferencedAssemblies(), reference =>
                reference.Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker");
        }

        [Fact]
        public void RepeatedProjection_PreservesSourceAndNeverMutatesValidatedOptions()
        {
            ExternalCompilationProvenanceDescriptor descriptor = CurrentPdb();
            var original = descriptor.CompilationOptions!.Options;
            var first = Project(new(PortablePdbProvenance: descriptor));
            Parallel.For(0, 100, _ => Assert.Equal(first, Project(new(PortablePdbProvenance: descriptor))));
            Assert.Equal(original, descriptor.CompilationOptions.Options);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.IsValidated(descriptor));
        }

        private static void AssertContext(ExceptionFlowAnalyzerSelectionProjectionResult result, ExceptionFlowCompilerProvenance origin, string version)
        {
            Assert.True(result.Succeeded, result.Failure?.Message);
            Assert.Null(result.Failure);
            Assert.Equal(origin, result.Context!.Provenance);
            Assert.Equal(version, result.Context.CompilerVersion);
        }
        private static void AssertFailure(ExceptionFlowAnalyzerSelectionProjectionResult result, ExceptionFlowAnalyzerSelectionProjectionFailureCode code)
        {
            Assert.False(result.Succeeded);
            Assert.Null(result.Context);
            Assert.Equal(code, result.Failure!.Code);
        }
        private static ExceptionFlowAnalyzerSelection Select(ExceptionFlowAnalyzerSelectionProjectionResult projection)
        {
            Assert.True(projection.Succeeded);
            ExceptionFlowAnalyzerSelectionDecision decision = ExceptionFlowAnalyzerSelectionPolicy.Select(projection.Context);
            Assert.True(decision.Succeeded, decision.Failure?.Message);
            return decision.Selection;
        }
        private static CSharpCompilation CurrentCompilation() => CSharpCompilation.Create("ProjectionCurrent",
            [CSharpSyntaxTree.ParseText("public class Fixture { void Broken() { Missing(); } }")], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        private static string CurrentVersion() => typeof(CSharpCompilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        private static ExternalCompilationProvenanceDescriptorFactoryTests.PortablePdbTestData EmitCurrent(string name)
            => ExternalCompilationProvenanceDescriptorFactoryTests.EmitPortablePdb(name, "public class Fixture { }",
                new CSharpParseOptions(LanguageVersion.CSharp12), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        private static ExternalCompilationProvenanceDescriptor CurrentPdb()
            => ExternalCompilationProvenanceDescriptorFactoryTests.ReadRequiredDescriptor(EmitCurrent("ProjectionCurrentPdb"));

        private static ExternalCompilationProvenanceDescriptor ReadRealProvenance(byte[] pe, byte[] pdb)
        {
            PortableExecutableReference reference = MetadataReference.CreateFromImage(ImmutableArray.CreateRange(pe));
            CSharpCompilation consumer = CSharpCompilation.Create("ProjectionMetadataConsumer", references: MetadataReferences.Default.Append(reference),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            IAssemblySymbol assembly = Assert.IsAssignableFrom<IAssemblySymbol>(consumer.GetAssemblyOrModuleSymbol(reference));
            Assert.True(ExternalAssemblyReferenceDescriptorFactory.TryCreate(consumer, assembly, out ExternalAssemblyReferenceDescriptor expected));
            using MemoryStream peStream = new(pe, writable: false);
            Assert.True(ExternalPeDebugDirectoryDescriptorFactory.TryCreate(expected, peStream, out ExternalPeDebugDirectoryDescriptor debug));
            using MemoryStream pdbStream = new(pdb, writable: false);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(debug, pdbStream, out ExternalCompilationProvenanceDescriptor descriptor));
            return descriptor;
        }
        private static ExternalCompilationProvenanceDescriptor SyntheticValidatedPdb(string[]? entries)
        {
            // Schema-unit fixture: trusted synthetic expected debug root; real Historical coverage above uses its actual PE/PDB checksum pair.
            MetadataBuilder metadata = new();
            int[] counts = new int[MetadataTokens.TableCount];
            counts[(int)TableIndex.Module] = 1;
            if (entries != null)
            {
                byte[] blob = Encoding.UTF8.GetBytes(string.Join("\0", entries) + "\0");
                metadata.AddCustomDebugInformation(MetadataTokens.EntityHandle(TableIndex.Module, 1),
                    metadata.GetOrAddGuid(new Guid("b5feec05-8cd0-4a83-96da-466284bb4bd8")), metadata.GetOrAddBlob(blob));
            }
            PortablePdbBuilder builder = new(metadata, [.. counts], entryPoint: default);
            BlobBuilder image = new();
            builder.Serialize(image);
            byte[] bytes = image.ToArray();
            using MetadataReaderProvider reader = MetadataReaderProvider.FromPortablePdbImage([.. bytes]);
            BlobContentId id = new(reader.GetMetadataReader().DebugMetadataHeader!.Id);
            ExternalPeDebugDirectoryDescriptor expected = new(new ExternalModuleIdentity("projection-schema-fixture.dll", Guid.Empty), false, [], [id], []);
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.TryCreate(expected, [.. bytes], out ExternalCompilationProvenanceDescriptor descriptor));
            Assert.True(ExternalCompilationProvenanceDescriptorFactory.IsValidated(descriptor));
            return descriptor;
        }
        private static string HistoricalImagePath() => Path.Combine(Root(), "artifacts", "p5o2b7", "historical-pdb", "historical-input.dll");
        private static void AssertNoHistoricalLoaded() => Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
            assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker"
            || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");
        private static string Root()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) { return directory.FullName; }
            }
            throw new InvalidOperationException("Cannot locate tool root.");
        }
    }
}
