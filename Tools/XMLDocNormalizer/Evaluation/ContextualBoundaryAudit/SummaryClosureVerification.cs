using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Verifies the bounded summary-owner move against committed sources rebound
/// in memory, with an explicit allowlist of qualification/accessibility changes.
/// </summary>
internal static class SummaryClosureVerification
{
    private const string Prefix = "XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.";
    private const string Analyzer = Prefix + "ExceptionFlowAnalyzer";
    private const string Builder = Prefix + "ExceptionFlowSummaryGraphBuilder";
    private const string Session = Prefix + "ExceptionFlowSummaryAnalysisSession";
    private const string Registrar = Prefix + "ExceptionFlowSummaryTargetRegistrar";

    internal static object Measure(Compilation current, string root, string reference)
    {
        Compilation baseline = CompositionBaseline.Load(current, root, reference);
        Dictionary<string, Snapshot> before = Methods(baseline, true);
        Dictionary<string, Snapshot> after = Methods(current, false);
        string Destination(Snapshot old) => (old.Owner, old.Name) switch
        {
            (Analyzer, "CreateSummaryAnalysisSession" or "AnalyzeSolutionTransitivelyThrownExceptions") => Session,
            (Builder, "TryBuildTransitiveSummaryGraph" or "BuildPendingSummaryNodes") => Session,
            (Builder, "AnalyzeSummarySymbolDeclarations" or "AnalyzeSummaryLocalFunction"
                or "AnalyzeSummaryAnonymousFunction" or "AnalyzeSummaryAccessor") => Analyzer,
            (Builder, "HasAnalyzableSummaryInvocationBody" or "TryGetSummaryInvocationBody") => Registrar,
            _ => old.Owner
        };
        var comparisons = before.Values.OrderBy(old => old.Id, StringComparer.Ordinal).Select(old =>
        {
            string destination = Destination(old);
            string newId = destination == old.Owner ? old.Id : destination + old.Id[old.Owner.Length..];
            after.TryGetValue(newId, out Snapshot? found);
            return new
            {
                BeforeId = old.Id,
                AfterId = newId,
                Moved = destination != old.Owner,
                Before = old,
                After = found,
                ExactDeclaration = old.DeclarationHash == found?.DeclarationHash,
                ExactBody = old.BodyHash == found?.BodyHash,
                EqualModuloExplicitOwnerEdits = old.NormalizedHash == found?.NormalizedHash
            };
        }).ToArray();
        var fieldsBefore = Fields(baseline);
        var fieldsAfter = Fields(current);
        var fieldChanges = fieldsBefore.Keys.Union(fieldsAfter.Keys).Where(id =>
            fieldsBefore.GetValueOrDefault(id) != fieldsAfter.GetValueOrDefault(id)).Order(StringComparer.Ordinal).ToArray();
        string[] added = after.Keys.Except(comparisons.Select(item => item.AfterId)).Order(StringComparer.Ordinal).ToArray();
        var changed = comparisons.Where(item => !item.EqualModuloExplicitOwnerEdits).ToArray();
        string[] errors = baseline.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error)
            .Select(item => item.ToString()).ToArray();
        if (errors.Length != 0 || added.Length != 0 || fieldChanges.Length != 0 || changed.Length != 0
            || comparisons.Count(item => item.Moved) != 10)
        {
            throw new InvalidOperationException("Summary move verification failed: "
                + System.Text.Json.JsonSerializer.Serialize(new { errors, added, fieldChanges, changed }));
        }

        return new
        {
            Schema = "ExceptionFlow.SummaryClosure.Verification.v1",
            BaselineReference = reference,
            Scope = "All production source methods/constructors/accessors/local functions and fields; committed source rebound in memory, no checkout. Trivia excluded. Only the explicitly listed receiver and three accessibility changes are normalized.",
            AllowedEdits = new[]
            {
                "Ten existing methods change owner; no new method implementation.",
                "TryBuildTransitiveSummaryGraph: qualify unchanged root registration with the existing builder field.",
                "BuildPendingSummaryNodes: qualify unchanged declaration analysis with Analyzer.",
                "Session.Analyze: remove builder receiver from moved queue orchestration.",
                "GetSummaryInvocationSourceCoverage: change Builder body-query receiver to TargetRegistrar.",
                "Main factory/one-shot consumers: change Analyzer receiver to Session.",
                "AnalyzeSummarySymbolDeclarations and TryGetSummaryInvocationBody: private -> internal; BuildPendingSummaryNodes: internal -> private."
            },
            BeforeMethodCount = before.Count,
            AfterMethodCount = after.Count,
            MovedMethodCount = comparisons.Count(item => item.Moved),
            AddedMethods = added,
            UnexpectedMethodChanges = changed,
            FieldCount = fieldsBefore.Count,
            FieldChanges = fieldChanges,
            FieldsBefore = fieldsBefore,
            FieldsAfter = fieldsAfter,
            Comparisons = comparisons,
            BaselineCompilationErrors = errors
        };
    }

    private static Dictionary<string, Snapshot> Methods(Compilation compilation, bool before)
    {
        Dictionary<string, Snapshot> output = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in SourceTrees(compilation))
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes().Where(node =>
                node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax))
            {
                if (model.GetDeclaredSymbol(node) is not IMethodSymbol method)
                {
                    continue;
                }

                string owner = method.ContainingType.ToDisplayString();
                SyntaxNode? body = node switch
                {
                    BaseMethodDeclarationSyntax declaration => declaration.Body ?? (SyntaxNode?)declaration.ExpressionBody,
                    AccessorDeclarationSyntax accessor => accessor.Body ?? (SyntaxNode?)accessor.ExpressionBody,
                    LocalFunctionStatementSyntax local => local.Body ?? (SyntaxNode?)local.ExpressionBody,
                    _ => null
                };
                SyntaxNode normalized = new OwnerEditNormalizer(owner, method.Name, before).Visit(node)!;
                string id = method.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                output.Add(id, new Snapshot(id, owner, method.Name, tree.FilePath,
                    Hash(node), Hash(body), Hash(normalized)));
            }
        }

        return output;
    }

    private static Dictionary<string, string> Fields(Compilation compilation)
    {
        Dictionary<string, string> output = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in SourceTrees(compilation))
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (VariableDeclaratorSyntax node in tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is IFieldSymbol field)
                {
                    output.Add(field.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), Hash(node.Parent!.Parent));
                }
            }
        }

        return output;
    }

    private static IEnumerable<SyntaxTree> SourceTrees(Compilation compilation) => compilation.SyntaxTrees
        .Where(tree => !tree.FilePath.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal));

    private static string Hash(SyntaxNode? node) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        node == null ? "" : string.Join("|", node.DescendantTokens().Select(token => token.RawKind + ":" + token.Text)))));

    private sealed class OwnerEditNormalizer(string owner, string method, bool before) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            MethodDeclarationSyntax result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            if ((owner == Builder || owner == Analyzer || owner == Session || owner == Registrar)
                && method is "AnalyzeSummarySymbolDeclarations" or "TryGetSummaryInvocationBody" or "BuildPendingSummaryNodes")
            {
                result = result.WithModifiers(SyntaxFactory.TokenList(result.Modifiers.Where(token =>
                    !token.IsKind(SyntaxKind.PrivateKeyword) && !token.IsKind(SyntaxKind.InternalKeyword))));
            }

            return result;
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            string receiver = node.Expression.ToString();
            string name = node.Name.Identifier.ValueText;
            if (!before && owner == Session && method == "TryBuildTransitiveSummaryGraph"
                && receiver == "builder" && name == "TryRegisterSummaryGraphRoot"
                || !before && owner == Session && method == "BuildPendingSummaryNodes"
                && receiver == "ExceptionFlowAnalyzer" && name == "AnalyzeSummarySymbolDeclarations"
                || before && owner == Session && method == "Analyze"
                && receiver == "builder" && name == "BuildPendingSummaryNodes")
            {
                return node.Name;
            }

            if (before && owner == Analyzer && method == "GetSummaryInvocationSourceCoverage"
                && receiver == "ExceptionFlowSummaryGraphBuilder" && name == "HasAnalyzableSummaryInvocationBody")
            {
                return node.WithExpression(SyntaxFactory.IdentifierName("ExceptionFlowSummaryTargetRegistrar"));
            }

            if (before && receiver == "ExceptionFlowAnalyzer"
                && name is "CreateSummaryAnalysisSession" or "AnalyzeSolutionTransitivelyThrownExceptions")
            {
                return node.WithExpression(SyntaxFactory.IdentifierName("ExceptionFlowSummaryAnalysisSession"));
            }

            return base.VisitMemberAccessExpression(node);
        }
    }

    private sealed record Snapshot(string Id, string Owner, string Name, string File,
        string DeclarationHash, string BodyHash, string NormalizedHash);
}
