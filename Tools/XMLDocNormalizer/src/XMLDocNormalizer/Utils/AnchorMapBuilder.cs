using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides helper methods for building anchor maps that associate identifier names
    /// (e.g., parameter or type parameter names) with their corresponding source positions.
    /// </summary>
    internal static class AnchorMapBuilder
    {
        /// <summary>
        /// Builds a dictionary mapping identifier names to their anchor positions (<see cref="SyntaxToken.SpanStart"/>).
        /// </summary>
        /// <typeparam name="T">
        /// The type of items that contain an identifier token (e.g., <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax"/>
        /// or <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.TypeParameterSyntax"/>).
        /// </typeparam>
        /// <param name="items">
        /// The collection of items from which identifier tokens are extracted.
        /// </param>
        /// <param name="identifierSelector">
        /// A function that selects the <see cref="SyntaxToken"/> representing the identifier from each item.
        /// </param>
        /// <param name="comparer">
        /// An optional <see cref="StringComparer"/> used for key comparison.
        /// If <c>null</c>, <see cref="StringComparer.Ordinal"/> is used.
        /// </param>
        /// <returns>
        /// A dictionary mapping identifier names to their corresponding anchor positions.
        /// If duplicate names occur, the first occurrence is retained.
        /// Identifiers with empty or whitespace-only names are ignored.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="items"/> or <paramref name="identifierSelector"/> is <c>null</c>.
        /// </exception>
        public static Dictionary<string, int> BuildAnchors<T>(
            IEnumerable<T> items,
            Func<T, SyntaxToken> identifierSelector,
            StringComparer? comparer = null)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (identifierSelector == null)
            {
                throw new ArgumentNullException(nameof(identifierSelector));
            }

            if (comparer == null)
            {
                comparer = StringComparer.Ordinal;
            }

            Dictionary<string, int> anchorByName = new(comparer);

            foreach (T item in items)
            {
                SyntaxToken identifierToken = identifierSelector(item);
                string name = identifierToken.ValueText;

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (anchorByName.ContainsKey(name) == false)
                {
                    anchorByName[name] = identifierToken.SpanStart;
                }
            }

            return anchorByName;
        }
    }
}