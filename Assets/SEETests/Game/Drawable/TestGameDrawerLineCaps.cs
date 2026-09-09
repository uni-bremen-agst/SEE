using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// Tests creation and initialization of line-cap configurations.
    /// </summary>
    [TestFixture]
    public class TestGameDrawerLineCaps
    {
        /// <summary>
        /// Verifies that a new line cap without its own visual configuration
        /// inherits the complete visual style of its parent line.
        /// </summary>
        [Test]
        public void TestNewCapInheritsMainLineVisuals()
        {
            LineConf line = new()
            {
                ColorKind = ColorKind.TwoDashed,
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                Thickness = 0.75f,
                LineKind = LineKind.Dashed25,
                Tiling = 4.0f
            };

            LineCapConf cap = GameDrawer.CreateLineCapConf(
                line,
                LineCapConf.CreateNone(),
                LineCap.Arrowhead);

            Assert.That(cap.CapKind, Is.EqualTo(LineCap.Arrowhead));
            Assert.That(cap.ColorKind, Is.EqualTo(ColorKind.TwoDashed));
            Assert.That(cap.PrimaryColor, Is.EqualTo(Color.red));
            Assert.That(cap.SecondaryColor, Is.EqualTo(Color.blue));
            Assert.That(cap.Thickness, Is.EqualTo(0.75f));
            Assert.That(cap.LineKind, Is.EqualTo(LineKind.Dashed25));
            Assert.That(cap.Tiling, Is.EqualTo(4.0f));
            Assert.That(cap.UseOwnVisuals, Is.False);
        }
    }
}
