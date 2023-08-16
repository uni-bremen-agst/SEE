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
            this.Side1Nodes = new List<Node>();
            this.Side2Nodes = new List<Node>();
        }

        public bool IsConst { get; set; }
        public bool IsVertical { get; set; }

        public IList<Node> Side1Nodes { get; }

        public IList<Node> Side2Nodes { get; }

        private string DebuggerDisplay
        {
            get {
                string s = "[";
                foreach (var node in Side1Nodes)
                {
                    s += node.ID + ",";
                }
                s += "][";
                foreach (var node in Side2Nodes)
                {
                    s += node.ID + ",";
                }
                s += "]";
                return s;
            }
        }
    }
}