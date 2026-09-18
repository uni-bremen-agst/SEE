using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the main drawable color picker menu.
    /// </summary>
    [TestFixture]
    public class TestColorPickerMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The primary color configured before the current test.
        /// </summary>
        private Color previousPrimaryColor;

        /// <summary>
        /// The secondary color configured before the current test.
        /// </summary>
        private Color previousSecondaryColor;

        /// <summary>
        /// Stores the current colors and opens the color picker menu with known test values.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            previousPrimaryColor = ValueHolder.CurrentPrimaryColor;
            previousSecondaryColor = ValueHolder.CurrentSecondaryColor;

            ValueHolder.CurrentPrimaryColor = Color.red;
            ValueHolder.CurrentSecondaryColor = Color.blue;

            ColorPickerMenu.Instance.Enable();
        }

        /// <summary>
        /// Destroys the menu and restores the global drawable colors after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ColorPickerMenu.Instance.Destroy();

            ValueHolder.CurrentPrimaryColor = previousPrimaryColor;
            ValueHolder.CurrentSecondaryColor = previousSecondaryColor;
        }

        /// <summary>
        /// Verifies that opening the menu synchronizes both color pickers with
        /// the current drawable colors.
        /// </summary>
        [Test]
        public void TestEnableUsesCurrentColors()
        {
            Assert.That(ColorPickerMenu.Instance.PrimaryColor, Is.EqualTo(Color.red));
            Assert.That(ColorPickerMenu.Instance.SecondaryColor, Is.EqualTo(Color.blue));
        }

        /// <summary>
        /// Verifies that assigning a primary color updates the corresponding picker.
        /// </summary>
        [Test]
        public void TestAssignPrimaryColorUpdatesPicker()
        {
            ColorPickerMenu.Instance.AssignPrimaryColor(Color.green);

            Assert.That(ColorPickerMenu.Instance.PrimaryColor, Is.EqualTo(Color.green));
        }

        /// <summary>
        /// Verifies that assigning a secondary color updates the corresponding picker.
        /// </summary>
        [Test]
        public void TestAssignSecondaryColorUpdatesPicker()
        {
            ColorPickerMenu.Instance.AssignSecondaryColor(Color.yellow);

            Assert.That(ColorPickerMenu.Instance.SecondaryColor, Is.EqualTo(Color.yellow));
        }

        /// <summary>
        /// Verifies that the switch state determines whether secondary-color selection is active.
        /// </summary>
        [Test]
        public void TestGetSwitchStatusUsesCurrentSwitchState()
        {
            SwitchManager switchManager = FindSwitch();

            switchManager.isOn = false;
            Assert.That(ColorPickerMenu.Instance.GetSwitchStatus(), Is.False);

            switchManager.isOn = true;
            Assert.That(ColorPickerMenu.Instance.GetSwitchStatus(), Is.True);
        }

        /// <summary>
        /// Verifies that the menu can be recreated after its previous UI instance was destroyed.
        /// </summary>
        [Test]
        public void TestEnableRecreatesDestroyedMenu()
        {
            ColorPickerMenu.Instance.Destroy();

            ValueHolder.CurrentPrimaryColor = Color.magenta;
            ValueHolder.CurrentSecondaryColor = Color.cyan;

            Assert.DoesNotThrow(() => ColorPickerMenu.Instance.Enable());

            Assert.That(ColorPickerMenu.Instance.IsOpen(), Is.True);
            Assert.That(ColorPickerMenu.Instance.PrimaryColor, Is.EqualTo(Color.magenta));
            Assert.That(ColorPickerMenu.Instance.SecondaryColor, Is.EqualTo(Color.cyan));
        }

        /// <summary>
        /// Verifies that assigning colors while no UI instance exists does not recreate the menu.
        /// </summary>
        [Test]
        public void TestAssignColorDoesNotRecreateDestroyedMenu()
        {
            ColorPickerMenu.Instance.Destroy();

            Assert.DoesNotThrow(() => ColorPickerMenu.Instance.AssignPrimaryColor(Color.green));
            Assert.DoesNotThrow(() => ColorPickerMenu.Instance.AssignSecondaryColor(Color.yellow));
            Assert.That(ColorPickerMenu.Instance.IsOpen(), Is.False);
        }

        /// <summary>
        /// Finds the primary-secondary selection switch of the currently instantiated menu.
        /// </summary>
        /// <returns>The selection switch.</returns>
        private static SwitchManager FindSwitch()
        {
            foreach (SwitchManager candidate in Object.FindObjectsByType<SwitchManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Switch")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the switch of the color picker menu.");
            return null;
        }
    }
}
