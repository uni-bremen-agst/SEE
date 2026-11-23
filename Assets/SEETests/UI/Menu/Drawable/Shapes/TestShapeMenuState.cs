using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Tests the state used by the shape menu.
    /// </summary>
    [TestFixture]
    public class TestShapeMenuState
    {
        /// <summary>
        /// Verifies that a newly created state uses the expected default configuration.
        /// </summary>
        [Test]
        public void NewStateUsesExpectedDefaults()
        {
            ShapeMenuState state = new();

            Assert.That(state.SelectedShape, Is.EqualTo(Shape.Line));
            Assert.That(state.Orientation, Is.EqualTo(Orientation.Up));
            Assert.That(state.GetLineStartCap(), Is.EqualTo(LineCap.None));
            Assert.That(state.GetLineEndCap(), Is.EqualTo(LineCap.None));
        }

        /// <summary>
        /// Verifies that setting the start line cap stores an independent copy.
        /// </summary>
        [Test]
        public void SetLineStartCapStoresIndependentCopy()
        {
            ShapeMenuState state = new();
            LineCapConf configuration = LineCapConf.CreateNone();
            configuration.CapKind = LineCap.Arrowhead;
            configuration.Thickness = 2.0f;

            state.SetLineStartCap(configuration);

            configuration.CapKind = LineCap.None;
            configuration.Thickness = 5.0f;

            LineCapConf storedConfiguration = state.GetLineStartCapConf();

            Assert.That(storedConfiguration.CapKind, Is.EqualTo(LineCap.Arrowhead));
            Assert.That(storedConfiguration.Thickness, Is.EqualTo(2.0f));
        }

        /// <summary>
        /// Verifies that setting the end line cap stores an independent copy.
        /// </summary>
        [Test]
        public void SetLineEndCapStoresIndependentCopy()
        {
            ShapeMenuState state = new();
            LineCapConf configuration = LineCapConf.CreateNone();
            configuration.CapKind = LineCap.Composition;
            configuration.Thickness = 3.0f;

            state.SetLineEndCap(configuration);

            configuration.CapKind = LineCap.None;
            configuration.Thickness = 6.0f;

            LineCapConf storedConfiguration = state.GetLineEndCapConf();

            Assert.That(storedConfiguration.CapKind, Is.EqualTo(LineCap.Composition));
            Assert.That(storedConfiguration.Thickness, Is.EqualTo(3.0f));
        }

        /// <summary>
        /// Verifies that retrieving the start line cap returns an independent copy.
        /// </summary>
        [Test]
        public void GetLineStartCapConfReturnsIndependentCopy()
        {
            ShapeMenuState state = new();
            LineCapConf configuration = LineCapConf.CreateNone();
            configuration.CapKind = LineCap.Arrow;

            state.SetLineStartCap(configuration);

            LineCapConf returnedConfiguration = state.GetLineStartCapConf();
            returnedConfiguration.CapKind = LineCap.None;

            Assert.That(state.GetLineStartCap(), Is.EqualTo(LineCap.Arrow));
        }

        /// <summary>
        /// Verifies that retrieving the end line cap returns an independent copy.
        /// </summary>
        [Test]
        public void GetLineEndCapConfReturnsIndependentCopy()
        {
            ShapeMenuState state = new();
            LineCapConf configuration = LineCapConf.CreateNone();
            configuration.CapKind = LineCap.Aggregation;

            state.SetLineEndCap(configuration);

            LineCapConf returnedConfiguration = state.GetLineEndCapConf();
            returnedConfiguration.CapKind = LineCap.None;

            Assert.That(state.GetLineEndCap(), Is.EqualTo(LineCap.Aggregation));
        }
    }
}
