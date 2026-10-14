using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Checks.Infrastructure.Tags;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;
using XMLDocNormalizer.Utils.Extensions;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Local XML syntax and compiler binding facts only; no coverage, relevance, guard or Finding decision.</summary>
    internal static class HistoricalDeclaredDocumentationFacts
    {
        internal static List<ExtractedXmlDocTag> Tags(MethodDeclarationSyntax member)
        {
            var doc = XmlDocUtils.TryGetDocComment(member);
            return doc == null ? [] : XmlDocTagExtraction.ExtractTags(doc, "exception", element =>
            { XmlDocTagExtraction.TryGetCrefAttributeValue(element, out var cref); return cref; });
        }
        internal static WorkerExceptionTagSyntax Syntax(ExtractedXmlDocTag tag)
        {
            var cref = SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(tag.Element, "cref");
            return new(tag.Element.SpanStart, tag.Element.Span.Length, cref?.SpanStart, cref?.Span.Length, tag.RawAttributeValue);
        }
        internal static WorkerDeclaredDocumentationSyntax Syntax(MethodDeclarationSyntax member)
        {
            var doc = XmlDocUtils.TryGetDocComment(member);
            return new(member.SyntaxTree.FilePath, member.SpanStart, member.Span.Length, MemberAnchorResolver.GetAnchorPosition(member),
                doc != null, doc?.HasInheritdoc() ?? false, Tags(member).Select(Syntax).ToArray());
        }
        internal static WorkerDeclaredDocumentationEvidence Create(SemanticModel model, MethodDeclarationSyntax member,
            CanonicalExceptionFlowAnalysisResult direct, CanonicalExceptionFlowAnalysisResult summary)
        {
            var exceptionBase = model.Compilation.GetTypeByMetadataName("System.Exception")!;
            var names = new Dictionary<CanonicalTypeIdentity, string>();
            var bindings = new List<WorkerExceptionTagBinding>();
            foreach (var tag in Tags(member))
            {
                var cref = SyntaxUtils.GetAttribute<XmlCrefAttributeSyntax>(tag.Element, "cref");
                var symbol = cref?.Cref == null ? null : model.GetSymbolInfo(cref.Cref).Symbol;
                var type = symbol as INamedTypeSymbol;
                var identity = type == null ? null : RoslynCanonicalIdentityFactory.CreateTypeIdentity(type);
                if (identity != null) { names[identity] = type!.ToDisplayString(); }
                bindings.Add(new(Syntax(tag), symbol == null ? WorkerCrefBindingKind.Unresolved : type == null
                    ? WorkerCrefBindingKind.OtherSymbol : WorkerCrefBindingKind.NamedType, identity,
                    type == null ? null : type.InheritsFromOrEquals(exceptionBase)));
            }
            foreach (var entry in direct.Entries.Concat(summary.Entries))
            {
                // Resolve the already bound compiler type by exact assembly and metadata definition, not display text.
                var identity = entry.ExceptionType;
                var assemblies = model.Compilation.SourceModule.ReferencedAssemblySymbols.Append(model.Compilation.Assembly);
                var type = assemblies.Where(assembly => RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly).Equals(identity.Assembly))
                    .Select(assembly => assembly.GetTypeByMetadataName(MetadataName(identity))).Single(type => type != null)!;
                if (!RoslynCanonicalIdentityFactory.CreateTypeIdentity(type).Equals(identity)) { throw new InvalidOperationException("Flow type definition differs."); }
                names[identity] = type.ToDisplayString();
            }
            return new(WorkerDeclaredDocumentationContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(model.GetDeclaredSymbol(member)!), Syntax(member), bindings.ToArray(),
                names.OrderBy(pair => JsonSerializer.Serialize(pair.Key, WorkerProtocol.JsonOptions), StringComparer.Ordinal)
                    .Select(pair => new WorkerTypeDisplayFact(pair.Key, pair.Value)).ToArray(),
                WorkerProtocol.SourceHash(JsonSerializer.Serialize(direct, WorkerProtocol.JsonOptions)),
                WorkerProtocol.SourceHash(JsonSerializer.Serialize(summary, WorkerProtocol.JsonOptions)));
        }
        internal static string MetadataName(CanonicalTypeIdentity type) => type.ContainingType != null
            ? MetadataName(type.ContainingType) + "+" + type.MetadataName : (string.IsNullOrEmpty(type.NamespaceName) ? "" : type.NamespaceName + ".") + type.MetadataName;
    }
}
