using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Cli.Output;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressMainPhaseTests
{
    [Fact] public void FourPendingMainStepsAreGrayAndNeverOrange()
    { using var s = new CompactProgressScene(); foreach (var label in new[] { "Load inputs", "Prepare compilations", "Analyze", "Finalize and report" }) s.Pending(label); s.Orange(0); Assert.DoesNotContain("00:00", s.Text("PENDING")); }
    [Fact] public void LoadCompletesGreenBeforePreparationBecomesTheOnlyOrangeStep()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.Orange(1); s.Tick(1); s.P.Complete(RunPhase.Loading); var saved = s.Done("Load inputs", "00:01"); s.P.Begin(RunPhase.Compilation); s.Tick(2); Assert.Equal(saved, s.Done("Load inputs")); s.Live("Prepare compilations"); s.P.Complete(RunPhase.Compilation); s.Done("Prepare compilations", "00:02"); s.Orange(0); }
    [Fact] public void InterleavedPartsShareOneOrangeMainPhaseAndNeutralLiveParts()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); foreach (var phase in new[] { RunPhase.Documentation, RunPhase.ExceptionFlow }) { s.P.Begin(phase); s.Tick(1); s.Live("Analyze"); s.Orange(1); Assert.DoesNotContain(s.Area("CURRENT WORK"), r => r.Tone == ProgressTone.Completed); } }
    [Fact] public void DocumentationTurnsGreenWhileFlowAndTheCommonPhaseContinue()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Documentation); s.P.DocumentCompleted(); s.Done("Documentation", "2/2 files"); Assert.DoesNotContain("Documentation", s.Text("CURRENT WORK")); s.P.Begin(RunPhase.ExceptionFlow); s.Tick(1); s.Live("Exception flow"); s.Orange(1); }
    [Fact] public void FlowMayCompleteFirstWithoutClosingDocumentationOrTheMainPhase()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.RootCompleted)); s.P.Complete(RunPhase.ExceptionFlow); s.Done("Exception flow", "1/1 roots"); s.P.Begin(RunPhase.Documentation); s.Tick(1); s.Live("Documentation"); Assert.Contains("1/2 files", s.Text("CURRENT WORK")); s.Orange(1); }
    [Fact] public void IntermediateAggregationDoesNotStartFinalization()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Aggregation); s.Tick(1); s.Live("Analyze"); s.Pending("Finalize and report"); s.Orange(1); Assert.Contains("Current: Intermediate finding aggregation", s.T.Screen); }
    [Fact] public void FileReportingDoesNotStartFinalization()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Begin(RunPhase.Output); s.Tick(1); s.Pending("Finalize and report"); s.Live("Analyze"); Assert.Contains("Current: File reporting within analysis", s.T.Screen); }
    [Fact] public void ReturningFromFileOutputDoesNotReopenAnyCompletedMainStep()
    { using var s = new CompactProgressScene(); s.Prepare(); var saved = s.Done("Prepare compilations"); s.Pair(); foreach (var phase in new[] { RunPhase.Aggregation, RunPhase.Output, RunPhase.Documentation, RunPhase.ExceptionFlow }) { s.P.Begin(phase); s.Tick(1); s.Orange(1); Assert.Equal(saved, s.Done("Prepare compilations")); } }
    [Fact] public void LateCompilationIsNotFalselyDeclaredCompleted()
    { using var s = new CompactProgressScene(); s.P.Begin(RunPhase.Loading); s.P.Complete(RunPhase.Loading); s.P.Begin(RunPhase.Compilation); s.Pair(); Assert.Contains("Prepare compilations: continuing", s.Text("CURRENT WORK")); Assert.DoesNotContain("Prepare compilations", s.Text("COMPLETED")); s.P.BeginFinalization(); s.Pending("Finalize and report"); s.P.Begin(RunPhase.Compilation); s.Tick(2); s.P.Complete(RunPhase.Compilation); s.Done("Prepare compilations"); }
    [Fact] public void CurrentDetectorUsesExistingDataAndUnknownFlowHasAnHonestFallback()
    { using var s = new CompactProgressScene(); s.Prepare(); s.P.SetTotal(RunPhase.Documentation, 2); s.P.Begin(RunPhase.Documentation); s.P.Detector("XmlDocParamDetector"); s.Tick(1); Assert.Contains("Current: Documentation check - XmlDocParamDetector", s.T.Screen); s.P.Begin(RunPhase.ExceptionFlow); s.Tick(1); Assert.Contains("Current: Exception flow", s.T.Screen); Assert.DoesNotContain("Current: Exception flow -", s.T.Screen); Assert.DoesNotContain("XmlDocParamDetector", s.T.EntireScreen); }
    [Fact] public void FinalizationRequiresAnExplicitActualBoundaryAndCompletesIndependently()
    { using var s = new CompactProgressScene(); s.Prepare(); s.EndParts(); s.P.Begin(RunPhase.Aggregation); s.P.Complete(RunPhase.Aggregation); s.P.CompleteAnalysis(); s.Pending("Finalize and report"); Assert.Empty(s.Area("CURRENT WORK")); var docs = s.Done("Documentation"); s.P.BeginFinalization(); s.Live("Finalize and report"); s.Tick(2); Assert.Equal(docs, s.Done("Documentation")); s.P.Begin(RunPhase.Output); s.P.Complete(RunPhase.Output); s.Done("Finalize and report", "00:02"); s.Orange(0); }
    [Fact] public void CompletedMainStepsKeepExactRowsColorsTimesAndTextAcrossRefreshes()
    { using var s = new CompactProgressScene(); s.Prepare(); var load = s.Done("Load inputs"); var prep = s.Done("Prepare compilations"); s.Pair(); for (int i = 0; i < 3; i++) { s.Tick(10); Assert.Equal(load, s.Done("Load inputs")); Assert.Equal(prep, s.Done("Prepare compilations")); } }
    [Fact] public void FinishDoesNotInventACompletedAnalysisOrFinalization()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); s.P.Finish(); Assert.Contains("Interrupted:", s.T.Screen); Assert.DoesNotContain("[ok] Analyze", s.T.Screen); Assert.DoesNotContain("[ok] Finalize", s.T.Screen); s.Pending("Finalize and report"); s.Orange(0); }
    [Fact] public void RealFailureIsRedAndDoesNotRecolorCompletedPreparation()
    { using var s = new CompactProgressScene(); s.Prepare(); var prep = s.Done("Prepare compilations"); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.RootFailed)); Assert.Contains(s.Frame(), r => r.Tone == ProgressTone.Error); Assert.Equal(prep, s.Done("Prepare compilations")); s.Orange(0); }
    [Fact] public void FinalResultsBelongOnlyToReportersAndDoNotImplyExecutionFailure()
    { using var s = new CompactProgressScene(); s.Prepare(); s.EndAll(); s.P.Findings(1275); s.P.Finish(new("Succeeded", "Complete", "Workspace", "SolutionTransitive", []), finalFindings: 1278, finalErrors: 0, finalWarnings: 980, finalHints: 298); s.NoResults(); foreach (var stale in new[] { "1275", "1278", "Current:", "Last activity:", "Member:", "Detector:" }) Assert.DoesNotContain(stale, s.T.EntireScreen); Assert.DoesNotContain(s.Frame(), r => r.Tone == ProgressTone.Error); s.Orange(0); }
    [Fact] public void VerboseDiagnosticsStayInsideAnalysisAndCannotAddOrangeRows()
    { using var s = new CompactProgressScene(verbose: true); s.Prepare(); s.Pair(); s.P.Observe(new(ExceptionFlowProgressKind.Admission)); s.P.Observe(new(ExceptionFlowProgressKind.DefinitionDiscovered, 7)); s.Tick(1); Assert.Contains("Admission:", s.Text("CURRENT WORK")); s.Orange(1); s.P.Begin(RunPhase.Documentation); s.P.Detector("Param"); s.Tick(1); Assert.Contains("Admission:", s.Text("CURRENT WORK")); s.Orange(1); }
    [Theory] [InlineData("Direct")] [InlineData("ProjectTransitive")] [InlineData("SolutionTransitive")] [InlineData("ProjectTransitiveDeclaredExceptions")]
    public void EveryModeRetainsTheSameSingleMainPhaseRule(string mode)
    { using var s = new CompactProgressScene(mode: mode); s.Prepare(); s.Pair(); s.Tick(1); s.Orange(1); Assert.Contains("1/2 files", s.T.Screen); Assert.Contains("Roots", s.T.Screen); if (mode == "Direct") { Assert.DoesNotContain("Contexts", s.T.Screen); Assert.DoesNotContain("Workitems", s.T.Screen); } else { Assert.Contains("Contexts", s.T.Screen); Assert.Contains("Workitems", s.T.Screen); } }
    [Fact] public void MonochromeRetainsStatusMarkersAndDoesNotLeakEscapeSequences()
    { using var s = new CompactProgressScene(color: false); s.Prepare(); s.Pair(); s.Tick(1); Assert.Contains("[>] Analyze", s.T.Screen); Assert.All(s.Frame(), r => Assert.Equal(ProgressTone.Neutral, r.Tone)); Assert.DoesNotContain('\u001b', s.T.Screen); }
    [Fact] public void ConsoleMessagesKeepCompletedAndPendingWorkInTheirAreas()
    { using var s = new CompactProgressScene(); s.Prepare(); s.Pair(); var old = Console.Out; try { Console.SetOut(new CompactProgressScene.ScreenWriter(s.T)); s.P.WithOutput(() => Console.WriteLine("ordinary log")); s.Orange(1); Assert.Contains("ordinary log", s.T.EntireScreen); s.Done("Load inputs"); s.Done("Prepare compilations"); s.Pending("Finalize and report"); } finally { Console.SetOut(old); } }
}