namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides a comparer for XML documentation smell identifiers such as
    /// <c>DOC100</c>, <c>DOC610</c>, or <c>DOC3010</c>.
    ///
    /// The comparer sorts identifiers by the numeric suffix rather than
    /// performing a pure lexicographic string comparison.
    /// </summary>
    internal sealed class SmellIdComparer : IComparer<string>
    {
        /// <summary>
        /// Compares two smell identifiers based on their numeric suffix.
        /// </summary>
        /// <param name="left">
        /// The first smell identifier.
        /// </param>
        /// <param name="right">
        /// The second smell identifier.
        /// </param>
        /// <returns>
        /// A value less than zero if <paramref name="left"/> is less than <paramref name="right"/>,
        /// zero if both identifiers are equal,
        /// or a value greater than zero if <paramref name="left"/> is greater than <paramref name="right"/>.
        /// </returns>
        public int Compare(string? left, string? right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int leftNumber;
            int rightNumber;

            bool leftParsed = TryParseSmellNumber(left, out leftNumber);
            bool rightParsed = TryParseSmellNumber(right, out rightNumber);

            if (leftParsed && rightParsed)
            {
                return leftNumber.CompareTo(rightNumber);
            }

            return string.CompareOrdinal(left, right);
        }

        /// <summary>
        /// Attempts to extract the numeric suffix from a smell identifier.
        /// </summary>
        /// <param name="smellId">
        /// The smell identifier (for example <c>DOC610</c>).
        /// </param>
        /// <param name="number">
        /// When this method returns, contains the parsed numeric suffix if parsing succeeded.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the numeric suffix could be parsed successfully;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryParseSmellNumber(string smellId, out int number)
        {
            number = 0;

            if (!smellId.StartsWith("DOC", StringComparison.Ordinal))
            {
                return false;
            }

            string numericPart = smellId.Substring(3);

            return int.TryParse(numericPart, out number);
        }
    }
}
