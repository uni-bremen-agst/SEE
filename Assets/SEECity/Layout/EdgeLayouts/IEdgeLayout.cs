using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Shared abstract super class of all edge layouts.
    /// </summary>
    public abstract class IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="edgesAboveBlocks">if true, edges are drawn above nodes, otherwise below</param>
        public IEdgeLayout(bool edgesAboveBlocks)
        {
            this.edgesAboveBlocks = edgesAboveBlocks;
        }

        /// <summary>
        /// The unique name of a layout.
        /// </summary>
        public string Name
        {
            get => name;
        }

        /// <summary>
        /// Returns the layout edges for all edges connecting nodes in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">the nodes whose connecting edges are to be laid out</param>
        /// <returns>layout edges</returns>
        public abstract ICollection<LayoutEdge> Create(ICollection<ILayoutNode> layoutNodes);

        /// <summary>
        /// Name of the layout.
        /// </summary>
        protected string name = "";

        /// <summary>
        /// Orientation of the edges; 
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses;
        /// </summary>
        protected readonly bool edgesAboveBlocks;

        /// <summary>
        /// Yields the greatest and smallest y co-ordinate and the maximal height of all <paramref name="nodes"/> given.
        /// 
        /// Precondition: <paramref name="nodes"/> is not empty.
        /// </summary>
        /// <param name="nodes">list of nodes whose greatest and smallest y co-ordinate is required</param>
        /// <param name="minY">smallest y co-ordinate</param>
        /// <param name="maxY">highest x co-ordinate</param>
        /// <param name="maxHeight">maximal height of nodes</param>
        protected void MinMaxBlockY(ICollection<ILayoutNode> nodes, out float minY, out float maxY, out float maxHeight)
        {
            maxY = Mathf.NegativeInfinity;
            minY = Mathf.Infinity;
            maxHeight = 0.0f;
            foreach (ILayoutNode node in nodes)
            {
                float y = node.Roof.y;
                if (y > maxY)
                {
                    maxY = y;
                }
                else if (y < minY)
                {
                    minY = y;
                }
                float h = node.Scale.y;
                if (h > maxHeight)
                {
                    maxHeight = h;
                }
            }
        }
    }
}
