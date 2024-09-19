using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.CityRendering;
using SEE.GO;
using SEE.Utils;
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
                ShouldAddTextObject(node);
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
            GameObject nodeObject = node.GameObject(true);
            AbstractSEECity city = nodeObject.ContainingCity();

            ShouldAddTextObject(node);
            if (city.NodeTypes[node.Type].ShowNames)
            {
                GetText(nodeObject)?.SetActive(true);
            }
            else
            {
                GetText(nodeObject)?.SetActive(false);
            }

            if (city.Renderer is GraphRenderer renderer)
            {
                renderer.AdjustStyle(nodeObject);
                if (GetText(nodeObject) != null)
                {
                    GetText(nodeObject).GetComponent<TextMeshPro>().color = nodeObject.GetColor().Invert();
                }
            }
        }

        /// <summary>
        /// Returns the text object of a node game object.
        /// </summary>
        /// <returns>The text object.</returns>
        private static GameObject GetText(GameObject node)
        {
            foreach (Transform transform in node.transform)
            {
                if (transform.name.Contains("Text") && transform.GetComponent<TextMeshPro>() != null)
                {
                    return transform.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the text object needs to be added.
        /// </summary>
        /// <param name="node">The node to be checked.</param>
        private static void ShouldAddTextObject(Node node)
        {
            if (node.GameObject().ContainingCity().NodeTypes[node.Type].ShowNames
                && node.GameObject().ContainingCity().Renderer is GraphRenderer renderer
                && GetText(node.GameObject()) == null)
            {
                renderer.AddDecorations(node.GameObject());
            }
        }
    }
}