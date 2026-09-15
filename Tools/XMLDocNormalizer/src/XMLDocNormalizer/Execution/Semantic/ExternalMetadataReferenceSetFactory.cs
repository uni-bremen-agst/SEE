using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Reconstructs the complete ordered metadata-reference sequence recorded
    /// in one external compilation provenance descriptor.
    /// </summary>
    internal static class ExternalMetadataReferenceSetFactory
    {
        /// <summary>
        /// Tries to reconstruct one Roslyn reference for every recorded
        /// metadata-reference ordinal from the corresponding validated
        /// material ordinal.
        /// </summary>
        /// <param name="compilationProvenance">
        /// The compilation provenance containing the original ordered
        /// metadata-reference descriptors.
        /// </param>
        /// <param name="materials">
        /// The validated materials already aligned to the expected descriptor
        /// ordinals.
        /// </param>
        /// <param name="referenceSet">The complete reconstructed reference set.</param>
        /// <returns>
        /// <see langword="true"/> when metadata-reference provenance is
        /// present, the material count and every ordinal match exactly, and
        /// P5D reconstructs every reference; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Success means complete with respect to the metadata-reference
        /// provenance recorded in <paramref name="compilationProvenance"/>.
        /// Materials are never searched, reordered, or deduplicated, and this
        /// method does not establish runtime dependency completeness.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="compilationProvenance"/> or
        /// <paramref name="materials"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalCompilationProvenanceDescriptor compilationProvenance,
            IReadOnlyList<ValidatedExternalMetadataReferenceMaterial> materials,
            out ExternalMetadataReferenceSet referenceSet)
        {
            ArgumentNullException.ThrowIfNull(compilationProvenance);
            ArgumentNullException.ThrowIfNull(materials);

            ExternalCompilationMetadataReferencesDescriptor? metadataReferences =
                compilationProvenance.MetadataReferences;

            if (metadataReferences == null
                || metadataReferences.References.Length != materials.Count)
            {
                referenceSet = null!;
                return false;
            }

            ImmutableArray<PortableExecutableReference>.Builder references =
                ImmutableArray.CreateBuilder<PortableExecutableReference>(materials.Count);

            for (int index = 0; index < materials.Count; index++)
            {
                ValidatedExternalMetadataReferenceMaterial? material = materials[index];
                ExternalCompilationMetadataReferenceDescriptor expected =
                    metadataReferences.References[index];

                if (material == null
                    || !HasSameExpectedProvenance(
                        expected,
                        material.Candidate.ExpectedReference)
                    || !ExternalMetadataReferenceFactory.TryCreate(
                        material,
                        out PortableExecutableReference reference)
                    || !HasExpectedProperties(reference, expected))
                {
                    referenceSet = null!;
                    return false;
                }

                references.Add(reference);
            }

            referenceSet = new ExternalMetadataReferenceSet(references.MoveToImmutable());
            return true;
        }

        /// <summary>
        /// Compares every field of two original metadata-reference provenance
        /// descriptors with ordinal string and alias-order semantics.
        /// </summary>
        /// <param name="expected">The descriptor recorded for this ordinal.</param>
        /// <param name="actual">The descriptor used to validate the material.</param>
        /// <returns>
        /// <see langword="true"/> when every provenance and property field is
        /// semantically identical; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasSameExpectedProvenance(
            ExternalCompilationMetadataReferenceDescriptor expected,
            ExternalCompilationMetadataReferenceDescriptor actual)
        {
            return string.Equals(expected.Name, actual.Name, StringComparison.Ordinal)
                && expected.Kind == actual.Kind
                && expected.EmbedInteropTypes == actual.EmbedInteropTypes
                && expected.Timestamp == actual.Timestamp
                && expected.ImageSize == actual.ImageSize
                && expected.ModuleVersionId == actual.ModuleVersionId
                && AliasesEqual(expected.Aliases, actual.Aliases);
        }

        /// <summary>
        /// Checks the inexpensive P5D reference-property postcondition.
        /// </summary>
        /// <param name="reference">The reconstructed Roslyn reference.</param>
        /// <param name="expected">The original reference properties.</param>
        /// <returns>
        /// <see langword="true"/> when all reconstructed properties match;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasExpectedProperties(
            PortableExecutableReference reference,
            ExternalCompilationMetadataReferenceDescriptor expected)
        {
            return reference.Properties.Kind == expected.Kind
                && reference.Properties.EmbedInteropTypes == expected.EmbedInteropTypes
                && AliasesEqual(reference.Properties.Aliases, expected.Aliases);
        }

        /// <summary>
        /// Compares two alias sequences without normalization or reordering.
        /// </summary>
        /// <param name="left">The first ordered alias sequence.</param>
        /// <param name="right">The second ordered alias sequence.</param>
        /// <returns>
        /// <see langword="true"/> when both sequences contain ordinally equal
        /// aliases at every position; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AliasesEqual(
            ImmutableArray<string> left,
            ImmutableArray<string> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
