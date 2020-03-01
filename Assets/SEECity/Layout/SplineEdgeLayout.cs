using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Draws edges as splines with three control points between either the roof or ground of
    /// game objects.
    /// </summary>
    public class SplineEdgeLayout : IEdgeLayout
    {
        public SplineEdgeLayout(NodeFactory blockFactory, float edgeWidth, bool edgesAboveBlocks) : base(blockFactory, edgeWidth, edgesAboveBlocks)
        {
        }

        public override ICollection<GameObject> DrawEdges(Graph graph, ICollection<GameObject> nodes)
        {
            List<GameObject> result = new List<GameObject>();

            SetGameNodes(nodes);
            float maxBlockHeight = GetMaxBlockHeight(nodes);

            Material edgeMaterial = new Material(defaultLineMaterial);
            if (edgeMaterial == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return result;
            }

            Vector3 levelUnit = Vector3.zero;
            levelUnit.y = maxBlockHeight;

            foreach (Edge edge in graph.ConnectingEdges(gameNodes.Keys))
            {
                Node sourceObject = edge.Source;
                Node targetObject = edge.Target;

                GameObject gameEdge = NewGameEdge(edge);

                Vector3 sourcePosition = edgesAboveBlocks ? blockFactory.Roof(gameNodes[sourceObject])
                                                          : blockFactory.Ground(gameNodes[sourceObject]);
                Vector3 targetPosition = edgesAboveBlocks ? blockFactory.Roof(gameNodes[targetObject])
                                                          : blockFactory.Ground(gameNodes[targetObject]);

                float factor = edgesAboveBlocks ? 1.0f : -1.0f;

                Vector3[] controlPoints = new Vector3[] {
                    sourcePosition,
                    factor * (Vector3.Lerp(sourcePosition, targetPosition, 0.3f) + levelUnit),
                    factor * (Vector3.Lerp(sourcePosition, targetPosition, 0.7f) + levelUnit),
                    targetPosition
                };
                BSplineFactory.Draw(gameEdge, controlPoints, edgeWidth * blockFactory.Unit, edgeMaterial);
                result.Add(gameEdge);
            }
            return result;
        }
    }
}
