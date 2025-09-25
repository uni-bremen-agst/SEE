using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using SEE.GraphProviders.VCS;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attributes of an author sphere.
    /// </summary>
    /// <remarks>This component will be attached to all author spheres.</remarks>
    public class AuthorSphere : SerializedMonoBehaviour
    {
        /// <summary>
        /// The identity of the author.
        /// </summary>
        public FileAuthor Author;

        /// <summary>
        /// All edges connecting the author sphere to the files they contributed to.
        /// </summary>
        public IList<AuthorEdge> Edges = new List<AuthorEdge>();

        /// <summary>
        /// The font used for author labels.
        /// </summary>
        private static TMP_FontAsset font;

        /// <summary>
        /// Prefix for game object names of node labels.
        /// </summary>
        private const string labelPrefix = "Label ";

        /// <summary>
        /// Returns a camera-facing label with the <paramref name="author"/>'s name which will float above
        /// <paramref name="gameObject"/>. The label will be a child of <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object this label should be added to (on top of it).</param>
        /// <param name="author">The author represented by the label.</param>
        /// <param name="fontSize">The font size of the label.</param>
        /// <returns>New game object representing the label</returns>
        private static GameObject AddLabel(GameObject gameObject, FileAuthor author, float fontSize = 2f)
        {
            // Load is not allowed to be called from a field initializer; that is why we do it here.
            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            GameObject nodeLabel = new(labelPrefix + author)
            {
                tag = Tags.Text,
                name = labelPrefix + author
            };
            nodeLabel.transform.position = gameObject.GetTop();

            TextMeshPro tm = nodeLabel.AddComponent<TextMeshPro>();
            tm.font = font;
            tm.fontSize = fontSize;
            tm.text = author.Name;
            tm.color = Color.white;
            tm.alignment = TextAlignmentOptions.Center;

            TextFactory.LiftText(nodeLabel, tm);

            nodeLabel.transform.SetParent(gameObject.transform);
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
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            result.transform.localScale *= 0.25f; // Standard size is too large.
            result.name = "AuthorSphere:" + author;
            result.layer = Layers.InteractableGraphObjects;
            result.transform.SetParent(parent.transform);

            InteractionDecorator.PrepareAuthorForInteraction(result);

            AuthorSphere authorSphere = result.AddComponent<AuthorSphere>();
            authorSphere.Author = author;

            result.AddComponent<ShowAuthorEdges>();

            AddLabel(result, author);

            Renderer renderer = result.GetComponent<Renderer>();

            material.shader = Shader.Find("Standard");
            renderer.sharedMaterial = material;

            result.transform.position = positionOffset + parent.transform.position;
            return result;
        }
    }
}
