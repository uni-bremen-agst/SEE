using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Evidence tool: only CURRENT Roslyn executes. Historical binaries are PE references.
string root = Path.GetFullPath(args[0]);
using JsonDocument b1 = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
    "Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json")));
JsonElement baseline = b1.RootElement.GetProperty("RoslynApiDrift");
string historicalVersion = "5.0.0-2.25451.107";
string[] names = ["Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp"];
MetadataReference[] platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator).Where(path => !Path.GetFileName(path).StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
    .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

(CSharpCompilation Compilation, Dictionary<string, ISymbol> Members) World(bool historical)
{
    MetadataReference[] references = names.Select(name => MetadataReference.CreateFromFile(historical
        ? Path.Combine(root, "artifacts/p5o2b1a2/packages",
            name == "Microsoft.CodeAnalysis" ? "microsoft.codeanalysis.common" : "microsoft.codeanalysis.csharp",
            historicalVersion, "lib/net8.0", name + ".dll")
        : Path.Combine(root, "src/XMLDocNormalizer/bin/Debug/net8.0", name + ".dll"))).ToArray();
    CSharpCompilation compilation = CSharpCompilation.Create("SurfaceProof", references: platform.Concat(references),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.All));
    Dictionary<string, ISymbol> members = new(StringComparer.Ordinal);
    void Add(ISymbol symbol) => members.TryAdd(symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), symbol);
    void Type(INamedTypeSymbol type)
    {
        Add(type);
        foreach (ISymbol member in type.GetMembers())
        {
            Add(member);
            if (member is IPropertySymbol property)
            {
                if (property.GetMethod != null) Add(property.GetMethod);
                if (property.SetMethod != null) Add(property.SetMethod);
            }
            if (member is INamedTypeSymbol nested) Type(nested);
        }
    }
    void Namespace(INamespaceSymbol ns)
    {
        foreach (var child in ns.GetNamespaceMembers()) Namespace(child);
        foreach (var type in ns.GetTypeMembers()) Type(type);
    }
    foreach (var reference in references) Namespace(((IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!).GlobalNamespace);
    return (compilation, members);
}

var current = World(false);
var historical = World(true);
SymbolDisplayFormat nullableFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
    SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
string TypeName(ITypeSymbol type) => type.ToDisplayString(nullableFormat);
string Contract(ISymbol symbol)
{
    string prefix = $"{symbol.Kind}|{symbol.DeclaredAccessibility}|{symbol.IsStatic}|";
    return prefix + (symbol switch
    {
        IMethodSymbol method => $"{method.MethodKind}|{method.RefKind}|{TypeName(method.ReturnType)}|"
            + string.Join(";", method.Parameters.Select(p =>
                $"{p.RefKind}:{TypeName(p.Type)}:params={p.IsParams}:optional={p.IsOptional}:default="
                + (p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : "<none>")))
            + "|" + string.Join(";", method.TypeParameters.Select(p =>
                $"{p.Name}:ref={p.HasReferenceTypeConstraint}:value={p.HasValueTypeConstraint}:new={p.HasConstructorConstraint}:"
                + string.Join(",", p.ConstraintTypes.Select(TypeName)))),
        IPropertySymbol property => $"{property.RefKind}|{TypeName(property.Type)}",
        IFieldSymbol field => $"{TypeName(field.Type)}|constant={field.ConstantValue}",
        INamedTypeSymbol type => $"{type.TypeKind}|arity={type.Arity}|base={type.BaseType?.ToDisplayString()}|"
            + string.Join(";", type.Interfaces.Select(i => i.ToDisplayString()).Order()),
        _ => symbol.ToDisplayString(nullableFormat)
    });
}

List<object> members = [];
foreach (JsonElement entry in baseline.GetProperty("Members").EnumerateArray())
{
    string signature = entry.GetProperty("Signature").GetString()!;
    ISymbol active = current.Members[signature];
    historical.Members.TryGetValue(signature, out ISymbol? old);
    string activeContract = Contract(active);
    string? oldContract = old == null ? null : Contract(old);
    if (activeContract != entry.GetProperty("CurrentContract").GetString()
        || oldContract != entry.GetProperty("HistoricalContract").GetString())
        throw new InvalidOperationException("Contract changed: " + signature);
    members.Add(new
    {
        Signature = signature,
        CurrentContract = activeContract,
        HistoricalContract = oldContract,
        SameContract = activeContract == oldContract,
        ExpectedAbsence = old == null
    });
}
string[] types = baseline.GetProperty("Types").EnumerateArray().Select(entry => entry.GetProperty("Type").GetString()!).ToArray();
if (types.Any(type => !current.Members.ContainsKey(type) || !historical.Members.ContainsKey(type)))
    throw new InvalidOperationException("Used type missing.");
string capability = "src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowRuntimeAwaitCapability.cs";
string[] productive = b1.RootElement.GetProperty("SourceVerification").GetProperty("Sources").EnumerateArray()
    .Select(entry => entry.GetProperty("File").GetString()!).Append(capability).ToArray();
List<SyntaxTree> trees = productive.Append("Evaluation/P5O2B1CompileProbe/CompileOnlySemanticEnvironment.cs")
    .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, file)), path: file)).ToList();
trees.Add(CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using System.IO; "
    + "global using System.Linq; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;"));
var source = current.Compilation.AddSyntaxTrees(trees).WithOptions(current.Compilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));
Diagnostic[] errors = source.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
if (errors.Length != 0) throw new InvalidOperationException(string.Join("\n", errors.Select(d => d.ToString())));
SortedSet<string> usedConstants = new(StringComparer.Ordinal);
SortedSet<string> nativeRuntimeReads = new(StringComparer.Ordinal);
foreach (SyntaxTree tree in trees)
{
    SemanticModel model = source.GetSemanticModel(tree);
    foreach (MemberAccessExpressionSyntax node in tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>())
    {
        ISymbol? symbol = model.GetSymbolInfo(node).Symbol;
        if (symbol is IPropertySymbol property && property.Name == "RuntimeAwaitMethod"
            && property.ContainingType.ToDisplayString() == "Microsoft.CodeAnalysis.CSharp.AwaitExpressionInfo") nativeRuntimeReads.Add(tree.FilePath);
        if (symbol is IFieldSymbol field && field.ContainingType.TypeKind == TypeKind.Enum
            && field.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            usedConstants.Add(field.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
    }
}
if (nativeRuntimeReads.Count != 0) throw new InvalidOperationException("Productive native member reference remains.");
string[] expectedConstants = baseline.GetProperty("EnumConstants").EnumerateArray().Select(e => e.GetProperty("Api").GetString()!).Order(StringComparer.Ordinal).ToArray();
if (!usedConstants.SequenceEqual(expectedConstants)) throw new InvalidOperationException("Used enum surface changed.");
var enumProof = usedConstants.Select(name =>
{
    object? active = ((IFieldSymbol)current.Members[name]).ConstantValue;
    object? old = ((IFieldSymbol)historical.Members[name]).ConstantValue;
    if (!Equals(active, old)) throw new InvalidOperationException("Enum value changed: " + name);
    return new { Api = name, CurrentValue = active, HistoricalValue = old, Equal = true };
}).ToArray();
var loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => names.Contains(a.GetName().Name))
    .Select(a => new
    {
        Assembly = a.FullName,
        InformationalVersion = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
        Mvid = a.ManifestModule.ModuleVersionId,
        Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location)))
    }).ToArray();
if (loaded.Any(a => a.InformationalVersion.StartsWith(historicalVersion, StringComparison.Ordinal)))
    throw new InvalidOperationException("Historical runtime loaded.");
File.WriteAllText(Path.Combine(root, "artifacts/p5o2b1a2/surface-proof.json"), JsonSerializer.Serialize(new
{
    Members = members,
    Types = types,
    EnumConstants = enumProof,
    LoadedRoslyn = loaded,
    AdditionalApiDrifts = 0,
    CurrentSourceCompileErrors = errors.Length,
    HistoricalRuntimeLoaded = false,
    ProductiveFiles = productive.Select(file => new { File = file, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, file)))) }),
    ProductiveFileCount = productive.Length,
    DirectNativeRuntimeAwaitReads = nativeRuntimeReads.Count,
    Scope = "All original 430 metadata contracts, 210 types, 84 actually used enum values. Genuine productive sources; no rewriting, emit or historical execution."
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Surface proof: {members.Count} original contracts, {types.Length} types, {enumProof.Length} enum constants, {productive.Length} productive files, 0 added drift.");

if (args.Contains("--self-uncertainty", StringComparer.Ordinal))
{
    // Read-only diagnosis of self-analysis evidence drift. Reflect only OUR current
    // Analyzer entry points, never compiler internals or historical runtime DLLs.
    string mainFolder = Path.Combine(root, "src/XMLDocNormalizer/bin/Debug/net8.0");
    System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (_, name) =>
    {
        string path = Path.Combine(mainFolder, name.Name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    };
    Assembly main = Assembly.LoadFrom(Path.Combine(mainFolder, "XMLDocNormalizer.dll"));
    Type contextType = main.GetType("XMLDocNormalizer.Execution.Semantic.ProjectClosureSemanticContext")!;
    Type environmentType = main.GetType("XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSemanticEnvironment")!;
    Type sessionType = main.GetType("XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSummaryAnalysisSession")!;
    using JsonDocument assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src/XMLDocNormalizer/obj/project.assets.json")));
    Dictionary<string, string> referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator).GroupBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    var libraries = assets.RootElement.GetProperty("libraries");
    string[] folders = assets.RootElement.GetProperty("packageFolders").EnumerateObject().Select(p => p.Name).ToArray();
    foreach (var package in assets.RootElement.GetProperty("targets").EnumerateObject().First().Value.EnumerateObject())
    {
        if (!package.Value.TryGetProperty("compile", out var compile)) continue;
        string packagePath = libraries.GetProperty(package.Name).GetProperty("path").GetString()!;
        foreach (var entry in compile.EnumerateObject())
        {
            if (!entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
            string path = folders.Select(folder => Path.Combine(folder, packagePath, entry.Name)).First(File.Exists);
            referencePaths[Path.GetFileName(path)] = path;
        }
    }
    string[] changed = ["ExceptionFlowAnalyzer.SummaryGraphAwaits.cs", "ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs", "ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs"];
    string[] allFiles = b1.RootElement.GetProperty("SourceVerification").GetProperty("CurrentMainSourceFiles").EnumerateArray()
        .Select(entry => entry.GetProperty("File").GetString()!).Append(capability).ToArray();
    SortedSet<string> Analyze(bool before)
    {
        List<SyntaxTree> allTrees = [];
        foreach (string file in allFiles)
        {
            string text = File.ReadAllText(Path.Combine(root, file));
            if (before && changed.Contains(Path.GetFileName(file)))
            {
                var start = new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                start.ArgumentList.Add("show");
                start.ArgumentList.Add("HEAD:Tools/XMLDocNormalizer/" + file.Replace('\\', '/'));
                using var process = System.Diagnostics.Process.Start(start)!;
                text = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("git show failed.");
            }
            allTrees.Add(CSharpSyntaxTree.ParseText(text, path: Path.Combine(root, file)));
        }
        allTrees.Add(trees.Last()); // The same SDK global usings.
        CSharpCompilation compilation = CSharpCompilation.Create("XMLDocNormalizer", allTrees,
            referencePaths.Values.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var compileErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (compileErrors.Length != 0) throw new InvalidOperationException(string.Join("\n", compileErrors.Select(e => e.ToString())));
        var detectorTree = allTrees.Single(tree => Path.GetFileName(tree.FilePath) == "XmlDocExceptionSemanticDetector.cs");
        var member = detectorTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText == "FindExceptionSmells")
            .OrderByDescending(method => method.ParameterList.Parameters.Count).First();
        object context = contextType.GetMethod("CreateSingleCompilationContext")!.Invoke(null, [detectorTree, compilation])!;
        object environment = environmentType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke([context]);
        object session = sessionType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke([environment]);
        object result = sessionType.GetMethod("Analyze", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(session, [member])!;
        return new SortedSet<string>((IEnumerable<string>)result.GetType().GetProperty("UncertainTargets")!.GetValue(result)!, StringComparer.Ordinal);
    }
    var oldTargets = Analyze(true);
    var newTargets = Analyze(false);
    var added = newTargets.Except(oldTargets).ToArray();
    var removed = oldTargets.Except(newTargets).ToArray();
    File.WriteAllText(Path.Combine(root, "artifacts/p5o2b1a2/self-uncertainty-diagnostic.json"), JsonSerializer.Serialize(new
    {
        Before = oldTargets,
        After = newTargets,
        Added = added,
        Removed = removed,
        Scope = "One current compilation, source at HEAD versus WIP; current Analyzer only, no historical runtime."
    }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Self uncertainty added: " + string.Join(" | ", added));
    Console.WriteLine("Self uncertainty removed: " + string.Join(" | ", removed));
}
