namespace XMLDocNormalizer.Checks.Infrastructure.Value
{
    /// <summary>
    /// Classifies how a property behaves with respect to value documentation.
    /// </summary>
    internal enum PropertyValueKind
    {
        /// <summary>
        /// Indicates that the property can be read.
        /// </summary>
        Readable,

        /// <summary>
        /// Indicates that the property can only be written.
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Indicates that the member is not a supported property value target.
        /// </summary>
        Other
    }
}