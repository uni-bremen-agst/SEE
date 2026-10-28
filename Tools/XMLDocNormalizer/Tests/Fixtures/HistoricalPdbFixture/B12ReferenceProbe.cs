using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

/// <summary>Test-only genuine Historical emission/graph observation; never executes submitted source or reference code.</summary>
internal static class B12ReferenceProbe
{
    internal static void Run(string? output, bool emit, bool preserveParse = false, bool preserveScope = false)
    {
        string input = Console.In.ReadToEnd();
        if (input.Length > 48 * 1024 * 1024) { throw new ArgumentException("Fixture request too large."); }
        using var json = JsonDocument.Parse(input);
        var root = json.RootElement;
        string source = root.GetProperty("source").GetString()!;
        string path = root.GetProperty("logicalPath").GetString()!;
        var context = root.GetProperty("context");
        var images = context.GetProperty("references").EnumerateArray().ToArray();
        var references = images.Select((image, index) => MetadataReference.CreateFromImage(
            image.GetProperty("image").GetBytesFromBase64(),
            new MetadataReferenceProperties(MetadataImageKind.Assembly,
                aliases: [.. image.GetProperty("aliases").EnumerateArray().Select(alias => alias.GetString()!)]),
            filePath: "fixture-reference-" + index + ".dll")).ToArray();
        var parse = new CSharpParseOptions(LanguageVersion.CSharp12);
        if (preserveParse)
        {
            var description = context.GetProperty("parseContext");
            if (!LanguageVersionFacts.TryParse(description.GetProperty("languageVersion").GetString(), out var language)
                || !Enum.IsDefined(language)) { throw new ArgumentException("Unsupported fixture language."); }
            parse = new CSharpParseOptions(language, DocumentationMode.Parse, SourceCodeKind.Regular,
                description.GetProperty("preprocessorSymbols").EnumerateArray().Select(symbol => symbol.GetString()!));
        }
        var tree = CSharpSyntaxTree.ParseText(source, parse, path, Encoding.UTF8);
        SyntaxTree[] trees = [tree];
        if (preserveScope)
        {
            var owned = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal) { [path] = tree };
            foreach (var document in context.GetProperty("additionalSources").EnumerateArray())
            {
                var description = document.GetProperty("parseContext");
                if (!LanguageVersionFacts.TryParse(description.GetProperty("languageVersion").GetString(), out var language)
                    || !Enum.IsDefined(language)) { throw new ArgumentException("Unsupported fixture language."); }
                string logicalPath = document.GetProperty("logicalPath").GetString()!;
                owned.Add(logicalPath, CSharpSyntaxTree.ParseText(document.GetProperty("text").GetString()!,
                    new CSharpParseOptions(language, DocumentationMode.Parse, SourceCodeKind.Regular,
                        description.GetProperty("preprocessorSymbols").EnumerateArray().Select(symbol => symbol.GetString()!)), logicalPath, Encoding.UTF8));
            }
            trees = context.GetProperty("scope").GetProperty("orderedSourcePaths").EnumerateArray()
                .Select(item => owned[item.GetString()!]).ToArray();
        }
        var compilation = CSharpCompilation.Create(context.GetProperty("assemblyName").GetString()!, trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, nullableContextOptions: NullableContextOptions.Enable));
        if (!emit)
        {
            if (preserveScope) { B14ScopeProbe.Observe(compilation, path, context.GetProperty("analysisMode").GetString()!); }
            else { B11GraphProbe.Observe(compilation, includeSummaries: true); }
            return;
        }
        Directory.CreateDirectory(output!);
        using var pe = File.Create(Path.Combine(output!, "input.dll"));
        using var pdb = File.Create(Path.Combine(output!, "input.pdb"));
        var result = compilation.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: "input.pdb"));
        if (!result.Success) { throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics)); }
        Console.WriteLine("{\"testOnly\":true,\"sourceExecuted\":false,\"genuineHistoricalEmission\":true}");
    }
}
