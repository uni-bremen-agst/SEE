using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressFinalizationTests
{
    [Fact]
    public void FinalizationBeginningRendersImmediatelyInsideTheRefreshWindow()
    {
        using var s = new Scene(); s.T.Writes.Clear(); s.P.BeginFinalization();
        Assert.Contains(s.T.Writes, row => row.Text.StartsWith("[>] Finalize and report"));
        s.Live(); Assert.Empty(s.Pauses);
    }

    [Fact]
    public void ExistingPerFileOutputCannotSuppressTheFinalMainStepBeginning()
    {
        using var s = new Scene(ready: false); s.P.Begin(RunPhase.Output); s.Ready();
        s.T.Writes.Clear(); s.P.BeginFinalization(); s.P.Begin(RunPhase.Output);
        s.Live(); Assert.Contains(s.T.Writes, row => row.Text.StartsWith("[>] Finalize and report"));
    }

    [Fact]
    public void VeryShortFinalizationRetainsTheVisibleFrameFor1000Milliseconds()
    {
        using var s = new Scene(); s.Start(); s.Now += TimeSpan.FromMilliseconds(5); s.End();
        Assert.Equal(TimeSpan.FromMilliseconds(995), Assert.Single(s.Pauses)); s.Done("00:00");
    }

    [Fact]
    public void ActualCompletionAndElapsedTimeAreFrozenBeforePresentationPause()
    {
        using var s = new Scene(); s.Start(); s.Now += TimeSpan.FromMilliseconds(20);
        s.OnPause = _ =>
        {
            s.Live(); int writes = s.T.Writes.Count;
            s.P.Complete(RunPhase.Output); // A second completion is already a no-op, even before the final paint.
            Assert.Equal(writes, s.T.Writes.Count);
            s.Now += TimeSpan.FromSeconds(2); // Simulated scheduling overshoot must never extend the work time.
        };
        s.End(); s.Done("00:00"); Assert.Equal(TimeSpan.FromMilliseconds(980), Assert.Single(s.Pauses));
        Assert.EndsWith("00:00", Assert.Single(s.Frame(), row => row.Text.StartsWith("XMLDocNormalizer")).Text);
    }

    [Fact]
    public void TimeAlreadyVisibleIsSubtractedFromThePresentationPause()
    {
        using var s = new Scene(); s.Start(); s.Now += TimeSpan.FromMilliseconds(120); s.End();
        Assert.Equal(TimeSpan.FromMilliseconds(880), Assert.Single(s.Pauses));
    }

    [Theory]
    [InlineData(1000)] [InlineData(1200)]
    public void HumanVisibleFinalizationDoesNotAddAnyPause(int milliseconds)
    {
        using var s = new Scene(); s.Start(); s.Now += TimeSpan.FromMilliseconds(milliseconds); s.End();
        Assert.Empty(s.Pauses); s.Done("00:01");
    }

    [Fact]
    public void ProvenCompletionEndsGreenExactlyOnceAndFinishKeepsItsTime()
    {
        using var s = new Scene(); s.Start(); s.End(); var saved = s.Done("00:00");
        s.Now += TimeSpan.FromSeconds(5); s.P.Finish(); Assert.Equal(saved, s.Done("00:00"));
        Assert.Single(s.Pauses); Assert.DoesNotContain("CURRENT WORK", s.T.Screen);
    }

    [Fact]
    public void FinishWithoutOutputCompletionCannotInventCompletionOrPause()
    {
        using var s = new Scene(); s.Start(); s.P.Finish();
        Assert.Empty(s.Pauses); Assert.DoesNotContain(s.Frame(), row => row.Text.StartsWith("[ok] Finalize and report"));
        Assert.Contains("Interrupted", s.T.Screen);
    }

    [Fact]
    public void FailedFinalizationNeverWaitsOrBecomesGreen()
    {
        using var s = new Scene(); s.Start(); s.P.Finish(failed: true);
        Assert.Empty(s.Pauses); Assert.DoesNotContain(s.Frame(), row => row.Text.StartsWith("[ok] Finalize and report"));
        Assert.Contains(s.Frame(), row => row.Tone == ProgressTone.Error);
    }

    [Fact]
    public void NonInteractiveProgressNeverCallsThePresentationDelay()
    {
        using var s = new Scene(interactive: false); s.Start(); s.End(); s.P.Finish();
        Assert.Empty(s.Pauses); Assert.Empty(s.T.Writes);
        Assert.Contains("Completed | [ok] Finalize and report", s.Output.ToString());
    }

    [Fact]
    public void DisabledProgressCreatesNoPresenter()
    {
        Assert.True(ArgParsing.TryParseOptions(["--check", "--progress", "off", Path.GetTempPath()], out var options));
        Assert.Null(RunProgress.Create(options!));
    }

    [Fact]
    public void LostConsoleOrChangedGeometrySkipsThePause()
    {
        foreach (bool geometry in new[] { false, true })
        {
            using var s = new Scene(); s.Start();
            if (geometry) s.T.BufferWidth = s.T.Width = 100; else s.T.Available = false;
            s.End(); Assert.Empty(s.Pauses); Assert.Contains("Live display unavailable", s.Output.ToString());
        }
    }

    [Fact]
    public void InlineReportingDoesNotIntroduceAFinalizationOrAPause()
    {
        using var s = new Scene(); s.P.UseInlineReporting(); s.P.BeginFinalization(); s.P.Begin(RunPhase.Output); s.End(); s.P.Finish();
        Assert.Empty(s.Pauses); Assert.DoesNotContain(s.Frame(), row => row.Text.Contains("Finalize and report"));
    }

    [Fact]
    public void CounterRefreshesRetainTheExisting500MillisecondThrottle()
    {
        using var s = new Scene(ready: false); s.P.Begin(RunPhase.ExceptionFlow); s.T.Writes.Clear();
        s.P.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 7)); s.Now += TimeSpan.FromMilliseconds(100); s.P.Refresh();
        Assert.Empty(s.T.Writes); s.Now += TimeSpan.FromMilliseconds(400); s.P.Refresh();
        Assert.Contains(s.T.Writes, row => row.Text.Contains("Contexts  : 7 discovered")); Assert.Empty(s.Pauses);
    }

    [Fact]
    public void ReportingForAtLeastOneSecondDoesNotAddAPauseAfterReanchoring()
    {
        using var s = new Scene(); s.Start(); bool reported = false; var stdout = Console.Out;
        try
        {
            Console.SetOut(new CompactProgressScene.ScreenWriter(s.T));
            s.P.WithOutput(() => { Console.WriteLine("real report"); s.Now += TimeSpan.FromSeconds(2); reported = true; });
            s.OnPause = _ => throw new InvalidOperationException("Long finalization must not pause.");
            s.End(); Assert.True(reported); Assert.Contains("real report", s.T.EntireScreen);
            Assert.Empty(s.Pauses); s.Done("00:02");
        }
        finally { Console.SetOut(stdout); }
    }

    [Fact]
    public void VisibleTimeBeforeAndAfterConsoleOutputIsCredited()
    {
        using var s = new Scene(); s.Start(); var stdout = Console.Out;
        try
        {
            s.Now += TimeSpan.FromMilliseconds(200);
            Console.SetOut(new CompactProgressScene.ScreenWriter(s.T));
            s.P.WithOutput(() => { Console.WriteLine("brief report"); s.Now += TimeSpan.FromMilliseconds(100); });
            s.Now += TimeSpan.FromMilliseconds(100); s.End();
            Assert.Equal(TimeSpan.FromMilliseconds(700), Assert.Single(s.Pauses)); s.Done("00:00");
            Assert.EndsWith("00:00", Assert.Single(s.Frame(), row => row.Text.StartsWith("XMLDocNormalizer")).Text);
        }
        finally { Console.SetOut(stdout); }
    }

    private sealed class Scene : IDisposable
    {
        internal TimeSpan Now;
        internal readonly ProgressTestTerminal T = new(width: 120) { SupportsColor = true };
        internal readonly StringWriter Output = new();
        internal readonly List<TimeSpan> Pauses = [];
        internal Action<TimeSpan>? OnPause;
        internal RunProgress P { get; }
        internal Scene(bool interactive = true, bool ready = true)
        {
            P = new(Output, interactive, false, () => Now, startTimer: false, terminal: T,
                displayPause: duration => { Pauses.Add(duration); OnPause?.Invoke(duration); Now += duration; });
            if (ready) Ready();
        }
        internal void Ready()
        {
            P.Begin(RunPhase.Loading); P.Complete(RunPhase.Loading); P.Begin(RunPhase.Compilation); P.Complete(RunPhase.Compilation);
            P.SetTotal(RunPhase.Documentation, 1); P.Begin(RunPhase.Documentation); P.DocumentCompleted();
            P.Begin(RunPhase.ExceptionFlow); P.Observe(new(ExceptionFlowProgressKind.RootStarted)); P.Observe(new(ExceptionFlowProgressKind.RootCompleted));
            P.CompleteExceptionFile(); P.Complete(RunPhase.Aggregation); P.CompleteAnalysis();
        }
        internal void Start() { P.BeginFinalization(); P.Begin(RunPhase.Output); }
        internal void End() => P.Complete(RunPhase.Output);
        internal (int Row, string Text, ProgressTone Tone)[] Frame() => T.StyledWrites.GroupBy(row => row.Row).Select(g => g.Last()).OrderBy(row => row.Row).Select(row => (row.Row, row.Text.TrimEnd(), row.Tone)).ToArray();
        internal void Live() => Assert.Contains(Frame(), row => row.Text.StartsWith("[>] Finalize and report") && row.Tone == ProgressTone.Active);
        internal (int Row, string Text, ProgressTone Tone) Done(string elapsed)
        {
            var row = Assert.Single(Frame(), row => row.Text.StartsWith("[ok] Finalize and report"));
            Assert.Equal(ProgressTone.Completed, row.Tone); Assert.EndsWith(elapsed, row.Text); return row;
        }
        public void Dispose() => P.Dispose();
    }
}
