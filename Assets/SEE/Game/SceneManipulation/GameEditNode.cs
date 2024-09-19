using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using TMPro;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Edits a node.
    /// </summary>
    public class GameEditNode
    {
        /// <summary>
        /// Changes the name of a node.
        /// </summary>
        /// <param name="node">The node which name should be changed.</param>
        /// <param name="newName">The new name.</param>
        public static void ChangeName(Node node, string newName)
        {
            node.SourceName = newName.Trim();
            if (node.GameObject() != null)
            {
                Transform transform = node.GameObject().transform;
                foreach (Transform t in transform)
                {
                    if (t.name.Contains("Text") || t.name.Contains("Label"))
                    {
                        t.GetComponent<TextMeshPro>().text = newName.Trim();
                    }
                }
            }
        }

        /// <summary>
        /// Changes the type of a node.
        /// </summary>
        /// <param name="node">The node which type should be changed.</param>
        /// <param name="type">The new type.</param>
        public static void ChangeType(Node node, string type)
        {
            node.Type = type;
            if (node.GameObject(true).ContainingCity().Renderer is GraphRenderer renderer)
            {
                renderer.AdjustStyle(node.GameObject());
            }
        }
    }
}