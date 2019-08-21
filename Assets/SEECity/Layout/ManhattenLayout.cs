using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;
using System;

namespace SEE
{
    public class ManhattenLayout : ILayout
    {
        private readonly string widthMetric;
        private readonly string heightMetric;
        private readonly string breadthMetric;

        public ManhattenLayout(string widthMetric, string heightMetric, string breadthMetric)
        {
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
        }

        public void Draw(Graph graph)
        {
            // The maximal values of the relevant metrics.
            Performance p;

            p = Performance.Begin("Determine metric maxima");
            Dictionary<string, float> metricMaxima = DetermineMetricMaxima(graph, widthMetric, heightMetric, breadthMetric);
            p.End();
            p = Performance.Begin("Layout nodes");
            CreateNodes(graph, metricMaxima);
            p.End();
            p = Performance.Begin("Layout edges");
            //CreateEdges(graph);
            p.End();
        }

        /// <summary>
        /// Returns the maximal values of the given node metrics.
        /// </summary>
        /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
        /// <returns>metric maxima</returns>
        private Dictionary<string, float> DetermineMetricMaxima(Graph graph, params string[] metrics)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            foreach (string metric in metrics)
            {
                result.Add(metric, 0.0f);
            }

            foreach (Node node in graph.Nodes())
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
        private void CreateNodes(Graph graph, Dictionary<string, float> metricMaxima)
        {
            //DumpMetricMaxima(metricMaxima);

            int length = (int)Mathf.Sqrt(graph.NodeCount);
            float column = 0f;
            float row = 1f;
            const float relativeOffset = 0.1f;

            //Vector3 maxSize = Vector3.zero;
            foreach (GameObject sceneNode in graph.GetNodes())
            {
                column++;
                if (column > length)
                {
                    // exceeded length of the square => start a new row
                    column = 1f;
                    row++;
                }
                Node node = sceneNode.GetComponent<Node>();

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
                sceneNode.transform.position = new Vector3(row + row * relativeOffset, height / 2.0f, column + column * relativeOffset);
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
                //Vector3 size = GetSize(sceneNode);
                //if (size.x > maxSize.x) maxSize.x = size.x;
                //if (size.y > maxSize.y) maxSize.y = size.y;
                //if (size.z > maxSize.z) maxSize.z = size.z;
            }
            //Debug.Log("Maxima: " + maxSize + "\n");
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
        private float NormalizedMetric(Dictionary<string, float> metricMaxima, Node node, string metric)
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
        private void CreateEdges(Graph graph)
        {
            // The distance of the edges relative to the houses; the maximal height of
            // a house is 1.0. This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            const float maxHeight = 1f;
            const float offset = maxHeight * 0.1f; // must be positive

            const float lineWidth = 0.01f;

            string materialPath = "BrickTextures/BricksTexture13/BricksTexture13";
            //Material newMat = Resources.Load<Material>(materialPath);
            Material newMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            //Material newMat = new Material(Shader.Find("Particles/Standard Surface"));
            if (newMat == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
                return;
            }

            foreach (GameObject gameEdge in graph.GetEdges())
            {
                Edge edge = gameEdge.GetComponent<Edge>();

                if (edge != null)
                {
                    Node source = edge.Source;
                    Node target = edge.Target;
                    if (source != null && target != null)
                    {
                        GameObject sourceObject = source.gameObject;
                        GameObject targetObject = target.gameObject;

                        LineRenderer renderer = gameEdge.GetComponent<LineRenderer>();
                        if (renderer == null)
                        {
                            // gameEdge does not yet have a renderer; we add a new one
                            renderer = gameEdge.AddComponent<LineRenderer>();
                        }
                        if (renderer != null)
                        {
                            //renderer.sharedMaterial = newMat;
                            renderer.material = newMat;

                            //renderer.widthCurve = 0.1f;
                            renderer.sortingLayerName = "OnTop";
                            renderer.sortingOrder = 5;
                            renderer.positionCount = 4; // number of vertices

                            // simplify rendering
                            renderer.receiveShadows = false;
                            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                            Vector3 sourceCenterToBorder = GetCenterToBorder(sourceObject);
                            Vector3 targetCenterToBorder = GetCenterToBorder(targetObject);

                            renderer.startWidth = lineWidth;
                            renderer.endWidth = lineWidth;

                            var points = new Vector3[renderer.positionCount];
                            // starting position
                            points[0] = sourceObject.transform.position; // center of source node
                            points[0].y += orientation * sourceCenterToBorder.y; // floor/ceiling

                            // position below/above starting position
                            points[1] = points[0];
                            points[1].y += orientation * offset;

                            // ending position
                            points[3] = targetObject.transform.position; // center of target node
                            points[3].y += orientation * targetCenterToBorder.y; // floor/ceiling

                            // position below/above ending position
                            points[2] = points[3];
                            points[2].y += orientation * offset;

                            renderer.SetPositions(points);

                            // put a capsule collider around the straight main line
                            // (the one from points[1] to points[2]

                            CapsuleCollider capsule = gameEdge.AddComponent<CapsuleCollider>();
                            capsule.radius = lineWidth / 2.0f;
                            capsule.center = Vector3.zero;
                            capsule.direction = 2; // Z-axis for easier "LookAt" orientation
                            capsule.transform.position = points[1] + (points[2] - points[1]) / 2;
                            capsule.transform.LookAt(points[1]);
                            capsule.height = (points[2] - points[1]).magnitude;

                            renderer.startColor = Color.green;
                            renderer.endColor = Color.red;

                            renderer.useWorldSpace = true;
                        }
                        else
                        {
                            Debug.LogError("Cannot attach renderer on scene edge " + gameEdge.name + ".\n");
                        }
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
            }
        }

        private Vector3 GetSize(GameObject o)
        {
            Renderer renderer = o.GetComponent<Renderer>();
            return renderer.bounds.size;
        }

        private Vector3 GetCenterToBorder(GameObject o)
        {
            Renderer renderer = o.GetComponent<Renderer>();
            return renderer.bounds.extents;
        }
    }
}