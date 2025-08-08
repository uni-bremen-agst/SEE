namespace SEE.Game.City
{
    /// <summary>
    /// This strategy shows edges connecting authors and their commits.
    /// </summary>
    public enum ShowAuthorEdgeStrategy : byte
    {
        /// <summary>
        /// Show edges connecting authors and their commits all the time.
        /// </summary>
        ShowAllways,

        /// <summary>
        /// Show edges connecting author spheres and nodes only when the user hovers over the author sphere or the node.
        /// </summary>
        ShowOnHover,

        /// <summary>
        /// Show edges connecting author spheres and nodes only when the user hovers over the author sphere or the node.
        /// Edges of nodes which have multiple authors (exceeding <see cref="BranchCity.AuthorThreshold"/>) will always been shown.
        /// </summary>
        ShowOnHoverOrWithMultipleAuthors,
    }
}
