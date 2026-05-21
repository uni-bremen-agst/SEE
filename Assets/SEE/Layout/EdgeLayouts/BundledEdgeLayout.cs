using SEE.Layout.Utils;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Layout.EdgeLayouts
{
    /// <summary>
    /// Draws edges as hierarchically bundled edges.
    ///
    /// See D. Holten, "Hierarchical Edge Bundles: Visualization of Adjacency Relations in Hierarchical Data"
    /// in IEEE Transactions on Visualization and Computer Graphics, vol. 12, no. 5, pp. 741-748, Sept.-Oct. 2006.
    /// </summary>
    public class BundledEdgeLayout : CurvyEdgeLayoutBase
    {
        /// <summary>
        /// Constructor.
        ///
        /// Parameter <paramref name="tension"/> specifies the degree of bundling. A value of
        /// zero means no bundling at all; the maximal value of 1 means maximal bundling.
        /// </summary>
        /// <param name="edgesAboveBlocks">If true, edges are drawn above nodes, otherwise below.</param>
        /// <param name="minLevelDistance">The minimal distance between different edge levels.</param>
        /// <param name="tension">Strength of the tension for bundling edges; must be in the range [0,1].</param>
        public BundledEdgeLayout(bool edgesAboveBlocks, float minLevelDistance, float tension = 0.85f)
            : base(edgesAboveBlocks, minLevelDistance)
        {
            Name = "Hierarchically Bundled";
            Debug.Assert(0.0f <= tension && tension <= 1.0f);
            this.tension = tension;
            levelDistance = minLevelDistance;
        }

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// </summary>
        private readonly float tension = 0.85f; // 0.85 is the value recommended by Holten

        /// <summary>
        /// The number of Unity units per level of the hierarchy for the height of control points.
        /// Its value must be greater than zero. It will be set relative to the maximal height
        /// of the nodes whose edges are to be laid out (in Create()). The value set in the
        /// constructor is the initial minimum value.
        /// </summary>
        private float levelDistance;

        /// <summary>
        /// The minimal/maximal y co-ordinate for all hierarchical control points at level 2 and above.
        /// Control points at level 0 (self loops) will be handled separately: self loops will be
        /// drawn from corner to corner on the roof or ground, respectively, depending upon
        /// edgesAboveBlocks. Likewise, edges for nodes that are siblings in the hierarchy will be
        /// drawn as simple splines from roof to roof or ground to ground of the two blocks, respectively,
        /// on their shortest path. The y co-ordinate of inner control points of all other edges will be
        /// at levelOffset or above/below. See GetLevelHeight() for more details.
        /// </summary>
        private float levelOffset = 0.0f;

        /// <summary>
        /// Adds way points to the given <paramref name="edges"/> along hierarchically
        /// bundled splines.
        /// The <paramref name="edges"/> are assumed to be in between pairs of nodes in
        /// the given set of <paramref name="nodes"/>. Because this is a hierarchical edge
        /// layout,  <paramref name="nodes"/> must include all ancestors for all nodes that are
        /// source or target of any edge in the given set of <paramref name="edges"/>.
        /// </summary>
        /// <param name="nodes">Nodes whose edges are to be drawn or which are
        /// ancestors of any nodes whose edges are to be drawn.</param>
        /// <param name="edges">Edges for which to add way points.</param>
        public override void Create<T>(IEnumerable<T> nodes, IEnumerable<ILayoutEdge<T>> edges)
        {
            IList<ILayoutEdge<T>> layoutEdges = edges.ToList();
            IList<T> layoutNodes = nodes.ToList();
            if (layoutEdges.Count > 0)
            {
                ICollection<T> roots = LayoutNodes.GetRoots(layoutNodes).ToList();
                Assert.IsTrue(roots.Any());
                maxLevel = GetMaxLevel(roots, -1);

                MinMaxBlockY(nodes, out float minY, out float maxY, out float maxHeight);
                levelDistance = Math.Max(levelDistance, maxHeight / 5.0f);
                levelOffset = EdgesAboveBlocks ? maxY + levelDistance : minY - levelDistance;

                LCAFinder<ILayoutNode> lca = new LCAFinder<ILayoutNode>(roots.Cast<ILayoutNode>().ToList());

                foreach (ILayoutEdge<T> edge in layoutEdges)
                {
                    edge.Spline = CreateSpline(edge.Source, edge.Target, lca);
                }
            }
        }

        /// <summary>
        /// The maximal level of the node hierarchy of the graph. The first level is 0.
        /// Thus, this value is greater or equal to zero. It is zero if we have only roots.
        /// </summary>
        private int maxLevel = 0;

        /// <summary>
        /// Returns the maximal tree level of the given <paramref name="nodes"/>, that is, the
        /// longest path from a leaf to any node in <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">Nodes whose maximal level is to be determined.</param>
        /// <param name="currentLevel">The current level of all <paramref name="nodes"/>.</param>
        /// <returns>Maximal tree level.</returns>
        private static int GetMaxLevel<T>(IEnumerable<T> nodes, int currentLevel) where T : ILayoutNode
        {
            int max = currentLevel + 1;
            return nodes.Select(node => GetMaxLevel(node.Children(), currentLevel + 1)).Prepend(max).Max();
        }

        /// <summary>
        /// Returns the path from <paramref name="child"/> to <paramref name="child"/> in the
        /// node hierarchy including the child and the ancestor.
        /// Assertations on result: path[0] = child and path[path.Length-1] = ancestor.
        /// If child = ancestor, path[0] = child = path[path.Length-1] = ancestor.
        /// Precondition: child has ancestor.
        /// </summary>
        /// <param name="child">From where to start.</param>
        /// <param name="ancestor">Where to stop.</param>
        /// <returns>Path from child to ancestor in the node hierarchy.</returns>
        private static ILayoutNode[] Ancestors(ILayoutNode child, ILayoutNode ancestor)
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
        /// Creates a spline along the node hierarchy. The path of the
        /// spline is determined as follows:
        ///
        /// If <paramref name="source"/> equals <paramref name="target"/>, a
        /// self loop is generated atop of the node.
        ///
        /// If <paramref name="source"/> and <paramref name="target"/> have no
        /// common LCA, the path starts at <paramref name="source"/> and ends
        /// at <paramref name="target"/> and reaches through the point on half
        /// distance between these two nodes, but at the top-most edge height
        /// (given by <paramref name="maxLevel"/>).
        ///
        /// If <paramref name="source"/> and <paramref name="target"/> are
        /// siblings (i.e., they are immediate ancestors of the same parent
        /// node), a direct spline is created between them.
        ///
        /// Otherwise, the control points of the spline are chosen along the
        /// node hierarchy path from the source node to their lowest common
        /// ancestor and then down again to the target node. The height of
        /// each such points is proportional to the level of the node
        /// hierarchy.
        /// </summary>
        /// <param name="source">Starting node.</param>
        /// <param name="target">Ending node.</param>
        /// <param name="lcaFinder">To retrieve the lowest common ancestor of source and target.</param>
        /// <returns>Points to draw a spline between source and target.</returns>
        private TinySpline.BSpline CreateSpline(ILayoutNode source, ILayoutNode target, LCAFinder<ILayoutNode> lcaFinder)
        {
            EqualityComparer<ILayoutNode> comparer = EqualityComparer<ILayoutNode>.Default;
            if (comparer.Equals(source, target))
            {
                return SelfLoop(source, EdgesAboveBlocks, levelDistance);
            }

            // Lowest common ancestor
            ILayoutNode lca = lcaFinder.LCA(source, target);
            if (lca == null)
            {
                // This should never occur if we have a single root node, but may happen if
                // there are multiple roots, in which case nodes in different trees of this
                // forest do not have a common ancestor.
                Debug.LogWarning($"Undefined lowest common ancestor for {source.ID} and {target.ID}\n");
                return BetweenTrees(source, target);
            }

            if (comparer.Equals(lca, source) || comparer.Equals(lca, target))
            {
                // The edge is along a node hierarchy path.
                // We will create a direct spline from source to target at the lowest level.
                return DirectSpline(source, target, levelOffset);
            }
            // assert: sourceObject != targetObject
            // assert: lcaObject != null
            // because the edges are only between leaves:
            // assert: sourceObject != lcaObject
            // assert: targetObject != lcaObject

            ILayoutNode[] sourceToLCA = Ancestors(source, lca);
            ILayoutNode[] targetToLCA = Ancestors(target, lca);

            Array.Reverse(targetToLCA, 0, targetToLCA.Length);

            // Note: lca is included in both paths
            if (sourceToLCA.Length == 2 && targetToLCA.Length == 2)
            {
                // source and target are siblings in the same subtree at the same level.
                // We assume that those nodes are close to each other for all hierarchical layouts,
                // which is true for EvoStreets, Balloon, TreeMap, and CirclePacking. If all edges
                // between siblings were led over one single control point, they would often take
                // a detour even though the nodes are close by. The detour would make it difficult
                // to follow the edges visually.
                return DirectSpline(source, target, levelOffset);
            }

            // Concatenate both paths.
            // We have sufficient many control points without the duplicated LCA,
            // hence, we can remove the duplicated LCA
            ILayoutNode[] fullPath = new ILayoutNode[sourceToLCA.Length + targetToLCA.Length - 1];
            sourceToLCA.CopyTo(fullPath, 0);
            // copy without the first element
            for (int i = 1; i < targetToLCA.Length; i++)
            {
                fullPath[sourceToLCA.Length + i - 1] = targetToLCA[i];
            }
            // Calculate control points along the node hierarchy
            Vector3[] controlPoints = new Vector3[fullPath.Length];
            controlPoints[0] = EdgesAboveBlocks ? source.Roof : source.Ground;
            for (int i = 1; i < fullPath.Length - 1; i++)
            {
                // We consider the height of intermediate nodes.
                // Note that a root has level 0 and the level is increased along
                // the childrens' depth. That is why we need to choose the height
                // as a measure relative to maxLevel.
                // TODO: Do we really want the center position here?
                controlPoints[i] = new Vector3(fullPath[i].CenterPosition.x,
                    GetLevelHeight(fullPath[i].Level),
                    fullPath[i].CenterPosition.z);

            }

            controlPoints[controlPoints.Length - 1] = EdgesAboveBlocks ? target.Roof : target.Ground;
            uint degree = controlPoints.Length >= 4 ? 3 : (uint)controlPoints.Length - 1;
            return new TinySpline.BSpline((uint)controlPoints.Length, 3, degree)
            {
                ControlPoints = TinySplineInterop.VectorsToList(controlPoints)
            }.Tension(tension);
        }

        /// <summary>
        /// Returns a spline for an edge from <paramref name="source"/> to <paramref name="target"/>.
        /// The first point is the center of the roof/ground of <paramref name="source"/> and the last
        /// point the center of the roof/ground of <paramref name="target"/>. The middle peak point
        /// is the position in between <paramref name="source"/> and <paramref name="target"/> where
        /// the y co-ordinate is specified by <paramref name="yLevel"/>.
        /// That means, an edge between the nodes is drawn as a direct spline on the shortest path
        /// between the two nodes from roof/ground to roof/ground. Thus, no hierarchical bundling is applied.
        /// </summary>
        /// <param name="source">The node where to start the edge.</param>
        /// <param name="target">The node where to end the edge.</param>
        /// <param name="yLevel">The y co-ordinate of the two middle control points.</param>
        /// <returns>Control points for a direct spline between the two nodes.</returns>
        private TinySpline.BSpline DirectSpline(ILayoutNode source, ILayoutNode target, float yLevel)
        {
            Vector3 start = EdgesAboveBlocks ? source.Roof : source.Ground;
            Vector3 end = EdgesAboveBlocks ? target.Roof : target.Ground;
            // position in between start and end
            Vector3 middle = Vector3.Lerp(start, end, 0.5f);
            middle.y = yLevel;
            return TinySpline.BSpline.InterpolateCubicNatural(
                TinySplineInterop.VectorsToList(start, middle, end), 3);
        }

        /// <summary>
        /// Returns the y co-ordinate for control points of nodes at the given <paramref name="level"/>
        /// of the node hierarchy. Root nodes are assumed to have level 0. There may be node hierarchies
        /// that are actually forrests rather than simple trees. In such cases, the lowest common ancestor
        /// of nodes in different trees does not exist and -1 will be passed as <paramref name="level"/>,
        /// which is perfectly acceptable. In such cases, the returned value will be just one levelDistance
        /// above those for normal root nodes:
        ///     levelOffset +/- (maxLevel + 1) * levelDistance  (+ if edgesAboveBlocks; otherwise -)
        /// If level = maxLevel, levelOffset will be returned.
        /// In all other cases the returned value is guaranteed to be greater (or smaller if edges
        /// are to be drawn below blocks, respectively) than levelOffset.
        /// </summary>
        /// <param name="level">Node hierarchy level.</param>
        /// <returns>Y co-ordinate for control points.</returns>
        private float GetLevelHeight(int level)
        {
            float relativeLevelDistance = (maxLevel - level) * levelDistance;
            if (EdgesAboveBlocks)
            {
                return levelOffset + relativeLevelDistance;
            }
            else
            {
                return levelOffset - relativeLevelDistance;
            }
        }

        /// <summary>
        /// Yields line points for two nodes that do not have a common ancestor in the
        /// node hierarchy. This may occur when we have multiple roots in the graph, that
        /// is, the node hierarchy is a forest and not just a single tree. In this case,
        /// we want the spline to reach above/below all other splines of nodes having a common
        /// ancestor.
        /// The first and last points are the respective roofs/grounds of <paramref name="source"/>
        /// and <paramref name="target"/>. The peak point of the direct spline lies in between the
        /// two nodes with respect to the x and z axis; its height (y axis) is the highest
        /// hierarchical level, that is, one levelDistance above all other edges within the same
        /// trees.
        /// </summary>
        /// <param name="source">Start of edge (in one tree).</param>
        /// <param name="target">End of the edge (in another tree).</param>
        /// <returns>Line points for two nodes without common ancestor.</returns>
        private TinySpline.BSpline BetweenTrees(ILayoutNode source, ILayoutNode target)
        {
            return DirectSpline(source, target, GetLevelHeight(-1));
        }
    }
}
