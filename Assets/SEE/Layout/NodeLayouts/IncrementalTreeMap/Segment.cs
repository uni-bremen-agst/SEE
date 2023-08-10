using System.Collections.Generic;
using System.Diagnostics;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Segment
    {
        public Segment(bool isConst, bool isVertical)
        {
            this.IsConst = isConst;
            this.IsVertical = isVertical;
            this.side1Nodes = new List<Node>();
            this.side2Nodes = new List<Node>();
        }

        public bool IsConst { get; set; }
        public bool IsVertical { get; set; }

        private IList<Node> side1Nodes;
        public IList<Node> Side1Nodes => side1Nodes;
        private IList<Node> side2Nodes;
        public IList<Node> Side2Nodes => side2Nodes;

        private string DebuggerDisplay
        {
            get {
                string s = "[";
                foreach (var node in side1Nodes)
                {
                    s += node.ID + ",";
                }
                s += "][";
                foreach (var node in side2Nodes)
                {
                    s += node.ID + ",";
                }
                s += "]";
                return s;
            }
        }
    }
}