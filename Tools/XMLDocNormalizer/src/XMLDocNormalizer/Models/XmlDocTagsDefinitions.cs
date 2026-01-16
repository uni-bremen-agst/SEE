namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Central registry of known XML documentation tags.
    /// </summary>
    internal static class XmlDocTagDefinitions
    {
        /// <summary>
        /// All XML documentation tags recognized by the normalizer.
        /// </summary>
        public static readonly IReadOnlySet<string> KnownTags =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "summary",
                "remarks",
                "param",
                "typeparam",
                "returns",
                "value",
                "exception",
                "example",
                "para",
                "list",
                "item",
                "term",
                "description",
                "code",
                "see",
                "seealso",
                "inheritdoc",
                "paramref",
                "typeparamref"
            };

        /// <summary>
        /// Tags that require a matching end tag (container elements).
        /// Empty elements such as <paramref/> or <typeparamref/> must NOT be part of this set,
        /// otherwise they would be incorrectly reported as "missing end tag".
        /// </summary>
        public static readonly HashSet<string> ContainerTags =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "summary",
                "remarks",
                "param",
                "typeparam",
                "returns",
                "value",
                "exception",
                "example",
                "para",
                "list",
                "item",
                "term",
                "description",
                "code"
            };
    }
}
