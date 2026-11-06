using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressLayoutTests
{
    private static readonly string[] Headings = ["COMPLETED", "CURRENT WORK", "PENDING"];

    [Fact]
    public void HeadingsUseExistingColorsWhileOnlyOneActivityIsOrange()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair();
        var frame = s.Frame();
        Assert.Equal(ProgressTone.Completed, Assert.Single(frame, r => r.Text == "COMPLETED").Tone);
        Assert.Equal(ProgressTone.Active, Assert.Single(frame, r => r.Text == "CURRENT WORK").Tone);
        Assert.Equal(ProgressTone.Muted, Assert.Single(frame, r => r.Text == "PENDING").Tone);
        s.Orange(1);
        Assert.Equal(2, frame.Count(r => r.Tone == ProgressTone.Active)); // Heading plus one actual activity.
        Assert.All(s.Area("CURRENT WORK").Where(r => !r.Text.StartsWith("[>] ")), r => Assert.Equal(ProgressTone.Neutral, r.Tone));
        Assert.All(s.Area("PENDING"), r => Assert.Equal(ProgressTone.Muted, r.Tone));
    }

    [Fact]
    public void ThreeAreasHaveExactlyOneEmptyRowBetweenThem()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair();
        Assert.Equal(3, AssertSpacing(Physical(s)));
        Assert.Equal(2, Physical(s).Count(string.IsNullOrEmpty));
    }

    [Fact]
    public void InitialPendingAreaHasNoOuterOrInternalSpacer()
    {
        using var s = new CompactProgressScene();
        Assert.Equal(1, AssertSpacing(Physical(s))); Assert.DoesNotContain("", Physical(s));
        Assert.DoesNotContain("00:00", s.Text("PENDING"));
    }

    [Fact]
    public void FinishedViewHasNoTrailingSpacersOrOldCurrentRows()
    {
        using var s = new CompactProgressScene(verbose: true); s.Prepare(); s.Pair();
        s.P.Observe(new(ExceptionFlowProgressKind.Admission)); s.P.Observe(new(ExceptionFlowProgressKind.DefinitionDiscovered, 123)); s.Tick(1);
        s.EndAll(); s.P.Finish();
        Assert.Equal(1, AssertSpacing(Physical(s))); Assert.DoesNotContain("", Physical(s));
        foreach (var stale in new[] { "CURRENT WORK", "PENDING", "Admission:", "Last activity:", "123" }) Assert.DoesNotContain(stale, s.T.EntireScreen);
        s.NoResults();
    }

    [Theory]
    [InlineData("completed-current")]
    [InlineData("current-pending")]
    [InlineData("completed-pending")]
    public void TwoVisibleAreasHaveExactlyOneSpacer(string state)
    {
        using var s = new CompactProgressScene();
        if (state == "completed-current") { s.Prepare(); s.EndParts(); s.P.BeginFinalization(); }
        else if (state == "current-pending") s.P.Begin(RunPhase.Loading);
        else { s.P.Begin(RunPhase.Loading); s.P.Complete(RunPhase.Loading); }
        Assert.Equal(2, AssertSpacing(Physical(s))); Assert.Equal(1, Physical(s).Count(string.IsNullOrEmpty));
    }

    [Fact]
    public void RepeatedRefreshesPreserveSpacerCoordinatesAndDoNotMoveCursor()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair();
        var before = Physical(s); int moves = s.T.CursorMoves, reserved = s.T.Reserved;
        s.T.Writes.Clear(); s.P.Refresh(); Assert.Empty(s.T.Writes);
        for (int i = 0; i < 3; i++) { s.Tick(1); AssertSpacing(Physical(s)); }
        Assert.Equal(before.Select((line, row) => (line, row)).Where(r => r.line.Length == 0).Select(r => r.row), Physical(s).Select((line, row) => (line, row)).Where(r => r.line.Length == 0).Select(r => r.row));
        Assert.Equal(moves, s.T.CursorMoves); Assert.Equal(reserved, s.T.Reserved);
        Assert.DoesNotContain(s.T.Writes, row => string.IsNullOrWhiteSpace(row.Text));
    }

    [Fact]
    public void AreaChangesClearOldCellsWithoutDuplicatingSeparators()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); AssertSpacing(Physical(s));
        var load = s.Done("Load inputs"); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted();
        AssertSpacing(Physical(s)); Assert.Equal(load, s.Done("Load inputs")); Assert.DoesNotContain("Documentation", s.Text("CURRENT WORK"));
        s.P.Begin(RunPhase.ExceptionFlow); s.Tick(1); AssertSpacing(Physical(s)); s.Orange(1);
        s.EndAll(); s.P.Finish(); AssertSpacing(Physical(s)); Assert.DoesNotContain("Current:", s.T.EntireScreen);
    }

    [Fact]
    public void VerboseAnalysisInformationHasUniformNeutralIndentation()
    {
        using var s = new CompactProgressScene(verbose: true); s.Prepare(); s.Pair();
        s.P.Observe(new(ExceptionFlowProgressKind.Admission)); s.P.Observe(new(ExceptionFlowProgressKind.DefinitionProcessed, 120)); s.Tick(1);
        var details = s.Area("CURRENT WORK").Where(r => !r.Text.StartsWith("[>] ")).ToArray();
        Assert.Contains(details, r => r.Text.Contains("1/2 files")); Assert.Contains(details, r => r.Text.Contains("Roots")); Assert.Contains(details, r => r.Text.Contains("Admission: 120"));
        Assert.All(details, r => { Assert.StartsWith("  ", r.Text); Assert.False(r.Text.StartsWith("   ")); Assert.Equal(ProgressTone.Neutral, r.Tone); });
        AssertSpacing(Physical(s)); s.Orange(1);
    }

    [Theory]
    [InlineData(40)] [InlineData(60)] [InlineData(79)]
    public void NarrowLayoutAndViewportResizeRetainSeparatorsAndCounters(int width)
    {
        using var s = new CompactProgressScene(width: width); s.Prepare(); s.Pair(); s.Tick(1);
        AssertSpacing(Physical(s)); Assert.Contains("1/2 files", s.T.Screen); Assert.Contains("0/1 completed/started", s.T.Screen);
        s.T.Width = Math.Max(40, width - 1); s.Tick(1); AssertSpacing(Physical(s));
        Assert.All(s.T.Writes, row => Assert.True(row.Text.Length < s.T.BufferWidth)); s.Orange(1);
    }

    [Fact]
    public void MonochromeHasIdenticalSpacingAndReadableHeadings()
    {
        using var color = new CompactProgressScene(); using var mono = new CompactProgressScene(color: false);
        foreach (var s in new[] { color, mono }) { s.Prepare(); s.Pair(); AssertSpacing(Physical(s)); }
        Assert.Equal(Physical(color), Physical(mono)); Assert.All(mono.Frame(), r => Assert.Equal(ProgressTone.Neutral, r.Tone));
        Assert.False(ProgressColorPolicy.Enable(false, false, "1", null)); Assert.DoesNotContain('\u001b', mono.T.Screen);
    }

    [Fact]
    public void TextFallbackSnapshotRetainsSeparatorsWithoutAnsiOrRepeatedRefreshes()
    {
        TimeSpan now = TimeSpan.Zero; var output = new StringWriter(); var terminal = new ProgressTestTerminal(width: 120) { SupportsColor = true };
        using var p = new RunProgress(output, true, true, () => now, startTimer: false, terminal: terminal);
        p.Begin(RunPhase.Loading); p.Complete(RunPhase.Loading); p.Begin(RunPhase.Documentation);
        terminal.Available = false; now += TimeSpan.FromSeconds(1); p.Refresh();
        string text = output.ToString(); int snapshot = text.IndexOf("XMLDocNormalizer", StringComparison.Ordinal);
        Assert.True(snapshot >= 0); AssertSpacing(text[snapshot..].Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
        Assert.DoesNotContain('\u001b', text); p.Refresh(); p.Refresh(); Assert.Equal(text, output.ToString());
    }

    [Fact]
    public void ConsoleMessagesReanchorTheSameSeparatedAreas()
    {
        using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); var stdout = Console.Out;
        try
        {
            Console.SetOut(new CompactProgressScene.ScreenWriter(s.T)); s.P.WithOutput(() => Console.WriteLine("ordinary log"));
            Assert.Contains("ordinary log", s.T.EntireScreen); AssertSpacing(Physical(s).Skip(1).ToArray()); s.Orange(1); s.NoResults();
        }
        finally { Console.SetOut(stdout); }
    }

    private static string[] Physical(CompactProgressScene s) => s.T.Screen.Split('\n').Take(s.T.Row).Select(row => row.TrimEnd()).ToArray();
    private static int AssertSpacing(string[] rows)
    {
        var headers = rows.Select((text, row) => (text, row)).Where(r => Headings.Contains(r.text)).ToArray();
        Assert.NotEmpty(headers); Assert.Equal(headers.Length - 1, rows.Count(string.IsNullOrEmpty));
        Assert.NotEmpty(rows[0]); Assert.NotEmpty(rows[^1]);
        for (int i = 0; i < headers.Length; i++)
        {
            int end = i + 1 < headers.Length ? headers[i + 1].row - 1 : rows.Length;
            Assert.All(rows[(headers[i].row + 1)..end], row => Assert.NotEmpty(row));
            if (i + 1 < headers.Length) Assert.Empty(rows[end]);
        }
        return headers.Length;
    }
}
