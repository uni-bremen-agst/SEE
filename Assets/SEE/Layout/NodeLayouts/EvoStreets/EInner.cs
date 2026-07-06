using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Assertions.Assert;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Representation of an inner node for the EvoStreets.
    /// </summary>
    internal class EInner : ENode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">The inner graph node represented by this <see cref="ENode"/>.</param>
        public EInner(ILayoutNode node) : base(node)
        {
        }

        /// <summary>
        /// Adds <see cref="child"/> to the <see cref="children"/> of this node.
        /// </summary>
        /// <param name="child">Immediate child to be added.</param>
        public void AddChild(ENode child)
        {
            children.Add(child);
        }

        /// <summary>
        /// Prints this node and all its descendants with an indentation proportional to its <see cref="TreeDepth"/>.
        /// Can be used for debugging.
        /// </summary>
        public override void Print()
        {
            base.Print();
            foreach (ENode child in children)
            {
                child.Print();
            }
        }

        /// <summary>
        /// The children of this inner node in the hierarchy.
        /// </summary>
        private readonly List<ENode> children = new();

        /// <summary>
        /// This is the rectangle for the street itself representing the inner node.
        /// The attribute <see cref="Rectangle"/> relates to the rectangle enclosing
        /// this street and all children of this inner node.
        /// </summary>
        private Rectangle street;

        /// <summary>
        /// Calculates and sets the necessary size of <see cref="Rectangle"/> and <see cref="street"/>
        /// for this node as follows:
        ///
        /// 1) This method recurses into its descendants first to calculate their respective size.
        ///
        /// 2) The <see cref="children"/> of this node will be aligned along the <see cref="street"/> left or right
        ///    with respect to the given <paramref name="orientation"/> in the predefined order of <see cref="children"/>
        ///    in such a way that both sides of the streets occupy a similar sum of lengths of those <see cref="children"/>.
        ///    The distribution is greedy, that is, does not guarantee that the overall length of the <see cref="street"/>
        ///    is minimized. At the beginning and end of the <see cref="street"/> as well as between neighboring <see cref="children"/>,
        ///    <paramref name="treeDescriptor.OffsetBetweenBuildings"/> will be added. The length of the <see cref="street"/>
        ///    is chosen to cover exactly the length of this alignment. The street width (which would be <see cref="Street.Depth"/>
        ///    if <paramref name="orientation"/> is <see cref="Orientation.East"/> and <see cref="Street.Width"/>
        ///    if <paramref name="orientation"/> is <see cref="Orientation.North"/>) is a relative proportion of
        ///    <paramref name="treeDescriptor.StreetWidth"/>: the fraction of <see cref="TreeDepth"/> and
        ///    <paramref name="treeDescriptor.MaximalDepth"/>.
        ///
        ///  3) In addition, this method stores the distance from the edge of the outermost <see cref="Rectangle"/>
        ///     to the center of the street in one of the length attributes of <see cref="street"/>.
        ///     If <paramref name="orientation"/> equals <see cref="Orientation.East"/> the relevant edge is the
        ///     left edge of <see cref="Rectangle"/>; otherwise it is its lower edge.
        ///     This value is just a relative value and requires an update towards world space when the inner is
        ///     actually located in <see cref="SetLocation(Orientation, Location)"/>. It allows us to determine
        ///     the position of <see cref="street"/> within <see cref="Rectangle"/>.
        ///
        /// Precondition:
        /// Here we accept only <see cref="Orientation.East"/> or <see cref="Orientation.North"/>
        /// as <paramref name="orientation"/> for reasons of simplicity. For calculating the size,
        /// we only need to know which edge of the rectangle is to be used to determine the
        /// length. The lengths of the rectangle for <see cref="Orientation.East"/> and
        /// <see cref="Orientation.West"/> are the same; likewise for <see cref="Orientation.North"/> and
        /// <see cref="Orientation.South"/>.
        /// </summary>
        /// <param name="orientation">Determines the direction of the street depicting this node in world space.</param>
        /// <param name="treeDescriptor">Parameters regarding the layout.</param>
        /// <param name="lastLayout">The layout computed in the previous layouting.</param>
        /// <param name="newNodes">The new nodes to be placed at the end of the street.</param>
        /// <param name="existingNodes">Nodes that existed already in the previous layout. They
        /// will be placed in their original order.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="orientation"/> is neither
        /// <see cref="Orientation.East"/> nor <see cref="Orientation.North"/>.</exception>
        public override void SetSizeAndDistribute
            (Orientation orientation,
            LayoutDescriptor treeDescriptor,
            Dictionary<ILayoutNode, NodeTransform> lastLayout,
            ILayoutNodeSet newNodes,
            ILayoutNodeSet existingNodes)
        {
            /// Note: an inner node can become a leaf in the new revision and vice versa.

            if (orientation != Orientation.East && orientation != Orientation.North)
            {
                throw new ArgumentException($"Unexpected orientation {orientation}. Only {Orientation.East} and {Orientation.North} are allowed.");
            }

            {
                /// Alternate the orientation for the children.
                Orientation childOrientation = orientation == Orientation.North ? Orientation.East : Orientation.North;
                /// First determine the size of all descendants.
                foreach (ENode child in children)
                {
                    child.SetSizeAndDistribute(childOrientation, treeDescriptor, lastLayout, newNodes, existingNodes);
                }
            }

            /// Now put the children along the street.

            /// The left offset where the next child is to be placed relative to the origin of the street.
            float leftOffset = treeDescriptor.OffsetBetweenBuildings;
            /// The right offset where the next child is to be placed relative to the origin of the street.
            float rightOffset = treeDescriptor.OffsetBetweenBuildings;

            /// If this EInner node is new, its children are considered new, too, and
            /// hence, we do not need to care about a stable layout. Likewise, if we
            /// do not have any previous layout.
            if (lastLayout == null || ContainedIn(newNodes))
            {
                /// Align all children at once, no matter whether they are existing or new.
                AlignChildren(node => true);
            }
            else
            {
                /// This EInner node existed in the previous layout already.
                /// Retrieve the orientation of this EInner node in the previous layout.
                Orientation previousOrientation = PreviousOrientation(this);

                /// Partition the children into two groups as follows:
                /// 1) Orientation is North or South: partition children into left and right from EInner node.
                /// 2) Orientation is East or West:   partition children into above and below from EInner node.
                (List<ENode> firstPartition, List<ENode> secondPartition) = Partition(previousOrientation);

                /// First place the <paramref name="existingNodes"/>.
                /// We want to preserve the original order of all existing children in the previous layout.

                /// Sort each partition according to the world-space center position.
                /// 1) ascendingly when orientation is East or North
                /// 2) descendingly when orientation is West or South
                bool ascending = previousOrientation == Orientation.East || previousOrientation == Orientation.North;
                /// If the previous orientation is West or East, the X co-ordinate of the previous layout
                /// determines the order, otherwise the Z co-ordinate.
                bool alongXaxis = previousOrientation == Orientation.West || previousOrientation == Orientation.East;
                Sort(firstPartition, ascending, alongXaxis);
                Sort(secondPartition, ascending, alongXaxis);

                foreach (ENode child in firstPartition)
                {
                    child.Left = true;
                    leftOffset = child.SetDistanceFromOrigin(leftOffset, orientation) + treeDescriptor.OffsetBetweenBuildings;
                }

                foreach (ENode child in secondPartition)
                {
                    child.Left = false;
                    rightOffset = child.SetDistanceFromOrigin(rightOffset, orientation) + treeDescriptor.OffsetBetweenBuildings;
                }

                /// Then place the <paramref name="newNodes"/> at the end of the street.
                AlignChildren(node => node.ContainedIn(newNodes));
            }

            /// Closing calculations of the width, depth and center of the <see cref="street"/>
            /// and the depth and width of <see cref="Rectangle"/>.
            if (orientation == Orientation.East)
            {
                street.Width = Mathf.Max(leftOffset, rightOffset);
                street.Depth = RelativeStreetWidth(this);
                Rectangle.Width = street.Width;
                float depthForRightChildren = Max(children, left: false, width: false);
                /// As a temporary value, we store the distance from the lower edge of the outermost rectangle
                /// to the center depth of the street. This value is just a relative value and requires an
                /// update when the inner node is actually located in <see cref="SetLocation(Orientation, Location)"/>.
                street.Center.Y = depthForRightChildren + street.Depth / 2;
                Rectangle.Depth = street.Depth + Max(children, left: true, width: false) + depthForRightChildren;
            }
            else
            {
                AreEqual(orientation, Orientation.North);
                street.Depth = Mathf.Max(leftOffset, rightOffset);
                street.Width = RelativeStreetWidth(this);
                Rectangle.Depth = street.Depth;
                float widthForLeftChildren = Max(children, left: true, width: true);
                /// As a temporary value, we store the distance from the left edge of the outermost rectangle
                /// to the center width of the street. This value is just a relative value and requires an
                /// update when the inner node is actually located in <see cref="SetLocation(Orientation, Location)"/>.
                street.Center.X = widthForLeftChildren + street.Width / 2;
                Rectangle.Width = street.Width + widthForLeftChildren + Max(children, left: false, width: true);
            }

            return;
            /// Below follow the local functions.

            /// The width of the street for given node. It depends upon is hierarchical depth. The deeper the
            /// node in the hierarchy, the narrower the street.
            float RelativeStreetWidth(EInner node)
            {
                return treeDescriptor.StreetWidth * (treeDescriptor.MaximalDepth + 1 - node.TreeDepth) / (treeDescriptor.MaximalDepth + 1);
            }

            /// Aligns all children for which isRelevant yields true on both sides of the street,
            /// attempting to fill both streets sides equally. Adjusts leftOffset and rightOffset.
            void AlignChildren(Func<ENode, bool> isRelevant)
            {
                foreach (ENode child in children.Where(enode => isRelevant(enode)))
                {
                    /// We want to populate boths sides of the street mostly equally.
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
            }

            /// Returns the previous layout for the node with the given id .
            /// Assumption: This method is applied to nodes that existed before only.
            ILayoutNode GetLayoutNode(string id)
            {
                /// FIXME (#975): This is a sequential search. We can do better.
                return lastLayout.Keys.FirstOrDefault(ln => ln.ID == id);
            }

            /// Returns the position of given node relative to its parent.
            /// If node is the root, <see cref="EvoStreetsNodeLayout.RootOrientation"/>
            /// will be returned.
            Orientation PreviousOrientation(EInner node)
            {
                ILayoutNode layoutNode = GetLayoutNode(node.Name);

                if (layoutNode.Parent == null)
                {
                    /// This node is the root of the lastLayout.
                    return EvoStreetsNodeLayout.RootOrientation;
                }
                NodeTransform layoutForNode = lastLayout[layoutNode];
                NodeTransform layoutForParent = lastLayout[layoutNode.Parent];

                /// By the design of an EvoStreet, a child is always connected to
                /// one of the four edges of a street (parent) and child and parent
                /// do not overlap.
                if (FloatUtils.IsLessThanOrEqual(layoutForParent.Left, layoutForNode.Left)
                    && FloatUtils.IsLessThanOrEqual(layoutForNode.Right, layoutForParent.Right))
                {
                    if (FloatUtils.IsLessThanOrEqual(layoutForParent.Back, layoutForNode.Front))
                    {
                        return Orientation.North;
                    }
                    if (FloatUtils.IsLessThanOrEqual(layoutForNode.Back, layoutForParent.Front))
                    {
                        return Orientation.South;
                    }
                    throw new InvalidOperationException("Impossible execution path. Unexpected relative positioning.");
                }

                if (FloatUtils.IsLessThanOrEqual(layoutForParent.Front, layoutForNode.Front)
                    && FloatUtils.IsLessThanOrEqual(layoutForNode.Back, layoutForParent.Back))
                {
                    if (FloatUtils.IsLessThanOrEqual(layoutForNode.Right, layoutForParent.Left))
                    {
                        return Orientation.West;
                    }
                    if (FloatUtils.IsLessThanOrEqual(layoutForParent.Right, layoutForNode.Left))
                    {
                        return Orientation.East;
                    }
                }
                throw new InvalidOperationException("Impossible execution path. Unexpected relative positioning.");
            }

            /// Partitions the children of this EInner node into two partitions.
            /// Orientation is North or South:
            ///    firstPartition = children to the left
            ///    secondPartition = children to the right
            /// Orientation is East or West:
            ///    firstPartition = children above
            ///    secondPartition = children below
            (List<ENode> firstPartition, List<ENode> secondPartition) Partition(Orientation orientation)
            {
                List<ENode> first = new();
                List<ENode> second = new();

                ILayoutNode thisNode = GetLayoutNode(Name);

                foreach (ENode child in children.Where(enode => enode.ContainedIn(existingNodes)))
                {
                    ILayoutNode childNode = GetLayoutNode(child.Name);
                    NodeTransform layoutForChild = lastLayout[childNode];

                    if (orientation == Orientation.North || orientation == Orientation.South)
                    {
                        if (layoutForChild.CenterPosition.x < thisNode.CenterPosition.x)
                        {
                            first.Add(child);
                        }
                        else
                        {
                            second.Add(child);
                        }
                    }
                    else
                    {
                        if (layoutForChild.CenterPosition.z > thisNode.CenterPosition.z)
                        {
                            first.Add(child);
                        }
                        else
                        {
                            second.Add(child);
                        }
                    }
                }
                return (first, second);
            }

            /// Sorts partition in the order indicated by ascending using
            /// the elements' position in the previous layout.
            /// If ascending is false, the order is descending.
            /// If isXaxis is true, the X co-ordinate of the previous layout
            /// is used, otherwise the Z co-ordindate.
            void Sort(List<ENode> partition, bool ascending, bool isXaxis)
            {
                if (ascending)
                {
                    partition.Sort((left, right) => CompareTo(left, right));
                }
                else
                {
                    partition.Sort((left, right) => CompareTo(right, left));
                }

                /// left belongs before right => -1
                /// left and right are equal in terms of ordering => 0
                /// left belongs after right => 1
                int CompareTo(ENode left, ENode right)
                {
                    ILayoutNode leftLayout = GetLayoutNode(left.Name);
                    ILayoutNode rightLayout = GetLayoutNode(right.Name);
                    float l = isXaxis ? leftLayout.CenterPosition.x : leftLayout.CenterPosition.z;
                    float r = isXaxis ? rightLayout.CenterPosition.x : rightLayout.CenterPosition.z;
                    if (l < r)
                    {
                        return -1;
                    }
                    else if (l > r)
                    {
                        return 1;
                    }
                    Debug.LogError($"Nodes {left.Name} and {right.Name} have the same position.\n");
                    return left.Name.CompareTo(right.Name);
                }
            }
        }

        /// <summary>
        /// First prints <paramref name="message"/> and then dumps all <paramref name="nodes"/>.
        /// </summary>
        /// <param name="message">Message to be printed first.</param>
        /// <param name="nodes">Nodes to be dumped.</param>
        /// <remarks>Used for debugging.</remarks>
        private static void Dump(string message, List<ENode> nodes)
        {
            Debug.Log(message + "[\n");
            foreach (ENode node in nodes)
            {
                Debug.Log($"   {node.Name}\n");
            }
            Debug.Log("]\n");
        }

        /// <summary>
        /// Returns the maximal length of all <paramref name="children"/> that are on the <paramref name="left"/>
        /// side (i.e, for which <see cref="Left"/> equals <paramref name="left"/> holds). If <paramref name="width"/>
        /// is true, <see cref="Rectangle.Width"/> will be used as the length; otherwise <see cref="Rectangle.Depth"/>.
        /// </summary>
        /// <param name="children">The children for which to determine the maximal length.</param>
        /// <param name="left">If true, only left children are considered; otherwise only right children.</param>
        /// <param name="width">If true, the maximum of <see cref="Rectangle.Width"/> will be returned, otherwise
        /// the maximum of <see cref="Rectangle.Depth"/> of the <paramref name="children"/> to be considered.</param>
        /// <returns>Maximal length of <paramref name="children"/>.</returns>
        private static float Max(IList<ENode> children, bool left, bool width)
        {
            return children.Where(child => child.Left == left)
                           .Select(child => width ? child.Rectangle.Width : child.Rectangle.Depth)
                           .Prepend(0).Max();
        }

        /// <summary>
        /// Returns the inverted <paramref name="orientation"/>.
        /// </summary>
        /// <param name="orientation">Orientation to be inverted.</param>
        /// <returns>Inverted <paramref name="orientation"/>.</returns>
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
        /// <param name="value">Location to be moved.</param>
        /// <param name="by">The distance of the movement.</param>
        /// <param name="orientation">The direction of the movement.</param>
        /// <returns>Moved location.</returns>
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
        /// <paramref name="centerLocation"/> based on <paramref name="orientation"/>.
        /// Sets the center location of the <see cref="street"/>.
        ///
        /// Only the final positions are set, sizes are not touched.
        /// </summary>
        /// <param name="orientation">The orientation of this node.</param>
        /// <param name="centerLocation">Center location to be set.</param>
        public override void SetLocation(Orientation orientation, Location centerLocation)
        {
            bool horizontal = orientation == Orientation.East || orientation == Orientation.West;
            Rectangle.Center = centerLocation;
            if (horizontal)
            {
                street.Center.X = Rectangle.Center.X;
                /// We have set <see cref="Rectangle.Center.Y"/> as a relative value in
                /// <see cref="SetSizeAndDistribute(Orientation, LayoutDescriptor, Dictionary{ILayoutNode, NodeTransform}, ILayoutNodeSet, ILayoutNodeSet)"/>.
                street.Center.Y += Rectangle.Center.Y - Rectangle.Depth / 2;
            }
            else
            {
                street.Center.Y = Rectangle.Center.Y;
                /// We have set <see cref="Rectangle.Center.X"/> as a relative value in
                /// <see cref="SetSizeAndDistribute(Orientation, LayoutDescriptor, Dictionary{ILayoutNode, NodeTransform}, ILayoutNodeSet, ILayoutNodeSet)"/>.
                street.Center.X += Rectangle.Center.X - Rectangle.Width / 2;
            }
            Location origin = MoveTo(street.Center, Length(orientation) / 2, Invert(orientation));

            float streetExtent = (horizontal ? street.Depth : street.Width) / 2;
            foreach (ENode child in children)
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
        /// Adds the layout information of this <see cref="EInner"/> to the <paramref name="layoutResult"/>.
        ///
        /// Unlike <see cref="ELeaf.ToLayout(ref Dictionary{ILayoutNode, NodeTransform}, float, float)"/>, this
        /// method adds the data from <see cref="street"/> because that rectangle is used to depict an inner
        /// node. The attribute <see cref="Rectangle"/> is just the area enclosing this street and all
        /// representations of the descendants of this node.
        ///
        /// This method recurses into the <see cref="children"/>.
        /// </summary>
        /// <param name="layoutResult">Layout where to add the layout information.</param>
        /// <param name="streetHeight">The height of an inner node (depicted as street).
        /// Will be used for the height of the node.</param>
        public override void ToLayout(ref Dictionary<ILayoutNode, NodeTransform> layoutResult, float streetHeight)
        {
            layoutResult[GraphNode]
                = new NodeTransform(street.Center.X, street.Center.Y,
                                    new Vector3(street.Width, streetHeight, street.Depth),
                                    0);
            foreach (ENode child in children)
            {
                child.ToLayout(ref layoutResult, streetHeight);
            }
        }
    }
}
