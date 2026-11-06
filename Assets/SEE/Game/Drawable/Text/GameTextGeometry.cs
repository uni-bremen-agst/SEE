using SEE.Game.Drawable.ActionHelpers;
using SEE.Utils;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.Game.Drawable.Text
{
    /// <summary>
    /// Provides geometry and mesh operations for drawable text objects.
    /// </summary>
    internal static class GameTextGeometry
    {
        /// <summary>
        /// Calculates the width of the given text based on its first line.
        /// </summary>
        /// <param name="text">The first line of the text.</param>
        /// <param name="fontAsset">The used font asset.</param>
        /// <param name="fontSize">The used font size.</param>
        /// <param name="style">The used font styles.</param>
        /// <returns>The calculated width of the text.</returns>
        private static float TextWidthApproximation(string text, TMP_FontAsset fontAsset,
            float fontSize, FontStyles style)
        {
            string result = RichTextRemover.RemoveRichText(text);

            text = text.ToLower();
            bool htmlBold = text.Contains("<b>");
            bool htmlUpperCase = text.Contains("<uppercase>");
            bool htmlSmallCaps = text.Contains("<smallcaps>");

            float pointSizeScale =
                fontSize / (fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale * 10);
            float emScale = fontSize * 0.001f;

            float styleSpacingAdjustment =
                (style & FontStyles.Bold) == FontStyles.Bold || htmlBold
                    ? fontAsset.boldSpacing
                    : 0;
            float normalSpacingAdjustment = fontAsset.normalSpacingOffset;
            float width = 0;

            if ((style & FontStyles.UpperCase) != 0
                || (style & FontStyles.SmallCaps) != 0
                || htmlUpperCase
                || htmlSmallCaps)
            {
                result = result.ToUpper();
            }

            for (int i = 0; i < result.Length; i++)
            {
                if (fontAsset.characterLookupTable.TryGetValue(
                    result[i], out TMP_Character character))
                {
                    width += character.glyph.metrics.horizontalAdvance * pointSizeScale
                        + (styleSpacingAdjustment + normalSpacingAdjustment) * emScale;
                }
            }

            return width;
        }

        /// <summary>
        /// Calculates the width and height of the given text.
        /// </summary>
        /// <param name="text">The written text.</param>
        /// <param name="fontAsset">The used font asset.</param>
        /// <param name="fontSize">The used font size.</param>
        /// <param name="styles">The used font styles.</param>
        /// <returns>The calculated width and height.</returns>
        internal static Vector2 CalculateWidthAndHeight(string text, TMP_FontAsset fontAsset,
            float fontSize, FontStyles styles)
        {
            string[] split = text.Split(
                new string[] { "\n", "<br>" },
                System.StringSplitOptions.None);

            float x = split
                .ToList()
                .Max(s => TextWidthApproximation(s, fontAsset, fontSize, styles));

            float y = split.Count() * 0.1f * fontSize;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Refreshes the mesh collider of the given text object.
        /// </summary>
        /// <param name="textObj">The text object whose mesh collider should be refreshed.</param>
        internal static void RefreshMeshCollider(GameObject textObj)
        {
            if (textObj.GetComponent<MeshCollider>() != null)
            {
                MeshCollider meshCollider = textObj.GetComponent<MeshCollider>();
                meshCollider.enabled = false;
                meshCollider.enabled = true;
            }
        }
    }
}
