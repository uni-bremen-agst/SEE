using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class WorkerFixturePreflight
{
    private const BindingFlags All = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static void Run(string tool, string export, string bin)
    {
        string output = Path.Combine(tool, "artifacts/p5o2b25a10/worker-fixture-families-r1.json");
        if (File.Exists(output)) throw new InvalidOperationException("Preserve worker fixture observations.");
        // Reviewed allowlist: Payload/FrameworkImages/Description use memory only (framework files are READ).
        // Native uses in-memory compiler/session/detector only. NEVER invoke Original/ValidatedInput/Probe/Evidence/tests.
        string[] files = ["ExceptionFlowHistoricalReferenceIdentityTests", "ExceptionFlowHistoricalScopeTests",
            "ExceptionFlowHistoricalDeclaredBoundaryTests", "ExceptionFlowHistoricalDeclaredPolicyTests",
            "ExceptionFlowHistoricalFindingContractTests", "ExceptionFlowHistoricalDeclaredProductionFindingTests",
            "ExceptionFlowHistoricalBoundedFindingTests"];
        foreach (string file in files)
        {
            string relative = "Tests/XMLDocNormalizerTests/Worker/" + file + ".cs";
            if (File.ReadAllText(Path.Combine(tool, relative)).Replace("\r\n", "\n", StringComparison.Ordinal)
                != File.ReadAllText(Path.Combine(export, relative)).Replace("\r\n", "\n", StringComparison.Ordinal))
                throw new InvalidOperationException("Inherited fixture factory source mismatch: " + relative);
        }
        AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(bin, name.Name + ".dll"))
            ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
        Assembly tests = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var rows = new List<object>();
        var families = new Dictionary<string, string[]>
        {
            ["ExceptionFlowHistoricalDeclaredBoundaryTests"] = ["xml-only", "unused-local-type", "mixed-direct-transitive", "local-chain", "no-local-type", "uncertain"],
            ["ExceptionFlowHistoricalDeclaredPolicyTests"] = ["local", "external", "mixed", "combined", "unused", "generic", "partial", "nested", "uncertain"],
            ["ExceptionFlowHistoricalFindingContractTests"] = ["local", "external", "mixed", "combined", "unused", "partial", "nested", "exact", "base", "inheritdoc", "invalid", "no-doc", "alias", "method", "duplicate", "empty-tag"],
            ["ExceptionFlowHistoricalDeclaredProductionFindingTests"] = ["local", "external", "mixed", "combined", "unused", "partial", "nested", "exact", "base", "inheritdoc", "invalid", "no-doc", "alias", "method", "duplicate", "empty-tag", "chain", "guard-direct"],
            ["ExceptionFlowHistoricalBoundedFindingTests"] = ["local", "external", "mixed", "combined", "unused", "partial", "nested", "exact", "base", "inheritdoc", "invalid", "no-doc", "alias", "method", "duplicate", "empty-tag", "chain", "guard-direct", "empty", "direct-only"]
        };
        foreach (var family in families)
        {
            Type type = tests.GetType("XMLDocNormalizerTests.Worker." + family.Key, true)!;
            foreach (string scenario in family.Value)
            {
                object payload = type.GetMethod("Payload", All)!.Invoke(null, [scenario])!;
                MethodInfo nativeMethod = type.GetMethod("Native", All)!;
                object?[] nativeArguments = nativeMethod.GetParameters().Length == 1 ? [payload]
                    : [payload, Enum.Parse(nativeMethod.GetParameters()[1].ParameterType, "ProjectTransitive")];
                JsonElement native = JsonSerializer.SerializeToElement(nativeMethod.Invoke(null, nativeArguments), options);
                string source = (string)payload.GetType().GetProperty("Source")!.GetValue(payload)!;
                object context = payload.GetType().GetProperty("Context")!.GetValue(payload)!;
                var extra = ((IEnumerable)context.GetType().GetProperty("AdditionalSources")!.GetValue(context)!).Cast<object>()
                    .Select(s => new { Path = s.GetType().GetProperty("LogicalPath")!.GetValue(s), Source = s.GetType().GetProperty("Text")!.GetValue(s) }).ToArray();
                var references = ((IEnumerable)context.GetType().GetProperty("References")!.GetValue(context)!).Cast<object>()
                    .Select(r => new { Identity = r.GetType().GetProperty("Identity")!.GetValue(r), ModuleName = r.GetType().GetProperty("ModuleName")!.GetValue(r),
                        ModuleVersionId = r.GetType().GetProperty("ModuleVersionId")!.GetValue(r), Sha256 = r.GetType().GetProperty("Sha256")!.GetValue(r) }).ToArray();
                rows.Add(new { Family = family.Key, Scenario = scenario, Source = source,
                    SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))), AdditionalSources = extra, References = references, Native = native });
            }
        }
        File.WriteAllText(output, JsonSerializer.Serialize(new { Kind = "Read-only reviewed current-equivalent in-memory fixture factories/native analysis; no Worker or test/writer execution",
            SourceComparedToInheritedExport = files, Rows = rows }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Observed {rows.Count} exact source/reference fixture families: {output}");
    }
}
