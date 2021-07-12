using System.Linq;
using SEE.DataModel;
using TMPro;
using UnityEngine;

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
        
        /// <summary>
        /// Name of the font used for the text.
        /// </summary>
        private const string PortalFontName = "Fonts & Materials/LiberationSans SDF - Portal";

        /// <summary>
        /// This will apply an outline effect across <em>all</em> TextMeshPro instances with the same
        /// shared material as the given one.
        /// </summary>
        /// <param name="outline">Whether the outline should be enabled or disabled.
        /// If this is false, all other parameters besides <paramref name="tm"/> will be ignored.</param>
        /// <param name="tm">The <see cref="TextMeshPro"/> instance whose material we should apply the effect on.
        /// Note that this will affect every TextMeshPro instance with the same material.</param>
        /// <param name="thickness">The thickness of the outline. Default: 0.1</param>
        /// <param name="outlineColor">The color of the outline. Default: white</param>
        public static void SetOutline(bool outline, TextMeshPro tm, float thickness = 0.1f, Color? outlineColor = null)
        {
            string[] keywords = tm.fontSharedMaterial.shaderKeywords ?? new string[]{};
            if (outline)
            {
                tm.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor ?? Color.black);
                tm.fontSharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, thickness);
                tm.fontSharedMaterial.shaderKeywords = keywords.Append(ShaderUtilities.Keyword_Outline).ToArray();
            }
            else
            {
                // Remove outline activation keyword
                tm.fontSharedMaterial.shaderKeywords = keywords 
                                                       .Where(x => !x.Equals(ShaderUtilities.Keyword_Outline))
                                                       .ToArray();
            }
            tm.UpdateMeshPadding();
        }

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
                                                 Color? textColor = null)
        {
            CreateText(text, position, textColor, out TextMeshPro tm, out GameObject result);

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
