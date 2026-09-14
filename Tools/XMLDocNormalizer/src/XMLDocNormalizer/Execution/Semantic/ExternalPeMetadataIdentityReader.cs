using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Reads Roslyn identities from validated managed PE metadata without
    /// loading executable code.
    /// </summary>
    internal static class ExternalPeMetadataIdentityReader
    {
        /// <summary>
        /// Tries to reconstruct a complete Roslyn assembly identity from
        /// manifest metadata.
        /// </summary>
        /// <param name="metadataReader">The managed metadata reader.</param>
        /// <param name="assemblyIdentity">The reconstructed identity.</param>
        /// <returns>
        /// <see langword="true"/> when the assembly identity fields are
        /// supported and valid; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryReadAssemblyIdentity(
            MetadataReader metadataReader,
            out AssemblyIdentity assemblyIdentity)
        {
            try
            {
                AssemblyDefinition assemblyDefinition = metadataReader.GetAssemblyDefinition();
                AssemblyFlags contentTypeFlags =
                    assemblyDefinition.Flags & AssemblyFlags.ContentTypeMask;
                AssemblyContentType contentType;

                if (contentTypeFlags == 0)
                {
                    contentType = AssemblyContentType.Default;
                }
                else if (contentTypeFlags == AssemblyFlags.WindowsRuntime)
                {
                    contentType = AssemblyContentType.WindowsRuntime;
                }
                else
                {
                    assemblyIdentity = null!;
                    return false;
                }

                ImmutableArray<byte> publicKeyOrToken =
                    metadataReader.GetBlobContent(assemblyDefinition.PublicKey);
                bool hasPublicKey =
                    (assemblyDefinition.Flags & AssemblyFlags.PublicKey) != 0;
                assemblyIdentity = new AssemblyIdentity(
                    metadataReader.GetString(assemblyDefinition.Name),
                    assemblyDefinition.Version,
                    metadataReader.GetString(assemblyDefinition.Culture),
                    publicKeyOrToken,
                    hasPublicKey,
                    (assemblyDefinition.Flags & AssemblyFlags.Retargetable) != 0,
                    contentType);
                return true;
            }
            catch (BadImageFormatException)
            {
                assemblyIdentity = null!;
                return false;
            }
            catch (ArgumentException)
            {
                assemblyIdentity = null!;
                return false;
            }
        }
    }
}
