namespace SEE.Game.City
{
    /// <summary>
    /// This strategy determines when to show edges connecting authors and the
    /// file nodes they changed.
    /// </summary>
    public enum ShowAuthorEdgeStrategy : byte
    {
        /// <summary>
        /// Show edges all the time.
        /// </summary>
        Always,

        /// <summary>
        /// Show edges only when the user hovers over the author sphere or the node.
        /// </summary>
        OnHoverOnly,

        /// <summary>
        /// Always show all edges connecting files with multiple authors
        /// (exceeding <see cref="BranchCity.AuthorThreshold"/>) and all other edges
        /// only if the user hovers over the author sphere or the node.
        /// </summary>
        OnHoverOrWithMultipleAuthors,
    }
}
