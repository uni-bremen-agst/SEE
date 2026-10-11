using System.Security.Cryptography;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Maps original P5C/P5E receipts into exact neutral images; no IO, metadata parser or engine policy.</summary>
    internal static class HistoricalWorkerReferenceProjection
    {
        internal static bool TryProject(ExternalMetadataReferenceSet set, ExternalCompilationProvenanceDescriptor provenance,
            ExternalAssemblyReferenceDescriptor target, out WorkerCompilationContext context)
        {
            context = null!;
            if (!ExternalMetadataReferenceSetFactory.TryGetValidatedMaterials(set, provenance, out var materials)
                || materials.Length > WorkerProtocol.MaximumReferenceCount
                || materials.Sum(material => (long)material.Image.Length) > WorkerProtocol.MaximumReferenceBytes)
            { return false; }
            var images = new List<WorkerReferenceImage>();
            foreach (var material in materials)
            {
                var candidate = material.Candidate;
                var expected = candidate.ExpectedReference;
                if (candidate.AssemblyIdentity == null || expected.Kind != Microsoft.CodeAnalysis.MetadataImageKind.Assembly
                    || expected.EmbedInteropTypes)
                { return false; }
                images.Add(new(expected.Name, material.Image.ToArray(), Convert.ToHexString(SHA256.HashData(material.Image.AsSpan())),
                    RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(candidate.AssemblyIdentity),
                    candidate.Module.Name, candidate.Module.ModuleVersionId, expected.Aliases.ToArray()));
            }
            context = new(ReferenceProfile: WorkerProtocol.ExactReferenceProfile, References: images.ToArray(),
                AssemblyName: target.AssemblyIdentity.Name,
                TargetIdentity: RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(target.AssemblyIdentity),
                TargetModuleName: target.Modules[0].Name);
            return WorkerInputValidation.HasSupportedReferences(context);
        }
    }
}
