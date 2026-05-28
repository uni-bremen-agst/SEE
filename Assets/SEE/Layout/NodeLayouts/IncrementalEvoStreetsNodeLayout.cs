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

            UnityEngine.Debug.Log(oldLayout ==null ? "oldLayout is NULL" : "oldLayout is Set");
            UnityEngine.Debug.Log(oldLayout?.LastLayout == null ? "LastLayout is NULL" : "LastLayout is SET");

            // Zuerst das normale EvoStreets-Layout berechnen.
            Dictionary<ILayoutNode, NodeTransform> newLayout =
                base.Layout(nodes, centerPosition, rectangle);

            // Erste Version: Kein vorheriges Layout vorhanden.
            if (oldLayout == null || oldLayout.LastLayout == null)
            {
                LastLayout = ToIdMap(newLayout);
                return newLayout;
            }

            Dictionary<string, NodeTransform> oldById = oldLayout.LastLayout;
            Dictionary<ILayoutNode, NodeTransform> result =
                new Dictionary<ILayoutNode, NodeTransform>(newLayout);

           // Arbeite pro Geschwistergruppe (gleicher Parent).
            // Das entspricht eher EvoStreets, da Geschwistergruppen die lokalen Straßenstrukturen bilden.
            foreach (IGrouping<ILayoutNode, ILayoutNode> group in nodes.GroupBy(n => n.Parent))
            {
                List<ILayoutNode> siblings = group.ToList();
                if (siblings.Count < 2)
                {
                    continue;
                }

                List<ILayoutNode> persistent = siblings
                    .Where(n => oldById.ContainsKey(n.ID) && newLayout.ContainsKey(n))
                    .ToList();

                List<ILayoutNode> added = siblings
                    .Where(n => !oldById.ContainsKey(n.ID) && newLayout.ContainsKey(n))
                    .ToList();

                 // Wenn es nicht genug bestehende Knoten gibt, gibt es
                // keine sinnvolle Struktur, die bewahrt werden kann.
                if (persistent.Count >= 2)
                {
                    bool horizontal = IsHorizontal(newLayout, persistent);

                    Func<NodeTransform, float> axis =
                        horizontal ? t => t.X : t => t.Z;

                    Func<NodeTransform, float> cross =
                        horizontal ? t => t.Z : t => t.X;

                    //  Bestehende Knoten behalten ihre alte Reihenfolge.
                    List<ILayoutNode> oldOrderedPersistent = persistent
                        .OrderBy(n => axis(oldById[n.ID]))
                        .ToList();

                    // Neue EvoStreets-Positionen definieren die neuen Slots/Größen.
                    List<NodeTransform> newOrderedPersistentSlots = persistent
                        .OrderBy(n => axis(newLayout[n]))
                        .Select(n => newLayout[n])
                        .ToList();

                    for (int i = 0; i < oldOrderedPersistent.Count; i++)
                    {
                        ILayoutNode node = oldOrderedPersistent[i];
                        NodeTransform oldTransform = oldById[node.ID];
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
                        if (group.Key != null && newLayout.ContainsKey(group.Key))
                        {
                            parentAxisCenter = axis(newLayout[group.Key]);
                        }
                        else
                        {
                            parentAxisCenter = currentPersistent.Average(t => axis(t));
                        }

                        // Knoten links vom Elternzentrum kommen an ein Ende,
                        // Knoten rechts davon an das andere Ende.
                        List<ILayoutNode> leftAdded = added
                            .Where(n => axis(newLayout[n]) < parentAxisCenter)
                            .OrderBy(n => axis(newLayout[n]))
                            .ToList();

                        List<ILayoutNode> rightAdded = added
                            .Where(n => axis(newLayout[n]) >= parentAxisCenter)
                            .OrderBy(n => axis(newLayout[n]))
                            .ToList();

                        int leftIndex = 1;
                        foreach (ILayoutNode node in leftAdded)
                        {
                            NodeTransform original = newLayout[node];
                            float newAxis = minAxis - spacing * leftIndex;
                            leftIndex++;

                            result[node] = CreateTransform(horizontal, newAxis, cross(original), original);
                        }

                        int rightIndex = 1;
                        foreach (ILayoutNode node in rightAdded)
                        {
                            NodeTransform original = newLayout[node];
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