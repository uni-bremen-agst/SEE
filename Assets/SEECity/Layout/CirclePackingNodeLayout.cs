using SEE.DataModel;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// This layout packs circles closely together as a set of nested circles to decrease 
    /// the total area of city.
    /// </summary>
    public class CirclePackingNodeLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        public CirclePackingNodeLayout(float groundLevel, NodeFactory leafNodeFactory) 
            : base(groundLevel, leafNodeFactory)
        {
            name = "Circle Packing";
        }

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        Dictionary<GameObject, NodeTransform> layout_result;

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            layout_result = new Dictionary<GameObject, NodeTransform>();

            List<Node> roots = GetRoots(gameNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root node.");
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }
            to_game_node = NodeMapping(gameNodes);
            Node root = roots[0];
            float out_radius = PlaceNodes(root);
            Vector3 position = new Vector3(0.0f, groundLevel, 0.0f);
            layout_result[to_game_node[root]] = new NodeTransform(position,
                                                                  GetScale(to_game_node[root], out_radius));
            MakeGlobal(position, root.Children());
            to_game_node = null;
            return layout_result;
        }

        /// <summary>
        /// "Globalizes" the layout. Initially, the position of children are relative to
        /// their parent assuming that the parent has position Vector3.zero. This 
        /// function adjusts the co-ordinates of all nodes to the world's co-ordinates.
        /// We also adjust the ground level of each inner node by its level lift.
        /// </summary>
        /// <param name="position">the position of the parent of all children</param>
        /// <param name="children">the children to be laid out</param>
        private void MakeGlobal(Vector3 position, List<Node> children)
        {
            foreach (Node child in children)
            {
                GameObject childObject = to_game_node[child];
                NodeTransform childTransform = layout_result[childObject];
                if (! child.IsLeaf())
                {
                    // The inner nodes will be slightly lifted along the y axis according to their
                    // tree depth so that they can be stacked visually (level 0 is at the bottom).
                    position.y += LevelLift(child);
                }
                childTransform.position += position;
                layout_result[childObject] = childTransform;
                MakeGlobal(childTransform.position, child.Children());
            }
        }

        /// <summary>
        /// Places all children of the given parent node (recursively for all descendants
        /// of the given parent).
        /// </summary>
        /// <param name="parent">node whose descendants are to be placed</param>
        /// <returns>the radius required for a circle represent parent</returns>
        private float PlaceNodes(Node parent)
        {
            List<Node> children = parent.Children();

            if (children.Count == 0)
            {
                // No scaling for leaves because they are already scaled.
                // Position Vector3.zero because they are located relative to their parent.
                // This position may be overridden later in the context of parent's parent.
                //layout[to_game_node[parent]] = new NodeTransform(Vector3.zero, Vector3.one);
                return LeafRadius(to_game_node[parent]);
            }
            else
            { 
                List<MyCircle> circles = new List<MyCircle>(children.Count);

                int i = 0;
                foreach (Node child in children)
                {
                    GameObject childObject = to_game_node[child];

                    float radius = child.IsLeaf() ? LeafRadius(childObject) : PlaceNodes(child);
                    // Position the children on a circle as required by CirclePacker.Pack.
                    float radians = ((float)i / (float)children.Count) * (2.0f * Mathf.PI);
                    circles.Add(new MyCircle(childObject, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
                    i++;
                }

                // The co-ordinates returned in circles are local, that is, relative to the zero center.
                // The resulting center and out_outer_radius relate to the circle object comprising
                // the children we just packed. By necessity, the objects whose children we are
                // currently processing is a composed object represented by a circle, otherwise
                // we would not have any children here.
                MyCirclePacker.Pack(circles, out float out_outer_radius);

                foreach (MyCircle circle in circles)
                {
                    // Note: The position of the transform is currently only local, relative to the zero center
                    // within the parent node. The co-ordinates will later be adjusted to the world scope.
                    layout_result[circle.gameObject] 
                         = new NodeTransform(new Vector3(circle.center.x, groundLevel, circle.center.y),
                                             GetScale(circle.gameObject, circle.radius));
                }
                return out_outer_radius;
            }
        }

        /// <summary>
        /// Returns the scaling vector for given node. If the node is a leaf, it will be the node's original size
        /// because leaves are not scaled at all. We do not want to change their size. Its predetermined
        /// by the client of this class. If the node is not a leaf, it will be represented by a circle
        /// whose scaling in x and y axes is twice the given radius (we do not scale along the y axis,
        /// hence, co-ordinate y of the resulting vector is always circleHeight.
        /// </summary>
        /// <param name="node">game node whose size is to be determined</param>
        /// <param name="radius">the radius for the game node if it is an inner node</param>
        /// <returns>the scale of <paramref name="node"/></returns>
        private Vector3 GetScale(GameObject node, float radius)
        {
            Node n = node.GetComponent<NodeRef>().node;
            return n.IsLeaf() ? leafNodeFactory.GetSize(node) 
                              : new Vector3(2 * radius, innerNodeHeight, 2 * radius);
        }

        /// <summary>
        /// Yields the radius of the minimal circle containing the given block.
        /// 
        /// Precondition: node must be a leaf node, a block generated by blockFactory.
        /// </summary>
        /// <param name="block">block whose radius is required</param>
        /// <returns>radius of the minimal circle containing the given block</returns>
        private float LeafRadius(GameObject block)
        {
            Vector3 extent = leafNodeFactory.GetSize(block) / 2.0f;
            return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
        }
    }
}
