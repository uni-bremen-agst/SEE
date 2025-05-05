using UnityEngine;

namespace SEE.Game.Table
{
    /// <summary>
    /// The scale states.
    /// </summary>
    public enum ScaleState
    {
        NotScaled,
        Increased,
        Decreased
    }

    /// <summary>
    /// Determines the scale state based on a given scale value.
    /// </summary>
    public static class ScaleDeterminer
    {
        /// <summary>
        /// Determines the <see cref="ScaleState"/> of a parent transform
        /// based on the child's adjusted <paramref name="scaleValue"/>.
        /// </summary>
        /// <param name="scaleValue">The local scale of a child transform that compensates for its parent's scale.</param>
        /// <returns>The inferred <see cref="ScaleState"/> of the parent: increased if the child was scaled down,
        /// decreased if the child was scaled up, or not scaled if the child scale is approximately 1.</returns>
        public static ScaleState DetermineInverseScale(float scaleValue)
        {
            if (scaleValue < 1f && !Mathf.Approximately(scaleValue, 1f))
            {
                return ScaleState.Increased;
            }
            else if (scaleValue > 1f && !Mathf.Approximately(scaleValue, 1f))
            {
                return ScaleState.Decreased;
            }
            else
            {
                return ScaleState.NotScaled;
            }
        }
    }
}