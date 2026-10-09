using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

// Test-only fixture emits one fixed source through the actual existing Historical runtime.
// It is not an Analyzer/Worker, accepts no submitted source and never executes the emitted assembly.
const string expected = "5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0";
Assembly common = typeof(Compilation).Assembly;
Assembly csharp = typeof(CSharpCompilation).Assembly;
foreach (Assembly engine in new[] { common, csharp })
{
    if (engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion != expected)
    {
        throw new InvalidOperationException("Historical fixture loaded the wrong compiler runtime.");
    }
}
if (args.Length != 1) { throw new ArgumentException("One trusted fixture output directory is required."); }
string output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(output);
const string source = "public static class Fixture { public static void Root() { throw null; } }";
SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12), "/controlled-historical.cs", Encoding.UTF8);
string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll", "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
MetadataReference[] references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal)).Select(Path.GetFullPath)
    .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
    .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
if (references.Length != 7) { throw new InvalidOperationException("Historical fixture reference profile is incomplete."); }
CSharpCompilation compilation = CSharpCompilation.Create("HistoricalSelectionInput", [tree], references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, nullableContextOptions: NullableContextOptions.Enable));
string pePath = Path.Combine(output, "historical-input.dll");
string pdbPath = Path.Combine(output, "historical-input.pdb");
using (FileStream pe = File.Create(pePath))
using (FileStream pdb = File.Create(pdbPath))
{
    EmitResult result = compilation.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: "historical-input.pdb"));
    if (!result.Success) { throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics)); }
}
var evidence = new
{
    TestOnly = true,
    SubmittedSourceAccepted = false,
    EmittedSourceExecuted = false,
    CompilerRuntime = new[] { common, csharp }.Select(assembly => new
    {
        Name = assembly.GetName().Name,
        InformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
        Mvid = assembly.ManifestModule.ModuleVersionId,
        Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
    }).ToArray(),
    PeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pePath))),
    PdbSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(pdbPath)))
};
string json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(output, "emission-evidence.json"), json);
Console.WriteLine(json);
