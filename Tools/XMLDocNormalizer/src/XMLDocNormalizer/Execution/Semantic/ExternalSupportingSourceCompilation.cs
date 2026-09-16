using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Couples one successfully reconstructed external C# compilation to the
    /// validated binary identity of its original target.
    /// </summary>
    /// <remarks>
    /// A registered external supporting-source compilation provides
    /// source-backed semantic information for an already validated external
    /// binary dependency. It is not an analysis target and does not
    /// independently produce XML-documentation findings.
    /// </remarks>
    internal sealed class ExternalSupportingSourceCompilation
    {
        /// <summary>
        /// Initializes a validated P5K handoff without retaining intermediate
        /// source, PE, PDB, or candidate-path material.
        /// </summary>
        /// <param name="targetAssembly">
        /// The P3 identity of the original external binary.
        /// </param>
        /// <param name="compilation">
        /// The exact C# compilation instance validated by P5K.
        /// </param>
        private ExternalSupportingSourceCompilation(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            CSharpCompilation compilation)
        {
            TargetAssembly = targetAssembly;
            Compilation = compilation;
        }

        /// <summary>
        /// Gets the validated identity of the original external binary.
        /// </summary>
        /// <value>
        /// The full assembly identity, ordered module identities, and
        /// reference-assembly classification. Its path is provenance only.
        /// </value>
        public ExternalAssemblyReferenceDescriptor TargetAssembly { get; }

        /// <summary>
        /// Gets the exact source-backed compilation instance produced by P5K.
        /// </summary>
        /// <value>The reconstructed C# compilation.</value>
        public CSharpCompilation Compilation { get; }

        /// <summary>
        /// Tries to create a registration handoff from an existing P5K result.
        /// </summary>
        /// <param name="targetAssembly">The P3-validated target identity.</param>
        /// <param name="compilationProvenance">The P5A provenance.</param>
        /// <param name="configuration">The P5G configuration.</param>
        /// <param name="syntaxTreeSet">The complete P5J tree set.</param>
        /// <param name="referenceSet">The complete P5E reference set.</param>
        /// <param name="compilation">
        /// The exact compilation returned by a successful P5K composition.
        /// </param>
        /// <param name="supportingSource">
        /// The validated registration handoff when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when all machine-checkable P5K input and
        /// composition invariants still hold for the exact compilation
        /// instance; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Completeness of generator output and the absence of historically
        /// version-sensitive compiler behavior remain caller and acquisition
        /// preconditions; they cannot be proven by registration.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any input is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            ExternalCompilationProvenanceDescriptor compilationProvenance,
            ExternalCSharpCompilationConfiguration configuration,
            ExternalCSharpSyntaxTreeSet syntaxTreeSet,
            ExternalMetadataReferenceSet referenceSet,
            CSharpCompilation compilation,
            out ExternalSupportingSourceCompilation supportingSource)
        {
            if (!ExternalCSharpCompilationFactory.IsValidComposition(
                    targetAssembly,
                    compilationProvenance,
                    configuration,
                    syntaxTreeSet,
                    referenceSet,
                    compilation))
            {
                supportingSource = null!;
                return false;
            }

            supportingSource = new ExternalSupportingSourceCompilation(
                targetAssembly,
                compilation);
            return true;
        }
    }
}
