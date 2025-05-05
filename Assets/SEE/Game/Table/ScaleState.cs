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
        /// Determines the <see cref="ScaleState"/> based on the given <paramref name="scaleValue"/>.
        /// </summary>
        /// <param name="scaleValue">The value to evaluate.</param>
        /// <returns>The corresponding <see cref="ScaleState"/> based on the value of <paramref name="scaleValue"/>.</returns>
        public static ScaleState DetermineScale(float scaleValue)
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