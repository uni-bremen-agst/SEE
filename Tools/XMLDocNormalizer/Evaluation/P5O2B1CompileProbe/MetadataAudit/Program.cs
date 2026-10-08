using System.Text.Json;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// One current Roslyn engine reads two independent sets of PE metadata references.
// Historical Roslyn assemblies are never loaded for runtime execution.
string root = Path.GetFullPath(args[0]);
string historicalVersion = "5.0.0-2.25451.107";
using JsonDocument evidence = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
    "Evaluation/P5O2A6F-summary-orchestration-cycle-closure-audit.json")));
string[] assemblyNames = ["Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp"];
string currentFolder = Path.Combine(root, "src/XMLDocNormalizer/bin/Debug/net8.0");
string historicalFolder = Path.Combine(root, "artifacts/p5o2b1/packages");
string[] platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Where(path => !Path.GetFileName(path).StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
    .ToArray();
MetadataReference[] platformReferences = platform.Select(path => MetadataReference.CreateFromFile(path)).ToArray();

(CSharpCompilation Compilation, Dictionary<string, ISymbol> Symbols) ReadWorld(bool historical)
{
    string[] paths = assemblyNames.Select(name => historical
        ? Path.Combine(historicalFolder,
            name == "Microsoft.CodeAnalysis" ? "microsoft.codeanalysis.common" : "microsoft.codeanalysis.csharp",
            historicalVersion, "lib/net8.0", name + ".dll")
        : Path.Combine(currentFolder, name + ".dll")).ToArray();
    MetadataReference[] roslyn = paths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    CSharpCompilation compilation = CSharpCompilation.Create("MetadataOnly",
        references: platformReferences.Concat(roslyn));
    Dictionary<string, ISymbol> symbols = new(StringComparer.Ordinal);
    void Add(ISymbol symbol)
    {
        symbols.TryAdd(symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), symbol);
    }
    void ReadType(INamedTypeSymbol type)
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
            if (member is INamedTypeSymbol nested) ReadType(nested);
        }
    }
    void ReadNamespace(INamespaceSymbol ns)
    {
        foreach (INamespaceSymbol child in ns.GetNamespaceMembers()) ReadNamespace(child);
        foreach (INamedTypeSymbol type in ns.GetTypeMembers()) ReadType(type);
    }
    foreach (MetadataReference reference in roslyn)
    {
        ReadNamespace(((IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!).GlobalNamespace);
    }
    return (compilation, symbols);
}

var current = ReadWorld(false);
var historical = ReadWorld(true);
SymbolDisplayFormat nullableFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
    SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
    | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
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
foreach (JsonElement entry in evidence.RootElement.GetProperty("RoslynInventory").GetProperty("MetadataMembers").EnumerateArray())
{
    string signature = entry.GetProperty("Signature").GetString()!;
    if (!current.Symbols.TryGetValue(signature, out ISymbol? active))
        throw new InvalidOperationException("Current metadata does not match the A6F inventory: " + signature);
    historical.Symbols.TryGetValue(signature, out ISymbol? old);
    members.Add(new
    {
        Signature = signature,
        Sites = entry.GetProperty("Sites").Clone(),
        CurrentContract = Contract(active),
        HistoricalContract = old == null ? null : Contract(old),
        Classification = old == null ? "API absent historically"
            : Contract(active) == Contract(old) ? "Same used metadata contract" : "Metadata contract difference; inspect",
        Strategy = old == null ? "Bounded compile-time accessor capability proof required; no production fix in B1"
            : Contract(active) == Contract(old) ? "No adaptation necessary for compilation" : "Review exact contract delta before B2"
    });
}
List<object> types = [];
foreach (JsonElement entry in evidence.RootElement.GetProperty("RoslynInventory").GetProperty("Types").EnumerateArray())
{
    string name = entry.GetProperty("Type").GetString()!;
    if (!current.Symbols.ContainsKey(name)) throw new InvalidOperationException("Current type missing: " + name);
    historical.Symbols.TryGetValue(name, out ISymbol? old);
    types.Add(new { Type = name, HistoricalPresent = old != null, SourceFiles = entry.GetProperty("SourceFiles").Clone() });
}

// Check used enum constants as well: compile success alone would not detect changed numeric values.
List<SyntaxTree> trees = evidence.RootElement.GetProperty("SourceManifest").EnumerateArray()
    .Select(entry => entry.GetProperty("File").GetString()!)
    .Append("Evaluation/P5O2B1CompileProbe/CompileOnlySemanticEnvironment.cs")
    .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, file)), path: file)).ToList<SyntaxTree>();
trees.Add(CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; "
    + "global using System.IO; global using System.Linq; global using System.Net.Http; "
    + "global using System.Threading; global using System.Threading.Tasks;"));
CSharpCompilation source = current.Compilation.AddSyntaxTrees(trees)
    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
Diagnostic[] sourceErrors = source.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
if (sourceErrors.Length != 0) throw new InvalidOperationException(string.Join("\n", sourceErrors.Select(d => d.ToString())));
Dictionary<string, List<object>> enumSites = new(StringComparer.Ordinal);
foreach (SyntaxTree tree in trees)
{
    SemanticModel model = source.GetSemanticModel(tree);
    foreach (MemberAccessExpressionSyntax node in tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>())
    {
        if (model.GetSymbolInfo(node).Symbol is not IFieldSymbol field
            || field.ContainingType.TypeKind != TypeKind.Enum
            || !field.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)) continue;
        string id = field.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        if (!enumSites.TryGetValue(id, out List<object>? sites)) enumSites.Add(id, sites = []);
        SyntaxNode? declaration = node.Ancestors().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax);
        ISymbol? caller = declaration == null ? null : model.GetDeclaredSymbol(declaration);
        sites.Add(new
        {
            File = tree.FilePath,
            Line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1,
            Caller = caller?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
        });
    }
}
object[] enums = enumSites.OrderBy(p => p.Key).Select(pair =>
{
    IFieldSymbol active = (IFieldSymbol)current.Symbols[pair.Key];
    IFieldSymbol? old = historical.Symbols.GetValueOrDefault(pair.Key) as IFieldSymbol;
    return (object)new
    {
        Api = pair.Key,
        CurrentValue = active.ConstantValue,
        HistoricalValue = old?.ConstantValue,
        Equal = old != null && Equals(active.ConstantValue, old.ConstantValue),
        Sites = pair.Value
    };
}).ToArray();
string output = Path.Combine(root, "artifacts/p5o2b1/metadata-audit.json");
var loadedRoslyn = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => assemblyNames.Contains(a.GetName().Name))
    .Select(a => new
    {
        Assembly = a.GetName().FullName,
        InformationalVersion = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        Mvid = a.ManifestModule.ModuleVersionId
    }).ToArray();
bool historicalRuntimeLoaded = loadedRoslyn.Any(a => a.InformationalVersion?.StartsWith(historicalVersion, StringComparison.Ordinal) == true);
if (historicalRuntimeLoaded) throw new InvalidOperationException("Historical runtime assembly was loaded.");
File.WriteAllText(output, JsonSerializer.Serialize(new
{
    Engine = typeof(CSharpCompilation).Assembly.GetName().FullName,
    HistoricalRuntimeLoaded = historicalRuntimeLoaded,
    LoadedRoslyn = loadedRoslyn,
    Scope = "Used PE metadata contracts, types and enum constants; not behavioral equivalence",
    CurrentSourceCompileErrors = sourceErrors.Length,
    Members = members,
    Types = types,
    EnumConstants = enums
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Metadata audit: {members.Count} member contracts, {types.Count} types, {enums.Length} enum constants; {output}");
