namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// A factory returning instances of subclasses of <see cref="ENode"/>.
    /// </summary>
    internal static class ENodeFactory
    {
        /// <summary>
        /// Returns a representation of <paramref name="node"/> for the EvoStreet layout.
        /// If <paramref name="node"/> is a leaf, an instance of <see cref="ELeaf"/> will
        /// be returned, otherwise an instance of <see cref="EInner"/>.
        /// </summary>
        /// <param name="node">Graph node to be laid out in an EvoStreets layout.</param>
        /// <returns>Representation of <paramref name="node"/> for the EvoStreets layout.</returns>
        public static ENode Create(ILayoutNode node)
        {
            return node.IsLeaf ? new ELeaf(node) : new EInner(node);
        }
    }
}
