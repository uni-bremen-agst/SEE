namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents a single XML documentation smell (rule) identified by an id, message template, and severity.
    /// </summary>
    internal sealed class XmlDocSmell
    {
        /// <summary>
        /// Gets the stable smell id (e.g., "DOC200").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the message template (can contain placeholders like "{0}").
        /// </summary>
        public string MessageTemplate { get; }

        /// <summary>
        /// Gets the default severity of the smell.
        /// </summary>
        public Severity Severity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDocSmell"/> class.
        /// </summary>
        /// <param name="id">The smell id.</param>
        /// <param name="messageTemplate">The message template.</param>
        /// <param name="severity">The smell severity.</param>
        public XmlDocSmell(string id, string messageTemplate, Severity severity)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Smell id must not be null or whitespace.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(messageTemplate))
            {
                throw new ArgumentException("Message template must not be null or whitespace.", nameof(messageTemplate));
            }

            Id = id;
            MessageTemplate = messageTemplate;
            Severity = severity;
        }

        /// <summary>
        /// Formats the message template using <see cref="string.Format(string, object[])"/>.
        /// </summary>
        /// <param name="args">Optional formatting arguments.</param>
        /// <returns>The formatted message.</returns>
        public string FormatMessage(params object[] args)
        {
            return args is { Length: > 0 }
                ? string.Format(MessageTemplate, args)
                : MessageTemplate;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Id} ({Severity})";
        }
    }
}