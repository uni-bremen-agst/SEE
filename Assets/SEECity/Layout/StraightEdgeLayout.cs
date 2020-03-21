using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.GO;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Draws edges as straight lines at either above or below the game nodes.
    /// </summary>
    public class StraightEdgeLayout : IEdgeLayout
    {
        public StraightEdgeLayout(NodeFactory blockFactory, float edgeWidth, bool edgesAboveBlocks) 
            : base(blockFactory, edgeWidth, edgesAboveBlocks)
        {
        }

        public override ICollection<GameObject> DrawEdges(Graph graph, ICollection<GameObject> nodes)
        {
            List<GameObject> result = new List<GameObject>();
            Material newMat = new Material(defaultLineMaterial);
            if (newMat == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return result;
            }

            SetGameNodes(nodes);
            MinMaxBlockY(nodes, out float minBlockY, out float maxBlockY, out float maxHeight);

            // The offset of the edges above or below the ground chosen relative 
            // to the height of the largest block.
            // We are using a value relative to the highest node so that edges 
            // are farther away from the blocks for cities with large blocks and
            // closer to blocks for cities with small blocks. This may help to 
            // better read the edges.
            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = 0.2f * maxHeight; // must be positive
            // The level at which edges are drawn.
            float edgeLevel = edgesAboveBlocks ? maxBlockY + offset : minBlockY - offset;

            foreach (Edge edge in graph.ConnectingEdges(gameNodes.Keys))
            {
                Node source = edge.Source;
                Node target = edge.Target;
                if (source != null && target != null)
                {
                    GameObject gameEdge = NewGameEdge(edge);
                    result.Add(gameEdge);

                    GameObject sourceObject = gameNodes[source];
                    GameObject targetObject = gameNodes[target];

                    // define the points along the line
                    Vector3 start;
                    Vector3 end;
                    if (edgesAboveBlocks)
                    {
                        start = blockFactory.Roof(sourceObject);
                        end = blockFactory.Roof(targetObject);
                    }
                    else
                    {
                        start = blockFactory.Ground(sourceObject);
                        end = blockFactory.Ground(targetObject);
                    }

                    Vector3[] points = LinePoints.StraightLinePoints(start, end, edgeLevel);

                    // gameEdge does not yet have a renderer; we add a new one
                    LineRenderer line = gameEdge.AddComponent<LineRenderer>();
                    // use sharedMaterial if changes to the original material should affect all
                    // objects using this material; renderer.material instead will create a copy
                    // of the material and will not be affected by changes of the original material
                    line.sharedMaterial = defaultLineMaterial;

                    LineFactory.SetDefaults(line);
                    LineFactory.SetWidth(line, edgeWidth * blockFactory.Unit);

                    // If enabled, the lines are defined in world space.
                    // This means the object's position is ignored, and the lines are rendered around 
                    // world origin.
                    line.useWorldSpace = true;

                    line.positionCount = points.Length; // number of vertices
                    line.SetPositions(points);

                    // put a capsule collider around the straight main line
                    // (the one from points[1] to points[2]
                    CapsuleCollider capsule = gameEdge.AddComponent<CapsuleCollider>();
                    capsule.radius = Math.Max(line.startWidth, line.endWidth) / 2.0f;
                    capsule.center = Vector3.zero;
                    capsule.direction = 2; // Z-axis for easier "LookAt" orientation
                    capsule.transform.position = points[1] + (points[2] - points[1]) / 2;
                    capsule.transform.LookAt(points[1]);
                    capsule.height = (points[2] - points[1]).magnitude;
                }
                else
                {
                    Debug.LogErrorFormat("Edge of type {0} has a missing source or target.\n", edge.Type);
                }
            }
            return result;
        }
    }
}
