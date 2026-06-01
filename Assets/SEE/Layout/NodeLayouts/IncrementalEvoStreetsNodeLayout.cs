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
    public class IncrementalEvoStreetsNodeLayout : EvoStreetsNodeLayout, IIncrementalNodeLayout
    {
        static IncrementalEvoStreetsNodeLayout()
        {
            Name = "IncrementalEvoStreets";
        }

        private IncrementalEvoStreetsNodeLayout oldLayout;

        /// <summary>
        /// Speichert das letzte Layout anhand stabiler Knoten-IDs.
        /// </summary>
        public Dictionary<string, NodeTransform> LastLayout { get; private set; }

        public IIncrementalNodeLayout OldLayout
        {
            set
            {
                if (value == null)
                {
                    oldLayout = null;
                }
                else if (value is IncrementalEvoStreetsNodeLayout layout)
                {
                    oldLayout = layout;
                }
                else
                {
                    throw new ArgumentException("Old layout must be IncrementalEvoStreetsNodeLayout");
                }
            }
        }

        protected override Dictionary<ILayoutNode, NodeTransform> Layout(
            IEnumerable<ILayoutNode> gameNodes,
            Vector3 centerPosition,
            Vector2 rectangle)
        {
            List<ILayoutNode> nodes = gameNodes.ToList();

            Debug.Log(oldLayout == null ? "oldLayout is NULL\n" : "oldLayout is set\n");
            Debug.Log(oldLayout?.LastLayout == null ? "LastLayout is NULL\n" : "LastLayout is set\n");

            /// The nonincremental layout for all existing and new nodes as calculated by the base EvoStreet layouter.
            /// The removed nodes relative to the stored old layout are not part of it.
            Dictionary<ILayoutNode, NodeTransform> nonincrementalLayout = base.Layout(nodes, centerPosition, rectangle);

            // If there is no previously stored old layout, we can just return the nonincremental layout.
            if (oldLayout == null || oldLayout.LastLayout == null)
            {
                LastLayout = ToIdMap(nonincrementalLayout);
                return nonincrementalLayout;
            }

            /// The previously stored old layout. It contains the existing and deleted nodes,
            /// but not the new ones.
            Dictionary<string, NodeTransform> lastLayout = oldLayout.LastLayout;
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
                if (existing.Count >= 2)
                {
                    bool horizontal = IsHorizontal(nonincrementalLayout, existing);

                    Func<NodeTransform, float> axis =
                        horizontal ? t => t.X : t => t.Z;

                    Func<NodeTransform, float> cross =
                        horizontal ? t => t.Z : t => t.X;

                    //  Bestehende Knoten behalten ihre alte Reihenfolge.
                    List<ILayoutNode> oldOrderedPersistent = existing
                        .OrderBy(n => axis(lastLayout[n.ID]))
                        .ToList();

                    // Neue EvoStreets-Positionen definieren die neuen Slots/Größen.
                    List<NodeTransform> newOrderedPersistentSlots = existing
                        .OrderBy(n => axis(nonincrementalLayout[n]))
                        .Select(n => nonincrementalLayout[n])
                        .ToList();

                    for (int i = 0; i < oldOrderedPersistent.Count; i++)
                    {
                        ILayoutNode node = oldOrderedPersistent[i];
                        NodeTransform oldTransform = lastLayout[node.ID];
                        NodeTransform targetTransform = newOrderedPersistentSlots[i];

                        // Interpolation zwischen alter und neuer Zielposition.
                        float x = Mathf.Lerp(oldTransform.X, targetTransform.X, 0.5f);
                        float z = Mathf.Lerp(oldTransform.Z, targetTransform.Z, 0.5f);

                        result[node] = new NodeTransform(
                            x,
                            z,
                            targetTransform.Scale,
                            targetTransform.Rotation
                        );
                    }

                    // Neue Knoten werden an den Enden der aktuellen Geschwisterachse angefügt.
                    if (added.Count > 0)
                    {
                        List<NodeTransform> currentPersistent = oldOrderedPersistent
                            .Select(n => result[n])
                            .OrderBy(t => axis(t))
                            .ToList();

                        float spacing = EstimateSpacing(currentPersistent, axis);
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

                        int leftIndex = 1;
                        foreach (ILayoutNode node in leftAdded)
                        {
                            NodeTransform original = nonincrementalLayout[node];
                            float newAxis = minAxis - spacing * leftIndex;
                            leftIndex++;

                            result[node] = CreateTransform(horizontal, newAxis, cross(original), original);
                        }

                        int rightIndex = 1;
                        foreach (ILayoutNode node in rightAdded)
                        {
                            NodeTransform original = nonincrementalLayout[node];
                            float newAxis = maxAxis + spacing * rightIndex;
                            rightIndex++;

                            result[node] = CreateTransform(horizontal, newAxis, cross(original), original);
                        }
                    }
                }
            }

            LastLayout = ToIdMap(result);
            return result;
        }

        private static Dictionary<string, NodeTransform> ToIdMap(Dictionary<ILayoutNode, NodeTransform> layout)
        {
            return layout.ToDictionary(kvp => kvp.Key.ID, kvp => kvp.Value);
        }

        private static bool IsHorizontal(
            Dictionary<ILayoutNode, NodeTransform> layout,
            List<ILayoutNode> nodes)
        {
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