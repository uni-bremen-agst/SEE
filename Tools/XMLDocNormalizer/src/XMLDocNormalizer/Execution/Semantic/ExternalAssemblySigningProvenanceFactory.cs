using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>Reads exact signing metadata from a validated target PE snapshot.</summary>
    internal static class ExternalAssemblySigningProvenanceFactory
    {
        /// <summary>Creates immutable signing provenance without validating a signature.</summary>
        /// <param name="peReader">The reader over the validated complete PE snapshot.</param>
        /// <param name="metadataReader">The reader over its manifest metadata.</param>
        /// <param name="assemblyIdentity">The independently validated full identity.</param>
        /// <param name="provenance">The exact signing provenance when readable.</param>
        /// <returns>
        /// <see langword="true"/> when all signing fields are readable and the
        /// manifest key agrees with <paramref name="assemblyIdentity"/>.
        /// Unsupported but readable shapes are returned as
        /// <see cref="ExternalAssemblySigningState.Unsupported"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="peReader"/>, <paramref name="metadataReader"/>,
        /// or <paramref name="assemblyIdentity"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            PEReader peReader,
            MetadataReader metadataReader,
            AssemblyIdentity assemblyIdentity,
            out ExternalAssemblySigningProvenance provenance)
        {
            ArgumentNullException.ThrowIfNull(peReader);
            ArgumentNullException.ThrowIfNull(metadataReader);
            ArgumentNullException.ThrowIfNull(assemblyIdentity);

            CorHeader? corHeader = peReader.PEHeaders.CorHeader;
            if (corHeader == null)
            {
                provenance = null!;
                return false;
            }

            try
            {
                AssemblyDefinition definition = metadataReader.GetAssemblyDefinition();
                AssemblyFlags assemblyFlags = definition.Flags;
                ImmutableArray<byte> publicKey = metadataReader
                    .GetBlobContent(definition.PublicKey);
                if (!publicKey.AsSpan().SequenceEqual(assemblyIdentity.PublicKey.AsSpan())
                    || !assemblyIdentity.PublicKeyToken.AsSpan().SequenceEqual(
                        CreatePublicKeyToken(publicKey).AsSpan()))
                {
                    provenance = null!;
                    return false;
                }

                DirectoryEntry signatureDirectory = corHeader.StrongNameSignatureDirectory;
                bool directoryAbsent = signatureDirectory.RelativeVirtualAddress == 0
                    && signatureDirectory.Size == 0;
                bool directoryPresent = signatureDirectory.RelativeVirtualAddress > 0
                    && signatureDirectory.Size > 0;
                ImmutableArray<byte> signature = directoryPresent
                    ? peReader.GetSectionData(signatureDirectory.RelativeVirtualAddress)
                        .GetContent(0, signatureDirectory.Size)
                    : ImmutableArray<byte>.Empty;
                if ((!directoryAbsent && !directoryPresent)
                    || signature.Length != signatureDirectory.Size)
                {
                    provenance = null!;
                    return false;
                }

                bool publicKeyFlag = (assemblyFlags & AssemblyFlags.PublicKey) != 0;
                bool hasPublicKey = !publicKey.IsEmpty;
                bool strongNameSigned =
                    (corHeader.Flags & CorFlags.StrongNameSigned) != 0;
                bool hasNonzeroSignature = signature.Any(static value => value != 0);
                ExternalAssemblySigningState state = Classify(
                    publicKeyFlag,
                    hasPublicKey,
                    strongNameSigned,
                    directoryPresent,
                    hasNonzeroSignature);
                ImmutableArray<byte> signatureHash = directoryPresent
                    ? ImmutableArray.Create(SHA256.HashData(signature.AsSpan()))
                    : ImmutableArray<byte>.Empty;
                provenance = new ExternalAssemblySigningProvenance(
                    state,
                    assemblyFlags,
                    corHeader.Flags,
                    publicKey,
                    assemblyIdentity.PublicKeyToken,
                    signatureDirectory.Size,
                    signatureHash);
                return true;
            }
            catch (BadImageFormatException)
            {
                provenance = null!;
                return false;
            }
            catch (ArgumentException)
            {
                provenance = null!;
                return false;
            }
        }

        /// <summary>Classifies only exact observable PE signing fields.</summary>
        /// <param name="publicKeyFlag">Whether the manifest declares a full public key.</param>
        /// <param name="hasPublicKey">Whether full public-key bytes are present.</param>
        /// <param name="strongNameSigned">Whether the CLI header declares strong-name signing.</param>
        /// <param name="directoryPresent">Whether a nonempty signature directory is present.</param>
        /// <param name="hasNonzeroSignature">Whether the signature contains a nonzero byte.</param>
        /// <returns>The exact supported or unsupported observable signing state.</returns>
        private static ExternalAssemblySigningState Classify(
            bool publicKeyFlag,
            bool hasPublicKey,
            bool strongNameSigned,
            bool directoryPresent,
            bool hasNonzeroSignature)
        {
            if (!publicKeyFlag
                && !hasPublicKey
                && !strongNameSigned
                && !directoryPresent)
            {
                return ExternalAssemblySigningState.Unsigned;
            }

            if (publicKeyFlag
                && hasPublicKey
                && strongNameSigned
                && directoryPresent)
            {
                return hasNonzeroSignature
                    ? ExternalAssemblySigningState.FullySigned
                    : ExternalAssemblySigningState.PublicSigned;
            }

            if (publicKeyFlag
                && hasPublicKey
                && !strongNameSigned
                && directoryPresent
                && !hasNonzeroSignature)
            {
                return ExternalAssemblySigningState.DelaySigned;
            }

            return ExternalAssemblySigningState.Unsupported;
        }

        /// <summary>Derives the standard strong-name token from a complete public key.</summary>
        /// <param name="publicKey">The complete manifest public key.</param>
        /// <returns>The derived eight-byte token, or an empty value for no key.</returns>
        private static ImmutableArray<byte> CreatePublicKeyToken(ImmutableArray<byte> publicKey)
        {
            if (publicKey.IsEmpty)
            {
                return ImmutableArray<byte>.Empty;
            }

            byte[] hash = SHA1.HashData(publicKey.AsSpan());
            Array.Reverse(hash, hash.Length - 8, 8);
            return ImmutableArray.Create(hash, hash.Length - 8, 8);
        }
    }
}
