using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class BalloonNodeLayout : NodeLayout
    {
        public BalloonNodeLayout(float groundLevel, BlockFactory blockFactory) 
            : base(groundLevel, blockFactory)
        {
            name = "BalloonNode"; // FIXME: change to "Balloon".
        }

        /// <summary>
        /// Returns all root graph nodes within gameNodes.
        /// </summary>
        /// <param name="gameNodes">game nodes for which to determine root nodes</param>
        /// <returns>all root graph nodes within gameNodes</returns>
        private static List<Node> GetRoots(ICollection<GameObject> gameNodes)
        {
            List<Node> roots = new List<Node>();

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().node;
                if (node.IsRoot())
                {
                    roots.Add(node);
                }
            }
            return roots;
        }

        /// <summary>
        /// The maximal depth of the node hierarchy.
        /// </summary>
        private int maxDepth = 0;

        /// <summary>
        /// Information about a node necessary to draw it.
        /// </summary>
        private struct NodeInfo
        {
            public readonly float radius;
            public readonly float outer_radius;
            public readonly float reference_length_children;
            // The level of node, that is, the distance from the root node to this node.
            // The root node has always level 0. 
            public readonly int level;

            public NodeInfo(float radius, int level, float outer_radius, float reference_length_children)
            {
                this.radius = radius;
                this.outer_radius = outer_radius;
                this.reference_length_children = reference_length_children;
                this.level = level;
            }
        }

        /// <summary>
        /// A mapping of nodes onto their circle data.
        /// </summary>
        private Dictionary<Node, NodeInfo> nodeInfos = new Dictionary<Node, NodeInfo>();

        private Dictionary<GameObject, NodeTransform> layout_result;

        private Dictionary<Node, GameObject> to_game_node;

        private static Dictionary<Node, GameObject> NodeMapping(ICollection<GameObject> gameNodes)
        {
            Dictionary<Node, GameObject> map = new Dictionary<Node, GameObject>();
            foreach (GameObject gameNode in gameNodes)
            {
                Node node = gameNode.GetComponent<NodeRef>().node;
                map[node] = gameNode;
            }
            return map;
        }

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            // puts the outermost circles of the roots next to each other;
            // later we might use a circle-packing algorithm instead,
            // e.g., https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp

            layout_result = new Dictionary<GameObject, NodeTransform>();
            to_game_node = NodeMapping(gameNodes);

            const float offset = 1.0f;
            List<Node> roots = GetRoots(gameNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root nodes.");
            }
            int[] max_depths = new int[roots.Count];

            // the maximal radius over all root circles; required to create the plane underneath
            float max_radius = 0.0f;

            // first calculate all radii including those for the roots
            {
                int i = 0;
                foreach (Node root in roots)
                {
                    CalculateRadius2D(root, 0, out float out_rad, out int max_depth_of_this_tree);
                    max_depths[i] = max_depth_of_this_tree;
                    i++;
                    if (out_rad > max_radius)
                    {
                        max_radius = out_rad;
                    }
                    if (max_depth_of_this_tree > maxDepth)
                    {
                        maxDepth = max_depth_of_this_tree;
                    }
                }
            }

            // now we know the minimal distance between two subsequent roots so that
            // their outer circles do not overlap
            {
                Vector3 position = Vector3.zero;
                int i = 0;
                foreach (Node root in roots)
                {
                    // for two neighboring circles the distance must be the sum of the their two radii;
                    // in case we draw the very first circle, no distance must be kept
                    position.x += i == 0 ? 0.0f : nodeInfos[roots[i - 1]].outer_radius + nodeInfos[roots[i]].outer_radius + offset;
                    DrawCircles(root, position, 0, max_depths[i]);
                    i++;
                }
            }

            // FIXME: define layout_result
            to_game_node = null;
            return layout_result;
        }

        // Concepts
        //
        // We associate each node i with its disc D_i around which its children
        // are placed. Let cp_i be the center and rad_i be the radius of D_i,
        // respectively. Let outer-radius out_rad_i be the minimum radious of the
        // circle which covers all the descendants of i when they are mapped onto
        // the same plane where D_i lies. Let outer-disc outer_disc_i for node i
        // be a disc with radius equal to out_rad_i (this is the disc containing
        // i at its center and the disc of all its desecendants).
        //
        // We define a reference point rp_i for node i as the intersection
        // point between the vertical line and horizontal line passing
        // through cp_i and i respectively, and an apex point as the point
        // which lies on the vertical line between rp_i and cp_i.
        //
        // We define apex height ah_i and reference height rh_i as the vertical
        // heights of ap_i and rp_i from cp_i, respectively. We define 
        // reference length rl_i as the length between i and rp_i. Each node
        // is associated with its attribute set AT_i = {rl_i, ah_i, rh_i},
        // which consists of reference length, apex, and reference height.
        //
        // We assume that for each hierarchical edge (i, j) in the tree, i is the
        // parent of j, and the root of the tree is at level 0.
        //
        // A reconfigurable disc tree (RDT) is a tree T(N, E) with each edge
        // (i, j) consisting of polylines [(i, rp_i), (rp_i, ap_i), (ap_i, j)]
        // where each length can be changed.
        //
        // We define a 3D RDT as one with zero reference length rl_i, and a 2D
        // RDT with non-zero reference length rl_i and zero reference height
        // rh_i for every node i, respectively. 
        //
        // Note that each node i in the 3D RDT is identical to its reference
        // point r_i, and each center point cp_i in the 2D RDT is identical to
        // its reference point rp_i (i.e., rp_i = cp_i).
        //
        // A 3D RDG can change its shape into a disc tree or a compact disc tree
        // by changing apex and reference height as follows. A disc tree is a
        // 3D RDT with zero apex and non-zero reference heights for each node. 
        // A compact disc tree is a 3D RDT with both of reference and apex heights 
        // equal to zero for each node at odd levels and with the identical apex
        // and reference heights for each node at even levels. A plane disc tree is
        // a 2D RDT which lies on the plane.
        // 
        // To draw a node i, cp_i, rad_i, and out_rad_i need to be known. The disc with 
        // radius rad_i and center point cp_i will be left empty. A circle will be drawn
        // with center point cp_i and radius out_rad_i representing i. The centers cp_k of 
        // the disc of every child k of i will all be located on a circle with center point
        // cp_i and radius (rad_i + rl_k), where all children of i have the same reference 
        // length rl_k.

        /// <summary>
        /// Calculates the inner and outer radius and the reference length of each node.
        /// This algorithm is described in the paper.
        /// </summary>
        /// <param name="node">the node for which the ballon layout is to be computed</param>
        /// <param name="level">the level of the currently visited node; a root has level 0</param>
        /// <param name="rad">radius of the circle around node at which the center of every circle 
        ///                   of its direct children is located</param>
        /// <param name="out_rad">radius of the minimal circle around node that includes every circle 
        ///                       of its descendants</param>
        private void CalculateRadius2D(Node node, int level, out float out_rad, out int max_depth)
        {
            float rad = 0.0f;
            float rl_child = 0.0f;

            if (node.IsLeaf())
            {
                max_depth = 1;
                GameObject block = to_game_node[node];

                // Necessary size of the block independent of the parent
                Vector3 size = blockFactory.GetSize(block);

                // The outer radius of an inner-most node is determined by the ground
                // rectangle of the block to be drawn for the node.
                // Pythagoras: diagonal of the ground rectangle.
                float diagonal = Mathf.Sqrt(size.x * size.x + size.z * size.z);

                // The outer-radius must be large enough to put in the block.
                out_rad = diagonal / 2.0f;
                rad = 0.0f;
            }
            else
            {
                // Twice the total sum of out_rad_k over all children k of i, in
                // other words, the total sum of the diameters of all its children.
                float inner_sum = 0.0f;
                // maximal out_rad_k over all children k of i
                float max_children_rad = 0.0f;

                int max_child_depth = 0;
                foreach (Node child in node.Children())
                {

                    // Find the radius rad_k and outer-radius out_rad_k for each child k of node i.
                    CalculateRadius2D(child, level + 1, out float child_out_rad, out int child_depth);
                    if (child_depth > max_child_depth)
                    {
                        max_child_depth = child_depth;
                    }
                    inner_sum += child_out_rad;
                    if (max_children_rad < child_out_rad)
                    {
                        max_children_rad = child_out_rad;
                    }
                }
                max_depth = max_child_depth + 1;
                inner_sum *= 2;

                // min_rad is the minimal circumference to accommodate all the children
                float min_rad = 0.0f;

                // Let C be the circle with center point cp_i on which
                // the center points of all children of i are to be placed.
                // We assume that inner_sum is the approximate sum of the subarcs of
                // D_i which lie inside the children's outer-discs.
                if (inner_sum < 2.0f * Math.PI * 2.0f * max_children_rad)
                {
                    // case 2:  all the children's outer-discs for node i can
                    // be placed on C without overlap if inner_sum is not greater
                    // than the circumentference of C
                    rad = max_children_rad > min_rad ? max_children_rad : min_rad;
                }
                else
                {
                    // case 1: there are so many children that we need to increase C
                    float value = inner_sum / (2.0f * (float)Math.PI) - max_children_rad;
                    rad = value > min_rad ? value : min_rad;
                }
                out_rad = rad + 2.0f * max_children_rad;

                rl_child = max_children_rad;
            }
            nodeInfos.Add(node, new NodeInfo(rad, level, out_rad, rl_child));
        }

        /// <summary>
        /// If node is a leaf, a block is drawn. If node is an inner node, a circle is drawn
        /// and its children are drawn recursively.
        /// </summary>
        /// <param name="node">node to be drawn</param>
        /// <param name="position">position at which to place the node</param>
        /// <param name="depth">depth of node in the hierarchy used to determine the width of the line</param>
        /// <param name="max_depth">maximal depth of the hierarchy</param>
        /// <param name="scaler">a scaler for the metrics to be drawn</param>
        /// <param name="factory">the factory to create the Donut charts </param>
        private void DrawCircles(Node node, Vector3 position, int depth, int max_depth)
        {
            List<Node> children = node.Children();

            if (children.Count == 0)
            {
                // leaf
                layout_result[to_game_node[node]] = new NodeTransform(position, Vector3.one);
            }
            else
            {
                // inner node
                layout_result[to_game_node[node]] 
                    = new NodeTransform(position, 
                                        new Vector3(nodeInfos[node].outer_radius, 1.0f, nodeInfos[node].outer_radius));

                // The center points of the children circles are located on the circle
                // with center point 'position' and radius of the inner circle of the
                // current node plus the reference length of the children. See the paper
                // for details.
                float parent_inner_radius = nodeInfos[node].radius + nodeInfos[node].reference_length_children;

                // Placing all children of the inner circle defined by the 
                // center point (the given position) and the radius with some
                // space in between if that is possible.

                Vector3 child_center = new Vector3(position.x, position.y, position.z);

                // The space in between neighboring child circles if there is any left.
                double space_between_child_circles = 0.0;

                {
                    // Calculate space_between_child_circles.
                    // Here, we first calculate the sum over all angles necessary to position the child
                    // circles onto the circle with radius parent_inner_radius and center
                    // point 'position'.

                    // The accumulated angles in radians.
                    double accummulated_alpha = 0.0;

                    foreach (Node child in children)
                    {
                        double child_outer_radius = nodeInfos[child].outer_radius;
                        // As in polar coordinates, the angle of the child circle w.r.t. to the 
                        // circle point of the node's circle. The distance from the node's center point
                        // to the child node's center point together with this angle defines the polar
                        // coordinates of the child relative to the node.

                        // Let cp_p be the center point of the parent circle and cp_c be
                        // the center point of the child circle. cp_c is placed on the circle
                        // around cp_p with radius r_p. Thus, the distance between cp_p
                        // and cp_c is r_p. The child circle has radius r_c. The child circle
                        // around cp_ with radius r_c intersects twice with the parent circle.
                        // The distance between cp_c and those intersection points is r_c.
                        // The two triangles formed by the cp_p, cp_c, and each intersection
                        // point, P, are isosceles triangles, with |cp_p - P| = |cp_p - cp_c| = r_p
                        // and |cp_c - P| = r_c. The angle alpha of this isosceles triangle is
                        // 2 * arcsin(r_c / (2*r_p)).
                        double alpha = 2 * System.Math.Asin(child_outer_radius / (2 * parent_inner_radius));
                        //Debug.Log(node.name + " 1) Alpha:         " + alpha + "\n");

                        // There are two identical isosceles triangles, one for each of the two
                        // intersection points of the parent circle and child circle. When we
                        // place the child circle on the parent circle, the other child circles
                        // must be placed on the next free points on the parent circles outside
                        // of the child circle with radius r_c. That is why, we need to double
                        // the angle alpha to position the next circle.
                        accummulated_alpha += 2 * alpha;
                    }
                    if (accummulated_alpha > 2 * Math.PI)
                    {
                        // No space left.
                    }
                    else
                    {
                        space_between_child_circles = (2 * Math.PI - accummulated_alpha) / (double)children.Count;
                    }
                }
                // Now that we know the space we can put in between neighboring circles, we can
                // draw the child circles.
                {
                    // The accumulated angles in radians.
                    double accummulated_alpha = 0.0;

                    foreach (Node child in children)
                    {
                        // As in polar coordinates, the angle of the child circle w.r.t. to the 
                        // circle point of the node's circle. The distance from the node's center point
                        // to the child node's center point together with this angle defines the polar
                        // coordinates of the child relative to the node.
                        double child_outer_radius = nodeInfos[child].outer_radius;

                        // Asin (arcsin) returns an angle, θ, measured in radians, such that 
                        // -π/2 ≤ θ ≤ π/2 -or- NaN if d < -1 or d > 1 or d equals NaN.
                        double alpha = 2 * System.Math.Asin(child_outer_radius / (2 * parent_inner_radius));

                        if (accummulated_alpha > 0.0)
                        {
                            // We are not drawing the very first child circle. We need to add 
                            // the alpha angle of the current child circle to the accumulated alpha.
                            accummulated_alpha += alpha;

                        }
                        // Convert polar coordinate back to cartesian coordinate.
                        child_center.x = position.x + (float)(parent_inner_radius * System.Math.Cos(accummulated_alpha));
                        child_center.z = position.z + (float)(parent_inner_radius * System.Math.Sin(accummulated_alpha));

                        DrawCircles(child, child_center, depth + 1, max_depth);

                        // The next available circle must be located outside of the child circle
                        accummulated_alpha += alpha + space_between_child_circles;
                    }
                }
            }
        }
    }
}
