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
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        public static GameObject GetText(string text, Vector3 position, int fontSize)
        {
            GameObject result = new GameObject("Text " + text);
            result.tag = Tags.Text;
            result.transform.position = position;

            TextMeshPro tm = result.AddComponent<TextMeshPro>();
            tm.text = text;
            tm.color = Color.black;
            tm.alignment = TextAlignmentOptions.Center;
            // TODO: Size based on depth
            tm.fontSize = fontSize;

            TextFacingCamera textFacing = result.AddComponent<TextFacingCamera>();
            // TODO: Rendering distance based on depth
            //textFacing.minimalDistance = 0.0f;
            //textFacing.maximalDistance = int.MaxValue;

            // No shading as this might be expensive and even distracts.
            Renderer renderer = result.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return result;
        }
    }
}
