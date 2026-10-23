using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>B25A stop-rule audit. These characterize unresolved defects, not successful production repair.</summary>
    public sealed class ExceptionFlowAssemblyQualifiedOwnershipTests
    {
        private const string Fault = "namespace Shared { public class Fault : System.Exception {} }";

        [Fact]
        public void OriginalNameOnlySubstitution_IsProven_ExactCandidateRepairsOnlyTheTopLevelCase()
        {
            var b = Compile("B", Fault);
            var a = Compile("A", Fault, Emit(b));
            var thrown = b.GetTypeByMetadataName("Shared.Fault")!;
            var root = a.Assembly.GetTypeByMetadataName("Shared.Fault")!;
            Assert.Equal(DocumentationCommentId.CreateDeclarationId(thrown), DocumentationCommentId.CreateDeclarationId(root));
            var before = (INamedTypeSymbol)typeof(ExceptionFlowSummaryGraphEvaluator)
                .GetMethod("NormalizeSummaryExceptionType", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [thrown, a])!;
            Assert.NotEqual(Identity(root), Identity(before)); // P11
            Assert.Equal(Identity(thrown), Identity(before));
            var candidate = Candidate(thrown, a);
            Assert.Equal(Identity(thrown), Identity(candidate));
            Evidence("root-cause", new
            {
                ActualThrownType = Identity(thrown),
                HistoricalWrongOwner = Identity(root),
                HistoricalWitness = "Evaluation/P5O2B25AProof/ (frozen pre-A10 evidence)",
                ActualNormalizedOwner = Identity(before),
                CandidateCorrectOwner = Identity(candidate),
                Resolution = "Unique complete canonical assembly/module/type identity",
                CandidateAdopted = true,
                SolutionFindingAdmission = false
            });
        }

        [Fact]
        public void ConstructedContainingType_CompleteIdentityButNoExactRebind_LeavesSameTypeUncaught()
        {
            const string source = "namespace Shared { public class Outer<T> { public class Fault : System.Exception {} } }";
            var b = Compile("B", source);
            var reference = Emit(b);
            var a = Compile("A", "", reference);
            var owner = (IAssemblySymbol)a.GetAssemblyOrModuleSymbol(reference)!;
            var thrown = b.GetTypeByMetadataName("Shared.Outer`1")!.Construct(b.GetSpecialType(SpecialType.System_Int32)).GetTypeMembers("Fault").Single();
            var caught = owner.GetTypeByMetadataName("Shared.Outer`1")!.Construct(a.GetSpecialType(SpecialType.System_Int32)).GetTypeMembers("Fault").Single();
            Assert.Equal(Identity(thrown), Identity(caught));
            Assert.NotEqual(Identity(thrown), Identity(thrown.OriginalDefinition));
            var rebound = new RoslynCanonicalIdentityResolver(a).ResolveType(Identity(thrown)); // P12
            Assert.NotNull(rebound);
            Assert.Equal(Identity(caught), Identity(rebound));
            Assert.True(SymbolEqualityComparer.Default.Equals(caught, rebound));
            var normalizedThrown = Candidate(thrown, a);
            var normalizedCaught = Candidate(caught, a);
            Assert.Equal(Identity(caught), Identity(normalizedThrown)); // R5
            Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedThrown));
            Assert.Equal(Identity(caught), Identity(normalizedCaught)); // K17
            Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedCaught));
            Assert.True(SymbolEqualityComparer.Default.Equals(normalizedThrown, normalizedCaught)); // P12
            Assert.True(ExceptionFlowCaughtTypeFilter.IsSameOrDerivedFrom(normalizedThrown, normalizedCaught));
            var alternative = ExceptionFlowCrossCompilationResolver.ResolveNamedType(thrown, a);
            Evidence("nested-rebind", new
            {
                SourceIdentity = Identity(thrown),
                MetadataCatchIdentity = Identity(caught),
                CompleteIdentitiesEqual = true,
                CanonicalRebindAvailable = true,
                CandidateRetainsActualSource = false,
                SameTypeCatchWorks = true,
                ExistingCrossCompilationAlternative = alternative == null ? null : Identity(alternative),
                Contract = "A9-FiniteOrbit-v1 exact nested construction rebinding",
                CandidateAdopted = true
            });
        }

        [Fact]
        public void GenericCatchComparison_ErasesDifferentConstruction_IndependentOfNameOnlyNormalizer()
        {
            var compilation = Compile("B", "namespace Shared { public class Fault<T> : System.Exception {} }");
            var definition = compilation.GetTypeByMetadataName("Shared.Fault`1")!;
            var thrown = definition.Construct(compilation.GetSpecialType(SpecialType.System_Int32));
            var caught = definition.Construct(compilation.GetSpecialType(SpecialType.System_String));
            Assert.NotEqual(Identity(thrown), Identity(caught));
            Assert.False(SymbolEqualityComparer.Default.Equals(thrown, caught));
            // P13: invariant generic classes must not match after erasing arguments.
            Assert.False(ExceptionFlowCaughtTypeFilter.IsSameOrDerivedFrom(thrown, caught));
            Evidence("generic-catch", new { Thrown = Identity(thrown), Caught = Identity(caught), IncorrectlySuppressed = false, ExactCatchObserved = true });
        }

        [Fact]
        public void SameCompleteAssemblyAndTypeIdentity_ExistingCanonicalResolverIsExact()
        {
            var original = Compile("Same", Fault);
            var root = Compile("Same", Fault);
            var identity = Identity(original.GetTypeByMetadataName("Shared.Fault")!);
            Assert.Same(root.GetTypeByMetadataName("Shared.Fault"), new RoslynCanonicalIdentityResolver(root).ResolveType(identity));
        }

        [Fact]
        public void StopRule_LeavesOriginalProductionAndClosedFindingAdmission()
        {
            string root = ExceptionFlowProductionCurrentParityTests.Root();
            string evaluator = File.ReadAllText(Path.Combine(root, "src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs"));
            Assert.DoesNotContain("GetFirstSymbolForDeclarationId", evaluator); // P14
            Assert.Contains("RoslynCanonicalIdentityResolver", evaluator);
            string external = File.ReadAllText(Path.Combine(root, "src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs"));
            Assert.Contains("Solution is not admitted", external);
            Assert.DoesNotContain("SupportingProjects", external);
        }

        [Theory]
        [InlineData("missing-assembly")]
        [InlineData("unknown-assembly")]
        [InlineData("version")]
        [InlineData("culture")]
        [InlineData("token")]
        [InlineData("module-owner")]
        [InlineData("module-name")]
        [InlineData("reference-owner")]
        [InlineData("metadata-name")]
        public void ExistingIdentityResolution_RejectsMissingForgedOrContradictoryFacts(string mutation)
        {
            var source = Compile("B", Fault);
            var root = Compile("A", Fault, Emit(source));
            var identity = Identity(source.GetTypeByMetadataName("Shared.Fault")!);
            var original = identity.Assembly!;
            CanonicalAssemblyIdentity? assembly = mutation == "missing-assembly" ? null : new(
                mutation == "unknown-assembly" ? "Unknown" : original.Name,
                mutation == "version" ? 9 : original.VersionMajor, original.VersionMinor, original.VersionBuild, original.VersionRevision,
                mutation == "culture" ? "de-DE" : original.CultureName,
                mutation == "token" ? "0102030405060708" : original.PublicKeyOrToken,
                original.HasPublicKey, original.IsRetargetable, original.ContentType);
            if (mutation == "reference-owner") { assembly = Identity(root.Assembly.GetTypeByMetadataName("Shared.Fault")!).Assembly; }
            var module = mutation is "module-owner" or "module-name"
                ? new CanonicalModuleIdentity(mutation == "module-owner" ? Identity(root.Assembly.GetTypeByMetadataName("Shared.Fault")!).Assembly! : original,
                    mutation == "module-name" ? "Wrong.dll" : identity.Module!.Name, identity.Module!.Ordinal, identity.Module.ModuleVersionId)
                : identity.Module;
            var invalid = new CanonicalTypeIdentity(identity.Kind, identity.NamedTypeKind, assembly, module, identity.NamespaceName,
                mutation == "metadata-name" ? "Absent" : identity.MetadataName, identity.ContainingType, identity.TypeArguments,
                identity.IsUnboundGenericType, identity.IsNativeIntegerType, null, 0, null, null, null, null, null, null, null, null);
            Assert.Null(new RoslynCanonicalIdentityResolver(root).ResolveType(invalid));
        }

        /// <summary>Test-only reproduction of the withdrawn candidate. Never used by production.</summary>
        private static INamedTypeSymbol Candidate(INamedTypeSymbol type, Compilation root) =>
            new RoslynCanonicalIdentityResolver(root).ResolveType(Identity(type)) as INamedTypeSymbol ?? type;
        private static CanonicalTypeIdentity Identity(ITypeSymbol type) => RoslynCanonicalIdentityFactory.CreateTypeIdentity(type);
        private static CSharpCompilation Compile(string name, string source, params MetadataReference[] references)
        {
            var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, path: "/" + name + ".cs")],
                MetadataReferences.Default.Concat(references), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            return compilation;
        }
        private static MetadataReference Emit(CSharpCompilation compilation)
        {
            using var image = new MemoryStream();
            var emitted = compilation.Emit(image);
            Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
            return MetadataReference.CreateFromImage(image.ToArray());
        }
        private static void Evidence(string name, object value)
        {
            string path = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/p5o2b25a10/analysis/a", name + ".json"); // R6
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
        }
    }
}
