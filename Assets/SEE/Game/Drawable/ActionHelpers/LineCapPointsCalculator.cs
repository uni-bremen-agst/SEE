using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Provides point calculations for line caps (decorations at the start or end of a line).
    /// Line caps are geometric shapes such as arrowheads or diamonds that are attached
    /// to a line based on its direction.
    /// </summary>
    public static class LineCapPointsCalculator
    {
        /// <summary>
        /// Defines the available types of line caps.
        /// </summary>
        public enum LineCap
        {
            None,
            Arrowhead,
            Aggregation,
            Composition,
            Circle
        }

        /// <summary>
        /// Gets a list with all line caps.
        /// </summary>
        /// <returns>A list that holds all line caps.</returns>
        public static List<LineCap> GetLineCaps()
        {
            return Enum.GetValues(typeof(LineCap)).Cast<LineCap>().ToList();
        }
    }
}