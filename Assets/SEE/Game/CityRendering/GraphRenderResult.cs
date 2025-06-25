using System.Collections.Generic;
using SEE.Layout;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    public class GraphRenderResult
    {
        public ICollection<ILayoutNode> Nodes { get; set; }

        public ICollection<ILayoutEdge<ILayoutNode>> Edges { get; set; }
    }
}
