using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public abstract class ILayout
    {
        public ILayout(bool showEdges,
               string widthMetric, string heightMetric, string breadthMetric,
               SerializableDictionary<string, IconFactory.Erosion> issueMap,
               BlockFactory blockFactory,
               IScale scaler,
               float edgeWidth)
        {
            this.showEdges = showEdges;
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
            this.issueMap = issueMap;
            this.blockFactory = blockFactory;
            this.scaler = scaler;
            this.edgeWidth = edgeWidth;
        }

        public virtual void Draw(Graph graph)
        {
            Performance p;
            p = Performance.Begin(name + " layout of nodes");
            DrawNodes(graph);
            p.End();
            if (showEdges)
            {
                p = Performance.Begin(name + " layout of edges");
                DrawEdges(graph);
                p.End();
            }
        }

        // A mapping of graph nodes onto the game objects representing them visually in the scene
        protected Dictionary<Node, GameObject> gameObjects = new Dictionary<Node, GameObject>();

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
        /// Mapping of node attributes onto erosion issue icons.
        /// </summary>
        protected readonly SerializableDictionary<string, IconFactory.Erosion> issueMap;

        /// <summary>
        /// Whether edges should be shown.
        /// </summary>
        protected readonly bool showEdges;

        /// <summary>
        /// A factory to create visual representations of graph nodes (e.g., cubes or CScape buildings).
        /// </summary>
        protected readonly BlockFactory blockFactory;

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
        protected virtual void DrawNodes(Graph graph) { }

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
        /// Used to determine the lengths of the node blocks.
        /// </summary>
        protected readonly IScale scaler;

        /// <summary>
        /// The width of every edge.
        /// </summary>
        protected readonly float edgeWidth;

        /// <summary>
        /// The unique name of a layout.
        /// </summary>
        public string Name
        {
            get => name;
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
        /// Returns the first immediate child of parent with given tag or null
        /// if none exists.
        /// </summary>
        /// <param name="parent">parent whose children are to be searched</param>
        /// <param name="tag">search tag</param>
        /// <returns>first immediate child of parent with given tag or null</returns>
        protected static GameObject GetChild(GameObject parent, string tag)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                Transform child = parent.transform.GetChild(i);
                if (child.tag == tag)
                {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}

