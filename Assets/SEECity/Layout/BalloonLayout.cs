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
        public void Draw(Graph graph)
        {
            DrawNodes(graph);
        }

        private const string materialPath = "Legacy Shaders/Particles/Additive";

        private void DrawNodes(Graph graph)
        {
            Material material = new Material(Shader.Find(materialPath));
            if (material == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return;
            }

            Vector3 position = Vector3.zero;

            foreach (Node root in graph.GetRoots())
            {
                CalculateRadius2D(root, out float out_rad);
                DrawCircles(root, position, material);
                position.x += 2.2f * out_rad;
                //break; // FIXME
            }


            /*
            foreach (Node root in graph.GetRoots())
            {
                const float radius = 0.4f;
                const float lineWidth = 0.1f;

                DrawCircle(root, position, lineWidth, radius, newMat);
                position.x += 2 * (radius + lineWidth);
            }
            */
        }

        private void DrawCircles(Node node, Vector3 position, Material material)
        {
            List<Node> children = node.Children();

            if (children.Count == 0)
            {
                // TODO: We will create a block for children.
                DrawCircle(node, position, radii[node].outer_radius, material);
                //Debug.Log("leaf " + node.name + " @ " + position + " radius " + radii[node].outer_radius + "\n");
            }
            else
            {
                DrawCircle(node, position, radii[node].outer_radius, material);
                // The center points of the children circles are located on the circle
                // with center point 'position' and radius of the inner circle of the
                // current node plus the reference length of the children. See the paper
                // for details.
                float parent_inner_radius = radii[node].radius + radii[node].reference_length_children;

                //Debug.Log("inner " + node.name + " @ " + position + " outer-radius " + radii[node].outer_radius + " inner-radius " + parent_inner_radius + "\n");

                // Placing all children of the inner circle defined by the 
                // center point (the given position) and the radius 

                Vector3 child_center = new Vector3(position.x, position.y, position.z);

                double accummulated_alpha = 0.0;

                foreach (Node child in children)
                {
                    double child_outer_radius = radii[child].outer_radius;
                    // As in polar coordinates, the angle of the child circle w.r.t. to the 
                    // circle point of the node's circle. The distance from the node's center point
                    // to the child node's center point together with this angle defines the polar
                    // coordinates of the child relative to the node.
                    double alpha = 2* System.Math.Asin(child_outer_radius / (2 * parent_inner_radius));

                    accummulated_alpha += alpha;
                    // Convert polar coordinate back to cartesian coordinate.
                    child_center.x = position.x + (float)(parent_inner_radius * System.Math.Cos(accummulated_alpha));
                    child_center.z = position.z + (float)(parent_inner_radius * System.Math.Sin(accummulated_alpha));

                    DrawCircles(child, child_center, material);

                    // The next available circle must be located outside of the child circle
                    accummulated_alpha += alpha;
                }

                //Debug.Log("Remaining angle: " + (360.0f - accummulated_alpha) + "\n");

                    // FIXME: This will not work for more than four children.
                    /*
                    int i = 0;
                    foreach (Node child in children)
                    {
                        Vector3 child_position = position;
                        float offset = radii[node].radius;
                        switch (i)
                        {
                            case 0: child_position.z += offset; break;
                            case 1: child_position.z -= offset; break;
                            case 2: child_position.x += offset; break;
                            case 3: child_position.x -= offset; break;
                        }
                        DrawCircles(child, child_position, material);
                        i++;
                        if (i > 3)
                        {
                            i = 0;
                        }
                    }
                    */
                }
        }

        private struct RadiusInfo
        {
            public readonly float radius;
            public readonly float outer_radius;
            public readonly float reference_length_children;

            public RadiusInfo(float radius, float outer_radius, float reference_length_children)
            {
                this.radius = radius;
                this.outer_radius = outer_radius;
                this.reference_length_children = reference_length_children;
            }
        }

        Dictionary<Node, RadiusInfo> radii = new Dictionary<Node, RadiusInfo>();

        const float minimal_radius = 0.1f;

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
        /// 
        /// </summary>
        /// <param name="node">the node for which the ballon layout is to be computed</param>
        /// <param name="rad">radius of the circle around node at which the center of every circly of its direct children is located</param>
        /// <param name="out_rad">radius of the minimal circle around node that includes every circle of its descendants</param>
        private void CalculateRadius2D(Node node, out float out_rad)
        {
            float rad = 0.0f;
            float rl_child = 0.0f;

            if (node.NumberOfChildren() == 0)
            {
                // If node i is a leaf, we can return an outer-radius of small value
                // minimal_radius so that it can be properly placed in the next lower level.
                // TODO: Here we need to consider the metric
                out_rad = minimal_radius;
                rad = 0.0f;
            }
            else
            {
                // Twice the total sum of out_rad_k over all children k of i, in
                // other words, the total sum of the diameters of all its children.
                float inner_sum = 0.0f;
                // maximal out_rad_k over all children k of i
                float max_children_rad = 0.0f;

                foreach (Node child in node.Children())
                {
                    // Find the radius rad_k and outer-radius out_rad_k for each child k of node i.
                    CalculateRadius2D(child, out float child_out_rad);
                    inner_sum += child_out_rad;
                    if (max_children_rad < child_out_rad)
                    {
                        max_children_rad = child_out_rad;
                    }
                }
                inner_sum *= 2;

                const float factor = 1.01f;
                // min_rad is the minimal circumference to accommodate all the children
                // TODO: If a metric determines the radius of leaves, they may have
                // different radii and we cannot just multiply the number of children
                // with the minimal diameter
                float min_rad = 0.0f; // factor * node.NumberOfChildren();

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
            radii.Add(node, new RadiusInfo(rad, out_rad, rl_child));

            // We could not draw a circle with center point cp_i and radius out_rad.
            
            //Debug.Log(node.name + " rad: " + rad + " outer-rad: " + out_rad + " reference-length of children: " + rl_child + "\n");
        }

        private void DrawCircle(Node node, Vector3 position, float radius, Material newMat)
        {
            const int segments = 360;
            GameObject go = node.gameObject;
            go.transform.position = position;

            LineRenderer line = go.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, Color.red);

            line.useWorldSpace = false;

            // use sharedMaterial if changes to the original material should affect all
            // objects using this material; renderer.material instead will create a copy
            // of the material and will not be affected by changes of the original material
            line.sharedMaterial = newMat;

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

        /* 
        private void DrawNodes(Graph graph)
        {
            SerializeTree(graph, out Node[] nodes);
            if (nodes == null)
            {
                // no nodes
                return;
            }

            //CalculateRadius2D(node, out float rad, out float out_rad);
        }

        /// <summary>
        /// Enumerates all nodes of the graph in nodes. The nodes are added in
        /// pre-order of the depth-first traversal. 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="nodes"></param>
        private void SerializeTree(Graph graph, out Node[] nodes)
        {
            List<Node> roots = graph.GetRoots();
            if (roots.Count == 0)
            {
                // no nodes
                nodes = null;
            }
            else if (roots.Count == 1)
            {
                nodes = new Node[graph.NodeCount];
                int dfsn = 0;
                DFS(nodes[0], nodes, ref dfsn);
            }
            else
            {
                // More than one root: We will introduce an artifical root whose
                // children are the actual roots
                nodes = new Node[graph.NodeCount + 1];
                // we cannot really create a node here, because Node derives from MonoBehaviour;
                // that is why we represent the artifical root by null; we need to make sure
                // that we handle this case properly later
                nodes[0] = null;
                // all other nodes will be placed after our artifical root
                int dfsn = 1;
                foreach (Node node in roots)
                {
                    DFS(nodes[0], nodes, ref dfsn);
                }
            }
            Dump(0, roots, nodes);
        }

        private void Dump(int node, List<Node> roots, Node[] nodes)
        {
            if (roots.Count > 1)
            {
                foreach (Node root in roots)
                {
                    Dump(node, nodes, 0);
                }
            }
            else
            {
                Dump(nodes[0], nodes, 0);
            }
        }

        private void Dump(Node node, Node[] nodes, int level)
        {

        }

        private void DFS(Node node, Node[] nodes, ref int dfsn)
        {
            nodes[dfsn] = node;
            dfsn++;
            foreach (Node child in node.Children())
            {
                DFS(child, nodes, ref dfsn);
            }
        }
        */
    }
}