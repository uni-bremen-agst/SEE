using System.Text;
using System.Text.Json;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Cli.Output;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressTests
{
    [Theory]
    [InlineData("auto", false)]
    [InlineData("on", false)]
    [InlineData("off", false)]
    [InlineData("ON", true)]
    public void ParserKeepsProgressIndependentOfVerbose(string mode, bool verbose)
    {
        using var fixture = new Files();
        string[] arguments = ["--check", "--progress", mode, fixture.Root];
        if (verbose) arguments = [..arguments, "--verbose"];
        Assert.True(ArgParsing.TryParseOptions(arguments, out var options));
        Assert.Equal(Enum.Parse<ProgressMode>(mode, true), options!.Progress);
        Assert.Equal(verbose, options.Verbose);
        Assert.Equal(fixture.Root, options.TargetPath);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData("--verbose")]
    public void InvalidProgressValuesDoNotBecomeTargets(string value)
    {
        using var fixture = new Files();
        Capture(() => Assert.False(ArgParsing.TryParseOptions(["--check", fixture.Root, "--progress", value], out _)));
    }

    [Fact]
    public void MissingOrDuplicateProgressValuesAreRejectedAndDefaultIsAuto()
    {
        using var fixture = new Files();
        Capture(() => {
            Assert.False(ArgParsing.TryParseOptions(["--check", fixture.Root, "--progress"], out _));
            Assert.False(ArgParsing.TryParseOptions(["--check", fixture.Root, "--progress", "on", "--progress", "off"], out _));
            Assert.True(ArgParsing.TryParseOptions(["--check", fixture.Root], out var options));
            Assert.Equal(ProgressMode.Auto, options!.Progress);
        });
    }

    [Theory]
    [InlineData("Auto", false, false, false, false, null, true)]
    [InlineData("Auto", true, false, false, false, null, false)]
    [InlineData("Auto", false, true, false, false, null, false)]
    [InlineData("Auto", false, false, true, false, null, false)]
    [InlineData("Auto", false, false, false, true, null, false)]
    [InlineData("Auto", false, false, false, false, "dumb", false)]
    [InlineData("On", true, true, true, true, "dumb", true)]
    [InlineData("Off", false, false, false, false, null, false)]
    public void SelectionRespectsStreamsAndCi(string mode, bool stdout, bool stderr, bool input, bool ci, string? term, bool expected)
        => Assert.Equal(expected, RunProgress.ShouldEnable(Enum.Parse<ProgressMode>(mode), stdout, stderr, input, ci, term));

    [Fact]
    public void RegularDetectorCallsDoNotAdvanceTheFileCounter()
    {
        TimeSpan now = TimeSpan.Zero; var writer = new StringWriter();
        using var progress = new RunProgress(writer, true, false, () => now, () => 300, false);
        progress.SetTotal(RunPhase.Documentation, 2);
        progress.Begin(RunPhase.Documentation, "First.cs");
        for (int n = 0; n < 20; n++) { now += TimeSpan.FromSeconds(1); progress.Detector("Param"); }
        Assert.Contains("0/2 files", writer.ToString());
        progress.DocumentCompleted(); now += TimeSpan.FromSeconds(1); progress.Refresh();
        Assert.Contains("50% 1/2 files", writer.ToString());
        Assert.DoesNotContain("100%", writer.ToString());
        progress.Finish(failed: true);
        Assert.Contains("failed/interrupted", writer.ToString());
        Assert.Contains("1/2 files", writer.ToString());
    }

    [Fact]
    public void RefreshOnlyChangesTimeAndActivityAge()
    {
        TimeSpan now = TimeSpan.Zero; var writer = new StringWriter();
        using var progress = new RunProgress(writer, true, false, () => now, () => 400, false);
        progress.Begin(RunPhase.ExceptionFlow);
        progress.Observe(new(ExceptionFlowProgressKind.RootStarted));
        progress.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 7));
        now = TimeSpan.FromSeconds(12); progress.Refresh();
        Assert.Contains("00:12", writer.ToString());
        Assert.Contains("activity 12s ago", writer.ToString());
        Assert.DoesNotContain("%", writer.ToString());
        progress.Finish();
        string text = writer.ToString();
        Assert.Contains("roots completed 0, started 1", text);
        Assert.Contains("contexts discovered 7", text);
        Assert.Contains("context workitems processed 0", text);
        Assert.DoesNotContain("ETA", text);
        int length = text.Length; progress.Refresh(); Assert.Equal(length, writer.ToString().Length);
    }

    [Fact]
    public void InterleavedPhasesAccumulateOnlyTheirActualElapsedTime()
    {
        TimeSpan now = TimeSpan.Zero; var writer = new StringWriter();
        using var progress = new RunProgress(writer, false, false, () => now, startTimer: false);
        progress.SetTotal(RunPhase.Documentation, 2);
        progress.Begin(RunPhase.Documentation); progress.DocumentCompleted();
        now = TimeSpan.FromSeconds(3); progress.Begin(RunPhase.ExceptionFlow);
        now = TimeSpan.FromSeconds(13); progress.Begin(RunPhase.Documentation); progress.DocumentCompleted();
        now = TimeSpan.FromSeconds(15); progress.Finish();
        Assert.Contains("[Documentation] finished; 2/2 files; 00:05", writer.ToString());
        Assert.Contains("processed 0; 00:10", writer.ToString());
    }

    [Fact]
    public void VerboseMemberTracksProcessingRatherThanDiscoveredTargets()
    {
        TimeSpan now = TimeSpan.Zero; var writer = new StringWriter(); var fixture = FlowFixture();
        using var progress = new RunProgress(writer, true, true, () => now, () => 300, false);
        progress.Begin(RunPhase.ExceptionFlow);
        var type = fixture.Compilation.GetTypeByMetadataName("Entry")!;
        progress.Observe(new(ExceptionFlowProgressKind.ContextStarted, Symbol: type.GetMembers("Run").Single()));
        progress.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, Symbol: type.GetMembers("B").Single()));
        now = TimeSpan.FromSeconds(2); progress.Refresh(); progress.Finish();
        Assert.Contains("member Entry.Run()", writer.ToString());
        Assert.DoesNotContain("member Entry.B()", writer.ToString());
    }

    [Fact]
    public void ShorterLinesClearRemaindersWithoutAnsi()
    {
        var writer = new StringWriter(); int length = 0;
        ConsoleLogger.DrawProgress(writer, "abcdef", ref length);
        ConsoleLogger.DrawProgress(writer, "xy", ref length);
        Assert.Equal("\rabcdef\rxy    ", writer.ToString());
        Assert.Equal(2, length);
        Assert.DoesNotContain('\u001b', writer.ToString());
    }

    [Theory]
    [InlineData(1)] [InlineData(12)] [InlineData(40)]
    public void NarrowTerminalAndResizeNeverWrapALine(int narrow)
    {
        TimeSpan now = TimeSpan.Zero; int width = 120; var writer = new StringWriter();
        using var progress = new RunProgress(writer, true, true, () => now, () => width, false);
        progress.Begin(RunPhase.Documentation, new string('a', 150) + ".cs");
        writer.GetStringBuilder().Clear(); width = narrow; now = TimeSpan.FromSeconds(1); progress.Refresh();
        Assert.All(writer.ToString().Split('\r'), line => Assert.True(line.Length <= narrow - 1));
    }

    [Fact]
    public void VerboseShowsTheActualDetectorAndNonInteractiveOutputIsBounded()
    {
        TimeSpan now = TimeSpan.Zero; var writer = new StringWriter();
        using (var progress = new RunProgress(writer, true, true, () => now, () => 180, false))
        {
            progress.Begin(RunPhase.Documentation, "One.cs"); now = TimeSpan.FromSeconds(2); progress.Detector("Param");
            Assert.Contains("detector Param", writer.ToString()); progress.Finish();
        }
        writer.GetStringBuilder().Clear();
        using (var progress = new RunProgress(writer, false, true, () => now, startTimer: false))
        {
            progress.SetTotal(RunPhase.Documentation, 1000);
            for (int n = 0; n < 1000; n++) {
                progress.Begin(RunPhase.Documentation, "One.cs"); progress.Detector("Param"); progress.DocumentCompleted();
                progress.Begin(RunPhase.ExceptionFlow); progress.Observe(new(ExceptionFlowProgressKind.RootStarted)); progress.Observe(new(ExceptionFlowProgressKind.RootCompleted));
            }
            progress.Finish(new("Succeeded", "Incomplete", "Test", "SolutionTransitive", ["TargetCoverageIncomplete"]));
        }
        Assert.True(writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 5);
        Assert.Contains("1000/1000 files", writer.ToString());
        Assert.Contains("analysis Incomplete", writer.ToString());
        Assert.DoesNotContain('\r', writer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Direct")]
    [InlineData("ProjectTransitive")]
    [InlineData("SolutionTransitive")]
    [InlineData("ProjectTransitiveDeclaredExceptions")]
    public void NativeQueriesPreserveFindingsAndCoverageInEveryMode(string modeName)
    {
        ExceptionAnalysisMode mode = Enum.Parse<ExceptionAnalysisMode>(modeName);
        var fixture = FlowFixture(); var events = new List<ExceptionFlowProgressEvent>();
        var baseline = Analyze(fixture, mode, null);
        var observed = Analyze(fixture, mode, events.Add);
        Assert.Equal(baseline, observed);
        Assert.Single(events, e => e.Kind == ExceptionFlowProgressKind.RootStarted);
        Assert.Single(events, e => e.Kind == ExceptionFlowProgressKind.RootCompleted);
        Assert.DoesNotContain(events, e => e.Kind == ExceptionFlowProgressKind.RootFailed);
        if (mode == ExceptionAnalysisMode.Direct) Assert.DoesNotContain(events, e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered);
        else {
            Assert.Equal(3, events.Count(e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered));
            Assert.Equal(3, events.Count(e => e.Kind == ExceptionFlowProgressKind.ContextProcessed));
            int firstProcessed = events.FindIndex(e => e.Kind == ExceptionFlowProgressKind.ContextProcessed);
            Assert.Contains(events.Skip(firstProcessed + 1), e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered);
            Assert.Contains(events, e => e.Kind == ExceptionFlowProgressKind.AdmissionSealing);
            Assert.Contains(events, e => e.Kind == ExceptionFlowProgressKind.Evaluation);
        }
    }

    [Fact]
    public void FailedDiagnosticConsumersCannotChangeAnalyzerEvidence()
    {
        var fixture = FlowFixture();
        Assert.Equal(Analyze(fixture, ExceptionAnalysisMode.SolutionTransitive, null),
            Analyze(fixture, ExceptionAnalysisMode.SolutionTransitive, _ => throw new IOException("closed sink")));
        var baseline = fixture.Session().Session.Analyze(fixture.Root("Run"));
        var run = fixture.Session();
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(baseline)),
            JsonSerializer.Serialize(AdmissionCharacterization.Flow(run.Session.AnalyzeWithProgress(fixture.Root("Run"), _ => throw new IOException("closed sink")))));
        using var progress = new RunProgress(new ClosedWriter(), false, false, startTimer: false);
        progress.Begin(RunPhase.Loading); progress.Finish();
    }

    [Theory]
    [InlineData("json")]
    [InlineData("sarif")]
    public void RealFilePipelineCountsFilesAndKeepsSerializedResultsUnchanged(string format)
    {
        using var fixture = new Files();
        var output = Enum.Parse<OutputFormat>(format, true);
        RunResult? off = null, on = null;
        string offPath = Path.Combine(fixture.Root, "off." + format), onPath = Path.Combine(fixture.Root, "on." + format);
        var silent = Capture(() => off = ToolRunner.Run(Options(fixture.Root, offPath, output, ProgressMode.Off)));
        var visible = Capture(() => on = ToolRunner.Run(Options(fixture.Root, onPath, output, ProgressMode.On)));
        Assert.Equal("", silent.Error);
        Assert.DoesNotContain("[Documentation]", visible.Output);
        Assert.Contains("2/2 files", visible.Error);
        Assert.DoesNotContain("20/", visible.Error);
        Assert.DoesNotContain('\u001b', visible.Error);
        Assert.DoesNotContain('\r', visible.Error.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal(off!.FindingCount, on!.FindingCount);
        Assert.Equal(JsonSerializer.Serialize(off.Coverage.Report()), JsonSerializer.Serialize(on.Coverage.Report()));
        using var first = JsonDocument.Parse(File.ReadAllText(offPath)); using var second = JsonDocument.Parse(File.ReadAllText(onPath));
        var left = first.RootElement; var right = second.RootElement;
        if (format == "json") Assert.Equal(left.GetProperty("Findings").GetRawText(), right.GetProperty("Findings").GetRawText());
        else Assert.Equal(left.GetProperty("runs")[0].GetProperty("results").GetRawText(), right.GetProperty("runs")[0].GetProperty("results").GetRawText());
        Assert.Null(ConsoleLogger.CurrentProgress);
    }

    [Fact]
    public void InputAndOutputFailuresCloseProgressWithoutClaimingSuccessfulExecution()
    {
        using var fixture = new Files();
        var invalid = Capture(() => Assert.Throws<ArgumentException>(() => ToolRunner.Run(Options(Path.Combine(fixture.Root, "missing"), "none", OutputFormat.Json, ProgressMode.On))));
        Assert.Contains("failed/interrupted", invalid.Error);
        Assert.Contains("[Run] Failed", invalid.Error);
        var output = Capture(() => Assert.ThrowsAny<Exception>(() => ToolRunner.Run(Options(fixture.Root, fixture.Root, OutputFormat.Json, ProgressMode.On))));
        Assert.Contains("[Write output] failed/interrupted", output.Error);
        Assert.Contains("[Run] Failed", output.Error);
        Assert.Null(ConsoleLogger.CurrentProgress);
    }

    private static AdmissionCharacterization.Fixture FlowFixture() => new([("/CLI1/Root.cs", """
        class Fault : System.Exception {}
        /// <summary>Container.</summary>
        class Entry {
            /// <summary>Run.</summary>
            /// <exception cref="Fault">Unused.</exception>
            public void Run() { A(); A(); }
            static void A() { B(); }
            static void B() {}
        }
        """)], assemblyName: "CLI1.Fixture");

    private static string Analyze(AdmissionCharacterization.Fixture fixture, ExceptionAnalysisMode mode, Action<ExceptionFlowProgressEvent>? observer)
    {
        var run = fixture.Session();
        var tree = fixture.Compilation.SyntaxTrees.Single(); var coverage = new AnalysisCoverage(); coverage.Begin("Fixture", mode);
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, tree.FilePath, fixture.Compilation.GetSemanticModel(tree),
            run.Context, new XmlDocOptions { ExceptionAnalysisMode = mode }, run.Session, coverage, observer);
        return JsonSerializer.Serialize(new { findings, Coverage = coverage.Report() });
    }

    private static ToolOptions Options(string target, string report, OutputFormat format, ProgressMode progress)
        => new(target, true, false, false, new(), format, report, false, false, progress: progress);

    private static (string Output, string Error) Capture(Action action)
    {
        TextWriter oldOut = Console.Out, oldError = Console.Error;
        using var stdout = new StringWriter(); using var stderr = new StringWriter();
        try { Console.SetOut(stdout); Console.SetError(stderr); action(); return (stdout.ToString(), stderr.ToString()); }
        finally { Console.SetOut(oldOut); Console.SetError(oldError); ConsoleLogger.VerboseEnabled = false; }
    }

    private sealed class ClosedWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void WriteLine(string? value) => throw new IOException("closed");
    }

    private sealed class Files : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "cli1-fixture-" + Guid.NewGuid().ToString("N"));
        internal Files()
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "First.cs"), "/// <summary>First.</summary>\nclass First {} ");
            File.WriteAllText(Path.Combine(Root, "Second.cs"), "class Second { public void Run() {} }");
            File.WriteAllText(Path.Combine(Root, "Skip.g.cs"), "class Generated {} ");
        }
        public void Dispose()
        {
            string target = Path.GetFullPath(Root);
            if (!target.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("cli1-fixture-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected fixture cleanup path");
            if (Directory.Exists(target)) Directory.Delete(target, true);
        }
    }
}
