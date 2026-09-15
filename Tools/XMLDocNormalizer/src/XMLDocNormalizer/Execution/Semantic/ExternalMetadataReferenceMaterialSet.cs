using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds the ordered validated PE materials acquired for every metadata
    /// reference recorded in one compilation provenance descriptor.
    /// </summary>
    /// <remarks>
    /// The sequence represents explicit ordinal acquisition only. It does not
    /// imply that the best candidate was found or that other candidates were
    /// searched.
    /// </remarks>
    internal sealed class ExternalMetadataReferenceMaterialSet
    {
        /// <summary>
        /// Initializes an ordered validated metadata-reference material set.
        /// </summary>
        /// <param name="materials">
        /// The materials in original metadata-reference provenance order.
        /// </param>
        internal ExternalMetadataReferenceMaterialSet(
            ImmutableArray<ValidatedExternalMetadataReferenceMaterial> materials)
        {
            Materials = materials.IsDefault
                ? ImmutableArray<ValidatedExternalMetadataReferenceMaterial>.Empty
                : materials;
        }

        /// <summary>
        /// Gets the validated materials in their original recorded order.
        /// </summary>
        /// <value>The immutable ordered material sequence.</value>
        public ImmutableArray<ValidatedExternalMetadataReferenceMaterial> Materials { get; }
    }
}
