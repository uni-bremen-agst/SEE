using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="UnityEngine.GameObject"/>
    /// regarding color.
    /// </summary>
    internal static class GameObjectColorExtensions
    {
        /// <summary>
        /// Sets the color for this <paramref name="gameObject"/> to given <paramref name="color"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> has a renderer whose material has a color attribute.
        /// </summary>
        /// <seealso cref="Material.color"/>
        /// <param name="gameObject">Object whose color is to be set.</param>
        /// <param name="color">The new color to be set.</param>
        public static void SetColor(this GameObject gameObject, Color color)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial.color = color;
            }
        }

        /// <summary>
        /// Retrieves the color from this <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> has a renderer whose material has a color attribute.
        /// </summary>
        /// <param name="gameObject">Object whose color is to be returned.</param>
        /// <returns>Color of this <paramref name="gameObject"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// If this <paramref name="gameObject"/> has no renderer attached to it.
        /// </exception>
        public static Color GetColor(this GameObject gameObject)
        {
            return gameObject.MustGetComponent<Renderer>().sharedMaterial.color;
        }

        /// <summary>
        /// Sets the alpha value (transparency) of the given <paramref name="gameObject"/>
        /// to <paramref name="alpha"/>.
        /// </summary>
        /// <param name="gameObject">Game objects whose transparency is to be set.</param>
        /// <param name="alpha">A value in between 0 and 1 for transparency.</param>
        public static void SetTransparency(this GameObject gameObject, float alpha)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                Color oldColor = renderer.material.color;
                renderer.material.color = oldColor.WithAlpha(alpha);
            }
        }

        /// <summary>
        /// Sets the start and end line color of <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a line renderer.
        /// </summary>
        /// <param name="gameObject">Object holding a line renderer whose start and end color is to be set.</param>
        /// <param name="startColor">Start color of the line.</param>
        /// <param name="endColor">End color of the line.</param>
        public static void SetLineColor(this GameObject gameObject, Color startColor, Color endColor)
        {
            if (gameObject.TryGetComponent(out LineRenderer renderer))
            {
                renderer.startColor = startColor;
                renderer.endColor = endColor;
            }
        }
    }
}
