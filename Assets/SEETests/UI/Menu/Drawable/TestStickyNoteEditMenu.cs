using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the sticky note edit menu.
    /// </summary>
    [TestFixture]
    public class TestStickyNoteEditMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The sticky note used by the tests.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// Creates the sticky note used by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            stickyNote = new GameObject("StickyNote");
        }

        /// <summary>
        /// Destroys all menus and objects created by the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            StickyNoteEditMenu.Instance.Destroy();
            ScaleMenu.Instance.Destroy();

            if (stickyNote != null)
            {
                Object.DestroyImmediate(stickyNote);
            }
        }

        /// <summary>
        /// Verifies that the lighting switch reflects an enabled lighting configuration.
        /// </summary>
        [Test]
        public void TestLightingSwitchUsesEnabledConfiguration()
        {
            DrawableConfig configuration = CreateConfiguration(true);

            StickyNoteEditMenu.Instance.Enable(stickyNote, configuration);

            Assert.That(FindLightingSwitch().isOn, Is.True);
        }

        /// <summary>
        /// Verifies that the lighting switch reflects a disabled lighting configuration.
        /// </summary>
        [Test]
        public void TestLightingSwitchUsesDisabledConfiguration()
        {
            DrawableConfig configuration = CreateConfiguration(false);

            StickyNoteEditMenu.Instance.Enable(stickyNote, configuration);

            Assert.That(FindLightingSwitch().isOn, Is.False);
        }

        /// <summary>
        /// Verifies that the scale button disables the edit menu and opens the scale menu.
        /// </summary>
        [Test]
        public void TestScaleButtonDisablesEditMenuAndOpensScaleMenu()
        {
            StickyNoteEditMenu.Instance.Enable(stickyNote, CreateConfiguration(true));

            ButtonManagerBasic scaleButton = FindButton("Scale");
            scaleButton.clickEvent.Invoke();

            Assert.That(scaleButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(ScaleMenu.Instance.IsOpen(), Is.True);
        }

        /// <summary>
        /// Creates a sticky note configuration for the tests.
        /// </summary>
        /// <param name="lighting">Whether lighting should be enabled.</param>
        /// <returns>The created configuration.</returns>
        private static DrawableConfig CreateConfiguration(bool lighting)
        {
            return new DrawableConfig
            {
                Order = 1,
                Color = Color.white,
                Lighting = lighting
            };
        }

        /// <summary>
        /// Finds the lighting switch of the sticky note edit menu.
        /// </summary>
        /// <returns>The lighting switch.</returns>
        private static SwitchManager FindLightingSwitch()
        {
            foreach (SwitchManager candidate in Object.FindObjectsByType<SwitchManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "LightningSwitch")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the lighting switch of the sticky note edit menu.");
            return null;
        }

        /// <summary>
        /// Finds a button of the sticky note edit menu by name.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>The matching button.</returns>
        private static ButtonManagerBasic FindButton(string name)
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Could not find the button '{name}' of the sticky note edit menu.");
            return null;
        }
    }
}
