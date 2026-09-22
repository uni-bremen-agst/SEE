using System;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// The available color modes of a drawable line.
    /// </summary>
    [Serializable]
    public enum ColorKind
    {
        /// <summary>
        /// Uses a single primary color for the entire line.
        /// </summary>
        Monochrome,

        /// <summary>
        /// Creates a color transition from the primary color to the secondary color.
        /// </summary>
        Gradient,

        /// <summary>
        /// Uses the primary and secondary colors for a two-colored dashed line.
        /// </summary>
        TwoDashed
    }
}
