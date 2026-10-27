using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Cli.Output;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Json;
using XMLDocNormalizer.Reporting.Sarif;
using XMLDocNormalizerTests.Helpers;
using XMLDocNormalizerTests.Worker;

namespace XMLDocNormalizerTests.Execution;

/// <summary>Product completion is independent of findings and requires actual analysis evidence.</summary>
[Collection("Console-dependent tests")]
public sealed class CoverageCompletionTests
{
    public static IEnumerable<object[]> ModesAndCases()
        => from mode in Enum.GetValues<ExceptionAnalysisMode>()
           from scenario in new[] { "empty", "throw", "missing", "diagnostic" }
           select new object[] { (int)mode, scenario };

    [Theory]
    [MemberData(nameof(ModesAndCases))]
    public async Task NormalCli_AllFourModes_ReportsIndependentCompletion(int modeValue, string scenario)
    {
        ExceptionAnalysisMode mode = (ExceptionAnalysisMode)modeValue;
        string root = ExceptionFlowProductionCurrentParityTests.Root();
        string folder = Path.Combine(root, "artifacts/coverage/cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string project = Path.Combine(folder, "Sample.csproj"), report = Path.Combine(folder, "report.json");
        await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup></Project>");
        string tag = scenario == "diagnostic" ? "/// <exception cref=\"Fault\">Possible failure.</exception>\n" : "";
        string source = scenario == "empty" ? "// empty selected scope" : scenario == "throw"
            ? "public class Fault : System.Exception {} public static class Fixture {\n/// <summary>Throws.</summary>\npublic static void Root() { throw new Fault(); } }"
            : "/// <summary>Fault.</summary>\npublic class Fault : System.Exception {}\n/// <summary>Fixture.</summary>\npublic static class Fixture {\n/// <summary>Root.</summary>\n" + tag + "public static void Root() { Missing(); } }";
        await File.WriteAllTextAsync(Path.Combine(folder, "Source.cs"), source);
        (int Exit, string Output, string Error) restored = await Execute(["restore", project, "--configfile", Path.Combine(root, "NuGet.Config"), "-v:quiet"]);
        Assert.True(restored.Exit == 0, restored.Error + restored.Output);
        string alias = mode switch { ExceptionAnalysisMode.Direct => "direct", ExceptionAnalysisMode.ProjectTransitive => "project", ExceptionAnalysisMode.SolutionTransitive => "solution", _ => "declared" };
        (int Exit, string Output, string Error) run = await Execute([Path.Combine(root, "src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll"),
            "--check", project, "--exception-analysis-mode", alias, "--format", "json", "--output", report]);
        Assert.True(run.Exit is 0 or 1, run.Error + run.Output);
        using JsonDocument? document = JsonDocument.Parse(await File.ReadAllTextAsync(report));
        JsonElement analysis = document.RootElement.GetProperty("Analysis");
        Assert.Equal(scenario is "missing" or "diagnostic" ? "Incomplete" : "Complete", analysis.GetProperty("Completion").GetString());
        Assert.Equal("Succeeded", analysis.GetProperty("Execution").GetString());
        Assert.Equal(mode.ToString(), analysis.GetProperty("Mode").GetString());
        JsonElement findings = document.RootElement.GetProperty("Findings");
        Assert.Equal(scenario is "empty" or "missing" ? 0 : 1, run.Exit);
        if (scenario is "empty" or "missing") Assert.Empty(findings.EnumerateArray());
        if (scenario == "throw") Assert.Contains(findings.EnumerateArray(), finding => finding.GetProperty("SmellId").GetString() == "DOC610");
        if (scenario == "diagnostic") Assert.Contains(findings.EnumerateArray(), finding => finding.GetProperty("SmellId").GetString() == (mode == ExceptionAnalysisMode.Direct ? "DOC630" : "DOC631"));
        if (scenario == "missing") Assert.DoesNotContain("Check succeeded", run.Output);
        string evidence = Path.Combine(root, "artifacts/coverage/cli-witnesses"); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, mode + "-" + scenario + ".json"), JsonSerializer.Serialize(new
        { Mode = mode.ToString(), Scenario = scenario, run.Exit, run.Output, run.Error, Report = document.RootElement.Clone() }));
    }

    [Theory]
    [MemberData(nameof(ModesAndCases))]
    public async Task C1ToC4_AllModes_KeepFindingsAndCompletionIndependent(int modeValue, string scenario)
    {
        ExceptionAnalysisMode mode = (ExceptionAnalysisMode)modeValue;
        string body = scenario == "throw" ? "throw new Fault();" : scenario is "missing" or "diagnostic" ? "Missing();" : "";
        string tag = scenario == "diagnostic" ? "/// <exception cref=\"Fault\">Possible failure.</exception>\n" : "";
        RunResult? result = await Native(mode, body, tag);
        if (scenario is "empty" or "throw")
        {
            Assert.Equal("Complete", result.Coverage.Report().Completion);
            Assert.Equal("Succeeded", result.Coverage.Report().Execution);
            Assert.Equal(scenario == "empty" ? 0 : 1, result.FindingCount);
            if (scenario == "throw") Assert.Equal(1, result.SmellCounts["DOC610"]);
        }
        else
        {
            Assert.Equal("Incomplete", result.Coverage.Report().Completion);
            Assert.Contains("SourceBindingIncomplete", result.Coverage.Report().ReasonCodes);
            Assert.DoesNotContain("DOC610", result.SmellCounts.Keys);
            Assert.DoesNotContain("DOC611", result.SmellCounts.Keys);
            if (scenario == "missing") Assert.Equal(0, result.FindingCount);
            else Assert.Contains(mode == ExceptionAnalysisMode.Direct ? "DOC630" : "DOC631", result.SmellCounts.Keys);
        }
        Assert.Equal(mode.ToString(), result.Coverage.Report().Mode);
    }

    [Theory]
    [InlineData((int)ExceptionAnalysisMode.ProjectTransitive)]
    [InlineData((int)ExceptionAnalysisMode.SolutionTransitive)]
    [InlineData((int)ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions)]
    public async Task StructurallyGrowingOrbit_IsUnsupportedWithoutAnyFinding(int modeValue)
    {
        ExceptionAnalysisMode mode = (ExceptionAnalysisMode)modeValue;
        RunResult? result = await Native(mode, "Grow<int>();", extra: "static void Grow<T>() { Grow<System.Collections.Generic.List<T>>(); }");
        Assert.Equal(0, result.FindingCount);
        Assert.Equal("Unsupported", result.Coverage.Report().Completion);
        Assert.Equal("Succeeded", result.Coverage.Report().Execution);
        Assert.Contains("OutsideProfile", result.Coverage.Report().ReasonCodes);
    }

    [Fact]
    public async Task Direct_DoesNotInventTransitiveCoverageRequirements()
    {
        RunResult? result = await Native(ExceptionAnalysisMode.Direct, "Grow<int>();", extra: "static void Grow<T>() { Grow<System.Collections.Generic.List<T>>(); }");
        Assert.Equal("Complete", result.Coverage.Report().Completion);
        Assert.Equal(0, result.FindingCount);
    }

    [Fact]
    public void C10_ManyCompleteTargetsAndOneIncomplete_AggregateInEitherOrder()
    {
        AnalysisCoverage complete = Started();
        AnalysisCoverage incomplete = Started(); incomplete.Incomplete("TargetCoverageIncomplete");
        foreach (bool reversed in new[] { false, true })
        {
            AnalysisCoverage aggregate = Started();
            IEnumerable<AnalysisCoverage>? targets = Enumerable.Repeat(complete, 99).Append(incomplete);
            foreach (AnalysisCoverage? target in reversed ? targets.Reverse() : targets) aggregate.Merge(target);
            Assert.Equal("Incomplete", aggregate.Report().Completion);
            Assert.Equal(new[] { "TargetCoverageIncomplete" }, aggregate.Report().ReasonCodes);
            aggregate.Merge(incomplete);
            Assert.Single(aggregate.Report().ReasonCodes);
        }
    }

    [Fact]
    public void UnknownAndFailedComponents_NeverBecomeComplete_CloneIsIndependent()
    {
        AnalysisCoverage original = Started();
        AnalysisCoverage? clone = original.Clone(); clone.Incomplete("TargetCoverageIncomplete");
        Assert.Equal("Complete", original.Report().Completion);
        Assert.Equal("Incomplete", clone.Report().Completion);
        original.Merge(new AnalysisCoverage());
        Assert.Equal("Incomplete", original.Report().Completion);
        Assert.Contains("CoverageNotAssessed", original.Report().ReasonCodes);
        clone.Fail("AnalysisExecutionFailed"); original.Merge(clone);
        Assert.Equal("Failed", original.Report().Execution);
        Assert.Equal("NotAssessed", new RunResult().Coverage.Report().Completion);
    }

    public static IEnumerable<object[]> CoverageStatePairs()
        => from left in Enumerable.Range(0, 6) from right in Enumerable.Range(0, 6) select new object[] { left, right };

    [Theory]
    [MemberData(nameof(CoverageStatePairs))]
    public void Aggregation_IsCommutativeIncludingUnknownAndNotRunComponents(int left, int right)
    {
        AnalysisCoverage? forward = State(left); forward.Merge(State(right));
        AnalysisCoverage? reverse = State(right); reverse.Merge(State(left));
        Assert.Equal(JsonSerializer.Serialize(forward.Report()), JsonSerializer.Serialize(reverse.Report()));
        if (left == 0 || right == 0) Assert.NotEqual("Complete", forward.Report().Completion);

        static AnalysisCoverage State(int state)
        {
            AnalysisCoverage coverage = state is 0 or 5 ? new() : Started();
            if (state == 2) coverage.Incomplete("TargetCoverageIncomplete");
            if (state is 3 or 5) coverage.Unsupported("OutsideProfile");
            if (state == 4) coverage.Fail("AnalysisExecutionFailed");
            return coverage;
        }
    }

    [Fact]
    public void RunResultClone_PreservesCoverageWithoutSharingMutation()
    {
        RunResult result = new(); result.Coverage.Begin("Workspace", ExceptionAnalysisMode.Direct);
        RunResult? clone = result.Clone(); clone.Coverage.Incomplete("SourceBindingIncomplete");
        Assert.Equal("Complete", result.Coverage.Report().Completion);
        Assert.Equal("Incomplete", clone.Coverage.Report().Completion);
    }

    [Theory]
    [InlineData("Complete")]
    [InlineData("Incomplete")]
    [InlineData("Unsupported")]
    public void C7ToC9_ZeroFindings_HasSeparateConsoleJsonAndSarifCoverage(string status)
    {
        RunResult result = new(); result.Coverage.Begin("Workspace", ExceptionAnalysisMode.SolutionTransitive);
        if (status == "Incomplete") result.Coverage.Incomplete("TargetCoverageIncomplete");
        if (status == "Unsupported") result.Coverage.Unsupported("OutsideProfile");
        TextWriter? original = Console.Out; using StringWriter output = new();
        try { Console.SetOut(output); RunResultConsoleReporter.ReportCheckRunResult(result,
            new ToolOptions("selected", true, false, false, new XmlDocOptions(), OutputFormat.Console, null, false, false)); }
        finally { Console.SetOut(original); }
        Assert.Contains("Analysis coverage:", output.ToString());
        if (status != "Complete")
        {
            Assert.DoesNotContain("Check succeeded", output.ToString());
            Assert.Contains("not a complete negative", output.ToString());
        }
        string folder = Path.Combine(Path.GetTempPath(), "coverage-report-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string jsonPath = Path.Combine(folder, "report.json"), sarifPath = Path.Combine(folder, "report.sarif");
        new JsonFindingsReporter(jsonPath, "selected").Complete(result);
        new SarifFindingsReporter(sarifPath).Complete(result);
        using JsonDocument? json = JsonDocument.Parse(File.ReadAllText(jsonPath));
        Assert.Equal(status, json.RootElement.GetProperty("Analysis").GetProperty("Completion").GetString());
        Assert.Empty(json.RootElement.GetProperty("Findings").EnumerateArray());
        using JsonDocument? sarif = JsonDocument.Parse(File.ReadAllText(sarifPath));
        JsonElement run = sarif.RootElement.GetProperty("runs")[0];
        Assert.Empty(run.GetProperty("results").EnumerateArray());
        Assert.Equal(status, run.GetProperty("properties").GetProperty("analysis").GetProperty("completion").GetString());
        Assert.Equal(0, result.FindingCount);
    }

    [Fact]
    public void OldJsonWithoutAnalysis_IsNotRetrospectivelyCertified()
    {
        const string json = """{"Tool":"t","Version":"1","GeneratedAtUtc":"2026-01-01T00:00:00Z","TargetPath":"t","Metrics":{},"Findings":[]}""";
        Assert.Null(JsonSerializer.Deserialize<JsonReport>(json)!.Analysis);
    }

    [Theory]
    [InlineData((int)ExceptionAnalysisMode.Direct)]
    [InlineData((int)ExceptionAnalysisMode.ProjectTransitive)]
    [InlineData((int)ExceptionAnalysisMode.SolutionTransitive)]
    [InlineData((int)ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions)]
    public void ExistingFailureExitAndNoNormalReport_StillExposeFailure(int modeValue)
    {
        ExceptionAnalysisMode mode = (ExceptionAnalysisMode)modeValue;
        AnalysisCoverage coverage = new(); coverage.Begin("Workspace", mode); coverage.Fail("AnalysisExecutionFailed");
        TextWriter? original = Console.Error; using StringWriter output = new();
        try { Console.SetError(output); RunResultConsoleReporter.ReportCoverage(coverage.Report(), error: true); }
        finally { Console.SetError(original); }
        Assert.Contains("Failed", output.ToString()); Assert.Contains("incomplete", output.ToString());
        Assert.DoesNotContain("Check succeeded", output.ToString());
        Assert.Equal(0, ToolExitCodes.Success); Assert.Equal(1, ToolExitCodes.Findings);
        Assert.Equal(2, ToolExitCodes.InvalidArguments); Assert.Equal(3, ToolExitCodes.ProjectNotFound);
        Assert.Equal(4, ToolExitCodes.ExternalTargetFailure);
    }

    [Fact]
    public async Task C5_BoundedHistoricalCompletion_RequiresRealPairAndKeepsNativeFindingContract()
    {
        (HistoricalWorkerPayloadProjectionInput Input, string Job) fixture = await Custom("chain");
        ExternalExceptionFindingAnalysisResult? result = await ExternalTargetExecution.AnalyzeAsync(fixture.Job);
        Assert.True(result.Succeeded, result.Failure);
        Assert.Equal("Complete", result.Coverage!.Report().Completion);
        Assert.Equal("Succeeded", result.Coverage.Report().Execution);
        Assert.Equal("SolutionTransitive", result.Coverage.Report().Mode);
        Assert.Equal("DOC611", Assert.Single(result.Findings).Smell.ID);
        foreach (ExternalExceptionFlowAnalysisResult? stage in new[] { result.Direct, result.Summary! })
        {
            Assert.Equal(ExceptionFlowAnalyzerSelection.Historical, stage.Dispatch!.Decision!.Selection);
            HistoricalWorkerCallResult? call = stage.Dispatch.Routing!.HistoricalAnalysis!;
            Assert.True(call.Success); Assert.Empty(call.Result!.Uncertainties);
            Assert.NotNull(call.Identity); HistoricalWorkerLifetimeAssertions.Stopped(call);
        }
        HistoricalWorkerCallResult? direct = result.Direct.Dispatch!.Routing!.HistoricalAnalysis!;
        WorkerResponse? forged = new WorkerResponse(WorkerProtocol.Version, "analyze", true,
            direct.Identity! with { WorkerVersion = "invalid-version" }, direct.Result, Provenance: direct.Provenance);
        HistoricalWorkerCallResult? rejected = HistoricalWorkerResponseValidation.Validate(JsonSerializer.Serialize(forged, WorkerProtocol.JsonOptions), "", 0,
            result.Direct.PayloadProjection.Value!.Payload);
        Assert.Equal(HistoricalWorkerClientFailureCode.IdentityMismatch, rejected.Failure!.Code);
        Assert.Null(rejected.Result);
        AssertFailureProjection(rejected, "WorkerValidationFailed");

        HistoricalWorkerClient? missingHost = new HistoricalWorkerClient(Path.Combine(Path.GetTempPath(), "absent-host-" + Guid.NewGuid().ToString("N")),
            Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(), "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(10));
        HistoricalWorkerCallResult? startFailure = await missingHost.AnalyzeAsync(result.Direct.PayloadProjection.Value.Payload);
        Assert.Equal(HistoricalWorkerClientFailureCode.StartFailure, startFailure.Failure!.Code);
        Assert.Null(startFailure.ProcessId); Assert.Null(startFailure.Result);
        AssertFailureProjection(startFailure, "WorkerExecutionFailed");

        foreach ((WorkerFailureCode code, string? message, string? reason) in new[]
        {
            (WorkerFailureCode.AnalysisFailure, "Exception-flow analysis is incomplete.", "ResultCoverageIncomplete"),
            (WorkerFailureCode.AnalysisFailure, "Independent Direct evidence is incomplete.", "ResultCoverageIncomplete"),
            (WorkerFailureCode.CompilationFailure, "Historical compilation failed.", "SourceBindingIncomplete")
        })
        {
            WorkerResponse? response = new WorkerResponse(WorkerProtocol.Version, "analyze", false, direct.Identity, null,
                Failure: new(code, message, []));
            HistoricalWorkerCallResult? classified = HistoricalWorkerResponseValidation.Validate(JsonSerializer.Serialize(response, WorkerProtocol.JsonOptions), "", 1,
                result.Direct.PayloadProjection.Value.Payload);
            Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, classified.Failure!.Code);
            ExternalExceptionFlowAnalysisResult? stage = result.Direct with { Dispatch = result.Direct.Dispatch with { Routing = ExceptionFlowAnalysisRoutingResult.FromHistorical(classified) } };
            ExternalExceptionFindingAnalysisResult? outcome = new ExternalExceptionFindingAnalysisResult(stage, null, [], false, false, message);
            AnalysisCoverageReport? coverage = AnalysisCoverageObserver.ExternalFailure(outcome, ExceptionAnalysisMode.SolutionTransitive).Report();
            Assert.Equal("Succeeded", coverage.Execution); Assert.Equal("Incomplete", coverage.Completion);
            Assert.Contains(reason, coverage.ReasonCodes); Assert.Null(classified.Result);
        }

        void AssertFailureProjection(HistoricalWorkerCallResult failed, string reason)
        {
            ExternalExceptionFlowAnalysisResult? stage = result.Direct with { Dispatch = result.Direct.Dispatch with { Routing = ExceptionFlowAnalysisRoutingResult.FromHistorical(failed) } };
            ExternalExceptionFindingAnalysisResult? outcome = new ExternalExceptionFindingAnalysisResult(stage, null, [], false, false, "Rejected execution");
            AnalysisCoverageReport? coverage = AnalysisCoverageObserver.ExternalFailure(outcome, ExceptionAnalysisMode.SolutionTransitive).Report();
            Assert.Equal("Failed", coverage.Execution); Assert.Equal("Incomplete", coverage.Completion);
            Assert.Contains(reason, coverage.ReasonCodes); Assert.Empty(outcome.Findings);
        }
    }

    [Theory]
    [InlineData("growing", "Unsupported")]
    [InlineData("uncovered-call", "Incomplete")]
    public async Task C6_RealHistoricalFailureIsClassifiedWithoutFallbackOrFindings(string scenario, string status)
    {
        (HistoricalWorkerPayloadProjectionInput Input, string Job) fixture = await Custom(scenario);
        ExternalExceptionFindingAnalysisResult? result = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(fixture.Input, new(new(Client())));
        Assert.False(result.Succeeded); Assert.Empty(result.Findings);
        AnalysisCoverageReport? coverage = AnalysisCoverageObserver.ExternalFailure(result, ExceptionAnalysisMode.SolutionTransitive).Report();
        Assert.Equal(status, coverage.Completion); Assert.Equal("Succeeded", coverage.Execution);
        Assert.Contains("OutsideProfile", coverage.ReasonCodes);
        HistoricalWorkerCallResult? call = result.Summary!.Dispatch!.Routing!.HistoricalAnalysis!;
        Assert.NotNull(call.Identity); Assert.Null(call.Result);
        Assert.Null(result.Summary.Dispatch.Routing.CurrentAnalysis);
        HistoricalWorkerLifetimeAssertions.Stopped(call);
        ExternalTargetExecutionException? failure = await Assert.ThrowsAsync<ExternalTargetExecutionException>(() => ExternalTargetExecution.AnalyzeAsync(fixture.Job));
        Assert.Equal(status, failure.Coverage.Report().Completion);
        Assert.Equal("Succeeded", failure.Coverage.Report().Execution);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlainFileAndDirectory_ExposeMissingSemanticCapability(bool directory)
    {
        string folder = Path.Combine(Path.GetTempPath(), "coverage-input-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "Source.cs"); File.WriteAllText(file, "// empty C# input");
        string report = Path.Combine(folder, "output.json");
        RunResult result = ToolRunner.Run(new ToolOptions(directory ? folder : file, true, false, false,
            new XmlDocOptions(), OutputFormat.Json, report, false, false));
        Assert.Equal(0, result.FindingCount);
        Assert.Equal("Unsupported", result.Coverage.Report().Completion);
        Assert.Contains("SemanticInputRequired", result.Coverage.Report().ReasonCodes);
        using JsonDocument? json = JsonDocument.Parse(File.ReadAllText(report));
        Assert.Equal("Unsupported", json.RootElement.GetProperty("Analysis").GetProperty("Completion").GetString());
    }

    [Theory]
    [InlineData(0, "direct")]
    [InlineData(1, "project")]
    [InlineData(2, "declared")]
    [InlineData(3, "solution")]
    public void CurrentCli_ActualReadFailureRetainsExceptionAndExposesCompletion(int modeValue, string alias)
    {
        string folder = Path.Combine(Path.GetTempPath(), "coverage-failure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string source = Path.Combine(folder, "Source.cs"), report = Path.Combine(folder, "report.json");
        File.WriteAllText(source, "// source");
        using FileStream? exclusive = new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        MethodInfo? entry = typeof(XMLDocNormalizer.Program).GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!;
        TextWriter? original = Console.Error; using StringWriter errors = new();
        try
        {
            Console.SetError(errors);
            TargetInvocationException? failure = Assert.Throws<TargetInvocationException>(() => entry.Invoke(null,
                [new[] { "--check", source, "--format", "json", "--output", report, "--exception-analysis-mode", alias }]));
            Assert.IsAssignableFrom<IOException>(failure.InnerException);
        }
        finally { Console.SetError(original); }
        Assert.Contains("Analysis execution: Failed", errors.ToString());
        Assert.Contains(((ExceptionAnalysisMode)modeValue).ToString(), errors.ToString());
        Assert.False(File.Exists(report));
    }

    private static async Task<(HistoricalWorkerPayloadProjectionInput Input, string Job)> Custom(string scenario)
    {
        MethodInfo? helper = typeof(ExceptionFlowHistoricalSolutionAdoptionTests).GetMethod("Custom", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task? task = (Task)helper.Invoke(null, [scenario, 2])!; await task;
        object? fixture = task.GetType().GetProperty("Result")!.GetValue(task)!;
        return ((HistoricalWorkerPayloadProjectionInput)fixture.GetType().GetProperty("Input")!.GetValue(fixture)!,
            (string)fixture.GetType().GetProperty("Job")!.GetValue(fixture)!);
    }

    private static HistoricalWorkerClient Client() => new("dotnet", Path.Combine(ExceptionFlowProductionCurrentParityTests.Root(),
        "artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll"), TimeSpan.FromSeconds(60));

    private static async Task<(int Exit, string Output, string Error)> Execute(string[] arguments)
    {
        ProcessStartInfo start = new("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using Process? process = Process.Start(start)!;
        Task<string>? output = process.StandardOutput.ReadToEndAsync(); Task<string>? error = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60)); }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
        return (process.ExitCode, await output, await error);
    }

    private static AnalysisCoverage Started()
    { AnalysisCoverage coverage = new(); coverage.Begin("Workspace", ExceptionAnalysisMode.SolutionTransitive); return coverage; }

    private static async Task<RunResult> Native(ExceptionAnalysisMode mode, string body, string tag = "", string extra = "")
    {
        using AdhocWorkspace workspace = new();
        Project? project = workspace.AddProject("Coverage", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp12, DocumentationMode.Parse))
            .WithMetadataReferences(MetadataReferences.Default);
        string source = "public class Fault : System.Exception {} public static class Fixture {\n/// <summary>Root.</summary>\n"
            + tag + "public static void Root() { " + body + " } " + extra + " }";
        project = project.AddDocument("Root.cs", SourceText.From(source), filePath: "/coverage/Root.cs").Project;
        Compilation? compilation = (await project.GetCompilationAsync())!;
        SyntaxTree? tree = (await project.Documents.Single().GetSyntaxTreeAsync())!;
        ProjectClosureSemanticContext? context = ProjectClosureSemanticContextBuilder.Build([project], mode);
        RunResult result = new(); result.Coverage.Begin("Workspace", mode);
        AnalysisCoverageObserver.ObserveCompilation(result.Coverage, compilation);
        result.AccumulateFindings(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, "/coverage/Root.cs", compilation.GetSemanticModel(tree),
            context, new XmlDocOptions { ExceptionAnalysisMode = mode }, null, result.Coverage));
        return result;
    }
}
