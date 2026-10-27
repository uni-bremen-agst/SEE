# Full closure: unchanged-contract scheduling correction

The first Full is preserved as `Results/full.{json,log,trx}`:
3448/3449, no skips. Its sole failure is
`ExternalSourceLineEndingReconstructionTests.ConcurrentReconstruction_PerformsOneRequestAndOneAttempt`,
the existing `Assert.True(await first)` at line341 after about five seconds.

Read-only trace: the mock handler signals `entered` and waits for `release`
under the real unchanged SourceLink client's five-second cancellation token.
`TryAcquire` is synchronous; `TryDownload` waits for that asynchronous handler.
The second acquisition correctly returns false while the entry is InProgress.
The test's original `await entered.Task` resumes on its caller's test
SynchronizationContext before it can issue that second request and release the
mock. Thus withholding that test continuation can expire the download before
release, independently of P5H correctness or B25B policy.

The standalone SchedulerProbe invokes the actual existing test method and its
actual unchanged assertions. It forces the first Task.Run to yield, restores
normal ThreadPool bounds, then deliberately holds the caller context for six
seconds. It reproduces the same True assertion with the original harness.
The exact same experiment passes after the correction. These are controlled
scheduling counterexamples, NOT new Full runs and NOT a claim that the first
Full captured per-continuation scheduling telemetry (it did not).

The final production diff remains unchanged. The sole additional existing-test
edit wraps only `await entered`, the existing InProgress assertion, and
`release.SetResult` in `Task.Run(async () => ...)`. The Fact itself still awaits
normally on xUnit's context. Its I/O-release coordination no longer depends on
resuming that context before the production deadline. All four existing
assertions and the five-second timeout are verbatim unchanged. No new expected
outcome, skip, retry, catch-ignore, timeout extension or analyzer suppression.
This is permitted by original B25B section12 (local fixture/helper issues without
changing an existing semantic assertion), not an extension of C1–C3.

The initial ConfigureAwait(false) attempt was rejected by xUnit1030 and removed,
not suppressed. The final wrapper builds with warnings as errors. The standalone
probe initially lacked an explicit Immutable9 runtime binding; that diagnostic
project alone was corrected, using the existing Test-bin assembly. Its first
managed load failure is not the historical native CLI `0xC0000005` crash.

Official follow-up: isolated test1/1; then one Broad and one Full under distinct
`*-harness` record names. The original green Broad is preserved as well. Boundary
and Targeted exclude this entire class (zero matches in their TRX); their
unchanged inputs/contracts are explicitly reused, not described as rerun.
The original Broad includes19 tests from this class, so it is freshly executed
against the corrected source. Exact source delta / assertions / timeout and
hashes are in `Results/harness-coordination-delta.json`. The earlier rejected
delta/guard attempt is retained, not treated as successful validation.
