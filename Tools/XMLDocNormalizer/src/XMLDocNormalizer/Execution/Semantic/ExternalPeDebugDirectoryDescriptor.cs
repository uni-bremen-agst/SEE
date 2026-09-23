using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes validated debug provenance from an external assembly's
    /// manifest PE image.
    /// </summary>
    internal sealed class ExternalPeDebugDirectoryDescriptor
    {
        /// <summary>
        /// Initializes a PE debug-directory descriptor.
        /// </summary>
        /// <param name="manifestModule">
        /// The validated manifest-module identity.
        /// </param>
        /// <param name="isDeterministic">
        /// Whether the PE contains a reproducible debug-directory entry.
        /// </param>
        /// <param name="codeViewPdbReferences">
        /// The CodeView entries in PE debug-directory order.
        /// </param>
        /// <param name="embeddedPortablePdbIds">
        /// The embedded Portable PDB identifiers in PE debug-directory order.
        /// </param>
        /// <param name="pdbChecksums">
        /// The PDB checksum entries in PE debug-directory order.
        /// </param>
        public ExternalPeDebugDirectoryDescriptor(
            ExternalModuleIdentity manifestModule,
            bool isDeterministic,
            ImmutableArray<ExternalCodeViewPdbReference> codeViewPdbReferences,
            ImmutableArray<BlobContentId> embeddedPortablePdbIds,
            ImmutableArray<ExternalPdbChecksum> pdbChecksums)
        {
            ManifestModule = manifestModule;
            SigningProvenance = ExternalAssemblySigningProvenance.Unsigned;
            IsDeterministic = isDeterministic;
            CodeViewPdbReferences = codeViewPdbReferences.IsDefault
                ? ImmutableArray<ExternalCodeViewPdbReference>.Empty
                : codeViewPdbReferences;
            EmbeddedPortablePdbIds = embeddedPortablePdbIds.IsDefault
                ? ImmutableArray<BlobContentId>.Empty
                : embeddedPortablePdbIds;
            PdbChecksums = pdbChecksums.IsDefault
                ? ImmutableArray<ExternalPdbChecksum>.Empty
                : pdbChecksums;
        }

        /// <summary>Initializes PE debug and signing provenance.</summary>
        /// <param name="manifestModule">The validated manifest-module identity.</param>
        /// <param name="signingProvenance">The exact target PE signing provenance.</param>
        /// <param name="isDeterministic">Whether the PE declares reproducibility.</param>
        /// <param name="codeViewPdbReferences">The ordered CodeView entries.</param>
        /// <param name="embeddedPortablePdbIds">The ordered embedded PDB identifiers.</param>
        /// <param name="pdbChecksums">The ordered PDB checksum declarations.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="signingProvenance"/> is <see langword="null"/>.
        /// </exception>
        internal ExternalPeDebugDirectoryDescriptor(
            ExternalModuleIdentity manifestModule,
            ExternalAssemblySigningProvenance signingProvenance,
            bool isDeterministic,
            ImmutableArray<ExternalCodeViewPdbReference> codeViewPdbReferences,
            ImmutableArray<BlobContentId> embeddedPortablePdbIds,
            ImmutableArray<ExternalPdbChecksum> pdbChecksums)
        {
            ArgumentNullException.ThrowIfNull(signingProvenance);
            ManifestModule = manifestModule;
            SigningProvenance = signingProvenance;
            IsDeterministic = isDeterministic;
            CodeViewPdbReferences = codeViewPdbReferences.IsDefault
                ? ImmutableArray<ExternalCodeViewPdbReference>.Empty
                : codeViewPdbReferences;
            EmbeddedPortablePdbIds = embeddedPortablePdbIds.IsDefault
                ? ImmutableArray<BlobContentId>.Empty
                : embeddedPortablePdbIds;
            PdbChecksums = pdbChecksums.IsDefault
                ? ImmutableArray<ExternalPdbChecksum>.Empty
                : pdbChecksums;
        }

        /// <summary>
        /// Gets the validated manifest-module identity.
        /// </summary>
        /// <value>The manifest-module name and MVID.</value>
        public ExternalModuleIdentity ManifestModule { get; }

        /// <summary>Gets exact signing metadata from the validated target PE.</summary>
        /// <value>The immutable signing provenance.</value>
        public ExternalAssemblySigningProvenance SigningProvenance { get; }

        /// <summary>
        /// Gets whether the PE declares itself reproducible.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when at least one reproducible
        /// debug-directory entry exists; otherwise <see langword="false"/>.
        /// </value>
        public bool IsDeterministic { get; }

        /// <summary>
        /// Gets the ordered CodeView PDB references.
        /// </summary>
        /// <value>The CodeView entries in PE debug-directory order.</value>
        public ImmutableArray<ExternalCodeViewPdbReference> CodeViewPdbReferences { get; }

        /// <summary>
        /// Gets the ordered embedded Portable PDB identifiers.
        /// </summary>
        /// <value>
        /// The embedded identifiers in PE debug-directory order.
        /// </value>
        public ImmutableArray<BlobContentId> EmbeddedPortablePdbIds { get; }

        /// <summary>
        /// Gets the ordered PDB checksum declarations.
        /// </summary>
        /// <value>The checksum entries in PE debug-directory order.</value>
        public ImmutableArray<ExternalPdbChecksum> PdbChecksums { get; }
    }
}
