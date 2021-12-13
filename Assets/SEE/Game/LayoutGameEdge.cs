using SEE.DataModel.DG;
using SEE.Layout;

namespace SEE.Game
{
    public class LayoutGameEdge : LayoutEdge
    {
        public LayoutGameNode SourceGameNode;
        
        public LayoutGameNode TargetGameNode;
        
        public LayoutGameEdge(LayoutGameNode source, LayoutGameNode target, Edge edge) : base(source, target, edge)
        {
            SourceGameNode = source;
            TargetGameNode = target;
        }
    }
}