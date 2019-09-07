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
            p = Performance.Begin(name + " layout of nodes");
            DrawNodes(graph);
            p.End();
            p = Performance.Begin(name + " layout of edges");
            DrawEdges(graph);
            p.End();
        }

        // This parameter determines the minimal width, breadth, and height of each cube. 
        // Subclasses may define a maximal length, too, in which case minimal_length
        // must be smaller than that maximal length.
        public static readonly float minimal_length = 0.1f;

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

