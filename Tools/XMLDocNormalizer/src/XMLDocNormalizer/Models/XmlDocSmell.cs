namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents a single XML documentation smell with reporting metadata.
    /// </summary>
    internal sealed class XmlDocSmell
    {
        /// <summary>
        /// Gets the stable smell ID.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Gets the message template used for concrete finding messages.
        /// </summary>
        public string MessageTemplate { get; }

        /// <summary>
        /// Gets the short placeholder-free rule title used by report formats such as SARIF.
        /// </summary>
        public string RuleTitle { get; }

        /// <summary>
        /// Gets the placeholder-free rule description used by report formats such as SARIF.
        /// </summary>
        public string RuleDescription { get; }

        /// <summary>
        /// Gets the default severity of the smell.
        /// </summary>
        public Severity Severity { get; }

        /// <summary>
        /// Initializes a new instance of the XmlDocSmell class.
        /// </summary>
        /// <param name="id">The smell id.</param>
        /// <param name="messageTemplate">The concrete finding message template.</param>
        /// <param name="severity">The smell severity.</param>
        /// <param name="ruleTitle">The placeholder-free rule title.</param>
        /// <param name="ruleDescription">The placeholder-free rule description.</param>
        public XmlDocSmell(
            string id,
            string messageTemplate,
            Severity severity,
            string ruleTitle,
            string ruleDescription)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Smell ID must not be null or whitespace.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(messageTemplate))
            {
                throw new ArgumentException("Message template must not be null or whitespace.", nameof(messageTemplate));
            }

            if (string.IsNullOrWhiteSpace(ruleTitle))
            {
                throw new ArgumentException("Rule title must not be null or whitespace.", nameof(ruleTitle));
            }

            if (string.IsNullOrWhiteSpace(ruleDescription))
            {
                throw new ArgumentException("Rule description must not be null or whitespace.", nameof(ruleDescription));
            }

            ID = id;
            MessageTemplate = messageTemplate;
            Severity = severity;
            RuleTitle = ruleTitle;
            RuleDescription = ruleDescription;
        }

        /// <summary>
        /// Formats the message template using the specified arguments.
        /// </summary>
        /// <param name="args">Optional formatting arguments.</param>
        /// <returns>The formatted message.</returns>
        public string FormatMessage(params object[] args)
        {
            if (args is { Length: > 0 })
            {
                return string.Format(MessageTemplate, args);
            }

            return MessageTemplate;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ID} ({Severity})";
        }
    }
}
