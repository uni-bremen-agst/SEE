using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Cli.Output;

internal enum ProgressMode { Auto, On, Off }
internal enum RunPhase { Loading, Compilation, Documentation, ExceptionFlow, Aggregation, Output, Rewrite, External, Isolated }

/// <summary>Observes existing sequential work; refreshes never advance work counters.</summary>
internal sealed class RunProgress : IDisposable
{
    private sealed class Phase
    {
        internal TimeSpan Elapsed;
        internal long Completed;
        internal long? Total;
        internal bool Failed;
        internal bool Started;
    }

    private readonly TextWriter writer;
    private readonly bool interactive;
    private readonly bool verbose;
    private readonly Func<TimeSpan> clock;
    private readonly Func<int> width;
    private readonly Dictionary<RunPhase, Phase> phases = [];
    private readonly Timer? timer;
    private ConsoleCancelEventHandler? cancelHandler;
    private RunPhase? active;
    private TimeSpan phaseStarted, lastActivity, lastRendered;
    private int lastLength;
    private bool finished, hasRendered, writable = true;
    private string? file, detector, detail;
    private ISymbol? member;
    private MemberDeclarationSyntax? root;
    private long rootsStarted, rootsCompleted, definitionsQueued, definitionsProcessed, contextsDiscovered, contextsProcessed;
    private int findings;

    internal RunProgress(TextWriter writer, bool interactive, bool verbose,
        Func<TimeSpan>? clock = null, Func<int>? width = null, bool startTimer = true)
    {
        this.writer = writer; this.interactive = interactive; this.verbose = verbose;
        FlowObserver = Observe;
        var watch = Stopwatch.StartNew();
        this.clock = clock ?? (() => watch.Elapsed);
        this.width = width ?? TerminalWidth;
        lastActivity = this.clock();
        if (interactive && startTimer) timer = new Timer(_ => Refresh(), null, 500, 500);
    }

    internal Action<ExceptionFlowProgressEvent> FlowObserver { get; }

    internal static bool ShouldEnable(ProgressMode mode, bool outputRedirected, bool errorRedirected,
        bool inputRedirected, bool ci, string? terminal)
        => mode == ProgressMode.On || mode == ProgressMode.Auto && !outputRedirected && !errorRedirected
            && !inputRedirected && !ci && !string.Equals(terminal, "dumb", StringComparison.OrdinalIgnoreCase);

    internal static RunProgress? Create(ToolOptions options)
    {
        bool ci = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        if (!ShouldEnable(options.Progress, Console.IsOutputRedirected, Console.IsErrorRedirected,
            Console.IsInputRedirected, ci, Environment.GetEnvironmentVariable("TERM"))) return null;
        var progress = new RunProgress(Console.Error, !Console.IsErrorRedirected && !Console.IsOutputRedirected && !ci
            && !string.Equals(Environment.GetEnvironmentVariable("TERM"), "dumb", StringComparison.OrdinalIgnoreCase), options.Verbose);
        progress.cancelHandler = (_, _) => progress.Finish(failed: true);
        Console.CancelKeyPress += progress.cancelHandler;
        return progress;
    }

    internal void Begin(RunPhase phase, string? currentFile = null)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished) return;
            TimeSpan now = clock();
            bool first = false;
            if (active != phase)
            {
                Accumulate(now);
                active = phase; phaseStarted = now;
                if (!phases.TryGetValue(phase, out var state)) phases.Add(phase, state = new());
                if (!state.Started)
                {
                    state.Started = true;
                    first = true;
                    if (!interactive) Write(() => writer.WriteLine($"[{Name(phase)}] started"));
                }
            }
            file = currentFile is null ? null : Path.GetFileName(currentFile);
            detector = null; detail = null; lastActivity = now;
            Render(first);
        }
    }

    internal void SetTotal(RunPhase phase, long total)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (!phases.TryGetValue(phase, out var state)) phases.Add(phase, state = new());
            state.Total = total;
        }
    }

    internal void DocumentCompleted(RunPhase phase = RunPhase.Documentation)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished) return;
            phases[phase].Completed++; lastActivity = clock(); Render(false);
        }
    }

    internal void Detector(string? name) { lock (ConsoleLogger.SyncRoot) { detector = name; lastActivity = clock(); Render(false); } }
    internal void Activity(string name) { lock (ConsoleLogger.SyncRoot) { detail = name; lastActivity = clock(); Render(false); } }
    internal void Findings(int count) { lock (ConsoleLogger.SyncRoot) findings = count; }

    internal void Observe(ExceptionFlowProgressEvent observation)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished) return;
            switch (observation.Kind)
            {
                case ExceptionFlowProgressKind.RootStarted: rootsStarted++; root = observation.Member; member = null; detail = "direct analysis"; break;
                case ExceptionFlowProgressKind.RootCompleted: rootsCompleted++; break;
                case ExceptionFlowProgressKind.RootFailed: if (active is { } failed) phases[failed].Failed = true; break;
                case ExceptionFlowProgressKind.Admission: detail = "admission"; break;
                case ExceptionFlowProgressKind.AdmissionSealing: detail = "admission sealing"; break;
                case ExceptionFlowProgressKind.DefinitionDiscovered: definitionsQueued += observation.Count; break;
                case ExceptionFlowProgressKind.DefinitionProcessed: definitionsProcessed += observation.Count; break;
                case ExceptionFlowProgressKind.ContextDiscovered: contextsDiscovered += observation.Count; break;
                case ExceptionFlowProgressKind.ContextProcessed: contextsProcessed += observation.Count; break;
                case ExceptionFlowProgressKind.Graph: detail = "summary construction"; break;
                case ExceptionFlowProgressKind.Evaluation: detail = "evaluation"; break;
            }
            if (verbose && observation.Symbol is not null && observation.Kind is ExceptionFlowProgressKind.DefinitionStarted
                or ExceptionFlowProgressKind.ContextStarted or ExceptionFlowProgressKind.Evaluation) member = observation.Symbol;
            lastActivity = clock(); Render(false);
        }
    }

    internal void Refresh() { lock (ConsoleLogger.SyncRoot) { if (!finished) Render(false); } }

    internal void ClearLine()
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (lastLength == 0) return;
            int clear = Math.Min(lastLength, Math.Max(0, width() - 1));
            Write(() => writer.Write("\r" + new string(' ', clear) + "\r")); lastLength = 0;
        }
    }

    internal void Finish(AnalysisCoverageReport? coverage = null, bool failed = false)
    {
        lock (ConsoleLogger.SyncRoot)
        {
            if (finished) return;
            Accumulate(clock()); finished = true; ClearLine();
            if (failed && active is { } current) phases[current].Failed = true;
            foreach (var (phase, state) in phases)
            {
                bool incomplete = state.Total is { } total && state.Completed < total;
                string status = state.Failed ? "failed/interrupted" : !state.Started ? "not started" : incomplete ? "not finished" : "finished";
                string units = state.Total is { } known ? $" {state.Completed}/{known} files;" : "";
                if (phase == RunPhase.ExceptionFlow) units = $" roots completed {rootsCompleted}, started {rootsStarted}; contexts discovered {contextsDiscovered}; context workitems processed {contextsProcessed};";
                if (verbose && phase == RunPhase.ExceptionFlow)
                    units += $" admission processed {definitionsProcessed}, discovered {definitionsQueued}; last member {DisplayMember()};";
                Write(() => writer.WriteLine($"[{Name(phase)}] {status};{units} {Time(state.Elapsed)}"));
            }
            string execution = failed ? "Failed" : coverage?.Execution ?? "Succeeded";
            Write(() => writer.WriteLine($"[Run] {execution}; analysis {coverage?.Completion ?? "unknown"}; mode {coverage?.Mode ?? "unknown"}"));
        }
    }

    private void Accumulate(TimeSpan now)
    {
        if (active is { } phase) phases[phase].Elapsed += now - phaseStarted;
        phaseStarted = now;
    }

    private void Render(bool force)
    {
        if (finished || !interactive || !writable || active is not { } phase) return;
        TimeSpan now = clock();
        if (!force && hasRendered && now - lastRendered < TimeSpan.FromMilliseconds(500)) return;
        lastRendered = now; hasRendered = true;
        Phase state = phases[phase];
        string units = "running";
        if (state.Total is > 0)
        {
            long total = state.Total.Value, completed = Math.Min(state.Completed, total);
            int percent = (int)(100 * completed / total);
            int fill = percent / 10;
            units = $"[{new string('#', fill)}{new string('-', 10 - fill)}] {percent}% {state.Completed}/{total} files";
        }
        else if (state.Total == 0) units = "0 files";
        if (phase == RunPhase.ExceptionFlow)
            units = $"roots completed {rootsCompleted}, started {rootsStarted}; contexts discovered {contextsDiscovered}; workitems processed {contextsProcessed}";
        int available = Math.Max(0, width() - 1);
        string elapsed = Time(state.Elapsed + now - phaseStarted);
        string age = $"activity {Math.Max(0, (int)(now - lastActivity).TotalSeconds)}s ago";
        string shortFile = file is { Length: > 20 } ? file[..17] + "..." : file ?? "";
        string prefix = $"[{Name(phase)}] {elapsed}";
        string message = $"{prefix}; {units}" + (file is null ? "" : $"; {shortFile}");
        if (phase == RunPhase.ExceptionFlow)
        {
            string step = detail switch { "admission" => "prepare", "admission sealing" => "seal", "summary construction" => "build", "evaluation" => "evaluate", _ => "direct" };
            prefix = $"[Exception/{step}] {elapsed}; {age}";
            string roots = $"roots {rootsCompleted} done, {rootsStarted} started";
            string contexts = $"contexts {contextsDiscovered} found, {contextsProcessed} processed";
            message = $"{prefix}; {roots}; {contexts}";
            if (message.Length > available) message = prefix + "; " + ((int)now.TotalSeconds % 2 == 0 ? roots : contexts);
        }
        if (verbose && (int)now.TotalSeconds % 4 >= 2)
        {
            string focus = phase == RunPhase.Documentation ? $"detector {detector ?? "-"}" : $"member {DisplayMember()}";
            message = $"{prefix}; {focus}; admission processed {definitionsProcessed}, discovered {definitionsQueued}; findings aggregated {findings}";
        }
        message = new string(message.Select(ch => char.IsControl(ch) ? ' ' : ch).ToArray());
        if (message.Length > available) message = message[..available];
        lastLength = Math.Min(lastLength, available);
        Write(() => ConsoleLogger.DrawProgress(writer, message, ref lastLength));
    }

    private void Write(Action write)
    {
        if (!writable) return;
        try { write(); writer.Flush(); }
        catch (Exception error) when (error is IOException or ObjectDisposedException or NotSupportedException or InvalidOperationException)
        { writable = false; }
    }

    private static string RootName(MemberDeclarationSyntax? node) => node switch
    {
        BaseMethodDeclarationSyntax method => method switch { MethodDeclarationSyntax m => m.Identifier.ValueText, ConstructorDeclarationSyntax c => c.Identifier.ValueText, _ => method.Kind().ToString() },
        BasePropertyDeclarationSyntax property => property.Kind().ToString(),
        _ => node?.Kind().ToString() ?? "-"
    };
    private string DisplayMember()
    {
        try { return new string((member?.ToDisplayString() ?? RootName(root)).Select(c => char.IsControl(c) ? ' ' : c).ToArray()); }
        catch (Exception) { return "unavailable"; }
    }
    private static string Time(TimeSpan value) => $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    private static int TerminalWidth() { try { return Math.Max(1, Console.WindowWidth); } catch (Exception e) when (e is IOException or PlatformNotSupportedException) { return 100; } }
    private static string Name(RunPhase phase) => phase switch
    {
        RunPhase.Loading => "Load inputs", RunPhase.Compilation => "Prepare compilations", RunPhase.Documentation => "Documentation",
        RunPhase.ExceptionFlow => "Exception flow", RunPhase.Aggregation => "Aggregate findings", RunPhase.Output => "Write output",
        RunPhase.Rewrite => "Rewrite", RunPhase.Isolated => "Isolated analysis", _ => "External analysis"
    };
    public void Dispose()
    {
        Finish(failed: true); timer?.Dispose();
        if (cancelHandler is not null) Console.CancelKeyPress -= cancelHandler;
    }
}

/// <summary>Delivers workspace notifications synchronously, without queued callbacks after disposal.</summary>
internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
