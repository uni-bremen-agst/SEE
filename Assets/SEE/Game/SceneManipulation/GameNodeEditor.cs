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
    /// Provides methods to edit a node, that is, to change its name or type.
    /// </summary>
    public class GameNodeEditor
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
            GetText(nodeObject)?.SetActive(city.NodeTypes[node.Type].ShowNames);

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
        /// Returns the text object of <paramref name="go"/>. This is the first child of
        /// <paramref name="go"/> that has a <see cref="TextMeshPro"/> component and whose
        /// name contains 'Text'.
        /// </summary>
        /// <param name="go">The game object whose text object is requested.</param>
        /// <returns>The text object or null if there is no such object.</returns>
        private static GameObject GetText(GameObject go)
        {
            foreach (Transform transform in go.transform)
            {
                if (transform.name.Contains("Text") && transform.GetComponent<TextMeshPro>() != null)
                {
                    return transform.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if the decorations of the node needs to be added <see cref="GraphRenderer.AddDecorations(GameObject)"/>
        /// and adds them if necessary.
        /// </summary>
        /// <param name="node">The node to be checked.</param>
        private static void ShouldAddTextObject(Node node)
        {
            if (node.GameObject().ContainingCity().NodeTypes[node.Type].ShowNames
                && node.GameObject().ContainingCity().Renderer is GraphRenderer renderer
                && GetText(node.GameObject()) == null
                && !string.IsNullOrWhiteSpace(node.SourceName))
            {
                renderer.AddDecorations(node.GameObject());
            }
        }
    }
}
