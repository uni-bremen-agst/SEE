using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Current analyzer evidence alongside bounded, freshly emitted CLR controls; no new flow semantics.</summary>
public sealed class ExceptionFlowStaticInitializationTriggerContractTests(ITestOutputHelper output)
{
    private const string Docs = "/// <summary>Read.</summary>\n" +
        "/// <exception cref=\"System.TypeInitializationException\">Initialization failed.</exception>\n" +
        "/// <exception cref=\"System.InvalidOperationException\">Inner failure.</exception>\n";

    public static IEnumerable<object[]> TriggerCases()
    {
        foreach (bool explicitConstructor in new[] { false, true })
        foreach (string access in new[] { "field", "method", "property", "new", "transitive", "other-instance", "typeof" })
            yield return [explicitConstructor, access];
    }

    [Theory]
    [MemberData(nameof(TriggerCases))]
    public void TriggerMatrixSeparatesRuntimeInitializationFromTheCurrentEmptyConsumerProjection(bool explicitConstructor, string access)
    {
        string expression = access switch {
            "field" => "Target.Value", "method" => "Target.Method()", "property" => "Target.Property",
            "new" => "new Target().Instance()", "transitive" => "Relay()",
            "other-instance" => "new Other().ReadOther()", _ => "0"
        };
        string source = Target(explicitConstructor, true) + "public class Other { public int ReadOther() => Target.Value; }\n" +
            "public class Consumer {\n" + Docs + "public static int Read() " +
            (access == "typeof" ? "{ _ = typeof(Target); return 0; }" : "=> " + expression + ";") + "\n" +
            "static int Relay() => Target.Value; }";
        var fixture = Fixture(source);
        var run = fixture.Session();
        var root = fixture.Root("Read");
        var flow = run.Session.Analyze(root);
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        Assert.Empty(flow.UncertainTargets);
        var products = Detect(fixture, root);
        Assert.Equal(new[] { "DOC632", "DOC632" }, products.Findings.Select(f => f.Smell.ID));
        Assert.Equal("Complete", products.Coverage.Completion);
        Assert.Empty(products.Coverage.ReasonCodes);
        var runtime = Execute(fixture);
        Assert.Equal(!explicitConstructor, runtime.BeforeFieldInit);
        if (access == "typeof" && explicitConstructor) {
            Assert.All(runtime.Calls, c => Assert.Null(c.Outer));
            Assert.Equal(0, runtime.Attempts);
        }
        else if (access == "typeof") {
            // typeof is no forcing operation; relaxed initialization may nevertheless occur early.
            Assert.All(runtime.Calls, c => Assert.True(c.Outer is null or "System.TypeInitializationException"));
            Assert.InRange(runtime.Attempts!.Value, 0, 1);
        }
        else if (explicitConstructor || access is "field" or "transitive" or "other-instance") {
            Assert.All(runtime.Calls, c => Assert.Equal("System.TypeInitializationException", c.Outer));
            Assert.All(runtime.Calls, c => Assert.Equal("System.InvalidOperationException", c.Inner));
            Assert.Equal(1, runtime.Attempts);
        }
        else {
            // beforefieldinit permits early initialization, but does not require it at a method/new trigger.
            Assert.All(runtime.Calls, c => Assert.True(c.Outer is null or "System.TypeInitializationException"));
            Assert.InRange(runtime.Attempts!.Value, 0, 1);
        }
        var model = fixture.Compilation.GetSemanticModel(root.SyntaxTree);
        var operations = root.DescendantNodes().Select(n => model.GetOperation(n)).OfType<IFieldReferenceOperation>()
            .Select(o => new { o.Field.Name, o.Field.IsStatic, Owner = o.Field.ContainingType.ToDisplayString() }).Distinct().ToArray();
        Write("trigger-" + explicitConstructor + "-" + access, source, new {
            Flow = AdmissionCharacterization.Flow(flow), products, runtime, operations,
            InitializedTypeStateInput = "Unavailable in the existing analyzer seam; CLR executions use fresh collectible loads."
        });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SuccessfulInitializationRunsOnceAndExplicitBodyFailureIsWrapped(bool explicitConstructor, bool bodyThrows)
    {
        string source = Target(explicitConstructor, false, bodyThrows) +
            "public class Consumer {\n" + Docs + "public static int Read() => Target.Value; }";
        var fixture = Fixture(source);
        var run = fixture.Session();
        var flow = run.Session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var runtime = Execute(fixture);
        Assert.Equal(1, runtime.Attempts);
        Assert.All(runtime.Calls, c => Assert.Equal(bodyThrows ? "System.TypeInitializationException" : null, c.Outer));
        Assert.All(runtime.Calls, c => Assert.Equal(bodyThrows ? "System.InvalidOperationException" : null, c.Inner));
        if (!bodyThrows) Assert.All(runtime.Calls, c => Assert.Equal(7, c.Value));
        Write("success-or-body-" + explicitConstructor + "-" + bodyThrows, source,
            new { Flow = AdmissionCharacterization.Flow(flow), Products = Detect(fixture, fixture.Root("Read")), runtime });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfigurationParseExampleExposesFormatExceptionOnlyAsInnerException(bool explicitConstructor)
    {
        string source = "public static class Configuration { public static readonly int Value = int.Parse(\"abc\"); " +
            (explicitConstructor ? "static Configuration() {}" : "") + "}\npublic class Consumer {\n" + Docs +
            "public static int Read() => Configuration.Value; }";
        var fixture = Fixture(source);
        var run = fixture.Session();
        var flow = run.Session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var runtime = Execute(fixture, "Configuration");
        Assert.All(runtime.Calls, c => Assert.Equal("System.TypeInitializationException", c.Outer));
        Assert.All(runtime.Calls, c => Assert.Equal("System.FormatException", c.Inner));
        Write("configuration-parse-" + explicitConstructor, source,
            new { Flow = AdmissionCharacterization.Flow(flow), Products = Detect(fixture, fixture.Root("Read")), runtime });
    }

    [Theory]
    [InlineData("System.InvalidOperationException", "System.TypeInitializationException")]
    [InlineData("System.TypeInitializationException", null)]
    [InlineData("System.Exception", null)]
    public void CatchMatchingUsesTheOuterTypeAndDoesNotUnwrapAutomatically(string caught, string? outer)
    {
        string source = Target(true, true) + "public class Consumer {\n" + Docs +
            "public static int Read() { try { return Target.Value; } catch (" + caught + ") { return 9; } } }";
        var fixture = Fixture(source);
        var flow = fixture.Session().Session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var runtime = Execute(fixture);
        Assert.All(runtime.Calls, c => Assert.Equal(outer, c.Outer));
        if (outer is null) Assert.All(runtime.Calls, c => Assert.Equal(9, c.Value));
        Assert.Equal(1, runtime.Attempts);
        Write("catch-" + caught, source,
            new { Flow = AdmissionCharacterization.Flow(flow), Products = Detect(fixture, fixture.Root("Read")), runtime });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ProducerAcquisitionKeepsSynthesizedConstructorFieldInitializerAndExplicitBodyDistinct(bool explicitConstructor, bool bodyThrows)
    {
        string source = Target(explicitConstructor, !bodyThrows, bodyThrows) + "public class Consumer { public static int Read() => Target.Value; }";
        var fixture = Fixture(source);
        var run = fixture.Session();
        var type = fixture.Compilation.GetTypeByMetadataName("Target")!;
        var constructor = Assert.Single(type.StaticConstructors);
        var fragment = new ExceptionFlowSummaryFragment();
        bool hasBody = ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(constructor, run.Environment,
            new AdmissionObservationRecorder(), fragment, new ExceptionFlowCallContext(constructor));
        Assert.Equal(!explicitConstructor, constructor.IsImplicitlyDeclared);
        Assert.Equal(explicitConstructor, hasBody);
        Assert.Empty(fragment.CallEdges);
        Assert.Equal(bodyThrows ? 1 : 0, fragment.Sources.Count);
        var initializer = fixture.Compilation.SyntaxTrees.Single().GetRoot().DescendantNodes()
            .OfType<VariableDeclaratorSyntax>().Single(v => v.Identifier.ValueText == "Value").Initializer!;
        Assert.IsAssignableFrom<IFieldInitializerOperation>(fixture.Compilation.GetSemanticModel(initializer.SyntaxTree).GetOperation(initializer));
        var constructorRoot = constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ConstructorDeclarationSyntax;
        var constructorFlow = constructorRoot is null ? null : run.Session.Analyze(constructorRoot);
        if (constructorFlow is not null) {
            Assert.True(constructorFlow.ProfileOutcome!.Complete);
            Assert.Equal(bodyThrows ? 1 : 0, constructorFlow.ThrownExceptions.Count);
        }
        Write("producer-" + explicitConstructor + "-" + bodyThrows, source, new {
            constructor.IsImplicitlyDeclared, MethodKind = constructor.MethodKind.ToString(),
            Declarations = constructor.DeclaringSyntaxReferences.Select(r => r.GetSyntax().Kind().ToString()).ToArray(),
            hasBody, Fragment = JsonSerializer.Deserialize<JsonElement>(AdmissionCharacterization.Fragment(fragment)),
            InitializerOperationAvailable = true,
            ConstructorFlow = constructorFlow is null ? null : AdmissionCharacterization.Flow(constructorFlow)
        });
    }

    [Fact]
    public void IndependentTypeQueryDoesNotSupplyMissingTriggerEvidenceOrChangeCallableFindings()
    {
        string source = Target(true, true).Replace("public class Target", "/// <summary>Target.</summary>\npublic class Target", StringComparison.Ordinal) +
            "public class Consumer {\n" + Docs + "public static int Read() => Target.Value;\n" +
            "/// <summary>Independent.</summary>\npublic static void Independent() {} }";
        var fixture = Fixture(source);
        var run = fixture.Session();
        var root = fixture.Root("Read");
        string before = JsonSerializer.Serialize(AdmissionCharacterization.Flow(run.Session.Analyze(root)));
        var type = fixture.Compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(c => c.Identifier.ValueText == "Target");
        var typeFlow = run.Session.Analyze(type);
        Assert.False(typeFlow.ProfileOutcome!.Complete);
        Assert.Equal(before, JsonSerializer.Serialize(AdmissionCharacterization.Flow(run.Session.Analyze(root))));
        var reverse = fixture.Session();
        reverse.Session.Analyze(type);
        Assert.Equal(before, JsonSerializer.Serialize(AdmissionCharacterization.Flow(reverse.Session.Analyze(root))));
        var independent = run.Session.Analyze(fixture.Root("Independent"));
        Assert.True(independent.ProfileOutcome!.Complete);
        Assert.Empty(independent.ThrownExceptions);
        var products = Detect(fixture, root);
        Assert.Equal("Incomplete", products.Coverage.Completion);
        Assert.Equal(new[] { "OutsideProfile", "TargetCoverageIncomplete" }, products.Coverage.ReasonCodes);
        Assert.Equal(2, products.Findings.Count(f => f.Smell.ID == "DOC632"));
        Write("type-independent-order", source, new {
            Flow = AdmissionCharacterization.Flow(run.Session.Analyze(root)), TypeFlow = AdmissionCharacterization.Flow(typeFlow),
            Independent = AdmissionCharacterization.Flow(independent), products,
            DiagnosticScope = "Entire fixture; separate local flow objects are explicitly named."
        });
    }

    [Fact]
    public void ClosedGenericTypesHaveSeparateRuntimeInitializationStatesButNoCurrentStaticTriggerEdges()
    {
        const string source = """
            public static class State { public static int IntAttempts; public static int StringAttempts; }
            public class Target<T> {
                public static int Value;
                static Target() {
                    if (typeof(T) == typeof(int)) { State.IntAttempts++; Value = 7; }
                    else { State.StringAttempts++; throw new System.InvalidOperationException(); }
                }
            }
            public static class Consumer {
                /// <summary>Read two separate closed states.</summary>
                /// <exception cref="System.TypeInitializationException">String initialization fails.</exception>
                public static int Read() { _ = Target<int>.Value; return Target<string>.Value; }
            }
            public static class Probe { public static int Attempts() => State.IntAttempts * 10 + State.StringAttempts; }
            """;
        var fixture = Fixture(source);
        var flow = fixture.Session().Session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var runtime = Execute(fixture, "Target`1");
        Assert.Equal(11, runtime.Attempts);
        Assert.All(runtime.Calls, c => Assert.Equal("System.TypeInitializationException", c.Outer));
        Write("generic-separated", source, new { Flow = AdmissionCharacterization.Flow(flow), Products = Detect(fixture, fixture.Root("Read")), runtime });
    }

    [Fact]
    public void DerivedStaticAccessDoesNotAutomaticallyInitializeTheBaseButConstructionReachesItsConstructor()
    {
        const string source = """
            public static class State { public static int Count; }
            public class Base { static Base() { State.Count++; throw new System.InvalidOperationException(); } public Base() {} }
            public class Target : Base { static Target() {} public Target() {} public static int Method() => 7; }
            public static class Consumer {
                /// <summary>Read.</summary>
                public static int Read() => Target.Method();
                public static int Construct() { _ = new Target(); return 0; }
            }
            public static class Probe { public static int Attempts() => State.Count; }
            """;
        var fixture = Fixture(source);
        var runtime = Execute(fixture);
        Assert.Equal(0, runtime.Attempts);
        Assert.All(runtime.Calls, c => Assert.Null(c.Outer));
        var construction = Execute(fixture, methodName: "Construct");
        Assert.Equal(1, construction.Attempts);
        Assert.All(construction.Calls, c => Assert.Equal("System.TypeInitializationException", c.Outer));
        var run = fixture.Session();
        Assert.Empty(run.Session.Analyze(fixture.Root("Read")).ThrownExceptions);
        Assert.Empty(run.Session.Analyze(fixture.Root("Construct")).ThrownExceptions);
        Write("base-derived", source, new {
            RuntimeStatic = runtime, RuntimeConstruction = construction,
            StaticFlow = AdmissionCharacterization.Flow(run.Session.Analyze(fixture.Root("Read"))),
            ConstructorFlow = AdmissionCharacterization.Flow(run.Session.Analyze(fixture.Root("Construct")))
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MetadataFieldBindingDoesNotAcquireOrFlagTheUnavailableInitializer(bool callMethod)
    {
        string target = Target(true, true);
        var dependency = Fixture(target, "C3C.Dependency");
        using var image = new MemoryStream();
        Assert.True(dependency.Compilation.Emit(image).Success);
        string source = "public class Consumer {\n" + Docs + "public static int Read() => Target." + (callMethod ? "Method()" : "Value") + "; }";
        var fixture = new AdmissionCharacterization.Fixture([("/C3C/Consumer.cs", source)],
            MetadataReferences.Default.Concat([MetadataReference.CreateFromImage(image.ToArray())]), assemblyName: "C3C.MetadataConsumer");
        var flow = fixture.Session().Session.Analyze(fixture.Root("Read"));
        Assert.Equal(!callMethod, flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var products = Detect(fixture, fixture.Root("Read"));
        Assert.Equal(2, products.Findings.Length);
        Assert.All(products.Findings, f => Assert.Equal(callMethod ? "DOC631" : "DOC632", f.Smell.ID));
        Assert.Equal(callMethod ? "Incomplete" : "Complete", products.Coverage.Completion);
        Write("metadata-" + callMethod, source, new { DependencySource = target, Flow = AdmissionCharacterization.Flow(flow), products });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AvailableSupportingSourceRepairsMethodBodyAvailabilityButDoesNotAddInitializationTriggers(bool callMethod)
    {
        string target = Target(true, true);
        var dependency = Fixture(target, "C3C.Supporting");
        string source = "public class Consumer {\n" + Docs + "public static int Read() => Target." + (callMethod ? "Method()" : "Value") + "; }";
        var fixture = new AdmissionCharacterization.Fixture([("/C3C/Consumer.cs", source)],
            MetadataReferences.Default.Concat([dependency.Compilation.ToMetadataReference()]), assemblyName: "C3C.SourceConsumer");
        var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(fixture.Root("Read").SyntaxTree, fixture.Compilation);
        context.RegisterSupportingSource(dependency.Compilation);
        var environment = new ExceptionFlowSemanticEnvironment(context);
        var session = new ExceptionFlowSummaryAnalysisSession(environment);
        var flow = session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var coverage = new AnalysisCoverage();
        coverage.Begin("C3C-supporting", ExceptionAnalysisMode.SolutionTransitive);
        var root = fixture.Root("Read");
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
            fixture.Compilation.GetSemanticModel(root.SyntaxTree), context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, session, coverage);
        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal("DOC632", f.Smell.ID));
        Assert.Equal("Complete", coverage.Report().Completion);
        Write("supporting-" + callMethod, source, new {
            DependencySource = target, Flow = AdmissionCharacterization.Flow(flow), Findings = findings, Coverage = coverage.Report(),
            EvidenceScope = "Current compilation-reference and registered supporting-source control, no historical runtime claim."
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitStaticConstructorBodyDoesNotReachTaggedOrUntaggedConsumer(bool tagged)
    {
        string source = Target(true, false, true) + "public class Consumer {\n" +
            (tagged ? Docs : "/// <summary>Read.</summary>\n") + "public static int Read() => Target.Method(); }";
        var fixture = Fixture(source);
        var flow = fixture.Session().Session.Analyze(fixture.Root("Read"));
        Assert.True(flow.ProfileOutcome!.Complete);
        Assert.Empty(flow.ThrownExceptions);
        var products = Detect(fixture, fixture.Root("Read"));
        Assert.Equal(tagged ? 2 : 0, products.Findings.Length);
        Assert.All(products.Findings, f => Assert.Equal("DOC632", f.Smell.ID));
        Assert.Equal("Complete", products.Coverage.Completion);
        var runtime = Execute(fixture);
        Assert.All(runtime.Calls, c => Assert.Equal("System.TypeInitializationException", c.Outer));
        Assert.All(runtime.Calls, c => Assert.Equal("System.InvalidOperationException", c.Inner));
        Write("explicit-body-consumer-" + tagged, source, new { Flow = AdmissionCharacterization.Flow(flow), products, runtime });
    }

    private static string Target(bool explicitConstructor, bool initializerThrows, bool bodyThrows = false)
        => "public static class State { public static int Count; }\npublic class Target {\n" +
        "public static int Value = Initialize();\nstatic int Initialize() { State.Count++; " +
        (initializerThrows ? "throw new System.InvalidOperationException();" : "return 7;") + " }\n" +
        (explicitConstructor ? "static Target() { " + (bodyThrows ? "throw new System.InvalidOperationException();" : "") + " }\n" : "") +
        "public static int Method() => 7; public static int Property => 7; public Target() {} public int Instance() => 7; }\n" +
        "public static class Probe { public static int Attempts() => State.Count; }\n";

    private static AdmissionCharacterization.Fixture Fixture(string source, string assembly = "C3C.Contract")
        => new([("/C3C/Fixture.cs", source)], assemblyName: assembly);

    private static DetectorProducts Detect(AdmissionCharacterization.Fixture fixture, MemberDeclarationSyntax root)
    {
        var run = fixture.Session();
        var coverage = new AnalysisCoverage();
        coverage.Begin("C3C-fixture", ExceptionAnalysisMode.SolutionTransitive);
        AnalysisCoverageObserver.ObserveCompilation(coverage, fixture.Compilation, run.Session);
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(root.SyntaxTree, root.SyntaxTree.FilePath,
            fixture.Compilation.GetSemanticModel(root.SyntaxTree), run.Context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, run.Session, coverage);
        return new(findings.ToArray(), coverage.Report());
    }

    private static RuntimeCapture Execute(AdmissionCharacterization.Fixture fixture, string targetName = "Target", string methodName = "Read")
    {
        using var image = new MemoryStream();
        var emitted = fixture.Compilation.Emit(image);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        image.Position = 0;
        var load = new AssemblyLoadContext("C3C bounded runtime control", isCollectible: true);
        try {
            var assembly = load.LoadFromStream(image);
            // Public emitted fixture entrypoints, invoked as delegates: no private analyzer reflection or Invoke wrapper.
            var read = assembly.GetType("Consumer", throwOnError: true)!.GetMethod(methodName)!.CreateDelegate<Func<int>>();
            var attempts = assembly.GetType("Probe")?.GetMethod("Attempts")?.CreateDelegate<Func<int>>();
            var calls = new List<RuntimeCall>();
            for (int i = 0; i < 2; i++) {
                try { calls.Add(new(read(), null, null)); }
                catch (Exception error) { calls.Add(new(null, error.GetType().FullName, error.InnerException?.GetType().FullName)); }
            }
            return new(RuntimeInformation.FrameworkDescription, RuntimeInformation.ProcessArchitecture.ToString(),
                (assembly.GetType(targetName, throwOnError: true)!.Attributes & TypeAttributes.BeforeFieldInit) != 0,
                attempts?.Invoke(), calls.ToArray());
        }
        finally { load.Unload(); }
    }

    private void Write(string scenario, string source, object evidence)
        => output.WriteLine("C3C_CAPTURE " + JsonSerializer.Serialize(new { Scenario = scenario, Source = source, Evidence = evidence }));

    private sealed record RuntimeCall(int? Value, string? Outer, string? Inner);
    private sealed record DetectorProducts(Finding[] Findings, AnalysisCoverageReport Coverage);
    private sealed record RuntimeCapture(string Framework, string Architecture, bool BeforeFieldInit, int? Attempts, RuntimeCall[] Calls);
}
