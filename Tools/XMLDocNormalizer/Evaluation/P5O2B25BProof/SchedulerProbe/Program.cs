using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

// Diagnostic scheduling counterexample, NOT a replacement test gate or a claim about captured Full telemetry.
string bin = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
if (File.Exists(output)) { throw new InvalidOperationException("Preserve prior experiment."); }
AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(bin, name.Name + ".dll"))
    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
var type = assembly.GetType("XMLDocNormalizerTests.Execution.Semantic.ExternalSourceLineEndingReconstructionTests", true)!;
var method = type.GetMethod("ConcurrentReconstruction_PerformsOneRequestAndOneAttempt")!;
var context = new HeldContext();
ThreadPool.GetMinThreads(out int minWorker, out int minIo); ThreadPool.GetMaxThreads(out int maxWorker, out int maxIo);
using var occupied = new ManualResetEventSlim(); using var start = new ManualResetEventSlim();
Task test;
try
{
    if (!ThreadPool.SetMinThreads(1, minIo) || !ThreadPool.SetMaxThreads(1, maxIo)) { throw new InvalidOperationException("Cannot control diagnostic scheduling."); }
    ThreadPool.QueueUserWorkItem(_ => { occupied.Set(); start.Wait(); });
    if (!occupied.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("Diagnostic worker was not occupied."); }
    SynchronizationContext.SetSynchronizationContext(context);
    test = (Task)method.Invoke(Activator.CreateInstance(type), null)!;
}
finally
{
    SynchronizationContext.SetSynchronizationContext(null);
    ThreadPool.SetMaxThreads(maxWorker, maxIo); ThreadPool.SetMinThreads(minWorker, minIo); start.Set();
}
var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
while (!test.IsCompleted && context.Count == 0 && DateTime.UtcNow < deadline) { await Task.Delay(10); }
bool captured = context.Count != 0;
if (captured) { await Task.Delay(TimeSpan.FromSeconds(6)); }
bool completedWhileContextHeld = test.IsCompleted;
context.Drain();
string? failure = null;
try { await test.WaitAsync(TimeSpan.FromSeconds(15)); }
catch (Exception exception) { failure = exception.ToString(); }
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
{
    Experiment = "Actual unchanged assertions under deliberately held caller SynchronizationContext",
    CapturedCallerContext = captured, CompletedWhileContextHeld = completedWhileContextHeld,
    Passed = failure == null, Failure = failure, ProductionOrTimeoutChanged = false,
    FullSchedulingTelemetryCaptured = false, OfficialGate = false
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Captured={captured}; completed while held={completedWhileContextHeld}; passed={failure == null}");
return 0;

sealed class HeldContext : SynchronizationContext
{
    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> pending = new();
    internal int Count => pending.Count;
    public override void Post(SendOrPostCallback callback, object? state) => pending.Enqueue((callback, state));
    internal void Drain() { while (pending.TryDequeue(out var item)) { item.Callback(item.State); } }
}
