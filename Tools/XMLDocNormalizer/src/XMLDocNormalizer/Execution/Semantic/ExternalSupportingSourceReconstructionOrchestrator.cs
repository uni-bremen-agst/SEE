using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Composes the existing P4/P5 factories for one explicitly prepared,
    /// demand-driven external reconstruction.
    /// </summary>
    internal static class ExternalSupportingSourceReconstructionOrchestrator
    {
        /// <summary>
        /// Tries to reconstruct a P5K compilation and create its P6A handoff.
        /// </summary>
        /// <param name="plan">The exact immutable reconstruction plan.</param>
        /// <param name="candidateDiscovery">
        /// The context-local binary candidate discovery catalog.
        /// </param>
        /// <param name="remoteReferenceAcquisition">
        /// The context-local bounded remote reference acquisition catalog.
        /// </param>
        /// <param name="pdbAcquisition">
        /// The context-local exact Portable PDB acquisition catalog.
        /// </param>
        /// <param name="sourceAcquisition">
        /// The context-local controlled source acquisition catalog.
        /// </param>
        /// <param name="supportingSource">The P6A handoff when successful.</param>
        /// <returns>
        /// <see langword="true"/> only when every existing P4/P5 factory
        /// succeeds without approximation; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryReconstruct(
            ExternalSupportingSourceReconstructionPlan plan,
            ExternalBinaryCandidateDiscovery candidateDiscovery,
            ExternalRemoteReferenceAcquisition remoteReferenceAcquisition,
            ExternalPortablePdbAcquisition pdbAcquisition,
            ExternalSourceAcquisition sourceAcquisition,
            out ExternalSupportingSourceCompilation supportingSource)
        {
            if (plan == null
                || candidateDiscovery == null
                || remoteReferenceAcquisition == null
                || pdbAcquisition == null
                || sourceAcquisition == null)
            {
                supportingSource = null!;
                return false;
            }

            try
            {
                if (!ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                        plan.TargetAssembly,
                        plan.TargetPeCandidatePath,
                        out ExternalPeDebugDirectoryDescriptor debugDirectory)
                    || !TryCreateCompilationProvenance(
                        plan,
                        debugDirectory,
                        pdbAcquisition,
                        out ExternalCompilationProvenanceDescriptor provenance)
                    || !ExternalCSharpCompilationConfigurationFactory.TryCreate(
                        provenance,
                        out ExternalCSharpCompilationConfiguration configuration)
                    || !TryCreateSyntaxTrees(
                        plan,
                        provenance,
                        configuration,
                        sourceAcquisition,
                        out ExternalCSharpSyntaxTreeSet syntaxTreeSet)
                    || !TryCreateReferenceMaterials(
                        plan,
                        provenance,
                        candidateDiscovery,
                        remoteReferenceAcquisition,
                        out ExternalMetadataReferenceMaterialSet materialSet)
                    || !ExternalMetadataReferenceSetFactory.TryCreate(
                        provenance,
                        materialSet.Materials,
                        out ExternalMetadataReferenceSet referenceSet)
                    || !ExternalCSharpCompilationFactory.TryCreate(
                        plan.TargetAssembly,
                        provenance,
                        configuration,
                        syntaxTreeSet,
                        referenceSet,
                        out CSharpCompilation compilation))
                {
                    supportingSource = null!;
                    return false;
                }

                return ExternalSupportingSourceCompilation.TryCreate(
                    plan.TargetAssembly,
                    provenance,
                    configuration,
                    syntaxTreeSet,
                    referenceSet,
                    compilation,
                    out supportingSource);
            }
            catch (ArgumentNullException)
            {
                supportingSource = null!;
                return false;
            }
        }

        /// <summary>
        /// Preserves authoritative explicit PDB semantics or invokes explicitly
        /// enabled local acquisition when no explicit candidate exists.
        /// </summary>
        /// <param name="plan">The immutable reconstruction plan.</param>
        /// <param name="debugDirectory">The authoritative P4A provenance.</param>
        /// <param name="pdbAcquisition">The context-local acquisition catalog.</param>
        /// <param name="provenance">The P4B/P5A result when successful.</param>
        /// <returns><see langword="true"/> only for exact validated provenance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when an explicit candidate reaches P4B with
        /// malformed validated provenance.
        /// </exception>
        private static bool TryCreateCompilationProvenance(
            ExternalSupportingSourceReconstructionPlan plan,
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            ExternalPortablePdbAcquisition pdbAcquisition,
            out ExternalCompilationProvenanceDescriptor provenance)
        {
            if (plan.PortablePdbCandidatePath != null)
            {
                return ExternalCompilationProvenanceDescriptorFactory.TryCreateFromFile(
                    debugDirectory,
                    plan.PortablePdbCandidatePath,
                    out provenance);
            }

            if (!plan.DiscoverPortablePdbLocally)
            {
                provenance = null!;
                return false;
            }

            return pdbAcquisition.TryAcquire(
                    debugDirectory,
                    plan.TargetPeCandidatePath,
                    out provenance,
                    out _);
        }

        /// <summary>
        /// Uses authoritative explicit paths or discovers a complete ordinal
        /// path sequence before delegating materialization to P5F.
        /// </summary>
        /// <param name="plan">The immutable reconstruction plan.</param>
        /// <param name="provenance">The validated P5A provenance.</param>
        /// <param name="candidateDiscovery">The context-local discovery catalog.</param>
        /// <param name="remoteReferenceAcquisition">
        /// The context-local bounded remote acquisition catalog.
        /// </param>
        /// <param name="materialSet">The complete P5F result when successful.</param>
        /// <returns>
        /// <see langword="true"/> only when every required reference ordinal
        /// has an exact candidate and P5F validates the complete sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown transitively when invalid provenance reaches the existing
        /// P5 validation boundary.
        /// </exception>
        private static bool TryCreateReferenceMaterials(
            ExternalSupportingSourceReconstructionPlan plan,
            ExternalCompilationProvenanceDescriptor provenance,
            ExternalBinaryCandidateDiscovery candidateDiscovery,
            ExternalRemoteReferenceAcquisition remoteReferenceAcquisition,
            out ExternalMetadataReferenceMaterialSet materialSet)
        {
            if (!plan.DiscoverReferenceCandidatesLocally)
            {
                return ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                    provenance,
                    plan.ReferenceCandidatePaths,
                    out materialSet);
            }

            ExternalCompilationMetadataReferencesDescriptor? metadataReferences =
                provenance.MetadataReferences;

            if (metadataReferences == null)
            {
                materialSet = null!;
                return false;
            }

            ImmutableArray<ValidatedExternalMetadataReferenceMaterial>.Builder materials =
                ImmutableArray.CreateBuilder<ValidatedExternalMetadataReferenceMaterial>(
                    metadataReferences.References.Length);

            for (int expectedReferenceOrdinal = 0;
                 expectedReferenceOrdinal < metadataReferences.References.Length;
                 expectedReferenceOrdinal++)
            {
                ExternalCompilationMetadataReferenceDescriptor expectedReference =
                    metadataReferences.References[expectedReferenceOrdinal];
                if (!candidateDiscovery.TryAcquireReferenceMaterial(
                        expectedReference,
                        expectedReferenceOrdinal,
                        remoteReferenceAcquisition,
                        out ValidatedExternalMetadataReferenceMaterial acquiredMaterial,
                        out _,
                        out _))
                {
                    materialSet = null!;
                    return false;
                }

                materials.Add(acquiredMaterial);
            }

            materialSet = new ExternalMetadataReferenceMaterialSet(materials.MoveToImmutable());
            return true;
        }

        /// <summary>
        /// Materializes the explicit source ordinal sequence through P5H-P5J.
        /// </summary>
        /// <param name="plan">The immutable explicit candidate plan.</param>
        /// <param name="provenance">The validated P5A provenance.</param>
        /// <param name="configuration">The reconstructed P5G configuration.</param>
        /// <param name="sourceAcquisition">The context-local P7B acquisition.</param>
        /// <param name="syntaxTreeSet">The complete P5J result when successful.</param>
        /// <returns>
        /// <see langword="true"/> when every explicit source ordinal succeeds
        /// through P5H, P5I, and P5J; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryCreateSyntaxTrees(
            ExternalSupportingSourceReconstructionPlan plan,
            ExternalCompilationProvenanceDescriptor provenance,
            ExternalCSharpCompilationConfiguration configuration,
            ExternalSourceAcquisition sourceAcquisition,
            out ExternalCSharpSyntaxTreeSet syntaxTreeSet)
        {
            try
            {
                List<ExternalSourceDocumentDescriptor> documents =
                    new(plan.SourceInputs.Length);
                List<ExternalCSharpSyntaxTree> trees = new(plan.SourceInputs.Length);

                foreach (ExternalSourceReconstructionInput input in plan.SourceInputs)
                {
                    if (input.DocumentOrdinal >= provenance.PortablePdb.Documents.Length)
                    {
                        syntaxTreeSet = null!;
                        return false;
                    }

                    ExternalSourceDocumentDescriptor document =
                        provenance.PortablePdb.Documents[input.DocumentOrdinal];
                    bool materialCreated;
                    ValidatedExternalSourceMaterial material;

                    if (input.CandidatePath != null)
                    {
                        materialCreated = ValidatedExternalSourceMaterialFactory.TryCreateFromFile(
                            document,
                            input.CandidatePath,
                            out material);
                    }
                    else
                    {
                        materialCreated =
                            ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(
                                document,
                                out material)
                            || (input.AllowAcquisition
                                && sourceAcquisition.TryAcquire(
                                    document,
                                    provenance.PortablePdb.SourceLink,
                                    configuration,
                                    out material));
                    }

                    if (!materialCreated
                        || !ExternalCSharpSyntaxTreeFactory.TryCreate(
                            material,
                            configuration,
                            out ExternalCSharpSyntaxTree tree))
                    {
                        syntaxTreeSet = null!;
                        return false;
                    }

                    documents.Add(document);
                    trees.Add(tree);
                }

                return ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                    configuration,
                    documents,
                    trees,
                    out syntaxTreeSet);
            }
            catch (ArgumentNullException)
            {
                syntaxTreeSet = null!;
                return false;
            }
        }
    }
}
