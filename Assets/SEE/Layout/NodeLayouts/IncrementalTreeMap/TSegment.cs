using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
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
        public bool IsConst {get;}
        private IList<TNode> side1Nodes;
        public IList<TNode> Side1Nodes{get;}
        private IList<TNode> side2Nodes;
        public IList<TNode> Side2Nodes{get;}
        private bool isVertical;
        public bool IsVertical {get;}

    }
}