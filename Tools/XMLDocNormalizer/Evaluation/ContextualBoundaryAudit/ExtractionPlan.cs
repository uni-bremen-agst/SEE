using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

internal static class ExtractionPlan
{
    internal static void Write(
        Compilation compilation, HashSet<string> core, HashSet<string> entries,
        string rootDirectory, string outputPath)
    {
        const string oldOwner = "ExceptionFlowAnalyzer";
        const string newOwner = "ExceptionFlowContextualFactEvaluator";
        List<string> patches = [];
        List<object> files = [];
        int movedCount = 0;
        int ingressSites = 0;
        HashSet<(string Caller, string Callee)> ingressEdges = [];
        string Id(ISymbol s) => s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        foreach (SyntaxTree tree in compilation.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal))
        {
            if (tree.FilePath.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            SemanticModel model = compilation.GetSemanticModel(tree);
            CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();
            SourceText text = tree.GetText();
            List<MemberDeclarationSyntax> moved = [];
            List<ClassDeclarationSyntax> wholeClasses = [];
            foreach (ClassDeclarationSyntax type in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                         .Where(t => model.GetDeclaredSymbol(t)?.Name == oldOwner))
            {
                List<MemberDeclarationSyntax> selected = type.Members.Where(m =>
                    m is MethodDeclarationSyntax method && model.GetDeclaredSymbol(method) is IMethodSymbol s && core.Contains(Id(s))
                    || m is FieldDeclarationSyntax field && field.Declaration.Variables.Any(v => v.Identifier.ValueText == "conditionalWeakTableValueFactCaches")
                    || m is ClassDeclarationSyntax nested && nested.Identifier.ValueText == "ConditionalWeakTableValueFactCachePartition").ToList();
                moved.AddRange(selected);
                if (selected.Count == type.Members.Count)
                {
                    wholeClasses.Add(type);
                }
            }

            List<(TextSpan Span, string Replacement)> changes = [];
            foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol target || !core.Contains(Id(target)))
                {
                    continue;
                }

                MethodDeclarationSyntax? caller = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (caller != null && model.GetDeclaredSymbol(caller) is IMethodSymbol symbol && core.Contains(Id(symbol)))
                {
                    continue;
                }

                changes.Add((invocation.Expression.Span, newOwner + "." + target.Name));
                ingressEdges.Add((Id(model.GetDeclaredSymbol(caller!)!), Id(target)));
                ingressSites++;
            }

            if (moved.Count == 0 && changes.Count == 0)
            {
                continue;
            }

            string relative = Path.GetRelativePath(rootDirectory, tree.FilePath).Replace('\\', '/');
            if (moved.Count > 0)
            {
                string name = Path.GetFileName(tree.FilePath).Replace(oldOwner, newOwner, StringComparison.Ordinal);
                string newPath = Path.GetDirectoryName(relative)!.Replace('\\', '/') + "/" + name;
                string members = "";
                foreach (MemberDeclarationSyntax member in moved.OrderBy(m => m.SpanStart))
                {
                    MemberDeclarationSyntax output = member;
                    if (member is MethodDeclarationSyntax method)
                    {
                        IMethodSymbol symbol = (IMethodSymbol)model.GetDeclaredSymbol(method)!;
                        movedCount++;
                        SyntaxKind visibility = entries.Contains(Id(symbol)) ? SyntaxKind.InternalKeyword : SyntaxKind.PrivateKeyword;
                        output = method.WithModifiers(SyntaxFactory.TokenList(method.Modifiers.Select(t =>
                            t.IsKind(SyntaxKind.InternalKeyword) || t.IsKind(SyntaxKind.PrivateKeyword)
                                ? SyntaxFactory.Token(t.LeadingTrivia, visibility, t.TrailingTrivia) : t)));
                    }

                    members += output.ToFullString();
                }

                string source = string.Concat(root.Usings.Select(u => u.ToFullString()))
                    + "\nnamespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow\n{\n"
                    + "    /// <summary>\n"
                    + "    /// Evaluates contextual value, symbol, sequence, and call facts.\n"
                    + "    /// </summary>\n"
                    + "    internal static partial class " + newOwner + "\n    {\n"
                    + members + "    }\n}\n";
                patches.Add("*** Begin Patch\n*** Add File: " + newPath + "\n"
                    + string.Join("\n", Lines(source).Select(l => "+" + l)) + "\n*** End Patch");

                files.Add(new { Old = relative, New = newPath, Methods = moved.OfType<MethodDeclarationSyntax>().Select(m => Id(model.GetDeclaredSymbol(m)!)).ToArray() });
            }

            bool deleteFile = moved.Count > 0 && root.Members.All(m => m is NamespaceDeclarationSyntax ns
                && ns.Members.All(n => n is ClassDeclarationSyntax c && wholeClasses.Contains(c)));
            if (deleteFile)
            {
                if (changes.Count != 0)
                {
                    throw new InvalidOperationException("Deleting file with retained ingress: " + relative);
                }

                patches.Add("*** Begin Patch\n*** Delete File: " + relative + "\n*** End Patch");
                continue;
            }

            IEnumerable<SyntaxNode> removed = wholeClasses.Cast<SyntaxNode>().Concat(moved.Where(m =>
                !wholeClasses.Any(c => c.FullSpan.Contains(m.FullSpan))));
            foreach (SyntaxNode node in removed)
            {
                int start = text.Lines.GetLineFromPosition(node.FullSpan.Start).Start;
                int end = text.Lines.GetLineFromPosition(node.FullSpan.End - 1).EndIncludingLineBreak;
                changes.Add((TextSpan.FromBounds(start, end), ""));
            }

            // Compute by syntax spans before emitting a single unambiguous file hunk.
            // One-line patches can otherwise select an identical SCC-internal call.
            string before = text.ToString();
            string after = before;
            foreach (var change in changes.OrderByDescending(c => c.Span.Start))
            {
                after = after.Remove(change.Span.Start, change.Span.Length)
                    .Insert(change.Span.Start, change.Replacement);
            }

            patches.Add("*** Begin Patch\n*** Update File: " + relative + "\n@@\n"
                + string.Join("\n", Lines(before).Select(l => "-" + l)) + "\n"
                + string.Join("\n", Lines(after).Select(l => "+" + l)) + "\n*** End Patch");
        }

        if (movedCount != 63 || ingressEdges.Count != 16)
        {
            throw new InvalidOperationException($"Unreviewed extraction boundary: {movedCount} methods, {ingressSites} ingress sites");
        }

        string directory = Path.Combine(Path.GetDirectoryName(outputPath)!, "extraction-patches");
        Directory.CreateDirectory(directory);
        for (int i = 0; i < patches.Count; i++)
        {
            File.WriteAllText(Path.Combine(directory, $"{i:D3}.patch"), patches[i]);
        }

        File.WriteAllText(outputPath, JsonSerializer.Serialize(new
        {
            SccMethods = movedCount,
            IngressSites = ingressSites,
            IngressEdges = ingressEdges.Count,
            Files = files,
            Patches = patches.Select((_, i) => Path.Combine(directory, $"{i:D3}.patch"))
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Extraction plan only: {movedCount} SCC methods, {ingressSites} call sites, {patches.Count} patches; production not written");
    }

    private static List<string> Lines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        return text.EndsWith('\n') ? lines.SkipLast(1).ToList() : lines.ToList();
    }
}

