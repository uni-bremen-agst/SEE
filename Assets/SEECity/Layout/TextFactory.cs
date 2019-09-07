using SEE.DataModel;
using TMPro;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for text objects that rotate towards the camera.
    /// </summary>
    internal class TextFactory
    {
        /// <summary>
        /// Returns a game object showing the tiven text at given position. The
        /// text rotates towards the main camera.
        /// </summary>
        /// <param name="text">the text to be drawn</param>
        /// <param name="position">the center position at which to draw the text</param>
        /// <param name="width">the width of the rectangle enclosing the text (essentially, 
        /// the text width); the font size will be chose appropriately</param>
        /// <returns>the game object representing the text</returns>
        public static GameObject GetText(string text, Vector3 position, float width)
        {
            GameObject result = new GameObject("Text " + text)
            {
                tag = Tags.Text
            };
            result.transform.position = position;

            TextMeshPro tm = result.AddComponent<TextMeshPro>();
            tm.text = text;
            tm.color = Color.black;
            tm.alignment = TextAlignmentOptions.Center;
            
            RectTransform rect = tm.GetComponent<RectTransform>();
            // We set only the width of the rectangle and leave the z axis to Unity
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            //rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, width);

            tm.enableAutoSizing = true;
            tm.fontSizeMin = 3;
            tm.fontSizeMax = 72;
          
            TextFacingCamera textFacing = result.AddComponent<TextFacingCamera>();
            // Rendering distance is set relative to the text's width
            textFacing.minimalDistance = width;
            textFacing.maximalDistance = 10.0f * width;

            // No shading as this might be expensive and even distracts.
            Renderer renderer = result.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return result;
        }
    }
}
