using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Measures the complete Analyzer/evaluator composition boundary using bound symbols,
/// including all seed users and structural forwarder candidates rather than name guesses.
/// </summary>
internal static class CompositionAudit
{
    internal static object Measure(Compilation compilation, IEnumerable<Callable> sourceNodes, IEnumerable<Edge> edges)
    {
        const string analyzerName = "ExceptionFlowAnalyzer";
        const string evaluatorName = "ExceptionFlowContextualFactEvaluator";
        Callable[] nodes = sourceNodes.ToArray();
        Edge[] calls = edges.ToArray();
        string Id(ISymbol symbol) => symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        List<object> boundary = [];
        List<object> candidates = [];
        List<object> sites = [];
        List<object> census = [];
        List<object> evaluatorCensus = [];
        List<object> seedEntries = [];
        HashSet<string> seedIds = new(StringComparer.Ordinal);
        List<ClassDeclarationSyntax> analyzerTypes = [];

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            analyzerTypes.AddRange(tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(type => type.Identifier.ValueText == analyzerName));
            foreach (MethodDeclarationSyntax syntax in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                IMethodSymbol method = model.GetDeclaredSymbol(syntax)!;
                if (method.ContainingType.Name is not (analyzerName or evaluatorName or "ExceptionFlowLocalSourceAnalyzer"))
                {
                    continue;
                }

                Callable node = nodes.Single(node => node.Id == Id(method));
                InvocationExpressionSyntax[] invocations = syntax.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>().ToArray();
                var bound = invocations.Select(invocation => new
                {
                    Invocation = invocation,
                    Target = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol
                }).Where(call => call.Target != null).ToArray();
                var direct = bound.Where(call => call.Target!.ContainingType.Name == evaluatorName).ToArray();
                bool onlyForward = direct.Length == 1 && bound.Length == 1
                    && (syntax.ExpressionBody?.Expression == direct[0].Invocation
                        || syntax.Body?.Statements is { Count: 1 } statements
                        && statements[0] is ReturnStatementSyntax returned && returned.Expression == direct[0].Invocation);
                bool seedForward = direct.Length == 1 && bound.Length == 1
                    && syntax.Body?.Statements is { Count: 2 } seedStatements
                    && seedStatements[0] is LocalDeclarationStatementSyntax local
                    && model.GetDeclaredSymbol(local.Declaration.Variables.Single()) is ILocalSymbol symbol
                    && symbol.Type.ToDisplayString() == "System.Collections.Generic.HashSet<Microsoft.CodeAnalysis.ISymbol>"
                    && seedStatements[1] is ReturnStatementSyntax seedReturn && seedReturn.Expression == direct[0].Invocation;
                string shape = onlyForward ? "PureForwarder" : seedForward ? "GuardSeedOnly" : "OrchestrationOrOtherLogic";
                var incoming = calls.Where(call => call.Callee == node.Id).ToArray();
                var description = new
                {
                    node.Id,
                    node.Owner,
                    node.File,
                    node.Line,
                    node.Visibility,
                    Shape = shape,
                    StatementCount = syntax.Body?.Statements.Count,
                    InvocationCount = bound.Length,
                    DirectEvaluatorInvocations = direct.Length,
                    Calls = calls.Where(call => call.Caller == node.Id).ToArray(),
                    Callers = incoming,
                    HasUsersOutsideAnalyzer = incoming.Any(call => nodes.SingleOrDefault(node => node.Id == call.Caller)?.Symbol.ContainingType.Name != analyzerName),
                    Body = syntax.Body?.ToString() ?? syntax.ExpressionBody?.ToString(),
                    BodyTokenHash = Hash(syntax.Body ?? (SyntaxNode?)syntax.ExpressionBody),
                    BodyTokenHashModuloEvaluatorQualification = Hash(syntax.Body ?? (SyntaxNode?)syntax.ExpressionBody, true),
                    DeclarationTokenHash = Hash(syntax)
                };
                if (method.ContainingType.Name == analyzerName)
                {
                    census.Add(description);
                    if (direct.Length > 0)
                    {
                        boundary.Add(description);
                    }

                    if (onlyForward || seedForward)
                    {
                        candidates.Add(description);
                    }
                }
                else if (method.ContainingType.Name == evaluatorName)
                {
                    evaluatorCensus.Add(description);
                }

                if (seedForward)
                {
                    seedEntries.Add(description);
                    if (method.Name is "CreateCallContext" or "IsDefinitelyNonNull")
                    {
                        seedIds.Add(node.Id);
                    }
                }

                foreach (var call in direct)
                {
                    sites.Add(new
                    {
                        Caller = node.Id,
                        CallerOwner = node.Owner,
                        Callee = Id(call.Target!),
                        CalleeOwner = call.Target!.ContainingType.ToDisplayString(),
                        node.File,
                        Line = call.Invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Expression = call.Invocation.Expression.ToString(),
                        Syntax = call.Invocation.ToString()
                    });
                }
            }
        }

        Edge[] externalEvaluatorEdges = calls.Where(call => call.Owner.EndsWith("." + evaluatorName, StringComparison.Ordinal)
            && nodes.SingleOrDefault(node => node.Id == call.Caller)?.Symbol.ContainingType.Name != evaluatorName).ToArray();
        List<object> seedSites = [];
        foreach (Callable caller in nodes)
        {
            SemanticModel model = compilation.GetSemanticModel(caller.Syntax.SyntaxTree);
            foreach (InvocationExpressionSyntax invocation in caller.Syntax.DescendantNodes(
                node => node is not LocalFunctionStatementSyntax).OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol target && seedIds.Contains(Id(target)))
                {
                    seedSites.Add(new
                    {
                        Caller = caller.Id,
                        Callee = Id(target),
                        caller.File,
                        Line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        ArgumentTokenHash = Hash(invocation.ArgumentList)
                    });
                }
            }
        }
        return new
        {
            Definition = "All Analyzer method declarations inspected structurally; exact bound invocations, distinct kind-labelled directed edges and all bound source callers. Direct return-only and fresh HashSet<ISymbol>-seed-only bodies are candidates, not automatic removal decisions. All other direct callers require documented semantic review.",
            AnalyzerMethods = census,
            EvaluatorMethods = evaluatorCensus,
            GuardSeedEntries = seedEntries,
            SeedInvocationSites = seedSites,
            EvaluatorCacheSupportMethods = nodes.Where(node => node.Kind == "MethodDeclaration"
                && node.Owner.Contains(evaluatorName + ".", StringComparison.Ordinal))
                .Select(node => new { node.Id, DeclarationTokenHash = Hash(node.Syntax) }).ToArray(),
            EvaluatorFields = compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes()
                .OfType<FieldDeclarationSyntax>().Where(field => field.Declaration.Variables.Any(variable =>
                    compilation.GetSemanticModel(tree).GetDeclaredSymbol(variable) is IFieldSymbol symbol
                    && symbol.ContainingType.ToDisplayString().Contains("." + evaluatorName, StringComparison.Ordinal)))
                .Select(field => new
                {
                    Id = Id(compilation.GetSemanticModel(tree).GetDeclaredSymbol(field.Declaration.Variables.First())!),
                    DeclarationTokenHash = Hash(field)
                })).ToArray(),
            AnalyzerToEvaluatorMethods = boundary,
            ThinFacadeCandidates = candidates,
            DirectInvocationSites = sites,
            EvaluatorExternalIngress = externalEvaluatorEdges,
            AnalyzerDeclarations = analyzerTypes.Count,
            AnalyzerDeclarationFiles = analyzerTypes.Select(type => type.SyntaxTree.FilePath).Distinct(StringComparer.Ordinal).Count(),
            AnalyzerOwnedNonblankLines = analyzerTypes.Sum(type => type.ToFullString().Split('\n').Count(line => !string.IsNullOrWhiteSpace(line))),
            EmptyAnalyzerDeclarations = analyzerTypes.Where(type => type.Members.Count == 0).Select(type => type.SyntaxTree.FilePath).ToArray()
        };
    }

    private static string Hash(SyntaxNode? syntax, bool normalizeQualification = false) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(string.Join("\n", (syntax?.DescendantTokens() ?? [])
            .Where(token => !normalizeQualification || !IsEvaluatorQualifier(token))
            .Select(token => token.RawKind + ":" + token.Text)))));

    private static bool IsEvaluatorQualifier(SyntaxToken token)
    {
        MemberAccessExpressionSyntax? access = token.Parent switch
        {
            IdentifierNameSyntax identifier when identifier.Identifier.ValueText == "ExceptionFlowContextualFactEvaluator"
                => identifier.Parent as MemberAccessExpressionSyntax,
            MemberAccessExpressionSyntax member when token.IsKind(SyntaxKind.DotToken) => member,
            _ => null
        };
        return access?.Expression.ToString() == "ExceptionFlowContextualFactEvaluator"
            && access.Parent is InvocationExpressionSyntax;
    }
}
