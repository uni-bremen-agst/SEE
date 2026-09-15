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
        internal ExternalCSharpSyntaxTreeSet(ImmutableArray<SyntaxTree> trees)
        {
            Trees = trees.IsDefault ? ImmutableArray<SyntaxTree>.Empty : trees;
        }

        /// <summary>
        /// Gets the original reconstructed trees in explicit source-document
        /// order.
        /// </summary>
        /// <value>The immutable ordered Roslyn syntax-tree sequence.</value>
        public ImmutableArray<SyntaxTree> Trees { get; }
    }
}
