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
            ExternalSourceAcquisition sourceAcquisition,
            out ExternalSupportingSourceCompilation supportingSource)
        {
            if (plan == null || candidateDiscovery == null || sourceAcquisition == null)
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
                    || !ExternalCompilationProvenanceDescriptorFactory.TryCreateFromFile(
                        debugDirectory,
                        plan.PortablePdbCandidatePath,
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
        /// Uses authoritative explicit paths or discovers a complete ordinal
        /// path sequence before delegating materialization to P5F.
        /// </summary>
        /// <param name="plan">The immutable reconstruction plan.</param>
        /// <param name="provenance">The validated P5A provenance.</param>
        /// <param name="candidateDiscovery">The context-local discovery catalog.</param>
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

            List<string> candidatePaths = new(metadataReferences.References.Length);

            foreach (ExternalCompilationMetadataReferenceDescriptor expectedReference in
                     metadataReferences.References)
            {
                if (!candidateDiscovery.TryFindReferenceCandidate(
                        expectedReference,
                        out string candidatePath))
                {
                    materialSet = null!;
                    return false;
                }

                candidatePaths.Add(candidatePath);
            }

            return ExternalMetadataReferenceMaterialSetFactory.TryCreate(
                provenance,
                candidatePaths,
                out materialSet);
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
