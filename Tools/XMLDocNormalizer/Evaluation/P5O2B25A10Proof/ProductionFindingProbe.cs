using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Read-only audit of the inherited production binary. Does not execute input
// source programs or existing test methods, and does not change old assertions.
string tool = Path.GetFullPath(args[0]);
string export = Path.Combine(tool, "artifacts/b25a9-c777/Tools/XMLDocNormalizer");
string bin = Path.Combine(export, "Tests/XMLDocNormalizerTests/bin/Debug/net8.0");
string output = Path.Combine(tool, "artifacts/p5o2b25a10/production-finding-probe.json");
if (args.Length > 1 && args[1] == "consolidate-preflight")
{
    try { ConsolidatePreflight.Run(tool); }
    catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
    return;
}
if (args.Length > 1 && args[1] == "worker-fixtures")
{
    try { WorkerFixturePreflight.Run(tool, export, bin); }
    catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
    return;
}
if (args.Length > 1 && args[1] == "test-paths")
{
    TestPathInventory.Run(tool, bin);
    return;
}
if (args.Length > 1 && args[1] is "fixtures" or "fixtures-all" or "fixtures-expanded")
{
    FixturePreflight.Run(tool, export, bin, args[1] == "fixtures-all", args[1] == "fixtures-expanded");
    return;
}
if (args.Length > 1 && args[1] == "classify-fixtures")
{
    FixtureClassification.Run(tool, bin);
    return;
}
if (args.Length > 1 && args[1] == "map")
{
    string mapOutput = Path.Combine(tool, "artifacts/p5o2b25a10/production-finding-assertion-map.json");
    if (File.Exists(mapOutput)) throw new InvalidOperationException("Preserve assertion map.");
    using var input = JsonDocument.Parse(File.ReadAllText(output));
    var entries = new List<object>();
    foreach (var candidate in input.RootElement.GetProperty("Candidates").EnumerateArray()
        .GroupBy(c => c.GetProperty("Test").GetString(), StringComparer.Ordinal).Select(g => g.First()))
    {
        string relative = candidate.GetProperty("File").GetString()!;
        string id = candidate.GetProperty("Test").GetString()!;
        string methodName = id[(id.LastIndexOf('.') + 1)..];
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(tool, relative)));
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == methodName);
        var owner = method.Ancestors().OfType<ClassDeclarationSyntax>().First();
        var methods = owner.Members.OfType<MethodDeclarationSyntax>().ToDictionary(m => m.Identifier.ValueText, StringComparer.Ordinal);
        var assertions = new List<object>();
        var helpers = new List<string>();
        var pending = new Queue<MethodDeclarationSyntax>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        pending.Enqueue(method);
        while (pending.TryDequeue(out var current))
        {
            if (!seen.Add(current.Identifier.ValueText)) continue;
            foreach (var call in current.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression is MemberAccessExpressionSyntax member && member.Expression.ToString() == "Assert")
                    assertions.Add(new { Owner = current.Identifier.ValueText, Line = call.GetLocation().GetLineSpan().StartLinePosition.Line + 1, Assertion = call.ToString() });
                if (call.Expression is IdentifierNameSyntax name && name.Identifier.ValueText.StartsWith("Assert", StringComparison.Ordinal)
                    && methods.TryGetValue(name.Identifier.ValueText, out var helper))
                {
                    helpers.Add(call.ToString());
                    pending.Enqueue(helper);
                }
            }
        }
        entries.Add(new { Test = id, File = relative, Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            HelperCallChain = helpers.Distinct().ToArray(), Assertions = assertions,
            PermissionScope = "Only normal-finding-presence assertions for this method's existing cases if A9 proves coverage outside; no global helper edit and no raw/type-fact assertion change" });
    }
    File.WriteAllText(mapOutput, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Mapped {entries.Count} observed test methods to assertion/helper locations: {mapOutput}");
    return;
}
if (File.Exists(output)) throw new InvalidOperationException("Preserve first probe output; no overwrite.");
AssemblyLoadContext.Default.Resolving += (_, name) =>
    File.Exists(Path.Combine(bin, name.Name + ".dll")) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
Assembly tests = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
MethodInfo find = tests.GetType("XMLDocNormalizerTests.Helpers.CheckAssert", true)!
    .GetMethods(BindingFlags.Static | BindingFlags.Public).Single(m => m.Name == "FindSemanticExceptionFindingsForSource" && m.GetParameters().Length == 3);
MethodInfo analyze = tests.GetType("XMLDocNormalizerTests.Helpers.ExceptionFlowAnalyzerTestHelper", true)!
    .GetMethod("AnalyzeSolutionTransitively", BindingFlags.Static | BindingFlags.Public)!;
object Property(object value, string name) => value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(value)!;
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var observations = new List<object>();
var skipped = new List<object>();
int files = 0, constants = 0, analyzed = 0;
foreach (string helper in new[] { "CheckAssert.cs", "ExceptionFlowAnalyzerTestHelper.cs" })
{
    string relative = "Tests/XMLDocNormalizerTests/Helpers/" + helper;
    if (Hash(Path.Combine(tool, relative)) != Hash(Path.Combine(export, relative)))
        throw new InvalidOperationException("Invoked helper source differs: " + relative);
}
string root = Path.Combine(tool, "Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions");
foreach (string path in Directory.GetFiles(root, "*.cs").Order(StringComparer.Ordinal))
{
    string text = File.ReadAllText(path);
    if (!text.Contains("FindSemanticExceptionFindingsForSource", StringComparison.Ordinal)) continue;
    files++;
    var tree = CSharpSyntaxTree.ParseText(text, path: path);
    var model = CSharpCompilation.Create("ConstantExtraction", [tree]).GetSemanticModel(tree);
    foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
        .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword) && m.AttributeLists.Count > 0))
    {
        string id = method.Ancestors().OfType<ClassDeclarationSyntax>().First().Identifier.ValueText + "." + method.Identifier.ValueText;
        var variable = method.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault(v => v.Identifier.ValueText == "source");
        if (variable?.Initializer == null) { skipped.Add(new { Test = id, Reason = "No local source initializer; parameterized/delegated fixture not reconstructed" }); continue; }
        var constant = model.GetConstantValue(variable.Initializer.Value);
        if (!constant.HasValue || constant.Value is not string source) { skipped.Add(new { Test = id, Reason = "Source is not a statically evaluable constant" }); continue; }
        constants++;
        string relative = Path.GetRelativePath(tool, path);
        string inheritedPath = Path.Combine(export, relative);
        bool testSourceByteEqual = Hash(path) == Hash(inheritedPath);
        bool testSourceTextEqualAfterCrlfToLf = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            == File.ReadAllText(inheritedPath).Replace("\r\n", "\n", StringComparison.Ordinal);
        // We analyze the CURRENT source string, not the inherited compiled test
        // method. Record file differences; only the invoked helper/engine must
        // bind to unchanged implementations.
        var sourceTree = CSharpSyntaxTree.ParseText(source);
        string[] roots = sourceTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.GetLeadingTrivia().ToFullString().Contains("<summary>", StringComparison.Ordinal))
            .Select(m => m.Identifier.ValueText).Distinct(StringComparer.Ordinal).ToArray();
        if (roots.Length == 0) { skipped.Add(new { Test = id, Reason = "No documented method root; property/member or raw-only scenario" }); continue; }
        try
        {
            var uncertain = new List<object>();
            foreach (string name in roots)
            {
                object run = analyze.Invoke(null, [source, name, Array.Empty<MetadataReference>()])!;
                object result = Property(run, "Result");
                string[] reasons = ((IEnumerable)Property(result, "UncertainTargets")).Cast<string>().ToArray();
                if (reasons.Length > 0) uncertain.Add(new { Root = name, Reasons = reasons });
            }
            analyzed++;
            if (uncertain.Count == 0) continue;
            foreach (string mode in new[] { "ProjectTransitive", "SolutionTransitive" })
            {
                object choice = Enum.Parse(find.GetParameters()[1].ParameterType, mode);
                object result = find.Invoke(null, [source, choice, Array.Empty<MetadataReference>()])!;
                var normal = ((IEnumerable)result).Cast<object>()
                    .Where(f => (string)Property(Property(f, "Smell"), "ID") is "DOC610" or "DOC611")
                    .Select(f => new { Id = (string)Property(Property(f, "Smell"), "ID"), Message = (string)Property(f, "Message") }).ToArray();
                if (normal.Length == 0) continue;
                observations.Add(new { Test = id, File = relative, Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Mode = mode, UncertainRoots = uncertain, NormalFindings = normal, SourceSha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source))),
                    TestFileSha256 = Hash(path), InheritedTestFileSha256 = Hash(inheritedPath), testSourceByteEqual, testSourceTextEqualAfterCrlfToLf,
                    Classification = "Audit candidate only: verify actual original mode/assertions and coverage grammar before requesting edits" });
            }
        }
        catch (TargetInvocationException exception)
        {
            skipped.Add(new { Test = id, Reason = "Existing helper could not analyze this constant fixture", Error = exception.InnerException?.Message });
        }
    }
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
    Kind = "Read-only inherited-binary production finding audit, NOT A10 implementation validation",
    TestAssemblySha256 = Hash(Path.Combine(bin, "XMLDocNormalizerTests.dll")),
    Files = files, ConstantSources = constants, AnalyzedSources = analyzed, Candidates = observations,
    SkippedFixtures = skipped, ExhaustiveBehavioralCoverage = false, ExistingFilesChanged = false,
    CandidateIsNotEditPermission = true
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Production finding audit: {files} files, {constants} constant sources, {analyzed} analyzed, {observations.Count} mode observations, {skipped.Count} excluded fixtures. Output: {output}");
