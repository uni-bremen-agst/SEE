using SEE.DataModel;
using TMPro;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for text objects that rotate towards the camera.
    /// </summary>
    internal class TextFactory
    {
        /// <summary>
        /// Color of the text.
        /// </summary>
        private static readonly Color TextColor = Color.gray; // Color.white;
        private const string PortalFontName = "Fonts & Materials/LiberationSans SDF - Portal";

        /// <summary>
        /// Returns a game object showing the given <paramref name="text"/> at given <paramref name="position"/>. 
        /// The text rotates towards the main camera.
        /// </summary>
        /// <param name="text">the text to be drawn</param>
        /// <param name="position">the center position at which to draw the text</param>
        /// <param name="width">the width of the rectangle enclosing the text (essentially, 
        /// the text width); the font size will be chose appropriately</param>
        /// <param name="lift">if true, the text will be lifted up by its extent; that is, is y position is actually the bottom line (position.y + extents.y)</param>
        /// <returns>the game object representing the text</returns>
        public static GameObject GetText(string text, Vector3 position, float width, bool lift = true, Color? textColor = null)
        {
            GameObject result = new GameObject("Text " + text)
            {
                tag = Tags.Text
            };
            result.transform.position = position;

            TextMeshPro tm = result.AddComponent<TextMeshPro>();
            tm.font = Resources.Load<TMP_FontAsset>(PortalFontName);
            tm.text = text;
            tm.color = textColor ?? TextColor;
            tm.alignment = TextAlignmentOptions.Center;

            RectTransform rect = tm.GetComponent<RectTransform>();
            // We set width and height of the rectangle and leave the actual size to Unity,
            // which will select a font that matches our size constraints.
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

            tm.enableAutoSizing = true;
            tm.fontSizeMin = 0.0f;
            tm.fontSizeMax = 5;

            // No shading as this might be expensive and even distracts.
            Renderer renderer = result.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (lift)
            {
                // may need to be called before retrieving the bounds to make sure they are up to date
                tm.ForceMeshUpdate();
                // unlike other types of game objects, the renderer does not allow us to retrieve the
                // extents of the text; we need to use tm.textBounds instead
                Bounds bounds = tm.textBounds;
                float yPosition = bounds.extents.y;
                result.transform.position += yPosition * Vector3.up;
            }
            return result;
        }
    }
}
