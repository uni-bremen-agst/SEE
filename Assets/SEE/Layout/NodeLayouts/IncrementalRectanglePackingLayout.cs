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
    /// This layout packs rectangles closely together as a set of nested packed rectangles to decrease
    /// the total area of city. It also ensures that the layout is incremental, meaning that if a node 
    /// is added or removed, the layout will adjust accordingly without having to recompute the entire 
    /// layout from scratch. The algorithm finds the best position for each rectangle based on its size 
    /// and the available space.
    /// </summary>
    public class IncrementalRectanglePackingLayout : NodeLayout, IIncrementalNodeLayout
    {
        static IncrementalRectanglePackingLayout()
        {
            Name = "Incremental Rectangle Packing Layout";
        }

        /// <summary>
        /// A reference to the layout calculated in the previous frame or state. 
        /// This is strictly required for the "incremental" aspect of the layout, as the algorithm 
        /// uses the positions from this old layout to try and keep nodes as close to their previous 
        /// positions as possible, maintaining the user's mental map of the visualization.
        /// </summary>
        public IncrementalRectanglePackingLayout oldLayout;

        /// <summary>
        /// Implements the IIncrementalNodeLayout interface property. Provides a safe setter to 
        /// inject the previous layout instance. It ensures type safety by throwing an ArgumentException 
        /// if the provided predecessor is not specifically an IncrementalRectanglePackingLayout.
        /// </summary>
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

        /// <summary>
        /// The primary working dictionary that stores the calculated scaling and positional data 
        /// (NodeTransform) for each node (ILayoutNode) currently being processed by the algorithm. 
        /// It is populated during the layout passes and ultimately returned to the engine.
        /// </summary>
        public Dictionary<ILayoutNode, NodeTransform> layoutResult;

        /// <summary>
        /// A cache storing the positional history of rectangles. 
        /// Key: Parent Node ID (or "dummy" for the root). 
        /// Value: A tuple containing a List of child nodes (Node ID, Position, Size) and a Vector2 representing 
        /// the bounding box (coverec) of that parent group. This dictionary is vital for maintaining spatial stability across updates.
        /// </summary>
        public Dictionary<string, (List<(string, Vector2, Vector2)>, Vector2)> lastPositions;


        /// <summary>
        /// The main entry point triggered by the layout engine. It initializes the result dictionary 
        /// and delegates the core layout calculations to the <see cref="ThirdScenario"/> method before returning the final transforms.
        /// </summary>
        /// <param name="layoutNodes">The collection of nodes that need to be laid out.</param>
        /// <param name="centerPosition">The target center position in the world space.</param>
        /// <param name="rectangle">The bounding area available for the layout.</param>
        /// <returns>A dictionary mapping every processed node to its calculated transform.</returns>
        protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
        {
            layoutResult = new Dictionary<ILayoutNode, NodeTransform>();
            ThirdScenario(layoutNodes.ToList(), centerPosition, rectangle);

            return layoutResult;

        }

        /// <summary>
        /// The core orchestrator method of the packing algorithm. It evaluates the structure of the input 
        /// nodes (e.g., single node, flat list of leaves, single root tree, or multi-root forest) and routes 
        /// them to the appropriate packing logic. It also initializes the lastPositions cache.
        /// </summary>
        /// <param name="leafNodes">The list of nodes to be processed.</param>
        /// <param name="centerPosition">The target spatial center (currently unused directly in this block).</param>
        /// <param name="rectangle">The overall space constraints.</param>
        public void ThirdScenario(List<ILayoutNode> leafNodes, Vector3 centerPosition, Vector2 rectangle)
        {

            if (oldLayout == null)
            {
                lastPositions = new Dictionary<string, (List<(string, Vector2, Vector2)>, Vector2)>();
            }
            else
            {
                lastPositions = oldLayout.lastPositions;
            }

            string rootLayoutNodeID = leafNodes.First().Parent != null ? leafNodes.First().Parent.ID : null;

            IList<ILayoutNode> layoutNodeList = leafNodes.ToList();
            // Handle Edge Case 1: Only a single node exists
            if (layoutNodeList.Count == 1)
            {

                ILayoutNode layoutNode = layoutNodeList.First();
                layoutResult[layoutNode] = new NodeTransform(0, 0, layoutNode.AbsoluteScale);
                return;
            }

            // Handle Edge Case 2: The list contains exclusively leaf nodes (no nested hierarchy)
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

            // Handle Hierarchy: If there are trees/graphs, resolve them from the roots down
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


        /// <summary>
        /// Recursively calculates the bounding area for a parent node by first packing all of its children. 
        /// Once the children are packed into a minimal rectangle, the parent's area is determined and updated in the layout.
        /// </summary>
        /// <param name="layout">The current state of node transforms.</param>
        /// <param name="node">The node being evaluated (could be a leaf or a parent container).</param>
        /// <param name="groundLevel">The Y-axis level representing the floor base.</param>
        /// <returns>A Vector2 representing the required width (x) and depth (y) to encapsulate this node and all its children.</returns>
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

        /// <summary>
        /// Helper wrapper method that executes the localized packing sequence for a specific group of nodes.
        /// It initializes a PTree, runs the history-aware packing logic, recalculates boundaries, and syncs the 
        /// local positions back to the main layout dictionary.
        /// </summary>
        /// <param name="layout">The dictionary to update with calculated positions.</param>
        /// <param name="nodes">The local subset of nodes to pack together.</param>
        /// <param name="groundLevel">The vertical Y floor.</param>
        /// <param name="parent">The ID of the parent node (used for caching/history lookup).</param>
        /// <returns>The calculated bounding box (coverec) representing the total space used by these packed nodes.</returns>
        private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, string parent = null)
        {
            string parentID = parent == null ? "dummy" : parent;
            PTree tree = new(Vector2.zero, Vector2.zero);

            var coverec = PerformHistory(layout, nodes, parentID, ref tree);
            //tree.Tighten(tree.Root);
            ResetCoverec(ref tree);
            PlaceNodesInLayout(ref layout, ref nodes, parent, ref tree);
            return coverec;

        }

        /// <summary>
        /// Translates child nodes from local, center-based coordinates relative to their parent into 
        /// absolute world coordinates by offsetting them by the parent's bottom-left corner coordinates.
        /// </summary>
        /// <param name="layout">The dictionary containing current node transforms.</param>
        /// <param name="parent">The parent container node whose boundaries constrain the children.</param>
        private static void MakeContained(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode parent)
        {
            // The x co-ordinate of the left lower corner of the parent.
            // The z co-ordinate of the left lower corner of the parent.
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

        /// <summary>
        /// Synchronizes the positional data calculated inside the mathematical PTree back into the 
        /// main NodeTransform dictionary used by the game engine/rendering system. Ensures that scale 
        /// dimensions correspond perfectly to the center-points expected by the engine.
        /// </summary>
        /// <param name="layout">Reference to the main layout results.</param>
        /// <param name="nodes">List of nodes being updated.</param>
        /// <param name="parent">ID of the parent node context.</param>
        /// <param name="tree">The PTree containing the resolved rectangle physics.</param>
        public void PlaceNodesInLayout(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, string parent, ref PTree tree)
        {
            foreach (ILayoutNode el in nodes)
            {
                //Debug.Log(el.Print());
                if (!layout.ContainsKey(el))
                {
                    continue;
                }
                PNode fitNode = tree.FindNodeById(el.ID);

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

        }

        /// <summary>
        /// Evaluates nodes that were present in the previous frame but have changed size. It adjusts 
        /// their size in the PTree directly and dynamically expands the bounding rectangle (coverec) 
        /// of the tree if the grown node extends beyond current limits.
        /// </summary>
        /// <param name="sameIDsNewSizes">A list of tuples pairing an existing node ID with its updated required size.</param>
        /// <param name="tree">The active partition tree being manipulated.</param>
        public void ResizeNodesInPTree(List<(string, Vector2)> sameIDsNewSizes, ref PTree tree)
        {
            if (sameIDsNewSizes.Count == 0)
                Debug.Log("sameIDsNewSizes is empty.");

            foreach ((string sameID, Vector2 size) in sameIDsNewSizes)
            {
                Vector2 requiredSize = size;
                PNode targetPNode = tree.FindNodeById(sameID);

                if (targetPNode != null)
                {
                    if (targetPNode.Rectangle.Size == requiredSize) continue;
                    else
                    {
                        tree.GrowLeaf(targetPNode, new Vector3(requiredSize.x, 1, requiredSize.y));

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

        /// <summary>
        /// The heart of the incremental packing algorithm. It restores nodes to their previous historical positions 
        /// to maintain layout stability, resizes them if they changed, packs entirely new nodes into available empty spaces, 
        /// and then runs an overlap physics solver to push overlapping nodes apart before caching the new stable state.
        /// </summary>
        /// <param name="layout">The current node transforms.</param>
        /// <param name="nodes">The nodes participating in this layout group.</param>
        /// <param name="parent">The parent ID used to fetch historical data.</param>
        /// <param name="tree">The spatial partition tree used to perform the geometric math.</param>
        /// <returns>The calculated boundary size required for this group of nodes.</returns>
        public Vector2 PerformHistory(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, string parent, ref PTree tree)
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
                // Reconstruct old layout state
                tree.coverec = bufferLastPos.Item2;

                foreach (ILayoutNode n in nodes)
                {
                    (string, Vector2, Vector2) tupple = bufferLastPos.Item1.FirstOrDefault(l => l.Item1 == n.ID);
                    if (tupple != default)
                    {
                        PNode pn = new PNode(tupple.Item2, tupple.Item3, tupple.Item1);
                        pn.Parent = tree.Root;

                        tree.Root.Rests.Add(pn);
                        pn.Occupied = true;
                        rests.Add(pn);
                    }
                }

                // Update existing nodes with new dimensions
                List<ILayoutNode> placedRectangles = nodes.Where(n => rests.Any(r => r.Id == n.ID)).ToList();


                sameIDsNewSizes = placedRectangles.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();

                ResizeNodesInPTree(sameIDsNewSizes, ref tree);
                tree.Tighten(tree.Root);

                // Add newly introduced nodes
                List<ILayoutNode> notPlacedRectangles = nodes.Where(n => !rests.Any(r => r.Id == n.ID)).ToList();

                newNodeIDsSizes = notPlacedRectangles.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();

                if (newNodeIDsSizes.Count > 0)
                {
                    PlaceNodesInPTree(newNodeIDsSizes, ref tree, parent);
                }

                // Resolve any overlaps caused by resizing or newly placed nodes
                ResolveAndExpand(tree.Root, tree.Root.Rests);

                // Cache state for the next frame
                List<(string, Vector2, Vector2)> allPlacedRectangles = tree.Root.Rests.Select(n => (n.Id, new Vector2(n.XX, n.YY), new Vector2(n.Width, n.Height))).ToList();
                lastPositions[parent] = (allPlacedRectangles, tree.coverec);

                return tree.coverec;
            }
            else
            {
                // Initial layout scenario (no history)
                newNodeIDsSizes = nodes.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();
                PlaceNodesInPTree(newNodeIDsSizes, ref tree, parent);
                tree.Tighten(tree.Root);
                ResolveAndExpand(tree.Root, tree.Root.Rests);

                lastPositions[parent] = (tree.Root.Rests.Select(n => (n.Id, n.Position, n.Size)).ToList(), tree.coverec);

                return tree.coverec;
            }
        }

        /// <summary>
        /// Inserts entirely new nodes into the PTree. It searches the tree for empty spaces (FreeLeaves) 
        /// and analyzes whether placing a new node there fits neatly (a "preserver") or forces the boundary 
        /// to grow (an "expander"). It attempts to place the node in a way that minimizes wasted empty space.
        /// </summary>
        /// <param name="newNodeIDsSizes">List of new nodes and their requested dimensions.</param>
        /// <param name="tree">The active partition tree.</param>
        /// <param name="parent">The ID of the parent context.</param>
        public void PlaceNodesInPTree(List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, string parent)
        {

            Vector2 coverec = tree.coverec; 

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
                    tree.PrintA();
                    Debug.Log("--------------------------------------------------------------------------------------------------------------");
                    if (tree.FreeLeaves.Count == 0) Debug.Log("no free leaves");
                    else Debug.Log("free leaves: " + tree.FreeLeaves.Count);
                    foreach (PNode freeLeaf in tree.FreeLeaves)
                    {
                        if (freeLeaf != null) Debug.Log(freeLeaf.ToStringNotOverride());
                        else Debug.Log("free leaf is null");
                    }
                    Debug.Log("--------------------------------------------------------------------------------------------------------------");

                    throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": :" + requiredSize + ": " + tree.coverec + " : " + tree.Root.Rectangle.Size + " : " + tree.Root.Rectangle.Size);
                }
                foreach (PNode pnode in sufficientLargeLeaves)
                {
                    Vector2 corner = pnode.Rectangle.Position + requiredSize;
                    Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));

                    if (PTree.FitsInto(expandedCoveRec, coverec))
                    {
                        float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
                        preservers[pnode] = waste;
                    }
                    else
                    {
                        float ratio = expandedCoveRec.x / expandedCoveRec.y;
                        expanders[pnode] = Mathf.Abs(ratio - 1);
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

        /// <summary>
    /// A physics-based relaxation algorithm that resolves overlaps between nodes caused by incremental updates.
    /// It iteratitively pushes overlapping rectangles apart along the axis of least penetration. 
    /// If the rectangles become jammed and cannot separate within the parent's current size, the parent 
    /// dynamically expands to provide more room, and the process repeats. Finally, it shrink-wraps the parent.
    /// </summary>
    /// <param name="parent">The parent PNode acting as the enclosing boundary.</param>
    /// <param name="nodes">The list of child PNodes to separate.</param>
    /// <param name="maxExpansions">The maximum number of times the parent boundary is allowed to expand.</param>
    /// <param name="iterationsPerPass">The number of separation physics steps taken per expansion attempt.</param>
    public void ResolveAndExpand(PNode parent, List<PNode> nodes, int maxExpansions = 20, int iterationsPerPass = 100)
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

        /// <summary>
        /// A helper method for the overlap physics solver. It strictly verifies whether any two 
        /// rectangles in the provided list are intersecting mathematically based on their center distances.
        /// </summary>
        /// <param name="nodes">The list of nodes to evaluate.</param>
        /// <returns>True if at least one overlap exists; otherwise, false.</returns>
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

        /// <summary>
        /// Helper function for the physics solver. It uniformly expands the boundaries of a parent 
        /// container outward from its central point, providing more area for overlapping children to spread into.
        /// </summary>
        /// <param name="parent">The parent node whose boundaries will be grown.</param>
        /// <param name="factor">The multiplier applied to width and height (e.g., 1.15 for +15%).</param>
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

        /// <summary>
        /// A cleanup utility utilized after overlaps are resolved. It evaluates the absolute minimum and 
        /// maximum coordinates occupied by all child nodes and perfectly shrinks the parent bounding box to wrap them, eliminating wasted padding.
        /// </summary>
        /// <param name="parent">The enclosing parent node to be shrunk.</param>
        /// <param name="nodes">The children nodes dictating the final required area.</param>
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

        /// <summary>
        /// Recalculates the maximum top-right corner point occupied by any active node in the tree. 
        /// This establishes the "coverec" (covering rectangle) property, representing the true bounds of the active layout.
        /// </summary>
        /// <param name="tree">The partition tree whose coverec boundary needs recalculation.</param>
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


        /// <summary>
        /// A quick heuristic calculation that adds up the raw width and height of all requested nodes. 
        /// Used by PerformHistory to estimate a guaranteed safe "worst-case scenario" starting size 
        /// for a bounding box prior to doing exact packing.
        /// </summary>
        /// <param name="nodes">The collection of nodes being measured.</param>
        /// <param name="layout">The dictionary providing the target scales/dimensions of the nodes.</param>
        /// <returns>A Vector2 representing the linear sum of all widths (x) and depths (y).</returns>
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

        /// <summary>
        /// A utility helper that sorts a list of nodes in descending order based on their physical area size. 
        /// Packing larger nodes first usually results in a tighter, more optimized layout because smaller 
        /// nodes can later be slotted into the remaining narrow gaps.
        /// </summary>
        /// <param name="nodes">The list of nodes to sort in-place.</param>
        /// <param name="layout">The dictionary used to look up the dimensions for each node.</param>
        private static void SortNodesByAreaSize(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
        {
            nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
            { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });
        }

        /// <summary>
        /// Mathematical helper that extracts the two-dimensional layout area (Width x Depth) 
        /// of a specific node from its 3D Unity scale vector. Note that Y is vertical height and is ignored.
        /// </summary>
        /// <param name="node">The transform container holding the scale data.</param>
        /// <returns>The calculated floating-point area.</returns>
        public static float AreaSize(NodeTransform node)
        {
            Vector3 size = node.Scale;
            return size.x * size.z;
        }

    }
}
