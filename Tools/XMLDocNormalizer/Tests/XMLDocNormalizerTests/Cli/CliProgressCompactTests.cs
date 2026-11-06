using System.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressCompactTests
{
    [Fact] public void CompletedDocumentationMovesExactlyOnceAndDisappearsFromCurrentWork()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted();
        var done = s.Done("Documentation", "2/2 files"); Assert.DoesNotContain(s.Area("CURRENT WORK"), r => r.Text.Contains("Documentation"));
        s.P.Begin(RunPhase.ExceptionFlow); s.Tick(2); Assert.Equal(done, s.Done("Documentation")); s.Orange(1);
    }
    [Fact] public void CurrentWorkNeverContainsAGreenEntry()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted();
        Assert.DoesNotContain(s.Area("CURRENT WORK"), r => r.Tone == ProgressTone.Completed);
    }
    [Fact] public void ThereIsOnlyOneOrangeActivityAcrossRepeatedInterleaving()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair();
        foreach (var phase in new[] { RunPhase.Documentation, RunPhase.ExceptionFlow, RunPhase.Aggregation, RunPhase.Output })
        { s.P.Begin(phase); s.Tick(1); s.Orange(1); }
    }
    [Fact] public void TwoUnfinishedPartsRemainTogetherWithNeutralCounters()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.Live("Analyze");
        Assert.Contains("1/2 files", s.Text("CURRENT WORK")); Assert.Contains("0 completed / 1 started", s.Text("CURRENT WORK"));
        Assert.DoesNotContain(s.Area("COMPLETED"), r => r.Text.Contains("Documentation") || r.Text.Contains("Exception flow"));
    }
    [Fact] public void DocumentationCompletionLeavesFlowRunningWithItsRealCounters()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted();
        s.P.Begin(RunPhase.ExceptionFlow); s.Tick(1); s.Live("Exception flow"); Assert.Contains("0 completed / 1 started", s.Text("CURRENT WORK"));
    }
    [Fact] public void FrozenCompletedValuesIgnoreLaterObservationsAndRepeatedCompletion()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.EndParts(); var flow = s.Done("Exception flow", "1/1 roots");
        s.P.Observe(new(ExceptionFlowProgressKind.RootStarted)); s.P.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 99));
        s.P.Complete(RunPhase.ExceptionFlow); s.Tick(10); Assert.Equal(flow, s.Done("Exception flow"));
    }
    [Fact] public void CompletionAndFinalShrinkClearOldMemberAndCounterCells()
    {
        using var s = new CompactProgressScene(verbose: true); s.Prepare(); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.Admission));
        s.P.Observe(new(ExceptionFlowProgressKind.DefinitionDiscovered, 123)); s.Tick(1); s.EndAll(); s.P.Finish();
        foreach (var stale in new[] { "CURRENT WORK", "PENDING", "123", "Admission:", "Current:", "Last activity:", "files pending" }) Assert.DoesNotContain(stale, s.T.EntireScreen);
    }
    [Fact] public void RefreshChangesValuesWithoutMovingAnyEntry()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); var rows = s.Frame().Select(r => r.Row).ToArray();
        int moves = s.T.CursorMoves, reserved = s.T.Reserved; s.Tick(1); s.Tick(1);
        Assert.Equal(rows, s.Frame().Select(r => r.Row)); Assert.Equal(moves, s.T.CursorMoves); Assert.Equal(reserved, s.T.Reserved);
    }
    [Fact] public void ResultsNeverAppearInLiveOrFinalDashboard()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Findings(1275); s.Tick(1); s.NoResults();
        s.EndAll(); s.P.Finish(new("Succeeded", "Incomplete", "Workspace", "Direct", []), finalFindings: 1278, finalErrors: 2, finalWarnings: 980, finalHints: 298);
        s.NoResults(); Assert.DoesNotContain("1278", s.T.EntireScreen); Assert.DoesNotContain(s.Frame(), r => r.Tone == ProgressTone.Error);
    }
    [Fact] public void ConsoleReporterTextIsPreservedAndNotReplicatedByDashboard()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); var old = Console.Out;
        try { Console.SetOut(new CompactProgressScene.ScreenWriter(s.T)); s.P.WithOutput(() => Console.WriteLine("Reporter findings: 7"));
            Assert.Equal(1, s.T.EntireScreen.Split("Reporter findings: 7").Length - 1); s.Orange(1); }
        finally { Console.SetOut(old); }
    }
    [Fact] public void VerboseAdmissionExistsOnlyInsideTheUnfinishedAnalysis()
    {
        using var s = new CompactProgressScene(verbose: true); s.Prepare(); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.Admission));
        s.P.Observe(new(ExceptionFlowProgressKind.DefinitionProcessed, 120)); s.Tick(1); Assert.Contains("Admission: 120 processed", s.Text("CURRENT WORK"));
        s.EndAll(); s.P.Finish(); Assert.DoesNotContain("Admission:", s.T.EntireScreen);
    }
    [Fact] public void FailureAndInterruptionNeverCreateCompletedEntries()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Finish(failed: true);
        Assert.DoesNotContain(s.Area("COMPLETED"), r => r.Text.Contains("Documentation") || r.Text.Contains("Exception flow"));
        Assert.Contains(s.Frame(), r => r.Tone == ProgressTone.Error); s.Orange(0);
    }
    [Fact] public void MonochromeAndColorHaveIdenticalSemanticAreas()
    {
        using var color = new CompactProgressScene(); using var plain = new CompactProgressScene(color: false);
        foreach (var s in new[] { color, plain }) { s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted(); }
        Assert.Equal(color.Frame().Select(r => r.Text), plain.Frame().Select(r => r.Text));
        Assert.All(plain.Frame(), r => Assert.Equal(ProgressTone.Neutral, r.Tone));
    }
    [Fact] public void TextFallbackUsesTheSameCategoriesAndNeverDuplicatesResults()
    {
        var output = new StringWriter(); using var p = new RunProgress(output, false, true, startTimer: false);
        p.Begin(RunPhase.Loading); p.Complete(RunPhase.Loading); p.Begin(RunPhase.Compilation); p.Complete(RunPhase.Compilation);
        p.Begin(RunPhase.Documentation); p.Finish(finalFindings: 1278, finalWarnings: 980);
        string text = output.ToString(); Assert.Contains("Pending | [ ]", text); Assert.Contains("Current | [>]", text); Assert.Contains("Completed | [ok]", text);
        Assert.DoesNotContain("Findings", text); Assert.DoesNotContain("Coverage", text); Assert.DoesNotContain("1278", text);
    }
}

internal class CompactProgressScene : IDisposable
{
    private TimeSpan now;
    private bool paired;
    internal ProgressTestTerminal T { get; }
    internal RunProgress P { get; }
    internal CompactProgressScene(bool verbose = false, string mode = "SolutionTransitive", bool color = true, int width = 120)
    { T = new(width: width) { SupportsColor = color }; P = new(new StringWriter(), true, verbose, () => now, startTimer: false, terminal: T, mode: mode, displayPause: _ => { }); P.Refresh(); }
    internal void Tick(int seconds) { now += TimeSpan.FromSeconds(seconds); P.Refresh(); }
    internal void Prepare() { P.Begin(RunPhase.Loading); Tick(1); P.Complete(RunPhase.Loading); P.Begin(RunPhase.Compilation); Tick(2); P.Complete(RunPhase.Compilation); }
    internal void Pair() { paired = true; P.SetTotal(RunPhase.Documentation, 2); P.Begin(RunPhase.Documentation, "First.cs"); P.DocumentCompleted(); P.Begin(RunPhase.ExceptionFlow); P.Observe(new(ExceptionFlowProgressKind.RootStarted)); Tick(1); }
    internal void EndParts() { if (!paired) Pair(); P.Begin(RunPhase.Documentation, "Last.cs"); P.DocumentCompleted(); P.Begin(RunPhase.ExceptionFlow); P.Observe(new(ExceptionFlowProgressKind.RootCompleted)); P.CompleteExceptionFile(); }
    internal void EndAll() { EndParts(); P.Begin(RunPhase.Aggregation); P.Complete(RunPhase.Aggregation); P.CompleteAnalysis(); P.BeginFinalization(); P.Begin(RunPhase.Output); P.Complete(RunPhase.Output); }
    internal (int Row, string Text, ProgressTone Tone)[] Frame() => T.StyledWrites.GroupBy(r => r.Row).Select(g => g.Last()).OrderBy(r => r.Row).Select(r => (r.Row, r.Text.TrimEnd(), r.Tone)).Where(r => r.Item2.Length > 0).ToArray();
    internal (int Row, string Text, ProgressTone Tone)[] Area(string name) => Frame().SkipWhile(r => r.Text != name).Skip(1).TakeWhile(r => r.Text is not ("COMPLETED" or "CURRENT WORK" or "PENDING") && !r.Text.StartsWith("[!!]") && !r.Text.StartsWith("[!]")).ToArray();
    internal string Text(string area) => string.Join("\n", Area(area).Select(r => r.Text));
    internal (int Row, string Text, ProgressTone Tone) Done(string label, string contains = "") { var row = Assert.Single(Area("COMPLETED"), r => r.Text.StartsWith("[ok] " + label)); Assert.Equal(T.SupportsColor ? ProgressTone.Completed : ProgressTone.Neutral, row.Tone); Assert.Contains(contains, row.Text); return row; }
    internal void Live(string label) { var row = Assert.Single(Area("CURRENT WORK"), r => r.Text.StartsWith("[>] " + label)); Assert.Equal(T.SupportsColor ? ProgressTone.Active : ProgressTone.Neutral, row.Tone); }
    internal void Pending(string label) { var row = Assert.Single(Area("PENDING"), r => r.Text == "[ ] " + label); Assert.Equal(T.SupportsColor ? ProgressTone.Muted : ProgressTone.Neutral, row.Tone); }
    internal void Orange(int count) => Assert.Equal(count, Frame().Count(r => r.Tone == ProgressTone.Active && r.Text.StartsWith("[>] ", StringComparison.Ordinal)));
    internal void NoResults() { foreach (string label in new[] { "Findings", "Warnings", "Hints", "Coverage", "SLOC", "Diagnostics", "Suggestions" }) Assert.DoesNotContain(label, T.EntireScreen); }
    public void Dispose() => P.Dispose();
    internal sealed class ScreenWriter(ProgressTestTerminal terminal) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value) => terminal.WriteConsole(value.ToString());
        public override void Write(string? value) { if (value is not null) terminal.WriteConsole(value); }
    }
}
