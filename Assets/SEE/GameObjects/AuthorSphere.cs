using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using SEE.GraphProviders.VCS;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attributes of an author sphere.
    /// </summary>
    /// <remarks>This component will be attached to all author spheres.</remarks>
    public class AuthorSphere : MonoBehaviour
    {
        /// <summary>
        /// The identity of the author.
        /// </summary>
        public FileAuthor Author;

        /// <summary>
        /// All edges connecting the author sphere to the files they contributed to.
        /// </summary>
        public IList<GameObject> Edges = new List<GameObject>();

        /// <summary>
        /// Returns a camera-facing label with the author's name which will float above the sphere.
        /// </summary>
        /// <param name="author">The author represented by the label.</param>
        /// <param name="position">The world-space position of the label.</param>
        /// <param name="fontSize">The font size of the label.</param>
        /// <returns>New game object representing the label</returns>
        private static GameObject AddLabel(FileAuthor author, Vector3 position, float fontSize = 2f)
        {
            GameObject nodeLabel = new("Text " + author)
            {
                tag = Tags.Text
            };
            nodeLabel.transform.position = position;

            TextMeshPro tm = nodeLabel.AddComponent<TextMeshPro>();
            tm.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tm.fontSize = fontSize;
            tm.text = author.Name;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;

            nodeLabel.name = "Label:" + author;
            nodeLabel.AddComponent<FaceCamera>();

            return nodeLabel;
        }

        /// <summary>
        /// Creates and returns a new game object representing an author under the given <paramref name="parent"/>
        /// representing the <paramref name="author"/>. The game object will be positioned at the given
        /// world-space <paramref name="positionOffset"/> above the world-position of <paramref name="parent"/>
        /// and is rendered using the given <paramref name="material"/>.
        ///
        /// The new game object will also have a label (as a child game object) with the author's name
        /// which will float above the game object.
        ///
        /// The following components will be added to the new game object:
        /// <see cref="AuthorSphere"/> and <see cref="ShowAuthorEdges"/>.
        /// </summary>
        /// <param name="parent">game object the new game object will be placed as a child</param>
        /// <param name="author">the author that will be represented by this game object</param>
        /// <param name="material">the material to be used for the new game object</param>
        /// <param name="positionOffset">the position offset of the new game object above
        /// the world-space position of the <paramref name="parent"/></param>
        /// <returns>new game object representing the author</returns>
        public static GameObject CreateAuthor(GameObject parent, FileAuthor author, Material material, Vector3 positionOffset)
        {
            // FIXME: We need to add a collider.
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            result.name = "AuthorSphere:" + author;

            AuthorSphere authorSphere = result.AddComponent<AuthorSphere>();
            authorSphere.Author = author;

            result.AddComponent<ShowAuthorEdges>();

            GameObject label = AddLabel(author, result.GetTop());
            label.transform.SetParent(result.transform);

            result.transform.SetParent(parent.transform);
            result.transform.localScale *= 0.25f;

            Renderer renderer = result.GetComponent<Renderer>();
            // Override shader so the spheres don't clip over the code city.
            material.shader = Shader.Find("Standard");
            renderer.sharedMaterial = material;

            result.transform.position = positionOffset + parent.transform.position;
            return result;
        }
    }
}
