using NUnit.Framework;
using SEE.Game.Drawable.ActionHelpers;
using System;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.UMLShapePointsCalculator;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Tests the shape and UML layout rules used by the shape menu.
    /// </summary>
    [TestFixture]
    public class TestShapeMenuLayout
    {
        /// <summary>
        /// Verifies that every supported shape has a layout rule.
        /// </summary>
        [Test]
        public void TestEveryShapeHasLayoutRule()
        {
            foreach (Shape shape in Enum.GetValues(typeof(Shape)))
            {
                Assert.DoesNotThrow(
                    () => ShapeMenuLayoutRules.Get(
                        shape,
                        UMLShape.Actor),
                    $"Missing layout rule for shape {shape}.");
            }
        }

        /// <summary>
        /// Verifies that every supported UML shape has a layout rule.
        /// </summary>
        [Test]
        public void TestEveryUMLShapeHasLayoutRule()
        {
            foreach (UMLShape umlShape in Enum.GetValues(typeof(UMLShape)))
            {
                Assert.DoesNotThrow(
                    () => ShapeMenuLayoutRules.Get(
                        Shape.UML,
                        umlShape),
                    $"Missing layout rule for UML shape {umlShape}.");
            }
        }

        /// <summary>
        /// Verifies the controls required for drawing a line.
        /// </summary>
        [Test]
        public void TestLineLayout()
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.Line,
                    UMLShape.Actor);

            Assert.That(rule.ShowBool, Is.True);
            Assert.That(rule.BoolIdentifier, Is.EqualTo("Loop"));
            Assert.That(rule.ShowLineStart, Is.True);
            Assert.That(rule.ShowLineEnd, Is.True);
            Assert.That(rule.ShowFinish, Is.True);
            Assert.That(rule.MoveBoolToLinePosition, Is.True);

            Assert.That(rule.Value1, Is.Null);
            Assert.That(rule.ShowInfo, Is.False);
            Assert.That(rule.ShowOrientation, Is.False);
            Assert.That(rule.ShowUMLSelector, Is.False);
        }

        /// <summary>
        /// Verifies the layout of a rectangle.
        /// </summary>
        [Test]
        public void TestRectangleLayout()
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.Rectangle,
                    UMLShape.Actor);

            AssertValueLayout(
                rule.Value1,
                "a",
                null);

            AssertValueLayout(
                rule.Value2,
                "b",
                null);

            Assert.That(rule.Value3, Is.Null);
            Assert.That(rule.Value4, Is.Null);
            Assert.That(rule.ShowInfo, Is.True);
            Assert.That(rule.ShowOrientation, Is.False);
        }

        /// <summary>
        /// Verifies the radius and orientation configuration
        /// of a half circle.
        /// </summary>
        [Test]
        public void TestHalfCircleLayout()
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.HalfCircle,
                    UMLShape.Actor);

            AssertValueLayout(
                rule.Value1,
                "Radius",
                null);

            Assert.That(rule.ShowOrientation, Is.True);
            Assert.That(rule.DefaultOrientation, Is.Null);
            Assert.That(rule.ShowInfo, Is.False);
        }

        /// <summary>
        /// Verifies the labels and default values of an arc.
        /// </summary>
        [Test]
        public void TestArcLayout()
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.Arc,
                    UMLShape.Actor);

            AssertValueLayout(
                rule.Value1,
                "Radius",
                null);

            AssertValueLayout(
                rule.Angle1,
                "Start Angle",
                null);

            AssertValueLayout(
                rule.Angle2,
                "End Angle",
                360);

            AssertValueLayout(
                rule.Vertices,
                "Verticies",
                PointsCalculator.DefaultVertices);

            Assert.That(rule.ShowInfo, Is.False);
            Assert.That(rule.ShowOrientation, Is.False);
        }

        /// <summary>
        /// Verifies the configuration of a UML actor.
        /// </summary>
        [Test]
        public void TestUMLActorLayout()
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.UML,
                    UMLShape.Actor);

            Assert.That(rule.ShowUMLSelector, Is.True);

            AssertValueLayout(
                rule.Value1,
                "Length",
                10);

            Assert.That(rule.Value2, Is.Null);
            Assert.That(rule.ShowOrientation, Is.False);
        }

        /// <summary>
        /// Verifies the configuration of a UML package.
        /// </summary>
        [Test]
        public void TestUMLPackageLayout()
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.UML,
                    UMLShape.Package);

            Assert.That(rule.ShowUMLSelector, Is.True);

            AssertValueLayout(
                rule.Value1,
                "a",
                30);

            AssertValueLayout(
                rule.Value2,
                "b",
                20);

            AssertValueLayout(
                rule.Value3,
                "Title-Width",
                15);

            AssertValueLayout(
                rule.Value4,
                "Title-Height",
                null);
        }

        /// <summary>
        /// Verifies the predefined orientation of UML shapes
        /// whose geometry depends on orientation.
        /// </summary>
        /// <param name="umlShape">
        /// The UML shape to verify.
        /// </param>
        /// <param name="expectedOrientation">
        /// The expected default orientation.
        /// </param>
        [TestCase(
            UMLShape.ProvideInterf,
            Orientation.Left)]
        [TestCase(
            UMLShape.ReceiveInterf,
            Orientation.Right)]
        [TestCase(
            UMLShape.SendActivity,
            Orientation.Right)]
        [TestCase(
            UMLShape.ReceiveActivity,
            Orientation.Left)]
        public void TestUMLOrientationLayout(
            UMLShape umlShape,
            Orientation expectedOrientation)
        {
            ShapeMenuLayoutRule rule =
                ShapeMenuLayoutRules.Get(
                    Shape.UML,
                    umlShape);

            Assert.That(rule.ShowUMLSelector, Is.True);
            Assert.That(rule.ShowOrientation, Is.True);
            Assert.That(
                rule.DefaultOrientation,
                Is.EqualTo(expectedOrientation));
        }

        /// <summary>
        /// Verifies a value-control layout.
        /// </summary>
        /// <param name="layout">
        /// The value-control layout to verify.
        /// </param>
        /// <param name="expectedIdentifier">
        /// The expected identifier.
        /// </param>
        /// <param name="expectedDefaultValue">
        /// The expected optional default value.
        /// </param>
        private static void AssertValueLayout(
            ShapeMenuValueLayout layout,
            string expectedIdentifier,
            int? expectedDefaultValue)
        {
            Assert.That(layout, Is.Not.Null);

            Assert.That(
                layout.Identifier,
                Is.EqualTo(expectedIdentifier));

            Assert.That(
                layout.DefaultValue,
                Is.EqualTo(expectedDefaultValue));
        }
    }
}
