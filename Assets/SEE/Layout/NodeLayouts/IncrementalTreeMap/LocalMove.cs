using System.Collections.Generic;
using System.Linq;
using System;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    public enum MoveKind
    {Flip, Stretch} 

    abstract public class LocalMove
    {
        protected TNode node1;
        protected TNode node2;

        abstract public void apply();
        // 
    }

    public class FlipMove : LocalMove
    {
        bool clockwise;
        public FlipMove(TNode node1, TNode node2, bool clockwise)
        {
            this.node1 = node1; this.node2 = node2; this.clockwise = clockwise;
        }
        
        override 
        public void apply()
        {

        }

    }

    public class StretchMove : LocalMove
    {

        public StretchMove(TNode node1, TNode node2)
        {
            this.node1 = node1; this.node2 = node2;
        }

        override 
        public void apply()
        {

        }
    }
}