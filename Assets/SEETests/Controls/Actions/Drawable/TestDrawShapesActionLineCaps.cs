using NUnit.Framework;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Tests line-cap-related preview state handling of <see cref="DrawShapesAction"/>.
    /// </summary>
    [TestFixture]
    public class TestDrawShapesActionLineCaps
    {
        /// <summary>
        /// Verifies that a normal preview update preserves a line kind that was
        /// changed through the configuration menu.
        /// </summary>
        [Test]
        public void TestNormalPreviewPreservesCurrentLineKind()
        {
            LineKind result = DrawShapesAction.ResolvePreviewLineKind(
                false,
                false,
                LineKind.Dashed25,
                LineKind.Solid);

            Assert.That(result, Is.EqualTo(LineKind.Dashed25));
        }

        /// <summary>
        /// Verifies that a reference cap requires the dashed-25 line kind.
        /// </summary>
        [Test]
        public void TestReferenceUsesDashed25()
        {
            LineKind result = DrawShapesAction.ResolvePreviewLineKind(
                true,
                false,
                LineKind.Dashed75,
                LineKind.Dashed75);

            Assert.That(result, Is.EqualTo(LineKind.Dashed25));
        }

        /// <summary>
        /// Verifies that removing the last reference cap restores the regular
        /// line kind configured for drawing.
        /// </summary>
        [Test]
        public void TestRemovingReferenceRestoresDrawingLineKind()
        {
            LineKind result = DrawShapesAction.ResolvePreviewLineKind(
                false,
                true,
                LineKind.Dashed25,
                LineKind.Dashed75);

            Assert.That(result, Is.EqualTo(LineKind.Dashed75));
        }
    }
}
