namespace XMLDocNormalizer.Checks
{
    /// <summary>
    /// Standardized messages ("smells") for XML documentation findings.
    /// This class provides all text messages used by XML doc checkers to report problems.
    /// </summary>
    internal static class XmlDocSmells
    {
        /// <summary>
        /// Returns a message indicating that an unknown or misspelled XML documentation tag was found.
        /// </summary>
        /// <param name="tagName">The tag name that is unknown or misspelled.</param>
        /// <returns>A formatted message for the unknown tag.</returns>
        public static string UnknownTag(string tagName) => $"Unknown XML documentation tag <{tagName}>.";

        /// <summary>
        /// Message indicating that an XML element is missing its end tag.
        /// </summary>
        public const string MissingEndTag = "Missing end tag (unclosed XML element).";

        /// <summary>
        /// Message indicating that a <paramref> or <typeparamref> tag contains content and should be empty.
        /// </summary>
        public const string ParamRefNotEmpty = "This tag should be an empty element, e.g. <paramref name=\"x\"/>.";

        /// <summary>
        /// Message indicating that a <param> tag is missing its required 'name' attribute.
        /// </summary>
        public const string ParamMissingName = "<param> tag is missing required 'name' attribute.";

        /// <summary>
        /// Message indicating that an <exception> tag is missing its required 'cref' attribute.
        /// </summary>
        public const string ExceptionMissingCref = "<exception> tag is missing required 'cref' attribute.";
    }
}
