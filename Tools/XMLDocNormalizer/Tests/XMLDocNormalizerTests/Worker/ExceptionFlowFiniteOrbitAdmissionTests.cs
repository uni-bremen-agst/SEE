using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>A10 production admission, separate from the diagnostic raw graph.</summary>
public sealed class ExceptionFlowFiniteOrbitAdmissionTests
{
    [Fact]
    public void Diagnosis_ExactConstructedDestinationDoesNotImplyReferenceInterning()
    {
        const string declaration = "namespace Shared { public class Outer<T> { public class Fault : System.Exception {} } }";
        var source = CSharpCompilation.Create("B", [CSharpSyntaxTree.ParseText(declaration)], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        Assert.True(source.Emit(image).Success);
        var reference = MetadataReference.CreateFromImage(image.ToArray());
        var destination = CSharpCompilation.Create("A", references: MetadataReferences.Default.Concat([reference]),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var owner = (IAssemblySymbol)destination.GetAssemblyOrModuleSymbol(reference)!;
        var thrown = source.GetTypeByMetadataName("Shared.Outer`1")!.Construct(source.GetSpecialType(SpecialType.System_Int32)).GetTypeMembers("Fault").Single();
        var caught = owner.GetTypeByMetadataName("Shared.Outer`1")!.Construct(destination.GetSpecialType(SpecialType.System_Int32)).GetTypeMembers("Fault").Single();
        var candidate = typeof(ExceptionFlowAssemblyQualifiedOwnershipTests).GetMethod("Candidate", BindingFlags.Static | BindingFlags.NonPublic)!;
        var normalizedThrown = (INamedTypeSymbol)candidate.Invoke(null, [thrown, destination])!;
        var normalizedCaught = (INamedTypeSymbol)candidate.Invoke(null, [caught, destination])!;
        var expected = RoslynCanonicalIdentityFactory.CreateTypeIdentity(caught);
        var rebound = new RoslynCanonicalIdentityResolver(destination).ResolveType(RoslynCanonicalIdentityFactory.CreateTypeIdentity(thrown));
        Assert.NotNull(rebound);
        Assert.Equal(expected, RoslynCanonicalIdentityFactory.CreateTypeIdentity(rebound));
        Assert.Equal(expected, RoslynCanonicalIdentityFactory.CreateTypeIdentity(normalizedThrown));
        Assert.Equal(expected, RoslynCanonicalIdentityFactory.CreateTypeIdentity(normalizedCaught));
        Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedThrown));
        Assert.True(SymbolEqualityComparer.Default.Equals(caught, normalizedCaught));
        DiagnosticEvidence("canonical-object-identity", new
        {
            Expected = expected, Source = RoslynCanonicalIdentityFactory.CreateTypeIdentity(thrown),
            Rebound = RoslynCanonicalIdentityFactory.CreateTypeIdentity(rebound),
            NormalizedThrown = RoslynCanonicalIdentityFactory.CreateTypeIdentity(normalizedThrown),
            NormalizedCaught = RoslynCanonicalIdentityFactory.CreateTypeIdentity(normalizedCaught),
            ReboundSameInstanceAsCaught = ReferenceEquals(caught, rebound),
            NormalizedThrownSameInstanceAsCaught = ReferenceEquals(caught, normalizedThrown),
            NormalizedCaughtSameInstanceAsCaught = ReferenceEquals(caught, normalizedCaught),
            BothDestinationSymbolsEqual = SymbolEqualityComparer.Default.Equals(normalizedThrown, normalizedCaught),
            ExistingCandidateHelperUnchanged = true, NoInterningOrCacheWorkaround = true
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Diagnosis_ClassifiedFailurePreservesActualValidatedIdentity(bool containing)
    {
        string api = containing
            ? "public static class Api<T> { public static void Recur() { if (Flags.Stop) throw new Fault<T>(); Api<System.Collections.Generic.List<T>>.Recur(); } }"
            : "public static class Api { public static void Recur<T>() { if (Flags.Stop) throw new Fault<T>(); Recur<System.Collections.Generic.List<T>>(); } }";
        string call = containing ? "Api<int>.Recur();" : "Api.Recur<int>();";
        string source = "public class Fault<T> : System.Exception {} public static class Flags { public static bool Stop; } "
            + api + " public static class Entry { public static void Root() { " + call + " } }";
        var input = new WorkerAnalysisInput(source, "Entry", "Root", new());
        string worker = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(),
            "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin", new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name,
            "net8.0/XMLDocNormalizer.HistoricalWorker.dll");
        ProcessStartInfo start = new("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(worker);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new WorkerRequest(WorkerProtocol.Version, "analyze", input), WorkerProtocol.JsonOptions));
            process.StandardInput.Close();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            string responseText = await stdout; string diagnostics = await stderr;
            var response = JsonSerializer.Deserialize<WorkerResponse>(responseText, WorkerProtocol.JsonOptions)!;
            Assert.Equal(1, process.ExitCode);
            Assert.False(response.Success);
            Assert.Null(response.Result);
            Assert.Equal(WorkerFailureCode.UnsupportedContext, response.Failure!.Code);
            Assert.NotNull(response.Identity);
            Assert.All(response.Identity.LoadedRoslyn, image => Assert.StartsWith("5.0.0-2.25451.107+", image.InformationalVersion));
            var validated = HistoricalWorkerResponseValidation.Validate(responseText, diagnostics, process.ExitCode, input, process.Id);
            Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, validated.Failure!.Code);
            Assert.Null(validated.Result);
            Assert.NotNull(validated.Identity);
            Assert.Equal(JsonSerializer.Serialize(response.Identity, WorkerProtocol.JsonOptions),
                JsonSerializer.Serialize(validated.Identity, WorkerProtocol.JsonOptions));
            Assert.Null(validated.Provenance);
            Assert.Null(validated.DeclaredPolicy);
            Assert.Null(validated.DeclaredDocumentation);
            Assert.False(HistoricalWorkerResponseValidation.TryGetFindingFacts(validated, out _));
            Assert.False(HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(validated, out _));
            WorkerIdentity[] invalidIdentities = [
                response.Identity with { WorkerVersion = "forged" },
                response.Identity with { ReferenceProfile = "forged" },
                response.Identity with { Worker = response.Identity.Worker with { Name = "WrongWorker" } },
                response.Identity with { Analyzer = response.Identity.Analyzer with { Sha256 = "invalid" } },
                response.Identity with { LoadedRoslyn = [response.Identity.LoadedRoslyn[0] with { InformationalVersion = "wrong" }, response.Identity.LoadedRoslyn[1]] }
            ];
            foreach (var invalid in invalidIdentities)
            {
                var rejected = HistoricalWorkerResponseValidation.Validate(
                    JsonSerializer.Serialize(response with { Identity = invalid }, WorkerProtocol.JsonOptions), diagnostics, process.ExitCode, input, process.Id);
                Assert.False(rejected.Success);
                Assert.Null(rejected.Result);
                Assert.Null(rejected.Identity);
                Assert.Equal(HistoricalWorkerClientFailureCode.IdentityMismatch, rejected.Failure!.Code);
            }
            var missingIdentity = HistoricalWorkerResponseValidation.Validate(
                JsonSerializer.Serialize(response with { Identity = null }, WorkerProtocol.JsonOptions), diagnostics, process.ExitCode, input, process.Id);
            Assert.False(missingIdentity.Success);
            Assert.Null(missingIdentity.Result);
            Assert.Null(missingIdentity.Identity);
            Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, missingIdentity.Failure!.Code);
            var current = WorkerSourceOutcome(source);
            Assert.Equal(current.ProfileOutcome!.Details.Order(StringComparer.Ordinal), response.Failure.Details.Order(StringComparer.Ordinal));
            DiagnosticEvidence("failure-identity-" + containing, new
            {
                Containing = containing, Source = source, Current = current.ProfileOutcome,
                RawResponse = response, ValidatedFailure = validated.Failure,
                ValidatedIdentity = validated.Identity,
                ActualValidatedIdentityPreserved = true,
                InvalidIdentityCasesRejected = invalidIdentities.Length,
                MissingIdentityRemainsFailClosed = !missingIdentity.Success && missingIdentity.Identity == null,
                NoSuccessfulResult = !validated.Success && validated.Result == null,
                OwnedProcessId = process.Id, OwnedProcessExited = process.HasExited,
                DiagnosticOnly = true, NotAnAlternativeProductionEntry = true
            });
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
        }
    }

    private static void DiagnosticEvidence(string name, object value)
    {
        string directory = Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/p5o2b25a10/diagnostics");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Production_GrowingRecurrenceIsRejectedBeforeRootMaterialization(bool containing)
    {
        string api = containing
            ? "class Api<T> { public static void M() { Api<System.Collections.Generic.List<T>>.M(); } }"
            : "class Api { public static void M<T>() { M<System.Collections.Generic.List<T>>(); } }";
        var run = Create(api + " class Entry { public static void Root() { "
            + (containing ? "Api<int>.M();" : "Api.M<int>();") + " } }");
        var result = run.Session.Analyze(run.Root);
        Assert.False(result.ProfileOutcome!.Complete);
        Assert.Contains(result.ProfileOutcome.Reasons, reason => reason.Category == "PositiveConstructorCycle");
        Assert.Empty(result.ThrownExceptions);
        Assert.Equal(0, Graph(run.Session).Count);
        Assert.True(result.ProfileOutcome.NormalFindingAdmissionBlocked);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Production_FiniteConstructionsPreserveSubstitutionAndExactCatch(bool containing)
    {
        string api = containing
            ? "class Api<T> { public static void M() { throw new Fault<T>(); } }"
            : "class Api { public static void M<T>() { throw new Fault<T>(); } }";
        string call = containing ? "Api<int>.M();" : "Api.M<int>();";
        string other = containing ? "Api<string>.M();" : "Api.M<string>();";
        var run = Create("class Fault<T> : System.Exception {} " + api
            + " class Entry { public static void Root() { try { " + call
            + " } catch (Fault<int>) {} " + other + " } }");
        var result = run.Session.Analyze(run.Root);
        Assert.True(result.ProfileOutcome!.Complete, string.Join("\n", result.ProfileOutcome.Details));
        var thrown = Assert.Single(result.ThrownExceptions);
        Assert.Equal("Fault", thrown.Name);
        Assert.Equal(SpecialType.System_String, Assert.Single(thrown.TypeArguments).SpecialType);
        Assert.False(result.HasUncertainPaths);
        int count = Graph(run.Session).Count;
        var again = run.Session.Analyze(run.Root);
        Assert.True(again.ProfileOutcome!.Complete);
        Assert.Equal(count, Graph(run.Session).Count);
    }

    [Fact]
    public void NeutralSlotCheck_AcceptsResetAndPermutationButRejectsPositiveReturnCycle()
    {
        Body[] bodies = [new("A", ["T", "U"], [])];
        Term[] seed = [Term.Named("int"), Term.Named("string")];
        var swap = ExceptionFlowFiniteOrbitProfile.Check(bodies, [new("A", "A", [Term.Var("U"), Term.Var("T")], "swap")], "A", seed, "scope", []);
        Assert.True(swap.Complete);
        var reset = ExceptionFlowFiniteOrbitProfile.Check(bodies, [new("A", "A", [Term.Named("List", Term.Var("U")), Term.Named("int")], "reset")], "A", seed, "scope", []);
        Assert.True(reset.Complete);
        var growing = ExceptionFlowFiniteOrbitProfile.Check(bodies, [new("A", "A", [Term.Named("List", Term.Var("U")), Term.Var("T")], "growth")], "A", seed, "scope", []);
        Assert.False(growing.Complete);
        Assert.Contains(growing.Reasons, reason => reason.Category == "PositiveConstructorCycle");
        Assert.Contains("return=", Assert.Single(growing.Reasons).Witness);
    }

    [Fact]
    public void ProductionSeal_RejectsAnUndiscoveredSuccessorBeforeGetOrAdd()
    {
        var run = Create("class Entry { public static void Root() {} public static void Other<T>() {} }");
        var environment = new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(run.Root.SyntaxTree, run.Compilation));
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(run.Root, environment);
        Assert.True(admission.Outcome.Complete);
        var root = run.Compilation.GetSemanticModel(run.Root.SyntaxTree).GetDeclaredSymbol(run.Root)!;
        var other = run.Compilation.GetTypeByMetadataName("Entry")!.GetMembers("Other").OfType<IMethodSymbol>().Single()
            .Construct(run.Compilation.GetSpecialType(SpecialType.System_Int32));
        var graph = new ExceptionFlowSummaryGraph();
        graph.BeginProduction(admission.Seal, environment);
        var context = new ExceptionFlowCallContext(root);
        var key = new ExceptionFlowCallableKey(root, context);
        graph.GetOrAdd(key, context);
        graph.BuildingKey = key;
        graph.BuildingContext = context;
        graph.BuildingCompilation = run.Compilation;
        var failure = Assert.Throws<ExceptionFlowProfileViolation>(() => graph.RegisterTarget(other, new(other)));
        Assert.Equal("TargetOrAnalysisCoverageIncomplete", failure.Reason.Category);
        Assert.Equal(1, graph.Count);
        graph.EndProduction(false);
        Assert.Equal(0, graph.Count);
    }

    [Fact]
    public void Production_ConflictingMetadataModuleWorldsAreRejectedBeforeRegistration()
    {
        MetadataReference Emit(string source)
        {
            var compilation = CSharpCompilation.Create("Same.Owner", [CSharpSyntaxTree.ParseText(source)], MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream).Success);
            return MetadataReference.CreateFromImage(stream.ToArray());
        }
        var run = Create("class Entry { public static void Root() {} }", Emit("public class One {}"));
        var otherTree = CSharpSyntaxTree.ParseText("class Other {}", path: "/A10/Other.cs");
        var other = CSharpCompilation.Create("Other.Scope", [otherTree], MetadataReferences.Default.Concat([Emit("public class Two {}")]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(run.Root.SyntaxTree, run.Compilation);
        context.RegisterSupportingSource(other);
        var session = new ExceptionFlowSummaryAnalysisSession(new ExceptionFlowSemanticEnvironment(context));
        var result = session.Analyze(run.Root);
        Assert.False(result.ProfileOutcome!.Complete);
        Assert.Contains(result.ProfileOutcome.Reasons, reason => reason.Category == "OwnerBindingFailure"
            && reason.Witness.StartsWith("Conflicting metadata module worlds:", StringComparison.Ordinal));
        Assert.Equal(0, Graph(session).Count);
    }

    [Fact]
    public async Task Production_WorkspaceProjectReferenceHasExactSourceCoverage()
    {
        var (_, project, document) = SolutionTestBuilder.CreateTwoProjectSolution(
            "public class Entry { public void M() { ExternalHelper.ThrowingCall(); } }",
            "public static class ExternalHelper { public static void ThrowingCall() { throw new System.InvalidOperationException(); } }");
        var context = ProjectClosureSemanticContextBuilder.Build([project], XMLDocNormalizer.Models.ExceptionAnalysisMode.SolutionTransitive);
        var compilation = (await project.GetCompilationAsync())!;
        var packagingErrors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.NotEmpty(packagingErrors);
        Assert.All(packagingErrors, item => Assert.Equal("CS5001", item.Id));
        var tree = (await document.GetSyntaxTreeAsync())!;
        var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var session = new ExceptionFlowSummaryAnalysisSession(new ExceptionFlowSemanticEnvironment(context));
        var result = session.Analyze(root);
        Assert.True(result.ProfileOutcome!.Complete, string.Join("\n", result.ProfileOutcome.Details));
        Assert.Equal("InvalidOperationException", Assert.Single(result.ThrownExceptions).Name);
    }

    [Fact]
    public void Production_SupportingSourceTransfersCallerScopedTemplateThenConcreteInvocation()
    {
        var dependencyTree = CSharpSyntaxTree.ParseText("public class Fault<T> : System.Exception {} public static class Api { public static void M<T>() { throw new Fault<T>(); } }", path: "/A10/Dependency.cs");
        var dependency = CSharpCompilation.Create("A10.Dependency", [dependencyTree], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        Assert.True(dependency.Emit(image).Success);
        var reference = MetadataReference.CreateFromImage(image.ToArray());
        var run = Create("class Entry { public static void Root() { Middle<string>(); } static void Middle<T>() { Api.M<T>(); } }", reference);
        var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(run.Root.SyntaxTree, run.Compilation);
        context.RegisterSupportingSource(dependency);
        var session = new ExceptionFlowSummaryAnalysisSession(new ExceptionFlowSemanticEnvironment(context));
        var result = session.Analyze(run.Root);
        Assert.True(result.ProfileOutcome!.Complete, string.Join("\n", result.ProfileOutcome.Details));
        var thrown = Assert.Single(result.ThrownExceptions);
        Assert.Equal("A10.Dependency", thrown.ContainingAssembly.Name);
        Assert.Equal(SpecialType.System_String, Assert.Single(thrown.TypeArguments).SpecialType);
        Assert.True(SymbolEqualityComparer.Default.Equals(thrown.ContainingAssembly,
            run.Compilation.GetAssemblyOrModuleSymbol(reference)));
    }

    [Theory]
    [InlineData("public static void M<T>(this T value) { throw new Fault<T>(); }", "1.M();")]
    [InlineData("public static void M<T,U>(this T value, U other) { throw new Fault<U>(); }", "1.M(2);")]
    public void Production_ReducedExtensionPreservesInferredCoordinates(string declaration, string call)
    {
        var run = Create("class Fault<T> : System.Exception {} static class Api { " + declaration
            + " } class Entry { public static void Root() { " + call + " } }");
        var result = run.Session.Analyze(run.Root);
        Assert.True(result.ProfileOutcome!.Complete, string.Join("\n", result.ProfileOutcome.Details));
        Assert.Equal(SpecialType.System_Int32, Assert.Single(Assert.Single(result.ThrownExceptions).TypeArguments).SpecialType);
    }

    [Fact]
    public void Production_OpenRootIsOutsideButRawDiagnosticEntryRemainsAvailable()
    {
        var run = Create("class Entry { public static void Root<T>() { Root<T>(); } }");
        Assert.False(run.Session.Analyze(run.Root).ProfileOutcome!.Complete);
        Assert.Equal(0, Graph(run.Session).Count);
        Assert.True(run.Session.TryBuildTransitiveSummaryGraph(run.Root, out var raw, out var key));
        Assert.NotNull(key);
        Assert.Equal(1, raw.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Substitution_ReducedAndStaticHandlesHaveTheSameFullConstructedIdentity(bool generic)
    {
        string source = generic
            ? "class Holder<T> { public class Nested {} } static class Api { public static void Visit<T>(this Holder<T>.Nested value, T extra) {} } class Entry { public static void Root() { new Holder<int>.Nested().Visit(1); } }"
            : "class Holder { public class Nested {} } static class Api { public static void Visit(this Holder.Nested value) {} } class Entry { public static void Root() { new Holder.Nested().Visit(); } }";
        var run = Create(source);
        var call = Assert.Single(run.Root.DescendantNodes().OfType<InvocationExpressionSyntax>());
        var selected = Assert.IsAssignableFrom<IMethodSymbol>(run.Compilation.GetSemanticModel(run.Root.SyntaxTree).GetSymbolInfo(call).Symbol);
        Assert.NotNull(selected.ReducedFrom);
        var expected = ExceptionFlowAnalyzer.GetInvocationAnalysisTarget(selected);
        var substitution = new ExceptionFlowInvocationSubstitution(null, run.Compilation);
        var actual = Assert.IsAssignableFrom<IMethodSymbol>(substitution.ApplyCallable(selected));
        Assert.Null(actual.ReducedFrom);
        Assert.Equal(RoslynCanonicalIdentityFactory.CreateCallableIdentity(expected, false),
            RoslynCanonicalIdentityFactory.CreateCallableIdentity(actual, false));
        Assert.True(SymbolEqualityComparer.Default.Equals(expected, actual));
        Assert.Equal(RoslynCanonicalIdentityFactory.CreateCallableIdentity(actual, false),
            RoslynCanonicalIdentityFactory.CreateCallableIdentity(substitution.ApplyCallable(expected), false));
        if (generic) Assert.Equal(SpecialType.System_Int32, Assert.Single(actual.TypeArguments).SpecialType);
    }

    [Fact]
    public void Production_IncompleteRecoveryRootIsClassifiedBeforeCanonicalizationOrRegistration()
    {
        var tree = CSharpSyntaxTree.ParseText("class", path: "/A10/incomplete.cs");
        var compilation = CSharpCompilation.Create("A10.Invalid", [tree], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var member = Assert.Single(tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>());
        Assert.True(member.Identifier.IsMissing);
        var session = new ExceptionFlowSummaryAnalysisSession(new(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation)));
        var result = session.Analyze(member);
        Assert.False(result.ProfileOutcome!.Complete);
        Assert.Contains(result.ProfileOutcome.Reasons, reason => reason.Category == "RepresentationOutsideGrammar");
        Assert.Contains("/A10/incomplete.cs", result.ProfileOutcome.Root, StringComparison.Ordinal);
        Assert.Empty(result.ThrownExceptions);
        Assert.True(result.HasUncertainPaths);
        Assert.Equal(0, Graph(session).Count);
    }

    [Fact]
    public void ProductionSeal_ChangedScopeCannotRegisterEvenAnAlreadySealedRoot()
    {
        var run = Create("class Entry { public static void Root() {} }");
        var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(run.Root.SyntaxTree, run.Compilation);
        var environment = new ExceptionFlowSemanticEnvironment(context);
        var admission = ExceptionFlowFiniteOrbitAdmission.Discover(run.Root, environment);
        var graph = new ExceptionFlowSummaryGraph();
        graph.BeginProduction(admission.Seal, environment);
        context.RegisterSupportingSource(CSharpCompilation.Create("New.Scope", [CSharpSyntaxTree.ParseText("class NewType {}")],
            MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)));
        var root = run.Compilation.GetSemanticModel(run.Root.SyntaxTree).GetDeclaredSymbol(run.Root)!;
        var failure = Assert.Throws<ExceptionFlowProfileViolation>(() => graph.GetOrAdd(new(root, new ExceptionFlowCallContext(root))));
        Assert.Equal("ScopeMapNotClosed", failure.Reason.Category);
        Assert.Equal(0, graph.Count);
        graph.EndProduction(false);
    }

    internal static (ExceptionFlowSummaryAnalysisSession Session, MethodDeclarationSyntax Root, CSharpCompilation Compilation) Create(string source, params MetadataReference[] references)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: "/A10/Entry.cs");
        var compilation = CSharpCompilation.Create("A10.FiniteOrbit", [tree], MetadataReferences.Default.Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "Root");
        var environment = new ExceptionFlowSemanticEnvironment(ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
        return (new(environment), root, compilation);
    }

    internal static ExceptionFlowSummaryGraph Graph(ExceptionFlowSummaryAnalysisSession session)
        => (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession).GetField("graph",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(session)!;

    /// <summary>Observes the real Current production outcome for R2 without changing the older finite-result helper.</summary>
    internal static XMLDocNormalizer.Models.DTO.ExceptionFlowAnalysisResult WorkerSourceOutcome(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
        string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        Assert.Equal(7, references.Length);
        var compilation = CSharpCompilation.Create("HistoricalWorkerInput", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.Empty(compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error));
        var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
        var session = new ExceptionFlowSummaryAnalysisSession(new ExceptionFlowSemanticEnvironment(
            ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation)));
        return session.Analyze(root);
    }
}
