using SEE.DataModel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Yields a squarified treemap node layout according to the algorithm 
    /// described by Bruls, Huizing, van Wijk, "Squarified Treemaps".
    /// pp. 33-42, Eurographics / IEEE VGTC Symposium on Visualization, 2000.
    /// </summary>
    public class TreemapLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="width">width of the rectangle in which to place all nodes</param>
        /// <param name="depth">width of the rectangle in which to place all nodes</param>
        public TreemapLayout(float groundLevel,
                             NodeFactory leafNodeFactory,
                             float width,
                             float depth)
        : base(groundLevel, leafNodeFactory)
        {
            name = "Treemap";
            this.width = width;
            this.depth = depth;
        }

        /// <summary>
        /// The width of the rectangle in which to place all nodes.
        /// </summary>
        private readonly float width;

        /// <summary>
        /// The depth of the rectangle in which to place all nodes.
        /// </summary>
        private readonly float depth;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private Dictionary<GameObject, NodeTransform> layout_result;

        public override Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes)
        {
            layout_result = new Dictionary<GameObject, NodeTransform>();

            if (gameNodes.Count == 0)
            {
                throw new Exception("No nodes to be laid out.");
            }
            else if (gameNodes.Count == 1)
            {
                GameObject gameNode = gameNodes.GetEnumerator().Current;
                layout_result[gameNode] = new NodeTransform(Vector3.zero, 
                                                            new Vector3(width, gameNode.transform.localScale.y, depth));
            }
            else
            {
                to_game_node = NodeMapping(gameNodes);
                CreateTree(gameNodes);
                CalculateSize();
                CalculateLayout();
            }
            return layout_result;
        }

        private void CalculateLayout()
        {
            if (roots.Count == 1)
            {
                GameObject root = to_game_node[roots[0]];
                layout_result[root] = new NodeTransform(Vector3.zero, 
                                                        new Vector3(width, root.transform.localScale.y, depth));
                CalculateLayout(children[roots[0]], -width / 2.0f, -depth / 2.0f, width, depth);
            }
            else
            {
                CalculateLayout(roots, -width / 2.0f, -depth / 2.0f, width, depth);
            }
        }

        private void CalculateLayout(List<Node> siblings, float x, float z, float width, float depth)
        {
            List<RectangleTiling.NodeSize> sizes = GetSizes(siblings);
            float padding = Mathf.Min(width, depth) * 0.01f;
            List<RectangleTiling.Rectangle> rects = RectangleTiling.Squarified_Layout_With_Padding(sizes, x, z, width, depth, padding);
            Add_To_Layout(sizes, rects);

            foreach (Node node in siblings)
            {
                List<Node> kids = children[node];
                if (kids.Count > 0)
                {
                    // Note: nodeTransform.position is the center position, while 
                    // CalculateLayout assumes co-ordinates x and z as the left front corner
                    NodeTransform nodeTransform = layout_result[to_game_node[node]];
                    CalculateLayout(kids, 
                                    nodeTransform.position.x - nodeTransform.scale.x / 2.0f, 
                                    nodeTransform.position.z - nodeTransform.scale.z / 2.0f,
                                    nodeTransform.scale.x, 
                                    nodeTransform.scale.z);
                }
            }
        }

        /// <summary>
        /// The set of children of each node. This is a subset of the node's children
        /// in the graph, limited to the children for which a layout is requested.
        /// </summary>
        private Dictionary<Node, List<Node>> children = new Dictionary<Node, List<Node>>();

        /// <summary>
        /// The roots of the subtrees of the original graph that are to be laid out.
        /// A node is considered a root if it has either no parent in the original
        /// graph or its parent is not contained in the set of nodes to be laid out.
        /// </summary>
        private List<Node> roots = new List<Node>();

        /// <summary>
        /// Creates the relevant tree consisting of the nodes to be laid out
        /// (a subtree of the node hierarchy of the original graph).
        /// 
        /// Precondition: to_game_node is computed.
        /// </summary>
        /// <param name="nodes">nodes to be laid out</param>
        private void CreateTree(ICollection<GameObject> nodes)
        {
            // The subset of nodes of the graph for which the layout is requested.
            HashSet<Node> allNodes = new HashSet<Node>(to_game_node.Keys);
            
            foreach(Node node in allNodes)
            {
                // Only children that are in the set of nodes to be laid out.
                HashSet<Node> kids = new HashSet<Node>(node.Children());
                kids.IntersectWith(allNodes);
                children[node] = new List<Node>(kids);
                {
                    Node parent = node.Parent;
                    // A node is considered a root if it has either no parent in the
                    // graph or its parent is not contained in the set of nodes to be laid out.
                    if (parent == null || !allNodes.Contains(parent))
                    {
                        roots.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the size of all nodes. The size of a leaf is the maximum of 
        /// its width and depth. The size of an inner node is the sum of the sizes 
        /// of all its children.
        /// </summary>
        /// <returns>total size of all node</returns>
        private float CalculateSize()
        {
            float total_size = 0.0f;
            foreach(Node root in roots)
            {
                total_size += CalculateSize(root);
            }
            return total_size;
        }

        /// <summary>
        /// The size metric of each node. The area of the rectangle is proportional to a node's size.
        /// </summary>
        private Dictionary<Node, RectangleTiling.NodeSize> sizes = new Dictionary<Node, RectangleTiling.NodeSize>();

        /// <summary>
        /// Calculates the size of node and all its descendants. The size of a leaf
        /// is the maximum of its width and depth. The size of an inner node is the
        /// sum of the sizes of all its children.
        /// </summary>
        /// <param name="node">node whose size it to be determined</param>
        /// <returns>size of node</returns>
        private float CalculateSize(Node node)
        {
            GameObject gameNode = to_game_node[node];

            if (children[node].Count == 0)
            {
                // a leaf      
                Vector3 size = leafNodeFactory.GetSize(gameNode);
                // x and z lenghts may differ; we need to consider the larger value
                float result = Mathf.Max(size.x, size.z);
                sizes[node] = new RectangleTiling.NodeSize(gameNode, result);
                return result;
            }
            else
            {
                float total_size = 0.0f;
                foreach (Node child in children[node])
                {
                    total_size += CalculateSize(child);
                }
                sizes[node] = new RectangleTiling.NodeSize(gameNode, total_size);
                return total_size;
            }
        }

        /// <summary>
        /// Returns the list of node area sizes; one for each node in nodes as
        /// defined in sizes.
        /// </summary>
        /// <param name="nodes">list of nodes whose sizes are to be determined</param>
        /// <returns>list of node area sizes</returns>
        private List<RectangleTiling.NodeSize> GetSizes(ICollection<Node> nodes)
        {
            List<RectangleTiling.NodeSize> result = new List<RectangleTiling.NodeSize>();
            foreach (Node node in nodes)
            {
                result.Add(sizes[node]);
            }
            return result;
        }

        /// <summary>
        /// Adds the transforms (position, scale) of the game objects (nodes) according to their
        /// corresponding rectangle in rects to layout_result. 
        /// 
        /// Precondition: For every i in the range of nodes: rects[i] is the transform
        /// corresponding to nodes[i].
        /// </summary>
        /// <param name="nodes">the game nodes</param>
        /// <param name="rects">their corresponding rectangle</param>
        private void Add_To_Layout
           (List<RectangleTiling.NodeSize> nodes,
            List<RectangleTiling.Rectangle> rects)
        {
            int i = 0;
            foreach (RectangleTiling.Rectangle rect in rects)
            {
                GameObject o = nodes[i].gameNode;
                Vector3 position = new Vector3(rect.x + rect.width / 2.0f, groundLevel, rect.z + rect.depth / 2.0f);
                Vector3 scale = new Vector3(rect.width / leafNodeFactory.Unit(), o.transform.localScale.y, rect.depth / leafNodeFactory.Unit());
                layout_result[o] = new NodeTransform(position, scale);
                i++;
            }
        }
    }
}
