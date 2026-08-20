using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Provides helper methods for transferring shared visual line data from
    /// Unity objects into configuration objects.
    /// </summary>
    internal static class LineVisualConfFactory
    {
        /// <summary>
        /// Reads the shared visual properties of the given <paramref name="source"/>
        /// and writes them into <paramref name="target"/>.
        /// </summary>
        /// <param name="source">
        /// The game object whose visual line properties should be read.
        /// </param>
        /// <param name="renderer">
        /// The line renderer attached to <paramref name="source"/>.
        /// </param>
        /// <param name="target">
        /// The target configuration object receiving the extracted values.
        /// </param>
        /// <remarks>
        /// This method extracts all properties shared by <see cref="LineConf"/>
        /// and <see cref="LineCapConf"/>, such as thickness, line kind,
        /// color kind, primary and secondary colors, tiling, and fill-out data.
        /// <para>
        /// The caller is responsible for validating the object and ensuring that
        /// the required components are present.
        /// </para>
        /// </remarks>
        public static void ApplyVisualProperties(GameObject source, LineRenderer renderer, ILineVisualConf target)
        {
            GameObject fillout = GameDrawer.GetOwnFillOutObject(source);
            LineValueHolder lineValueHolder = source.GetComponent<LineValueHolder>();

            target.ColorKind = lineValueHolder.ColorKind;
            target.LineKind = lineValueHolder.LineKind;
            target.Thickness = renderer.startWidth;
            target.Tiling = renderer.textureScale.x;
            target.FillOutStatus = fillout != null;
            target.FillOutColor = fillout != null ? fillout.GetColor() : Color.clear;

            ApplyColors(renderer, target);
        }

        /// <summary>
        /// Determines the primary and secondary colors from the given
        /// <paramref name="renderer"/> and writes them into <paramref name="target"/>.
        /// </summary>
        /// <param name="renderer">
        /// The line renderer from which the colors are read.
        /// </param>
        /// <param name="target">
        /// The target configuration object receiving the color values.
        /// </param>
        private static void ApplyColors(LineRenderer renderer, ILineVisualConf target)
        {
            switch (target.ColorKind)
            {
                case GameDrawer.ColorKind.Monochrome:
                    target.PrimaryColor = renderer.material.color;
                    target.SecondaryColor = Color.clear;
                    break;
                case GameDrawer.ColorKind.Gradient:
                    target.PrimaryColor = renderer.startColor;
                    target.SecondaryColor = renderer.endColor;
                    break;
                case GameDrawer.ColorKind.TwoDashed:
                    target.PrimaryColor = renderer.materials[0].color;
                    target.SecondaryColor = renderer.materials[1].color;
                    break;
                default:
                    target.PrimaryColor = Color.clear;
                    target.SecondaryColor = Color.clear;
                    break;
            }
        }
    }
}
