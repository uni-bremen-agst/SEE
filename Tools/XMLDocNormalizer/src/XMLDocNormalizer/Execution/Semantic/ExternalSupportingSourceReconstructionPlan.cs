using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Stores stable, explicitly prepared candidate inputs for one exact
    /// external binary reconstruction.
    /// </summary>
    internal sealed class ExternalSupportingSourceReconstructionPlan :
        IEquatable<ExternalSupportingSourceReconstructionPlan>
    {
        /// <summary>
        /// Initializes an immutable reconstruction plan without opening any
        /// candidate file.
        /// </summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="targetPeCandidatePath">The explicit target PE path.</param>
        /// <param name="portablePdbCandidatePath">The explicit Portable PDB path.</param>
        /// <param name="referenceCandidatePaths">
        /// Explicit reference paths aligned to the P5A reference ordinals.
        /// </param>
        /// <param name="sourceInputs">
        /// Explicit source-tree ordinals selecting Portable PDB document
        /// ordinals and either embedded source or an explicit file.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required input is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an input sequence contains a null element.
        /// </exception>
        public ExternalSupportingSourceReconstructionPlan(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            string targetPeCandidatePath,
            string portablePdbCandidatePath,
            IEnumerable<string> referenceCandidatePaths,
            IEnumerable<ExternalSourceReconstructionInput> sourceInputs)
        {
            ArgumentNullException.ThrowIfNull(targetAssembly);
            ArgumentNullException.ThrowIfNull(targetPeCandidatePath);
            ArgumentNullException.ThrowIfNull(portablePdbCandidatePath);
            ArgumentNullException.ThrowIfNull(referenceCandidatePaths);
            ArgumentNullException.ThrowIfNull(sourceInputs);

            ImmutableArray<string> references = referenceCandidatePaths.ToImmutableArray();
            ImmutableArray<ExternalSourceReconstructionInput> sources =
                sourceInputs.ToImmutableArray();

            if (references.Any(static path => path == null)
                || sources.Any(static source => source == null))
            {
                throw new ArgumentException(
                    "Reconstruction input sequences cannot contain null elements.");
            }

            TargetAssembly = targetAssembly;
            TargetPeCandidatePath = targetPeCandidatePath;
            PortablePdbCandidatePath = portablePdbCandidatePath;
            ReferenceCandidatePaths = references;
            SourceInputs = sources;
        }

        /// <summary>
        /// Gets the exact P3 binary identity used as the plan key.
        /// </summary>
        /// <value>The identity including ordered module MVIDs.</value>
        public ExternalAssemblyReferenceDescriptor TargetAssembly { get; }

        /// <summary>
        /// Gets the explicitly selected target PE candidate path.
        /// </summary>
        /// <value>The target candidate path.</value>
        public string TargetPeCandidatePath { get; }

        /// <summary>
        /// Gets the explicitly selected Portable PDB candidate path.
        /// </summary>
        /// <value>The Portable PDB path.</value>
        public string PortablePdbCandidatePath { get; }

        /// <summary>
        /// Gets the immutable reference candidate sequence.
        /// </summary>
        /// <value>The paths in original P5A reference ordinal order.</value>
        public ImmutableArray<string> ReferenceCandidatePaths { get; }

        /// <summary>
        /// Gets the immutable source-tree input sequence.
        /// </summary>
        /// <value>The source inputs in original compilation tree order.</value>
        public ImmutableArray<ExternalSourceReconstructionInput> SourceInputs { get; }

        /// <inheritdoc/>
        public bool Equals(ExternalSupportingSourceReconstructionPlan? other)
        {
            return ReferenceEquals(this, other)
                || (other != null
                    && TargetAssembly.Equals(other.TargetAssembly)
                    && string.Equals(
                        TargetPeCandidatePath,
                        other.TargetPeCandidatePath,
                        StringComparison.Ordinal)
                    && string.Equals(
                        PortablePdbCandidatePath,
                        other.PortablePdbCandidatePath,
                        StringComparison.Ordinal)
                    && ReferenceCandidatePaths.SequenceEqual(
                        other.ReferenceCandidatePaths,
                        StringComparer.Ordinal)
                    && SourceInputs.SequenceEqual(other.SourceInputs));
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ExternalSupportingSourceReconstructionPlan);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(TargetAssembly);
            hash.Add(TargetPeCandidatePath, StringComparer.Ordinal);
            hash.Add(PortablePdbCandidatePath, StringComparer.Ordinal);

            foreach (string path in ReferenceCandidatePaths)
            {
                hash.Add(path, StringComparer.Ordinal);
            }

            foreach (ExternalSourceReconstructionInput source in SourceInputs)
            {
                hash.Add(source);
            }

            return hash.ToHashCode();
        }
    }
}
