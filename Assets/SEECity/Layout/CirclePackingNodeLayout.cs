using SEE.DataModel;
using SEEC.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CirclePackingNodeLayout : NodeLayout
    {
        public CirclePackingNodeLayout(float groundLevel, NodeFactory blockFactory) 
            : base(groundLevel, blockFactory)
        {
            name = "CirclePackingNode"; // FIXME: change to "CirclePacking".

            //MyCirclePacker.TestCirclePacker(); // FIXME: Remove this line.
        }

        Dictionary<GameObject, NodeTransform> layout;

        private static void Dump(Dictionary<GameObject, NodeTransform> layout, string message = "")
        {
            if (! string.IsNullOrEmpty(message))
            {
                Debug.Log(message + "\n");
            }
            foreach (var entry in layout)
            {
                Debug.LogFormat("node {0}: layout={1}\n", entry.Key.name, entry.Value.ToString());
            }
        }

        private static void Dump(List<MyCircle> circles, string message = "", string prefix = "")
        {
            string p = string.IsNullOrEmpty(message) ? "" : "[" + prefix + "] ";
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log(p + message + "\n");
            }
            int i = 0;
            foreach (var circle in circles)
            {
                Debug.LogFormat(p + "circle {0}: {1}\n", i, circle.ToString());
                i++;
            }
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            layout = new Dictionary<GameObject, NodeTransform>();

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
            float out_radius = DrawNodes(root);
            Vector3 position = new Vector3(0.0f, groundLevel, 0.0f);
            layout[to_game_node[root]] = new NodeTransform(position,
                                                           GetScale(to_game_node[root], out_radius));
            //Dump(layout, "BEFORE");
            MakeGlobal(position, root.Children());
            //Dump(layout, "AFTER");
            return layout;
        }

        /// <summary>
        /// "Globalizes" the layout. Initially, the position of children are relative to
        /// their parent assuming that the parent has position Vector3.zero. This 
        /// function adjusts the co-ordinates of all nodes to the world's co-ordinates.
        /// </summary>
        /// <param name="position">the position of the parent of all children</param>
        /// <param name="children">the children to be laid out</param>
        private void MakeGlobal(Vector3 position, List<Node> children)
        {
            foreach (Node child in children)
            {
                GameObject childObject = to_game_node[child];
                NodeTransform childTransform = layout[childObject];
                childTransform.position += position;
                layout[childObject] = childTransform;
                MakeGlobal(childTransform.position, child.Children());
            }
        }

        private float DrawNodes(Node parent)
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

                    float radius = child.IsLeaf() ? LeafRadius(childObject) : DrawNodes(child);
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
                Dump(circles, "BEFORE for " + to_game_node[parent].name, "PACK");
                MyCirclePacker.Pack(circles, out float out_outer_radius);
                Dump(circles, "AFTER for " + to_game_node[parent].name, "PACK");

                //layout[to_game_node[parent]] = new NodeTransform(new Vector3(center.x, groundLevel, center.y), 
                //                                                 GetScale(to_game_node[parent], out_outer_radius));

                foreach (MyCircle circle in circles)
                {
                    // Note: The position of the transform is currently only local, relative to the zero center
                    // within the parent node. The co-ordinates will later be adjusted to the world scope.
                    layout[circle.gameObject] = new NodeTransform(new Vector3(circle.center.x, groundLevel, circle.center.y),
                                                                  GetScale(circle.gameObject, circle.radius));
                }
                return out_outer_radius;
            }
        }

        private const float circleHeight = 0.1f;

        /// <summary>
        /// Returns the scaling vector for given node. If the node is a leaf, it will be the node's original size
        /// because leaves are not scaled at all. We do not want to change their size. Its predetermined
        /// by the client of this class. If the node is not a leaf, it will be represented by a circle
        /// whose scaling in x and y axes is twice the given radius (we do not scale along the y axis,
        /// hence, co-ordinate y of the resulting vector is always circleHeight.
        /// </summary>
        /// <param name="node">game node whose size is to be determined</param>
        /// <param name="radius">the radius for the game node if it is an inner node</param>
        /// <returns></returns>
        private Vector3 GetScale(GameObject node, float radius)
        {
            Node n = node.GetComponent<NodeRef>().node;
            // FIXME: Do we need multiply radius by Unit()?
            return n.IsLeaf() ? blockFactory.GetSize(node) : new Vector3(2 * radius, circleHeight, 2 * radius);
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
            Vector3 extent = blockFactory.GetSize(block) / 2.0f;
            return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
        }
    }
}
