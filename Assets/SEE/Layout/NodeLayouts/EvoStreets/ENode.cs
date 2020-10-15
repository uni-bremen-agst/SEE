using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Necessary layout data on graph nodes 
    /// </summary>
    [System.Serializable]
    public class ENode
    {
        public ENode() { }

        public ENode(ILayoutNode node)
        {
            GraphNode = node;
        }

        /// <summary>
        /// The scaling of the node (width, height, depth).
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The position of the node in the world.
        /// </summary>
        public Vector3 Location;

        /// <summary>
        /// The node in the original graph this ENode is representing.
        /// </summary>
        public ILayoutNode GraphNode;

        /// <summary>
        /// The pivot world co-ordinate along the x axis.
        /// </summary>
        public float XPivot;

        /// <summary>
        /// The pivot world co-ordinate along the z axis.
        /// </summary>
        public float ZPivot;

        /// <summary>
        /// The rotation of the node within the x/y plane (ground) in degrees.
        /// </summary>
        public float Rotation;

        /// <summary>
        /// The depth of this node in the hierarchy. A root has depth 0. This
        /// value will be used to determine the width of a street.
        /// </summary>
        public int Depth;

        /// <summary>
        /// True if this node is left from a street.
        /// </summary>
        public bool Left;

        /// <summary>
        /// The parent of this ENode in the hierarchy. A root has parent null.
        /// </summary>
        public ENode ParentNode;

        /// <summary>
        /// The children of this ENode in the hierarchy.
        /// </summary>
        public List<ENode> Children = new List<ENode>();

        /// <summary>
        /// True if this node has no children in the original graph. Leaves are
        /// represented as houses in EvoStreets. A node is either a house or
        /// a street.
        /// </summary>
        /// <returns>Children.Count == 0</returns>
        public bool IsHouse()
        {
            return GraphNode.IsLeaf;
        }

        /// <summary>
        /// True if this node has at least one child in the original graph.  Inner nodes are
        /// represented as streets in EvoStreets. A node is either a house or a street.
        /// </summary>
        /// <returns>Children.Count > 0</returns>
        public bool IsStreet()
        {
            return !GraphNode.IsLeaf;
        }

        /// <summary>
        /// The largest z co-ordinate of the scale of all children of this node.
        /// </summary>
        public float MaxChildZ
        {
            get
            {
                float max = 0;
                foreach (ENode child in Children)
                {
                    if (child.Scale.z > max)
                    {
                        max = child.Scale.z;
                    }
                }
                return max;
            }
        }

        public override string ToString()
        {
            return $"Node[ID={GraphNode.ID}]";
        }
    }
}
