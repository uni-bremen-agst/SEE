using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Tests the geometry calculation of line caps.
    /// </summary>
    [TestFixture]
    public class TestLineCapPointsCalculator
    {
        /// <summary>
        /// Verifies that a line cap using its own visuals calculates its geometry
        /// from its own thickness instead of the thickness of the parent line.
        /// </summary>
        [Test]
        public void TestOwnVisualsUseCapThickness()
        {
            LineConf line = CreateLine(0.5f);
            LineCapConf cap = CreateCap(LineCap.Arrowhead, 0.1f);

            List<LineCapShape> shapes =
                GetShapes(cap, line, LineCapPosition.Start, true);

            Assert.That(shapes.Count, Is.EqualTo(1));
            Assert.That(shapes[0].ConnectionPoint.x, Is.EqualTo(-0.6f).Within(0.0001f));
        }

        /// <summary>
        /// Verifies that a line cap inheriting its visuals calculates its geometry
        /// from the thickness of the parent line.
        /// </summary>
        [Test]
        public void TestInheritedVisualsUseParentLineThickness()
        {
            LineConf line = CreateLine(0.5f);
            LineCapConf cap = CreateCap(LineCap.Arrowhead, 0.1f);

            List<LineCapShape> shapes =
                GetShapes(cap, line, LineCapPosition.Start, false);

            Assert.That(shapes.Count, Is.EqualTo(1));
            Assert.That(shapes[0].ConnectionPoint.x, Is.EqualTo(-3.0f).Within(0.0001f));
        }

        /// <summary>
        /// Verifies that changing the parent-line thickness does not affect the
        /// geometry of a cap that uses its own visual configuration.
        /// </summary>
        [Test]
        public void TestOwnVisualGeometryIsIndependentOfParentThickness()
        {
            LineConf thinLine = CreateLine(0.2f);
            LineConf thickLine = CreateLine(0.8f);

            LineCapConf cap = CreateCap(LineCap.Aggregation, 0.1f);

            List<LineCapShape> thinParentShapes =
                GetShapes(cap, thinLine, LineCapPosition.Start, true);

            List<LineCapShape> thickParentShapes =
                GetShapes(cap, thickLine, LineCapPosition.Start, true);

            Assert.That(thinParentShapes.Count, Is.EqualTo(thickParentShapes.Count));

            for (int shapeIndex = 0; shapeIndex < thinParentShapes.Count; shapeIndex++)
            {
                Assert.That(
                    thinParentShapes[shapeIndex].ConnectionPoint,
                    Is.EqualTo(thickParentShapes[shapeIndex].ConnectionPoint));

                Assert.That(
                    thinParentShapes[shapeIndex].Points.Length,
                    Is.EqualTo(thickParentShapes[shapeIndex].Points.Length));

                for (int pointIndex = 0;
                     pointIndex < thinParentShapes[shapeIndex].Points.Length;
                     pointIndex++)
                {
                    Assert.That(
                        thinParentShapes[shapeIndex].Points[pointIndex],
                        Is.EqualTo(thickParentShapes[shapeIndex].Points[pointIndex]));
                }
            }
        }

        /// <summary>
        /// Creates a parent-line configuration with a sufficiently long segment
        /// so that cap geometry is not limited by the segment length.
        /// </summary>
        /// <param name="thickness">The thickness of the parent line.</param>
        /// <returns>The created line configuration.</returns>
        private static LineConf CreateLine(float thickness)
        {
            return new LineConf
            {
                Thickness = thickness,
                RendererPositions = new[]
                {
                    Vector3.zero,
                    new Vector3(10.0f, 0.0f, 0.0f)
                }
            };
        }

        /// <summary>
        /// Creates a line-cap configuration with the given kind and thickness.
        /// </summary>
        /// <param name="capKind">The line-cap kind.</param>
        /// <param name="thickness">The thickness of the line cap.</param>
        /// <returns>The created line-cap configuration.</returns>
        private static LineCapConf CreateCap(LineCap capKind, float thickness)
        {
            return new LineCapConf
            {
                CapKind = capKind,
                Thickness = thickness
            };
        }
    }
}
