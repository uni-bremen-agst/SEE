namespace SEE.Game.City
{
    /// <summary>
    /// The kinds of edge animations available.
    /// </summary>
    public enum EdgeAnimationKind : byte
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
        /// No animation supplied. Will throw a
        /// <exception cref="ArgumentOutOfRangeException"> when used.
        ///</summary>
        None
    }
}
