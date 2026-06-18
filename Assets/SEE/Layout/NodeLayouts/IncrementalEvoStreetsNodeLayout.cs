using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
     /// <summary>
    /// Inkrementelle Version von EvoStreets.
    /// Sie behält die alte Reihenfolge bestehender Geschwisterknoten so weit wie möglich bei
    /// und hängt neu hinzugefügte Knoten an die Enden der Straßenachse an.
    /// </summary>
    public class IncrementalEvoStreetsNodeLayout : EvoStreetsNodeLayout
    {
        static IncrementalEvoStreetsNodeLayout()
        {
            Name = "IncrementalEvoStreets";
        }

        protected override Dictionary<ILayoutNode, NodeTransform> Layout(
            IEnumerable<ILayoutNode> gameNodes,
            Vector3 centerPosition,
            Vector2 rectangle)
        {
            List<ILayoutNode> nodes = gameNodes.ToList();

            Debug.Log(oldLayout == null ? "oldLayout is NULL\n" : "oldLayout is set\n");

            /// The nonincremental layout for all existing and new nodes as calculated by the base EvoStreet layouter.
            /// The removed nodes relative to the stored old layout are not part of it.
            Dictionary<ILayoutNode, NodeTransform> nonincrementalLayout = base.Layout(nodes, centerPosition, rectangle);

            IncrementalEvoStreetsNodeLayout previousLayout = oldLayout as IncrementalEvoStreetsNodeLayout;

            // If there is no previously stored old layout, we can just return the nonincremental layout.
            if (oldLayout == null || previousLayout.LastLayout == null)
            {
                LastLayout = ToIdMap(nonincrementalLayout);
                return nonincrementalLayout;
            }

            /// The previously stored old layout. It contains the existing and deleted nodes,
            /// but not the new ones.
            Dictionary<string, NodeTransform> lastLayout = previousLayout.LastLayout;
            /// The resulting layout that we will return eventually.
            Dictionary<ILayoutNode, NodeTransform> result = new(nonincrementalLayout);

            /// We will process all nodes with the same parent per iteration.
            /// These will be aligned on the same street.
            /// FIXME: What if a node has been moved in the hierarchy?
            foreach (IGrouping<ILayoutNode, ILayoutNode> group in nodes.GroupBy(n => n.Parent))
            {
                List<ILayoutNode> siblings = group.ToList();
                if (siblings.Count < 2)
                {
                    continue;
                }

                /// The nodes that have existed in the old layout.
                List<ILayoutNode> existing = siblings
                    .Where(n => lastLayout.ContainsKey(n.ID) && nonincrementalLayout.ContainsKey(n))
                    .ToList();

                /// The new nodes, that is, the ones that have not existed in the old layout
                /// but exist in the new non-incremental layout.
                List<ILayoutNode> added = siblings
                    .Where(n => !lastLayout.ContainsKey(n.ID) && nonincrementalLayout.ContainsKey(n))
                    .ToList();

                // Wenn es nicht genug bestehende Knoten gibt, gibt es
                // keine sinnvolle Struktur, die bewahrt werden kann.
                /// FIXME: Even if there is only one existing node, we want the new nodes
                /// to start at the end of the street.
                if (existing.Count >= 2)
                {
                    /// FIXME: It looks like this is the way to derive whether the street
                    /// is north-south or east-west. How could that be derived from the
                    /// width and depth of the enclosing rectangle?
                    /// Horizontal means east-west.
                    bool isHorizontal = IsHorizontal(nonincrementalLayout, existing);

                    /// A dynamic getter method for the center world-space co-ordinates along the direction of the street.
                    Func<NodeTransform, float> axis = isHorizontal ? t => t.X : t => t.Z;

                    /// A dynamic getter method for the center world-space co-ordinates orthogonal to the direction of the street.
                    Func<NodeTransform, float> cross = isHorizontal ? t => t.Z : t => t.X;

                    /// The existing nodes ordered by their co-ordinates in the last layout.
                    /// They should be drawn first.
                    List<ILayoutNode> oldOrderedPersistent = existing
                        .OrderBy(n => axis(lastLayout[n.ID]))
                        .ToList();

                    /// The layout of the existing nodes ordered by their co-ordinates in the current
                    /// non-incremental layout.
                    /// The order between oldOrderedPersistent and newOrderedPersistentSlots may
                    /// be different.
                    List<NodeTransform> newOrderedPersistentSlots = existing
                        .OrderBy(n => axis(nonincrementalLayout[n]))
                        .Select(n => nonincrementalLayout[n])
                        .ToList();

                    /// For all existing nodes: determine their new position.
                    for (int i = 0; i < oldOrderedPersistent.Count; i++)
                    {
                        /// FIXME: oldOrderedPersistent[i] and newOrderedPersistentSlots[i] may relate
                        /// to different nodes.
                        ILayoutNode node = oldOrderedPersistent[i];
                        NodeTransform oldTransform = lastLayout[node.ID];
                        NodeTransform newTransform = newOrderedPersistentSlots[i];

                        // Interpolation zwischen alter und neuer Zielposition.
                        /// FIXME: Does not consider the scale of the game nodes. It only uses
                        /// the center position. Nodes may overlap.
                        /// FIXME: Why are the midpoints used?
                        /// FIXME: What if new nodes are in between existing nodes? The new layout
                        /// is calculated based on existing and new nodes.
                        result[node] = new NodeTransform(
                            Mathf.Lerp(oldTransform.X, newTransform.X, 0.5f), /// midpoint between the old and new X co-ordindates
                            Mathf.Lerp(oldTransform.Z, newTransform.Z, 0.5f), /// midpoint between the old and new Z co-ordindates
                            newTransform.Scale,
                            newTransform.Rotation
                        );
                    }

                    // Neue Knoten werden an den Enden der aktuellen Geschwisterachse angefügt.
                    if (added.Count > 0)
                    {
                        /// The current (intermediate) positions of the existing nodes.
                        /// They were initially set by the non-incremental layout and then updated
                        /// in the loop above for all existing nodes.
                        List<NodeTransform> currentPersistent = oldOrderedPersistent
                            .Select(n => result[n])
                            .OrderBy(t => axis(t))
                            .ToList();

                        float minAxis = axis(currentPersistent.First());
                        float maxAxis = axis(currentPersistent.Last());

                        float parentAxisCenter;
                        if (group.Key != null && nonincrementalLayout.ContainsKey(group.Key))
                        {
                            parentAxisCenter = axis(nonincrementalLayout[group.Key]);
                        }
                        else
                        {
                            parentAxisCenter = currentPersistent.Average(t => axis(t));
                        }

                        // Knoten links vom Elternzentrum kommen an ein Ende,
                        // Knoten rechts davon an das andere Ende.
                        List<ILayoutNode> leftAdded = added
                            .Where(n => axis(nonincrementalLayout[n]) < parentAxisCenter)
                            .OrderBy(n => axis(nonincrementalLayout[n]))
                            .ToList();

                        List<ILayoutNode> rightAdded = added
                            .Where(n => axis(nonincrementalLayout[n]) >= parentAxisCenter)
                            .OrderBy(n => axis(nonincrementalLayout[n]))
                            .ToList();

                        float spacing = EstimateSpacing(currentPersistent, axis);
                        int leftIndex = 1;
                        foreach (ILayoutNode node in leftAdded)
                        {
                            NodeTransform original = nonincrementalLayout[node];
                            float newAxis = minAxis - spacing * leftIndex;
                            leftIndex++;

                            result[node] = CreateTransform(isHorizontal, newAxis, cross(original), original);
                        }

                        int rightIndex = 1;
                        foreach (ILayoutNode node in rightAdded)
                        {
                            NodeTransform original = nonincrementalLayout[node];
                            float newAxis = maxAxis + spacing * rightIndex;
                            rightIndex++;

                            result[node] = CreateTransform(isHorizontal, newAxis, cross(original), original);
                        }
                    }
                }
            }

            LastLayout = ToIdMap(result);
            return result;
        }

        /// <summary>
        /// Returns true if width of the minimal rectangle enclosing all <paramref name="nodes"/>
        /// is larger than its depth.
        /// </summary>
        /// <param name="layout">The layout where to look up the co-ordindates of the <paramref name="nodes"/>.</param>
        /// <param name="nodes">The nodes enclosed in the rectangle.</param>
        /// <returns>True if width of the minimal rectangle enclosing all <paramref name="nodes"/>
        /// is larger than its depth.</returns>
        private static bool IsHorizontal(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes)
        {
            /// FIXME: X and Z below are just center positions. The enclosing
            /// rectangle is larger than these.
            float minX = nodes.Min(n => layout[n].X);
            float maxX = nodes.Max(n => layout[n].X);
            float minZ = nodes.Min(n => layout[n].Z);
            float maxZ = nodes.Max(n => layout[n].Z);

            return (maxX - minX) >= (maxZ - minZ);
        }

        private static float EstimateSpacing(
            List<NodeTransform> ordered,
            Func<NodeTransform, float> axis)
        {
            if (ordered.Count < 2)
            {
                /// FIXME: Why this magical number 2?
                return 2.0f;
            }

            List<float> distances = new List<float>();
            for (int i = 1; i < ordered.Count; i++)
            {
                float d = Mathf.Abs(axis(ordered[i]) - axis(ordered[i - 1]));
                if (d > 0.001f)
                {
                    distances.Add(d);
                }
            }

            if (distances.Count == 0)
            {
                /// FIXME: Why this magical number 2?
                return 2.0f;
            }

            return distances.Average();
        }

        private static NodeTransform CreateTransform(
            bool horizontal,
            float axisValue,
            float crossValue,
            NodeTransform template)
        {
            float x = horizontal ? axisValue : crossValue;
            float z = horizontal ? crossValue : axisValue;

            return new NodeTransform(
                x,
                z,
                template.Scale,
                template.Rotation
            );
        }
    }
}