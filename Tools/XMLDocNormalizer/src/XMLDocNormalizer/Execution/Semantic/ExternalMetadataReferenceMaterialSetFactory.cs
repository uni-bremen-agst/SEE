using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Acquires one explicit PE candidate file for every metadata-reference
    /// ordinal recorded in an external compilation provenance descriptor.
    /// </summary>
    internal static class ExternalMetadataReferenceMaterialSetFactory
    {
        /// <summary>
        /// Tries to materialize each explicitly aligned candidate path through
        /// P5C against the expected reference at the same ordinal.
        /// </summary>
        /// <param name="compilationProvenance">
        /// The compilation provenance containing the original ordered
        /// metadata-reference descriptors.
        /// </param>
        /// <param name="candidatePaths">
        /// The caller-selected candidate file paths already aligned to the
        /// expected reference ordinals.
        /// </param>
        /// <param name="materialSet">The complete validated material set.</param>
        /// <returns>
        /// <see langword="true"/> when metadata-reference provenance is
        /// present, the candidate count matches exactly, and P5C validates and
        /// materializes every ordinal; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Candidate paths are consumed exactly in caller-supplied order. This
        /// method performs no discovery, matching, reordering, normalization,
        /// or deduplication. A successful result only establishes that each
        /// explicitly supplied candidate was validated against the expected
        /// reference at the same ordinal.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilationProvenance"/> or
        /// <paramref name="candidatePaths"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalCompilationProvenanceDescriptor compilationProvenance,
            IReadOnlyList<string> candidatePaths,
            out ExternalMetadataReferenceMaterialSet materialSet)
        {
            ArgumentNullException.ThrowIfNull(compilationProvenance);
            ArgumentNullException.ThrowIfNull(candidatePaths);

            ExternalCompilationMetadataReferencesDescriptor? metadataReferences =
                compilationProvenance.MetadataReferences;

            if (metadataReferences == null
                || metadataReferences.References.Length != candidatePaths.Count)
            {
                materialSet = null!;
                return false;
            }

            ImmutableArray<ValidatedExternalMetadataReferenceMaterial>.Builder materials =
                ImmutableArray.CreateBuilder<ValidatedExternalMetadataReferenceMaterial>(
                    candidatePaths.Count);

            for (int index = 0; index < candidatePaths.Count; index++)
            {
                string? candidatePath = candidatePaths[index];

                if (candidatePath == null
                    || !ValidatedExternalMetadataReferenceMaterialFactory.TryCreateFromFile(
                        metadataReferences.References[index],
                        candidatePath,
                        out ValidatedExternalMetadataReferenceMaterial material))
                {
                    materialSet = null!;
                    return false;
                }

                materials.Add(material);
            }

            materialSet = new ExternalMetadataReferenceMaterialSet(
                materials.MoveToImmutable());
            return true;
        }
    }
}
