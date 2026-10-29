using SEE.Game.Drawable.Line;
using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests color selection from lines and line caps.
    /// </summary>
    [TestFixture]
    public class TestColorPickerLineMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// A temporary line hierarchy used for direct line-cap hit resolution tests.
        /// </summary>
        private GameObject lineObject;

        /// <summary>
        /// Destroys the menu and temporary line hierarchy after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ColorPickerLineMenu.Instance.Destroy();

            if (lineObject != null)
            {
                Object.DestroyImmediate(lineObject);
                lineObject = null;
            }
        }

        /// <summary>
        /// Verifies that the primary color of a simple line is selected immediately.
        /// </summary>
        [Test]
        public void TestSimpleLineSelectsPrimaryColorImmediately()
        {
            LineConf configuration = CreateConfigurationWithoutCaps();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);

            Assert.That(ColorPickerLineMenu.Instance.IsOpen(), Is.False);
            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.PrimaryColor));
        }

        /// <summary>
        /// Verifies that the secondary color of a non-monochrome line is selected immediately.
        /// </summary>
        [Test]
        public void TestSimpleLineSelectsSecondaryColorImmediately()
        {
            LineConf configuration = CreateConfigurationWithoutCaps();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, false);

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.SecondaryColor));
        }

        /// <summary>
        /// Verifies that requesting the secondary color of a monochrome line returns its primary color.
        /// </summary>
        [Test]
        public void TestMonochromeLineUsesPrimaryColorForSecondarySelection()
        {
            LineConf configuration = CreateConfigurationWithoutCaps();
            configuration.ColorKind = ColorKind.Monochrome;

            ColorPickerLineMenu.Instance.BeginSelection(configuration, false);

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.PrimaryColor));
        }

        /// <summary>
        /// Verifies that a filled line exposes its fill-out color even when it is monochrome.
        /// </summary>
        [Test]
        public void TestMonochromeFilledLineOffersFillOutColor()
        {
            LineConf configuration = CreateConfigurationWithoutCaps();
            configuration.ColorKind = ColorKind.Monochrome;
            configuration.FillOutStatus = true;

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);

            Assert.That(ColorPickerLineMenu.Instance.IsOpen(), Is.True);
            Assert.That(FindButton("Secondary").enabled, Is.False);
            Assert.That(FindButton("Secondary").GetComponent<Button>().interactable, Is.False);

            FindButton("FillOut").clickEvent.Invoke();

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.FillOutColor));
        }

        /// <summary>
        /// Verifies that lines with caps first offer main, start-cap, and end-cap selection.
        /// </summary>
        [Test]
        public void TestLineWithCapsOffersSegmentSelection()
        {
            LineConf configuration = CreateConfigurationWithCaps();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);

            Assert.That(FindButton("Primary").buttonText, Is.EqualTo("Main"));
            Assert.That(FindButton("Secondary").buttonText, Is.EqualTo("Start Cap"));
            Assert.That(FindButton("FillOut").buttonText, Is.EqualTo("End Cap"));
        }

        /// <summary>
        /// Verifies that a missing end cap cannot be selected.
        /// </summary>
        [Test]
        public void TestMissingEndCapIsDisabled()
        {
            LineConf configuration = CreateConfigurationWithCaps();
            configuration.LineCapEnd = LineCapConf.CreateNone();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);

            ButtonManagerBasic endCap = FindButton("FillOut");

            Assert.That(endCap.enabled, Is.False);
            Assert.That(endCap.GetComponent<Button>().interactable, Is.False);
        }

        /// <summary>
        /// Verifies that selecting the start cap returns its own primary color.
        /// </summary>
        [Test]
        public void TestStartCapUsesItsOwnPrimaryColor()
        {
            LineConf configuration = CreateConfigurationWithCaps();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);
            FindButton("Secondary").clickEvent.Invoke();

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.LineCapStart.PrimaryColor));
        }

        /// <summary>
        /// Verifies that selecting the start cap with the right mouse button returns
        /// the cap's own secondary color.
        /// </summary>
        [Test]
        public void TestStartCapUsesItsOwnSecondaryColor()
        {
            LineConf configuration = CreateConfigurationWithCaps();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, false);
            FindButton("Secondary").clickEvent.Invoke();

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.LineCapStart.SecondaryColor));
        }

        /// <summary>
        /// Verifies that a filled line cap opens the explicit color selection after
        /// the line-cap segment was selected.
        /// </summary>
        [Test]
        public void TestFilledStartCapOffersItsFillOutColor()
        {
            LineConf configuration = CreateConfigurationWithCaps();
            configuration.LineCapStart.FillOutStatus = true;

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);

            /// The "Secondary" prefab button represents Start Cap in the first step.
            FindButton("Secondary").clickEvent.Invoke();

            Assert.That(FindButton("Primary").buttonText, Is.EqualTo("Primary"));
            Assert.That(FindButton("Secondary").buttonText, Is.EqualTo("Secondary"));
            Assert.That(FindButton("FillOut").buttonText, Is.EqualTo("Fill Out"));

            FindButton("FillOut").clickEvent.Invoke();

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.LineCapStart.FillOutColor));
        }

        /// <summary>
        /// Verifies that directly selecting a start cap bypasses the segment selection.
        /// </summary>
        [Test]
        public void TestDirectStartCapSelectionBypassesSegmentSelection()
        {
            LineConf configuration = CreateConfigurationWithCaps();

            ColorPickerLineMenu.Instance.BeginSelection(
                configuration,
                false,
                LineColorTarget.StartCap);

            Assert.That(ColorPickerLineMenu.Instance.IsOpen(), Is.False);
            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.LineCapStart.SecondaryColor));
        }

        /// <summary>
        /// Verifies that nested objects of a generated start cap are recognized as part
        /// of the start cap.
        /// </summary>
        [Test]
        public void TestNestedStartCapObjectResolvesToStartCap()
        {
            lineObject = new GameObject("Line");

            GameObject startCap =
                new GameObject(ValueHolder.LineStartCapPrefix + "Test");
            startCap.transform.SetParent(lineObject.transform);

            GameObject nestedObject = new GameObject("FillOut");
            nestedObject.transform.SetParent(startCap.transform);

            LineColorTarget? target =
                ColorPickerLineMenu.ResolveDirectTarget(nestedObject, lineObject);

            Assert.That(target, Is.EqualTo(LineColorTarget.StartCap));
        }

        /// <summary>
        /// Verifies that nested objects of a generated end cap are recognized as part
        /// of the end cap.
        /// </summary>
        [Test]
        public void TestNestedEndCapObjectResolvesToEndCap()
        {
            lineObject = new GameObject("Line");

            GameObject endCap =
                new GameObject(ValueHolder.LineEndCapPrefix + "Test");
            endCap.transform.SetParent(lineObject.transform);

            GameObject nestedObject = new GameObject("FillOut");
            nestedObject.transform.SetParent(endCap.transform);

            LineColorTarget? target =
                ColorPickerLineMenu.ResolveDirectTarget(nestedObject, lineObject);

            Assert.That(target, Is.EqualTo(LineColorTarget.EndCap));
        }

        /// <summary>
        /// Verifies that destroying the menu discards an unconsumed immediate color selection.
        /// </summary>
        [Test]
        public void TestDestroyDiscardsPendingColor()
        {
            LineConf configuration = CreateConfigurationWithoutCaps();

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);
            ColorPickerLineMenu.Instance.Destroy();

            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.False);
            Assert.That(color, Is.EqualTo(Color.clear));
        }

        /// <summary>
        /// Verifies that the target-selection buttons display the secondary colors
        /// when the color picker was opened with the secondary-color request.
        /// </summary>
        [Test]
        public void TestSegmentSelectionDisplaysRequestedSecondaryColors()
        {
            LineConf configuration = CreateConfigurationWithCaps();

            configuration.PrimaryColor = Color.yellow;
            configuration.SecondaryColor = Color.black;

            configuration.LineCapStart.PrimaryColor = Color.red;
            configuration.LineCapStart.SecondaryColor = Color.green;

            configuration.LineCapEnd.PrimaryColor = Color.magenta;
            configuration.LineCapEnd.SecondaryColor = Color.blue;

            ColorPickerLineMenu.Instance.BeginSelection(configuration, false);

            Assert.That(
                FindButton("Primary").GetComponent<Image>().color,
                Is.EqualTo(Color.black));

            Assert.That(
                FindButton("Secondary").GetComponent<Image>().color,
                Is.EqualTo(Color.green));

            Assert.That(
                FindButton("FillOut").GetComponent<Image>().color,
                Is.EqualTo(Color.blue));
        }

        /// <summary>
        /// Verifies that button labels use a readable text color for bright and dark
        /// background colors.
        /// </summary>
        [Test]
        public void TestButtonTextColorProvidesContrast()
        {
            Assert.That(
                ColorPickerLineMenu.GetReadableTextColor(Color.white),
                Is.EqualTo(Color.black));

            Assert.That(
                ColorPickerLineMenu.GetReadableTextColor(Color.yellow),
                Is.EqualTo(Color.black));

            Assert.That(
                ColorPickerLineMenu.GetReadableTextColor(Color.black),
                Is.EqualTo(Color.white));

            Assert.That(
                ColorPickerLineMenu.GetReadableTextColor(new Color(0.1f, 0.25f, 0.05f)),
                Is.EqualTo(Color.white));
        }

        /// <summary>
        /// Creates a line configuration without line caps.
        /// </summary>
        /// <returns>The created configuration.</returns>
        private static LineConf CreateConfigurationWithoutCaps()
        {
            return new LineConf
            {
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                ColorKind = ColorKind.Gradient,
                FillOutStatus = false,
                FillOutColor = Color.green,
                LineCapStart = LineCapConf.CreateNone(),
                LineCapEnd = LineCapConf.CreateNone()
            };
        }

        /// <summary>
        /// Creates a line configuration with visually independent start and end caps.
        /// </summary>
        /// <returns>The created configuration.</returns>
        private static LineConf CreateConfigurationWithCaps()
        {
            LineConf configuration = CreateConfigurationWithoutCaps();

            configuration.LineCapStart = new LineCapConf
            {
                CapKind = LineCapPointsCalculator.LineCap.Arrowhead,
                ColorKind = ColorKind.Gradient,
                PrimaryColor = Color.magenta,
                SecondaryColor = Color.cyan,
                FillOutStatus = false,
                FillOutColor = Color.yellow,
                UseOwnVisuals = true
            };

            configuration.LineCapEnd = new LineCapConf
            {
                CapKind = LineCapPointsCalculator.LineCap.Arrow,
                ColorKind = ColorKind.Gradient,
                PrimaryColor = Color.black,
                SecondaryColor = Color.white,
                FillOutStatus = false,
                FillOutColor = Color.green,
                UseOwnVisuals = true
            };

            return configuration;
        }

        /// <summary>
        /// Finds a button of the currently instantiated line color picker menu.
        /// </summary>
        /// <param name="name">The original prefab name of the button.</param>
        /// <returns>The matching button manager.</returns>
        private static ButtonManagerBasic FindButton(string name)
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Could not find the button '{name}' of the line color picker menu.");
            return null;
        }
    }
}
