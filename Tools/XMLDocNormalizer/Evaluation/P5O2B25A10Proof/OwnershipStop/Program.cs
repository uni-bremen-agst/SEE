using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

// Evidence-only observer. Calls the existing unchanged B25 fixture and Native
// observer in an isolated export. It does not change or bypass any assertion.
string export = Path.GetFullPath(args[0]);
if (!AppContext.BaseDirectory.StartsWith(export + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("The evidence observer must itself execute inside the isolated export so inherited fixture Root() cannot select the workspace.");
string bin = Path.Combine(export, "Tests/XMLDocNormalizerTests/bin/Debug/net8.0");
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string path = Path.Combine(bin, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
Assembly tests = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
Type fixture = tests.GetType("XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalSolutionFindingTests", true)!;
Console.WriteLine("Loaded isolated B25 test assembly.");
const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
Task creation = (Task)fixture.GetMethod("Create", flags)!.Invoke(null, [2, "same-name-types"])!;
await creation;
Console.WriteLine("Created isolated unchanged B25 fixture.");
object created = creation.GetType().GetProperty("Result")!.GetValue(creation)!;
object input = created.GetType().GetProperty("Input")!.GetValue(created)!;
Assembly main = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, "XMLDocNormalizer.dll"));
Type projection = main.GetType("XMLDocNormalizer.Execution.Analysis.HistoricalWorkerPayloadProjection", true)!;
object projected = projection.GetMethods(flags | BindingFlags.Public)
    .Single(method => method.Name == "Project" && method.GetParameters().Length == 1).Invoke(null, [input])!;
object value = projected.GetType().GetProperty("Value")!.GetValue(projected)!;
object payload = value.GetType().GetProperty("Payload")!.GetValue(value)!;
JsonElement native = (JsonElement)fixture.GetMethod("Native", flags)!.Invoke(null, [payload])!;
string destination = Path.GetFullPath(args[1]);
if (File.Exists(destination)) throw new InvalidOperationException("Preserve prior witness; no overwrite.");
Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
await File.WriteAllTextAsync(destination, native.GetRawText());
Console.WriteLine("Unchanged B25 Native witness saved: " + destination);
