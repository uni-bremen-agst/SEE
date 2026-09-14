using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one metadata reference serialized in a Portable PDB.
    /// </summary>
    internal sealed class ExternalCompilationMetadataReferenceDescriptor
    {
        /// <summary>
        /// Initializes validated metadata-reference provenance.
        /// </summary>
        /// <param name="name">The serialized lookup name.</param>
        /// <param name="aliases">The aliases in serialized order.</param>
        /// <param name="kind">Whether the referenced image is an assembly or module.</param>
        /// <param name="embedInteropTypes">Whether interop types were embedded.</param>
        /// <param name="timestamp">The serialized PE COFF timestamp.</param>
        /// <param name="imageSize">The serialized PE image size.</param>
        /// <param name="moduleVersionId">The serialized module version identifier.</param>
        internal ExternalCompilationMetadataReferenceDescriptor(
            string name,
            ImmutableArray<string> aliases,
            MetadataImageKind kind,
            bool embedInteropTypes,
            int timestamp,
            int imageSize,
            Guid moduleVersionId)
        {
            Name = name;
            Aliases = aliases.IsDefault ? ImmutableArray<string>.Empty : aliases;
            Kind = kind;
            EmbedInteropTypes = embedInteropTypes;
            Timestamp = timestamp;
            ImageSize = imageSize;
            ModuleVersionId = moduleVersionId;
        }

        /// <summary>
        /// Gets the serialized reference lookup name.
        /// </summary>
        /// <value>A provenance hint that is not binary identity.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the aliases in their original serialized order.
        /// </summary>
        /// <value>The untrimmed, unsorted aliases.</value>
        public ImmutableArray<string> Aliases { get; }

        /// <summary>
        /// Gets whether the referenced image is an assembly or module.
        /// </summary>
        /// <value>The decoded metadata image kind.</value>
        public MetadataImageKind Kind { get; }

        /// <summary>
        /// Gets whether the original reference embedded interop types.
        /// </summary>
        /// <value>The exact serialized flag value.</value>
        public bool EmbedInteropTypes { get; }

        /// <summary>
        /// Gets the serialized PE COFF timestamp.
        /// </summary>
        /// <value>The uninterpreted signed 32-bit value.</value>
        public int Timestamp { get; }

        /// <summary>
        /// Gets the serialized PE image size.
        /// </summary>
        /// <value>The uninterpreted signed 32-bit value.</value>
        public int ImageSize { get; }

        /// <summary>
        /// Gets the serialized module version identifier.
        /// </summary>
        /// <value>The exact MVID recorded by the compiler.</value>
        public Guid ModuleVersionId { get; }
    }
}
