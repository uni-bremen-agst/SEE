using SEE.DataModel;

using UnityEngine;
using System.Collections.Generic;

namespace SEE.Layout
{
    public abstract class ILayout
    {
        public virtual void Draw(Graph graph)
        {
            Performance p;
            p = Performance.Begin("Determine metric maxima");
            Dictionary<string, float> metricMaxima = DetermineMetricMaxima(graph, widthMetric, heightMetric, breadthMetric);
            p.End();
            p = Performance.Begin(name + " layout of nodes");
            DrawNodes(graph, metricMaxima);
            p.End();
            p = Performance.Begin(name + " layout of edges");
            DrawEdges(graph);
            p.End();
        }

        /// <summary>
        /// Path to the material used for edges.
        /// </summary>
        protected const string materialPath = "Legacy Shaders/Particles/Additive";
        // protected const string materialPath = "BrickTextures/BricksTexture13/BricksTexture13";
        // protected const string materialPath = "Particles/Standard Surface";

        /// <summary>
        /// The material used for edges.
        /// </summary>
        protected readonly static Material defaultLineMaterial = LineMaterial();

        /// <summary>
        /// Returns the default material for edges using the materialPath.
        /// </summary>
        /// <returns>default material for edges</returns>
        private static Material LineMaterial()
        {
            Material material = new Material(Shader.Find(materialPath));
            if (material == null)
            {
                Debug.LogError("Could not find material " + materialPath + "\n");
            }
            return material;
        }

        /// <summary>
        /// Creates the GameObjects representing the nodes of the graph.
        /// The graph must have been loaded before via Load().
        /// Intended to be overriden by subclasses.
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        /// <param name="metricMaxima">maximal metrics of nodes to be visualized; used for their normalization</param>
        protected virtual void DrawNodes(Graph graph, Dictionary<string, float> metricMaxima) { }

        /// <summary>
        /// Creates the GameObjects representing the edges of the graph.
        /// The graph must have been loaded before via Load().
        /// Intended to be overriden by subclasses.
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        protected virtual void DrawEdges(Graph graph) { }

        // name of the layout
        protected string name = "";

        /// <summary>
        /// The unique name of a layout.
        /// </summary>
        public string Name
        {
            get => name;
        }

        public ILayout(string widthMetric, string heightMetric, string breadthMetric)
        {
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
        }

        /// <summary>
        /// The metric used to determine the width of a node.
        /// </summary>
        protected readonly string widthMetric;
        /// <summary>
        /// The metric used to determine the height of a node.
        /// </summary>
        protected readonly string heightMetric;
        /// <summary>
        /// The metric used to determine the breadth of a node.
        /// </summary>
        protected readonly string breadthMetric;

        /// <summary>
        /// Returns the maximal values of the given node metrics.
        /// </summary>
        /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
        /// <returns>metric maxima</returns>
        protected Dictionary<string, float> DetermineMetricMaxima(Graph graph, params string[] metrics)
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
        /// Returns a value in the range [0.0, 1.0] representing the relative value of the given
        /// metric in the metrics value range for the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="metric"></param>
        /// <returns></returns>
        protected float NormalizedMetric(Dictionary<string, float> metricMaxima, Node node, string metric)
        {
            float max = metricMaxima[metric];

            if (max <= 0.0f)
            {
                return 0.0f;
            }
            else if (node.TryGetNumeric(metric, out float width))
            {
                if (width <= 0.0f)
                {
                    return 0.0f;
                }
                else
                {
                    return (float)width / max;
                }
            }
            else
            {
                return 0.0f;
            }
        }

        /// <summary>
        /// Dumps metricMaxima for debugging.
        /// </summary>
        protected void DumpMetricMaxima(Dictionary<string, float> metricMaxima)
        {
            foreach (var item in metricMaxima)
            {
                Debug.Log("maximum of " + item.Key + ": " + item.Value + "\n");
            }
        }

        /// <summary>
        /// Yields a vector where each element (x, y, z) is a linear interpolation of the normalized
        /// value of the metrics that determine the width, height, and breadth of the given node.
        /// The range of the linear interpolation is set by [minimalLength, maximalLength].
        /// </summary>
        /// <param name="node"></param>
        /// <param name="metricMaxima"></param>
        /// <param name="minimalLength"></param>
        /// <param name="maximalLength"></param>
        /// <returns></returns>
        protected Vector3 ScaleNode(Node node, Dictionary<string, float> metricMaxima, float minimalLength, float maximalLength)
        {
            float x;
            float y;
            float z;

            if (node != null)
            {
                x = Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, widthMetric));
                y = Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, heightMetric));
                z = Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, breadthMetric));
            }
            else
            {
                x = minimalLength;
                y = minimalLength;
                z = minimalLength;
            }
            return new Vector3(x, z, y);
        }

        /// <summary>
        /// Total size of the bounding box of given game object.
        /// This is always twice as large as the extent (see GetExtent()).
        /// </summary>
        /// <param name="gameObject">game object whose size is to be determined</param>
        /// <returns>size of the game object</returns>
        protected static Vector3 GetSize(GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            return renderer.bounds.size;
        }

        /// <summary>
        /// The extents of the bounding box of given game object.
        /// This is always half of the size of the bounds (see GetSize()).
        /// </summary>
        /// <param name="gameObject">game object whose extent is to be determined</param>
        /// <returns>extent of the game object</returns>
        protected static Vector3 GetExtent(GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            return renderer.bounds.extents;
        }

        /// <summary>
        /// Removes everything the layout has added to the scence, such as planes etc.
        /// </summary>
        public abstract void Reset();
    }
}

