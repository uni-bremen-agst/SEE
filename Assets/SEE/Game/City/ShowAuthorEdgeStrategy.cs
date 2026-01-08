namespace SEE.Game.City
{
    /// <summary>
    /// This strategy determines when to to shows edges connecting authors and the file nodes they changed.
    /// </summary>
    public enum ShowAuthorEdgeStrategy : byte
    {
        /// <summary>
        /// Show edges all the time.
        /// </summary>
        ShowAlways,

        /// <summary>
        /// Show edges only when the user hovers over the author sphere or the node.
        /// </summary>
        ShowOnHoverOnly,

        /// <summary>
        /// Always show all edges connecting files with multiple authors (exceeding <see cref="BranchCity.AuthorThreshold"/>)
        /// and all other edges only if the user hovers over the author sphere or the node.
        /// </summary>
        ShowOnHoverOrWithMultipleAuthors,
    }
}
