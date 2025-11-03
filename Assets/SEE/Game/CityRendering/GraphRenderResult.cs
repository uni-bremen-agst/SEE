using System.Collections.Generic;
using SEE.Layout;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Represents the result of the rendering of a graph.
    /// </summary>
    public class GraphRenderResult
    {
        /// <summary>
        /// The nodes that were rendered with their layout.
        /// </summary>
        public ICollection<ILayoutNode> Nodes { get; set; } = new List<ILayoutNode>();

        /// <summary>
        /// The edges that were rendered.
        /// </summary>
        public ICollection<ILayoutEdge<ILayoutNode>> Edges { get; set; } = new List<ILayoutEdge<ILayoutNode>>();
    }
}
