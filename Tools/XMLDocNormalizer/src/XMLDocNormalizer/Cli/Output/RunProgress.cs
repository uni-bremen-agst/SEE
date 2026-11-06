using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Cli.Output;

internal enum ProgressMode { Auto, On, Off }
internal enum RunPhase { Loading, Compilation, Documentation, ExceptionFlow, Aggregation, Output, Rewrite, External, Isolated }
internal enum PhaseStatus { Pending, Running, Completed, Failed, Interrupted }
internal enum MainStep { Loading, Preparation, Analysis, Finalization }

/// <summary>Observes work and explicit scope boundaries; rendering never advances a phase.</summary>
internal sealed class RunProgress : IDisposable
{
    private sealed class Phase
    {
        internal PhaseStatus Status;
        internal TimeSpan Elapsed;
        internal long Completed;
        internal long? Total;
        internal string? File;
    }
    private readonly TextWriter writer;
    private bool interactive;
    private readonly bool verbose;
    private readonly Func<TimeSpan> clock;
    private readonly Action<TimeSpan> displayPause;
    private TimeSpan? finalizationVisibleSince;
    private ProgressTerminalState? finalizationFrame;
    private TimeSpan finalizationVisibleElapsed, displayHoldElapsed;
    private readonly ProgressRegion? region;
    private readonly string mode;
    private readonly Dictionary<RunPhase, Phase> phases = Enum.GetValues<RunPhase>().ToDictionary(p => p, _ => new Phase());
    private readonly List<RunPhase> order;
    private readonly Dictionary<MainStep, Phase> main = Enum.GetValues<MainStep>().ToDictionary(p => p, _ => new Phase());
    private sealed record CompletedWork(string Label, TimeSpan Elapsed, string Counter = "", string Extra = "");
    private readonly List<CompletedWork> completed = [];
    private MainStep? mainActive;
    private bool finalizing, inlineReporting;
    private readonly Timer? timer;
    private ConsoleCancelEventHandler? cancelHandler;
    private RunPhase? active;
    private TimeSpan phaseStarted, lastFlowActivity, lastRendered;
    private bool finished, hasRendered, initialized, coordinatingOutput, writable = true;
    private bool? compact;
    private string? detector, detail, workOverride;
    private bool executionFailed, admissionObserved;
    private ISymbol? member;
    private MemberDeclarationSyntax? root;
    private long rootsStarted, rootsCompleted, definitionsQueued, definitionsProcessed, contextsDiscovered, contextsProcessed;

    internal RunProgress(TextWriter writer, bool interactive, bool verbose,
        Func<TimeSpan>? clock = null, Func<int>? width = null, bool startTimer = true,
        IProgressTerminal? terminal = null, string mode = "unknown", IReadOnlyList<RunPhase>? phasePlan = null,
        Action<TimeSpan>? displayPause = null)
    {
        this.writer = writer; this.verbose = verbose; this.mode = mode;
        order = phasePlan?.ToList() ?? [RunPhase.Loading, RunPhase.Compilation, RunPhase.Documentation, RunPhase.ExceptionFlow, RunPhase.Aggregation, RunPhase.Output];
        terminal ??= interactive ? WindowsProgressTerminal.TryCreate(writer) : null;
        this.interactive = interactive && terminal is not null;
        if (this.interactive) region = new ProgressRegion(terminal!);
        FlowObserver = Observe;
        var watch = Stopwatch.StartNew(); this.clock = clock ?? (() => watch.Elapsed);
        this.displayPause = displayPause ?? (duration => Thread.Sleep(duration));
        if (this.interactive && startTimer) timer = new Timer(_ => Refresh(), null, 500, 500);
    }
    internal Action<ExceptionFlowProgressEvent> FlowObserver { get; }
    internal static bool ShouldEnable(ProgressMode mode, bool outputRedirected, bool errorRedirected,
        bool inputRedirected, bool ci, string? terminal)
        => mode == ProgressMode.On || mode == ProgressMode.Auto && !outputRedirected && !errorRedirected
            && !inputRedirected && !ci && !string.Equals(terminal, "dumb", StringComparison.OrdinalIgnoreCase);
    internal static RunProgress? Create(ToolOptions options, bool isolatedComparison = false)
    {
        bool ci = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        if (!ShouldEnable(options.Progress, Console.IsOutputRedirected, Console.IsErrorRedirected,
            Console.IsInputRedirected, ci, Environment.GetEnvironmentVariable("TERM"))) return null;
        bool semantic = options.TargetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            || options.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        RunPhase[] plan = options.ExternalTarget ? [RunPhase.Loading, RunPhase.External, RunPhase.Aggregation, RunPhase.Output]
            : isolatedComparison ? [RunPhase.Isolated, RunPhase.Aggregation, RunPhase.Output]
            : semantic ? [RunPhase.Loading, RunPhase.Compilation, RunPhase.Documentation, RunPhase.ExceptionFlow, RunPhase.Aggregation, RunPhase.Output]
            : options.CheckOnly ? [RunPhase.Loading, RunPhase.Documentation, RunPhase.Aggregation, RunPhase.Output]
            : [RunPhase.Loading, RunPhase.Rewrite];
        var progress = new RunProgress(Console.Error, !Console.IsErrorRedirected && !Console.IsOutputRedirected && !ci
            && !string.Equals(Environment.GetEnvironmentVariable("TERM"), "dumb", StringComparison.OrdinalIgnoreCase), options.Verbose,
            mode: options.XmlDocOptions.ExceptionAnalysisMode.ToString(), phasePlan: plan);
        progress.cancelHandler = (_, _) => progress.Finish(failed: true);
        Console.CancelKeyPress += progress.cancelHandler;
        return progress;
    }
    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (!interactive) foreach (var step in Enum.GetValues<MainStep>().Where(Applicable)) EmitMain(step);
    }
    internal void Begin(RunPhase phase, string? currentFile = null)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished) return;
            if (IsTerminal(phases[phase].Status))
            {
                if (phase == RunPhase.Compilation && main[MainStep.Analysis].Status == PhaseStatus.Running)
                { workOverride = "Current: Project compilation access within analysis"; Render(false); }
                return;
            }
            Initialize(); TimeSpan now = clock();
            if (!order.Contains(phase)) order.Add(phase); // Optional scopes append; existing phase positions never move.
            bool first = phases[phase].Status == PhaseStatus.Pending;
            Accumulate(now); active = phase; phaseStarted = now;
            workOverride = null;
            phases[phase].Status = PhaseStatus.Running;
            MainStep owner = phase == RunPhase.Loading ? MainStep.Loading
                : phase == RunPhase.Compilation && main[MainStep.Analysis].Status == PhaseStatus.Pending ? MainStep.Preparation
                : finalizing ? MainStep.Finalization : MainStep.Analysis;
            if (phase == RunPhase.Compilation && main[MainStep.Preparation].Status == PhaseStatus.Pending) StartMain(MainStep.Preparation);
            mainActive = owner; StartMain(owner);
            phases[phase].File = currentFile is null ? null : Path.GetFileName(currentFile);
            if (phase == RunPhase.Documentation) detector = null;
            if (phase == RunPhase.ExceptionFlow) { lastFlowActivity = now; member = null; root = null; detail = null; }
            if (!interactive && first && phase is RunPhase.Documentation or RunPhase.ExceptionFlow or RunPhase.Rewrite) Emit(phase);
            Render(first);
        }
    }
    internal void SetTotal(RunPhase phase, long total)
    {
        lock (ConsoleLogger.SyncRoot) if (!IsTerminal(phases[phase].Status)) phases[phase].Total = total;
    }
    internal void DocumentCompleted(RunPhase phase = RunPhase.Documentation)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished || phases[phase].Status != PhaseStatus.Running) return;
            phases[phase].Completed++;
            if (phases[phase].Total is { } total && phases[phase].Completed == total) Complete(phase);
            else Render(false);
        }
    }
    /// <summary>Called only at the end of the complete phase scope, including zero-work scopes.</summary>
    internal void Complete(RunPhase phase)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished || IsTerminal(phases[phase].Status)) return;
            if (phases[phase].Total is { } total && phases[phase].Completed < total) return;
            Transition(phase, PhaseStatus.Completed);
        }
    }
    internal void Interrupt(RunPhase phase)
    {
        lock (ConsoleLogger.SyncRoot) if (!finished && !IsTerminal(phases[phase].Status)) Transition(phase, PhaseStatus.Interrupted);
    }
    internal void CompleteExceptionFile()
    {
        lock (ConsoleLogger.SyncRoot)
            if (phases[RunPhase.Documentation].Status == PhaseStatus.Completed && rootsStarted == rootsCompleted)
                Complete(RunPhase.ExceptionFlow);
    }
    /// <summary>The visited scope ended; missing eligible files remain incomplete, never falsely green.</summary>
    internal void EndAnalysis(bool exceptionFlow = true)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            var docs = phases[RunPhase.Documentation];
            if (docs.Total is { } total && docs.Completed == total) Complete(RunPhase.Documentation);
            else Interrupt(RunPhase.Documentation);
            if (!exceptionFlow) return;
            if (docs.Status == PhaseStatus.Completed && rootsStarted == rootsCompleted) Complete(RunPhase.ExceptionFlow);
            else Interrupt(RunPhase.ExceptionFlow);
        }
    }
    private void Transition(RunPhase phase, PhaseStatus status)
    {
        Accumulate(clock());
        phases[phase].Status = status; phases[phase].File = null;
        if (status == PhaseStatus.Completed && phase is RunPhase.Documentation or RunPhase.ExceptionFlow or RunPhase.Rewrite)
        {
            string counter = phases[phase].Total is { } total ? $"{phases[phase].Completed}/{total} files" : "";
            string extra = "";
            if (phase == RunPhase.ExceptionFlow)
            {
                counter = $"{rootsCompleted}/{rootsStarted} roots";
                if (mode != "Direct") extra = $"Contexts: {contextsDiscovered}; workitems: {contextsProcessed}";
            }
            completed.Add(new(Name(phase), phases[phase].Elapsed, counter, extra));
        }
        if (active == phase) active = null;
        if (phase is RunPhase.Loading or RunPhase.Compilation)
        {
            var step = phase == RunPhase.Loading ? MainStep.Loading : MainStep.Preparation;
            main[step].Elapsed = phases[phase].Elapsed; SetMain(step, status);
        }
        else if (status == PhaseStatus.Failed) SetMain(finalizing ? MainStep.Finalization : MainStep.Analysis, status);
        if (phase == RunPhase.Output && finalizing && status == PhaseStatus.Completed)
        {
            SetMain(MainStep.Finalization, status);
            PauseCompletedFinalization(); // Work and elapsed time are already frozen; delay only the final paint.
        }
        if (!interactive && phase is RunPhase.Documentation or RunPhase.ExceptionFlow or RunPhase.Rewrite) Emit(phase);
        Render(true); // A proved transition is visible immediately, independent of the periodic throttle.
    }
    private void StartMain(MainStep step)
    {
        if (main[step].Status != PhaseStatus.Pending) return;
        main[step].Status = PhaseStatus.Running;
        if (!interactive) EmitMain(step);
    }
    private void SetMain(MainStep step, PhaseStatus status)
    {
        if (IsTerminal(main[step].Status)) return;
        main[step].Status = status;
        if (status == PhaseStatus.Completed && ShowMainCompletion(step)) completed.Add(new(MainName(step), main[step].Elapsed));
        if (mainActive == step) mainActive = null;
        if (!interactive) EmitMain(step);
    }
    internal void UseInlineReporting() { lock (ConsoleLogger.SyncRoot) inlineReporting = true; }
    internal void CompleteAnalysis()
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished || IsTerminal(main[MainStep.Analysis].Status)) return;
            if (Applicable(MainStep.Preparation) && main[MainStep.Preparation].Status is PhaseStatus.Pending or PhaseStatus.Running) return;
            var required = order.Where(p => p is RunPhase.Documentation or RunPhase.ExceptionFlow or RunPhase.Rewrite or RunPhase.External or RunPhase.Isolated
                || p == RunPhase.Aggregation && phases[p].Status != PhaseStatus.Pending).Select(p => phases[p].Status).ToArray();
            if (required.Any(s => s is PhaseStatus.Pending or PhaseStatus.Running)) return;
            Accumulate(clock());
            SetMain(MainStep.Analysis, required.Contains(PhaseStatus.Failed) ? PhaseStatus.Failed
                : required.Contains(PhaseStatus.Interrupted) ? PhaseStatus.Interrupted : PhaseStatus.Completed);
            Render(true);
        }
    }
    internal void BeginFinalization()
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished || inlineReporting || !order.Contains(RunPhase.Output)) return;
            CompleteAnalysis();
            if (!IsTerminal(main[MainStep.Analysis].Status)) return;
            Accumulate(clock()); finalizing = true; mainActive = MainStep.Finalization;
            StartMain(MainStep.Finalization); Render(true);
        }
    }
    internal void Detector(string? name)
    {
        lock (ConsoleLogger.SyncRoot) if (!finished && phases[RunPhase.Documentation].Status == PhaseStatus.Running) { detector = name; Render(false); }
    }
    internal void Activity(string name) { lock (ConsoleLogger.SyncRoot) { if (!finished) { detail = name; Render(false); } } }
    // Compatibility with existing progress notifications; results belong exclusively to the reporters.
    internal void Findings(int count) { }
    internal void Observe(ExceptionFlowProgressEvent observation)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished || IsTerminal(phases[RunPhase.ExceptionFlow].Status)) return;
            switch (observation.Kind)
            {
                case ExceptionFlowProgressKind.RootStarted: rootsStarted++; root = observation.Member; member = null; detail = "direct analysis"; break;
                case ExceptionFlowProgressKind.RootCompleted: rootsCompleted++; break;
                case ExceptionFlowProgressKind.RootFailed: Transition(RunPhase.ExceptionFlow, PhaseStatus.Failed); break;
                case ExceptionFlowProgressKind.Admission: detail = "admission"; break;
                case ExceptionFlowProgressKind.AdmissionSealing: detail = "admission sealing"; break;
                case ExceptionFlowProgressKind.DefinitionDiscovered: definitionsQueued += observation.Count; break;
                case ExceptionFlowProgressKind.DefinitionProcessed: definitionsProcessed += observation.Count; break;
                case ExceptionFlowProgressKind.ContextDiscovered: contextsDiscovered += observation.Count; break;
                case ExceptionFlowProgressKind.ContextProcessed: contextsProcessed += observation.Count; break;
                case ExceptionFlowProgressKind.Graph: detail = "summary construction"; break;
                case ExceptionFlowProgressKind.Evaluation: detail = "evaluation"; break;
            }
            if (observation.Kind is ExceptionFlowProgressKind.Admission or ExceptionFlowProgressKind.AdmissionSealing
                or ExceptionFlowProgressKind.DefinitionDiscovered or ExceptionFlowProgressKind.DefinitionProcessed) admissionObserved = true;
            if (verbose && observation.Symbol is not null && observation.Kind is ExceptionFlowProgressKind.DefinitionStarted
                or ExceptionFlowProgressKind.ContextStarted or ExceptionFlowProgressKind.Evaluation) member = observation.Symbol;
            lastFlowActivity = clock(); Render(false);
        }
    }
    internal void Refresh() { lock (ConsoleLogger.SyncRoot) { if (!finished) { Initialize(); Render(false); } } }
    internal void ClearLine() { lock (ConsoleLogger.SyncRoot) region?.LeaveVisible(); }
    internal void WithOutput(Action output)
    {
        if (coordinatingOutput || !interactive || !writable || region is not { IsReserved: true }) { output(); return; }
        TextWriter stdout = Console.Out, stderr = Console.Error; bool interrupted = false;
        void BeforeWrite()
        {
            if (interrupted) return;
            if (finalizationVisibleSince is { } since) finalizationVisibleElapsed += clock() - since;
            interrupted = true; finalizationVisibleSince = null; finalizationFrame = null;
            Write(() => { if (!region.Suspend()) FallBack(); });
        }
        try
        {
            coordinatingOutput = true;
            Console.SetOut(new ProgressOutputWriter(stdout, BeforeWrite)); Console.SetError(new ProgressOutputWriter(stderr, BeforeWrite)); output();
        }
        finally { Console.SetOut(stdout); Console.SetError(stderr); coordinatingOutput = false; if (interrupted) Render(true); }
    }
    internal void Finish(AnalysisCoverageReport? coverage = null, bool failed = false, int? finalFindings = null,
        int? finalErrors = null, int? finalWarnings = null, int? finalHints = null)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished) return;
            Initialize(); executionFailed = failed || coverage?.Execution == "Failed";
            Accumulate(clock());
            if (executionFailed && active is { } failedPhase && phases[failedPhase].Status == PhaseStatus.Running) Transition(failedPhase, PhaseStatus.Failed);
            foreach (var phase in order.ToArray()) if (phases[phase].Status == PhaseStatus.Running) Transition(phase, PhaseStatus.Interrupted);
            foreach (var step in Enum.GetValues<MainStep>()) if (main[step].Status == PhaseStatus.Running)
                SetMain(step, executionFailed && mainActive == step ? PhaseStatus.Failed : PhaseStatus.Interrupted);
            Accumulate(clock()); active = null; finished = true;
            if (interactive) { Render(true); ClearLine(); }
            else Write(() => writer.WriteLine($"{(executionFailed ? "[!!] Execution failed; " : "")}Total {Time(clock() - displayHoldElapsed)}"));
        }
    }
    private static bool IsTerminal(PhaseStatus status) => status is PhaseStatus.Completed or PhaseStatus.Failed or PhaseStatus.Interrupted;
    private void Accumulate(TimeSpan now)
    {
        if (active is { } phase && phases[phase].Status == PhaseStatus.Running) phases[phase].Elapsed += now - phaseStarted;
        if (mainActive is { } step && main[step].Status == PhaseStatus.Running) main[step].Elapsed += now - phaseStarted;
        phaseStarted = now;
    }
    private TimeSpan Elapsed(RunPhase phase, TimeSpan now) => phases[phase].Elapsed
        + (active == phase && phases[phase].Status == PhaseStatus.Running ? now - phaseStarted : TimeSpan.Zero);
    private void Emit(RunPhase phase)
    {
        var state = phases[phase];
        string group = state.Status == PhaseStatus.Completed ? "Completed" : state.Status == PhaseStatus.Pending ? "Pending"
            : IsTerminal(state.Status) ? "Stopped" : "Current";
        string counter = state.Total is { } total ? $"; {state.Completed}/{total} files" : "";
        if (phase == RunPhase.ExceptionFlow && state.Status != PhaseStatus.Pending)
        {
            counter = $"; roots completed {rootsCompleted}, started {rootsStarted}";
            if (mode != "Direct") counter += $"; contexts discovered {contextsDiscovered}; context workitems processed {contextsProcessed}";
        }
        string time = state.Status == PhaseStatus.Pending ? "" : $"; {Time(state.Elapsed)}";
        string marker = state.Status == PhaseStatus.Running ? "" : Marker(state.Status) + " ";
        Write(() => writer.WriteLine($"{group} | {marker}{Name(phase)}{counter}{time}"));
    }
    private bool Applicable(MainStep step) => step switch
    {
        MainStep.Loading => order.Contains(RunPhase.Loading), MainStep.Preparation => order.Contains(RunPhase.Compilation),
        MainStep.Finalization => !inlineReporting && order.Contains(RunPhase.Output), _ => true
    };
    private TimeSpan MainElapsed(MainStep step, TimeSpan now) => main[step].Elapsed
        + (mainActive == step && main[step].Status == PhaseStatus.Running ? now - phaseStarted : TimeSpan.Zero);
    private static string MainName(MainStep step) => step switch
    {
        MainStep.Loading => "Load inputs", MainStep.Preparation => "Prepare compilations",
        MainStep.Analysis => "Analyze", _ => "Finalize and report"
    };
    private bool ShowMainCompletion(MainStep step) => step != MainStep.Analysis
        || !order.Any(p => p is RunPhase.Documentation or RunPhase.ExceptionFlow or RunPhase.Rewrite);
    private void EmitMain(MainStep step)
    {
        if (!Applicable(step) || main[step].Status == PhaseStatus.Completed && !ShowMainCompletion(step)) return;
        var status = main[step].Status;
        string group = status == PhaseStatus.Completed ? "Completed" : status == PhaseStatus.Pending ? "Pending"
            : IsTerminal(status) ? "Stopped" : "Current";
        Write(() => writer.WriteLine($"{group} | {Marker(status)} {MainName(step)}"
            + (status == PhaseStatus.Pending ? "" : $"; {Time(main[step].Elapsed)}")));
    }
    private void Render(bool force)
    {
        if (!interactive || !writable) return;
        TimeSpan now = clock();
        if (!force && hasRendered && now - lastRendered < TimeSpan.FromMilliseconds(500)) return;
        lastRendered = now; hasRendered = true;
        Write(() =>
        {
            if (!region!.TryGetState(out var state) || compact == false && state.WindowWidth < 80
                || !region.Render(Rows(now, state))) { FallBack(); return; }
            if (finalizing && main[MainStep.Finalization].Status == PhaseStatus.Running && region.TryGetState(out var frame))
            {
                finalizationVisibleSince ??= clock();
                finalizationFrame = frame;
            }
        });
    }
    private void PauseCompletedFinalization()
    {
        var since = finalizationVisibleSince; var frame = finalizationFrame;
        TimeSpan visible = finalizationVisibleElapsed;
        finalizationVisibleElapsed = TimeSpan.Zero;
        finalizationVisibleSince = null; finalizationFrame = null;
        TimeSpan minimum = TimeSpan.FromMilliseconds(1000);
        if (main[MainStep.Finalization].Elapsed >= minimum) return;
        if (!interactive || !writable || since is null || frame is null || region is not { IsReserved: true }
            || !region.TryGetState(out var current) || current != frame.Value) return;
        visible += clock() - since.Value;
        if (visible < TimeSpan.Zero) visible = TimeSpan.Zero;
        TimeSpan remaining = minimum - visible;
        if (remaining <= TimeSpan.Zero) return;
        TimeSpan holdStarted = clock();
        try { displayPause(remaining); }
        finally { displayHoldElapsed += clock() - holdStarted; }
    }
    private void FallBack()
    {
        interactive = false;
        Write(() => writer.WriteLine("[Progress] Live display unavailable; explicit phase states follow."));
        foreach (var row in Rows(clock(), new(120, 300, 0, 40, 120, 0, 0))) Write(() => writer.WriteLine(row.Text));
    }
    private ProgressRow[] Rows(TimeSpan now, ProgressTerminalState terminal)
    {
        compact ??= terminal.WindowWidth < 80;
        bool small = compact.Value, summary = mode != "Direct";
        int columns = Math.Max(39, Math.Min(terminal.WindowWidth, terminal.BufferWidth) - 1);
        string shortMode = mode switch { "ProjectTransitiveDeclaredExceptions" => "Project+declared", "ProjectTransitive" => "Project transitive", "SolutionTransitive" => "Solution transitive", _ => mode };
        var rows = new List<ProgressRow> { new(small ? $"XMLDoc | {shortMode} | {Time(now - displayHoldElapsed)}" : $"XMLDocNormalizer | {mode} | {Time(now - displayHoldElapsed)}") };
        int sections = 0;
        void Section(string label, ProgressTone tone)
        {
            if (sections++ > 0) rows.Add(new(""));
            rows.Add(new(label, tone));
        }
        string Short(string label) => !small ? label : label switch
        {
            "Prepare compilations" => "Prepare", "Documentation" => "Docs", "Exception flow" => "Flow",
            "Finalize and report" => "Report", _ => label
        };
        ProgressRow Timed(string marker, string label, TimeSpan elapsed, ProgressTone tone, string counter = "")
        {
            string suffix = (counter.Length > 0 ? counter + " " : "") + Time(elapsed);
            int room = Math.Max(1, columns - marker.Length - suffix.Length - 2);
            return new($"{marker} {ProgressRegion.Fit(Short(label), room).PadRight(room)} {suffix}", tone);
        }
        var stopped = order.Where(p => phases[p].Status is PhaseStatus.Failed or PhaseStatus.Interrupted).ToArray();
        if (completed.Count > 0)
        {
            Section("COMPLETED", ProgressTone.Completed);
            foreach (var work in completed)
            {
                rows.Add(Timed("[ok]", work.Label, work.Elapsed, ProgressTone.Completed, work.Counter));
                if (work.Extra.Length > 0) rows.Add(new("  " + work.Extra, ProgressTone.Completed));
            }
        }
        if (executionFailed || stopped.Any(p => phases[p].Status == PhaseStatus.Failed))
            rows.Add(new("[!!] Execution failed" + (stopped.Length > 0 ? ": " + Name(stopped[0]) : ""), ProgressTone.Error));
        else if (stopped.Length > 0) rows.Add(new("[!] Interrupted: " + string.Join(", ", stopped.Select(Name)), ProgressTone.Neutral));
        var running = Enum.GetValues<MainStep>().Where(step => Applicable(step) && main[step].Status == PhaseStatus.Running).ToArray();
        if (!finished && running.Length > 0)
        {
            Section("CURRENT WORK", ProgressTone.Active);
            MainStep owner = mainActive is { } current && running.Contains(current) ? current : running[^1];
            bool analyzing = owner == MainStep.Analysis;
            bool docs = analyzing && order.Contains(RunPhase.Documentation) && phases[RunPhase.Documentation].Status == PhaseStatus.Running;
            bool flow = analyzing && order.Contains(RunPhase.ExceptionFlow) && phases[RunPhase.ExceptionFlow].Status == PhaseStatus.Running;
            bool docDone = phases[RunPhase.Documentation].Status == PhaseStatus.Completed;
            bool flowDone = phases[RunPhase.ExceptionFlow].Status == PhaseStatus.Completed;
            string label = analyzing && active == RunPhase.ExceptionFlow && docDone && flow ? "Exception flow"
                : analyzing && active == RunPhase.Documentation && flowDone && docs ? "Documentation" : MainName(owner);
            TimeSpan elapsed = label == "Exception flow" ? Elapsed(RunPhase.ExceptionFlow, now)
                : label == "Documentation" ? Elapsed(RunPhase.Documentation, now) : MainElapsed(owner, now);
            rows.Add(Timed("[>]", label, elapsed, ProgressTone.Active));
            foreach (var other in running.Where(step => step != owner))
                rows.Add(new("  " + Short(MainName(other)) + ": continuing", ProgressTone.Neutral));
            if (analyzing)
            {
                foreach (var phase in order.Where(p => p is RunPhase.Documentation or RunPhase.Rewrite && phases[p].Status == PhaseStatus.Running))
                {
                    var state = phases[phase];
                    string bar = "Eligible files pending";
                    if (state.Total is > 0)
                    {
                        int percent = (int)(100 * Math.Min(state.Completed, state.Total.Value) / state.Total.Value);
                        int cells = small ? 8 : 16, fill = percent * cells / 100;
                        bar = $"[{new string('#', fill)}{new string('-', cells - fill)}] {percent}% {state.Completed}/{state.Total} files";
                    }
                    else if (state.Total == 0) bar = "0 eligible files";
                    rows.Add(new("  " + Short(phase == RunPhase.Documentation ? "Documentation" : "File changes") + ": " + bar, ProgressTone.Neutral));
                }
                if (flow)
                {
                    rows.Add(new(small ? $"  Roots: {rootsCompleted}/{rootsStarted} completed/started" : $"  Roots     : {rootsCompleted} completed / {rootsStarted} started"));
                    if (summary)
                    {
                        rows.Add(new($"  Contexts  : {contextsDiscovered} discovered"));
                        rows.Add(new($"  Workitems : {contextsProcessed} processed"));
                    }
                    rows.Add(new($"  Last activity: {Math.Max(0, (int)(now - lastFlowActivity).TotalSeconds)}s ago"));
                }
                rows.Add(new("  " + CurrentActivity()));
                if (verbose && flow && admissionObserved)
                    rows.Add(new($"  Admission: {definitionsProcessed} processed / {definitionsQueued} discovered"));
            }
        }
        var pending = Enum.GetValues<MainStep>().Where(step => Applicable(step) && main[step].Status == PhaseStatus.Pending).Select(MainName).ToList();
        if (main[MainStep.Analysis].Status == PhaseStatus.Running)
            pending.AddRange(order.Where(p => p is RunPhase.Documentation or RunPhase.ExceptionFlow && phases[p].Status == PhaseStatus.Pending).Select(Name));
        if (pending.Count > 0)
        {
            Section("PENDING", ProgressTone.Muted);
            foreach (string label in pending) rows.Add(new("[ ] " + Short(label), ProgressTone.Muted));
        }
        return rows.ToArray();
    }
    private string CurrentActivity()
    {
        if (finished || main[MainStep.Analysis].Status != PhaseStatus.Running) return "";
        if (workOverride is not null) return workOverride;
        if (active is { } phase && IsTerminal(phases[phase].Status)) return "Current: Analysis running";
        return active switch
        {
            RunPhase.Documentation when detector is not null => $"Current: Documentation check - {detector}",
            RunPhase.Documentation => "Current: Documentation check",
            RunPhase.ExceptionFlow when verbose && member is not null => $"Member: {DisplayMember()}",
            RunPhase.ExceptionFlow when root is not null => $"Current: Exception flow - {RootName(root)}",
            RunPhase.ExceptionFlow => "Current: Exception flow",
            RunPhase.Aggregation => "Current: Intermediate finding aggregation",
            RunPhase.Output when !finalizing => "Current: File reporting within analysis",
            RunPhase.Compilation => "Current: Compilation preparation within analysis",
            RunPhase.Rewrite => "Current: File changes",
            RunPhase.Isolated => "Current: Isolated analysis",
            RunPhase.External => "Current: Bounded external analysis",
            _ => "Current: Analysis running"
        };
    }
    private static string Marker(PhaseStatus status) => status switch
    {
        PhaseStatus.Completed => "[ok]", PhaseStatus.Running => "[>]", PhaseStatus.Failed => "[!!]", PhaseStatus.Interrupted => "[!]", _ => "[ ]"
    };
    private static ProgressTone TerminalTone(PhaseStatus status) => status switch
    {
        PhaseStatus.Completed => ProgressTone.Completed, PhaseStatus.Failed => ProgressTone.Error,
        PhaseStatus.Interrupted => ProgressTone.Notice, PhaseStatus.Pending => ProgressTone.Muted, _ => ProgressTone.Neutral
    };
    private void Write(Action write)
    {
        if (!writable) return;
        try { write(); writer.Flush(); }
        catch (Exception error) when (error is IOException or ObjectDisposedException or NotSupportedException or InvalidOperationException) { writable = false; }
    }
    private static string RootName(MemberDeclarationSyntax? node) => node switch
    {
        BaseMethodDeclarationSyntax method => method switch { MethodDeclarationSyntax m => m.Identifier.ValueText, ConstructorDeclarationSyntax c => c.Identifier.ValueText, _ => method.Kind().ToString() },
        BasePropertyDeclarationSyntax property => property.Kind().ToString(), _ => node?.Kind().ToString() ?? "-"
    };
    private string DisplayMember()
    {
        try { return new string((member?.ToDisplayString() ?? RootName(root)).Select(c => char.IsControl(c) ? ' ' : c).ToArray()); }
        catch (Exception) { return "unavailable"; }
    }
    private static string Time(TimeSpan value) => $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    private static string Name(RunPhase phase) => phase switch
    {
        RunPhase.Loading => "Load inputs", RunPhase.Compilation => "Prepare compilations", RunPhase.Documentation => "Documentation",
        RunPhase.ExceptionFlow => "Exception flow", RunPhase.Aggregation => "Aggregate findings", RunPhase.Output => "Write output",
        RunPhase.Rewrite => "Rewrite", RunPhase.Isolated => "Isolated analysis", _ => "External analysis"
    };
    public void Dispose() { Finish(failed: true); timer?.Dispose(); if (cancelHandler is not null) Console.CancelKeyPress -= cancelHandler; }
}
internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
