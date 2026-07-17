namespace XMLDocNormalizer.Configuration
{
    /// <summary>
    /// Specifies when missing value documentation is reported.
    /// </summary>
    internal enum ValueDocumentationMode
    {
        /// <summary>
        /// Does not report missing value documentation.
        /// </summary>
        None,

        /// <summary>
        /// Reports missing value documentation for all readable properties and indexers.
        /// </summary>
        AllReadableProperties,

        /// <summary>
        /// Reports missing value documentation for readable properties and indexers except DTO-like data containers.
        /// </summary>
        ExcludeDtoLikeTypes,

        /// <summary>
        /// Reports missing value documentation only for indexers.
        /// </summary>
        IndexersOnly
    }
}