using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System;

namespace SEE.Layout
{
    /// <summary>
    /// Creates a balloon layout according to "Reconfigurable Disc Trees for Visualizing 
    /// Large Hierarchical Information Space" by Chang-Sung Jeong and Alex Pang.
    /// Published in: Proceeding INFOVIS '98 Proceedings of the 1998 IEEE Symposium on 
    /// Information Visualization, Pages 19-25.
    /// </summary>
    public class BalloonLayout : ILayout
    {
        public BalloonLayout(string widthMetric, string heightMetric, string breadthMetric, 
                             SerializableDictionary<string, IconFactory.Erosion> issueMap,
                             string[] innerNodeMetrics,
                             BlockFactory blockFactory,
                             IScale scaler,
                             bool showErosions,
                             bool showDonuts)
        : base(widthMetric, heightMetric, breadthMetric, issueMap, blockFactory, scaler, showErosions)
        {
            name = "Ballon";
            this.showDonuts = showDonuts;
            this.innerNodeMetrics = innerNodeMetrics;
        }

        /// <summary>
        /// The names of the metrics for inner nodes to be put onto the 
        /// </summary>
        private readonly string[] innerNodeMetrics;

        /// <summary>
        /// Whether donut charts should be visible in the inner circle of the nodes.
        /// </summary>
        public bool showDonuts = true;

        /// <summary>
        /// The minimal line width of a circle drawn for an inner node.
        /// Will be adjusted by the world unit factor.
        /// </summary>
        public float minmalCircleLineWidth = 0.01f;

        /// <summary>
        /// The maximal line width of a circle drawn for an inner node.
        /// Will be adjusted by the world unit factor.
        /// </summary>
        public float maximalCircleLineWidth = 1.0f;

        public override void Draw(Graph graph)
        {
            // puts the outermost circles of the roots next to each other;
            // later we might use a circle-packing algorithm instead,
            // e.g., https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp

            graph.SortHierarchyByName();

            const float offset = 1.0f;
            Node[] roots = graph.GetRoots().ToArray();
            if (roots.Length == 0)
            {
                Debug.LogError("Graph has no nodes.\n");
                return;
            }
            int[] max_depths = new int[roots.Length];

            // the maximal radius over all root circles; required to create the plane underneath
            float max_radius = 0.0f;

            // first calculate all radii including those for the roots
            {
                int i = 0;
                foreach (Node root in roots)
                {
                    CalculateRadius2D(root, 0, out float out_rad, out int max_depth_of_this_tree, scaler);
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
                DonutFactory factory = new DonutFactory(innerNodeMetrics);
                Vector3 position = Vector3.zero;
                int i = 0;
                foreach (Node root in graph.GetRoots())
                {
                    // for two neighboring circles the distance must be the sum of the their two radii;
                    // in case we draw the very first circle, no distance must be kept
                    position.x += i == 0 ? 0.0f : nodeInfos[roots[i - 1]].outer_radius + nodeInfos[roots[i]].outer_radius + offset;
                    DrawCircles(root, position, 0, max_depths[i], scaler, factory);
                    i++;
                }
            }
            DrawPlane(roots, max_radius);
        }

        /// <summary>
        /// The maximal depth of the node hierarchy.
        /// </summary>
        private int maxDepth = 0;

        // The plane underneath the nodes.
        private GameObject plane;

        ~BalloonLayout()
        {
            if (plane != null)
            {
                Destroyer.DestroyGameObject(plane);
            }
        }

        /// <summary>
        /// Draws the plane underneath the nodes. Defines attribute 'plane'.
        /// </summary>
        /// <param name="roots">the roots of the graph</param>
        /// <param name="max_radius">the maximal radius of all roots</param>
        private void DrawPlane(Node[] roots, float max_radius)
        {
            // The factor by which we enlarge the plane somewhat. The plane may be a bit
            // larger than the maximal extents of the circles. That solves may also solve the issue
            // of the line width of the circle drawn (which depends upon its tree depth) that is not 
            // capture by the radius.
            const float enlargementFactor = 1.12f; // should not be smaller than 1.0

            // Width of the plane underneath the root circles determined by the left-most and right-most circle.
            float width = (gameNodes[roots[roots.Length - 1]].transform.position.x - gameNodes[roots[0]].transform.position.x
                + nodeInfos[roots[0]].outer_radius + nodeInfos[roots[roots.Length - 1]].outer_radius)
                * enlargementFactor;

            // Breadth of the plane: double the radius. 
            float depth = (2.0f * max_radius) * enlargementFactor;

            // Height of the plane.
            float height = 1.0f;

            Vector3 leftRootCenter = gameNodes[roots[0]].transform.position;
            float planePositionX = (leftRootCenter.x - nodeInfos[roots[0]].outer_radius) + (width / enlargementFactor / 2.0f);
            float planePositionY = leftRootCenter.y - 1.0f; // somewhat underneath roots
            float planePositionZ = leftRootCenter.z;
            Vector3 centerPosition = new Vector3(planePositionX, planePositionY, planePositionZ);

            plane = PlaneFactory.NewPlane(centerPosition, Color.gray, width, depth, height);
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
        private void DrawCircles(Node node, Vector3 position, int depth, int max_depth, IScale scaler, DonutFactory factory)
        {
            List<Node> children = node.Children();

            if (children.Count == 0)
            {
                DrawLeaf(node, position, nodeInfos[node].outer_radius);
            }
            else
            {
                DrawInnerNode(node, position, nodeInfos[node].outer_radius, depth, max_depth, factory);

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

                        DrawCircles(child, child_center, depth + 1, max_depth, scaler, factory);

                        // The next available circle must be located outside of the child circle
                        accummulated_alpha += alpha + space_between_child_circles;
                    }
                }
            }
        }

        /// <summary>
        /// We will draw a leaf nodes as two objects: cube and cylinder. Both become children
        /// of the node's game object. The cube represents the metrics and is put onto the
        /// cylinder. The cylinder is the Ballon circle.
        /// </summary>
        /// <param name="node">leaf node to be drawn</param>
        /// <param name="position">center point of the node where it is to be positioned</param>
        /// <param name="radius">the radius for the cylinder</param>
        private void DrawLeaf(Node node, Vector3 position, float radius)
        {
            GameObject gameObject = gameNodes[node];
            blockFactory.SetPosition(gameObject, position);

            // FIXME: We do without garden for the time being
            //AddGarden(parent);

            if (showErosions)
            {
                AddErosionIssues(node);
            }
        }

        private void AddGarden(GameObject parent)
        {
            // Second child: the cylinder.
            // The cylinder will be placed just below the center of the cube;
            // it will fill the complete plane of the parent;
            // the "garden" will be the second child; IMPORTANT NOTE: If we
            // ever change the order of the children, we need to adjust Roof().
            GameObject cylinder = new GameObject
            {
                name = "garden " + parent.name
            };
            // FIXME: Re-enable this:
            //blockFactory.AddFrontYard(cylinder.gameObject);
            // game object of node becomes the parent of cube
            cylinder.transform.parent = parent.transform;
            // relative position within parent
            cylinder.transform.localPosition = Vector3.zero;
            // Scale to full extent of the parent's width and breadth (chosen to
            // be twice the radius above). The cylinder's height should be minimal.
            cylinder.transform.localScale = new Vector3(1.0f, cylinder_height, 1.0f);
        }

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

        private Dictionary<Node, NodeInfo> nodeInfos = new Dictionary<Node, NodeInfo>();

        // This parameter determines the radius of circles for leaves.
        private const float minimal_radius = 0.5f;

        // This parameter determines the maximal width, breadth, and height of each cube. 
        // The cubes for the nodes representing leaves are put into the innermost leaf circles.
        // The maximal length l (width or breadth) of the square within the circle with given 
        // radius r is l = sqrt(2) * r.
        private static readonly float maximal_length = (float)Math.Sqrt(2.0) * minimal_radius;

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
        /// <param name="rad">radius of the circle around node at which the center of every circly of its direct children is located</param>
        /// <param name="out_rad">radius of the minimal circle around node that includes every circle of its descendants</param>
        private void CalculateRadius2D(Node node, int level, out float out_rad, out int max_depth, IScale scaler)
        {
            float rad = 0.0f;
            float rl_child = 0.0f;

            if (node.IsLeaf())
            {
                max_depth = 1;

                // First child: add block to parent; block will become the first child.
                // The block is create here so that we know its size. It will be positioned later.
                GameObject block = blockFactory.NewBlock();
                NodeRef noderef = block.AddComponent<NodeRef>();
                noderef.node = node;
                block.name = node.LinkName;
                gameNodes[node] = block;

                {
                    // The block's width and breadth are proportional to the two metrics.
                    Vector3 scale = new Vector3(scaler.GetNormalizedValue(node, widthMetric),
                                                scaler.GetNormalizedValue(node, heightMetric),
                                                scaler.GetNormalizedValue(node, breadthMetric));

                    // We scale the block before it becomes a child of parent so that its scale
                    // is not relative to its parent.
                    blockFactory.ScaleBlock(block, scale);
                }

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
                    CalculateRadius2D(child, level + 1, out float child_out_rad, out int child_depth, scaler);
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

            // We could not draw a circle with center point cp_i and radius out_rad.
            
            //Debug.Log(node.name + " rad: " + rad + " outer-rad: " + out_rad + " reference-length of children: " + rl_child + "\n");
        }

        const float cylinder_height = 0.01f;

        private Color lightCylinderColor = new Color((float)236 / 255, (float)236 / 255, (float)236 / 255, 1.0f); // White smoke
        private Color rightCylinderColor = new Color((float)46 / 255, (float)46 / 255, (float)46 / 255, 1.0f); // Outer space

        //private Color lightCylinderColor = new Color((float)200 / 255, (float)247 / 255, (float)197 / 255, 1.0f); // Lawn green
        // private Color rightCylinderColor = new Color((float)30 / 255, (float)130 / 255, (float)76 / 255, 1.0f); // Salem green

        /// <summary>
        /// Draws an inner node as a circle line.
        /// </summary>
        /// <param name="node">node to be drawn</param>
        /// <param name="position">center position where to place the node</param>
        /// <param name="radius">radius of the circle line</param>
        /// <param name="depth">depth of the node in the hierarchy used to determine the width of the line</param>
        /// <param name="max_depth">maximal depth of the hierarchy</param>
        /// <param name="factory">a factory to draw the Donut charts</param>
        private void DrawInnerNode(Node node, Vector3 position, float radius, int depth, int max_depth, DonutFactory factory)
        {
            GameObject sceneNode = new GameObject();
            gameNodes[node] = sceneNode;
            NodeRef nodeRef = sceneNode.AddComponent<NodeRef>();
            nodeRef.node = node;
            sceneNode.name = node.LinkName;
            sceneNode.tag = Tags.Node;

            // If we wanted to have the nesting of circles on different ground levels depending
            // on the depth of the node, we would use position.y - (max_depth - depth + 1) * cylinder_height
            // for the y co-ordinate.
            sceneNode.transform.position = position; 

            // Roots have depth 0. We want the line to be thicker for nodes higher in the hierarchy.
            float lineWidth = Mathf.Lerp(minmalCircleLineWidth,
                                         maximalCircleLineWidth, 
                                         (float)(max_depth - depth) / max_depth);

            if (showDonuts)
            {
                AddDonut(node, sceneNode, radius, factory);
                GameObject circleLine = new GameObject("circle line of " + sceneNode.name)
                {
                    tag = Tags.Decoration
                };
                AttachCircleLine(circleLine, radius, lineWidth * blockFactory.Unit());
                circleLine.transform.position = sceneNode.transform.position;
                circleLine.transform.parent = sceneNode.transform;
            }
            else
            {
                AttachCircleLine(sceneNode, radius, lineWidth * blockFactory.Unit());
            }

            // The text may occupy up to 30% of the diameter in width.
            GameObject text = TextFactory.GetText(node.SourceName, position, 2.0f * radius * 0.3f);
        }

        private void AddDonut(Node node, GameObject sceneNode, float radius, DonutFactory factory)
        {
            // FIXME: Derive real metrics from nodes.
            float innerValue = UnityEngine.Random.Range(0.0f, 1.0f);
            float m1 = UnityEngine.Random.Range(0.0f, 50.0f);
            float m2 = UnityEngine.Random.Range(0.0f, 90.0f);
            float m3 = UnityEngine.Random.Range(0.0f, 150.0f);
            float m4 = UnityEngine.Random.Range(0.0f, 200.0f);
            factory.DonutChart(sceneNode, 0.3f * radius, innerValue, new float[] {m1, m2, m2, m3 });
        }

        private void SetColor(GameObject gameObject, Color color)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            var tempMaterial = new Material(renderer.sharedMaterial)
            {
                color = color
            };
            renderer.sharedMaterial = tempMaterial;
        }

        private static void AttachCircleLine(GameObject circle, float radius, float lineWidth)
        {
            // Number of line segments constituting the circle
            const int segments = 360;

            LineRenderer line = circle.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.white);
            LineFactory.SetWidth(line, lineWidth);

            // We want to set the points of the circle lines relative to the game object.
            line.useWorldSpace = false;

            line.sharedMaterial = new Material(defaultLineMaterial);

            line.positionCount = segments + 1;
            const int pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
            Vector3[] points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius);
            }
            line.SetPositions(points);
        }
    }
}