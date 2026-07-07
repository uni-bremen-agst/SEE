using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides helper methods to create findings with stable line and column calculation.
    /// </summary>
    internal static class FindingFactory
    {
        /// <summary>
        /// Creates a finding anchored at the given absolute position in the syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree used to compute line and column information.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The XML documentation tag name associated with the finding.</param>
        /// <param name="smell">The smell definition describing the finding.</param>
        /// <param name="absolutePosition">The absolute source position used as anchor for line and column calculation.</param>
        /// <param name="snippet">An optional source snippet for display.</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        /// <returns>
        /// A constructed finding instance.
        /// </returns>
        public static Finding AtPosition(
            SyntaxTree tree,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            int absolutePosition,
            string snippet = "",
            params object[] messageArgs)
        {
            return AtPosition(
                tree,
                filePath,
                tagName,
                smell,
                absolutePosition,
                context: null,
                snippet: snippet,
                messageArgs: messageArgs);
        }

        /// <summary>
        /// Creates a finding anchored at the given absolute position in the syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree used to compute line and column information.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The XML documentation tag name associated with the finding.</param>
        /// <param name="smell">The smell definition describing the finding.</param>
        /// <param name="absolutePosition">The absolute source position used as anchor for line and column calculation.</param>
        /// <param name="context">The source declaration context for study-oriented reporting.</param>
        /// <param name="snippet">An optional source snippet for display.</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        /// <returns>
        /// A constructed finding instance.
        /// </returns>
        public static Finding AtPosition(
            SyntaxTree tree,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            int absolutePosition,
            FindingContext? context,
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
                context,
                messageArgs);
        }

        /// <summary>
        /// Creates a finding anchored at the start of the given source span.
        /// </summary>
        /// <param name="tree">The syntax tree used to compute line and column information.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The XML documentation tag name associated with the finding.</param>
        /// <param name="smell">The smell definition describing the finding.</param>
        /// <param name="span">The source span whose start is used as anchor for line and column calculation.</param>
        /// <param name="snippet">An optional source snippet for display.</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        /// <returns>
        /// A constructed finding instance.
        /// </returns>
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

        /// <summary>
        /// Creates a finding anchored at the start of the given source span.
        /// </summary>
        /// <param name="tree">The syntax tree used to compute line and column information.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="tagName">The XML documentation tag name associated with the finding.</param>
        /// <param name="smell">The smell definition describing the finding.</param>
        /// <param name="span">The source span whose start is used as anchor for line and column calculation.</param>
        /// <param name="context">The source declaration context for study-oriented reporting.</param>
        /// <param name="snippet">An optional source snippet for display.</param>
        /// <param name="messageArgs">Optional message arguments used for placeholder formatting.</param>
        /// <returns>
        /// A constructed finding instance.
        /// </returns>
        public static Finding AtSpanStart(
            SyntaxTree tree,
            string filePath,
            string tagName,
            XmlDocSmell smell,
            TextSpan span,
            FindingContext? context,
            string snippet = "",
            params object[] messageArgs)
        {
            return AtPosition(tree, filePath, tagName, smell, span.Start, context, snippet, messageArgs);
        }
    }
}
