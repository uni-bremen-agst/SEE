using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.PortableExecutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Records immutable signing metadata read from one validated target PE.
    /// </summary>
    internal sealed class ExternalAssemblySigningProvenance
    {
        /// <summary>Gets canonical unsigned provenance for controlled synthetic inputs.</summary>
        /// <value>An unsigned IL-only target shape without key or signature data.</value>
        internal static ExternalAssemblySigningProvenance Unsigned { get; } = new(
            ExternalAssemblySigningState.Unsigned,
            default,
            CorFlags.ILOnly,
            ImmutableArray<byte>.Empty,
            ImmutableArray<byte>.Empty,
            strongNameSignatureSize: 0,
            ImmutableArray<byte>.Empty);

        /// <summary>Initializes exact immutable target signing provenance.</summary>
        /// <param name="state">The classified PE signing shape.</param>
        /// <param name="assemblyFlags">The exact manifest assembly flags.</param>
        /// <param name="corFlags">The exact CLI header flags.</param>
        /// <param name="publicKey">The complete manifest public key.</param>
        /// <param name="publicKeyToken">The token derived by Roslyn from the key.</param>
        /// <param name="strongNameSignatureSize">The signature-directory size.</param>
        /// <param name="strongNameSignatureSha256">
        /// The SHA-256 digest of the signature bytes, or an empty value when absent.
        /// </param>
        internal ExternalAssemblySigningProvenance(
            ExternalAssemblySigningState state,
            AssemblyFlags assemblyFlags,
            CorFlags corFlags,
            ImmutableArray<byte> publicKey,
            ImmutableArray<byte> publicKeyToken,
            int strongNameSignatureSize,
            ImmutableArray<byte> strongNameSignatureSha256)
        {
            State = state;
            AssemblyFlags = assemblyFlags;
            CorFlags = corFlags;
            PublicKey = publicKey.IsDefault ? ImmutableArray<byte>.Empty : publicKey;
            PublicKeyToken = publicKeyToken.IsDefault
                ? ImmutableArray<byte>.Empty
                : publicKeyToken;
            StrongNameSignatureSize = strongNameSignatureSize;
            StrongNameSignatureSha256 = strongNameSignatureSha256.IsDefault
                ? ImmutableArray<byte>.Empty
                : strongNameSignatureSha256;
        }

        /// <summary>Gets the classified target signing shape.</summary>
        /// <value>The exact binary signing-state classification.</value>
        public ExternalAssemblySigningState State { get; }

        /// <summary>Gets the exact manifest assembly flags.</summary>
        /// <value>The flags from the Assembly metadata row.</value>
        public AssemblyFlags AssemblyFlags { get; }

        /// <summary>Gets the exact CLI header flags.</summary>
        /// <value>The target PE CLI flags.</value>
        public CorFlags CorFlags { get; }

        /// <summary>Gets the complete manifest public key.</summary>
        /// <value>The immutable key bytes, or an empty value for unsigned targets.</value>
        public ImmutableArray<byte> PublicKey { get; }

        /// <summary>Gets the token derived from the complete public key.</summary>
        /// <value>The immutable token bytes, or an empty value for unsigned targets.</value>
        public ImmutableArray<byte> PublicKeyToken { get; }

        /// <summary>Gets the original strong-name signature-directory size.</summary>
        /// <value>The nonnegative size in bytes.</value>
        public int StrongNameSignatureSize { get; }

        /// <summary>Gets a digest of the original signature bytes.</summary>
        /// <value>The SHA-256 digest, or an empty value when no directory exists.</value>
        public ImmutableArray<byte> StrongNameSignatureSha256 { get; }

        /// <summary>Gets whether semantic reconstruction supports this exact shape.</summary>
        /// <value>
        /// <see langword="true"/> only for unsigned or fully signed targets.
        /// </value>
        public bool IsSemanticReconstructionSupported =>
            State == ExternalAssemblySigningState.Unsigned
                ? (AssemblyFlags & AssemblyFlags.PublicKey) == 0
                    && (CorFlags & CorFlags.StrongNameSigned) == 0
                    && PublicKey.IsEmpty
                    && PublicKeyToken.IsEmpty
                    && StrongNameSignatureSize == 0
                    && StrongNameSignatureSha256.IsEmpty
                : State == ExternalAssemblySigningState.FullySigned
                    && (AssemblyFlags & AssemblyFlags.PublicKey) != 0
                    && (CorFlags & CorFlags.StrongNameSigned) != 0
                    && !PublicKey.IsEmpty
                    && !PublicKeyToken.IsEmpty
                    && HasStrongNameSignature;

        /// <summary>Gets whether the original PE contains nonzero signature bytes.</summary>
        /// <value><see langword="true"/> only for a classified fully signed target.</value>
        public bool HasStrongNameSignature =>
            State == ExternalAssemblySigningState.FullySigned
                && StrongNameSignatureSize > 0
                && StrongNameSignatureSha256.Length == 32;
    }
}
