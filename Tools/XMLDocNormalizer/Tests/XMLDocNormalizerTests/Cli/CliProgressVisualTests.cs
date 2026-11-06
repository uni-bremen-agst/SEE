using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressVisualTests
{
    [Theory]
    [InlineData(false, false, null, null, true)]
    [InlineData(true, false, null, null, false)]
    [InlineData(false, true, null, null, false)]
    [InlineData(false, false, "1", null, false)]
    [InlineData(false, false, "0", null, false)]
    [InlineData(false, false, "", null, true)]
    [InlineData(false, false, null, "DUMB", false)]
    [InlineData(false, false, null, "xterm-256color", true)]
    public void ColorPolicyHonorsRedirectCiNoColorAndUnsuitableTerminals(bool redirected, bool ci, string? noColor, string? term, bool enabled)
        => Assert.Equal(enabled, ProgressColorPolicy.Enable(redirected, ci, noColor, term));

    [Theory]
    [InlineData("Neutral", 7)] [InlineData("Active", 6)] [InlineData("Completed", 10)]
    [InlineData("Notice", 14)] [InlineData("Error", 12)] [InlineData("Muted", 8)]
    public void NativePalettePreservesBackgroundAndRestoresNeutralAttributes(string name, int foreground)
    {
        var tone = Enum.Parse<ProgressTone>(name);
        ushort original = 0x97;
        Assert.Equal(0x90 | foreground, WindowsProgressTerminal.Attributes(original, tone));
        Assert.Equal(original, WindowsProgressTerminal.Attributes(original, ProgressTone.Neutral));
    }

    [Fact]
    public void AStyleOnlyChangeUpdatesOneRowAndRetainsStatusTextWithoutAnsi()
    {
        var terminal = new ProgressTestTerminal { SupportsColor = true }; var region = new ProgressRegion(terminal, 2);
        Assert.True(region.Render(new ProgressRow[] { new("[>] active", ProgressTone.Active), new("stable") }));
        terminal.Writes.Clear(); terminal.StyledWrites.Clear();
        Assert.True(region.Render(new ProgressRow[] { new("[>] active", ProgressTone.Notice), new("stable") }));
        Assert.Single(terminal.Writes); Assert.Equal(ProgressTone.Notice, Assert.Single(terminal.StyledWrites).Tone);
        Assert.DoesNotContain('\u001b', terminal.Screen);
    }

    [Fact]
    public void MonochromeTerminalIgnoresStyleChangesWithoutExtraWrites()
    {
        var terminal = new ProgressTestTerminal(); var region = new ProgressRegion(terminal, 1);
        Assert.True(region.Render(new ProgressRow[] { new("[ok] completed", ProgressTone.Completed) }));
        terminal.Writes.Clear(); terminal.StyledWrites.Clear();
        Assert.True(region.Render(new ProgressRow[] { new("[ok] completed", ProgressTone.Active) }));
        Assert.Empty(terminal.Writes); Assert.Contains("[ok] completed", terminal.Screen);
    }

    [Fact]
    public void LossOfColorSupportFallsBackToNeutralWithoutLosingTheRegion()
    {
        var terminal = new ProgressTestTerminal { SupportsColor = true }; var region = new ProgressRegion(terminal, 2);
        Assert.True(region.Render(new ProgressRow[] { new("[>] active", ProgressTone.Active), new("hint", ProgressTone.Muted) }));
        terminal.SupportsColor = false; terminal.StyledWrites.Clear();
        Assert.True(region.Render(new ProgressRow[] { new("[>] active", ProgressTone.Active), new("hint", ProgressTone.Muted) }));
        Assert.Equal(2, terminal.StyledWrites.Count); Assert.All(terminal.StyledWrites, row => Assert.Equal(ProgressTone.Neutral, row.Tone));
        Assert.Equal(2, terminal.Reserved); Assert.Contains("[>] active", terminal.Screen);
    }

    [Theory]
    [InlineData(40, 24)] [InlineData(60, 24)] [InlineData(79, 24)]
    public void CompactLayoutRetainsBothAreasCountersAndClock(int width, int height)
    {
        TimeSpan now = TimeSpan.Zero; var terminal = new ProgressTestTerminal(width: width) { Height = height };
        var writer = new StringWriter();
        using var progress = new RunProgress(writer, true, true, () => now, startTimer: false, terminal: terminal, mode: "SolutionTransitive");
        progress.SetTotal(RunPhase.Documentation, 2); progress.Begin(RunPhase.Documentation, "Long-file-name.cs"); progress.DocumentCompleted();
        progress.Begin(RunPhase.ExceptionFlow); progress.Observe(new(ExceptionFlowProgressKind.RootStarted));
        progress.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 7)); progress.Observe(new(ExceptionFlowProgressKind.ContextProcessed, 2));
        now = TimeSpan.FromSeconds(2); progress.Refresh();
        Assert.Contains("Docs: [", terminal.Screen); Assert.Contains("[>] Analyze", terminal.Screen);
        Assert.Contains("50% 1/2 files", terminal.Screen); Assert.Contains("Roots: 0/1 completed/started", terminal.Screen);
        Assert.Contains("Contexts  : 7 discovered", terminal.Screen); Assert.Contains("Workitems : 2 processed", terminal.Screen);
        Assert.Contains("00:02", terminal.Screen); Assert.DoesNotContain("unavailable", writer.ToString());
        Assert.All(terminal.Writes, row => Assert.True(row.Text.Length < terminal.BufferWidth));
        progress.Finish(new("Succeeded", "Incomplete", "Workspace", "SolutionTransitive", []), finalFindings: 3);
        Assert.Contains("Interrupted:", terminal.Screen); Assert.DoesNotContain("Coverage:", terminal.Screen); Assert.DoesNotContain("Findings:", terminal.Screen);
        Assert.DoesNotContain("Live display unavailable", writer.ToString());
    }

    [Theory]
    [InlineData("Direct")]
    [InlineData("ProjectTransitive")]
    [InlineData("SolutionTransitive")]
    [InlineData("ProjectTransitiveDeclaredExceptions")]
    public void FinalViewOmitsReporterResultsAndClearsTransientDiagnostics(string mode)
    {
        using var s = new CompactProgressScene(verbose: true, mode: mode); s.Prepare(); s.Pair();
        s.P.Begin(RunPhase.Documentation); s.P.Detector("Param"); s.Tick(1); Assert.Contains("Current: Documentation check - Param", s.T.Screen);
        s.P.Begin(RunPhase.ExceptionFlow);
        if (mode != "Direct") { s.P.Observe(new(ExceptionFlowProgressKind.Admission)); s.P.Observe(new(ExceptionFlowProgressKind.DefinitionDiscovered, 3)); s.P.Observe(new(ExceptionFlowProgressKind.DefinitionProcessed, 2)); }
        s.P.Findings(1275); s.Tick(1); s.NoResults(); s.EndAll();
        s.P.Finish(new("Succeeded", "Incomplete", "Workspace", mode, []), finalFindings: 1278);
        s.NoResults(); s.Done("Documentation", "2/2 files"); s.Done("Exception flow", "1/1 roots");
        foreach (string transient in new[] { "Last.cs", "Last activity", "Current file", "Current:", "Detector:", "Member:", "Stage:", "Admission:", "1275", "1278" }) Assert.DoesNotContain(transient, s.T.EntireScreen);
        Assert.DoesNotContain(s.Frame(), r => r.Tone == ProgressTone.Error);
    }
    [Fact]
    public void FinalFailureIsRedWhileCoverageAndFindingsRemainSeparate()
    {
        var terminal = new ProgressTestTerminal { SupportsColor = true };
        using var progress = new RunProgress(new StringWriter(), true, false, startTimer: false, terminal: terminal);
        progress.Begin(RunPhase.Loading); progress.Finish(failed: true);
        Assert.Contains("[!!] Execution failed", terminal.Screen);
        Assert.Contains(terminal.StyledWrites, row => row.Text.Contains("Execution failed", StringComparison.Ordinal) && row.Tone == ProgressTone.Error);
        Assert.DoesNotContain("Coverage:", terminal.Screen); Assert.DoesNotContain("Findings:", terminal.Screen);
        Assert.DoesNotContain("Findings: 0 reported", terminal.Screen);
    }

    [Fact]
    public void AdaptiveRegionClearsRemovedRowsAndDoesNotReservePhantomBlankLines()
    {
        var terminal = new ProgressTestTerminal(); var region = new ProgressRegion(terminal);
        Assert.True(region.Render(new[] { "header", "current file", "member snapshot", "interim findings" }));
        Assert.Equal(4, terminal.Reserved); terminal.Writes.Clear();
        Assert.True(region.Render(new[] { "header", "final findings" }));
        Assert.DoesNotContain("snapshot", terminal.EntireScreen); Assert.DoesNotContain("interim", terminal.EntireScreen);
        Assert.Equal(2, terminal.Row); Assert.Equal(4, terminal.Reserved);
        terminal.Writes.Clear(); Assert.True(region.Render(new[] { "header", "final findings" }));
        Assert.Empty(terminal.Writes);
    }

    [Fact]
    public void ResizeUsesCompactHierarchyAndClearsTheFormerVerboseTail()
    {
        TimeSpan now = TimeSpan.Zero; var terminal = new ProgressTestTerminal(width: 120); var writer = new StringWriter();
        using var progress = new RunProgress(writer, true, true, () => now, startTimer: false, terminal: terminal, mode: "Direct");
        progress.Begin(RunPhase.Documentation, "A-very-long-file-name-for-the-wide-view.cs");
        progress.Detector("Param"); now += TimeSpan.FromSeconds(1); progress.Refresh();
        int docsRow = terminal.StyledWrites.Last(row => row.Text.Contains("Documentation:", StringComparison.Ordinal)).Row;
        terminal.Writes.Clear(); terminal.Width = 40; now += TimeSpan.FromSeconds(1); progress.Refresh();
        Assert.Contains("Documentation:", terminal.Screen);
        Assert.Equal(docsRow, terminal.StyledWrites.Last(row => row.Text.Contains("Documentation:", StringComparison.Ordinal)).Row);
        Assert.Contains("Current: Documentation check - Param", terminal.Screen);
        Assert.Empty(terminal.Writes); Assert.Contains("Live display unavailable", writer.ToString());
    }
}
