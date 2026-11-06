using System.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressRegionTests
{
    [Fact]
    public void InterleavedPhasesKeepBothSectionsAndTheirOwnFileVisible()
    {
        TimeSpan now = TimeSpan.Zero; var terminal = new ProgressTestTerminal();
        using var progress = new RunProgress(new StringWriter(), true, false, () => now, startTimer: false, terminal: terminal, mode: "Direct");
        progress.SetTotal(RunPhase.Documentation, 2);
        progress.Begin(RunPhase.Documentation, "First.cs");
        now = TimeSpan.FromSeconds(3); progress.Begin(RunPhase.ExceptionFlow);
        progress.Observe(new(ExceptionFlowProgressKind.RootStarted));
        now = TimeSpan.FromSeconds(13); progress.Begin(RunPhase.Documentation, "Second.cs");
        Assert.Contains("Documentation:", terminal.Screen);
        Assert.Contains("[>] Analyze", terminal.Screen);
        Assert.Contains("Current:", terminal.Screen);
        Assert.Contains("Roots     : 0 completed / 1 started", terminal.Screen);
        Assert.Contains("00:13", terminal.Screen);
        Assert.DoesNotContain("finished", terminal.Screen);
        Assert.Equal(12, terminal.Reserved);
        now = TimeSpan.FromSeconds(14); progress.Begin(RunPhase.ExceptionFlow);
        Assert.Contains("Current:", terminal.Screen);
    }

    [Fact]
    public void RefreshWritesOnlyChangedRowsAndDoesNotMoveTheCursorOrReserveAgain()
    {
        TimeSpan now = TimeSpan.Zero; var terminal = new ProgressTestTerminal();
        using var progress = new RunProgress(new StringWriter(), true, false, () => now, startTimer: false, terminal: terminal);
        progress.Begin(RunPhase.ExceptionFlow);
        terminal.Writes.Clear(); int moves = terminal.CursorMoves;
        now = TimeSpan.FromMilliseconds(500); progress.Refresh();
        Assert.Empty(terminal.Writes);
        now = TimeSpan.FromSeconds(1); progress.Refresh();
        Assert.Equal(3, terminal.Writes.Count); // activity age, active scope time, total time
        Assert.DoesNotContain(terminal.Writes, row => string.IsNullOrWhiteSpace(row.Text));
        Assert.Equal(moves, terminal.CursorMoves); Assert.Equal(14, terminal.Reserved);
        terminal.Writes.Clear();
        progress.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 7));
        progress.Observe(new(ExceptionFlowProgressKind.ContextProcessed, 2));
        now += TimeSpan.FromMilliseconds(500); progress.Refresh();
        Assert.Contains(terminal.Writes, row => row.Text.Contains("Workitems : 2 processed", StringComparison.Ordinal)); // genuine events, not refreshes, change counters
        Assert.Contains(terminal.Writes, row => row.Text.Contains("Contexts  : 7 discovered", StringComparison.Ordinal));
        Assert.Contains("Contexts  : 7 discovered", terminal.Screen);
    }

    [Fact]
    public void ShorterRowsClearOnlyTheirOwnTail()
    {
        var terminal = new ProgressTestTerminal(); var region = new ProgressRegion(terminal, 3);
        Assert.True(region.Render(["abcdefgh", "keep", "third"])); terminal.Writes.Clear();
        Assert.True(region.Render(["xy", "keep", "third"]));
        Assert.Equal((0, "xy      "), Assert.Single(terminal.Writes));
        Assert.Equal("xy\nkeep\nthird\n", terminal.Screen);
        Assert.Equal(3, terminal.Reserved);
    }

    [Fact]
    public void EmptyAndFileOnlyReportActionsLeaveEveryRowUntouched()
    {
        var terminal = new ProgressTestTerminal();
        using var progress = new RunProgress(new StringWriter(), true, false, startTimer: false, terminal: terminal);
        progress.Begin(RunPhase.Documentation); terminal.Writes.Clear();
        int moves = terminal.CursorMoves, reserved = terminal.Reserved; string screen = terminal.Screen;
        progress.WithOutput(() => { Console.Out.Write(""); Console.Error.Write((string?)null); });
        Assert.Empty(terminal.Writes); Assert.Equal(moves, terminal.CursorMoves);
        Assert.Equal(reserved, terminal.Reserved); Assert.Equal(screen, terminal.Screen);
    }

    [Fact]
    public void RealAndNestedConsoleMessagesRemainSeparateFromTheRestoredRegion()
    {
        var terminal = new ProgressTestTerminal(); using var stdout = new ScreenWriter(terminal);
        var originalOut = Console.Out; var originalError = Console.Error;
        using var progress = new RunProgress(new StringWriter(), true, false, startTimer: false, terminal: terminal);
        try
        {
            Console.SetOut(stdout); progress.Begin(RunPhase.Documentation, "A-long-file-name.cs");
            progress.WithOutput(() => {
                Console.WriteLine("log");
                progress.WithOutput(() => Console.WriteLine("nested"));
            });
            Assert.StartsWith("log\nnested\nXMLDocNormalizer", terminal.Screen);
            Assert.Contains("Documentation:", terminal.Screen);
            Assert.Contains("[ ] Exception flow", terminal.Screen);
            Assert.Equal(22, terminal.Reserved); // original and restored 11-row region, including one separator
            Assert.DoesNotContain("logDoc", terminal.Screen);
            Assert.Same(originalError, Console.Error);
        }
        finally { Console.SetOut(originalOut); }
    }

    [Fact]
    public void ThrowingConsoleActionRestoresWritersAndDashboard()
    {
        var terminal = new ProgressTestTerminal(); using var stdout = new ScreenWriter(terminal);
        var originalOut = Console.Out; var originalError = Console.Error;
        using var progress = new RunProgress(new StringWriter(), true, false, startTimer: false, terminal: terminal);
        try
        {
            Console.SetOut(stdout); var expectedOut = Console.Out;
            progress.Begin(RunPhase.Documentation);
            Assert.Throws<IOException>(() => progress.WithOutput(() => { Console.WriteLine("before failure"); throw new IOException(); }));
            Assert.Same(expectedOut, Console.Out); Assert.Same(originalError, Console.Error);
            Assert.Contains("Documentation:", terminal.Screen);
        }
        finally { Console.SetOut(originalOut); }
    }

    [Theory]
    [InlineData(1, 40)] [InlineData(31, 40)] [InlineData(100, 5)]
    public void UnsuitableDimensionsUseBoundedFallback(int width, int height)
    {
        var writer = new StringWriter(); var terminal = new ProgressTestTerminal(writer, width) { Height = height };
        TimeSpan now = TimeSpan.Zero;
        using var progress = new RunProgress(writer, true, false, () => now, startTimer: false, terminal: terminal);
        progress.Begin(RunPhase.Documentation);
        for (int i = 0; i < 1000; i++) { now += TimeSpan.FromSeconds(1); progress.Refresh(); }
        Assert.Empty(terminal.Writes); Assert.Equal(0, terminal.Reserved);
        Assert.Equal(12, writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        progress.Finish(failed: true);
        Assert.Contains("Execution failed", writer.ToString());
    }

    [Fact]
    public void LostCursorSupportAndBufferReflowStopNativeUpdatesPermanently()
    {
        foreach (bool reflow in new[] { false, true })
        {
            TimeSpan now = TimeSpan.Zero; var writer = new StringWriter(); var terminal = new ProgressTestTerminal(writer);
            using var progress = new RunProgress(writer, true, false, () => now, startTimer: false, terminal: terminal);
            progress.Begin(RunPhase.Documentation); terminal.Writes.Clear();
            if (reflow) terminal.BufferWidth = terminal.Width = 100; else terminal.Available = false;
            now += TimeSpan.FromSeconds(1); progress.Refresh();
            int length = writer.ToString().Length;
            terminal.Available = true;
            for (int i = 0; i < 100; i++) { now += TimeSpan.FromSeconds(1); progress.Refresh(); }
            Assert.Empty(terminal.Writes); Assert.Equal(length, writer.ToString().Length);
            Assert.Contains("Live display unavailable", writer.ToString());
        }
    }

    [Fact]
    public void WindowResizeClipsRowsAndClearsPreviouslyVisibleTailsWithoutWrapping()
    {
        var terminal = new ProgressTestTerminal(width: 120); var region = new ProgressRegion(terminal, 2);
        Assert.True(region.Render([new string('a', 110), "short"])); terminal.Width = 40;
        terminal.Writes.Clear(); Assert.True(region.Render([new string('b', 110), "short"]));
        Assert.All(terminal.Writes, row => Assert.True(row.Text.TrimEnd().Length <= 39));
        Assert.DoesNotContain('a', terminal.Screen);
        terminal.Width = 120; Assert.True(region.Render(["x", "short"]));
        Assert.StartsWith("x\nshort", terminal.Screen);
    }

    [Fact]
    public void LiveRowsUsePredictableCellsForUnicodeAndControlCharacters()
    {
        Assert.Equal("a??? b", ProgressRegion.Fit("a漢😀\u0301\nb", 6));
        var terminal = new ProgressTestTerminal(width: 40); var region = new ProgressRegion(terminal, 1);
        Assert.True(region.Render(["漢😀\u0301"])); Assert.True(region.Render(["x"]));
        Assert.Equal("x\n", terminal.Screen); Assert.DoesNotContain('\u001b', terminal.Screen);
    }

    [Fact]
    public void VerboseRowsRemainInPlaceAndFinalFailureRetainsTheDashboard()
    {
        TimeSpan now = TimeSpan.Zero; var terminal = new ProgressTestTerminal();
        using var progress = new RunProgress(new StringWriter(), true, true, () => now, startTimer: false, terminal: terminal);
        progress.Begin(RunPhase.Documentation); progress.Detector("Param");
        progress.Begin(RunPhase.ExceptionFlow); progress.Observe(new(ExceptionFlowProgressKind.RootStarted));
        progress.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 7));
        now = TimeSpan.FromSeconds(2); progress.Refresh();
        Assert.Contains("Current: Exception flow", terminal.Screen);
        Assert.DoesNotContain("Detector: Param", terminal.Screen);
        now = TimeSpan.FromSeconds(4); progress.Refresh();
        Assert.Contains("CURRENT WORK", terminal.Screen);
        Assert.DoesNotContain("Findings", terminal.Screen);
        progress.Finish(failed: true);
        Assert.Contains("[!!] Execution failed", terminal.Screen); Assert.Contains("failed", terminal.Screen);
        Assert.DoesNotContain("Contexts  : 7 discovered", terminal.Screen); Assert.DoesNotContain("Documentation:", terminal.Screen);
        int writes = terminal.Writes.Count; progress.Refresh(); Assert.Equal(writes, terminal.Writes.Count);
        Assert.Equal(14, terminal.Reserved);
    }

    private sealed class ScreenWriter(ProgressTestTerminal terminal) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value) => terminal.WriteConsole(value.ToString());
        public override void Write(string? value) { if (value is not null) terminal.WriteConsole(value); }
    }
}
