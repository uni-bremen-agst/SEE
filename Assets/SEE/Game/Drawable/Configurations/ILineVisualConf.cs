using UnityEngine;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Defines the common visual properties shared by line-related configurations.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by configuration classes that describe the
    /// visual appearance of rendered lines or line caps.
    /// <para>
    /// It allows shared extraction logic to write common line-rendering properties
    /// into different configuration types without duplicating code.
    /// </para>
    /// </remarks>
    public interface ILineVisualConf
    {
        /// <summary>
        /// Gets or sets how the colors of the line are interpreted.
        /// </summary>
        ColorKind ColorKind { get; set; }

        /// <summary>
        /// Gets or sets the primary color of the line.
        /// </summary>
        Color PrimaryColor { get; set; }

        /// <summary>
        /// Gets or sets the secondary color of the line.
        /// </summary>
        /// <remarks>
        /// This value is interpreted depending on the <see cref="ColorKind"/>.
        /// </remarks>
        Color SecondaryColor { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the line.
        /// </summary>
        float Thickness { get; set; }

        /// <summary>
        /// Gets or sets the line style used to render the line.
        /// </summary>
        LineKind LineKind { get; set; }

        /// <summary>
        /// Gets or sets the tiling of a dashed line.
        /// </summary>
        /// <remarks>
        /// This value is only relevant for dashed line kinds.
        /// </remarks>
        float Tiling { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a fill-out is present.
        /// </summary>
        bool FillOutStatus { get; set; }

        /// <summary>
        /// Gets or sets the fill-out color.
        /// </summary>
        Color FillOutColor { get; set; }
    }
}
