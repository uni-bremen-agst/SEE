using NUnit.Framework;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Tests color-kind validation while editing lines and line caps.
    /// </summary>
    [TestFixture]
    public class TestEditLineColorMenu
    {
        /// <summary>
        /// Verifies that two-dashed coloring is skipped for a solid line when
        /// monochrome is currently selected.
        /// </summary>
        [Test]
        public void TestSolidSkipsTwoDashedFromMonochrome()
        {
            ColorKind result = EditLineColorMenu.GetValidColorKind(
                ColorKind.TwoDashed,
                ColorKind.Monochrome,
                LineKind.Solid);

            Assert.That(result, Is.EqualTo(ColorKind.Gradient));
        }

        /// <summary>
        /// Verifies that two-dashed coloring is skipped for a solid line when
        /// gradient is currently selected.
        /// </summary>
        [Test]
        public void TestSolidSkipsTwoDashedFromGradient()
        {
            ColorKind result = EditLineColorMenu.GetValidColorKind(
                ColorKind.TwoDashed,
                ColorKind.Gradient,
                LineKind.Solid);

            Assert.That(result, Is.EqualTo(ColorKind.Monochrome));
        }

        /// <summary>
        /// Verifies that gradient coloring remains valid for a solid line.
        /// </summary>
        [Test]
        public void TestSolidAllowsGradient()
        {
            ColorKind result = EditLineColorMenu.GetValidColorKind(
                ColorKind.Gradient,
                ColorKind.Monochrome,
                LineKind.Solid);

            Assert.That(result, Is.EqualTo(ColorKind.Gradient));
        }

        /// <summary>
        /// Verifies that two-dashed coloring remains valid for a dashed line kind.
        /// </summary>
        [Test]
        public void TestDashedAllowsTwoDashed()
        {
            ColorKind result = EditLineColorMenu.GetValidColorKind(
                ColorKind.TwoDashed,
                ColorKind.Monochrome,
                LineKind.Dashed25);

            Assert.That(result, Is.EqualTo(ColorKind.TwoDashed));
        }
    }
}
