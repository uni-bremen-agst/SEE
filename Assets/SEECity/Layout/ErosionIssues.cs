﻿using System.Collections.Generic;
using SEE.DataModel;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Allows to add erosion issues as sprites atop of game objects.
    /// </summary>
    public class ErosionIssues
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="issueMap">the relevant metrics for the erosion issues</param>
        /// <param name="leaveNodeFactory">factory that created the game nodes that are to be decorated</param>
        /// <param name="scaler">scaling to be applied on the metrics for the erosion issues</param>
        public ErosionIssues(SerializableDictionary<string, IconFactory.Erosion> issueMap, NodeFactory leaveNodeFactory, IScale scaler)
        {
            this.issueMap = issueMap;
            this.leaveNodeFactory = leaveNodeFactory;
            this.scaler = scaler;
        }

        /// <summary>
        /// The settings that determine the relevant metrics for the erosion issues.
        /// </summary>
        private readonly SerializableDictionary<string, IconFactory.Erosion> issueMap;

        /// <summary>
        /// The factory that created the game nodes that are to be decorated.
        /// </summary>
        private readonly NodeFactory leaveNodeFactory;

        /// <summary>
        /// The scaling to be applied on the metrics for the erosion issues.
        /// </summary>
        private readonly IScale scaler;

        /// <summary>
        /// Creates sprites for software-erosion indicators for all given game nodes.
        /// </summary>
        /// <param name="gameNodes">list of game nodes for which to create erosion visualizations</param>
        public void Add(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject block in gameNodes)
            {
                AddErosionIssues(block);
            }
        }

        /// <summary>
        /// Stacks sprites for software-erosion issues atop of the roof of the given node
        /// in ascending order in terms of the sprite width. The sprite width is proportional
        /// to the normalized metric value for the erosion issue.
        /// </summary>
        /// <param name="node"></param>
        protected void AddErosionIssues(GameObject gameNode)
        {
            Node node = gameNode.GetComponent<NodeRef>().node;

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

                        Vector3 spriteSize = GetSizeOfSprite(sprite);
                        // Scale the sprite to one Unity unit.
                        float spriteScale = 1.0f / spriteSize.x;
                        // Scale the erosion issue by normalization.
                        float metricScale = scaler.GetNormalizedValue(node, issue.Key);
                        // First: scale its width to unit size 1.0 maintaining the aspect ratio
                        sprite.transform.localScale *= spriteScale * leaveNodeFactory.Unit();
                        // Now scale it by the normalized metric.
                        sprite.transform.localScale *= metricScale;
                        sprite.transform.position = leaveNodeFactory.Roof(gameNode);
                        sprites.Add(sprite);
                    }
                }
            }

            // Now we stack the sprites on top of the roof of the building in
            // ascending order of their widths.
            {
                // The space that we put in between two subsequent erosion issue sprites.
                Vector3 delta = Vector3.up / 100.0f;
                Vector3 currentRoof = leaveNodeFactory.Roof(gameNode);
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
