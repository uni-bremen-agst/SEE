using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Abstract class for the layout of graph nodes for the EvoStreets layout.
    /// </summary>
    internal abstract class ENode
    {
        /// <summary>
        /// Constructor. Sets <see cref="GraphNode"/> to <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The graph node represented by this <see cref="ENode"/>.</param>
        public ENode(ILayoutNode node)
        {
            GraphNode = node;
        }

        /// <summary>
        /// The world-space rectangle this node occupies. In case of a leaf, it is simply
        /// the area needed for the <see cref="GraphNode"/>. In case <see cref="GraphNode"/>
        /// is an inner node, the rectangle covers the space of the street representing
        /// the <see cref="GraphNode"/> and all its descendants.
        /// Note that the rectangle and the street for inner nodes are two different
        /// things. The center position of the street is generally different from the
        /// center of the rectangle because one side of the street occupies more space
        /// than the other side.
        /// </summary>
        public Rectangle Rectangle;

        /// <summary>
        /// Calculates and sets the necessary size of <see cref="Rectangle"/> for this node.
        /// For inner nodes, the descendants will be ordered along the street on both sides.
        /// </summary>
        /// <param name="orientation">The orientation of this node in world space.</param>
        /// <param name="treeDescriptor">Parameters regarding the layout.</param>
        /// <param name="lastLayout">The layout computed in the previous layouting.</param>
        /// <param name="newNodes">The new nodes to be placed at the end of the street.</param>
        /// <param name="existingNodes">Nodes that existed already in the previous layout. They
        /// will be placed in their original order.</param>
        public abstract void SetSizeAndDistribute
            (Orientation orientation,
            LayoutDescriptor treeDescriptor,
            Dictionary<ILayoutNode, NodeTransform> lastLayout,
            ILayoutNodeSet newNodes,
            ILayoutNodeSet existingNodes);

        /// <summary>
        /// The distance from the starting point of the street containing this node to the
        /// node's <see cref="Rectangle.Center"/>. The starting point of a street oriented
        /// toward East is its left corner. The starting point of a street oriented
        /// towards North, is its lower corner.
        ///
        /// Note: This value will be computed assuming only the orientation towards
        /// <see cref="Orientation.East"/> or <see cref="Orientation.North"/>
        /// by <see cref="SetSizeAndDistribute(Orientation, LayoutDescriptor, Dictionary{ILayoutNode, NodeTransform}, ILayoutNodeSet, ILayoutNodeSet)"/>
        /// and, hence, is always positive.
        /// </summary>
        internal float DistanceFromOrigin;

        /// <summary>
        /// Sets <see cref="DistanceFromOrigin"/> as the sum of <paramref name="currentDistanceFromOrigin"/>
        /// and the length (extent, really, i.e., half of <see cref="Length(Orientation)"/>) of this node
        /// along the given <paramref name="orientation"/>.
        /// Returns <paramref name="currentDistanceFromOrigin"/> plus the length of the <see cref="Rectangle"/>
        /// with respect to <paramref name="orientation"/>.
        /// </summary>
        /// <param name="currentDistanceFromOrigin">The current distance from the origin.</param>
        /// <param name="orientation">The orientation of the street currently handled.</param>
        /// <returns>The updated <paramref name="currentDistanceFromOrigin"/>.</returns>
        internal float SetDistanceFromOrigin(float currentDistanceFromOrigin, Orientation orientation)
        {
            float extent = Length(orientation) / 2.0f;
            DistanceFromOrigin = currentDistanceFromOrigin + extent;
            return DistanceFromOrigin + extent;
        }

        /// <summary>
        /// Returns the length of the enclosing rectangle. If <paramref name="orientation"/>
        /// is <see cref="Orientation.East"/> or <see cref="Orientation.West"/>, the length
        /// is <see cref="Size.Width"/> otherwise <see cref="Size.Depth"/>.
        /// </summary>
        /// <param name="orientation">Specifies which edge of the enclosing rectangle is meant as length.</param>
        /// <returns>The length of the enclosing rectangle along the given <paramref name="orientation"/>.</returns>
        public float Length(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East => Rectangle.Width,
                Orientation.West => Rectangle.Width,
                Orientation.North => Rectangle.Depth,
                Orientation.South => Rectangle.Depth,
                _ => throw new NotImplementedException($"Unhandled case {orientation}.")
            };
        }

        /// <summary>
        /// The node in the original graph this ENode is representing.
        /// </summary>
        protected readonly ILayoutNode GraphNode;

        /// <summary>
        /// The depth of this node in the hierarchy. A root has depth 0. This
        /// value will be used to determine the width of a street.
        /// </summary>
        public int TreeDepth;

        /// <summary>
        /// True if this node is left from a street. Otherwise it is assumed to be right.
        /// This is an absolute value in world space. Left of a street directed toward North or South
        /// is always West. Left of a street directed towards East or West is always North.
        /// </summary>
        public bool Left;

        /// <summary>
        /// Adds the layout information of this <see cref="ENode"/> to the <paramref name="layoutResult"/>.
        /// </summary>
        /// <param name="layoutResult">Layout where to add the layout information.</param>
        /// <param name="streetHeight">The height of an inner node (depicted as street).</param>
        public abstract void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layoutResult, float streetHeight);

        /// <summary>
        /// Prints this node with an indentation proportional to its <see cref="TreeDepth"/>. Can be used for debugging.
        /// </summary>
        public virtual void Print()
        {
            Debug.Log(string.Concat(Enumerable.Repeat("-", TreeDepth)) + ToString() + "\n");
        }

        /// <summary>
        /// This node as a human-readable string.
        /// </summary>
        /// <returns>Node as a human-readable string.</returns>
        public override string ToString()
        {
            return $"ENode[ID={GraphNode.ID}, Depth={TreeDepth}, IsLeft={Left}, Rectangle={Rectangle}, distanceFromOrigin={DistanceFromOrigin:F4}]";
        }

        /// <summary>
        /// Sets the <see cref="Rectangle.Center"/> of this node to <paramref name="centerLocation"/> based
        /// on <paramref name="orientation"/>. For a more precise description, see the overrides of this
        /// method in the subclasses.
        /// </summary>
        /// <param name="orientation">The orientation of this node.</param>
        /// <param name="centerLocation">Center location to be set.</param>
        public abstract void SetLocation(Orientation orientation, Location centerLocation);

        /// <summary>
        /// Returns the new orientation based on <paramref name="orientation"/> and whether
        /// this node is left or right from a street.
        /// </summary>
        /// <param name="orientation">The current orientation of this node before the rotation.</param>
        /// <returns>Absolute new orientation after the rotation in world space.</returns>
        internal Orientation Rotate(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East or Orientation.West => Left ? Orientation.North : Orientation.South,
                Orientation.North or Orientation.South => Left ? Orientation.West : Orientation.East,
                _ => throw new NotImplementedException($"Unhandled case {orientation}."),
            };
        }

        /// <summary>
        /// True if there is a layout node in <paramref name="newNodes"/> with
        /// the id of <see cref="GraphNode"/>.
        /// </summary>
        /// <param name="newNodes">Where to look up the id.</param>
        /// <returns>True if the id of <see cref="GraphNode"/> can be found in
        /// <paramref name="newNodes"/>.</returns>
        internal bool ContainedIn(ILayoutNodeSet newNodes)
        {
            return newNodes.Contains(GraphNode);
        }

        /// <summary>
       /// Returns the ID of <see cref="GraphNode"/>.
        /// </summary>
        /// <returns>Id of <see cref="GraphNode"/>.</returns>
        internal string Name => GraphNode.ID;
    }
}
