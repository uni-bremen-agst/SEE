using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Source-only inventory. No existing test method, writer, process or source program is executed.
internal static class TestPathInventory
{
    internal static void Run(string tool, string bin)
    {
        string output = Path.Combine(tool, "artifacts/p5o2b25a10/test-path-inventory-r1.json");
        if (File.Exists(output)) throw new InvalidOperationException("Preserve inventory.");
        var trees = Directory.GetFiles(Path.Combine(tool, "Tests/XMLDocNormalizerTests"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(s => s is "bin" or "obj" or "OldFiles"))
            .Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), path: p)).ToArray();
        var references = Directory.GetFiles(bin, "*.dll").Where(p => Path.GetFileName(p) != "XMLDocNormalizerTests.dll")
            .Select(p => MetadataReference.CreateFromFile(p, Path.GetFileName(p) == "XMLDocNormalizer.ExceptionFlow.Core.dll"
                ? new MetadataReferenceProperties(aliases: ["ExceptionFlowCore"]) : MetadataReferenceProperties.Assembly)).ToList();
        references.AddRange(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(p => !references.Any(r => Path.GetFileName(r.Display) == Path.GetFileName(p))).Select(p => MetadataReference.CreateFromFile(p)));
        var global = CSharpSyntaxTree.ParseText("global using System; global using System.IO; global using System.Linq; global using System.Collections.Generic; global using System.Threading; global using System.Threading.Tasks; global using Xunit;");
        var compilation = CSharpCompilation.Create("XMLDocNormalizerTests", trees.Append(global), references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        var methods = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).ToArray();
        var bySymbol = new Dictionary<ISymbol, MethodDeclarationSyntax>(SymbolEqualityComparer.Default);
        var byClass = methods.GroupBy(m => m.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText + "." + m.Identifier.ValueText)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
        var models = trees.ToDictionary(t => t, t => compilation.GetSemanticModel(t));
        foreach (var m in methods) if (models[m.SyntaxTree].GetDeclaredSymbol(m) is {} s) bySymbol[s] = m;
        var calls = new Dictionary<MethodDeclarationSyntax, (string[] External, MethodDeclarationSyntax[] Helpers, string[] Unresolved)>();
        foreach (var m in methods)
        {
            var helpers = new HashSet<MethodDeclarationSyntax>(); var external = new SortedSet<string>(); var unresolved = new SortedSet<string>();
            foreach (var call in m.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var info = models[m.SyntaxTree].GetSymbolInfo(call);
                if (info.Symbol is IMethodSymbol target)
                {
                    if (bySymbol.TryGetValue(target.OriginalDefinition, out var helper)) helpers.Add(helper);
                    else if (target.ContainingNamespace.ToDisplayString().StartsWith("XMLDocNormalizer", StringComparison.Ordinal)) external.Add(target.ContainingType.Name + "." + target.Name);
                }
                else if (info.Symbol == null && call.Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Assert" } })
                    unresolved.Add(call.Expression.ToString());
                // Explicit reflection edges used by the established witness helpers.
                if (call.Expression is MemberAccessExpressionSyntax { Expression: TypeOfExpressionSyntax typeOf, Name.Identifier.ValueText: "GetMethod" }
                    && call.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal)
                {
                    string key = typeOf.Type.ToString().Split('.').Last() + "." + literal.Token.ValueText;
                    if (byClass.TryGetValue(key, out var reflected)) foreach (var r in reflected) helpers.Add(r);
                }
            }
            calls[m] = (external.ToArray(), helpers.ToArray(), unresolved.ToArray());
        }
        var rows = new List<object>();
        foreach (var method in methods.Where(m => m.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString() is "Fact" or "Theory")))
        {
            var seen = new HashSet<MethodDeclarationSyntax>(); var pending = new Queue<MethodDeclarationSyntax>(); pending.Enqueue(method);
            while (pending.TryDequeue(out var next)) { if (!seen.Add(next)) continue; foreach (var helper in calls[next].Helpers) pending.Enqueue(helper); }
            var entries = seen.SelectMany(m => calls[m].External).Distinct().Order().ToArray();
            var sources = seen.SelectMany(m => m.DescendantNodes().OfType<LiteralExpressionSyntax>())
                .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression) && (l.Token.ValueText.Contains("class ", StringComparison.Ordinal) || l.Token.ValueText.Contains("void Root", StringComparison.Ordinal)))
                .Select(l => new { File = Path.GetRelativePath(tool, l.SyntaxTree.FilePath), Line = Line(l), Text = l.Token.ValueText }).ToArray();
            var inline = method.AttributeLists.SelectMany(l => l.Attributes).Where(a => a.Name.ToString() == "InlineData").Select(a => a.ArgumentList!.Arguments.Select(v => v.ToString()).ToArray()).ToArray();
            var member = method.AttributeLists.SelectMany(l => l.Attributes).Where(a => a.Name.ToString() == "MemberData").Select(a => a.ToString()).ToArray();
            var asserts = seen.SelectMany(m => m.DescendantNodes().OfType<InvocationExpressionSyntax>())
                .Where(c => c.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "Assert" } })
                .Select(c => new { File = Path.GetRelativePath(tool, c.SyntaxTree.FilePath), Line = Line(c), Expression = c.ToString() }).ToArray();
            rows.Add(new { Test = method.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText + "." + method.Identifier.ValueText,
                File = Path.GetRelativePath(tool, method.SyntaxTree.FilePath), Line = Line(method), InlineCases = inline, MemberData = member,
                DirectCalls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(c => c.Expression.ToString()).Distinct().ToArray(),
                Entries = entries, Helpers = seen.Select(m => m.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText + "." + m.Identifier.ValueText).Order().ToArray(),
                SourceLiterals = sources, Assertions = asserts,
                UnresolvedCalls = seen.SelectMany(m => calls[m].Unresolved).Distinct().Order().ToArray() });
        }
        File.WriteAllText(output, JsonSerializer.Serialize(new { Kind = "Read-only all-test source and helper-call inventory; not behavioral classification",
            Files = trees.Select(t => new { Path = Path.GetRelativePath(tool, t.FilePath), Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(t.FilePath))) }),
            Rows = rows }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Indexed {trees.Length} current test/helper files, {rows.Count} attributed test methods: {output}");
    }
    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
