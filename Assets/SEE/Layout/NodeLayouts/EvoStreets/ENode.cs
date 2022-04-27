using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// The classes here implement the EvoStreets layout. The layouting works in two
    /// steps:
    ///
    /// (1) the sizes required for all nodes are calculated by distributing
    ///     children of inner node left and right along a street
    /// (2) once the required size of each node is know, they will be placed
    ///     along the street.

    /// <summary>
    /// Provides parameters regarding the layout of the EvoStreets.
    /// </summary>
    internal struct LayoutDescriptor
    {
        /// <summary>
        /// The maximal depth of the tree.
        /// </summary>
        public float MaximalDepth;

        /// <summary>
        /// The width of the street for the root node. The width of streets at the lower level will be depicted
        /// smaller relative to this value depending upon their level in the tree and <see cref="MaximalDepth"/>.
        /// </summary>
        public float StreetWidth;

        /// <summary>
        /// The distance between two neighboring node representations.
        /// </summary>
        public float OffsetBetweenBuildings;
    }

    /// <summary>
    /// The absolute orientation of nodes in world space.
    /// </summary>
    internal enum Orientation
    {
        North,
        East,
        South,
        West
    }

    /// <summary>
    /// A absolute location in a two-dimensional world space.
    /// </summary>
    internal struct Location
    {
        /// <summary>
        /// Absolute value along the X axis (width) in world space.
        /// </summary>
        public float X;

        /// <summary>
        /// Absolute value along the Y axis (depth) in world space.
        /// </summary>
        public float Y;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">absolute value along the X axis (width) in world space</param>
        /// <param name="y">absolute value along the Y axis (depth) in world space</param>
        public Location(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// The co-ordinates as a human-readable string.
        /// </summary>
        /// <returns>co-ordinates as a human-readable string</returns>
        public override string ToString()
        {
            return $"[x={X:F4}, y={Y:F4}]";
        }
    }

    /// <summary>
    /// A rectangle.
    /// </summary>
    internal struct Rectangle
    {
        /// <summary>
        /// The width in world space, i.e., along the x axis.
        /// </summary>
        public float Width;

        /// <summary>
        /// The depth in world space, i.e., along the z axis.
        /// </summary>
        public float Depth;

        /// <summary>
        /// The center point of the rectangle in world space.
        /// </summary>
        public Location Center;

        /// <summary>
        /// The rectangle as a human-readable string.
        /// </summary>
        /// <returns>rectangle as a human-readable string</returns>
        public override string ToString()
        {
            return $"[center={Center}, width={Width:F4}, depth={Depth:F4}]";
        }
    }

    /// <summary>
    /// Abstract class for the layout of graph nodes for the EvoStreets layout.
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
        /// The outermost enclosing rectangle of this node (and possibly its descendants)
        /// in world space.
        /// </summary>
        public Rectangle Rectangle;

        /// <summary>
        /// Calculates and sets the necessary size of <see cref="Rectangle"/> for this node.
        /// </summary>
        /// <param name="orientation">the orientation of this node in world space</param>
        /// <param name="treeDescriptor">parameters regarding the layout</param>
        public abstract void SetSize(Orientation orientation, LayoutDescriptor treeDescriptor);

        /// <summary>
        /// The distance from the starting point of the street containing this node to the
        /// node's <see cref="Rectangle.Center"/>. The starting point of a street oriented
        /// toward East is its left corner. The starting point of a street oriented
        /// towards North, is its lower corner.
        ///
        /// Note: This value will be computed assuming only the orientation towards East
        /// or North by <see cref="SetSize(Orientation, LayoutDescriptor)"/> and, hence, is always positive.
        /// </summary>
        internal float DistanceFromOrigin;

        /// <summary>
        /// Sets <see cref="DistanceFromOrigin"/> as the sum of <paramref name="currentDistanceFromOrigin"/>
        /// and the length (extent, really, i.e., half of <see cref="Length(Orientation)"/>) of this node
        /// along the given <paramref name="orientation"/>.
        /// Returns <paramref name="currentDistanceFromOrigin"/> plus the length of the <see cref="Rectangle"/>
        /// with respect to <paramref name="orientation"/>.
        /// </summary>
        /// <param name="currentDistanceFromOrigin">the current distance from the origin</param>
        /// <param name="orientation">the orientation of the street currently handled.</param>
        /// <returns>the updated <paramref name="currentDistanceFromOrigin"/></returns>
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
        /// <param name="orientation">specifies which edge of the enclosing rectangle is meant as length</param>
        /// <returns>the length of the enclosing rectangle along the given <paramref name="orientation"/></returns>
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
        protected ILayoutNode GraphNode;

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
        /// Adds the layout information of this <see cref="ENode"/> to the <paramref name="layout_result"/>.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate of the ground of this node to be added to <paramref name="layout_result"/></param>
        /// <param name="layout_result">layout where to add the layout information</param>
        /// <param name="streetHeight">the height of an inner node (depicted as street)</param>
        public abstract void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layout_result, float groundLevel, float streetHeight);

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
        /// <returns>node as a human-readable string</returns>
        public override string ToString()
        {
            return $"ENode[ID={GraphNode.ID}, Depth={TreeDepth}, IsLeft={Left}, Rectangle={Rectangle}, distanceFromOrigin={DistanceFromOrigin.ToString("F4")}]";
        }

        /// <summary>
        /// Sets the <see cref="Rectangle.Center"/> of this node to <paramref name="centerLocation"/> based
        /// on <paramref name="orientation"/>. For a more precise description, see the overrides of this
        /// method in the subclasses.
        /// </summary>
        /// <param name="orientation">the orientation of this node</param>
        /// <param name="centerLocation">center location to be set</param>
        public abstract void SetLocation(Orientation orientation, Location centerLocation);

        /// <summary>
        /// Returns the new orientation based on <paramref name="orientation"/> and whether
        /// this node is left or right from a street.
        /// </summary>
        /// <param name="orientation">the current orientation of this node before the rotation</param>
        /// <returns>absolute new orientation after the rotation in world space</returns>
        internal Orientation Rotate(Orientation orientation)
        {
            // FIXME: In C# version 9, we can use the 'or' operator here.
            return orientation switch
            {
                Orientation.East => Left ? Orientation.North : Orientation.South,
                Orientation.West => Left ? Orientation.North : Orientation.South,
                Orientation.North => Left ? Orientation.West : Orientation.East,
                Orientation.South => Left ? Orientation.West : Orientation.East,
                _ => throw new NotImplementedException($"Unhandled case {orientation}.")
            };
        }
    }

    /// <summary>
    /// Representation of a leaf node for the EvoStreets.
    /// </summary>
    internal class ELeaf : ENode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">the leaf graph node represented by this <see cref="ENode"/></param>
        public ELeaf(ILayoutNode node) : base(node)
        {
        }

        /// <summary>
        /// Sets <see cref="Rectangle.Width"/> and <see cref="Rectangle.Depth"/> of this node
        /// to the absolute scale of its underlying <see cref="GraphNode"/>.
        /// For reasons of uniformity (unambigious interpretation), the orientation
        /// of a leaf is always towards East/West, that is, its width metric is
        /// depicted uniformly along the x axis in world space.
        /// </summary>
        /// <param name="orientation">will be ignored</param>
        /// <param name="treeDescriptor">will be ignored</param>
        public override void SetSize(Orientation orientation, LayoutDescriptor treeDescriptor)
        {
            Rectangle.Width = GraphNode.AbsoluteScale.x;
            Rectangle.Depth = GraphNode.AbsoluteScale.z;
        }

        /// <summary>
        /// Sets <see cref="Center"/> to <paramref name="centerLocation"/>.
        /// </summary>
        /// <param name="orientation">will be ignored</param>
        /// <param name="centerLocation">the center location to be set</param>
        public override void SetLocation(Orientation orientation, Location centerLocation)
        {
            Rectangle.Center = centerLocation;
        }

        /// <summary>
        /// Adds the layout information of this <see cref="ELeaf"/> to the <paramref name="layout_result"/>.
        /// <seealso cref="ENode.ToLayout(ref Dictionary{ILayoutNode, NodeTransform}, float, float)"/>.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate of the ground of this node to be added to <paramref name="layout_result"/></param>
        /// <param name="layout_result">layout where to add the layout information</param>
        /// <param name="streetHeight">will be ignored</param>
        public override void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layout_result, float groundLevel, float streetHeight)
        {
            layout_result[GraphNode] = new NodeTransform(new Vector3(Rectangle.Center.X, groundLevel, Rectangle.Center.Y),
                                                         new Vector3 (Rectangle.Width, GraphNode.AbsoluteScale.y, Rectangle.Depth),
                                                         0);
        }
    }

    /// <summary>
    /// Representation of an inner node for the EvoStreets.
    /// </summary>
    internal class EInner : ENode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">the inner graph node represented by this <see cref="ENode"/></param>
        public EInner(ILayoutNode node) : base(node)
        {
        }

        /// <summary>
        /// Adds <see cref="child"/> to the <see cref="Children"/> of this node.
        /// </summary>
        /// <param name="child">immediate child to be added</param>
        public void AddChild(ENode child)
        {
            Children.Add(child);
        }

        /// <summary>
        /// Prints this node and all its descendants with an indentation proportional to its <see cref="TreeDepth"/>.
        /// Can be used for debugging.
        /// </summary>
        public override void Print()
        {
            base.Print();
            foreach (ENode child in Children)
            {
                child.Print();
            }
        }

        /// <summary>
        /// The children of this inner node in the hierarchy.
        /// </summary>
        public List<ENode> Children = new List<ENode>();

        /// <summary>
        /// This is the rectangle for the street itself representing the inner node.
        /// The attribute <see cref="Rectangle"/> relates to the rectangle enclosing
        /// this street and all children of this inner node.
        /// </summary>
        public Rectangle Street;

        /// <summary>
        /// Calculates and sets the necessary size of <see cref="Rectangle"/> and <see cref="Street"/>
        /// for this node as follows:
        ///
        /// 1) This method recurses into its descendants first to calculate their respective size.
        ///
        /// 2) The <see cref="Children"/> of this node will be aligned along the <see cref="Street"/> left or right
        ///    with respect to the given <paramref name="orientation"/> in the predefined order of <see cref="Children"/>
        ///    in such a way that both sides of the streets occupy a similar sum of lengths of those <see cref="Children"/>.
        ///    The distribution is greedy, that is, does not guarantee that the overall length of the <see cref="Street"/>
        ///    is minimized. At the begin and end of the <see cref="Street"/> as well as between neighboring <see cref="Children"/>,
        ///    <paramref name="treeDescriptor.OffsetBetweenBuildings"/> will be added. The length of the <see cref="Street"/>
        ///    is chosen to cover exactly the length of this alignment. The street width (which would be <see cref="Street.Depth"/>
        ///    if <paramref name="orientation"/> is <see cref="Orientation.East"/> and <see cref="Street.Width"/>
        ///    if <paramref name="orientation"/> is <see cref="Orientation.North"/>) is a relative proportion of
        ///    <paramref name="treeDescriptor.StreetWidth"/>: the fraction of <see cref="TreeDepth"/> and
        ///    <paramref name="treeDescriptor.MaximalDepth"/>.
        ///
        ///  3) In addition, this method stores the distance from the edge of the outermost <see cref="Rectangle"/>
        ///     to the center of the street in one of the length attributes of <see cref="Street"/>.
        ///     If <paramref name="orientation"/> equals <see cref="Orientation.East"/> the relevant edge is the
        ///     left edge of <see cref="Rectangle"/>; otherwise it is its lower edge.
        ///     This value is just a relative value and requires an update towards world space when the inner is
        ///     actually located in <see cref="SetLocation(Orientation, Location)"/>. It allows us to determine
        ///     the position of <see cref="Street"/> within <see cref="Rectangle"/>.
        ///
        /// Precondition:
        /// Here we accept only <see cref="Orientation.East"/> or <see cref="Orientation.North"/>
        /// as <paramref name="orientation"/> for reasons of simplicity. For calculating the size,
        /// we only need to know which edge of the rectangle is to be used to determine the
        /// length. The lengths of the rectangle for <see cref="Orientation.East"/> and
        /// <see cref="Orientation.West"/> are the same; likewise for <see cref="Orientation.North"/> and
        /// <see cref="Orientation.South"/>.
        /// </summary>
        /// <param name="orientation">determines the direction of the street depicting this node in world space</param>
        /// <param name="treeDescriptor">parameters regarding the layout</param>
        /// <exception cref="ArgumentException">thrown if <paramref name="orientation"/> is neither
        /// <see cref="Orientation.East"/> nor <see cref="Orientation.North"/></exception>
        public override void SetSize(Orientation orientation, LayoutDescriptor treeDescriptor)
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
            float leftOffset = treeDescriptor.OffsetBetweenBuildings;
            float rightOffset = treeDescriptor.OffsetBetweenBuildings;

            foreach (ENode child in Children)
            {
                child.Left = leftOffset <= rightOffset;
                if (child.Left)
                {
                    leftOffset = child.SetDistanceFromOrigin(leftOffset, orientation) + treeDescriptor.OffsetBetweenBuildings;
                }
                else
                {
                    rightOffset = child.SetDistanceFromOrigin(rightOffset, orientation) + treeDescriptor.OffsetBetweenBuildings;
                }
            }

            if (orientation == Orientation.East)
            {
                Street.Width = Mathf.Max(leftOffset, rightOffset);
                Street.Depth = RelativeStreetWidth(this);
                Rectangle.Width = Street.Width;
                float depthForRightChildren = Max(Children, left: false, width: false);
                /// As a temporary value, we store the distance from the lower edge of the outermost rectangle
                /// to the center depth of the street. This value is just a relative value and requires an
                /// update when the inner is actually located in <see cref="SetLocation(Orientation, Location)"/>.
                Street.Center.Y = depthForRightChildren + Street.Depth / 2;
                Rectangle.Depth = Street.Depth + Max(Children, left: true, width: false) + depthForRightChildren;
            }
            else
            {
                // assert: orientation == Orientation.North
                Street.Depth = Mathf.Max(leftOffset, rightOffset);
                Street.Width = RelativeStreetWidth(this);
                Rectangle.Depth = Street.Depth;
                float widthForLeftChildren = Max(Children, left: true, width: true);
                /// As a temporary value, we store the distance from the left edge of the outermost rectangle
                /// to the center width of the street. This value is just a relative value and requires an
                /// update when the inner is actually located in <see cref="SetLocation(Orientation, Location)"/>.
                Street.Center.X = widthForLeftChildren + Street.Width / 2;
                Rectangle.Width = Street.Width + widthForLeftChildren + Max(Children, left: false, width: true);
            }

            float RelativeStreetWidth(EInner node)
            {
                return treeDescriptor.StreetWidth * ((treeDescriptor.MaximalDepth + 1) - node.TreeDepth) / (treeDescriptor.MaximalDepth + 1);
            }
        }

        /// <summary>
        /// Returns the maximal length of all <paramref name="children"/> that are on the <paramref name="left"/>
        /// side (i.e, for which <see cref="Left"/> equals <paramref name="left"/> holds). If <paramref name="width"/>
        /// is true, <see cref="Rectangle.Width"/> will be used as the length; otherwise <see cref="Rectangle.Depth"/>.
        /// </summary>
        /// <param name="children">the children for which to determine the maximal length</param>
        /// <param name="left">if true, only left children are considered; otherwise only right children</param>
        /// <param name="width">if true, the maximum of <see cref="Rectangle.Width"/> will be returned, otherwise
        /// the maximum of <see cref="Rectangle.Depth"/> of the <paramref name="children"/> to be considered</param>
        /// <returns>maximal length of <paramref name="children"/></returns>
        private static float Max(IList<ENode> children, bool left, bool width)
        {
            float result = 0;
            foreach (ENode child in children)
            {
                if (child.Left == left)
                {
                    float length = width ? child.Rectangle.Width : child.Rectangle.Depth;
                    if (length > result)
                    {
                        result = length;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the inverted <paramref name="orientation"/>.
        /// </summary>
        /// <param name="orientation">orientation to be inverted</param>
        /// <returns>inverted <paramref name="orientation"/></returns>
        private static Orientation Invert(Orientation orientation)
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

        /// <summary>
        /// Returns the moved location <paramref name="value"/> towards <paramref name="orientation"/>
        /// by the given (positive) distance <paramref name="by"/>.
        /// </summary>
        /// <param name="value">location to be moved</param>
        /// <param name="by">the distance of the movement</param>
        /// <param name="orientation">the direction of the movement</param>
        /// <returns>moved location</returns>
        private static Location MoveTo(Location value, float by, Orientation orientation)
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
        /// Sets the center location of <see cref="Rectangle"/> of the node to
        /// <paramref name="centerLocation"/> based  on <paramref name="orientation"/>.
        /// Sets the center location of the <see cref="Street"/>.
        /// </summary>
        /// <param name="orientation">the orientation of this node</param>
        /// <param name="centerLocation">center location to be set</param>
        public override void SetLocation(Orientation orientation, Location centerLocation)
        {
            bool horizontal = orientation == Orientation.East || orientation == Orientation.West;
            Rectangle.Center = centerLocation;
            if (horizontal)
            {
                Street.Center.X = Rectangle.Center.X;
                /// We have set <see cref="Rectangle.Center.Y"/> as a relative value in <see cref="SetSize(Orientation, LayoutDescriptor)"/>.
                Street.Center.Y += Rectangle.Center.Y - Rectangle.Depth / 2;
            }
            else
            {
                Street.Center.Y = Rectangle.Center.Y;
                /// We have set <see cref="Rectangle.Center.X"/> as a relative value in <see cref="SetSize(Orientation, LayoutDescriptor)"/>.
                Street.Center.X += Rectangle.Center.X - Rectangle.Width / 2;
            }
            Location origin = MoveTo(Street.Center, Length(orientation) / 2, Invert(orientation));

            float streetExtent = (horizontal ? Street.Depth : Street.Width) / 2;
            foreach (ENode child in Children)
            {
                Orientation childOrientation = child.Rotate(orientation);
                // Move child parallel to the street.
                Location childCenter = MoveTo(origin, child.DistanceFromOrigin, orientation);
                // Move child to the edge of the street
                childCenter = MoveTo(childCenter, streetExtent + child.Length(childOrientation) / 2, childOrientation);
                child.SetLocation(childOrientation, childCenter);
            }
        }

        /// <summary>
        /// Adds the layout information of this <see cref="EInner"/> to the <paramref name="layout_result"/>.
        ///
        /// Unlike <see cref="ELeaf.ToLayout(ref Dictionary{ILayoutNode, NodeTransform}, float, float)"/>, this
        /// method adds the data from <see cref="Street"/> because that rectangle is used to depict an inner
        /// node. The attribute <see cref="Rectangle"/> is just the area enclosing this street and all
        /// representations of the descendants of this node.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate of the ground of this node to be added to <paramref name="layout_result"/></param>
        /// <param name="layout_result">layout where to add the layout information</param>
        /// <param name="streetHeight">the height of an inner node (depicted as street)</param>
        public override void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layout_result, float groundLevel, float streetHeight)
        {
            layout_result[GraphNode]
                = new NodeTransform(new Vector3(Street.Center.X, groundLevel, Street.Center.Y),
                                    new Vector3(Street.Width, streetHeight, Street.Depth),
                                    0);
            foreach (ENode child in Children)
            {
                child.ToLayout(ref layout_result, groundLevel, streetHeight);
            }
        }
    }

    /// <summary>
    /// A factory returning instances of subclasses of <see cref="ENode"/>.
    /// </summary>
    internal static class ENodeFactory
    {
        /// <summary>
        /// Returns a representation of <paramref name="node"/> for the EvoStreet layout.
        /// If <paramref name="node"/> is a leaf, an instance of <see cref="ELeaf"/> will
        /// be returned, otherwise an instance of <see cref="EInner"/>.
        /// </summary>
        /// <param name="node">graph node to be laid out in an EvoStreets layout</param>
        /// <returns>representation of <paramref name="node"/> for the EvoStreets layout</returns>
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
