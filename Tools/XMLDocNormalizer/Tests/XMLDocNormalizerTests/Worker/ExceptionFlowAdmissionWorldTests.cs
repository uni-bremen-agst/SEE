using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Characterizes effective scope/digest inputs without introducing a world key (K9/K12).</summary>
public sealed class ExceptionFlowAdmissionWorldTests
{
    private const string Source = "class Entry { public static void A() {} public static void B() {} }";

    [Theory]
    [InlineData("text")]
    [InlineData("path")]
    [InlineData("define")]
    [InlineData("language")]
    [InlineData("documentation")]
    [InlineData("nullable")]
    [InlineData("overflow")]
    [InlineData("optimization")]
    [InlineData("unsafe")]
    [InlineData("platform")]
    [InlineData("aliases")]
    public void RelevantAnalysisViewInputsChangeScopeDigest(string change)
    {
        var baseline = new AdmissionCharacterization.Fixture([("/P2B/World.cs", Source)]);
        var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);
        var references = MetadataReferences.Default.ToArray();
        switch (change)
        {
            case "define": parse = parse.WithPreprocessorSymbols("FEATURE"); break;
            case "language": parse = parse.WithLanguageVersion(LanguageVersion.CSharp11); break;
            case "documentation": parse = parse.WithDocumentationMode(DocumentationMode.Diagnose); break;
            case "nullable": options = options.WithNullableContextOptions(NullableContextOptions.Disable); break;
            case "overflow": options = options.WithOverflowChecks(true); break;
            case "optimization": options = options.WithOptimizationLevel(OptimizationLevel.Release); break;
            case "unsafe": options = options.WithAllowUnsafe(true); break;
            case "platform": options = options.WithPlatform(Platform.X64); break;
            case "aliases": references[0] = references[0].WithAliases(["global", "Extra"]); break;
        }
        var different = new AdmissionCharacterization.Fixture([
            (change == "path" ? "/P2B/Other.cs" : "/P2B/World.cs", Source + (change == "text" ? " // different" : ""))
        ], references, parse, options);
        var first = baseline.Session();
        var second = different.Session();
        string scope = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(first.Environment);
        Assert.Equal(scope, first.Session.Analyze(baseline.Root("A")).ProfileOutcome!.ScopeDigest);
        Assert.Equal(scope, first.Session.Analyze(baseline.Root("B")).ProfileOutcome!.ScopeDigest);
        Assert.NotEqual(scope, ExceptionFlowFiniteOrbitAdmission.ScopeDigest(second.Environment));
        Assert.NotEqual(first.Session.Analyze(baseline.Root("A")).ProfileOutcome!.InventoryDigest,
            second.Session.Analyze(different.Root("A")).ProfileOutcome!.InventoryDigest);
    }

    [Fact]
    public void NewCompilationCanHaveTheSameDigestWithoutSharingSymbolOwnership()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/World.cs", Source)]);
        var clone = fixture.Compilation.Clone();
        Assert.NotSame(fixture.Compilation, clone);
        var first = fixture.Session();
        var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(fixture.Root("A").SyntaxTree, clone);
        var second = new ExceptionFlowSemanticEnvironment(context);
        Assert.Equal(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(first.Environment), ExceptionFlowFiniteOrbitAdmission.ScopeDigest(second));
        Assert.True(first.Environment.TryGetSemanticModel(fixture.Root("A").SyntaxTree, out var originalModel));
        Assert.True(second.TryGetSemanticModel(fixture.Root("A").SyntaxTree, out var cloneModel));
        Assert.Same(fixture.Compilation, originalModel.Compilation);
        Assert.Same(clone, cloneModel.Compilation);
        Assert.NotSame(originalModel, cloneModel);
        Assert.False(SymbolEqualityComparer.Default.Equals(fixture.Symbol("A"), clone
            .GetSemanticModel(fixture.Root("A").SyntaxTree).GetDeclaredSymbol(fixture.Root("A"))));
    }

    [Fact]
    public void SourceKindIsPartOfScopeDigest()
    {
        var regular = new AdmissionCharacterization.Fixture([("/P2B/Kind.cs", Source)]);
        var scriptTree = CSharpSyntaxTree.ParseText(Source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12).WithKind(SourceCodeKind.Script),
            "/P2B/Kind.cs");
        var script = CSharpCompilation.CreateScriptCompilation("P2B.Characterization", scriptTree,
            MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        Assert.Empty(script.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var scriptEnvironment = new ExceptionFlowSemanticEnvironment(
            ProjectClosureSemanticContext.CreateSingleCompilationContext(scriptTree, script));
        Assert.NotEqual(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(regular.Session().Environment),
            ExceptionFlowFiniteOrbitAdmission.ScopeDigest(scriptEnvironment));
    }

    [Fact]
    public void SameAssemblyIdentityWithDifferentHeldModuleImagesChangesScopeDigest()
    {
        PortableExecutableReference Reference(string member)
        {
            var dependency = new AdmissionCharacterization.Fixture([("/P2B/Api.cs",
                "public class Api { public static void " + member + "() {} }")], assemblyName: "P2B.SameOwner");
            using var image = new MemoryStream();
            Assert.True(dependency.Compilation.Emit(image).Success);
            return MetadataReference.CreateFromImage(image.ToArray());
        }
        var firstReference = Reference("First");
        var secondReference = Reference("Second");
        var first = new AdmissionCharacterization.Fixture([("/P2B/World.cs", Source)],
            MetadataReferences.Default.Append(firstReference));
        var second = new AdmissionCharacterization.Fixture([("/P2B/World.cs", Source)],
            MetadataReferences.Default.Append(secondReference));
        Assert.Equal(((IAssemblySymbol)first.Compilation.GetAssemblyOrModuleSymbol(firstReference)!).Identity,
            ((IAssemblySymbol)second.Compilation.GetAssemblyOrModuleSymbol(secondReference)!).Identity);
        Assert.NotEqual(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(first.Session().Environment),
            ExceptionFlowFiniteOrbitAdmission.ScopeDigest(second.Session().Environment));
    }

    [Fact]
    public void MissingTreeOwnershipCanBecomeAvailableAfterSupportingRegistration()
    {
        var owner = new AdmissionCharacterization.Fixture([("/P2B/Owner.cs", Source)]);
        var support = new AdmissionCharacterization.Fixture([("/P2B/Support.cs", "class Other { public static void Target() {} }")],
            assemblyName: "P2B.Support");
        var run = owner.Session();
        var root = support.Root("Target");
        Assert.False(run.Environment.TryGetSemanticModel(root.SyntaxTree, out _));
        var missing = run.Session.Analyze(root);
        Assert.False(missing.ProfileOutcome!.Complete);
        Assert.Contains(missing.ProfileOutcome.Reasons, reason => reason.Category == "OwnerBindingFailure");
        string before = ExceptionFlowFiniteOrbitAdmission.ScopeDigest(run.Environment);
        run.Context.RegisterSupportingSource(support.Compilation);
        Assert.True(run.Environment.TryGetSemanticModel(root.SyntaxTree, out var model));
        Assert.Same(support.Compilation, model.Compilation);
        var available = run.Session.Analyze(root);
        Assert.True(available.ProfileOutcome!.Complete);
        Assert.NotEqual(before, available.ProfileOutcome.ScopeDigest);
        Assert.DoesNotContain(available.ProfileOutcome.Reasons, reason => reason.Category == "OwnerBindingFailure");
    }
}
