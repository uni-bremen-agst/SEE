namespace SEE.Game.City
{
    /// <summary>
    /// The kinds of node and edge animations available for appearing
    /// or disappearing.
    /// </summary>
    public enum GraphElementAnimationKind : byte
    {
        /// <summary>
        /// Edges fade into existence from full transparency
        /// to full opacity.
        /// </summary>
        Fading,
        /// <summary>
        /// Edges are continually built from subsplines
        /// starting at the source node and ending at the target
        /// node.
        /// </summary>
        Buildup,
        /// <summary>
        /// No animation supplied.
        ///</summary>
        None
    }
}
