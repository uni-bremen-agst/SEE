using Microsoft.MixedReality.Toolkit.Utilities;
using SEE.DataModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.GO
{
    /// <summary>
    /// A factory for text objects that rotate towards the camera.
    /// </summary>
    internal static class TextFactory
    {
        /// <summary>
        /// Color of the text.
        /// </summary>
        private static readonly Color TextColorDefault = Color.black;
        private const string PortalFontName = "Fonts & Materials/LiberationSans SDF - Portal";

        /// <summary>
        /// Returns a game object showing the given <paramref name="text"/> at given <paramref name="position"/>
        /// with given <paramref name="fontSize"/>. 
        /// The text rotates towards the main camera.
        /// </summary>
        /// <param name="text">the text to be drawn</param>
        /// <param name="position">the center position at which to draw the text</param>
        /// <param name="fontSize">the size of the font with which the text is drawn</param>
        /// <param name="lift">if true, the text will be lifted up by its extent; that is, its y position is actually
        /// the bottom line (position.y + extents.y)</param>
        /// <param name="textColor">the color of the text (default: black)</param>
        /// <returns>the game object representing the text</returns>
        public static GameObject GetTextWithSize(string text, Vector3 position, float fontSize, bool lift = true, 
                                                 bool outline = false, Color? textColor = null)
        {
            CreateText(text, position, textColor, out TextMeshPro tm, out GameObject result);

            if (outline)
            {
                //TODO: Use shader outline instead
                Outline outl = result.AddComponent<Outline>();
                outl.effectColor = textColor?.Invert() ?? Color.white;
            }

            tm.fontSize = fontSize;

            // No shading as this might be expensive and even distracts.
            DisableShading(result);

            if (lift)
            {
                LiftText(tm, result);
            }
            return result;
        }

        /// <summary>
        /// Returns a game object showing the given <paramref name="text"/> at given <paramref name="position"/>
        /// with given <paramref name="width"/>
        /// The text rotates towards the main camera.
        /// </summary>
        /// <param name="text">the text to be drawn</param>
        /// <param name="position">the center position at which to draw the text</param>
        /// <param name="width">the width of the rectangle enclosing the text (essentially, 
        /// the text width); the font size will be chosen appropriately</param>
        /// <param name="lift">if true, the text will be lifted up by its extent; that is, its y position is actually
        /// the bottom line (position.y + extents.y)</param>
        /// <param name="textColor">the color of the text (default: <see cref="TextColorDefault"/>)</param>
        /// <returns>the game object representing the text</returns>
        public static GameObject GetTextWithWidth(string text, Vector3 position, float width, bool lift = true, 
            Color? textColor = null)
        {
            CreateText(text, position, textColor, out TextMeshPro tm, out GameObject result);

            RectTransform rect = tm.GetComponent<RectTransform>();
            // We set width and height of the rectangle and leave the actual size to Unity,
            // which will select a font that matches our size constraints.
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

            tm.enableAutoSizing = true;
            tm.fontSizeMin = 0.0f;
            tm.fontSizeMax = 5;

            // No shading as this might be expensive and even distracts.
            DisableShading(result);

            if (lift)
            {
                LiftText(tm, result);
            }
            return result;
        }

        /// <summary>
        /// Creates a new GameObject with a TextMeshPro component at the given <paramref name="position"/>.
        /// The text will have the given <paramref name="text"/> and <paramref name="textColor"/> and will be
        /// center aligned.
        /// </summary>
        /// <param name="text">The text to be used</param>
        /// <param name="position">the center position at which to create the GameObject</param>
        /// <param name="textColor">the color of the text</param>
        /// <param name="tm">the TextMeshPro component which will be attached to <paramref name="result"/></param>
        /// <param name="result">the GameObject containing the TextMeshPro component <paramref name="tm"/></param>
        /// <returns></returns>
        private static void CreateText(string text, Vector3 position, Color? textColor, out TextMeshPro tm, 
            out GameObject result)
        {
            result = new GameObject("Text " + text)
            {
                tag = Tags.Text
            };
            result.transform.position = position;

            tm = result.AddComponent<TextMeshPro>();
            tm.font = Resources.Load<TMP_FontAsset>(PortalFontName);
            tm.text = text;
            tm.color = textColor ?? TextColorDefault;
            tm.alignment = TextAlignmentOptions.Center;
        }

        private static void LiftText(TextMeshPro tm, GameObject result)
        {
            // may need to be called before retrieving the bounds to make sure they are up to date
            tm.ForceMeshUpdate();
            // unlike other types of game objects, the renderer does not allow us to retrieve the
            // extents of the text; we need to use tm.textBounds instead
            Bounds bounds = tm.textBounds;
            float yPosition = bounds.extents.y;
            result.transform.position += yPosition * Vector3.up;
        }

        private static void DisableShading(GameObject result)
        {
            Renderer renderer = result.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
