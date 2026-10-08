using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.B1AProof;

string root = Path.GetFullPath(args[0]);
string output = Path.Combine(root, "artifacts/p5o2b1a");
using JsonDocument b1 = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
    "Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json")));
JsonElement surface = b1.RootElement.GetProperty("RoslynApiDrift");
string[] names = ["Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp"];
MetadataReference[] platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Where(p => !Path.GetFileName(p).StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
        && !Path.GetFileName(p).StartsWith("XMLDocNormalizer", StringComparison.Ordinal))
    .Select(p => MetadataReference.CreateFromFile(p)).ToArray();

(CSharpCompilation Compilation, Dictionary<string, ISymbol> Symbols, string[] Paths) World(bool historical)
{
    string version = historical ? "5.0.0-2.25451.107" : "5.0.0";
    string[] paths = names.Select(n => Path.Combine(output, "packages",
        n == names[0] ? "microsoft.codeanalysis.common" : "microsoft.codeanalysis.csharp",
        version, "lib/net8.0", n + ".dll")).ToArray();
    MetadataReference[] refs = paths.Select(p => MetadataReference.CreateFromFile(p)).ToArray();
    CSharpCompilation compilation = CSharpCompilation.Create("MetadataOnly", references: platform.Concat(refs),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.All));
    Dictionary<string, ISymbol> symbols = new(StringComparer.Ordinal);
    void Add(ISymbol s) => symbols.TryAdd(s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), s);
    void Type(INamedTypeSymbol type)
    {
        Add(type);
        foreach (ISymbol member in type.GetMembers())
        {
            Add(member);
            if (member is IPropertySymbol p)
            {
                if (p.GetMethod != null) Add(p.GetMethod);
                if (p.SetMethod != null) Add(p.SetMethod);
            }
            if (member is INamedTypeSymbol nested) Type(nested);
        }
    }
    void Namespace(INamespaceSymbol ns)
    {
        foreach (INamespaceSymbol child in ns.GetNamespaceMembers()) Namespace(child);
        foreach (INamedTypeSymbol type in ns.GetTypeMembers()) Type(type);
    }
    foreach (MetadataReference reference in refs)
        Namespace(((IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!).GlobalNamespace);
    return (compilation, symbols, paths);
}
var current = World(false);
var historical = World(true);
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
foreach (JsonElement assembly in b1.RootElement.GetProperty("HistoricalReferenceAssemblies").EnumerateArray())
{
    string file = Path.GetFileName(assembly.GetProperty("Path").GetString()!);
    string actual = historical.Paths.Single(p => Path.GetFileName(p) == file);
    if (Hash(actual) != assembly.GetProperty("Sha256").GetString()) throw new InvalidOperationException("Historical reference mismatch.");
}

List<object> tests = [];
void Check(string name, bool condition, object? detail = null)
{
    if (!condition) throw new InvalidOperationException("Proof failed: " + name);
    tests.Add(new { Name = name, Passed = true, Detail = detail });
}
Check("Current native API present", RuntimeAwaitCapability.IsSupported);
string code = """
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    class Awaitable { public Awaiter GetAwaiter() => default; }
    struct Awaiter : INotifyCompletion {
        public bool IsCompleted => true;
        public void GetResult() { }
        public void OnCompleted(Action action) { }
    }
    class ExtensionAwaitable { }
    static class Extensions { public static Awaiter GetAwaiter(this ExtensionAwaitable value) => default; }
    class C {
        public static void RuntimeHelper() { }
        public async Task TaskAwait() { await Task.CompletedTask; }
        public async Task GenericAwait() { await Task.FromResult(1); }
        public async Task ValueTaskAwait() { await new ValueTask(); }
        public async Task CustomAwait() { await new Awaitable(); }
        public async Task ExtensionAwait() { await new ExtensionAwaitable(); }
        public async Task DynamicAwait(dynamic d) { await d; }
    }
    """;
SyntaxTree testTree = CSharpSyntaxTree.ParseText(code);
CSharpCompilation testCompilation = current.Compilation.AddSyntaxTrees(testTree)
    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
Check("Current await fixtures bind", !testCompilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
Check("Current fixture corelib does not support runtime async", !testCompilation.SupportsRuntimeCapability(RuntimeCapability.RuntimeAsyncMethods));
SemanticModel testModel = testCompilation.GetSemanticModel(testTree);
List<AwaitExpressionInfo> infos = [];
foreach (AwaitExpressionSyntax node in testTree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>())
{
    AwaitExpressionInfo info = testModel.GetAwaitExpressionInfo(node);
    infos.Add(info);
    string name = node.Ancestors().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText;
    Check("Native equivalence: " + name, ReferenceEquals(info.RuntimeAwaitMethod, RuntimeAwaitCapability.GetMethod(info)),
        new { NativeNull = info.RuntimeAwaitMethod == null, Awaiter = info.GetAwaiterMethod?.ToDisplayString(), info.IsDynamic });
}
Check("Normal custom await has complete pattern", infos[3].GetAwaiterMethod != null
    && infos[3].IsCompletedProperty?.GetMethod != null && infos[3].GetResultMethod != null);
Check("Extension awaiter information preserved", infos[4].GetAwaiterMethod?.IsExtensionMethod == true);
SyntaxTree invalidTree = CSharpSyntaxTree.ParseText("class Broken { async System.Threading.Tasks.Task M() { await 42; } }");
CSharpCompilation invalidCompilation = current.Compilation.AddSyntaxTrees(invalidTree)
    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
AwaitExpressionInfo invalid = invalidCompilation.GetSemanticModel(invalidTree)
    .GetAwaitExpressionInfo(invalidTree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single());
Check("Incomplete binding native equivalence", invalid.GetAwaiterMethod == null
    && ReferenceEquals(invalid.RuntimeAwaitMethod, RuntimeAwaitCapability.GetMethod(invalid)));
Check("Default struct native equivalence", ReferenceEquals(RuntimeAwaitCapability.GetMethod(default), default(AwaitExpressionInfo).RuntimeAwaitMethod));

// Structural non-null proof using only the CURRENT assembly's internal constructor.
// This is deliberately not described as a real binder-generated runtime-async fixture.
ConstructorInfo ctor = typeof(AwaitExpressionInfo).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
    .Single(c => c.GetParameters().Length == 5);
IMethodSymbol target = testCompilation.GetTypeByMetadataName("C")!.GetMembers("RuntimeHelper").OfType<IMethodSymbol>().Single();
AwaitExpressionInfo syntheticDirect = (AwaitExpressionInfo)ctor.Invoke([null, null, null, target, false]);
AwaitExpressionInfo pattern = infos[3];
AwaitExpressionInfo syntheticPattern = (AwaitExpressionInfo)ctor.Invoke([
    pattern.GetAwaiterMethod, pattern.IsCompletedProperty, pattern.GetResultMethod, target, false]);
Check("Structural non-null, direct runtime helper", ReferenceEquals(target, syntheticDirect.RuntimeAwaitMethod)
    && ReferenceEquals(target, RuntimeAwaitCapability.GetMethod(syntheticDirect)));
Check("Structural non-null, runtime helper with complete pattern", ReferenceEquals(target, syntheticPattern.RuntimeAwaitMethod)
    && ReferenceEquals(target, RuntimeAwaitCapability.GetMethod(syntheticPattern)));
Check("Counterexample: complete pattern does not imply no runtime helper", syntheticPattern.GetAwaiterMethod != null
    && syntheticPattern.IsCompletedProperty?.GetMethod != null && syntheticPattern.GetResultMethod != null
    && syntheticPattern.RuntimeAwaitMethod != null);
MethodInfo read = typeof(RuntimeAwaitCapability).GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Static)!;
bool unavailableRejected = false;
try { read.Invoke(null, [pattern, null]); }
catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException) { unavailableRejected = true; }
Check("Missing capability rejects; never fabricates null", unavailableRejected);
Parallel.For(0, 10000, _ =>
{
    if (!ReferenceEquals(target, RuntimeAwaitCapability.GetMethod(syntheticPattern)))
        throw new InvalidOperationException("Concurrent accessor mismatch.");
});
Check("Concurrent cached accessor, 10000 calls", true);
for (int i = 0; i < 10000; i++) RuntimeAwaitCapability.GetMethod(syntheticPattern);
long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 100000; i++) RuntimeAwaitCapability.GetMethod(syntheticPattern);
long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
Check("Cached struct accessor has zero measured per-call allocation", allocated == 0, new { Calls = 100000, AllocatedBytes = allocated });

List<object> apiRecheck = [];
foreach (JsonElement member in surface.GetProperty("Members").EnumerateArray())
{
    string signature = member.GetProperty("Signature").GetString()!;
    bool active = current.Symbols.ContainsKey(signature);
    bool old = historical.Symbols.ContainsKey(signature);
    bool expectedOld = member.GetProperty("HistoricalContract").ValueKind != JsonValueKind.Null;
    if (!active || old != expectedOld) throw new InvalidOperationException("Additional API drift: " + signature);
    apiRecheck.Add(new
    {
        Signature = signature,
        CurrentPresent = active,
        HistoricalPresent = old,
        B1ExpectedPresenceEqual = true
    });
}
foreach (JsonElement type in surface.GetProperty("Types").EnumerateArray())
    if (!historical.Symbols.ContainsKey(type.GetProperty("Type").GetString()!)) throw new InvalidOperationException("Type drift.");
foreach (JsonElement constant in surface.GetProperty("EnumConstants").EnumerateArray())
{
    string id = constant.GetProperty("Api").GetString()!;
    if (!Equals(((IFieldSymbol)current.Symbols[id]).ConstantValue, ((IFieldSymbol)historical.Symbols[id]).ConstantValue))
        throw new InvalidOperationException("Enum drift: " + id);
}
INamedTypeSymbol oldInfo = historical.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.CSharp.AwaitExpressionInfo")!;
INamedTypeSymbol oldBound = historical.Compilation.GetTypeByMetadataName("Microsoft.CodeAnalysis.CSharp.BoundAwaitableInfo")!;
var historicalApi = new
{
    AwaitExpressionInfoMembers = oldInfo.GetMembers().Select(m => m.ToDisplayString()).ToArray(),
    InternalRuntimeAsyncMembers = oldBound.GetMembers().Where(m => m.Name.Contains("RuntimeAsync", StringComparison.Ordinal))
        .Select(m => new { m.Name, Accessibility = m.DeclaredAccessibility.ToString(), Signature = m.ToDisplayString() }).ToArray(),
    RuntimeAwaitPropertyAbsent = !oldInfo.GetMembers("RuntimeAwaitMethod").Any(),
    BoundAwaitableInfoAccessibility = oldBound.DeclaredAccessibility.ToString()
};

// Four exact expression substitutions, entirely in memory. No projected .cs file is written.
string capabilityPath = Path.Combine(root, "Evaluation/P5O2B1AProof/RuntimeAwaitCapability.cs");
List<object> projections = [];
List<SyntaxTree> sources = [];
foreach (JsonElement entry in b1.RootElement.GetProperty("SourceVerification").GetProperty("Sources").EnumerateArray())
{
    string file = entry.GetProperty("File").GetString()!;
    string path = Path.Combine(root, file);
    if (Hash(path) != entry.GetProperty("Sha256").GetString()) throw new InvalidOperationException("Productive source changed: " + file);
    SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: file);
    MemberAccessExpressionSyntax[] accesses = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>()
        .Where(n => n.Name.Identifier.ValueText == "RuntimeAwaitMethod").ToArray();
    if (accesses.Length > 0)
    {
        foreach (MemberAccessExpressionSyntax node in accesses)
            projections.Add(new
            {
                File = file,
                Line = tree.GetLineSpan(node.Span).StartLinePosition.Line + 1,
                From = node.ToString(),
                To = "XMLDocNormalizer.B1AProof.RuntimeAwaitCapability.GetMethod(awaitInfo)"
            });
        SyntaxNode projected = tree.GetRoot().ReplaceNodes(accesses, (original, _) =>
            SyntaxFactory.ParseExpression("XMLDocNormalizer.B1AProof.RuntimeAwaitCapability.GetMethod(awaitInfo)").WithTriviaFrom(original));
        tree = CSharpSyntaxTree.Create((CSharpSyntaxNode)projected, path: file);
    }
    sources.Add(tree);
}
if (sources.Count != 120 || projections.Count != 4) throw new InvalidOperationException("Unexpected projection boundary.");
sources.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(capabilityPath), path: capabilityPath));
sources.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root,
    "Evaluation/P5O2B1CompileProbe/CompileOnlySemanticEnvironment.cs"))));
sources.Add(CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; "
    + "global using System.IO; global using System.Linq; global using System.Net.Http; "
    + "global using System.Threading; global using System.Threading.Tasks;"));
List<object> compiles = [];
foreach (bool historicalBuild in new[] { false, true, true })
{
    string name = historicalBuild ? "HistoricalProjected" : "CurrentProjected";
    var world = historicalBuild ? historical : current;
    CSharpCompilation compilation = world.Compilation.WithAssemblyName("XMLDocNormalizer.B1A." + name)
        .AddSyntaxTrees(sources).WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable, generalDiagnosticOption: ReportDiagnostic.Error,
            deterministic: true));
    using MemoryStream pe = new();
    var emit = compilation.Emit(pe);
    string[] diagnostics = emit.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
        .Select(d => d.ToString()).ToArray();
    compiles.Add(new
    {
        Name = name,
        emit.Success,
        ErrorsAndWarnings = diagnostics,
        ImageSha256 = emit.Success ? Convert.ToHexString(SHA256.HashData(pe.ToArray())) : null
    });
    if (!emit.Success || diagnostics.Length != 0) throw new InvalidOperationException("Projected compile failed: " + string.Join("\n", diagnostics));
}

// Verify compiler source against Portable PDB document checksums, without loading historical code.
List<object> documents = [];
string? historicalSourceLink = null;
string pdbPath = Path.ChangeExtension(historical.Paths[1], ".pdb");
using (FileStream stream = File.OpenRead(pdbPath))
using (MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(stream))
{
    MetadataReader reader = provider.GetMetadataReader();
    foreach (CustomDebugInformationHandle handle in reader.CustomDebugInformation)
    {
        CustomDebugInformation debug = reader.GetCustomDebugInformation(handle);
        if (reader.GetGuid(debug.Kind) == new Guid("cc110556-a091-4d38-9fec-25ab9a351a6a"))
            historicalSourceLink = System.Text.Encoding.UTF8.GetString(reader.GetBlobBytes(debug.Value));
    }
    foreach (DocumentHandle handle in reader.Documents)
    {
        Document doc = reader.GetDocument(handle);
        string name = reader.GetString(doc.Name);
        string sourcePath = Path.Combine(output, "roslyn-sources", "historical-" + Path.GetFileName(name));
        if (!File.Exists(sourcePath)) continue;
        byte[] bytes = File.ReadAllBytes(sourcePath);
        byte[] expected = reader.GetBlobBytes(doc.Hash);
        byte[] actual = expected.Length == 32 ? SHA256.HashData(bytes) : SHA1.HashData(bytes);
        bool equal = expected.SequenceEqual(actual);
        byte[] crlfBytes = System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(sourcePath).Replace("\r\n", "\n").Replace("\n", "\r\n"));
        // Preserve the original UTF8 BOM when normalizing Git LF to build-checkout CRLF.
        if (bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }))
            crlfBytes = new byte[] { 239, 187, 191 }.Concat(crlfBytes).ToArray();
        byte[] crlfHash = expected.Length == 32 ? SHA256.HashData(crlfBytes) : SHA1.HashData(crlfBytes);
        bool crlfEqual = expected.SequenceEqual(crlfHash);
        documents.Add(new
        {
            Document = name,
            Source = sourcePath,
            Sha256 = Hash(sourcePath),
            PdbChecksumEqual = equal || crlfEqual,
            LineEndingNormalizationRequired = !equal && crlfEqual,
            PdbExpectedChecksum = Convert.ToHexString(expected),
            NormalizedSourceChecksum = Convert.ToHexString(crlfHash)
        });
        if (!equal && !crlfEqual) throw new InvalidOperationException("Source/PDB checksum mismatch: " + name);
    }
}
if (!documents.Any()) throw new InvalidOperationException("No compiler source/PDB comparisons performed.");
List<object> binaryMethods = [];
using (FileStream stream = File.OpenRead(historical.Paths[1]))
using (PEReader peReader = new(stream))
{
    MetadataReader reader = peReader.GetMetadataReader();
    string TypeName(TypeDefinitionHandle handle)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
    }
    string EntityName(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MethodDefinition => TypeName(reader.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType())
            + "." + reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
        HandleKind.FieldDefinition => TypeName(reader.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType())
            + "." + reader.GetString(reader.GetFieldDefinition((FieldDefinitionHandle)handle).Name),
        HandleKind.MemberReference => reader.GetString(reader.GetMemberReference((MemberReferenceHandle)handle).Name),
        HandleKind.MethodSpecification => EntityName(reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method),
        HandleKind.TypeDefinition => TypeName((TypeDefinitionHandle)handle),
        _ => handle.Kind.ToString()
    };
    Dictionary<short, OpCode> opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => op.Value);
    foreach (TypeDefinitionHandle typeHandle in reader.TypeDefinitions)
    {
        string owner = TypeName(typeHandle);
        foreach (MethodDefinitionHandle methodHandle in reader.GetTypeDefinition(typeHandle).GetMethods())
        {
            MethodDefinition method = reader.GetMethodDefinition(methodHandle);
            string name = reader.GetString(method.Name);
            bool selected = (owner.EndsWith(".AwaitExpressionInfo", StringComparison.Ordinal) && name == ".ctor")
                || (owner.EndsWith(".MemberSemanticModel", StringComparison.Ordinal) && name == "GetAwaitExpressionInfo")
                || (owner.EndsWith(".CSharpCompilation", StringComparison.Ordinal) && name == "IsRuntimeAsyncEnabledIn")
                || (owner.EndsWith(".CSharpOperationFactory", StringComparison.Ordinal) && name == "CreateBoundAwaitExpressionOperation")
                || (owner.EndsWith(".Binder", StringComparison.Ordinal)
                    && (name.Contains("AwaitableExpression", StringComparison.Ordinal) || name.Contains("RuntimeAwait", StringComparison.Ordinal)));
            if (!selected || method.RelativeVirtualAddress == 0) continue;
            byte[] il = peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
            List<object> instructions = [];
            List<string> dependencies = [];
            for (int offset = 0; offset < il.Length;)
            {
                int start = offset;
                short opcode = il[offset++];
                if (opcode == 0xfe) opcode = unchecked((short)(0xfe00 | il[offset++]));
                OpCode op = opcodes[opcode];
                int size = op.OperandType switch
                {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                    _ => 4
                };
                string? reference = null;
                if (op.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
                {
                    reference = EntityName(MetadataTokens.EntityHandle(BitConverter.ToInt32(il, offset)));
                    dependencies.Add(reference);
                }
                if (op.OperandType == OperandType.InlineString)
                    reference = reader.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(il, offset) & 0x00ffffff));
                instructions.Add(new
                {
                    Offset = start,
                    Opcode = op.Name,
                    Reference = reference,
                    Operand = size == 0 ? null : Convert.ToHexString(il.AsSpan(offset, size))
                });
                offset += size;
            }
            binaryMethods.Add(new
            {
                Owner = owner,
                Method = name,
                Parameters = method.GetParameters().Select(h => reader.GetString(reader.GetParameter(h).Name)).ToArray(),
                IlSha256 = Convert.ToHexString(SHA256.HashData(il)),
                Dependencies = dependencies,
                Instructions = instructions
            });
        }
    }
}
JsonElement binary = JsonSerializer.SerializeToElement(binaryMethods);
JsonElement binding = binary.EnumerateArray().Single(m => m.GetProperty("Method").GetString() == "GetAwaitableExpressionInfo"
    && m.GetProperty("Dependencies").GetArrayLength() > 1);
JsonElement infoFactory = binary.EnumerateArray().Single(m => m.GetProperty("Method").GetString() == "GetAwaitExpressionInfo");
string[] bindingRefs = binding.GetProperty("Dependencies").EnumerateArray().Select(e => e.GetString()!).ToArray();
string[] infoRefs = infoFactory.GetProperty("Dependencies").EnumerateArray().Select(e => e.GetString()!).ToArray();
bool bindingHasBothPaths = bindingRefs.Any(s => s.EndsWith(".IsRuntimeAsyncEnabledIn", StringComparison.Ordinal))
    && bindingRefs.Any(s => s.Contains("tryGetRuntimeAwaitHelper", StringComparison.Ordinal))
    && bindingRefs.Any(s => s.Contains("getRuntimeAwaitAwaiter", StringComparison.Ordinal))
    && bindingRefs.Any(s => s.EndsWith(".GetGetAwaiterMethod", StringComparison.Ordinal))
    && bindingRefs.Any(s => s.EndsWith(".GetGetResultMethod", StringComparison.Ordinal));
bool publicInfoOmitsHelper = infoRefs.Any(s => s.EndsWith(".get_GetAwaiter", StringComparison.Ordinal))
    && infoRefs.Any(s => s.EndsWith(".get_IsCompleted", StringComparison.Ordinal))
    && infoRefs.Any(s => s.EndsWith(".get_GetResult", StringComparison.Ordinal))
    && !infoRefs.Any(s => s.Contains("RuntimeAsync", StringComparison.Ordinal));
if (!bindingHasBothPaths || !publicInfoOmitsHelper)
    throw new InvalidOperationException("Historical binary semantic evidence does not match the source analysis.");
var loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => names.Contains(a.GetName().Name))
    .Select(a => new
    {
        Assembly = a.GetName().FullName,
        Version = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        Mvid = a.ManifestModule.ModuleVersionId,
        Sha256 = Hash(a.Location)
    }).ToArray();
if (loaded.Any(a => a.Version?.StartsWith("5.0.0-2.25451.107", StringComparison.Ordinal) == true))
    throw new InvalidOperationException("Historical runtime loaded.");
foreach (var assembly in loaded)
{
    JsonElement baseline = surface.GetProperty("LoadedRoslyn").EnumerateArray()
        .Single(a => a.GetProperty("Assembly").GetString() == assembly.Assembly);
    if (baseline.GetProperty("Mvid").GetString() != assembly.Mvid.ToString()
        || baseline.GetProperty("InformationalVersion").GetString() != assembly.Version)
        throw new InvalidOperationException("Current Roslyn identity differs from B1.");
    string name = assembly.Assembly!.Split(',')[0] + ".dll";
    if (Hash(current.Paths.Single(p => Path.GetFileName(p) == name)) != assembly.Sha256)
        throw new InvalidOperationException("Current metadata reference differs from the executed assembly.");
}
var report = new
{
    Decision = "NOT READY",
    Reason = "Null fallback unsafe: historical binder has runtime async but public AwaitExpressionInfo drops its helper target.",
    HistoricalVersion = "5.0.0-2.25451.107",
    CapabilitySourceSha256 = Hash(capabilityPath),
    HistoricalFallback = "Explicit unsupported rejection in compile-only experiment; no null fallback or productive helper.",
    CurrentTests = tests,
    HistoricalApi = historicalApi,
    ApiSurfaceRecheck = apiRecheck,
    HistoricalTypesPresent = 210,
    EqualEnumConstants = 84,
    AdditionalUsedApiDrifts = 0,
    Projection = projections,
    ProductiveSourceFiles = 120,
    ProjectedCompiles = compiles,
    ProjectedAnalyzerExecuted = false,
    PhysicalProjectedSources = 0,
    CompilerSourcePdbChecksums = documents,
    HistoricalBinaryMethodEvidence = binaryMethods,
    HistoricalBinaryAssertions = new { BindingHasBothRuntimeAndPatternPaths = bindingHasBothPaths, PublicAwaitInfoOmitsRuntimeHelper = publicInfoOmitsHelper },
    HistoricalSourceLink = historicalSourceLink,
    LoadedRoslyn = loaded,
    HistoricalRuntimeLoaded = false,
    RealNonNullRuntimeAwaitScenario = "Not generated: fixtures target installed net8.0 corelib without the runtime-async capability. Two explicit current-constructor structural non-null tests instead; not binder equivalence.",
    NextStep = "Prove an uncertainty-aware unavailable result and two caller guards, or exact historical helper reconstruction, before B2; method-or-null alone cannot represent missing capability."
};
File.WriteAllText(Path.Combine(output, "proof-results.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"B1A proof: {tests.Count} current checks; same capability source; projected Current/Historical/repeat compiles 0/0; {documents.Count} compiler source/PDB checksums; NOT READY semantic fallback gate.");
