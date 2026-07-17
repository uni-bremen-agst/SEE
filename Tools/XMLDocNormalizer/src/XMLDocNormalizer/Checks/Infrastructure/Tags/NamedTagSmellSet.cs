using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Tags
{
    /// <summary>
    /// Provides the smell mapping for a name-based documentation tag family.
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
        /// <value>
        /// The missing-tag smell for the analyzed tag family.
        /// </value>
        public XmlDocSmell MissingTag { get; } = missingTag;

        /// <summary>
        /// Gets the smell for a tag that exists but has an empty description.
        /// </summary>
        /// <value>
        /// The empty-description smell for the analyzed tag family.
        /// </value>
        public XmlDocSmell EmptyDescription { get; } = emptyDescription;

        /// <summary>
        /// Gets the smell for a tag referencing a name that does not exist.
        /// </summary>
        /// <value>
        /// The unknown-tag smell for the analyzed tag family.
        /// </value>
        public XmlDocSmell UnknownTag { get; } = unknownTag;

        /// <summary>
        /// Gets the smell for duplicate tags referencing the same name.
        /// </summary>
        /// <value>
        /// The duplicate-tag smell for the analyzed tag family.
        /// </value>
        public XmlDocSmell DuplicateTag { get; } = duplicateTag;
    }
}
