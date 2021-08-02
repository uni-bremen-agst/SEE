using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Allows to add erosion issues as sprites atop of game objects.
    /// </summary>
    internal class ErosionIssues
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="issueMap">the relevant metrics for the erosion issues</param>
        /// <param name="innerNodeFactory">factory that created the game nodes that are to be decorated</param>
        /// <param name="scaler">scaling to be applied on the metrics for the erosion issues</param>
        /// <param name="erosionScalingFactor">the factor by which the erosion icons shall be scaled</param>
        public ErosionIssues(Dictionary<string, IconFactory.Erosion> issueMap,
                             NodeFactory innerNodeFactory, IScale scaler, float erosionScalingFactor)
        {
            this.issueMap = issueMap;
            this.innerNodeFactory = innerNodeFactory;
            this.scaler = scaler;
            this.erosionScalingFactor = erosionScalingFactor;
        }
        
        /// <summary>
        /// Prefix used for game objects containing erosion sprites.
        /// </summary>
        public const string EROSION_SPRITE_PREFIX = "Erosion:";

        /// <summary>
        /// The settings that determine the relevant metrics for the erosion issues.
        /// </summary>
        private readonly Dictionary<string, IconFactory.Erosion> issueMap;

        /// <summary>
        /// The factory that created the game nodes that are to be decorated.
        /// </summary>
        private readonly NodeFactory innerNodeFactory;

        /// <summary>
        /// The scaling to be applied on the metrics for the erosion issues.
        /// </summary>
        private readonly IScale scaler;

        /// <summary>
        /// The maximal absolute width of a sprite representing an erosion in world-space Unity units.
        /// </summary>
        private readonly float erosionScalingFactor;

        /// <summary>
        /// Creates sprites for software-erosion indicators for all given game nodes as children.
        /// </summary>
        /// <param name="gameNodes">list of game nodes for which to create erosion visualizations</param>
        public void Add(IEnumerable<GameObject> gameNodes)
        {
            foreach (GameObject block in gameNodes)
            {
                NodeRef nodeRef = block.GetComponent<NodeRef>();
                AddErosionIssues(nodeRef);
            }
        }

        /// <summary>
        /// Stacks sprites for software-erosion issues atop of the roof of the given node
        /// in ascending order in terms of the sprite width. The sprite width is proportional
        /// to the normalized metric value for the erosion issue. The sprites are added as
        /// children to <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">the game node which the sprites are to be created for</param>
        protected void AddErosionIssues(NodeRef gameNode)
        {
            Node node = gameNode.Value;

            // The list of sprites for the erosion issues.
            List<GameObject> sprites = new List<GameObject>();

            // Create and scale the sprites and add them to the list of sprites.
            foreach (KeyValuePair<string, IconFactory.Erosion> issue in issueMap)
            {
                if (node.TryGetNumeric(issue.Key, out float value) && value > 1.0)
                {
                    // Scale the erosion issue by normalization set in relation to the
                    // maximum value of the normalized metric. Hence, this value is in [0,1].
                    float metricScale = scaler.GetRelativeNormalizedValueInLevel(issue.Key, node);

                    GameObject sprite = IconFactory.Instance.GetIcon(Vector3.zero, issue.Value, 
                                                                     value.ToString(CultureInfo.InvariantCulture), 
                                                                     new Color(metricScale, 0, 0, 
                                                                               Mathf.Lerp(0.75f, 1f, metricScale)));
                    
                    // NOTE: The EROSION_SPRITE_PREFIX must be present here,
                    // otherwise partial erosion display won't work!
                    sprite.name = $"{EROSION_SPRITE_PREFIX} {sprite.name} {node.SourceName}";

                    Vector3 spriteSize = GetSizeOfSprite(sprite);
                    // Scale the sprite to one Unity unit.
                    float spriteScale = 1.0f / spriteSize.x;
                    Vector3 scale = sprite.transform.localScale;
                    // First: scale its width to unit size 1.0 maintaining the aspect ratio.
                    scale *= spriteScale;
                    // assert: scale.x = 1
                    // Now scale it by the normalized metric.
                    // NOTE: Commented out because color already represents the metric.
                    //scale *= metricScale;
                    // assert: scale.x in [0,1]
                    // UnityEngine.Assertions.Assert.IsTrue(0 <= scale.x);
                    // UnityEngine.Assertions.Assert.IsTrue(scale.x <= 1, $"scale.x={scale.x}");

                    // Now scale the sprite into the corridor [0, maxSpriteWidth]
                    scale *= Mathf.Lerp(0, innerNodeFactory.GetSize(gameNode.gameObject).x, scale.x);

                    // Finally, scale sprite by the configured scaling factor
                    scale *= erosionScalingFactor;

                    sprite.transform.localScale = scale;
                    sprite.transform.position = innerNodeFactory.Roof(gameNode.gameObject);

                    sprites.Add(sprite);
                }
            }

            // Now we stack the sprites on top of the roof of the building in
            // ascending order of their widths.
            {
                Vector3 currentRoof = innerNodeFactory.Roof(gameNode.gameObject);
                currentRoof += Vector3.up * innerNodeFactory.GetSize(gameNode.gameObject).x / 6;
                sprites.Sort(Comparer<GameObject>.Create((left, right) =>
                                                             GetSizeOfSprite(left).x.CompareTo(GetSizeOfSprite(right).x)));
                foreach (GameObject sprite in sprites)
                {
                    Vector3 size = GetSizeOfSprite(sprite);
                    // Note: Consider that the position of the sprite is its center.
                    Vector3 halfHeight = (size.y / 2.0f) * Vector3.up;
                    sprite.transform.position = currentRoof + halfHeight;
                    currentRoof = sprite.transform.position + halfHeight;
                }
            }
            // The sprites have reached their final scale and position. Now we can
            // add them to their parent.
            foreach (GameObject sprite in sprites)
            {
                sprite.transform.SetParent(gameNode.transform);
            }
        }

        /// <summary>
        /// Returns the size of the sprite for given game node that was drawn for
        /// a software-erosion indicator above the roof of the node.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <returns>size of the sprite</returns>
        protected static Vector3 GetSizeOfSprite(GameObject gameNode)
        {
            // The game object representing an erosion is a composite of
            // multiple LOD child objects to be drawn depending how close
            // the camera is. The container object 'go' itself does not
            // have a renderer. We need to obtain the renderer of the
            // first child hat represents the object at LOD 0 instead.
            Renderer renderer = gameNode.GetComponentInChildren<Renderer>();
            // Note: renderer.sprite.bounds.size yields the original size
            // of the sprite of the prefab. It does not consider the scaling.
            // It depends only upon the imported graphic. That is why we
            // need to use renderer.bounds.size.
            return renderer.bounds.size;
        }
    }
}