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
        public bool IsConst {get => isConst;}
        private IList<TNode> side1Nodes;
        public IList<TNode> Side1Nodes{get => side1Nodes;}
        private IList<TNode> side2Nodes;
        public IList<TNode> Side2Nodes{get => side2Nodes;}
        private bool isVertical;
        public bool IsVertical {get => isVertical;}

    }
}