using SEE.Game.Drawable.ActionHelpers;
using System;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.UMLShapePointsCalculator;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Describes the configuration of a single value control in the shape menu.
    /// </summary>
    internal sealed class ShapeMenuValueLayout
    {
        /// <summary>
        /// Creates a value-control layout.
        /// </summary>
        /// <param name="identifier">
        /// The label shown for the value control.
        /// May be null if the existing label should be preserved.
        /// </param>
        /// <param name="defaultValue">
        /// The optional default value assigned to the control.
        /// </param>
        internal ShapeMenuValueLayout(
            string identifier,
            int? defaultValue = null)
        {
            Identifier = identifier;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the label shown for the value control.
        /// </summary>
        internal string Identifier { get; }

        /// <summary>
        /// Gets the optional default value of the control.
        /// </summary>
        internal int? DefaultValue { get; }
    }

    /// <summary>
    /// Describes which controls are visible and how they are configured
    /// for a selected shape.
    /// </summary>
    internal sealed class ShapeMenuLayoutRule
    {
        /// <summary>
        /// Configuration of the first value control.
        /// </summary>
        internal ShapeMenuValueLayout Value1 { get; set; }

        /// <summary>
        /// Configuration of the second value control.
        /// </summary>
        internal ShapeMenuValueLayout Value2 { get; set; }

        /// <summary>
        /// Configuration of the third value control.
        /// </summary>
        internal ShapeMenuValueLayout Value3 { get; set; }

        /// <summary>
        /// Configuration of the fourth value control.
        /// </summary>
        internal ShapeMenuValueLayout Value4 { get; set; }

        /// <summary>
        /// Configuration of the first angle control.
        /// </summary>
        internal ShapeMenuValueLayout Angle1 { get; set; }

        /// <summary>
        /// Configuration of the second angle control.
        /// </summary>
        internal ShapeMenuValueLayout Angle2 { get; set; }

        /// <summary>
        /// Configuration of the offset control.
        /// </summary>
        internal ShapeMenuValueLayout Offset { get; set; }

        /// <summary>
        /// Configuration of the vertices control.
        /// </summary>
        internal ShapeMenuValueLayout Vertices { get; set; }

        /// <summary>
        /// Whether the boolean control should be shown.
        /// </summary>
        internal bool ShowBool { get; set; }

        /// <summary>
        /// Label shown for the boolean control.
        /// </summary>
        internal string BoolIdentifier { get; set; }

        /// <summary>
        /// Whether the start line-cap control should be shown.
        /// </summary>
        internal bool ShowLineStart { get; set; }

        /// <summary>
        /// Whether the end line-cap control should be shown.
        /// </summary>
        internal bool ShowLineEnd { get; set; }

        /// <summary>
        /// Whether the finish control should be shown.
        /// </summary>
        internal bool ShowFinish { get; set; }

        /// <summary>
        /// Whether the information control should be shown.
        /// </summary>
        internal bool ShowInfo { get; set; }

        /// <summary>
        /// Whether the UML-shape selector should be shown.
        /// </summary>
        internal bool ShowUMLSelector { get; set; }

        /// <summary>
        /// Whether the orientation selector should be shown.
        /// </summary>
        internal bool ShowOrientation { get; set; }

        /// <summary>
        /// Optional default orientation to select.
        /// </summary>
        internal Orientation? DefaultOrientation { get; set; }

        /// <summary>
        /// Whether the boolean control should be moved directly
        /// before the finish control.
        /// </summary>
        internal bool MoveBoolToLinePosition { get; set; }
    }

    /// <summary>
    /// Provides the shape and UML-specific layout rules used by the shape menu.
    /// </summary>
    internal static class ShapeMenuLayoutRules
    {
        /// <summary>
        /// Returns the layout rule for the selected shape.
        /// </summary>
        /// <param name="shape">The selected shape.</param>
        /// <param name="umlShape">
        /// The selected UML shape if <paramref name="shape"/> is
        /// <see cref="Shape.UML"/>.
        /// </param>
        /// <returns>The corresponding layout rule.</returns>
        /// <exception cref="NotImplementedException">
        /// Thrown if the selected shape has no layout rule.
        /// </exception>
        internal static ShapeMenuLayoutRule Get(
            Shape shape,
            UMLShape umlShape)
        {
            switch (shape)
            {
                case Shape.Line:
                    return new ShapeMenuLayoutRule
                    {
                        ShowBool = true,
                        BoolIdentifier = "Loop",
                        ShowLineStart = true,
                        ShowLineEnd = true,
                        ShowFinish = true,
                        MoveBoolToLinePosition = true
                    };

                case Shape.Square:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("a"),
                        ShowInfo = true
                    };

                case Shape.Rectangle:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("a"),
                        Value2 = new ShapeMenuValueLayout("b"),
                        ShowInfo = true
                    };

                case Shape.Rhombus:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("f"),
                        Value2 = new ShapeMenuValueLayout("e"),
                        ShowInfo = true
                    };

                case Shape.Kite:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("f1"),
                        Value2 = new ShapeMenuValueLayout("f2"),
                        Value3 = new ShapeMenuValueLayout("e"),
                        ShowInfo = true
                    };

                case Shape.Triangle:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("c"),
                        Value2 = new ShapeMenuValueLayout("h"),
                        ShowInfo = true
                    };

                case Shape.Circle:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("Radius"),
                        ShowInfo = true
                    };

                case Shape.HalfCircle:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("Radius"),
                        ShowOrientation = true
                    };

                case Shape.Ellipse:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("X-Scale"),
                        Value2 = new ShapeMenuValueLayout("Y-Scale"),
                        ShowInfo = true
                    };

                case Shape.Parallelogram:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("a"),
                        Value2 = new ShapeMenuValueLayout("h"),
                        Offset = new ShapeMenuValueLayout("Shift"),
                        ShowInfo = true
                    };

                case Shape.Trapezoid:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("a"),
                        Value2 = new ShapeMenuValueLayout("c"),
                        Value3 = new ShapeMenuValueLayout("h"),
                        ShowInfo = true
                    };

                case Shape.Polygon:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("Length"),
                        Vertices = new ShapeMenuValueLayout(null),
                        ShowInfo = true
                    };

                case Shape.Arc:
                    return new ShapeMenuLayoutRule
                    {
                        Value1 = new ShapeMenuValueLayout("Radius"),
                        Angle1 = new ShapeMenuValueLayout("Start Angle"),
                        Angle2 = new ShapeMenuValueLayout(
                            "End Angle",
                            360),
                        Vertices = new ShapeMenuValueLayout(
                            "Verticies",
                            PointsCalculator.DefaultVertices)
                    };

                case Shape.UML:
                    return GetUMLRule(umlShape);

                default:
                    throw new NotImplementedException(
                        $"The selected shape {shape} has not been integrated yet.");
            }
        }

        /// <summary>
        /// Returns the layout rule for the selected UML shape.
        /// </summary>
        /// <param name="umlShape">The selected UML shape.</param>
        /// <returns>The corresponding layout rule.</returns>
        /// <exception cref="NotImplementedException">
        /// Thrown if the selected UML shape has no layout rule.
        /// </exception>
        private static ShapeMenuLayoutRule GetUMLRule(
            UMLShape umlShape)
        {
            switch (umlShape)
            {
                case UMLShape.Actor:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "Length",
                            10)
                    };

                case UMLShape.Note:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "a",
                            30),
                        Value2 = new ShapeMenuValueLayout(
                            "b",
                            20)
                    };

                case UMLShape.Package:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "a",
                            30),
                        Value2 = new ShapeMenuValueLayout(
                            "b",
                            20),
                        Value3 = new ShapeMenuValueLayout(
                            "Title-Width",
                            15),
                        Value4 = new ShapeMenuValueLayout(
                            "Title-Height")
                    };

                case UMLShape.ProvideInterf:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "Radius",
                            10),
                        ShowOrientation = true,
                        DefaultOrientation = Orientation.Left
                    };

                case UMLShape.ReceiveInterf:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "Radius",
                            10),
                        ShowOrientation = true,
                        DefaultOrientation = Orientation.Right
                    };

                case UMLShape.SendActivity:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "a",
                            20),
                        Value2 = new ShapeMenuValueLayout(
                            "b",
                            10),
                        ShowOrientation = true,
                        DefaultOrientation = Orientation.Right
                    };

                case UMLShape.ReceiveActivity:
                    return new ShapeMenuLayoutRule
                    {
                        ShowUMLSelector = true,
                        Value1 = new ShapeMenuValueLayout(
                            "a",
                            20),
                        Value2 = new ShapeMenuValueLayout(
                            "b",
                            10),
                        ShowOrientation = true,
                        DefaultOrientation = Orientation.Left
                    };

                default:
                    throw new NotImplementedException(
                        $"The selected UML shape {umlShape} has not been integrated yet.");
            }
        }
    }
}
