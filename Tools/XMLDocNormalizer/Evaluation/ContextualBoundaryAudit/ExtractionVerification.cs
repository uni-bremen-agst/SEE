using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Compares extracted implementation tokens and state against the unchanged starting HEAD.
/// This is evidence generation only; it never edits or reconstructs production files.
/// </summary>
internal static class ExtractionVerification
{
    internal static object Measure(Compilation compilation, string rootDirectory, string baselineRef)
    {
        const string owner = "ExceptionFlowContextualFactEvaluator";
        Dictionary<string, CompilationUnitSyntax> originals = new(StringComparer.Ordinal);
        CompilationUnitSyntax Original(string relative)
        {
            if (!originals.TryGetValue(relative, out CompilationUnitSyntax? syntax))
            {
                ProcessStartInfo start = new("git")
                {
                    WorkingDirectory = rootDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                start.ArgumentList.Add("show");
                start.ArgumentList.Add(baselineRef + ":Tools/XMLDocNormalizer/" + relative);
                using Process process = Process.Start(start)!;
                string source = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(error);
                }

                originals[relative] = syntax = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
            }

            return syntax;
        }

        List<object> methods = [];
        List<object> fields = [];
        List<object> retainedTypes = [];
        List<object> retainedAnalyzerMethods = [];
        List<ClassDeclarationSyntax> analyzerDeclarations = [];
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (ClassDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                         .Where(declaration => declaration.Identifier.ValueText == "ExceptionFlowAnalyzer"))
            {
                analyzerDeclarations.Add(declaration);
                Original(Path.GetRelativePath(rootDirectory, tree.FilePath).Replace('\\', '/'));
            }

            foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                         .Where(method => model.GetDeclaredSymbol(method)?.ContainingType.Name == "ExceptionFlowAnalyzer"))
            {
                string relative = Path.GetRelativePath(rootDirectory, tree.FilePath).Replace('\\', '/');
                MethodDeclarationSyntax before = Original(relative).DescendantNodes().OfType<MethodDeclarationSyntax>().Single(candidate =>
                    candidate.Identifier.ValueText == method.Identifier.ValueText
                    && Fingerprint(candidate.ParameterList) == Fingerprint(method.ParameterList)
                    && ((ClassDeclarationSyntax)candidate.Parent!).Identifier.ValueText == "ExceptionFlowAnalyzer");
                retainedAnalyzerMethods.Add(new
                {
                    Id = model.GetDeclaredSymbol(method)!.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    File = relative,
                    EqualModuloEvaluatorQualification = BodyWithoutEvaluatorQualification(before)
                        == BodyWithoutEvaluatorQualification(method)
                });
            }

            foreach (ClassDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                         .Where(type => type.Identifier.ValueText == owner))
            {
                string relative = Path.GetRelativePath(rootDirectory, tree.FilePath).Replace('\\', '/');
                string oldRelative = relative.Replace(owner, "ExceptionFlowAnalyzer", StringComparison.Ordinal);
                CompilationUnitSyntax original = Original(oldRelative);
                foreach (MethodDeclarationSyntax method in type.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    string container = ((ClassDeclarationSyntax)method.Parent!).Identifier.ValueText;
                    MethodDeclarationSyntax before = original.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(candidate =>
                        candidate.Identifier.ValueText == method.Identifier.ValueText
                        && candidate.ParameterList.Parameters.Count == method.ParameterList.Parameters.Count
                        && ((ClassDeclarationSyntax)candidate.Parent!).Identifier.ValueText == container.Replace(owner, "ExceptionFlowAnalyzer", StringComparison.Ordinal));
                    string beforeHash = Fingerprint(before.Body ?? (SyntaxNode?)before.ExpressionBody);
                    string afterHash = Fingerprint(method.Body ?? (SyntaxNode?)method.ExpressionBody);
                    methods.Add(new
                    {
                        Id = model.GetDeclaredSymbol(method)!.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        BeforeFile = oldRelative,
                        AfterFile = relative,
                        BeforeBodyTokenHash = beforeHash,
                        AfterBodyTokenHash = afterHash,
                        Equal = beforeHash == afterHash,
                        ParametersEqual = Fingerprint(before.ParameterList) == Fingerprint(method.ParameterList),
                        BeforeVisibility = Visibility(before),
                        AfterVisibility = Visibility(method)
                    });
                }

                foreach (FieldDeclarationSyntax field in type.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    FieldDeclarationSyntax before = original.DescendantNodes().OfType<FieldDeclarationSyntax>().Single(candidate =>
                        candidate.Declaration.Variables.Single().Identifier.ValueText == field.Declaration.Variables.Single().Identifier.ValueText);
                    fields.Add(new
                    {
                        Id = model.GetDeclaredSymbol(field.Declaration.Variables.Single())!.ToDisplayString(),
                        BeforeFile = oldRelative,
                        AfterFile = relative,
                        BeforeTokenHash = Fingerprint(before),
                        AfterTokenHash = Fingerprint(field),
                        Equal = Fingerprint(before) == Fingerprint(field)
                    });
                }

                foreach (IGrouping<string, ClassDeclarationSyntax> retained in original.DescendantNodes().OfType<ClassDeclarationSyntax>()
                             .Where(candidate => !candidate.AncestorsAndSelf().OfType<ClassDeclarationSyntax>()
                                 .Any(parent => parent.Identifier.ValueText == "ExceptionFlowAnalyzer"))
                             .GroupBy(candidate => candidate.Identifier.ValueText, StringComparer.Ordinal))
                {
                    string currentPath = Path.Combine(rootDirectory, oldRelative);
                    IEnumerable<ClassDeclarationSyntax> current = CSharpSyntaxTree.ParseText(File.ReadAllText(currentPath)).GetRoot()
                        .DescendantNodes().OfType<ClassDeclarationSyntax>().Where(candidate => candidate.Identifier.ValueText == retained.Key);
                    retainedTypes.Add(new
                    {
                        Name = retained.Key,
                        File = oldRelative,
                        Partials = retained.Count(),
                        Equal = retained.Select(Fingerprint).Order(StringComparer.Ordinal)
                            .SequenceEqual(current.Select(Fingerprint).Order(StringComparer.Ordinal))
                    });
                }
            }
        }

        ClassDeclarationSyntax[] originalAnalyzerDeclarations = originals.Values.SelectMany(root => root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>().Where(declaration => declaration.Identifier.ValueText == "ExceptionFlowAnalyzer")).ToArray();
        return new
        {
            Baseline = baselineRef,
            Methods = methods,
            Fields = fields,
            RetainedTypesInMixedFiles = retainedTypes,
            RetainedAnalyzerMethods = retainedAnalyzerMethods,
            AnalyzerOwnershipSize = new
            {
                Definition = "Analyzer class declarations and nonblank lines within their FullSpan, including class documentation/nested state but excluding namespace/usings and other owners",
                BeforeDeclarations = originalAnalyzerDeclarations.Length,
                AfterDeclarations = analyzerDeclarations.Count,
                BeforeOwnedNonblankLines = originalAnalyzerDeclarations.Sum(OwnedLines),
                AfterOwnedNonblankLines = analyzerDeclarations.Sum(OwnedLines)
            }
        };
    }

    private static string Fingerprint(SyntaxNode? syntax) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n", syntax?.DescendantTokens().Select(token => token.RawKind + ":" + token.Text) ?? []))));

    private static string Visibility(MethodDeclarationSyntax method) => string.Join(" ", method.Modifiers
        .Where(token => token.IsKind(SyntaxKind.PrivateKeyword) || token.IsKind(SyntaxKind.InternalKeyword)
            || token.IsKind(SyntaxKind.PublicKeyword) || token.IsKind(SyntaxKind.ProtectedKeyword))
        .Select(token => token.Text));

    private static int OwnedLines(ClassDeclarationSyntax declaration) => declaration.ToFullString().Split('\n')
        .Count(line => !string.IsNullOrWhiteSpace(line));

    private static string BodyWithoutEvaluatorQualification(MethodDeclarationSyntax method)
    {
        SyntaxNode? body = method.Body ?? (SyntaxNode?)method.ExpressionBody;
        SyntaxToken[] tokens = body?.DescendantTokens().ToArray() ?? [];
        List<string> values = [];
        for (int index = 0; index < tokens.Length; index++)
        {
            if (tokens[index].Text == "ExceptionFlowContextualFactEvaluator"
                && index + 1 < tokens.Length && tokens[index + 1].IsKind(SyntaxKind.DotToken))
            {
                index++;
                continue;
            }

            values.Add(tokens[index].RawKind + ":" + tokens[index].Text);
        }

        return string.Join("\n", values);
    }
}
