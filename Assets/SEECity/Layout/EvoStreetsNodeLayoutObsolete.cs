using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{

    enum Orientation
    {
        North,
        East,
        South,
        West
    }

    static class OrientationMethods
    {
        public static Orientation Opposite(this Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.North:
                    return Orientation.South;
                case Orientation.South:
                    return Orientation.North;
                case Orientation.East:
                    return Orientation.West;
                case Orientation.West:
                    return Orientation.East;
                default:
                    throw new Exception("Unhandled orientation: " + orientation.ToString());
            }
        }
    }

    [Obsolete("Use EvoStreetsNodeLayout instead.")]
    public class EvoStreetsNodeLayoutObsolete : NodeLayout
    {
        public EvoStreetsNodeLayoutObsolete(float groundLevel, NodeFactory leafNodeFactory, InnerNodeFactory innerNodeFactory) 
            : base(groundLevel, leafNodeFactory)
        {
            this.innerNodeFactory = innerNodeFactory;
        }

        /// <summary>
        /// The factory that was used to create inner nodes.
        /// </summary>
        private readonly InnerNodeFactory innerNodeFactory;

        /// <summary>
        /// The set of children of each node. This is a subset of the node's children
        /// in the graph, limited to the children for which a layout is requested.
        /// </summary>
        private Dictionary<Node, List<Node>> children;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        Dictionary<GameObject, NodeTransform> layout_result = new Dictionary<GameObject, NodeTransform>();

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            if (gameNodes.Count == 0)
            {
                throw new Exception("No nodes to be laid out.");
            }
            else if (gameNodes.Count == 1)
            {
                GameObject gameNode = gameNodes.GetEnumerator().Current;
                layout_result[gameNode] = new NodeTransform(Vector3.zero, gameNode.transform.localScale);
            }
            else
            {
                to_game_node = NodeMapping(gameNodes);
                /// The roots of the subtrees of the original graph that are to be laid out.
                /// A node is considered a root if it has either no parent in the original
                /// graph or its parent is not contained in the set of nodes to be laid out.
                CreateTree(to_game_node.Keys, out List<Node> roots, out children);
                if (roots.Count == 0)
                {
                    throw new Exception("Subgraph has no root.");
                }
                LayoutTree(roots);
            }
            return layout_result;
        }

        /// <summary>
        /// The maximal depth of the tree set by LayoutTree(List<Node>).
        /// Is at least 1, if there is root node.
        /// </summary>
        private int maxDepth;

        private void LayoutTree(List<Node> roots)
        {
            maxDepth = MaxDepth(roots, children);
            if (roots.Count == 1)
            {
                LayoutTree(new Location(Vector2.zero, Corner.SouthWest), roots[0], Orientation.East, 1);
            }
            else
            {
                throw new Exception("Subgraph has multiple roots.");
                // FIXME: We might want to support multiple roots.
                //foreach (Node root in roots)
                //{
                //    LayoutTree(root, children, false);
                //}
            }
        }


        private enum Corner
        {
            NorthEast, 
            SouthEast,
            SouthWest,
            NorthWest,
        }

        private struct Location
        {
            public Location(Vector2 position, Corner corner)
            {
                this.position = position;
                this.corner = corner;
            }
            public Location(float x, float y, Corner corner) 
                : this(new Vector2(x, y), corner)
            {
            }
            public Vector2 position;
            public Corner corner;
        }

        private Vector2 LayoutTree(Location location, Node node, Orientation orientation, int depth)
        {
            GameObject gameNode = to_game_node[node];
            if (node.IsLeaf())
            {
                NodeTransform nodeTransform = ToNodeTransform(gameNode, location);
                layout_result[gameNode] = nodeTransform;
                return new Vector2(nodeTransform.scale.x, nodeTransform.scale.z);
            }
            else
            {
                // We are dealing with an inner node that is to be visualized as a street.

                // Half of the road wide (this may be the half the width or depth of the street game object
                // depending upon whether the direction of the street is oriented towards North/South or West/East.
                // This value will remain constant. We need to add 1 to the difference of maxDepth - depth,
                // because otherwise the term would be 0. We divide by 2 because the extent is half the
                // road wide.
                float roadExtent = (maxDepth - depth + 1) * 0.01f / 2.0f;

                // A street is either horizontal (orientation East or West) or vertical (orientation North or South).
                // The direction of travel is defined by the orientation. The position where we start
                // is defined by the location. This position could be any corner of the street. We
                // want to travel along the center of the street, hence, need to adjust our starting position.
                Vector2 origin = CentralStrip(location, orientation, roadExtent);

                // When we travel along the street, we can define the area aside of us in left and right
                // relative to our direction of travel. To put it differently, we have two vehicles on the
                // street traveling parallel to each other: the left and right vehicle relative to the 
                // direction of travel. We try to make sure that they stay as close to each other as
                // possible. Both vehicles place game objects when they move, the left vehicle on the
                // left side and the right vehicle on the right side. Whenever they placed an object
                // at the location they currently are (which determines one corner, A, of the object), they 
                // will move forward to the next corner of the object, B, at the street. A is at the
                // current position of the vehicle plus roadExtent (remember we are traveling on the center
                // line of the street).

                // The distance of the current street position relative to the origin for the right area
                // (the right vehicle).
                float rightLaneDistance = 0.0f;

                // The distance of the current street position relative to the origin 0 for the left area
                // (the left vehicle).
                float leftLaneDistance = 0.0f;

                // The maximal length of the children on the right side.
                float maxRightChildLength = 0.0f;

                // The maximal length of the children on the left side.
                float maxLeftChildLength = 0.0f;

                foreach (Node child in children[node])
                {
                    if (rightLaneDistance <= leftLaneDistance)
                    {
                        // right vehicle must catch up
                        MoveOnRightLane(child, origin, ref rightLaneDistance, ref maxRightChildLength, orientation, roadExtent, depth);
                    }
                    else
                    {
                        // left vehicle must catch up
                        MoveOnLeftLane(child, origin, ref leftLaneDistance, ref maxLeftChildLength, orientation, roadExtent, depth);
                    }
                }
                float roadLength = Mathf.Max(leftLaneDistance, rightLaneDistance);

                // Now that we have placed all children we know the length of the street
                // and can set its location and scale in the layout.
                // Preserve the original height of the street.
                float originalHeight = innerNodeFactory.GetSize(gameNode).y;
                Vector3 scale = orientation == Orientation.North || orientation == Orientation.South ?
                                  new Vector3(2 * roadExtent, originalHeight, roadLength)
                                : new Vector3(roadLength, originalHeight, 2 * roadExtent);
                Vector3 position = StreetCenter(origin, roadLength, orientation);
                layout_result[gameNode] = new NodeTransform(position, scale);

                // The total size is always in absolute world co-ordinates.
                Vector2 totalSize = orientation == Orientation.North || orientation == Orientation.South ?
                                      new Vector2(roadLength, 2 * roadExtent + maxRightChildLength + maxLeftChildLength)
                                    : new Vector2(2 * roadExtent + maxRightChildLength + maxLeftChildLength, roadLength);
                return totalSize;
            }
        }

        private Vector3 StreetCenter(Vector2 origin, float roadLength, Orientation orientation)
        {
            float halfRoadLength = roadLength / 2.0f;

            switch (orientation)
            {
                case Orientation.East:
                    return new Vector3(origin.x + halfRoadLength, groundLevel, origin.y);
                case Orientation.West:
                    return new Vector3(origin.x - halfRoadLength, groundLevel, origin.y);
                case Orientation.North:
                    return new Vector3(origin.x, groundLevel, origin.y + halfRoadLength);
                case Orientation.South:
                    return new Vector3(origin.x, groundLevel, origin.y - halfRoadLength);
                default:
                    throw new Exception("Unhandled orientation " + orientation);
            }
        }

        private Vector2 CentralStrip(Location location, Orientation orientation, float roadExtent)
        {
            Vector2 result = location.position;
            switch (location.corner)
            {
                case Corner.SouthWest:
                    if (orientation == Orientation.North || orientation == Orientation.South)
                    {
                        result.x += roadExtent;
                    }
                    else
                    {
                        result.y += roadExtent;
                    }
                    break;
                case Corner.SouthEast:
                    if (orientation == Orientation.North || orientation == Orientation.South)
                    {
                        result.x -= roadExtent;
                    }
                    else
                    {
                        result.y += roadExtent;
                    }
                    break;
                case Corner.NorthEast:
                    if (orientation == Orientation.North || orientation == Orientation.South)
                    {
                        result.x -= roadExtent;
                    }
                    else
                    {
                        result.y -= roadExtent;
                    }
                    break;
                case Corner.NorthWest:
                    if (orientation == Orientation.North || orientation == Orientation.South)
                    {
                        result.x += roadExtent;
                    }
                    else
                    {
                        result.y -= roadExtent;
                    }
                    break;
                default:
                    throw new Exception("Unhandled location corner " + location.corner);
            }
            return result;
        }

        private void MoveOnRightLane
            (Node child,
             Vector2 origin, 
             ref float distanceFromOrigin,
             ref float maxChildLength,
             Orientation orientation,
             float roadExtent, 
             int depth)
        {
            Vector2 nextStop = Advance(origin, distanceFromOrigin, orientation);
            Location childLocation;
            // right is relative, we need an absolute orthogonal orientation for the child
            Orientation childOrientation;
            switch (orientation)
            {
                case Orientation.East:
                    childOrientation = Orientation.South;
                    childLocation = new Location(nextStop.x, nextStop.y - roadExtent, Corner.NorthWest);
                    break;
                case Orientation.West:
                    childOrientation = Orientation.North;
                    childLocation = new Location(nextStop.x, nextStop.y + roadExtent, Corner.SouthEast);
                    break;
                case Orientation.North:
                    childOrientation = Orientation.East;
                    childLocation = new Location(nextStop.x + roadExtent, nextStop.y, Corner.SouthWest);
                    break;
                case Orientation.South:
                    childOrientation = Orientation.West;
                    childLocation = new Location(nextStop.x - roadExtent, nextStop.y, Corner.NorthEast);
                    break;
                default:
                    throw new Exception("Unhandled orientation " + orientation);
            }
            Vector2 size = LayoutTree(childLocation, child, childOrientation, depth + 1);
            if (orientation == Orientation.North || orientation == Orientation.South)
            {
                distanceFromOrigin += size.y;
                if (size.x > maxChildLength)
                {
                    maxChildLength = size.x;
                }
            }
            else
            {
                distanceFromOrigin += size.x;
                if (size.y > maxChildLength)
                {
                    maxChildLength = size.y;
                }
            }
        }

        private void MoveOnLeftLane
            (Node child,
             Vector2 origin,
             ref float distanceFromOrigin,
             ref float maxChildLength,
             Orientation orientation,
             float roadExtent,
             int depth)
        {
            Vector2 nextStop = Advance(origin, distanceFromOrigin, orientation);
            Location childLocation;
            // left is relative, we need an absolute orthogonal orientation for the child
            Orientation childOrientation;
            switch (orientation)
            {
                case Orientation.East:
                    childOrientation = Orientation.North;
                    childLocation = new Location(nextStop.x, nextStop.y + roadExtent, Corner.SouthWest);
                    break;
                case Orientation.West:
                    childOrientation = Orientation.South;
                    childLocation = new Location(nextStop.x, nextStop.y - roadExtent, Corner.NorthEast);
                    break;
                case Orientation.North:
                    childOrientation = Orientation.West;
                    childLocation = new Location(nextStop.x - roadExtent, nextStop.y, Corner.SouthEast);
                    break;
                case Orientation.South:
                    childOrientation = Orientation.East;
                    childLocation = new Location(nextStop.x + roadExtent, nextStop.y, Corner.NorthWest);
                    break;
                default:
                    throw new Exception("Unhandled orientation " + orientation);
            }
            Vector2 size = LayoutTree(childLocation, child, childOrientation, depth + 1);
            if (orientation == Orientation.North || orientation == Orientation.South)
            {
                distanceFromOrigin += size.y;
                if (size.x > maxChildLength)
                {
                    maxChildLength = size.x;
                }
            }
            else
            {
                distanceFromOrigin += size.x;
                if (size.y > maxChildLength)
                {
                    maxChildLength = size.y;
                }
            }
        }

        private Vector2 Advance(Vector2 origin, float distanceFromOrigin, Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.East:
                    return new Vector2(origin.x + distanceFromOrigin, origin.y);
                case Orientation.West:
                    return new Vector2(origin.x - distanceFromOrigin, origin.y);
                case Orientation.North:
                    return new Vector2(origin.x, origin.y + distanceFromOrigin);
                case Orientation.South:
                    return new Vector2(origin.x, origin.y - distanceFromOrigin);
                default:
                    throw new Exception("Unhandled orientation " + orientation);
            }
        }

        private NodeTransform ToNodeTransform(GameObject gameNode, Location location)
        {
            // preserve the original size of the leaf
            Vector3 extent = leafNodeFactory.GetSize(gameNode) / 2.0f;
            // The position in the layout must be the center of the leaf, hence,
            // we need to adjust the given position accordingly.

            Vector3 adjustedPosition;
            adjustedPosition.y = groundLevel;
            switch (location.corner)
            {
                case Corner.SouthWest:
                    adjustedPosition.x = location.position.x + extent.x;
                    adjustedPosition.z = location.position.y + extent.z;
                    break;
                case Corner.SouthEast:
                    adjustedPosition.x = location.position.x - extent.x;
                    adjustedPosition.z = location.position.y + extent.z;
                    break;
                case Corner.NorthEast:
                    adjustedPosition.x = location.position.x - extent.x;
                    adjustedPosition.z = location.position.y - extent.z;
                    break;
                case Corner.NorthWest:
                    adjustedPosition.x = location.position.x + extent.x;
                    adjustedPosition.z = location.position.y - extent.z;
                    break;
                default:
                    throw new Exception("Unhandled location corner " + location.corner);
            }
            return new NodeTransform(adjustedPosition, gameNode.transform.localScale);
        }

        /// <summary>
        /// 
        /// The interpetation of position is as follows:
        /// left lower corner  if   horizontal &&   onLeftLane => street grows towards North
        /// left upper corner  if   horizontal && ! onLeftLane => street grows towards South 
        /// right lower corner if ! horizontal &&   onLeftLane => street grows towards West
        /// left lower corner  if ! horizontal && ! onLeftLane => street grows towards East
        /// </summary>
        /// <param name="location">the position on the street this node should be placed; its
        /// exact interpretation depends upon horizontal and onLeftLane</param>
        /// <param name="node">the node to be placed</param>
        /// <param name="horizontal">whether the street stretches horizontally</param>
        /// <param name="onLeftLane">whether the node should be put on the left lane of the street
        /// it is contained in (this means above the street if horizontal or left from the street if 
        /// not horizontal)</param>
        /// <param name="depth">the depth of the node in the tree (a root has depth 1)</param>
        /// <returns>the bounding box of the rectangle that occupies the space for the node</returns>
        private Vector2 LayoutTree(Vector2 position, Node node, bool horizontal, bool onLeftLane, int depth)
        {
            GameObject gameNode = to_game_node[node];
            if (node.IsLeaf())
            {
                // preserve the original size of the leaf
                Vector3 leafExtent = leafNodeFactory.GetSize(gameNode) / 2.0f;
                // The position in the layout must be the center of the leaf, hence,
                // we need to adjust the given position accordingly.
                Vector3 adjustedPosition = CenterPosition(leafExtent, position, horizontal, onLeftLane);
                layout_result[gameNode] = new NodeTransform(adjustedPosition, leafExtent);
                return new Vector2(leafExtent.x, leafExtent.z);
            }
            else
            {
                // How wide half the road is (this may be the half the width or depth of the street game object
                // depending upon whether the direction of the street is horizontal or vertical.
                // This value will remain constant. We need to add 1 to the difference of maxDepth - depth,
                // because otherwise the term would be 0. We devide by 2 because the extent is half the
                // road wide.
                float roadExtent = (maxDepth - depth + 1) * 0.01f / 2.0f;

                // The distance of the current street position relative to the origin 0 for the right lane.
                // The right lane of the road is below the left lane if the road is directed horizontally
                // and left from left lane if the road is directed vertically. The value may be negative
                // if we travel towards West or South.
                float rightLaneDistance = 0.0f;

                // The distance of the current street position relative to the origin 0 for the left lane.
                float leftLaneDistance = 0.0f;

                // Normalize position to center of the lane.
                AdjustToCenterLane(ref position, roadExtent, horizontal, onLeftLane);

                Vector2 boundingBox = Vector2.zero;

                // Distribute the children up/down if horizontal or left/right if not horizontal
                // at the street so that the space is evenly distributed.
                foreach (Node child in children[node])
                {  
                    if (leftLaneDistance <= rightLaneDistance)
                    {
                        // add child on the left lane of the street
                        bool childIsOnLeftLane = true;
                        Vector2 childPosition = ChildPosition(position, leftLaneDistance, roadExtent, horizontal, childIsOnLeftLane);
                        Vector2 childArea = LayoutTree(childPosition, child, !horizontal, childIsOnLeftLane, depth + 1);
                        leftLaneDistance += horizontal ? childArea.x : childArea.y;
                    }
                    else
                    {
                        // add child on the right lane of the street
                        bool childIsOnLeftLane = false;
                        Vector2 childPosition = ChildPosition(position, rightLaneDistance, roadExtent, horizontal, childIsOnLeftLane);
                        Vector2 childArea = LayoutTree(childPosition, child, !horizontal, childIsOnLeftLane, depth + 1);
                        rightLaneDistance += horizontal ? childArea.x : childArea.y;
                    }
                }
                // All children are now placed on the street.
                // The length of the road (again its concrete x or z interpretation depends upon horizontal).
                float roadLength = Mathf.Max(leftLaneDistance, rightLaneDistance);

                // Preserve the original height of the street.
                float originalHeight = innerNodeFactory.GetSize(gameNode).y;
                Vector3 scale = StreetScale(roadLength, originalHeight, 2 * roadExtent, horizontal);

                layout_result[gameNode] = new NodeTransform(AdjustToCenterLength(position, roadLength, horizontal, onLeftLane), scale);

                return boundingBox;
            }
        }

        /// <summary>
        /// Returns the center position of the street on ground level.
        /// 
        /// The interpetation of given street position is as follows:
        /// left lower corner  if   horizontal &&   onLeftLane => position.y += roadExtent
        /// left upper corner  if   horizontal && ! onLeftLane => position.y -= roadExtent
        /// right lower corner if ! horizontal &&   onLeftLane => position.x -= roadExtent
        /// left lower corner  if ! horizontal && ! onLeftLane => position.x += roadExtent
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="roadLength"></param>
        /// <param name="horizontal"></param>
        /// <param name="onLeftLane"></param>
        /// <returns></returns>
        private Vector3 AdjustToCenterLength(Vector2 position, float roadLength, bool horizontal, bool onLeftLane)
        {
            float roadHalfLength = roadLength / 2.0f;
            if (horizontal)
            {
                if (onLeftLane)
                {
                    return new Vector3(position.x + roadHalfLength, groundLevel, position.y);
                }
                else
                {
                    return new Vector3(position.x - roadHalfLength, groundLevel, position.y);
                }
            }
            else
            {
                if (onLeftLane)
                {
                    return new Vector3(position.x, groundLevel, position.y + roadHalfLength);
                }
                else
                {
                    return new Vector3(position.x, groundLevel, position.y - roadHalfLength);
                }
            }
        }

        /// <summary>
        /// Adjusts position's co-ordinates so that it represent the center of the two street
        /// lanes. 
        /// 
        /// The interpetation of given street position is as follows:
        ///   horizontal &&   onLeftLane => position.y -= roadExtent
        ///   horizontal && ! onLeftLane => position.y -= roadExtent
        /// ! horizontal &&   onLeftLane => position.x += roadExtent
        /// ! horizontal && ! onLeftLane => position.x -= roadExtent
        /// </summary>
        /// <param name="location">street position</param>
        /// <param name="roadExtent">the width of the road</param>
        /// <param name="horizontal">whether the the road is directed horizontally</param>
        /// <param name="onLeftLane">whether we are on left lane</param>
        private void AdjustToCenterLane(ref Vector2 position, float roadExtent, bool horizontal, bool onLeftLane)
        {
            if (horizontal)
            {
                if (onLeftLane)
                {
                    position.y -= roadExtent;
                }
                else
                {
                    position.y += roadExtent;
                }
            }
            else
            {
                if (onLeftLane)
                {
                    position.x += roadExtent;
                }
                else
                {
                    position.x -= roadExtent;
                }
            }
        }

        /// <summary>
        /// Returns the center point of the rectangle with given position and extent 
        /// (half its size). The y co-ordinate is always the ground level.
        /// 
        /// The interpetation of position is as follows:
        /// left lower corner  if   horizontal &&   onLeftLane
        /// left upper corner  if   horizontal && ! onLeftLane
        /// right lower corner if ! horizontal &&   onLeftLane
        /// left lower corner  if ! horizontal && ! onLeftLane
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="location"></param>
        /// <param name="horizontal"></param>
        /// <param name="onLeftLane"></param>
        /// <returns>center point</returns>
        private Vector3 CenterPosition(Vector3 extent, Vector2 position, bool horizontal, bool onLeftLane)
        {
            if (horizontal)
            {
                if (onLeftLane)
                {
                    return new Vector3(position.x + extent.x, groundLevel, position.y + extent.z);
                }
                else
                {
                    return new Vector3(position.x + extent.x, groundLevel, position.y - extent.z);
                }
            }
            else
            {
                if (onLeftLane)
                {
                    return new Vector3(position.x - extent.x, groundLevel, position.y + extent.z);
                }
                else
                {
                    return new Vector3(position.x + extent.x, groundLevel, position.y + extent.z);
                }
            }
        }

        /// <summary>
        /// Returns the position of a child to be put onto a street at given 
        /// origin position. The origin is assumed to be on the center of the
        /// two lanes.
        /// 
        /// The interpetation of the resulting child position is as follows:
        /// left lower corner  if   horizontal &&   onLeftLane
        /// left upper corner  if   horizontal && ! onLeftLane
        /// right lower corner if ! horizontal &&   onLeftLane
        /// left lower corner  if ! horizontal && ! onLeftLane
        /// </summary>
        /// <param name="location">the origin of the street </param>
        /// <param name="laneDistance">the distance from the origin to the resulting position</param>
        /// <param name="roadExtent">the offset from the road center where the child should be placed</param>
        /// <param name="horizontal">whether the road is horizontally directed</param>
        /// <param name="onLeftLane">whether the child should be put on the left lane of the road</param>
        /// <returns>position of a child</returns>
        private Vector2 ChildPosition(Vector2 position, float laneDistance, float roadExtent, bool horizontal, bool onLeftLane)
        {
            if (horizontal)
            {
                // We need to move to the right along the x axis.
                if (onLeftLane)
                {
                    return new Vector2(position.x + laneDistance, position.y + roadExtent);
                }
                else
                {
                    return new Vector2(position.x + laneDistance, position.y - roadExtent);
                }
            }
            else
            {
                // We need to move up along the logical y axis (this would be z in Unity's world).
                if (onLeftLane)
                {
                    return new Vector2(position.x - roadExtent, position.y + laneDistance);
                }
                else
                {
                    return new Vector2(position.x + roadExtent, position.y + laneDistance);
                }
            }
        }

        private Vector3 StreetScale(float roadLength, float height, float roadWidth, bool horizontal)
        {
            if (! horizontal)
            {
                float swap = roadLength;
                roadLength = roadWidth;
                roadWidth = swap;
            }
            return new Vector3(roadLength, height, roadWidth);
        }
    }
}
