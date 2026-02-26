using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Namespace
{
    /// <summary>
    /// Represents a stable reporting location for an aggregated missing-namespace-documentation finding.
    /// </summary>
    /// <remarks>
    /// This type stores the minimum information required to create a finding at a stable location:
    /// syntax tree, file path and absolute anchor position.
    /// </remarks>
    internal sealed class NamespaceMissingLocation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamespaceMissingLocation"/> class.
        /// </summary>
        /// <param name="tree">The syntax tree used for line/column mapping.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="anchorPosition">The absolute anchor position used for reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tree"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null/empty/whitespace.</exception>
        public NamespaceMissingLocation(SyntaxTree tree, string filePath, int anchorPosition)
        {
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            Tree = tree;
            FilePath = filePath;
            AnchorPosition = anchorPosition;
        }

        /// <summary>
        /// Gets the syntax tree used for line/column mapping.
        /// </summary>
        public SyntaxTree Tree { get; }

        /// <summary>
        /// Gets the file path used for reporting.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the absolute anchor position used for reporting.
        /// </summary>
        public int AnchorPosition { get; }
    }
}