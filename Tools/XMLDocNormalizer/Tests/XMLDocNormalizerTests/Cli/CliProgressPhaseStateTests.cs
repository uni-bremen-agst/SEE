using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressPhaseStateTests
{
    [Fact] public void InitialFrameShowsEveryPlannedMainPhasePendingAndGray()
    { using var s = new CompactProgressScene(); foreach (var label in new[] { "Load inputs", "Prepare compilations", "Analyze", "Finalize and report" }) s.Pending(label); Assert.Empty(s.Area("CURRENT WORK")); }
    [Fact] public void StartingLoadDoesNotStartAnyOtherPhase()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.Live("Load inputs"); foreach (var label in new[] { "Prepare compilations", "Analyze", "Finalize and report" }) s.Pending(label); s.Orange(1); }
    [Fact] public void LoadTurnsGreenImmediatelyBeforeCompilationStarts()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.Tick(1); s.P.Complete(RunPhase.Loading); s.Done("Load inputs", "00:01"); s.Pending("Prepare compilations"); }
    [Fact] public void CompilationCannotChangeTheCompletedLoadSection()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.Tick(1); s.P.Complete(RunPhase.Loading); var saved = s.Done("Load inputs"); s.P.Begin(RunPhase.Compilation); s.Tick(9); Assert.Equal(saved, s.Done("Load inputs")); s.Live("Prepare compilations"); }
    [Fact] public void CompilationTurnsGreenBeforeEitherAnalysisFinishes()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Compilation); s.Tick(2); s.P.Complete(RunPhase.Compilation); s.Done("Prepare compilations", "00:02"); s.Pending("Analyze"); Assert.DoesNotContain("Documentation", s.Text("COMPLETED")); }
    [Fact] public void InterleavedAnalysesRemainRunningAcrossWorkSwitches()
    { using var s = new CompactProgressScene(); s.Pair(); foreach (var phase in new[] { RunPhase.Documentation, RunPhase.ExceptionFlow }) { s.P.Begin(phase); s.Tick(1); s.Live("Analyze"); Assert.Contains("1/2 files", s.Text("CURRENT WORK")); Assert.Contains("0 completed / 1 started", s.Text("CURRENT WORK")); Assert.DoesNotContain("paused", s.T.Screen); } }
    [Fact] public void APhaseSwitchIsNotACompletionProof()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.P.Begin(RunPhase.Compilation); s.Tick(20); Assert.DoesNotContain("Load inputs", s.Text("COMPLETED")); Assert.Contains("Load inputs: continuing", s.Text("CURRENT WORK")); s.Orange(1); }
    [Fact] public void LastDocumentTurnsOnlyDocumentationGreenWithoutWaitingForFinish()
    { using var s = new CompactProgressScene(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted(); s.Done("Documentation", "2/2 files"); Assert.Contains("0 completed / 1 started", s.Text("CURRENT WORK")); Assert.DoesNotContain("Documentation", s.Text("CURRENT WORK")); }
    [Fact] public void EqualRootCountsDoNotCompleteTheUnfinishedExceptionScope()
    { using var s = new CompactProgressScene(); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.RootCompleted)); s.P.CompleteExceptionFile(); s.Tick(20); Assert.Contains("1 completed / 1 started", s.Text("CURRENT WORK")); Assert.DoesNotContain("Exception flow", s.Text("COMPLETED")); }
    [Fact] public void LastExceptionFileTurnsFlowGreenBeforeFinalizationBegins()
    { using var s = new CompactProgressScene(); s.EndParts(); s.Done("Exception flow", "1/1 roots"); s.Pending("Finalize and report"); Assert.DoesNotContain("Last activity", s.T.EntireScreen); }
    [Fact] public void IntermediateAggregationAndOutputDoNotCompleteTheMainAnalysis()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Aggregation); s.Tick(1); s.P.Complete(RunPhase.Aggregation); s.Live("Analyze"); s.Pending("Finalize and report"); s.P.Begin(RunPhase.Output); s.Tick(2); s.P.Complete(RunPhase.Output); s.Live("Analyze"); s.Pending("Finalize and report"); }
    [Fact] public void CompletedSectionsRetainCoordinatesTextColorsTimesAndCounters()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted(); var docs = s.Done("Documentation", "2/2 files"); s.P.Begin(RunPhase.ExceptionFlow); s.P.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 7)); s.Tick(4); s.P.SetTotal(RunPhase.Documentation, 99); s.P.DocumentCompleted(); s.P.Begin(RunPhase.Documentation, "Must-not-reappear.cs"); s.Tick(20); Assert.Equal(docs, s.Done("Documentation")); s.P.Observe(new(ExceptionFlowProgressKind.RootCompleted)); s.P.CompleteExceptionFile(); var flow = s.Done("Exception flow"); s.P.Begin(RunPhase.Aggregation); s.P.Observe(new(ExceptionFlowProgressKind.ContextDiscovered, 99)); s.Tick(20); Assert.Equal(flow, s.Done("Exception flow")); Assert.Equal(docs, s.Done("Documentation")); Assert.Contains("Contexts: 7; workitems: 0", s.Text("COMPLETED")); }
    [Fact] public void RootFailureIsImmediatelyRedAndCanNeverBecomeGreen()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.ExceptionFlow); s.P.Observe(new(ExceptionFlowProgressKind.RootFailed)); Assert.Contains(s.Frame(), r => r.Tone == ProgressTone.Error); s.P.Complete(RunPhase.ExceptionFlow); s.P.Begin(RunPhase.ExceptionFlow); s.Tick(2); Assert.DoesNotContain("Exception flow", s.Text("COMPLETED")); Assert.Contains(s.Frame(), r => r.Tone == ProgressTone.Error); }
    [Fact] public void PrematureFinishInterruptsRemainingWorkWithoutInventingCompletions()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.P.Complete(RunPhase.Loading); s.Pair(); s.P.Finish(failed: true); s.Done("Load inputs"); Assert.DoesNotContain("Documentation", s.Text("COMPLETED")); Assert.DoesNotContain("Exception flow", s.Text("COMPLETED")); s.Pending("Finalize and report"); Assert.Contains(s.Frame(), r => r.Tone == ProgressTone.Error); }
    [Fact] public void SuccessfulFinishDoesNotPromoteRunningPhasesToCompleted()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Documentation); s.P.Finish(); Assert.Contains("Interrupted: Documentation", s.T.Screen); Assert.DoesNotContain("Documentation", s.Text("COMPLETED")); s.Pending("Prepare compilations"); }
    [Fact] public void UnavailableDocumentsEndAsInterruptedRatherThanGreenOrTechnicalFailure()
    { using var s = new CompactProgressScene(); s.P.SetTotal(RunPhase.Documentation, 2); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted(); s.P.EndAnalysis(); Assert.Contains("Interrupted: Documentation, Exception flow", s.T.Screen); Assert.DoesNotContain("Documentation", s.Text("COMPLETED")); Assert.DoesNotContain("[!!]", s.T.Screen); }
    [Fact] public void VerboseEventsAndRefreshesCannotChangeCompletedStatesOrColors()
    { using var s = new CompactProgressScene(verbose: true); s.Prepare(); var load = s.Done("Load inputs"); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.Admission)); s.P.Observe(new(ExceptionFlowProgressKind.DefinitionDiscovered, 5)); s.P.Activity("detail"); s.P.Detector("ignored while flow owns current work"); s.Tick(4); Assert.Equal(load, s.Done("Load inputs")); s.Live("Analyze"); Assert.Contains("Admission:", s.Text("CURRENT WORK")); Assert.DoesNotContain("ignored", s.T.Screen); }
    [Fact] public void RealConsoleOutputPreservesCompletedContentAndOrder()
    { using var s = new CompactProgressScene(); s.Prepare(); var saved = s.Done("Load inputs"); var stdout = Console.Out; try { Console.SetOut(new CompactProgressScene.ScreenWriter(s.T)); s.P.WithOutput(() => Console.WriteLine("ordinary log")); var moved = s.Done("Load inputs"); Assert.Equal((saved.Text, saved.Tone), (moved.Text, moved.Tone)); Assert.Contains("ordinary log", s.T.EntireScreen); Assert.True(moved.Row < s.Done("Prepare compilations").Row); } finally { Console.SetOut(stdout); } }
    [Fact] public void FallbackPublishesTheSameImmediateTransitionsAndOnlyOnce()
    { TimeSpan now = TimeSpan.Zero; var output = new StringWriter(); using var p = new RunProgress(output, false, false, () => now, startTimer: false); p.Refresh(); Assert.Equal(4, output.ToString().Split("Pending |", StringSplitOptions.None).Length - 1); p.Begin(RunPhase.Loading); now += TimeSpan.FromSeconds(2); p.Complete(RunPhase.Loading); Assert.Contains("Completed | [ok] Load inputs; 00:02", output.ToString()); var saved = output.ToString(); p.Refresh(); p.Refresh(); Assert.Equal(saved, output.ToString()); p.Begin(RunPhase.Compilation); Assert.Contains("Current | [>] Prepare compilations", output.ToString()); Assert.Equal(1, output.ToString().Split("Completed | [ok] Load inputs").Length - 1); }
    [Fact] public void PendingZeroWorkScopesCompleteOnlyAtTheExplicitScopeBoundary()
    { using var s = new CompactProgressScene(); s.P.SetTotal(RunPhase.Documentation, 0); s.Tick(1); Assert.DoesNotContain("Documentation", s.Text("COMPLETED")); s.P.EndAnalysis(); s.Done("Documentation", "0/0 files"); s.Done("Exception flow", "0/0 roots"); }
    [Theory] [InlineData(32, 12)] [InlineData(40, 6)] [InlineData(79, 6)]
    public void InsufficientRoomUsesCompleteTextTransitionsInsteadOfMovingOrClippingSlots(int width, int height)
    { var terminal = new ProgressTestTerminal(width: width) { Height = height, SupportsColor = true }; var output = new StringWriter(); using var p = new RunProgress(output, true, true, () => TimeSpan.Zero, startTimer: false, terminal: terminal); p.Begin(RunPhase.Loading); p.Complete(RunPhase.Loading); p.Begin(RunPhase.Compilation); Assert.Empty(terminal.Writes); Assert.Contains("Completed | [ok] Load inputs", output.ToString()); Assert.Contains("Current | [>] Prepare compilations", output.ToString()); Assert.DoesNotContain('\u001b', output.ToString()); var saved = output.ToString(); p.Refresh(); Assert.Equal(saved, output.ToString()); }
}