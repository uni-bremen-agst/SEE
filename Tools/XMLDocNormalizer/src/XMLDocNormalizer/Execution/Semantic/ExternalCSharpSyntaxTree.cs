using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds source text reconstructed from checksum-validated bytes and the
    /// C# syntax tree parsed directly from that text.
    /// </summary>
    /// <remarks>
    /// This result represents source and parse reconstruction only. It is not
    /// a complete compilation or a claim of bit-identical compiler state.
    /// </remarks>
    internal sealed class ExternalCSharpSyntaxTree
    {
        /// <summary>
        /// Initializes a successfully reconstructed C# source tree.
        /// </summary>
        /// <param name="document">The Portable PDB document provenance.</param>
        /// <param name="text">The text decoded from validated source bytes.</param>
        /// <param name="tree">The syntax tree parsed directly from the text.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is <see langword="null"/>.
        /// </exception>
        internal ExternalCSharpSyntaxTree(
            ExternalSourceDocumentDescriptor document,
            SourceText text,
            SyntaxTree tree)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(tree);

            Document = document;
            Text = text;
            Tree = tree;
        }

        /// <summary>
        /// Gets the exact Portable PDB source-document provenance.
        /// </summary>
        /// <value>The document associated with the validated source bytes.</value>
        public ExternalSourceDocumentDescriptor Document { get; }

        /// <summary>
        /// Gets the source text decoded directly from validated source bytes.
        /// </summary>
        /// <value>The decoded text with its original-byte checksum.</value>
        public SourceText Text { get; }

        /// <summary>
        /// Gets the C# syntax tree parsed directly from <see cref="Text"/>.
        /// </summary>
        /// <value>
        /// The tree using reconstructed parse options and the document name as
        /// opaque path provenance. Parse diagnostics do not invalidate it.
        /// </value>
        public SyntaxTree Tree { get; }
    }
}
