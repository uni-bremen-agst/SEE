using SEE.Layout.NodeLayouts.RectanglePacking;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using MoreLinq;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// Simple rectangle layout that places nodes in a line
    /// and sorts them descending by Z inside the rectangle.
    /// </summary>
    public class IncrementalRectanglePackingLayout : NodeLayout, IIncrementalNodeLayout
    {
        static IncrementalRectanglePackingLayout()
        {
            Name = "Incremental Rectangle Packing Layout";
        }

        public IncrementalRectanglePackingLayout oldLayout;

        public IIncrementalNodeLayout OldLayout
        {
            set
            {
                if (value is IncrementalRectanglePackingLayout layout)
                {
                    oldLayout = layout;
                }
                else
                {
                    throw new ArgumentException(
                        $"Predecessor of {nameof(IncrementalRectanglePackingLayout)} was not an {nameof(IncrementalRectanglePackingLayout)}.");
                }
            }
        }


        public Dictionary<ILayoutNode, NodeTransform> layoutResult;


        public static bool changedOrDeleted = false;
        //                       parentID list of (id, position, size) , coverec
        public static Dictionary<string, (List<(string, Vector2, Vector2)>, Vector2)> lastPositions;



        protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
        {
            layoutResult = new Dictionary<ILayoutNode, NodeTransform>();
            ThirdScenario(layoutNodes.ToList(), centerPosition, rectangle);

            return layoutResult;

        }

        public void ThirdScenario(List<ILayoutNode> leafNodes, Vector3 centerPosition, Vector2 rectangle)
        {

            if (oldLayout == null)
            {
                lastPositions = new Dictionary<string, (List<(string, Vector2, Vector2)>, Vector2)>();
            }

            string rootLayoutNodeID = leafNodes.First().Parent != null ? leafNodes.First().Parent.ID : null;

            IList<ILayoutNode> layoutNodeList = leafNodes.ToList();
            if (layoutNodeList.Count == 1)
            {

                ILayoutNode layoutNode = layoutNodeList.First();
                layoutResult[layoutNode] = new NodeTransform(0, 0, layoutNode.AbsoluteScale);
                return;
            }

            {
                int numberOfLeaves = 0;
                foreach (ILayoutNode node in layoutNodeList)
                {
                    if (node.IsLeaf)
                    {

                        Vector3 scale = node.AbsoluteScale;
                        layoutResult[node] = new NodeTransform(0, 0, scale);
                        numberOfLeaves++;
                    }
                }
                if (numberOfLeaves == layoutNodeList.Count)
                {
                    // There are only leaves.
                    Pack(layoutResult, layoutNodeList.Cast<ILayoutNode>().ToList(), GroundLevel);
                    return;
                }
            }


            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(leafNodes);
            if (roots.Count == 1)
            {
                ILayoutNode root = roots.FirstOrDefault();
                Vector2 area = PlaceNodes(layoutResult, root, GroundLevel);
                layoutResult[root] = new NodeTransform(0, 0, new Vector3(area.x, root.AbsoluteScale.y, area.y));
                MakeContained(layoutResult, root);
                return;
            }
            else
            {
                Debug.Log("multiple or zero roots");
                foreach (ILayoutNode leafNode in leafNodes)
                {

                    layoutResult[leafNode] = new NodeTransform(
                        0,
                        0,
                        leafNode.AbsoluteScale
                    );
                }

                Pack(layoutResult, leafNodes.Cast<ILayoutNode>().ToList(), GroundLevel, rootLayoutNodeID);
            }
        }
        public Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
        {
            if (node.IsLeaf)
            {
                return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
            }
            else
            {
                ICollection<ILayoutNode> children = node.Children();

                foreach (ILayoutNode child in children)
                {
                    if (!child.IsLeaf)
                    {
                        Vector2 childArea = PlaceNodes(layout, child, groundLevel);
                        layout[child] = new NodeTransform(0, 0,
                                                          new Vector3(childArea.x, child.AbsoluteScale.y, childArea.y));

                    }
                }
                if (children.Count > 0)
                {
                    Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel, node.ID);
                    return new Vector2(area.x, area.y);
                }
                else
                {
                    return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
                }
            }
        }

        private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, string parent = null)
        {
            string parentID = parent == null ? "dummy" : parent;
            PTree tree = new(Vector2.zero, Vector2.zero);

            var coverec = PerformHistoryNew(layout, nodes, parentID, ref tree);
            //tree.Tighten(tree.Root);
            ResetCoverec(ref tree);
            PlaceNodesInLayout(ref layout, ref nodes, parent, ref tree);
            return coverec;

        }

        private static void MakeContained(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode parent)
        {
            /*
            // The x co-ordinate of the left lower corner of the parent.
            // The z co-ordinate of the left lower corner of the parent.
             */
            NodeTransform parentTransform = layout[parent];
            Vector3 parentExtent = parentTransform.Scale / 2.0f;
            float xCorner = parentTransform.X - parentExtent.x;
            float zCorner = parentTransform.Z - parentExtent.z;

            foreach (ILayoutNode child in parent.Children())
            {
                //Debug.Log("Making contained: " + child.ID);
                layout[child].MoveBy(xCorner, zCorner);
                MakeContained(layout, child);
            }
        }

        public void PlaceNodesInLayout(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, string parent, ref PTree tree)
        {
            foreach (ILayoutNode el in nodes)
            {
                //Debug.Log(el.Print());
                if (!layout.ContainsKey(el))
                {
                    continue;
                }
                PNode fitNode = tree.FindNodeById2(el.ID);

                if (fitNode == null)
                {
                    Debug.Log("fitnode is null" + el.ID);
                    continue;

                }
                Vector3 scale = layout[el].Scale;
                layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                               fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                               scale, fitNode);

            }

            //PrintHistory();
            //tree.Print1();
            //Debug.Log("1********************************************************************************************************");
        }

        public void ResizeNodesInPTree1(List<(string, Vector2)> sameIDsNewSizes, ref PTree tree)
        {
            if (sameIDsNewSizes.Count == 0)
                Debug.Log("sameIDsNewSizes is empty.");

            foreach ((string sameID, Vector2 size) in sameIDsNewSizes)
            {
                Vector2 requiredSize = size;
                PNode targetPNode = tree.FindNodeById2(sameID);

                if (targetPNode != null)
                {
                    if (targetPNode.Rectangle.Size == requiredSize) continue;
                    else
                    {
                        tree.GrowLeaf2(targetPNode, new Vector3(requiredSize.x, 1, requiredSize.y));

                        Vector2 corner = targetPNode.Rectangle.Position + size;
                        Vector2 expandedCoveRec = new(Mathf.Max(tree.coverec.x, corner.x), Mathf.Max(tree.coverec.y, corner.y));
                        if (!PTree.FitsInto(expandedCoveRec, tree.coverec))
                        {
                            tree.coverec = expandedCoveRec;
                        }

                    }
                }
                else
                {
                    continue;
                }
            }
        }

        public Vector2 PerformHistoryNew(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, string parent, ref PTree tree)
        {
            SortNodesByAreaSize(nodes, layout);
            Vector2 worstCaseSize = Sum(nodes, layout);
            tree.Root.Rectangle.Size = worstCaseSize * 1.1f;
            tree.Root.Rectangle.Position = Vector2.zero;

            List<(string, Vector2)> newNodeIDsSizes = new List<(string, Vector2)>();
            List<(string, Vector2)> sameIDsNewSizes = new List<(string, Vector2)>();
            List<PNode> rests = new List<PNode>();

            var bufferLastPos = lastPositions.FirstOrDefault(p => p.Key == parent).Value;

            if (bufferLastPos != default)
            {
                tree.coverec = bufferLastPos.Item2;

                foreach (ILayoutNode n in nodes)
                {
                    (string, Vector2, Vector2) tupple = bufferLastPos.Item1.FirstOrDefault(l => l.Item1 == n.ID);
                    if (tupple != default)
                    {
                        PNode pn = new PNode(tupple.Item2, tupple.Item3, tupple.Item1);
                        //Debug.Log("ID " + pn.Id + "  position " + pn.Rectangle.Position + " size " + pn.Rectangle.Size);
                        pn.Parent = tree.Root;

                        tree.Root.Rests.Add(pn);
                        pn.Occupied = true;
                        rests.Add(pn);
                    }
                }


                List<ILayoutNode> placedRectangles = nodes.Where(n => rests.Any(r => r.Id == n.ID)).ToList();


                sameIDsNewSizes = placedRectangles.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();

                ResizeNodesInPTree1(sameIDsNewSizes, ref tree);
                tree.Tighten(tree.Root);


                List<ILayoutNode> notPlacedRectangles = nodes.Where(n => !rests.Any(r => r.Id == n.ID)).ToList();

                newNodeIDsSizes = notPlacedRectangles.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();

                if (newNodeIDsSizes.Count > 0)
                    PlaceNodesInPTreeNew(newNodeIDsSizes, ref tree, parent);

                ResolveAndExpand(tree.Root, tree.Root.Rests);

                List<(string, Vector2, Vector2)> allPlacedRectangles = tree.Root.Rests.Select(n => (n.Id, new Vector2(n.XX, n.YY), new Vector2(n.Width, n.Height))).ToList();
                lastPositions[parent] = (allPlacedRectangles, tree.coverec);

                return tree.coverec;
            }
            else
            {
                newNodeIDsSizes = nodes.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();
                PlaceNodesInPTreeNew(newNodeIDsSizes, ref tree, parent);
                tree.Tighten(tree.Root);
                ResolveAndExpand(tree.Root, tree.Root.Rests);

                lastPositions[parent] = (tree.Root.Rests.Select(n => (n.Id, n.Position, n.Size)).ToList(), tree.coverec);

                return tree.coverec;
            }
        }

        public void PlaceNodesInPTreeNew(List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, string parent)
        {

            Vector2 coverec = tree.coverec; // fix me each node should have its own coverec and tree which is not defined here u cant simply have one coverec for all nodes in the level because they can be in different subtrees of the root and thus have different coverecs and also when you place a node in the tree it can change the coverec of its subtree but not necessarily the coverec of the whole tree so you need to keep track of coverecs on a more granular level and not just one coverec for the whole tree

            foreach ((string newID, Vector2 size) in newNodeIDsSizes)
            {
                Vector2 requiredSize = size;

                Dictionary<PNode, float> preservers = new();
                Dictionary<PNode, float> expanders = new();
                tree.FreeLeaves = tree.FindEmpty(tree.Root, tree.Root.Rests);

                IList<PNode> sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize, Vector2.zero);

                if (sufficientLargeLeaves.Count == 0)
                {
                    Debug.Log("--------------------------------------------------------------------------------------------------------------");
                    tree.Print1();
                    Debug.Log("--------------------------------------------------------------------------------------------------------------");
                    if (tree.FreeLeaves.Count == 0) Debug.Log("no free leaves");
                    else Debug.Log("free leaves: " + tree.FreeLeaves.Count);
                    foreach (PNode freeLeaf in tree.FreeLeaves)
                    {
                        if (freeLeaf != null) Debug.Log(freeLeaf.ToString1());
                        else Debug.Log("free leaf is null");
                    }
                    Debug.Log("--------------------------------------------------------------------------------------------------------------");

                    throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": :" + requiredSize + ": " + tree.coverec + " : " + tree.Root.Rectangle.Size + " : " + tree.Root.Rectangle.Size);
                }
                foreach (PNode pnode in sufficientLargeLeaves)
                {
                    Vector2 corner = pnode.Rectangle.Position + requiredSize;
                    Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));

                    //Debug.Log(expandedCoveRec + " " + coverec);

                    if (PTree.FitsInto(expandedCoveRec, coverec))
                    {
                        float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
                        preservers[pnode] = waste;
                        //Debug.Log("added to preservers");
                    }
                    else
                    {

                        float ratio = expandedCoveRec.x / expandedCoveRec.y;
                        expanders[pnode] = Mathf.Abs(ratio - 1);

                        //Debug.Log("added to extenders");
                    }

                }
                PNode targetNode = null;
                if (preservers.Count > 0)
                {
                    float lowestWaste = Mathf.Infinity;
                    foreach (KeyValuePair<PNode, float> entry in preservers)
                    {
                        if (entry.Value < lowestWaste)
                        {

                            targetNode = entry.Key;
                            lowestWaste = entry.Value;
                        }
                    }
                }
                else
                {

                    Single minValue = expanders.Values.Min();

                    IEnumerable<KeyValuePair<PNode, float>> candidates = expanders
                        .Where(kv => kv.Value == minValue);

                    KeyValuePair<PNode, float>? best = null;

                    foreach (KeyValuePair<PNode, float> kv in candidates)
                    {
                        Single area = kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y;

                        if (best == null)
                        {
                            best = kv;
                        }
                        else
                        {
                            Single bestArea = best.Value.Key.Rectangle.Size.x * best.Value.Key.Rectangle.Size.y;

                            if (area < bestArea)
                            {
                                best = kv;
                            }
                        }
                    }

                    targetNode = best?.Key;

                }
                if (targetNode == null)
                {
                    Debug.LogError("targetNode is null!");
                    continue;
                }

                PNode fitNode = new PNode(targetNode.Rectangle.Position, requiredSize, newID);
                tree.Root.Rests.Add(fitNode);
                fitNode.Parent = tree.Root;
                fitNode.Occupied = true;

                {

                    Vector2 corner = fitNode.Rectangle.Position + size;
                    Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));
                    if (!PTree.FitsInto(expandedCoveRec, coverec))
                    {
                        coverec = expandedCoveRec;
                        tree.coverec = coverec;
                    }
                }


            }
        }

        public void ResolveAndExpand(PNode parent, List<PNode> nodes, int maxExpansions = 10, int iterationsPerPass = 50)
        {
            float expansionFactor = 1.15f; // Grow the parent by 15% when out of space

            for (int attempt = 0; attempt < maxExpansions; attempt++)
            {
                // 1. Run the separation algorithm within current bounds
                for (int iter = 0; iter < iterationsPerPass; iter++)
                {
                    bool movedAny = false;

                    // Push overlapping nodes apart
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        for (int j = i + 1; j < nodes.Count; j++)
                        {
                            PNode a = nodes[i];
                            PNode b = nodes[j];

                            float aCenterX = a.Position.x + (a.Width / 2f);
                            float aCenterY = a.Position.y + (a.Height / 2f);
                            float bCenterX = b.Position.x + (b.Width / 2f);
                            float bCenterY = b.Position.y + (b.Height / 2f);

                            float distX = bCenterX - aCenterX;
                            float distY = bCenterY - aCenterY;

                            if (distX == 0f && distY == 0f) distX = 0.01f;

                            float minX = (a.Width / 2f) + (b.Width / 2f);
                            float minY = (a.Height / 2f) + (b.Height / 2f);

                            // If overlapping
                            if (Mathf.Abs(distX) < minX && Mathf.Abs(distY) < minY)
                            {
                                float overlapX = distX > 0 ? minX - distX : -minX - distX;
                                float overlapY = distY > 0 ? minY - distY : -minY - distY;

                                Vector2 posA = a.Position;
                                Vector2 posB = b.Position;

                                if (Mathf.Abs(overlapX) < Mathf.Abs(overlapY))
                                {
                                    posA.x -= overlapX / 2f;
                                    posB.x += overlapX / 2f;
                                }
                                else
                                {
                                    posA.y -= overlapY / 2f;
                                    posB.y += overlapY / 2f;
                                }

                                a.Rectangle.Position = posA;
                                b.Rectangle.Position = posB;
                                movedAny = true;
                            }
                        }
                    }

                    // Clamp to the parent's current bounds
                    foreach (var node in nodes)
                    {
                        Vector2 pos = node.Position;

                        if (pos.x < parent.Position.x) pos.x = parent.Position.x;
                        if (pos.y < parent.Position.y) pos.y = parent.Position.y;

                        if (pos.x + node.Width > parent.Position.x + parent.Width)
                            pos.x = parent.Position.x + parent.Width - node.Width;

                        if (pos.y + node.Height > parent.Position.y + parent.Height)
                            pos.y = parent.Position.y + parent.Height - node.Height;

                        node.Rectangle.Position = pos;
                    }

                    // If nothing had to move, the layout is completely stable!
                    if (!movedAny) break;
                }

                // 2. Verify if we successfully separated everything
                if (!HasOverlaps(nodes))
                {
                    // Success! Shrink-wrap the parent to tightly fit the final layout to save space.
                    TrimParentToFit(parent, nodes);
                    return;
                }

                // 3. If overlaps STILL exist, they are jammed. Expand the parent symmetrically!
                ExpandParent(parent, expansionFactor);
            }

            // Final fallback: If we hit max expansions, just wrap whatever state is left.
            TrimParentToFit(parent, nodes);
        }

        private bool HasOverlaps(List<PNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    PNode a = nodes[i];
                    PNode b = nodes[j];

                    float distX = (b.Position.x + b.Width / 2f) - (a.Position.x + a.Width / 2f);
                    float distY = (b.Position.y + b.Height / 2f) - (a.Position.y + a.Height / 2f);

                    float minX = (a.Width / 2f) + (b.Width / 2f);
                    float minY = (a.Height / 2f) + (b.Height / 2f);

                    if (Mathf.Abs(distX) < minX && Mathf.Abs(distY) < minY)
                        return true;
                }
            }
            return false;
        }

        private void ExpandParent(PNode parent, float factor)
        {
            float newWidth = parent.Width * factor;
            float newHeight = parent.Height * factor;

            // Calculate offset to ensure it expands from the center, not just the top-right
            float offsetX = (newWidth - parent.Width) / 2f;
            float offsetY = (newHeight - parent.Height) / 2f;

            parent.Rectangle.Position = new Vector2(parent.Position.x - offsetX, parent.Position.y - offsetY);
            parent.Rectangle.Size = new Vector2(newWidth, newHeight);
        }

        private void TrimParentToFit(PNode parent, List<PNode> nodes)
        {
            if (nodes.Count == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            // Find the absolute min and max bounds of all child nodes
            foreach (var node in nodes)
            {
                if (node.Position.x < minX) minX = node.Position.x;
                if (node.Position.y < minY) minY = node.Position.y;
                if (node.Position.x + node.Width > maxX) maxX = node.Position.x + node.Width;
                if (node.Position.y + node.Height > maxY) maxY = node.Position.y + node.Height;
            }

            parent.Rectangle.Position = new Vector2(minX, minY);
            parent.Rectangle.Size = new Vector2(maxX - minX, maxY - minY);
        }


        public void ResetCoverec(ref PTree tree)
        {
            List<Vector2> pnodes = tree.Root.Rests
              .Select(n => n.Rectangle.Position + n.Rectangle.Size)
              .ToList();
            Vector2 max = Vector2.zero;
            foreach (Vector2 corner in pnodes)
            {
                max = new Vector2(
                    Mathf.Max(max.x, corner.x),
                    Mathf.Max(max.y, corner.y)
                );
            }
            tree.coverec = max;
        }

        public static Vector2 GetRectangleSize(NodeTransform node)
        {
            Vector3 size = node.Scale;
            return new Vector2(size.x, size.z);
        }


        public static Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
        {
            Vector2 result = Vector2.zero;
            foreach (ILayoutNode element in nodes)
            {
                if (!layout.ContainsKey(element))
                {
                    Debug.LogWarning("Layout does not contain element************************************** " + element.ID);
                    continue;
                }

                Vector3 size = layout[element].Scale;
                result.x += size.x;
                result.y += size.z;

            }
            return result;
        }

        private static void SortNodesByAreaSize(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
        {
            nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
            { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });
        }

        public static float AreaSize(NodeTransform node)
        {
            Vector3 size = node.Scale;
            return size.x * size.z;
        }




    }
}