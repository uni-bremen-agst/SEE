namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents a single tool finding (check result) with location, tag and smell information.
    /// </summary>
    internal sealed class Finding
    {
        /// <summary>
        /// Gets the smell (rule) that produced this finding.
        /// </summary>
        public XmlDocSmell Smell { get; }

        /// <summary>
        /// Gets the file path of the finding.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the XML tag name associated with the finding (e.g., "returns", "paramref").
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// Gets the 1-based line number.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the 1-based column number.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Gets the human-readable finding message.
        /// </summary>
        /// <remarks>
        /// This is the formatted message for <see cref="Smell"/> with any placeholder arguments applied.
        /// </remarks>
        public string Message { get; }

        /// <summary>
        /// Gets a short snippet of the problematic node.
        /// </summary>
        public string Snippet { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Finding"/> class.
        /// </summary>
        /// <param name="smell">The smell (rule) that produced the finding.</param>
        /// <param name="filePath">The source file path.</param>
        /// <param name="tagName">The XML tag name.</param>
        /// <param name="line">The 1-based line number.</param>
        /// <param name="column">The 1-based column number.</param>
        /// <param name="snippet">A snippet of the problematic node.</param>
        /// <param name="messageArgs">Optional formatting arguments for the smell message template.</param>
        public Finding(
            XmlDocSmell smell,
            string filePath,
            string tagName,
            int line,
            int column,
            string snippet,
            params object[] messageArgs)
        {
            Smell = smell ?? throw new ArgumentNullException(nameof(smell));

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be null or whitespace.", nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(tagName))
            {
                throw new ArgumentException("Tag name must not be null or whitespace.", nameof(tagName));
            }

            if (line < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "Line must be 1-based and greater than or equal to 1.");
            }

            if (column < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(column), "Column must be 1-based and greater than or equal to 1.");
            }

            FilePath = filePath;
            TagName = tagName;
            Line = line;
            Column = column;
            Snippet = snippet ?? string.Empty;

            Message = smell.FormatMessage(messageArgs ?? Array.Empty<object>());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Smell.Id}|{Smell.Severity}] [{Line},{Column}] <{TagName}>: {Message} | {Snippet}";
        }
    }
}
