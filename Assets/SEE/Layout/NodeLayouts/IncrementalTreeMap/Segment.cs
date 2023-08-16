using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A segment represent the line that separates the rectangles of <see cref="Node"/>s of a layout.
    /// Actually the specific position is not relevant but the relation to adjacent nodes.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Segment
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="isConst"><see cref="IsConst"/></param>
        /// <param name="isVertical"><see cref="IsVertical"/></param>
        public Segment(bool isConst, bool isVertical)
        {
            this.IsConst = isConst;
            this.IsVertical = isVertical;
            this.Side1Nodes = new List<Node>();
            this.Side2Nodes = new List<Node>();
        }

        /// <summary>
        /// Is true if the segment is a border of the layout.
        /// Means that <see cref="Side1Nodes"/> or <see cref="Side2Nodes"/> are empty.
        /// A layout has i.g. 4 const segments.
        /// </summary>
        public bool IsConst { get; set; }
        
        /// <summary>
        /// Is true if the segment is separates in <see cref="Direction.Left"/> and <see cref="Direction.Right"/>.
        /// </summary>
        public bool IsVertical { get; set; }

        /// <summary>
        /// The adjacent nodes on the <see cref="Direction.Lower"/> or <see cref="Direction.Left"/>
        /// depending on <see cref="IsVertical"/>
        /// </summary>
        public IList<Node> Side1Nodes { get; }

        /// <summary>
        /// The adjacent nodes on the <see cref="Direction.Upper"/> or <see cref="Direction.Right"/>
        /// depending on <see cref="IsVertical"/>
        /// </summary>
        public IList<Node> Side2Nodes { get; }

        /// <summary>
        /// Method for easy overview in debugger
        /// </summary>
        private string DebuggerDisplay
        {
            get {
                string s = Side1Nodes.Aggregate("[", (current, node) => current + (node.ID + ","));
                s += "][";
                s = Side2Nodes.Aggregate(s, (current, node) => current + (node.ID + ","));
                s += "]";
                return s;
            }
        }
    }
}