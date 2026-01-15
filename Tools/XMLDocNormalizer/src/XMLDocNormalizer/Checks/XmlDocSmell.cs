namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Represents a single XML documentation "smell" (finding) with a unique ID and message.
    /// </summary>
    internal readonly struct XmlDocSmell
    {
        /// <summary>
        /// The unique identifier of the smell, e.g., W001, E002.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The message describing the finding. May contain placeholders for formatting.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Creates a new XML documentation smell.
        /// </summary>
        /// <param name="id">Unique ID of the smell.</param>
        /// <param name="message">Message text describing the smell.</param>
        public XmlDocSmell(string id, string message)
        {
            Id = id;
            Message = message;
        }

        /// <summary>
        /// Formats the message using optional arguments.
        /// </summary>
        /// <param name="args">Arguments for string formatting.</param>
        /// <returns>Formatted message string.</returns>
        public string Format(params object[] args) => string.Format(Message, args);
    }
}
