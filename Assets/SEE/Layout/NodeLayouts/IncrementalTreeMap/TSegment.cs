using System.Collections.Generic;
using System.Diagnostics;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TSegment
    {
        public TSegment(bool isConst, bool isVertical)
        {
            this.isConst = isConst;
            this.isVertical = isVertical;
            this.side1Nodes = new List<TNode>();
            this.side2Nodes = new List<TNode>();
        }

        private bool isConst;
        public bool IsConst {get => isConst; set {this.isConst = value;}}
        private IList<TNode> side1Nodes;
        public IList<TNode> Side1Nodes{get => side1Nodes;}
        private IList<TNode> side2Nodes;
        public IList<TNode> Side2Nodes{get => side2Nodes;}
        private bool isVertical;
        public bool IsVertical {get => isVertical; set {this.isVertical = value;}}

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