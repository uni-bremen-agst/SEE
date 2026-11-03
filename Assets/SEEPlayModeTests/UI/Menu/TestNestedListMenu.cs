using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Menus for the SEE user interface.
/// </summary>
namespace SEE.UI.Menu
{
    /// <summary>
    /// Tests the window controls and close-button visibility of <see cref="NestedListMenu"/>.
    /// </summary>
    [Category("ModernUIPack")]
    internal class TestNestedListMenu
    {
        /// <summary>
        /// The canvas containing the UI objects created by each test.
        /// </summary>
        private GameObject canvas;

        /// <summary>
        /// The event system used to dispatch simulated pointer events.
        /// </summary>
        private GameObject eventSystem;

        /// <summary>
        /// Creates a canvas and an event system for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            canvas = new("UI Canvas", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            eventSystem = new("Menu prefab events", typeof(EventSystem));
        }

        /// <summary>
        /// Destroys the UI objects created by each test.
        /// </summary>
        /// <returns>An enumerator that waits one frame for destruction to complete.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(canvas);
            Object.Destroy(eventSystem);
            yield return null;
        }

        /// <summary>
        /// Checks that Back raises its event without closing the window and Close closes it.
        /// </summary>
        /// <returns>An enumerator that waits for the menu to open.</returns>
        /// <exception cref="AssertionException">A button reference is missing or a button produces an incorrect action.</exception>
        [UnityTest]
        public IEnumerator NestedMenuBackKeepsTheWindowOpenAndCloseClosesIt()
        {
            GameObject instance = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/NestedMenu"),
                                                     canvas.transform, false);
            ModalWindowManager modal = instance.GetComponent<ModalWindowManager>();
            Assert.That(modal.confirmButton, Is.Not.Null, "Close must reference the new ButtonManager.");
            Assert.That(modal.cancelButton, Is.Not.Null, "Back must reference the new ButtonManager.");
            modal.OpenWindow();
            yield return null;

            int backs = 0;
            modal.onCancel.AddListener(() => backs++);
            PointerEventData pointer = new(eventSystem.GetComponent<EventSystem>())
            {
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(modal.cancelButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(backs, Is.EqualTo(1));
            Assert.That(modal.isOn, Is.True, "Back navigates up without closing the menu.");

            ExecuteEvents.Execute(modal.confirmButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(modal.isOn, Is.False, "Close must close the menu.");
        }

        /// <summary>
        /// Checks that changing the title keeps Close hidden when a selection is required.
        /// </summary>
        /// <returns>An enumerator that waits for the menu to initialize.</returns>
        /// <exception cref="AssertionException">The menu is missing or Close becomes visible despite requiring a selection.</exception>
        [UnityTest]
        public IEnumerator RequiredSelectionMenuKeepsCloseHiddenAfterTitleChanges()
        {
            UserSettings.UserSetting settings = canvas.AddComponent<UserSettings.UserSetting>();
            settings.enabled = false;
            GameObject container = new("Required selection menu");
            container.transform.SetParent(canvas.transform);
            NestedListMenu menu = container.AddComponent<NestedListMenu>();
            menu.Parent = canvas.transform;
            menu.Title = "Original title";
            menu.AllowNoSelection = false;
            yield return null;
            yield return null;

            ModalWindowManager modal = canvas.GetComponentInChildren<ModalWindowManager>(true);
            Assert.That(modal, Is.Not.Null);
            Assert.That(modal.confirmButton.gameObject.activeSelf, Is.False);
            menu.Title = "Updated title";
            Assert.That(modal.confirmButton.gameObject.activeSelf, Is.False,
                        "Refreshing content must not expose Close when a selection is required.");
        }
    }
}
