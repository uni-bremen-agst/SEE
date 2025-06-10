using System.Collections.Generic;
using SEE.Layout;

namespace SEE.Net
{
    public class SeeCitySnapshot
    {
        public IEnumerable<ILayoutNode> Nodes { get; set; }

        public IEnumerable<ILayoutEdge<ILayoutNode>> Edges { get; set; }
    }
}
