using System;
using UnityEngine;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Represents the configuration of a line cap (decoration at the start or end of a line).
    /// A line cap defines both the type of the cap and its visual appearance.
    /// </summary>
    [Serializable]
    public class LineCapConf : ICloneable
    {
        /// <summary>
        /// The type of the line cap.
        /// Determines the geometric shape of the cap.
        /// </summary>
        public LineCapPointsCalculator.LineCap CapKind;

        /// <summary>
        /// Specifies how the colors of the cap are interpreted.
        /// </summary>
        public ColorKind ColorKind;

        /// <summary>
        /// The primary color of the cap.
        /// </summary>
        public Color PrimaryColor;

        /// <summary>
        /// The secondary color of the cap.
        /// Used depending on the <see cref="ColorKind"/>.
        /// </summary>
        public Color SecondaryColor;

        /// <summary>
        /// The thickness of the cap lines.
        /// </summary>
        public float Thickness;

        /// <summary>
        /// The line style used to render the cap.
        /// </summary>
        public LineKind LineKind;

        /// <summary>
        /// Specifies how often the texture is repeated along the cap line.
        /// </summary>
        public int Tiling;

        /// <summary>
        /// Indicates whether the cap should be filled out.
        /// </summary>
        public bool FillOutStatus;

        /// <summary>
        /// The fill-out color of the cap.
        /// </summary>
        public Color FillOutColor;

        /// <summary>
        /// Creates a deep copy of this <see cref="LineCapConf"/>.
        /// </summary>
        /// <returns>A cloned instance of this <see cref="LineCapConf"/>.</returns>
        public object Clone()
        {
            LineCapConf clone = new LineCapConf
            {
                CapKind = this.CapKind,
                ColorKind = this.ColorKind,
                PrimaryColor = this.PrimaryColor,
                SecondaryColor = this.SecondaryColor,
                Thickness = this.Thickness,
                LineKind = this.LineKind,
                Tiling = this.Tiling,
                FillOutStatus = this.FillOutStatus,
                FillOutColor = this.FillOutColor
            };

            return clone;
        }
    }
}