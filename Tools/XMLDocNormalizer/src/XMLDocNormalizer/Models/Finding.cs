namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents a single tool finding with location, tag, smell, message, snippet, and source context information.
    /// </summary>
    internal sealed class Finding
    {
        /// <summary>
        /// Gets the smell rule that produced this finding.
        /// </summary>
        /// <value>
        /// The smell rule that produced this finding.
        /// </value>
        public XmlDocSmell Smell { get; }

        /// <summary>
        /// Gets the source file path associated with this finding.
        /// </summary>
        /// <value>
        /// The source file path associated with this finding.
        /// </value>
        public string FilePath { get; }

        /// <summary>
        /// Gets the XML documentation tag name associated with this finding.
        /// </summary>
        /// <value>
        /// The XML documentation tag name associated with this finding.
        /// </value>
        /// <remarks>
        /// Examples are summary, param, returns, exception, or documentation.
        /// </remarks>
        public string TagName { get; }

        /// <summary>
        /// Gets the one-based line number of this finding.
        /// </summary>
        /// <value>
        /// The one-based line number of this finding.
        /// </value>
        public int Line { get; }

        /// <summary>
        /// Gets the one-based column number of this finding.
        /// </summary>
        /// <value>
        /// The one-based column number of this finding.
        /// </value>
        public int Column { get; }

        /// <summary>
        /// Gets the human-readable finding message.
        /// </summary>
        /// <value>
        /// The human-readable finding message.
        /// </value>
        public string Message { get; }

        /// <summary>
        /// Gets a short source snippet of the problematic node.
        /// </summary>
        /// <value>
        /// A short source snippet of the problematic node.
        /// </value>
        public string Snippet { get; }

        /// <summary>
        /// Gets study-oriented metadata that describes the declaration and documentation subject affected by this finding.
        /// </summary>
        /// <value>
        /// The study-oriented metadata associated with this finding.
        /// </value>
        public FindingContext Context { get; }

        /// <summary>
        /// Initializes a new instance of the Finding class.
        /// </summary>
        /// <param name="smell">The smell rule that produced the finding.</param>
        /// <param name="filePath">The source file path.</param>
        /// <param name="tagName">The XML documentation tag name.</param>
        /// <param name="line">The one-based line number.</param>
        /// <param name="column">The one-based column number.</param>
        /// <param name="snippet">A short source snippet of the problematic node.</param>
        /// <param name="context">The source declaration context for study-oriented reporting.</param>
        /// <param name="messageArgs">Optional formatting arguments for the smell message template.</param>
        /// <exception cref="ArgumentNullException">Thrown when smell is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath or tagName is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when line or column is smaller than one.</exception>
        public Finding(
            XmlDocSmell smell,
            string filePath,
            string tagName,
            int line,
            int column,
            string snippet,
            FindingContext? context = null,
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
            Context = context ?? FindingContext.Unknown;

            Message = smell.FormatMessage(messageArgs ?? Array.Empty<object>());
        }

        /// <summary>
        /// Returns a stable, human-readable representation of this finding for console output and logs.
        /// </summary>
        /// <returns>
        /// A formatted string containing smell id, severity, location, tag name, and message.
        /// If a snippet is present, it is appended after a separator.
        /// </returns>
        public override string ToString()
        {
            string header = $"[{Smell.ID}|{Smell.Severity}] [{Line},{Column}] <{TagName}>: {Message}";

            if (string.IsNullOrWhiteSpace(Snippet))
            {
                return header;
            }

            return header + " | " + Snippet;
        }
    }
}
