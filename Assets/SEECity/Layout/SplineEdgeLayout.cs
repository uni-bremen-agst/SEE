using System.Collections.Generic;
using SEE.DataModel;
using SEE.GO;
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

            Material edgeMaterial = new Material(defaultLineMaterial);
            if (edgeMaterial == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return result;
            }

            SetGameNodes(nodes);

            foreach (Edge edge in graph.ConnectingEdges(gameNodes.Keys))
            {
                Node sourceObject = edge.Source;
                Node targetObject = edge.Target;

                GameObject gameEdge = NewGameEdge(edge);

                Vector3 start = edgesAboveBlocks ? blockFactory.Roof(gameNodes[sourceObject])
                                                 : blockFactory.Ground(gameNodes[sourceObject]);
                Vector3 end = edgesAboveBlocks ? blockFactory.Roof(gameNodes[targetObject])
                                               : blockFactory.Ground(gameNodes[targetObject]);

                // The offset of the edges above or below the ground chosen relative 
                // to the distance between the two blocks.
                // We are using a value relative to the distance so that edges 
                // connecting close blocks do not shoot into the sky. Otherwise they
                // would be difficult to read. Likewise, edges connecting blocks farther
                // away should go higher so that we avoid edge and node crossings.
                // This heuristic may help to better read the edges.

                // This offset is used to draw the line somewhat below
                // or above the house (depending on the orientation).
                float offset = 1.5f * Vector3.Distance(start, end); // must be positive
                // The level at which edges are drawn.
                float edgeLevel = edgesAboveBlocks ? Mathf.Max(start.y, end.y) + offset
                                             : Mathf.Min(start.y, end.y) - offset;

                Vector3[] controlPoints = new Vector3[4];
                controlPoints[0] = start;
                controlPoints[1] = Vector3.Lerp(start, end, 0.333333f);
                controlPoints[1].y = edgeLevel;
                controlPoints[2]   = Vector3.Lerp(start, end, 0.666666f);
                controlPoints[3].y = edgeLevel;
                controlPoints[3]= end;

                BSplineFactory.Draw(gameEdge, controlPoints, edgeWidth * blockFactory.Unit, edgeMaterial);
                result.Add(gameEdge);
            }
            return result;
        }
    }
}
