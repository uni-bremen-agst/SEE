using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds an immutable ordered sequence of reconstructed C# syntax trees
    /// whose source-document provenance was explicitly aligned and validated.
    /// </summary>
    /// <remarks>
    /// The sequence is complete only for the caller-supplied reconstruction
    /// scope. It does not claim that every Portable PDB document represents an
    /// original compilation source file.
    /// </remarks>
    internal sealed class ExternalCSharpSyntaxTreeSet
    {
        /// <summary>
        /// Initializes an ordered reconstructed syntax-tree set.
        /// </summary>
        /// <param name="trees">The trees in explicit source-document order.</param>
        /// <param name="sourceMaterials">
        /// The aligned P5H material provenance, when retained by P5J.
        /// </param>
        internal ExternalCSharpSyntaxTreeSet(
            ImmutableArray<SyntaxTree> trees,
            ImmutableArray<ValidatedExternalSourceMaterial> sourceMaterials = default)
        {
            Trees = trees.IsDefault ? ImmutableArray<SyntaxTree>.Empty : trees;
            SourceMaterials = sourceMaterials.IsDefault
                ? ImmutableArray<ValidatedExternalSourceMaterial>.Empty
                : sourceMaterials;
        }

        /// <summary>
        /// Gets the original reconstructed trees in explicit source-document
        /// order.
        /// </summary>
        /// <value>The immutable ordered Roslyn syntax-tree sequence.</value>
        public ImmutableArray<SyntaxTree> Trees { get; }

        /// <summary>
        /// Gets the aligned exact source-material provenance retained from P5H.
        /// </summary>
        /// <value>
        /// Direct or reconstructed exact materials in source-tree order, or an
        /// empty sequence for legacy manually constructed test fixtures.
        /// </value>
        public ImmutableArray<ValidatedExternalSourceMaterial> SourceMaterials { get; }
    }
}
