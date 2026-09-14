using SEE.Game.Drawable.Configurations;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.UMLShapePointsCalculator;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Holds the current shape and UML shape configuration selected in the shape menu.
    /// </summary>
    internal sealed class ShapeMenuState
    {
        /// <summary>
        /// The currently selected shape.
        /// </summary>
        internal Shape SelectedShape { get; set; }

        /// <summary>
        /// The currently selected UML shape.
        /// </summary>
        internal UMLShape SelectedUMLShape { get; set; }

        /// <summary>
        /// The first shape-specific value.
        /// </summary>
        internal float Value1 { get; set; }

        /// <summary>
        /// The second shape-specific value.
        /// </summary>
        internal float Value2 { get; set; }

        /// <summary>
        /// The third shape-specific value.
        /// </summary>
        internal float Value3 { get; set; }

        /// <summary>
        /// The fourth shape-specific value.
        /// </summary>
        internal float Value4 { get; set; }

        /// <summary>
        /// The first shape-specific angle.
        /// </summary>
        internal float Angle1 { get; set; }

        /// <summary>
        /// The second shape-specific angle.
        /// </summary>
        internal float Angle2 { get; set; }

        /// <summary>
        /// The shape-specific offset.
        /// </summary>
        internal float Offset { get; set; }

        /// <summary>
        /// The number of vertices used for applicable shapes.
        /// </summary>
        internal int Vertices { get; set; }

        /// <summary>
        /// The currently selected orientation.
        /// </summary>
        internal Orientation Orientation { get; set; } = Orientation.Up;

        /// <summary>
        /// The currently selected start line-cap configuration.
        /// </summary>
        private LineCapConf lineStartCapConf = LineCapConf.CreateNone();

        /// <summary>
        /// The currently selected end line-cap configuration.
        /// </summary>
        private LineCapConf lineEndCapConf = LineCapConf.CreateNone();

        /// <summary>
        /// Returns a copy of the current start line-cap configuration.
        /// </summary>
        /// <returns>The current start line-cap configuration.</returns>
        internal LineCapConf GetLineStartCapConf()
        {
            return lineStartCapConf.Clone();
        }

        /// <summary>
        /// Returns a copy of the current end line-cap configuration.
        /// </summary>
        /// <returns>The current end line-cap configuration.</returns>
        internal LineCapConf GetLineEndCapConf()
        {
            return lineEndCapConf.Clone();
        }

        /// <summary>
        /// Returns the current start line-cap kind.
        /// </summary>
        /// <returns>The current start line-cap kind.</returns>
        internal LineCap GetLineStartCap()
        {
            return lineStartCapConf.CapKind;
        }

        /// <summary>
        /// Returns the current end line-cap kind.
        /// </summary>
        /// <returns>The current end line-cap kind.</returns>
        internal LineCap GetLineEndCap()
        {
            return lineEndCapConf.CapKind;
        }

        /// <summary>
        /// Sets the start line-cap configuration.
        /// </summary>
        /// <param name="configuration">The new configuration.</param>
        internal void SetLineStartCap(LineCapConf configuration)
        {
            lineStartCapConf = configuration != null
                ? configuration.Clone()
                : LineCapConf.CreateNone();
        }

        /// <summary>
        /// Sets the end line-cap configuration.
        /// </summary>
        /// <param name="configuration">The new configuration.</param>
        internal void SetLineEndCap(LineCapConf configuration)
        {
            lineEndCapConf = configuration != null
                ? configuration.Clone()
                : LineCapConf.CreateNone();
        }
    }
}
