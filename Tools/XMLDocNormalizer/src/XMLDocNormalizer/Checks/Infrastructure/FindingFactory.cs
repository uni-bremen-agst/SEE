using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides helper methods to create <see cref="Finding"/> instances with stable line/column calculation.
    /// </summary>
    internal static class FindingFactory
    {
        /// <summary>
        /// Creates a <see cref="Finding"/> anchored at the given absolute position in the syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree used to compute line and column information.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The tag name associated with the finding (e.g. <c>summary</c>).</param>
        /// <param name="smell">The smell definition describing the finding.</param>
        /// <param name="absolutePosition">The absolute position used as anchor for line/column.</param>
        /// <param name="snippet">An optional snippet for display (may be empty).</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        /// <returns>A constructed <see cref="Finding"/> instance.</returns>
        public static Finding AtPosition(
            SyntaxTree tree,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            int absolutePosition,
            string snippet = "",
            params object[] messageArgs)
        {
            TextSpan span = new(absolutePosition, length: 1);
            FileLinePositionSpan lineSpan = tree.GetLineSpan(span);

            int line = lineSpan.StartLinePosition.Line + 1;
            int column = lineSpan.StartLinePosition.Character + 1;

            return new Finding(
                smell,
                filePath,
                tagName,
                line,
                column,
                snippet,
                messageArgs);
        }

        /// <summary>
        /// Creates a <see cref="Finding"/> anchored at the start of the given <see cref="TextSpan"/>.
        /// </summary>
        /// <param name="tree">The syntax tree used to compute line and column information.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The tag name associated with the finding.</param>
        /// <param name="smell">The smell definition describing the finding.</param>
        /// <param name="span">The span whose start is used as anchor for line/column.</param>
        /// <param name="snippet">An optional snippet for display (may be empty).</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        /// <returns>A constructed <see cref="Finding"/> instance.</returns>
        public static Finding AtSpanStart(
            SyntaxTree tree,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            TextSpan span,
            string snippet = "",
            params object[] messageArgs)
        {
            return AtPosition(tree, filePath, tagName, smell, span.Start, snippet, messageArgs);
        }
    }
}
