using InControl.UnityDeviceProfiles;
using SEE.DataModel.DG;
using SEE.Game.City;
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
            GameObject nodeObject = node.GameObject(true);
            AbstractSEECity city = nodeObject.ContainingCity();

            if (city.NodeTypes[node.Type].ShowNames)
            {
                GetText()?.SetActive(true);
            }
            else
            {
                GetText()?.SetActive(false);
            }

            if (city.Renderer is GraphRenderer renderer)
            {
                renderer.AdjustStyle(nodeObject);
                if (GetText() != null)
                {
                    GetText().GetComponent<TextMeshPro>().color = nodeObject.GetColor().Invert();
                }
            }

            return;

            GameObject GetText()
            {
                foreach (Transform transform in nodeObject.transform)
                {
                    if (transform.name.Contains("Text") && transform.GetComponent<TextMeshPro>() != null)
                    {
                        return transform.gameObject;
                    }
                }
                return null;
            }
        }
    }
}