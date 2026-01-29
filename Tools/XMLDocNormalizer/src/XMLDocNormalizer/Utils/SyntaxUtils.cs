using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Utils
{
    internal static class SyntaxUtils
    {
        /// <summary>
        /// Creates a short, single-line snippet for a syntax node that is suitable for console output.
        /// </summary>
        /// <param name="node">The node to create a snippet for.</param>
        /// <returns>A single-line snippet, truncated to a reasonable maximum length.</returns>
        internal static string GetSnippet(SyntaxNode node)
        {
            string snippet = node.ToString().Replace(Environment.NewLine, " ");
            if (snippet.Length > 160)
            {
                snippet = snippet.Substring(0, 160) + "...";
            }

            return snippet;
        }
    }
}