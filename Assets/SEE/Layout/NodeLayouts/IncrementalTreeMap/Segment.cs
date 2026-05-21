using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A segment is a core element in the layout algorithm.
    /// Segments are representing lines separating/slicing the layout in rectangles.
    /// The specific position of a segment in the layout is actually not relevant,
    /// but the relation to adjacent nodes is relevant.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class Segment
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isConst"><see cref="IsConst"/>.</param>
        /// <param name="isVertical"><see cref="IsVertical"/>.</param>
        public Segment(bool isConst, bool isVertical)
        {
            this.IsConst = isConst;
            this.IsVertical = isVertical;
        }

        /// <summary>
        /// Is true if the segment is a border of the layout.
        /// In most cases that means that <see cref="Side1Nodes"/> or <see cref="Side2Nodes"/> are empty
        /// and layout has e.g. four const segments.
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Is true if the segment separates the plane in <see cref="Direction.Left"/> and <see cref="Direction.Right"/>.
        /// </summary>
        public bool IsVertical { get; set; }

        /// <summary>
        /// The adjacent nodes on the <see cref="Direction.Lower"/> or <see cref="Direction.Left"/>
        /// depending on <see cref="IsVertical"/>
        /// </summary>
        public IList<Node> Side1Nodes { get; } = new List<Node>();

        /// <summary>
        /// The adjacent nodes on the <see cref="Direction.Upper"/> or <see cref="Direction.Right"/>
        /// depending on <see cref="IsVertical"/>
        /// </summary>
        public IList<Node> Side2Nodes { get; } = new List<Node>();

        /// <summary>
        /// Method for better overview in debugger.
        /// </summary>
        private string DebuggerDisplay
        {
            get
            {
                string s = Side1Nodes.Aggregate("[", (current, node) => current + (node.ID + ","));
                s += "][";
                s = Side2Nodes.Aggregate(s, (current, node) => current + (node.ID + ","));
                s += "]";
                return s;
            }
        }
    }
}
