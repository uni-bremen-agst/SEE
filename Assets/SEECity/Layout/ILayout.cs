using SEE.DataModel;

using UnityEngine;
using System.Collections.Generic;
using CScape;

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
        /// Mapping of node attributes onto erosion issue icons.
        /// </summary>
        protected readonly SerializableDictionary<string, IconFactory.Erosion> issueMap;

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

        public ILayout(string widthMetric, string heightMetric, string breadthMetric, SerializableDictionary<string, IconFactory.Erosion> issueMap)
        {
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
            this.issueMap = issueMap;
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
            return GetExtent(gameObject) * 2.0f;
        }

        /// <summary>
        /// Scales the given game object by given scale.
        /// </summary>
        /// <param name="gameObject">object to be scaled</param>
        /// <param name="scale">scale to be used for that</param>
        protected static void ScaleBlock(GameObject gameObject, Vector3 scale)
        {
            BlockModifier bm = gameObject.GetComponent<BlockModifier>();
            if (bm == null)
            {
                Debug.LogErrorFormat("Game object {0} without block modifier.\n", gameObject.name);
            }
            else
            {
                bm.Scale(scale);
            }
        }

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

        /// <summary>
        /// The extents of the bounding box of given game object.
        /// This is always half of the size of the bounds (see GetSize()).
        /// </summary>
        /// <param name="gameObject">game object whose extent is to be determined</param>
        /// <returns>extent of the game object</returns>
        protected static Vector3 GetExtent(GameObject gameObject)
        {
            BlockModifier bm = gameObject.GetComponent<BlockModifier>();
            if (bm == null)
            {
                Debug.LogErrorFormat("Game object {0} without block modifier.\n", gameObject.name);
                return Vector3.one;
            }
            else
            {
                return bm.GetExtent();
            }
        }

        /// <summary>
        /// Returns the roof position of a node.
        /// </summary>
        /// <param name="node">node for which to determine the roof position</param>
        /// <returns>roof position</returns>
        protected static Vector3 Roof(GameObject node)
        {
            Vector3 result = node.transform.position;
            result.y += GetExtent(node).y;
            return result;
        }

        /// <summary>
        /// Removes everything the layout has added to the scence, such as planes etc.
        /// </summary>
        public abstract void Reset();

    }
}

