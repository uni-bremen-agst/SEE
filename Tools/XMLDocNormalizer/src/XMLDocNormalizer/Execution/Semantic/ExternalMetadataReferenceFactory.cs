using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Reconstructs Roslyn metadata references from exact PE snapshots whose
    /// build provenance was validated by P5C.
    /// </summary>
    internal static class ExternalMetadataReferenceFactory
    {
        /// <summary>
        /// Tries to create a Roslyn reference over the retained validated PE
        /// image with the original serialized reference properties.
        /// </summary>
        /// <param name="material">
        /// The validated candidate descriptor and its exact immutable image.
        /// </param>
        /// <param name="reference">The reconstructed Roslyn reference.</param>
        /// <returns>
        /// <see langword="true"/> when the reference properties are supported
        /// and the supplied image contains either a standalone module or a
        /// single-module assembly; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// The candidate path is retained only as descriptive provenance. The
        /// reference reads exclusively from the validated image and does not
        /// load or execute assembly code. Multi-module assemblies fail closed
        /// because P5C currently retains only the manifest PE image.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="material"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ValidatedExternalMetadataReferenceMaterial material,
            out PortableExecutableReference reference)
        {
            ArgumentNullException.ThrowIfNull(material);

            ExternalMetadataReferenceCandidateDescriptor candidate = material.Candidate;
            ExternalCompilationMetadataReferenceDescriptor expected =
                candidate.ExpectedReference;

            if (material.Image.IsDefaultOrEmpty || candidate.Kind != expected.Kind)
            {
                reference = null!;
                return false;
            }

            try
            {
                MetadataReferenceProperties properties = new(
                    expected.Kind,
                    expected.Aliases,
                    expected.EmbedInteropTypes);

                if (expected.Kind == MetadataImageKind.Assembly
                    && ContainsAdditionalManagedModule(material))
                {
                    reference = null!;
                    return false;
                }

                reference = MetadataReference.CreateFromImage(
                    material.Image,
                    properties,
                    documentation: null,
                    filePath: candidate.FilePath);
                return true;
            }
            catch (ArgumentException)
            {
                reference = null!;
                return false;
            }
            catch (BadImageFormatException)
            {
                reference = null!;
                return false;
            }
            catch (InvalidOperationException)
            {
                reference = null!;
                return false;
            }
        }

        /// <summary>
        /// Determines whether an assembly manifest refers to at least one
        /// additional file containing managed metadata.
        /// </summary>
        /// <param name="material">The validated manifest image.</param>
        /// <returns>
        /// <see langword="true"/> when an AssemblyFile entry represents a
        /// managed module; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ContainsAdditionalManagedModule(
            ValidatedExternalMetadataReferenceMaterial material)
        {
            using PEReader peReader = new(material.Image);
            MetadataReader metadataReader = peReader.GetMetadataReader();

            foreach (AssemblyFileHandle handle in metadataReader.AssemblyFiles)
            {
                if (metadataReader.GetAssemblyFile(handle).ContainsMetadata)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
