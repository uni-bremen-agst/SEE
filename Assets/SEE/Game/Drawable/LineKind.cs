using System;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// The available visual styles of a drawable line.
    /// </summary>
    [Serializable]
    public enum LineKind
    {
        /// <summary>
        /// A continuous solid line without a dash pattern.
        /// </summary>
        Solid,

        /// <summary>
        /// A dashed line whose texture tiling can be configured individually.
        /// </summary>
        Dashed,

        /// <summary>
        /// A dashed line using the predefined 25 dash-pattern variant.
        /// </summary>
        Dashed25,

        /// <summary>
        /// A dashed line using the predefined 50 dash-pattern variant.
        /// </summary>
        Dashed50,

        /// <summary>
        /// A dashed line using the predefined 75 dash-pattern variant.
        /// </summary>
        Dashed75,

        /// <summary>
        /// A dashed line using the predefined 100 dash-pattern variant.
        /// </summary>
        Dashed100
    }
}
