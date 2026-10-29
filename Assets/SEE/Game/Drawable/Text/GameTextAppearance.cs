using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace SEE.Game.Drawable.Text
{
    /// <summary>
    /// Provides appearance operations for drawable text objects.
    /// </summary>
    internal static class GameTextAppearance
    {
        /// <summary>
        /// The shader keyword for enabling a text outline.
        /// </summary>
        private const string outlineKeyword = "OUTLINE_ON";

        /// <summary>
        /// Changes whether the outline of the given text is enabled.
        /// </summary>
        /// <param name="textObj">The text whose outline status should be changed.</param>
        /// <param name="status">Whether the outline should be enabled.</param>
        internal static void ChangeOutlineStatus(GameObject textObj, bool status)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                LocalKeyword keyword =
                    new(textMesh.fontMaterial.shader, outlineKeyword);

                textMesh.fontMaterial.SetKeyword(keyword, status);
            }
        }

        /// <summary>
        /// Changes the outline color of the given text.
        /// </summary>
        /// <param name="textObj">The text whose outline color should be changed.</param>
        /// <param name="color">The new outline color.</param>
        internal static void ChangeOutlineColor(GameObject textObj, Color color)
        {
            if (textObj.CompareTag(Tags.DText))
            {
                textObj.GetComponent<TextMeshPro>().outlineColor = color;
            }
        }

        /// <summary>
        /// Returns whether the outline of the given text is enabled.
        /// </summary>
        /// <param name="textMesh">The text whose outline status should be returned.</param>
        /// <returns>Whether the outline is enabled.</returns>
        internal static bool IsOutlineEnabled(TextMeshPro textMesh)
        {
            return textMesh.fontMaterial.IsKeywordEnabled(outlineKeyword);
        }
    }
}
