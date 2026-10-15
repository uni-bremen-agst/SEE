using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Retains existing Main source/context and original bound symbol world; no new acquisition, binding or analyzer.</summary>
    internal static class HistoricalWorkerDeclaredDocumentationProjection
    {
        private static readonly ConditionalWeakTable<WorkerAnalysisInput, Origin> origins = new();
        internal sealed class Origin(IMethodSymbol root, MethodDeclarationSyntax member, WorkerDeclaredDocumentationRequest request)
        {
            internal IMethodSymbol Root { get; } = root;
            internal MethodDeclarationSyntax Member { get; } = member;
            internal WorkerDeclaredDocumentationRequest Request { get; } = request;
            internal string SyntaxJson { get; } = Json(request.Syntax);
            internal WorkerAnalysisProvenance? Provenance { get; set; }
        }
        private static string Json(object? value) => JsonSerializer.Serialize(value, WorkerProtocol.JsonOptions);
        internal static bool TryProject(HistoricalWorkerPayloadProjectionInput input, WorkerCompilationContext context,
            out WorkerCompilationContext projected, out Origin? origin)
        {
            projected = context; origin = null;
            var request = context.Finding?.Documentation ?? context.DeclaredPolicy?.Documentation;
            if (request == null) { return true; }
            bool finding = context.Finding != null;
            var expectedMode = input.AnalysisMode == Models.ExceptionAnalysisMode.Direct ? WorkerDeclaredPolicyMode.Direct
                : input.AnalysisMode == Models.ExceptionAnalysisMode.ProjectTransitive ? WorkerDeclaredPolicyMode.ProjectTransitive
                : WorkerDeclaredPolicyMode.Unspecified;
            if ((finding ? request.Profile != WorkerFindingContract.Profile || request.Mode != expectedMode || expectedMode == WorkerDeclaredPolicyMode.Unspecified
                    : request.Profile != WorkerDeclaredDocumentationContract.Profile || request.Mode != WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions)
                || request.Method != null || request.Syntax != null) { return false; }
            var members = input.Source.Tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(member => member.Identifier.ValueText == input.Root.MetadataName && member.ParameterList.Parameters.Count == 0
                    && member.TypeParameterList == null && member.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && TypeMetadataName(member) == HistoricalDeclaredDocumentationFacts.MetadataName(RoslynCanonicalIdentityFactory.CreateTypeIdentity(input.Root.ContainingType))).ToArray();
            if (members.Length != 1) { return false; }
            var syntax = HistoricalDeclaredDocumentationFacts.Syntax(members[0]);
            if (syntax.Tags!.Length > WorkerDeclaredDocumentationContract.MaximumTags) { return false; }
            request = request with { Method = RoslynCanonicalIdentityFactory.CreateCallableIdentity(input.Root), Syntax = syntax };
            projected = finding ? context with { Finding = context.Finding! with { Documentation = request } }
                : context with { DeclaredPolicy = context.DeclaredPolicy! with { Documentation = request } };
            origin = new(input.Root, members[0], request);
            return true;
        }
        private static string TypeMetadataName(MethodDeclarationSyntax member)
        {
            string space = string.Join(".", member.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse()
                .SelectMany(declaration => declaration.Name.DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText)));
            string type = string.Join("+", member.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(declaration => declaration.Identifier.ValueText));
            return (space.Length == 0 ? "" : space + ".") + type;
        }
        internal static void Register(WorkerAnalysisInput payload, Origin? origin)
        {
            if (origin == null) { return; }
            origin.Provenance = WorkerProtocol.Provenance(payload); origins.Add(payload, origin);
        }
        internal static bool TryValidate(WorkerAnalysisInput input, WorkerDeclaredDocumentationEvidence? evidence,
            WorkerDeclaredPolicyEvidence policy, CanonicalExceptionFlowAnalysisResult summary, out DeclaredFindingDocumentationFacts? facts)
        {
            facts = null;
            if (input.Context!.DeclaredPolicy!.Documentation == null) { return evidence == null; }
            if (evidence == null || !origins.TryGetValue(input, out var origin) || origin.Provenance != WorkerProtocol.Provenance(input)
                || evidence.Profile != WorkerDeclaredDocumentationContract.Profile || evidence.Mode != WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions
                || !Equals(evidence.Method, origin.Request.Method) || Json(evidence.Syntax) != origin.SyntaxJson
                || evidence.Bindings == null || evidence.Bindings.Length != origin.Request.Syntax!.Tags!.Length || evidence.TypeDisplays == null
                || evidence.DirectSha256 != WorkerProtocol.SourceHash(Json(policy.DirectResult)) || evidence.SummarySha256 != WorkerProtocol.SourceHash(Json(summary))
                || policy.DirectResult!.Entries.Any(direct => !summary.Entries.Any(entry => entry.ExceptionType.Equals(direct.ExceptionType))))
            { return false; }
            return TryValidateBindings(input, origin, evidence, policy.DirectResult!, summary, policy.Declarations, out facts);
        }
        internal static bool TryValidateFinding(WorkerAnalysisInput input, WorkerDeclaredDocumentationEvidence? evidence,
            CanonicalExceptionFlowAnalysisResult direct, CanonicalExceptionFlowAnalysisResult flow, out DeclaredFindingDocumentationFacts? facts)
        {
            facts = null;
            var request = input.Context!.Finding!;
            if (evidence == null || !origins.TryGetValue(input, out var origin) || origin.Provenance != WorkerProtocol.Provenance(input)
                || evidence.Profile != WorkerFindingContract.Profile || evidence.Mode != request.Documentation!.Mode
                || !Equals(evidence.Method, origin.Request.Method) || Json(evidence.Syntax) != origin.SyntaxJson
                || evidence.Bindings == null || evidence.Bindings.Length != origin.Request.Syntax!.Tags!.Length || evidence.TypeDisplays == null
                || evidence.DirectSha256 != WorkerProtocol.SourceHash(Json(direct)) || evidence.SummarySha256 != WorkerProtocol.SourceHash(Json(flow)))
            { return false; }
            // No Declared inventory/guard; independently validate every actual flow exception in the original PE symbol world.
            foreach (var entry in direct.Entries.Concat(flow.Entries))
            {
                if (!TryType(origin, entry.ExceptionType, out var type, out var exceptionBase) || !type!.InheritsFromOrEquals(exceptionBase!)) { return false; }
            }
            return TryValidateBindings(input, origin, evidence, direct, flow, null, out facts);
        }
        private static bool TryValidateBindings(WorkerAnalysisInput input, Origin origin, WorkerDeclaredDocumentationEvidence evidence,
            CanonicalExceptionFlowAnalysisResult direct, CanonicalExceptionFlowAnalysisResult summary,
            WorkerExceptionDeclaration[]? declarations, out DeclaredFindingDocumentationFacts? facts)
        {
            facts = null;
            if (evidence.Bindings == null || evidence.TypeDisplays == null || origin.Request.Syntax?.Tags == null) { return false; }
            var allTypes = summary.Entries.Concat(direct.Entries).Select(entry => entry.ExceptionType).ToHashSet();
            var bindings = new List<DeclaredFindingTag>();
            var originalTags = HistoricalDeclaredDocumentationFacts.Tags(origin.Member);
            for (int index = 0; index < evidence.Bindings.Length; index++)
            {
                var binding = evidence.Bindings[index]; var tag = originalTags[index];
                if (binding == null || binding.Syntax != origin.Request.Syntax.Tags[index]
                    || binding.Kind is not (WorkerCrefBindingKind.Unresolved or WorkerCrefBindingKind.NamedType or WorkerCrefBindingKind.OtherSymbol)) { return false; }
                if (binding.Kind == WorkerCrefBindingKind.NamedType)
                {
                    if (binding.Syntax!.CrefStart == null || string.IsNullOrWhiteSpace(binding.Syntax.RawCref)
                        || binding.Type == null || binding.IsException == null || !TryType(origin, binding.Type, out var type, out var exceptionBase)
                        || binding.IsException != type!.InheritsFromOrEquals(exceptionBase!)) { return false; }
                    bool defined = declarations?.Any(declaration => declaration.Type!.Equals(binding.Type)) ?? true;
                    if (binding.Type.Assembly!.Equals(input.Context!.TargetIdentity) && binding.IsException == true && !defined) { return false; }
                    if (allTypes.Contains(binding.Type) && binding.IsException != true) { return false; }
                    allTypes.Add(binding.Type);
                }
                else if (binding.Type != null || binding.IsException != null
                    || binding.Kind == WorkerCrefBindingKind.OtherSymbol && binding.Syntax!.RawCref == null) { return false; }
                var anchor = Anchor(origin.Member, binding.Syntax!.CrefStart ?? binding.Syntax.Start!.Value, "ExceptionTag",
                    string.IsNullOrWhiteSpace(binding.Syntax.RawCref) ? null : "cref:" + binding.Syntax.RawCref);
                bindings.Add(new(binding.Syntax, binding.Kind.Value, binding.Type, binding.IsException, anchor, SyntaxUtils.GetSnippet(tag.Element)));
            }
            var displays = new Dictionary<CanonicalTypeIdentity, string>(); var keys = new List<string>();
            foreach (var item in evidence.TypeDisplays)
            {
                if (item?.Type == null || !allTypes.Contains(item.Type) || !TryType(origin, item.Type, out var type, out _)
                    || item.DisplayName != type!.ToDisplayString() || !displays.TryAdd(item.Type, item.DisplayName)) { return false; }
                keys.Add(Json(item.Type));
            }
            if (!allTypes.SetEquals(displays.Keys) || !keys.SequenceEqual(keys.OrderBy(key => key, StringComparer.Ordinal), StringComparer.Ordinal)) { return false; }
            facts = new(evidence.Method!, evidence.Syntax!.HasDocumentation!.Value, evidence.Syntax.HasInheritdoc!.Value,
                Anchor(origin.Member, evidence.Syntax.Anchor!.Value, "ExceptionFlow", null), bindings.ToImmutableArray(),
                evidence.TypeDisplays.ToImmutableArray());
            return true;
        }
        private static bool TryType(Origin origin, CanonicalTypeIdentity identity, out INamedTypeSymbol? type, out INamedTypeSymbol? exceptionBase)
        {
            type = null; exceptionBase = null;
            if (identity.Kind != CanonicalTypeIdentityKind.Named || string.IsNullOrWhiteSpace(identity.MetadataName) || identity.TypeArguments.Length != 0 || identity.IsUnboundGenericType
                || identity.MetadataName.Contains('`')) { return false; }
            var assemblies = origin.Root.ContainingModule.ReferencedAssemblySymbols.Append(origin.Root.ContainingAssembly).ToArray();
            var matching = assemblies.Where(assembly => RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly).Equals(identity.Assembly)).ToArray();
            if (matching.Length != 1) { return false; }
            type = matching[0].GetTypeByMetadataName(HistoricalDeclaredDocumentationFacts.MetadataName(identity));
            exceptionBase = assemblies.Select(assembly => assembly.GetTypeByMetadataName("System.Exception")).Where(symbol => symbol != null)
                .Distinct<INamedTypeSymbol?>(SymbolEqualityComparer.Default).SingleOrDefault();
            return type != null && exceptionBase != null && RoslynCanonicalIdentityFactory.CreateTypeIdentity(type).Equals(identity);
        }
        private static DeclaredFindingAnchor Anchor(MethodDeclarationSyntax member, int position, string subject, string? target)
        {
            var line = member.SyntaxTree.GetLineSpan(new TextSpan(position, 1)).StartLinePosition;
            return new(member.SyntaxTree.FilePath, position, line.Line + 1, line.Character + 1,
                FindingContextBuilder.ForDeclaration(member, subject, targetName: target, filePath: member.SyntaxTree.FilePath));
        }
    }
}
