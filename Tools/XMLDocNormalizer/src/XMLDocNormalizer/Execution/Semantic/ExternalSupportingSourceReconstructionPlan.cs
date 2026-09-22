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
            : this(
                targetAssembly,
                targetPeCandidatePath,
                portablePdbCandidatePath,
                referenceCandidatePaths,
                sourceInputs,
                discoverReferenceCandidatesLocally: false)
        {
        }

        /// <summary>
        /// Creates a plan that explicitly opts into demand-driven local
        /// discovery for every P5A metadata-reference ordinal.
        /// </summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="targetPeCandidatePath">The explicit target PE path.</param>
        /// <param name="portablePdbCandidatePath">The explicit Portable PDB path.</param>
        /// <param name="sourceInputs">The explicit source-tree input sequence.</param>
        /// <returns>
        /// An immutable plan whose reference candidates must be discovered
        /// within search roots configured on its semantic context.
        /// </returns>
        /// <remarks>
        /// Discovery only finds local candidate files. A candidate becomes
        /// usable only after the existing P5B/P5C validation succeeds.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required input is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sourceInputs"/> contains a null element.
        /// </exception>
        public static ExternalSupportingSourceReconstructionPlan
            CreateWithLocalReferenceDiscovery(
                ExternalAssemblyReferenceDescriptor targetAssembly,
                string targetPeCandidatePath,
                string portablePdbCandidatePath,
                IEnumerable<ExternalSourceReconstructionInput> sourceInputs)
        {
            return new ExternalSupportingSourceReconstructionPlan(
                targetAssembly,
                targetPeCandidatePath,
                portablePdbCandidatePath,
                ImmutableArray<string>.Empty,
                sourceInputs,
                discoverReferenceCandidatesLocally: true);
        }

        /// <summary>
        /// Creates a plan that opts into demand-driven local Portable PDB and
        /// metadata-reference candidate discovery.
        /// </summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="targetPeCandidatePath">The explicit target PE path.</param>
        /// <param name="sourceInputs">The explicit source-tree input sequence.</param>
        /// <returns>A plan without an authoritative explicit PDB candidate.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required input is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sourceInputs"/> contains a null element.
        /// </exception>
        public static ExternalSupportingSourceReconstructionPlan
            CreateWithLocalArtifactDiscovery(
                ExternalAssemblyReferenceDescriptor targetAssembly,
                string targetPeCandidatePath,
                IEnumerable<ExternalSourceReconstructionInput> sourceInputs)
        {
            return new ExternalSupportingSourceReconstructionPlan(
                targetAssembly,
                targetPeCandidatePath,
                portablePdbCandidatePath: null,
                ImmutableArray<string>.Empty,
                sourceInputs,
                discoverReferenceCandidatesLocally: true,
                discoverPortablePdbLocally: true);
        }

        /// <summary>
        /// Creates a plan with explicit reference candidates and demand-driven
        /// local Portable PDB discovery.
        /// </summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="targetPeCandidatePath">The explicit target PE path.</param>
        /// <param name="referenceCandidatePaths">The explicit ordinal reference paths.</param>
        /// <param name="sourceInputs">The explicit source-tree input sequence.</param>
        /// <returns>A plan without an authoritative explicit PDB candidate.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required input is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an input sequence contains a null element.
        /// </exception>
        public static ExternalSupportingSourceReconstructionPlan
            CreateWithLocalPortablePdbDiscovery(
                ExternalAssemblyReferenceDescriptor targetAssembly,
                string targetPeCandidatePath,
                IEnumerable<string> referenceCandidatePaths,
                IEnumerable<ExternalSourceReconstructionInput> sourceInputs)
        {
            return new ExternalSupportingSourceReconstructionPlan(
                targetAssembly,
                targetPeCandidatePath,
                portablePdbCandidatePath: null,
                referenceCandidatePaths,
                sourceInputs,
                discoverReferenceCandidatesLocally: false,
                discoverPortablePdbLocally: true);
        }

        /// <summary>
        /// Initializes one explicit or discovery-enabled immutable plan.
        /// </summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="targetPeCandidatePath">The explicit target PE path.</param>
        /// <param name="portablePdbCandidatePath">The explicit Portable PDB path.</param>
        /// <param name="referenceCandidatePaths">
        /// The explicit reference candidates, or an empty sequence for discovery.
        /// </param>
        /// <param name="sourceInputs">The explicit source-tree input sequence.</param>
        /// <param name="discoverReferenceCandidatesLocally">
        /// Whether P5A reference candidates must be discovered locally.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required input is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an input sequence contains a null element.
        /// </exception>
        private ExternalSupportingSourceReconstructionPlan(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            string targetPeCandidatePath,
            string portablePdbCandidatePath,
            IEnumerable<string> referenceCandidatePaths,
            IEnumerable<ExternalSourceReconstructionInput> sourceInputs,
            bool discoverReferenceCandidatesLocally)
            : this(
                targetAssembly,
                targetPeCandidatePath,
                portablePdbCandidatePath,
                referenceCandidatePaths,
                sourceInputs,
                discoverReferenceCandidatesLocally,
                discoverPortablePdbLocally: false)
        {
        }

        /// <summary>Initializes the complete immutable plan state.</summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="targetPeCandidatePath">The explicit target PE path.</param>
        /// <param name="portablePdbCandidatePath">The optional authoritative PDB path.</param>
        /// <param name="referenceCandidatePaths">The explicit reference paths.</param>
        /// <param name="sourceInputs">The explicit source-tree inputs.</param>
        /// <param name="discoverReferenceCandidatesLocally">Whether references are discovered.</param>
        /// <param name="discoverPortablePdbLocally">Whether a missing PDB is discovered.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any required input is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the PDB policy is inconsistent or an input sequence
        /// contains a null element.
        /// </exception>
        private ExternalSupportingSourceReconstructionPlan(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            string targetPeCandidatePath,
            string? portablePdbCandidatePath,
            IEnumerable<string> referenceCandidatePaths,
            IEnumerable<ExternalSourceReconstructionInput> sourceInputs,
            bool discoverReferenceCandidatesLocally,
            bool discoverPortablePdbLocally)
        {
            ArgumentNullException.ThrowIfNull(targetAssembly);
            ArgumentNullException.ThrowIfNull(targetPeCandidatePath);
            ArgumentNullException.ThrowIfNull(referenceCandidatePaths);
            ArgumentNullException.ThrowIfNull(sourceInputs);

            if ((portablePdbCandidatePath == null) != discoverPortablePdbLocally)
            {
                throw new ArgumentException(
                    "Exactly one explicit or locally discovered Portable PDB policy is required.");
            }

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
            DiscoverReferenceCandidatesLocally = discoverReferenceCandidatesLocally;
            DiscoverPortablePdbLocally = discoverPortablePdbLocally;
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
        public string? PortablePdbCandidatePath { get; }

        /// <summary>
        /// Gets whether a missing authoritative PDB path is resolved through
        /// context-local candidate acquisition.
        /// </summary>
        /// <value><see langword="true"/> only for explicit local opt-in.</value>
        public bool DiscoverPortablePdbLocally { get; }

        /// <summary>
        /// Gets the immutable reference candidate sequence.
        /// </summary>
        /// <value>The paths in original P5A reference ordinal order.</value>
        public ImmutableArray<string> ReferenceCandidatePaths { get; }

        /// <summary>
        /// Gets whether every P5A reference candidate must be discovered from
        /// explicitly configured local search roots.
        /// </summary>
        /// <value>
        /// <see langword="true"/> for explicit local discovery; otherwise
        /// <see langword="false"/> for authoritative explicit paths.
        /// </value>
        public bool DiscoverReferenceCandidatesLocally { get; }

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
                    && SourceInputs.SequenceEqual(other.SourceInputs)
                    && DiscoverReferenceCandidatesLocally
                        == other.DiscoverReferenceCandidatesLocally
                    && DiscoverPortablePdbLocally == other.DiscoverPortablePdbLocally);
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

            hash.Add(DiscoverReferenceCandidatesLocally);
            hash.Add(DiscoverPortablePdbLocally);

            return hash.ToHashCode();
        }
    }
}
