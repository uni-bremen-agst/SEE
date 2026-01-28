using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Provides the smell mapping for a name-based documentation tag family (e.g. param or typeparam).
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="NamedTagSmellSet"/> class.
    /// </remarks>
    /// <param name="missingTag">Smell for a declared name without a corresponding tag.</param>
    /// <param name="emptyDescription">Smell for a tag that exists but has an empty description.</param>
    /// <param name="unknownTag">Smell for a tag referencing a name that does not exist.</param>
    /// <param name="duplicateTag">Smell for duplicate tags referencing the same name.</param>
    internal sealed class NamedTagSmellSet(
        XmlDocSmell missingTag,
        XmlDocSmell emptyDescription,
        XmlDocSmell unknownTag,
        XmlDocSmell duplicateTag)
    {

        /// <summary>
        /// Gets the smell for a declared name without a corresponding tag.
        /// </summary>
        public XmlDocSmell MissingTag { get; } = missingTag;

        /// <summary>
        /// Gets the smell for a tag that exists but has an empty description.
        /// </summary>
        public XmlDocSmell EmptyDescription { get; } = emptyDescription;

        /// <summary>
        /// Gets the smell for a tag referencing a name that does not exist.
        /// </summary>
        public XmlDocSmell UnknownTag { get; } = unknownTag;

        /// <summary>
        /// Gets the smell for duplicate tags referencing the same name.
        /// </summary>
        public XmlDocSmell DuplicateTag { get; } = duplicateTag;
    }
}
