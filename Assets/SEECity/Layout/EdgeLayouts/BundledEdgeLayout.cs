using System.Collections.Generic;
using UnityEngine;
using System;

namespace SEE.Layout
{
    /// <summary>
    /// Draws edges as hierarchically bundled edges.
    /// </summary>
    public class BundledEdgeLayout : IEdgeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="edgesAboveBlocks">if true, edges are drawn above nodes, otherwise below</param>
        public BundledEdgeLayout(bool edgesAboveBlocks)
            : base(edgesAboveBlocks)
        {
            name = "Hierarchically Bundled";
        }

        /// <summary>
        /// The maximal depth of the node hierarchy of the graph.
        /// </summary>
        private int maxDepth = 0;

        public override ICollection<LayoutEdge> Create(ICollection<ILayoutNode> layoutNodes)
        {
            ICollection<LayoutEdge> layout = new List<LayoutEdge>();

            ICollection<ILayoutNode> roots = GetRoots(layoutNodes);
            maxDepth = GetMaxDepth(roots, -1);

            // TODO: We want the level distances be chosen relative to the maximal height of all
            // game objects within the same subtree. At the very least, we want that for siblings
            // whose edges are drawn as direct splines. They should be led across all other siblings.
            // CalculateMaxHeights(gameNodes);
            MinMaxBlockY(layoutNodes, out minY, out float maxY, out float maxHeight);
            minY *= 1.5f;

            LCAFinder<ILayoutNode> lca = new LCAFinder<ILayoutNode>(roots);

            foreach (ILayoutNode source in layoutNodes)
            {
                foreach (ILayoutNode target in source.Successors)
                {
                    layout.Add(new LayoutEdge(source, target,
                                              LinePoints.BSplineLinePoints(GetControlPoints(source, target, lca, maxDepth))));

                }
            }
            return layout;
        }

        /// <summary>
        /// Returns the maximal tree depth of the given <paramref name="nodes"/>, that is, the 
        /// longest path from a leaf to any node in <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">nodes whose maximal depth is to be determined</param>
        /// <param name="currentDepth">the current depth of all <paramref name="nodes"/></param>
        /// <returns>maximal tree depth</returns>
        private int GetMaxDepth(ICollection<ILayoutNode> nodes, int currentDepth)
        {
            int max = currentDepth + 1;
            foreach (ILayoutNode node in nodes)
            {
                max = Math.Max(max, GetMaxDepth(node.Children(), currentDepth + 1));
            }
            return max;
        }

        /// <summary>
        /// Yields all nodes in <paramref name="layoutNodes"/> that do not have a parent,
        /// i.e., are root nodes.
        /// </summary>
        /// <param name="layoutNodes">list of nodes</param>
        /// <returns>all root nodes in <paramref name="layoutNodes"/></returns>
        private ICollection<ILayoutNode> GetRoots(ICollection<ILayoutNode> layoutNodes)
        {
            ICollection<ILayoutNode> result = new List<ILayoutNode>();
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                if (layoutNode.Parent == null)
                {
                    result.Add(layoutNode);
                }
            }
            return result;
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
        private Vector3[] GetControlPoints(ILayoutNode source, ILayoutNode target, LCAFinder<ILayoutNode> lcaFinder, int maxDepth)
        {
            Vector3[] controlPoints = null;

            if (source == target)
            {
                controlPoints = SelfLoop(source);
            }
            else
            {
                // Lowest common ancestor
                ILayoutNode lca = lcaFinder.LCA(source, target);
                if (lca == null)
                {
                    // This should never occur if we have a single root node, but may happen if
                    // there are multiple roots, in which case nodes in different trees of this
                    // forrest do not have a common ancestor.
                    Debug.LogError("Undefined lowest common ancestor for "
                        + source.LinkName + " and " + target.LinkName + "\n");
                    controlPoints = ThroughCenter(source, target, maxDepth);
                }
                else if (lca == source || lca == target)
                {
                    // The edge is along a node hierarchy path.
                    // We will create a direct spline from source to target at the lowest level.
                    controlPoints = DirectSpline(source, target, GetLevelHeight(maxDepth));
                }
                else
                {
                    // assert: sourceObject != targetObject
                    // assert: lcaObject != null
                    // because the edges are only between leaves:
                    // assert: sourceObject != lcaObject
                    // assert: targetObject != lcaObject

                    ILayoutNode[] sourceToLCA = Ancestors(source, lca);
                    //Debug.Assert(sourceToLCA.Length > 1);
                    //Debug.Assert(sourceToLCA[0] == source);
                    //Debug.Assert(sourceToLCA[sourceToLCA.Length - 1] == lcaNode);

                    ILayoutNode[] targetToLCA = Ancestors(target, lca);
                    //Debug.Assert(targetToLCA.Length > 1);
                    //Debug.Assert(targetToLCA[0] == target);
                    //Debug.Assert(targetToLCA[targetToLCA.Length - 1] == lcaNode);

                    Array.Reverse(targetToLCA, 0, targetToLCA.Length);

                    // Note: lcaNode is included in both paths
                    if (sourceToLCA.Length == 2 && targetToLCA.Length == 2)
                    {
                        // source and target are siblings in the same subtree at the same level.
                        // We assume that those nodes are close to each other for all hierarchical layouts,
                        // which is true for EvoStreets, Balloon, TreeMap, and CirclePacking. If all edges 
                        // between siblings were led over one single control point, they would often take 
                        // a detour even though the nodes are close by. The detour would make it difficult 
                        // to follow the edges visually.
                        controlPoints = DirectSpline(source, target, GetLevelHeight(maxDepth));
                    }
                    else
                    {
                        //Debug.LogFormat("maxDepth = {0}\n", maxDepth);
                        // Concatenate both paths.
                        // We have sufficient many control points without the duplicated LCA,
                        // hence, we can remove the duplicate
                        ILayoutNode[] fullPath = new ILayoutNode[sourceToLCA.Length + targetToLCA.Length - 1];
                        sourceToLCA.CopyTo(fullPath, 0);
                        // copy without the first element
                        for (int i = 1; i < targetToLCA.Length; i++)
                        {
                            fullPath[sourceToLCA.Length + i - 1] = targetToLCA[i];
                        }
                        // Calculate control points along the node hierarchy 
                        controlPoints = new Vector3[fullPath.Length];
                        controlPoints[0] = source.Roof;
                        for (int i = 1; i < fullPath.Length - 1; i++)
                        {
                            // We consider the height of intermediate nodes.
                            // Note that a root has level 0 and the level is increased along 
                            // the childrens' depth. That is why we need to choose the height
                            // as a measure relative to maxDepth.
                            // TODO: Do we really want the center position here?
                            controlPoints[i] = fullPath[i].CenterPosition
                                               + GetLevelHeight(fullPath[i].Level) * Vector3.up;
                        }
                        controlPoints[controlPoints.Length - 1] = target.Roof;
                        //Dump(controlPoints);
                    }
                }
            }
            return controlPoints;
        }

        /// <summary>
        /// Returns four control points for an edge from <paramref name="source"/> to <paramref name="target"/>.
        /// The first control point is the center of the roof of <paramref name="source"/> and the last
        /// control point the center of the roof of <paramref name="target"/>. The second and third control point
        /// is the position in between <paramref name="source"/> and <paramref name="target"/> where
        /// the y co-ordinate is specified by <paramref name="yLevel"/>. That means an edge between the nodes is drawn
        /// as a direct spline on the shortest path between the two nodes from roof to roof. Thus, no hierarchical
        /// bundling is applied.
        /// </summary>
        /// <param name="source">the object where to start the edge</param>
        /// <param name="target">the object where to end the edge</param>
        /// <param name="yLevel">the y co-ordinate of the two middle control points</param>
        /// <returns>control points for a direct spline between the two nodes</returns>
        private Vector3[] DirectSpline(ILayoutNode source, ILayoutNode target, float yLevel)
        {
            // We do need at least four control points for Bsplines (it is fine to include 
            // the middle control point twice).
            Vector3 start = source.Roof;
            Vector3 end = target.Roof;
            // position in between start and end
            Vector3 middle = Vector3.Lerp(start, end, 0.5f);
            middle.y += yLevel;
            return SplineLinePoints(start, middle, end);
        }

        /// <summary>
        /// Returns control points of for a B-spline for the following path:
        ///  from <paramref name="start"/> to <paramref name="middle"/>
        ///  and then from <paramref name="middle"/> to <paramref name="middle"/> 
        ///  and finally from <paramref name="middle"/> to <paramref name="end"/>.
        /// </summary>
        /// <param name="start">first control point</param>
        /// <param name="middle">second and third control point</param>
        /// <param name="end">last control point</param>
        /// <returns>control points</returns>
        private static Vector3[] SplineLinePoints(Vector3 start, Vector3 middle, Vector3 end)
        {
            Vector3[] controlPoints = new Vector3[4];
            controlPoints[0] = start;
            controlPoints[1] = middle;
            controlPoints[2] = middle;
            controlPoints[3] = end;
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
        private ILayoutNode[] Ancestors(ILayoutNode child, ILayoutNode ancestor)
        {
            int childLevel = child.Level;
            int ancestorLevel = ancestor.Level;
            // Note: roots have level 0, lower nodes have a level greater than 0;
            // thus, childLevel >= ancestorLevel

            // if ancestorLevel = childLevel, then path.Count = 1
            ILayoutNode[] path = new ILayoutNode[childLevel - ancestorLevel + 1];
            ILayoutNode cursor = child;
            int i = 0;
            while (true)
            {
                path[i] = cursor;
                if (cursor == ancestor)
                {
                    break;
                }
                cursor = cursor.Parent;
                i++;
            }
            return path;
        }

        /// <summary>
        /// The number of Unity units per level of the hierarchy for the height of control points.
        /// It will be chosen relative to the height of the largest leaf node.
        /// </summary>
        private float levelDistance = 1.0f;

        /// <summary>
        /// The minimal y co-ordinate for all hierarchical control points at level 2 and above.
        /// Control points at level 0 (self loop) will be handled separately: self loops will be
        /// drawn on top of the roof or below the ground of a node. Likewise, edges for nodes
        /// that are siblings in the hierarchy will be drawn as simple splines from roof to
        /// roof or ground to ground, respectively. The y co-ordinate of inner control points 
        /// of all other edges will be at minY or above. See GetLevelHeight() for more details.
        /// </summary>
        private float minY;

        /// <summary>
        /// Returns the y co-ordinate for control points of nodes at the given <paramref name="level"/>
        /// of the node hierarchy. Root nodes are assumed to have level 0. There may be node hierarchies
        /// that are actually forrests rather than simple trees. In such cases, the given <paramref name="level"/>
        /// can be -1, which is perfectly acceptable. In such cases, the returned value will be just one 
        /// levelDistance above those for normal root nodes. If level == maxDepth, minY will be returned;
        /// in all other cases the returned value is guaranteed to be greater than minY.
        /// </summary>
        /// <param name="level">node hierarchy level</param>
        /// <returns>y co-ordinate for control points</returns>
        private float GetLevelHeight(int level)
        {
            return minY + (maxDepth - level) * levelDistance;
        }

        /// <summary>
        /// Yields four control points for a self loop at a node. The first control 
        /// point is the front left corner of the roof of <paramref name="node"/>
        /// and the last control point is its opposite back right roof corner.
        /// Thus, the edge is diagonal across the roof. The second and third control
        /// points are the same position: the position directly in between the first
        /// and last control point but with a y co-ordinate that is the roof's 
        /// y co-ordinate plus the distance between the first and last control point.
        /// As a consequence, self loops are wider and higher for larger roof areas.
        /// </summary>
        /// <param name="node">node whose self loop control points are required</param>
        /// <returns>control points forming a self loop above the node</returns>
        private Vector3[] SelfLoop(ILayoutNode node)
        {
            Vector3 roofCenter = node.Roof;
            Vector3 extent = node.Scale / 2.0f;

            Vector3 start = new Vector3(roofCenter.x - extent.x, roofCenter.y, roofCenter.z - extent.z);
            Vector3 end = new Vector3(roofCenter.x + extent.x, roofCenter.y, roofCenter.z + extent.z);
            Vector3 middle = roofCenter + Vector3.Distance(start, end) * Vector3.up;
            Vector3[] controlPoints = new Vector3[4];
            controlPoints[0] = start;
            controlPoints[1] = middle;
            controlPoints[2] = middle;
            controlPoints[3] = end;
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
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="maxDepth">maximal depth of the node hierarchy</param>
        /// <returns>control points for two nodes without common ancestor</returns>
        private Vector3[] ThroughCenter(ILayoutNode source, ILayoutNode target, int maxDepth)
        {
            Vector3 start = source.Roof;
            Vector3 end = target.Roof;
            // note: height is independent of the roofs; it is the distance to the ground
            return ThroughCenter(start, end, GetLevelHeight(-1));
        }

        /// <summary>
        /// Yields control points for a B-spline from <paramref name="start"/> to <paramref name="end"/>.
        /// The first and last control points are <paramref name="start"/> and <paramref name="end"/>,
        /// respectively. The second and third control points are the same position that lies in between 
        /// <paramref name="start"/> and <paramref name="end"/> with respect to the x and z axis; its height
        /// (y axis) is <paramref name="yLevel"/>.
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="targetObject"></param>
        /// <param name="yLevel">y co-ordinate of the second and third control points returned</param>
        /// <returns>control points for two nodes without common ancestor</returns>
        private static Vector3[] ThroughCenter(Vector3 start, Vector3 end, float yLevel)
        {
            Vector3[] controlPoints = new Vector3[4];
            controlPoints[0] = start;
            controlPoints[3] = end;
            // the point in between the start and end
            Vector3 center = controlPoints[0] + 0.5f * (controlPoints[3] - controlPoints[0]);
            center.y = yLevel;
            controlPoints[1] = center;
            controlPoints[2] = center;
            return controlPoints;
        }
    }
}
