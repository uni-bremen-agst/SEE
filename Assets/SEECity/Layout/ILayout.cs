using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public abstract class INodeLayout
    {
        public INodeLayout(string widthMetric, string heightMetric, string breadthMetric,
               SerializableDictionary<string, IconFactory.Erosion> issueMap,
               BlockFactory blockFactory,
               IScale scaler,
               bool showErosions)
        {
            this.widthMetric = widthMetric;
            this.heightMetric = heightMetric;
            this.breadthMetric = breadthMetric;
            this.issueMap = issueMap;
            this.blockFactory = blockFactory;
            this.scaler = scaler;
            this.showErosions = showErosions;
        }

        // A mapping of graph nodes onto the game objects representing them visually in the scene
        protected Dictionary<Node, GameObject> gameNodes = new Dictionary<Node, GameObject>();

        public Dictionary<Node, GameObject> Nodes()
        {
            return gameNodes;
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
        /// Mapping of node attributes onto erosion issue icons.
        /// </summary>
        protected readonly SerializableDictionary<string, IconFactory.Erosion> issueMap;

        /// <summary>
        /// A factory to create visual representations of graph nodes (e.g., cubes or CScape buildings).
        /// </summary>
        protected readonly BlockFactory blockFactory;

        /// <summary>
        /// Whether the erosions should be drawn.
        /// </summary>
        protected bool showErosions;

        /// <summary>
        /// The y co-ordinate of the ground where blocks are placed.
        /// </summary>
        protected const float groundLevel = 0.0f;

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
        public abstract void Draw(Graph graph);

        // name of the layout
        protected string name = "";

        /// <summary>
        /// Used to determine the lengths of the node blocks.
        /// </summary>
        protected readonly IScale scaler;

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
        /// Adds a NodeRef component to given block referencing to given node.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="node"></param>
        protected void AttachNode(GameObject block, Node node)
        {
            NodeRef nodeRef = block.AddComponent<NodeRef>();
            nodeRef.node = node;
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

        /// <summary>
        /// Stacks sprites for software-erosion issues atop of the roof of the given node
        /// in ascending order in terms of the sprite width. The sprite width is proportional
        /// to the normalized metric value for the erosion issue.
        /// </summary>
        /// <param name="node"></param>
        protected void AddErosionIssues(Node node)
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
                        sprite.name = sprite.name + " " + node.SourceName;
                        // The sprite becomes a child of the node.
                        sprite.transform.parent = gameNodes[node].transform;

                        Vector3 spriteSize = GetSizeOfSprite(sprite);
                        // Scale the sprite to one Unity unit.
                        float spriteScale = 1.0f / spriteSize.x;
                        // Scale the erosion issue by normalization.
                        float metricScale = scaler.GetNormalizedValue(node, issue.Key);
                        // First: scale its width to unit size 1.0 maintaining the aspect ratio
                        sprite.transform.localScale *= spriteScale * blockFactory.Unit();
                        // Now scale it by the normalized metric.
                        sprite.transform.localScale *= metricScale; 

                        sprites.Add(sprite);
                    }
                }
            }

            // Now we stack the sprites on top of the roof of the building in
            // ascending order of their widths.
            {
                // The space that we put in between two subsequent erosion issue sprites.
                Vector3 delta = Vector3.up / 100.0f;
                Vector3 currentRoof = blockFactory.Roof(gameNodes[node]);
                sprites.Sort(Comparer<GameObject>.Create((left, right) => GetSizeOfSprite(left).x.CompareTo(GetSizeOfSprite(right).x)));
                foreach (GameObject sprite in sprites)
                {
                    Vector3 size = GetSizeOfSprite(sprite);
                    // Note: Consider that the position of the sprite is its center.
                    Vector3 halfHeight = (size.y / 2.0f) * Vector3.up;
                    sprite.transform.position = currentRoof + delta + halfHeight;
                    currentRoof = sprite.transform.position + halfHeight;
                }
            }
        }
    }
}