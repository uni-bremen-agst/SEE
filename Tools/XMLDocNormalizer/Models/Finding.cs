namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents a single tool finding (check result) with location and message.
    /// </summary>
    internal sealed class Finding
    {
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
        public string Message { get; }

        /// <summary>
        /// Gets a short snippet of the problematic node.
        /// </summary>
        public string Snippet { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Finding"/> class.
        /// </summary>
        /// <param name="filePath">The source file path.</param>
        /// <param name="tagName">The XML tag name.</param>
        /// <param name="line">The 1-based line number.</param>
        /// <param name="column">The 1-based column number.</param>
        /// <param name="message">The finding message.</param>
        /// <param name="snippet">A snippet of the problematic node.</param>
        public Finding(string filePath, string tagName, int line, int column, string message, string snippet)
        {
            FilePath = filePath;
            TagName = tagName;
            Line = line;
            Column = column;
            Message = message;
            Snippet = snippet;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Line},{Column}] <{TagName}>: {Message} | {Snippet}";
        }
    }
}
