using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;

namespace SEE
{
    public class GridLayout : ILayout
    {
        private readonly string widthMetric;
        private readonly string heightMetric;
        private readonly string breadthMetric;

        public GridLayout(string widthMetric, string heightMetric, string breadthMetric)
        {
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
        }

        public void Draw(ISceneGraph graph)
        {
            // The maximal values of the relevant metrics.
            Dictionary<string, float> metricMaxima = DetermineMetricMaxima(graph, widthMetric, heightMetric, breadthMetric);
            CreateNodes(graph, metricMaxima);
            CreateEdges(graph);
        }

        /// <summary>
        /// Returns the maximal values of the given node metrics.
        /// </summary>
        /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
        /// <returns>metric maxima</returns>
        private Dictionary<string, float> DetermineMetricMaxima(IGraph graph, params string[] metrics)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            foreach (string metric in metrics)
            {
                result.Add(metric, 0.0f);
            }

            foreach (INode node in graph.Nodes())
            {
                foreach (string metric in metrics)
                {
                    if (node.TryGetNumeric(metric, out float value))
                    {
                        if (value > result[metric])
                        {
                            result[metric] = value;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Creates the GameObjects representing the nodes of the graph.
        /// The graph must have been loaded before via Load().
        /// </summary>
        private void CreateNodes(ISceneGraph graph, Dictionary<string, float> metricMaxima)
        {
            int length = (int)Mathf.Sqrt(graph.NodeCount);
            int column = 0;
            int row = 1;

            foreach (GameObject sceneNode in graph.GetNodes())
            {
                column++;
                if (column > length)
                {
                    // exceeded length of the square => start a new row
                    column = 1;
                    row++;
                }
                INode node = sceneNode.GetComponent<INode>();

                float width;
                float breadth;
                float height;

                if (node != null)
                {
                    width = NormalizedMetric(metricMaxima, node, widthMetric);
                    breadth = NormalizedMetric(metricMaxima, node, breadthMetric);
                    height = NormalizedMetric(metricMaxima, node, heightMetric);
                }
                else
                {
                    Debug.LogError("Scene node " + sceneNode.name + " does not have a graph node component.\n");
                    width = minimalLength;
                    breadth = minimalLength;
                    height = minimalLength;
                }
                sceneNode.transform.localScale = new Vector3(width, height, breadth);

                // The position is the center of a GameObject. We want all GameObjects
                // be placed at the same ground level 0. That is why we need to "lift"
                // every building by half of its height.
                sceneNode.transform.position = new Vector3(row + row * 0.3f, height / 2.0f, column + column * 0.3f);
                /*
                {
                    Renderer renderer;
                    //Fetch the GameObject's Renderer component
                    renderer = house.GetComponent<Renderer>();
                    //Change the GameObject's Material Color to red
                    //m_ObjectRenderer.material.color = Color.red;
                    Debug.Log("house size: " + renderer.bounds.size + "\n");
                }
                */
            }
        }

        // orientation of the edges; 
        // if -1, the edges are drawn below the houses;
        // if 1, the edges are drawn above the houses;
        // use either -1 or 1
        private const float orientation = -1f;

        /// <summary>
        /// Dumps metricMaxima for debugging.
        /// </summary>
        private void DumpMetricMaxima(Dictionary<string, float> metricMaxima)
        {
            foreach (var item in metricMaxima)
            {
                Debug.Log("maximum of " + item.Key + ": " + item.Value + "\n");
            }
        }

        // The minimal length of any axis (width, breadth, height) of a block.
        // Must not exceed 1.0f.
        private const float minimalLength = 0.1f;

        /// <summary>
        /// Returns a value in the range [0.0, 1.0] representing the relative value of the given
        /// metric in the metrics value range for the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="metric"></param>
        /// <returns></returns>
        private float NormalizedMetric(Dictionary<string, float> metricMaxima, INode node, string metric)
        {
            float max = metricMaxima[metric];

            if (max <= 0.0f)
            {
                return minimalLength;
            }
            if (node.TryGetNumeric(metric, out float width))
            {
                if (width <= minimalLength)
                {
                    return minimalLength;
                }
                else
                {
                    return (float)width / max;
                }
            }
            else
            {
                return minimalLength;
            }
        }

        /// <summary>
        /// Creates the GameObjects representing the edges of the graph.
        /// The graph must have been loaded before via Load().
        /// </summary>
        private void CreateEdges(ISceneGraph graph)
        {
            // the distance of the edges relative to the houses; the maximal height of
            // a house is 1.0
            const float above = orientation * (1f / 2.0f);

            foreach (GameObject gameEdge in graph.GetEdges())
            {
                IEdge edge = gameEdge.GetComponent<IEdge>();
                if (edge != null)
                {
                    INode source = edge.Source;
                    INode target = edge.Target;
                    if (source != null && target != null)
                    {
                        //GameObject s = source.
                    }
                    else
                    {
                        Debug.LogError("Scene edge " + gameEdge.name + " has a missing source or target.\n");
                    }
                }
                else
                {
                    Debug.LogError("Scene edge " + gameEdge.name + " does not have a graph edge component.\n");
                }
                //GameObject sceneEdge = DrawLine(nodes[gameEdge.Source.LinkName], nodes[gameEdge.Target.LinkName], linePrefab, above);
            }
        }

        /// <summary>
        /// Draws a straight line from the two given GameObjects. 
        /// </summary>
        /// <param name="from">the source object of the line</param>
        /// <param name="to">the target object of the line</param>
        /// <param name="linePrefab">the preftab from which to instantiate the line</param>
        /// <param name="offset">the y offset at which to draw the begin and end of the line</param>
        /// <returns></returns>
        private GameObject DrawLine(GameObject from, GameObject to, GameObject linePrefab, float offset)
        {
            GameObject edge = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(linePrefab);
            LineRenderer renderer = edge.GetComponent<LineRenderer>();

            renderer.sortingLayerName = "OnTop";
            renderer.sortingOrder = 5;
            renderer.positionCount = 4; // number of vertices

            var points = new Vector3[renderer.positionCount];
            // starting position
            points[0] = from.transform.position;
            // position above starting position
            points[1] = from.transform.position;
            points[1].y += offset;
            // position above ending position
            points[2] = to.transform.position;
            points[2].y += offset;
            // ending position
            points[3] = to.transform.position;
            renderer.SetPositions(points);

            //renderer.SetWidth(0.5f, 0.5f);
            renderer.useWorldSpace = true;
            return edge;
        }

    }
}