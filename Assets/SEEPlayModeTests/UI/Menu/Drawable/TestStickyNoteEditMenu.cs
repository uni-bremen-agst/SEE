
using Michsky.MUIP;
using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Play-mode integration tests for the sticky note edit menu.
    /// The tests use the real menu prefabs and allow MUIP components
    /// to initialize through Unity's runtime lifecycle.
    /// </summary>
    [TestFixture]
    internal class TestStickyNoteEditMenu : TestUI
    {
        /// <summary>
        /// The UI canvas hosting the tested menus.
        /// </summary>
        private GameObject uiCanvasObject;

        /// <summary>
        /// The sticky note used by the tests.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// Creates the UI canvas and sticky note required by each test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for Unity's initialization lifecycle.
        /// </returns>
        [UnitySetUp]
        public IEnumerator SetUpStickyNoteEditMenu()
        {
            uiCanvasObject = new GameObject(
                "UI Canvas",
                typeof(RectTransform),
                typeof(Canvas));

            stickyNote = new GameObject("StickyNote");

            yield return null;
        }

        /// <summary>
        /// Destroys the tested menus and all objects created by the test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for deferred object destruction.
        /// </returns>
        [UnityTearDown]
        public IEnumerator TearDownStickyNoteEditMenu()
        {
            ScaleMenu.Instance.Destroy();
            StickyNoteEditMenu.Instance.Destroy();

            if (stickyNote != null)
            {
                Object.Destroy(stickyNote);
            }

            if (uiCanvasObject != null)
            {
                Object.Destroy(uiCanvasObject);
            }

            yield return null;
        }

        /// <summary>
        /// Opens the sticky note edit menu and isolates unrelated slider
        /// and color-picker callbacks from the lighting and menu-transition
        /// tests. The UI components remain enabled for their normal
        /// PlayMode initialization.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used to initialize the edit menu.
        /// </param>
        private void OpenEditMenu(DrawableConfig configuration)
        {
            StickyNoteEditMenu.Instance.Enable(stickyNote, configuration);

            LayerSliderController slider =
                uiCanvasObject.GetComponentInChildren<LayerSliderController>(true);

            Assert.That(slider, Is.Not.Null);
            slider.OnValueChanged.RemoveAllListeners();

            HSVPicker.ColorPicker colorPicker =
                uiCanvasObject.GetComponentInChildren<HSVPicker.ColorPicker>(true);

            Assert.That(colorPicker, Is.Not.Null);
            colorPicker.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Verifies that the lighting switch reflects an enabled
        /// lighting configuration.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestLightingSwitchUsesEnabledConfiguration()
        {
            DrawableConfig configuration = CreateConfiguration(true);

            OpenEditMenu(configuration);

            yield return null;

            Assert.That(FindLightingSwitch().isOn, Is.True);
        }

        /// <summary>
        /// Verifies that the lighting switch reflects a disabled
        /// lighting configuration.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestLightingSwitchUsesDisabledConfiguration()
        {
            DrawableConfig configuration = CreateConfiguration(false);

            OpenEditMenu(configuration);

            yield return null;

            Assert.That(FindLightingSwitch().isOn, Is.False);
        }

        /// <summary>
        /// Verifies that the scale button disables the edit menu
        /// and opens the scale menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestScaleButtonDisablesEditMenuAndOpensScaleMenu()
        {
            OpenEditMenu(CreateConfiguration(true));

            ButtonManager scaleButton = FindButton("Scale");

            scaleButton.onClick.Invoke();

            yield return null;

            Assert.That(scaleButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(ScaleMenu.Instance.IsOpen(), Is.True);
        }

        /// <summary>
        /// Creates a sticky note configuration for the tests.
        /// </summary>
        /// <param name="lighting">
        /// Whether lighting should be enabled.
        /// </param>
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
        /// Finds the lighting switch belonging to the instantiated
        /// sticky note edit menu.
        /// </summary>
        /// <returns>The lighting switch.</returns>
        private SwitchManager FindLightingSwitch()
        {
            SwitchManager[] candidates =
                uiCanvasObject.GetComponentsInChildren<SwitchManager>(true);

            foreach (SwitchManager candidate in candidates)
            {
                if (candidate.gameObject.name == "LightningSwitch")
                {
                    return candidate;
                }
            }

            Assert.Fail(
                "Could not find the lighting switch of the sticky note edit menu.");

            return null;
        }

        /// <summary>
        /// Finds a button belonging to the instantiated edit menu.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>The matching button.</returns>
        private ButtonManager FindButton(string name)
        {
            ButtonManager[] candidates =
                uiCanvasObject.GetComponentsInChildren<ButtonManager>(true);

            foreach (ButtonManager candidate in candidates)
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail(
                $"Could not find the button '{name}' of the sticky note edit menu.");

            return null;
        }
    }
}
