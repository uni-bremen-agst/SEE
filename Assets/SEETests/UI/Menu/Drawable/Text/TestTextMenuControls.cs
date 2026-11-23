using NUnit.Framework;
using UnityEngine;

namespace SEE.UI.Menu.Drawable.Text
{
    /// <summary>
    /// Tests the UI-reference resolution performed by
    /// <see cref="TextMenuControls"/>.
    /// </summary>
    [TestFixture]
    public class TestTextMenuControls
    {
        /// <summary>
        /// The resource path of the text-menu prefab used by the production menu.
        /// </summary>
        private const string TextMenuPrefab =
            "Prefabs/UI/Drawable/TextMenu";

        /// <summary>
        /// The instantiated text-menu object used by the current test.
        /// </summary>
        private GameObject textMenu;

        /// <summary>
        /// Creates a fresh text-menu prefab instance before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            GameObject prefab =
                Resources.Load<GameObject>(TextMenuPrefab);

            Assert.That(
                prefab,
                Is.Not.Null,
                $"Could not load text-menu prefab at {TextMenuPrefab}.");

            textMenu = Object.Instantiate(prefab);
        }

        /// <summary>
        /// Destroys the instantiated text-menu object after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (textMenu != null)
            {
                Object.DestroyImmediate(textMenu);
            }
        }

        /// <summary>
        /// Verifies that all controls required by the text menu can be
        /// resolved from the production text-menu prefab.
        /// </summary>
        [Test]
        public void TestAllControlsAreResolved()
        {
            TextMenuControls controls =
                new(textMenu);

            Assert.That(controls.MenuObject, Is.SameAs(textMenu));

            Assert.That(controls.BoldButton, Is.Not.Null);
            Assert.That(controls.BoldButtonManager, Is.Not.Null);

            Assert.That(controls.ItalicButton, Is.Not.Null);
            Assert.That(controls.ItalicButtonManager, Is.Not.Null);

            Assert.That(controls.UnderlineButton, Is.Not.Null);
            Assert.That(controls.UnderlineButtonManager, Is.Not.Null);

            Assert.That(controls.StrikethroughButton, Is.Not.Null);
            Assert.That(
                controls.StrikethroughButtonManager,
                Is.Not.Null);

            Assert.That(controls.LowerCaseButton, Is.Not.Null);
            Assert.That(
                controls.LowerCaseButtonManager,
                Is.Not.Null);

            Assert.That(controls.UpperCaseButton, Is.Not.Null);
            Assert.That(
                controls.UpperCaseButtonManager,
                Is.Not.Null);

            Assert.That(controls.SmallCapsButton, Is.Not.Null);
            Assert.That(
                controls.SmallCapsButtonManager,
                Is.Not.Null);

            Assert.That(controls.FontColorButton, Is.Not.Null);
            Assert.That(
                controls.FontColorButtonManager,
                Is.Not.Null);

            Assert.That(controls.OutlineColorButton, Is.Not.Null);
            Assert.That(
                controls.OutlineColorButtonManager,
                Is.Not.Null);

            Assert.That(controls.OutlineObject, Is.Not.Null);
            Assert.That(controls.OutlineSwitch, Is.Not.Null);

            Assert.That(controls.ThicknessObject, Is.Not.Null);
            Assert.That(controls.ThicknessSlider, Is.Not.Null);

            Assert.That(controls.FontSizeInput, Is.Not.Null);

            Assert.That(controls.EditTextObject, Is.Not.Null);
            Assert.That(
                controls.EditTextButtonManager,
                Is.Not.Null);

            Assert.That(controls.OrderInLayerObject, Is.Not.Null);
            Assert.That(
                controls.OrderInLayerSlider,
                Is.Not.Null);
            Assert.That(
                controls.OrderInLayerUnitySlider,
                Is.Not.Null);

            Assert.That(controls.ReturnButtonObject, Is.Not.Null);
            Assert.That(
                controls.ReturnButtonManager,
                Is.Not.Null);
        }
    }
}
