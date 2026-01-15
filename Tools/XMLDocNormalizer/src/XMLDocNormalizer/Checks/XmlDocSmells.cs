namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Standardized XML documentation smells with unique IDs.
    /// Provides all messages used by XML doc checkers to report problems.
    /// </summary>
    internal static class XmlDocSmells
    {
        /// <summary>
        /// Unknown or misspelled XML documentation tag.
        /// </summary>
        public static readonly XmlDocSmell UnknownTag =
            new XmlDocSmell("W001", "Unknown XML documentation tag <{0}>.");

        /// <summary>
        /// Missing end tag (unclosed XML element).
        /// </summary>
        public static readonly XmlDocSmell MissingEndTag =
            new XmlDocSmell("E002", "Missing end tag (unclosed XML element).");

        /// <summary>
        /// <paramref> or <typeparamref> tag contains content and should be empty.
        /// </summary>
        public static readonly XmlDocSmell ParamRefNotEmpty =
            new XmlDocSmell("W003", "This tag should be an empty element, e.g. <paramref name=\"x\"/>.");

        /// <summary>
        /// <param> tag missing required 'name' attribute.
        /// </summary>
        public static readonly XmlDocSmell ParamMissingName =
            new XmlDocSmell("E004", "<param> tag is missing required 'name' attribute.");

        /// <summary>
        /// <exception> tag missing required 'cref' attribute.
        /// </summary>
        public static readonly XmlDocSmell ExceptionMissingCref =
            new XmlDocSmell("E005", "<exception> tag is missing required 'cref' attribute.");
    }
}
