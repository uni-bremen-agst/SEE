namespace SEE.Game.City
{
    /// <summary>
    /// This strategy determines when to show graph edges.
    /// </summary>
    public enum ShowEdgeStrategy : byte
    {
        /// <summary>
        /// Do not show any edge.
        /// </summary>
        Never,

        /// <summary>
        /// Show edges all the time.
        /// </summary>
        Always,

        /// <summary>
        /// Show edges only when the user hovers over the author sphere or the node.
        /// </summary>
        OnHoverOnly,
    }
}
