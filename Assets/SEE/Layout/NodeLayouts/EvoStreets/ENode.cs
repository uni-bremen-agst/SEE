using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    internal struct TreeDescriptor
    {
        public float MaximalDepth;
        public float StreetWidth;
    }

    internal enum Orientation
    {
        North,
        East,
        South,
        West
    }

    /// <summary>
    /// Absolute size of a rectangle.
    /// </summary>
    internal struct Scale
    {
        /// <summary>
        /// The width in world space, i.e., along the x axis.
        /// </summary>
        public float Width;

        /// <summary>
        /// The depth in world space, i.e., along the z axis.
        /// </summary>
        public float Depth;

        public override string ToString()
        {
            return $"[width={Width.ToString("F4")}, depth={Depth.ToString("F4")}]";
        }
    }

    internal struct Location
    {
        public float X;

        public float Y;

        public Location(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"[x={X.ToString("F4")}, y={Y.ToString("F4")}]";
        }
    }

    /// <summary>
    /// Necessary layout data on graph nodes for the EvoStreets layout.
    /// </summary>
    internal abstract class ENode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">the graph node represented by this <see cref="ENode"/></param>
        public ENode(ILayoutNode node)
        {
            GraphNode = node;
        }

        /// <summary>
        /// The distance between two neighboring node representations.
        /// </summary>
        protected const float offsetBetweenBuildings = 0.05f;

        /// <summary>
        /// The size of the enclosing rectangle of this node in world space.
        /// </summary>
        public Scale Size;

        /// <summary>
        /// Calculates and sets the necessary <see cref="Size"/> of this node. Sets the
        /// <paramref name="orientation"/>.
        /// </summary>
        /// <param name="orientation">the orientation of this node</param>
        /// <param name="treeDescriptor">parameters regarding the layout of the tree</param>
        public abstract void SetSize(Orientation orientation, TreeDescriptor treeDescriptor);

        /// <summary>
        /// The distance from the origin of the street (left-most corner of the street axis) to
        /// the center of the outermost rectangle enclosing this node (the hull). If the node is a leaf, this
        /// outermost rectangle is identical to the ground area of the node, that is, the rectangle
        /// defined by <see cref="Center"/> and <see cref="Size"/>. If the node is an inner node
        /// instead, the size of the outermost rectangle is again <see cref="Size"/>, but its center
        /// is not equivalent to <see cref="Center"/>. <see cref="Center"/> for inner nodes relates
        /// to the center of the street representing the inner node, but there may be leaves left
        /// and right (or above or below, respectively) from this street. The center of the outermost
        /// rectangle (the hull) is the <see cref="Center"/> plus the maximal width of its left children.
        /// </summary>
        internal float distanceFromOrigin;

        /// <summary>
        /// Sets <see cref="distanceFromOrigin"/> as the sum of <paramref name="currentDistanceFromOrigin"/>
        /// and the length of this node along the given <paramref name="orientation"/>.
        /// </summary>
        /// <param name="currentDistanceFromOrigin">the current distance from the origin</param>
        /// <param name="orientation">the orientation of the street currently handled.</param>
        /// <returns></returns>
        internal abstract float SetDistanceFromOrigin(float currentDistanceFromOrigin, Orientation orientation);

        /// <summary>
        /// Returns the length of the enclosing rectangle. If <paramref name="orientation"/>
        /// is <see cref="Orientation.East"/> or <see cref="Orientation.West"/>, the length
        /// is <see cref="Size.Width"/> otherwise <see cref="Size.Depth"/>.
        /// </summary>
        /// <param name="orientation">specifies which edge of the enclosing rectangle is meant as length</param>
        /// <returns>the length of the enclosing rectangle along the given <paramref name="orientation"/></returns>
        public float Length(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East => Size.Width,
                Orientation.West => Size.Width,
                Orientation.North => Size.Depth,
                Orientation.South => Size.Depth,
                _ => throw new NotImplementedException($"Unhandled case {orientation}.")
            };
        }

        /// <summary>
        /// The location of the center of the rectangle in world space.
        /// </summary>
        public Location Center;

        /// <summary>
        /// The node in the original graph this ENode is representing.
        /// </summary>
        protected ILayoutNode GraphNode;

        /// <summary>
        /// The depth of this node in the hierarchy. A root has depth 0. This
        /// value will be used to determine the width of a street.
        /// </summary>
        public int TreeDepth;

        /// <summary>
        /// True if this node is left from a street.
        /// </summary>
        public bool Left;

        /// <summary>
        /// The parent of this ENode in the hierarchy. A root has parent null.
        /// </summary>
        public ENode ParentNode;

        /// <summary>
        /// Adds the layout information of this <see cref="ENode"/> to the <paramref name="layout_result"/>.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate of the ground of this node to be added to <paramref name="layout_result"/></param>
        /// <param name="layout_result">layout result</param>
        /// <param name="streetHeight">the height of an inner node (depicted as street)</param>
        public abstract void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layout_result, float groundLevel, float streetHeight);

        public virtual void Print()
        {
            Debug.Log(string.Concat(Enumerable.Repeat("-", TreeDepth)) + ToString() + "\n");
        }

        public override string ToString()
        {
            return $"ENode[ID={GraphNode.ID}, Depth={TreeDepth}, IsLeft={Left}, Center={Center}, Size={Size}, distanceFromOrigin={distanceFromOrigin.ToString("F4")}]";
        }

        /// <summary>
        /// Sets the center location of the node to <paramref name="centerLocation"/> based
        /// on <paramref name="orientation"/>. For a more precise description, see the
        /// overrides of this method in the subclasses.
        /// </summary>
        /// <param name="orientation">the orientation of this node</param>
        /// <param name="centerLocation">center location to be set</param>
        public abstract void SetLocation(Orientation orientation, Location centerLocation);

        /// <summary>
        /// Returns the new orientation based on <paramref name="orientation"/> and whether
        /// this node is left or right from a street.
        /// </summary>
        /// <param name="orientation">the current orientation of this node before the rotation</param>
        /// <returns>new orientation after the rotation</returns>
        internal Orientation Rotate(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East => Left ? Orientation.North : Orientation.South,
                Orientation.West => Left ? Orientation.South : Orientation.North,
                Orientation.North => Left ? Orientation.West : Orientation.East,
                Orientation.South => Left ? Orientation.East : Orientation.West,
                _ => throw new NotImplementedException($"Unhandled case {orientation}.")
            };
        }
    }

    internal class ELeaf : ENode
    {
        public ELeaf(ILayoutNode node) : base(node)
        {
        }

        /// <summary>
        /// Sets <see cref="Size"/> of this node to the absolute scale of its
        /// underlying <see cref="GraphNode"/>.
        /// For reasons of uniformity (unambigious interpretation), the orientation
        /// of a leaf is always towards East/West, that is, their width metric is
        /// depicted uniformely along the x axis.
        /// </summary>
        /// <param name="orientation">will be ignored</param>
        /// <param name="treeDescriptor">will be ignored</param>
        public override void SetSize(Orientation orientation, TreeDescriptor treeDescriptor)
        {
            Size.Width = GraphNode.AbsoluteScale.x;
            Size.Depth = GraphNode.AbsoluteScale.z;
        }

        /// <summary>
        /// Sets <see cref="Center"/> to <paramref name="centerLocation"/>.
        /// </summary>
        /// <param name="orientation">will be ignored</param>
        /// <param name="centerLocation">the center location to be set</param>
        public override void SetLocation(Orientation orientation, Location centerLocation)
        {
            Center = centerLocation;
        }

        public override void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layout_result, float groundLevel, float streetHeight)
        {
            layout_result[GraphNode] = new NodeTransform(new Vector3(Center.X, groundLevel, Center.Y),
                                                         new Vector3 (Size.Width, GraphNode.AbsoluteScale.y, Size.Depth),
                                                         0);
        }

        internal override float SetDistanceFromOrigin(float currentDistanceFromOrigin, Orientation orientation)
        {
            float extent = Length(orientation) / 2.0f;
            distanceFromOrigin = currentDistanceFromOrigin + extent;
            return distanceFromOrigin + extent;
        }
    }

    internal class EInner : ENode
    {
        public EInner(ILayoutNode node) : base(node)
        {
        }

        public void AddChild(ENode child)
        {
            child.ParentNode = this;
            Children.Add(child);
        }

        public override void Print()
        {
            base.Print();
            foreach (ENode child in Children)
            {
                child.Print();
            }
        }

        /// <summary>
        /// The children of this ENode in the hierarchy.
        /// </summary>
        public List<ENode> Children = new List<ENode>();

        /// <summary>
        /// This is the size of the street itself representing the inner node.
        /// The attribute <see cref="Size"/> relates to the rectangle enclosing
        /// this street and all children.
        /// </summary>
        public Scale StreetSize;

        /// <summary>
        ///
        /// Note: Does not set <see cref="Orientation"/> to <paramref name="orientation"/>. This value
        /// may be overridden later: <see cref="Orientation.East"/> may be overriden by
        /// <see cref="Orientation.West"/> and <see cref="Orientation.North"/> by
        /// <see cref="Orientation.South"/> depending on which side of the street this node
        /// will be placed.
        ///
        /// Here we accept only <see cref="Orientation.East"/> or <see cref="Orientation.North"/>
        /// as <paramref name="orientation"/> for reasons of simplicity. For calculating the size
        /// we only need to know which edge of the rectangle is to be used to determine the
        /// length. The lengths of the rectangle for <see cref="Orientation.East"/> and
        /// <see cref="Orientation.West"/> are the same; likewise for <see cref="Orientation.North"/> and
        /// <see cref="Orientation.South"/>.
        /// </summary>
        /// <param name="orientation">determines the direction of the street depicting this node</param>
        /// <param name="treeDescriptor">parameters regarding the layout of the tree</param>
        /// <exception cref="ArgumentException">thrown if <paramref name="orientation"/> is neither
        /// <see cref="Orientation.East"/> nor <see cref="Orientation.North"/></exception>
        public override void SetSize(Orientation orientation, TreeDescriptor treeDescriptor)
        {
            if (orientation != Orientation.East && orientation != Orientation.North)
            {
                throw new ArgumentException($"Unexpected orientation {orientation}. Only {Orientation.East} and {Orientation.North} are allowed.");
            }

            {
                // First determine the size of all descendants.
                Orientation childOrientation = orientation == Orientation.North ? Orientation.East : Orientation.North;
                foreach (ENode child in Children)
                {
                    child.SetSize(childOrientation, treeDescriptor);
                }
            }

            // Now put the children along the street.
            float leftOffset = offsetBetweenBuildings;
            float rightOffset = offsetBetweenBuildings;

            foreach (ENode child in Children)
            {
                child.Left = leftOffset <= rightOffset;
                if (child.Left)
                {
                    leftOffset = child.SetDistanceFromOrigin(leftOffset, orientation) + offsetBetweenBuildings;
                }
                else
                {
                    rightOffset = child.SetDistanceFromOrigin(rightOffset, orientation) + offsetBetweenBuildings;
                }
            }

            if (orientation == Orientation.East)
            {
                StreetSize.Width = Mathf.Max(leftOffset, rightOffset);
                StreetSize.Depth = RelativeStreetWidth(this);
                Size.Width = StreetSize.Width;
                Size.Depth = StreetSize.Depth + Max(Children, left: true, width: false) + Max(Children, left: false, width: false);
            }
            else
            {
                // assert: orientation == Orientation.North
                StreetSize.Depth = Mathf.Max(leftOffset, rightOffset);
                StreetSize.Width = RelativeStreetWidth(this);
                Size.Depth = StreetSize.Depth;
                Size.Width = StreetSize.Width + Max(Children, left: true, width: true) + Max(Children, left: false, width: true);
            }

            float RelativeStreetWidth(EInner node)
            {
                return treeDescriptor.StreetWidth * ((treeDescriptor.MaximalDepth + 1) - node.TreeDepth) / (treeDescriptor.MaximalDepth + 1);
            }
        }

        private static float Max(IList<ENode> children, bool left, bool width)
        {
            float result = 0;
            foreach (ENode child in children)
            {
                if (child.Left == left)
                {
                    float length = width ? child.Size.Width : child.Size.Depth;
                    if (length > result)
                    {
                        result = length;
                    }
                }
            }
            return result;
        }

        protected static float Increase(float value, float by, Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East => value + by,
                Orientation.West => value - by,
                Orientation.North => value + by,
                Orientation.South => value - by,
                _ => throw new NotImplementedException($"Unhandled case {orientation}.")
            };
        }

        protected static Orientation Invert(Orientation orientation)
        {
            return orientation switch
            {
                Orientation.East => Orientation.West,
                Orientation.West => Orientation.East,
                Orientation.North => Orientation.South,
                Orientation.South => Orientation.North,
                _ => throw new NotImplementedException($"Unhandled case {orientation}.")
            };
        }

        protected static Location MoveTo(Location value, float by, Orientation orientation)
        {
            Location result = value;
            switch (orientation)
            {
                case Orientation.East: result.X += by; break;
                case Orientation.West: result.X -= by; break;
                case Orientation.North: result.Y += by; break;
                case Orientation.South: result.Y -= by; break;
                default:
                    throw new NotImplementedException($"Unhandled case {orientation}.");
            }

            return result;
        }

        /// <summary>
        /// Sets the center location of the node to <paramref name="centerLocation"/> based
        /// on <paramref name="orientation"/>. For a more precise description, see the
        /// overrides of this method in the subclasses.
        /// </summary>
        /// <param name="orientation">the orientation of this node</param>
        /// <param name="centerLocation">center location to be set</param>
        public override void SetLocation(Orientation orientation, Location centerLocation)
        {
            Center = centerLocation;
            Location origin = MoveTo(centerLocation, Length(orientation) / 2, Invert(orientation));
            float streetExtent = (orientation == Orientation.East || orientation == Orientation.West
                                  ? StreetSize.Depth : StreetSize.Width) / 2;
            foreach (ENode child in Children)
            {
                Orientation childOrientation = child.Rotate(orientation);
                // Move child parallel to the street.
                Location childCenter = MoveTo(origin, child.distanceFromOrigin, orientation);
                // Move child to the edge of the street
                childCenter = MoveTo(childCenter, streetExtent + child.Length(childOrientation) / 2, childOrientation);
                child.SetLocation(childOrientation, childCenter);
            }
        }

        public override void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layout_result, float groundLevel, float streetHeight)
        {
            layout_result[GraphNode]
                = new NodeTransform(new Vector3(Center.X, groundLevel, Center.Y),
                                    //new Vector3(Size.Width, streetHeight, Size.Depth),
                                    new Vector3(StreetSize.Width, streetHeight, StreetSize.Depth),
                                    0);
            foreach (ENode child in Children)
            {
                child.ToLayout(ref layout_result, groundLevel, streetHeight);
            }
        }

        internal override float SetDistanceFromOrigin(float currentDistanceFromOrigin, Orientation orientation)
        {
            if (orientation != Orientation.East && orientation != Orientation.North)
            {
                throw new ArgumentException($"Unexpected orientation {orientation}. Only {Orientation.East} and {Orientation.North} are allowed.");
            }

            float streetWidth = orientation == Orientation.East ? StreetSize.Width : StreetSize.Depth;
            distanceFromOrigin = currentDistanceFromOrigin + streetWidth + Max(Children, left: true, width: orientation == Orientation.East);
            return currentDistanceFromOrigin + Length(orientation);
        }
    }

    internal static class ENodeFactory
    {
        public static ENode Create(ILayoutNode node)
        {
            if (node.IsLeaf)
            {
                return new ELeaf(node);
            }
            else
            {
                return new EInner(node);
            }
        }
    }
}
