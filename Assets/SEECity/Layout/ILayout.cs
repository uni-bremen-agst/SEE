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
               float edgeWidth,
               bool showErosions)
        {
            this.showEdges = showEdges;
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
            this.issueMap = issueMap;
            this.blockFactory = blockFactory;
            this.scaler = scaler;
            this.edgeWidth = edgeWidth;
            this.showErosions = showErosions;
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
        /// Whether the erosions should be drawn.
        /// </summary>
        protected bool showErosions;

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

        protected static Vector3 GetSizeOfSprite(GameObject go)
        {
            // The game object representing an erosion is a composite of 
            // multiple LOD child objects to be drawn depending how close
            // the camera is. The container object 'go' itself does not
            // have a renderer. We need to obtain the renderer of the
            // first child hat represents the object at LOD 0 instead.
            Renderer renderer = go.GetComponentInChildren<Renderer>();
            // Note: renderer.sprite.bounds.size yields the original size
            // of the sprite of the prefab. It does not consider the scaling.
            // It depends only upon the imported graphic. That is why we
            // need to use renderer.bounds.size.
            return renderer.bounds.size;
        }

        // Comparer for the widths of sprites.
        private readonly IComparer<GameObject> comparer = new WidthComparer();

        /// <summary>
        /// Comparer for the widths of sprites. Let width(x) be the width
        /// of a sprite. Yields:
        /// -1 if width(left) < width(right) 
        /// 0 if width(left) = width(right)
        /// 1 if width(left) ></width> width(right)
        /// </summary>
        private class WidthComparer : IComparer<GameObject>
        {
            public int Compare(GameObject left, GameObject right)
            {
                float widthLeft = GetSizeOfSprite(left).x;
                float widthRight = GetSizeOfSprite(right).x;
                return widthLeft.CompareTo(widthRight);
            }
        }

        /// <summary>
        /// Stacks sprites for software-erosion issues atop of the roof of the given node
        /// in ascending order in terms of the sprite width. The sprite width is proportional
        /// to the normalized metric value for the erosion issue.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="scaler"></param>
        protected void AddErosionIssues(Node node, IScale scaler)
        {
            // The list of sprites for the erosion issues.
            List<GameObject> sprites = new List<GameObject>();

            // Create and scale the sprites and add them to the list of sprites.
            foreach (KeyValuePair<string, IconFactory.Erosion> issue in issueMap)
            {
                if (node.TryGetNumeric(issue.Key, out float value))
                {
                    if (value > 0.0f)
                    {
                        GameObject sprite = IconFactory.Instance.GetIcon(Vector3.zero, issue.Value);
                        // The sprite will not become a child of node so that we can more easily
                        // scale it. If the sprite had a parent, localScale would be relative to
                        // the parent's size. That just complicates things.
                        Vector3 spriteSize = GetSizeOfSprite(sprite);
                        // Scale the sprite to one Unity unit.
                        float spriteScale = 1.0f / spriteSize.x;
                        // Scale the erosion issue by normalization.
                        float metricScale = scaler.GetNormalizedValue(node, issue.Key);
                        //Debug.LogFormat("sprite {0} before scaling: size={1}.\n",
                        //                sprite.name, GetSizeOfSprite(sprite));
                        // First: scale its width to unit size 1.0 maintaining the aspect ratio
                        sprite.transform.localScale *= spriteScale * blockFactory.Unit();
                        //Debug.LogFormat("sprite {0} scaled to unit size: size={1}.\n",
                        //                sprite.name, GetSizeOfSprite(sprite));
                        // Now scale it by the normalized metric.
                        sprite.transform.localScale *= metricScale;
                        //Debug.LogFormat("sprite {0} after scaling: size={1}.\n",
                        //                sprite.name, GetSizeOfSprite(sprite));
                        sprite.name = sprite.name + " " + node.SourceName;
                        sprites.Add(sprite);
                    }
                }
            }

            // Now we stack the sprites on top of the roof of the building in
            // ascending order of their widths.
            {
                // The space that we put in between two subsequent erosion issue sprites.
                Vector3 delta = Vector3.up / 100.0f;
                Vector3 currentRoof = blockFactory.Roof(gameObjects[node]);
                sprites.Sort(comparer);
                //Debug.Log("---------------------------------\n");
                foreach (GameObject sprite in sprites)
                {
                    Vector3 size = GetSizeOfSprite(sprite);
                    // Note: Consider that the position of the sprite is its center.
                    Vector3 halfHeight = (size.y / 2.0f) * Vector3.up;
                    sprite.transform.position = currentRoof + delta + halfHeight;
                    currentRoof = sprite.transform.position + halfHeight;

                    //Debug.LogFormat("sprite {0}: size={1} position={2} halfHeight={3}.\n",
                    //                sprite.name, size, sprite.transform.position, halfHeight);
                }
            }
        }
    }
}