using System;
using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    public class BundledEdgeLayout : IEdgeLayout
    {
        public BundledEdgeLayout(BlockFactory blockFactory, float edgeWidth, bool edgesAboveBlocks)
            : base(blockFactory, edgeWidth, edgesAboveBlocks)
        {
        }

        private Dictionary<Node, int> nodeLevel = new Dictionary<Node, int>();

        private int maxDepth = 0;

        private void SetLevel(Graph graph)
        {
            maxDepth = 0;

            foreach (Node root in graph.GetRoots())
            {
                int depth = SetLevel(root, 0);
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }
            }
        }

        private int SetLevel(Node node, int level)
        {
            nodeLevel[node] = level;

            int depth = 0;

            foreach (Node child in node.Children())
            {
                int childDepth = SetLevel(child, level + 1);
                if (childDepth > depth)
                {
                    depth = childDepth;
                }
            }
            return depth + 1;
        }

        public override void DrawEdges(Graph graph, IList<GameObject> nodes)
        {
            SetGameNodes(nodes);
            SetLevel(graph);
            // The distance between of the control points at the subsequent levels of the hierarchy.
            levelUnit = MaximalNodeHeight();

            Material newMat = new Material(defaultLineMaterial);
            if (newMat == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return;
            }

            List<Node> roots = graph.GetRoots();
            if (roots.Count != 1)
            {
                Debug.LogError("Graph must have a single root node.\n");
                return;
            }
            LCAFinder lca = new LCAFinder(graph, roots);
            foreach (Edge edge in graph.ConnectingEdges(gameNodes.Keys))
            {
                GameObject go = new GameObject
                {
                    tag = Tags.Edge
                };

                Node source = edge.Source;
                Node target = edge.Target;
                if (source != null && target != null)
                {
                    go.name = edge.Type + "(" + source.LinkName + ", " + target.LinkName + ")";
                    BSplineFactory.Draw(go, GetControlPoints(source, target, lca, maxDepth), edgeWidth * blockFactory.Unit(), newMat);
                }
                else
                {
                    Debug.LogErrorFormat("Scene edge from {0} to {1} of type {2} has a missing source or target.\n",
                                         source != null ? source.LinkName : "null",
                                         target != null ? target.LinkName : "null",
                                         edge.Type);
                }
            }
        }

        /// <summary>
        /// Yields the list of control points for the bspline along the node hierarchy.
        /// If source equals target, a self loop is generated atop of the node.
        /// If source and target have no common ancestor, the path starts at source
        /// and ends at target and reaches through the point on half distance between 
        /// these two nodes, but at the top-most edge height (given by maxDepth).
        /// If source and target are siblings (immediate ancestors of the same parent
        /// node), the control points are all on ground level froum source to target
        /// via their parent.
        /// Otherwise, the control points are chosen along the path from the source
        /// node to their lowest common ancestor and then down again to the target 
        /// node. The height of each such control point is proportional to the level 
        /// of the node hierarchy. The higher the node in the hierarchy on this path,
        /// the higher the control point.
        /// </summary>
        /// <param name="source">starting node</param>
        /// <param name="target">ending node</param>
        /// <param name="lcaFinder">to retrieve the lowest common ancestor of source and target</param>
        /// <param name="maxDepth">the maximal depth of the node hierarchy</param>
        /// <returns>control points to draw a bspline between source and target</returns>
        private Vector3[] GetControlPoints(Node source, Node target, LCAFinder lcaFinder, int maxDepth)
        {
            Vector3[] controlPoints;

            GameObject sourceObject = gameNodes[source];
            GameObject targetObject = gameNodes[target];

            if (source == target)
            {
                controlPoints = SelfLoop(sourceObject);
            }
            else
            {
                // Lowest common ancestor
                Node lca = lcaFinder.LCA(source, target);
                if (lca == null)
                {
                    // This should never occur if we have a single root node, but may happen if
                    // there are multiple roots, in which case nodes in different trees of this
                    // forrest do not have a common ancestor.
                    Debug.LogError("Undefined lowest common ancestor for "
                        + source.LinkName + " and " + target.LinkName + "\n");
                    controlPoints = ThroughCenter(sourceObject, targetObject, maxDepth);
                }
                else
                {
                    GameObject lcaObject = gameNodes[lca];
                    if (lcaObject == null)
                    {
                        Debug.LogError("Undefined game object for lowest common ancestor of "
                                       + source.LinkName + " and " + target.LinkName + "\n");
                        controlPoints = ThroughCenter(sourceObject, targetObject, maxDepth);
                    }
                    else
                    {
                        // assert: sourceObject != targetObject
                        // assert: lcaObject != null
                        // because the edges are only between leaves:
                        // assert: sourceObject != lcaObject
                        // assert: targetObject != lcaObject
                        NodeRef lcaNodeRef = lcaObject.GetComponent<NodeRef>();
                        Node lcaNode = lcaNodeRef.node;

                        Node[] sourceToLCA = Ancestors(source, lcaNode);
                        Debug.Assert(sourceToLCA.Length > 1);
                        Debug.Assert(sourceToLCA[0] == source);
                        Debug.Assert(sourceToLCA[sourceToLCA.Length - 1] == lcaNode);

                        Node[] targetToLCA = Ancestors(target, lcaNode);
                        Debug.Assert(targetToLCA.Length > 1);
                        Debug.Assert(targetToLCA[0] == target);
                        Debug.Assert(targetToLCA[targetToLCA.Length - 1] == lcaNode);

                        Array.Reverse(targetToLCA, 0, targetToLCA.Length);
                        // Note: lcaNode is included in both paths

                        if (sourceToLCA.Length == 2 && targetToLCA.Length == 2)
                        {
                            // source and target are siblings in the same subtree at the same level
                            // the total path has length 4 and and we do need at least four control points 
                            // for Bsplines (it is fine to include LCA twice).
                            // Edges between leaves will be on the ground.
                            controlPoints = new Vector3[4];
                            controlPoints[0] = blockFactory.Ground(sourceObject);
                            controlPoints[1] = gameNodes[lcaNode].transform.position;
                            controlPoints[2] = controlPoints[1];
                            controlPoints[3] = blockFactory.Ground(targetObject);
                        }
                        else
                        {
                            //Debug.LogFormat("maxDepth = {0}\n", maxDepth);
                            // Concatenate both paths.
                            // We have sufficient many control points without the duplicated LCA,
                            // hence, we can remove the duplicate
                            Node[] fullPath = new Node[sourceToLCA.Length + targetToLCA.Length - 1];
                            sourceToLCA.CopyTo(fullPath, 0);
                            // copy without the first element
                            for (int i = 1; i < targetToLCA.Length; i++)
                            {
                                fullPath[sourceToLCA.Length + i - 1] = targetToLCA[i];
                            }
                            // Calculate control points along the node hierarchy 
                            controlPoints = new Vector3[fullPath.Length];
                            controlPoints[0] = blockFactory.Roof(sourceObject);
                            for (int i = 1; i < fullPath.Length - 1; i++)
                            {
                                // We consider the height of intermediate nodes.
                                // Note that a root has level 0 and the level is increases along 
                                // the childrens' depth. That is why we need to choose the height
                                // as a measure relative to maxDepth.
                                controlPoints[i] = gameNodes[fullPath[i]].transform.position + (maxDepth - nodeLevel[fullPath[i]]) * levelUnit;
                            }
                            controlPoints[controlPoints.Length - 1] = blockFactory.Roof(targetObject);
                            //Dump(controlPoints);
                        }
                    }
                }
            }
            return controlPoints;
        }

        /// <summary>
        /// Dumps given control points for debugging.
        /// </summary>
        /// <param name="controlPoints">control points to be emitted</param>
        private void Dump(Vector3[] controlPoints)
        {
            int i = 0;
            foreach (Vector3 cp in controlPoints)
            {
                Debug.LogFormat("controlpoint[{0}] = {1}\n", i, cp);
                i++;
            }
        }

        /// <summary>
        /// Returns the path from child to ancestor in the tree including
        /// the child and the ancestor.
        /// Assertations on result: path[0] = child and path[path.Length-1] = ancestor.
        /// If child = ancestor, path[0] = child = path[path.Length-1] = ancestor.
        /// Precondition: child has ancestor.
        /// </summary>
        /// <param name="child">from where to start</param>
        /// <param name="ancestor">where to stop</param>
        /// <returns>path from child to ancestor in the tree</returns>
        private Node[] Ancestors(Node child, Node ancestor)
        {
            int childLevel = nodeLevel[child];
            int ancestorLevel = nodeLevel[ancestor];
            // Note: roots have level 0, lower nodes have a level greater than 0;
            // thus, childLevel >= ancestorLevel

            // if ancestorLevel = childLevel, then path.Count = 1
            Node[] path = new Node[childLevel - ancestorLevel + 1];
            int i = 0;
            while (true)
            {
                path[i] = child;
                if (child == ancestor)
                {
                    break;
                }
                child = child.Parent;
                i++;
            }
            return path;
        }

        /// <summary>
        /// The number of Unity units per level of the hierarchy for the height of control points.
        /// This factor must be relative to the height of the buildings. The initial value is
        /// just a default.
        /// </summary>
        private static Vector3 levelUnit = 2.0f * Vector3.up;

        /// <summary>
        /// The number of Unity units by which the second and third control point of 
        /// a self loop is located towards left and right (x axis).
        /// </summary>
        private static readonly float selfLoopExtent = 3.0f;

        /// <summary>
        /// Yields control points for a self loop at a node. The control points
        /// start and end at the center of roof of the node (first and last
        /// control points). The second control point is selfLoopExtent units
        /// to the left and levelUnit units above of the roof center. The 
        /// third control point is opposite of the second control point, that
        /// is, selfLoopExtent units to the right and levelUnit units above of 
        /// the roof center.
        /// </summary>
        /// <param name="node">node whose self loop control points are required</param>
        /// <returns>control points forming a self loop above the node</returns>
        private Vector3[] SelfLoop(GameObject node)
        {
            // we need at least four control points; tinySplines wants that
            Vector3[] controlPoints = new Vector3[4];
            // self-loop
            controlPoints[0] = blockFactory.Roof(node);
            controlPoints[1] = controlPoints[0] + levelUnit + selfLoopExtent * Vector3.left;
            controlPoints[2] = controlPoints[1] + 2.0f * selfLoopExtent * Vector3.right;
            controlPoints[3] = blockFactory.Roof(node);
            return controlPoints;
        }

        /// <summary>
        /// Yields control points for two nodes that do not have a common ancestor in the
        /// node hierarchy. This may occur when we have multiple roots in the graph, that
        /// is, the node hierarchy is a forrest and not just a single tree. In this case,
        /// we want the spline to reach above all other splines of nodes having a common
        /// ancestor. 
        /// The first and last control points are the respective roots of source and target
        /// node. The second and third control points are the same: it lies in between 
        /// the two nodes with respect to the x and z axis; its height (y axis) is the
        /// highest hierarchical level, that is, one levelUnit above the level at maxDepth.
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="targetObject"></param>
        /// <param name="maxDepth">maximal depth of the node hierarchy</param>
        /// <returns>control points for two nodes without common ancestor</returns>
        private Vector3[] ThroughCenter(GameObject sourceObject, GameObject targetObject, int maxDepth)
        {
            Vector3[] controlPoints = new Vector3[4];
            controlPoints[0] = blockFactory.Roof(sourceObject);
            controlPoints[3] = blockFactory.Roof(targetObject);
            // the point in between the two roofs
            Vector3 center = controlPoints[0] + 0.5f * (controlPoints[3] - controlPoints[0]);
            // note: height is independent of the roofs; it is the distance to the ground
            center.y = levelUnit.y * (maxDepth + 1);
            controlPoints[1] = center;
            controlPoints[2] = center;
            return controlPoints;
        }

        /// <summary>
        /// Yields the maximal height over all nodes (stored in gameObjects).
        /// </summary>
        /// <returns>maximal height of nodes in y coordinate (x and z are zero)</returns>
        private Vector3 MaximalNodeHeight()
        {
            Vector3 result = Vector3.zero;
            foreach (KeyValuePair<Node, GameObject> node in gameNodes)
            {
                Node n = node.Key;
                if (n != null && n.IsLeaf())
                {
                    Vector3 size = blockFactory.GetSize(node.Value);
                    if (size.y > result.y)
                    {
                        result.y = size.y;
                    }
                }
            }
            return result;
        }
    }
}
