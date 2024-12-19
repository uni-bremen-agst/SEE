using UnityEngine;

namespace SEE.UI3D
{
    /// <summary>
    /// Properties of 3d ui-elements.
    /// </summary>
    internal static class UI3DProperties
    {
        /// <summary>
        /// A plain color shader used for single-colored objects.
        /// </summary>
        internal const string PlainColorShaderName = "Unlit/PlainColorShader";

        /// <summary>
        /// The default alpha value used for various transparent ui-elements.
        /// </summary>
        internal const float DefaultAlpha = 0.5f;

        /// <summary>
        /// The default color of every 3d ui-element.
        /// </summary>
        internal static readonly Color DefaultColor = new(1.0f, 0.25f, 0.0f, DefaultAlpha);

        /// <summary>
        /// The secondary default color of every 3d ui-element.
        /// </summary>
        internal static readonly Color DefaultColorSecondary = new(1.0f, 0.75f, 0.0f, DefaultAlpha);

        /// <summary>
        /// The tertiary color of every 3d ui-element.
        /// </summary>
        internal static readonly Color DefaultColorTertiary = new(1.0f, 0.0f, 0.5f, DefaultAlpha);
    }
}