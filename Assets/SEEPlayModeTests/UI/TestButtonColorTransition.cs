using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SEE.UI.ConfigMenu;

/// <summary>
/// User interface components for SEE.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Tests the text, color transitions, and interaction of buttons using <see cref="ButtonColorTransition"/>.
    /// </summary>
    [Category("ModernUIPack")]
    internal class TestButtonColorTransition
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
        /// Whether this fixture owns the event system it must remove.
        /// </summary>
        private bool ownsEventSystem;

        /// <summary>
        /// Creates a test canvas and reuses an existing event system when available.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            canvas = new("UI Canvas", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            ownsEventSystem = EventSystem.current == null;
            eventSystem = ownsEventSystem ? new("Menu prefab events", typeof(EventSystem)) : EventSystem.current.gameObject;
        }

        /// <summary>
        /// Destroys the UI objects created by each test.
        /// </summary>
        /// <returns>An enumerator that waits one frame for destruction to complete.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(canvas);
            if (ownsEventSystem)
            {
                Object.Destroy(eventSystem);
            }
            yield return null;
        }

        /// <summary>
        /// Checks that simple buttons retain their captions, hover colors, and click behavior.
        /// </summary>
        /// <returns>An enumerator that waits for initialization and color transitions.</returns>
        /// <exception cref="AssertionException">A caption, hover color, object path, or click behavior
        /// differs from the expected behavior.</exception>
        [UnityTest]
        public IEnumerator SimpleButtonsKeepTheirTextHoverAndClickBehavior()
        {
            foreach (string prefabName in new[] { "Button", "ActionButton", "PopupMenuButton", "PopupMenuSubMenuButton" })
            {
                GameObject instance = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/" + prefabName),
                                                         canvas.transform, false);
                ButtonManager manager = instance.GetComponent<ButtonManager>();
                Assert.That(manager, Is.Not.Null, prefabName + " has no ButtonManager.");

                // Callers assign the caption immediately after instantiating the prefab.
                manager.buttonText = "New caption";
                yield return null;
                yield return null;
                Assert.That(manager.normalText.text, Is.EqualTo("New caption"), prefabName);

                if (prefabName != "ActionButton")
                {
                    Assert.That(instance.transform.Find("Icon"), Is.Not.Null, prefabName);
                }
                if (prefabName == "PopupMenuSubMenuButton")
                {
                    Assert.That(instance.transform.Find("RightIcon"), Is.Not.Null);
                }

                Button button = instance.GetComponent<Button>();
                PointerEventData pointer = new(eventSystem.GetComponent<EventSystem>())
                {
                    button = PointerEventData.InputButton.Left
                };
                ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerEnterHandler);
                yield return new WaitForSecondsRealtime(0.2f);
                AssertTint(button.targetGraphic.canvasRenderer.GetColor(), button.colors.highlightedColor,
                           prefabName + " lost its hover tint.");
                Assert.That(manager.normalText.text, Is.EqualTo("New caption"), prefabName);
                ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerExitHandler);
                yield return new WaitForSecondsRealtime(0.2f);
                AssertTint(button.targetGraphic.canvasRenderer.GetColor(), button.colors.normalColor,
                           prefabName + " did not restore its normal tint.");

                int clicks = 0;
                manager.onClick.AddListener(() => clicks++);
                ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(clicks, Is.EqualTo(1), prefabName + " must invoke its action exactly once.");

                manager.Interactable(false);
                yield return new WaitForEndOfFrame();
                Assert.That(button.interactable, Is.False, prefabName + " must also disable the Unity button.");
                Assert.That(manager.normalText.text, Is.EqualTo("New caption"), prefabName);
                ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(clicks, Is.EqualTo(1), prefabName + " must ignore clicks while disabled.");

                Object.Destroy(instance);
                yield return null;
            }
        }
        /// <summary>
        /// Checks that the helper preserves captions assigned by configuration tabs.
        /// </summary>
        /// <returns>An enumerator that waits for tab initialization and reactivation.</returns>
        /// <exception cref="AssertionException">A tab caption changes during initialization or reactivation.</exception>
        [UnityTest]
        public IEnumerator ConfigTabsKeepTheirAssignedCaptions()
        {
            canvas.AddComponent<TabController>().enabled = false;
            canvas.AddComponent<TabGroup>().enabled = false;
            foreach (string title in new[] { "Leaf nodes", "Inner nodes", "Nodes layout", "Edges layout", "Miscellaneous" })
            {
                GameObject instance = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/TabButton"),
                                                         canvas.transform, false);
                TabButton tab = instance.GetComponent<TabButton>();
                tab.ButtonText = title;
                tab.IsDefaultActive = false;
                yield return null;
                yield return null;
                ButtonManager manager = instance.GetComponent<ButtonManager>();
                Assert.That(manager.normalText.text, Is.EqualTo(title));
                instance.SetActive(false);
                instance.SetActive(true);
                yield return null;
                Assert.That(manager.normalText.text, Is.EqualTo(title));
            }
        }

        /// <summary>
        /// Checks that migrated controls retain their previous automatic navigation.
        /// </summary>
        /// <returns>An enumerator that waits for each button to initialize.</returns>
        /// <exception cref="AssertionException">A migrated button loses its automatic navigation.</exception>
        [UnityTest]
        public IEnumerator MigratedButtonsKeepAutomaticNavigation()
        {
            foreach ((string prefabName, string buttonName) in new[]
            {
                ("DictateButton", "DictateButton"),
                ("Input Group - Color Picker", "Trigger"),
                ("RuntimeConfigMenu/RuntimeSettingsObject", "AddBtn"),
                ("RuntimeConfigMenu/RuntimeSettingsObject", "RemoveBtn")
            })
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/" + prefabName);
                ButtonManager source = System.Array.Find(prefab.GetComponentsInChildren<ButtonManager>(true),
                                                         candidate => candidate.name == buttonName);
                Assert.That(source, Is.Not.Null, prefabName + "/" + buttonName);
                GameObject instance = Object.Instantiate(source.gameObject, canvas.transform, false);
                instance.SetActive(true);
                yield return null;
                Assert.That(instance.GetComponent<Button>().navigation.mode,
                            Is.EqualTo(Navigation.Mode.Automatic), prefabName + "/" + buttonName);
            }
        }

        /// <summary>
        /// Compares every channel of the rendered color with the configured tint.
        /// </summary>
        /// <param name="actual">The rendered tint.</param>
        /// <param name="expected">The expected tint.</param>
        /// <param name="message">The failure description.</param>
        /// <exception cref="AssertionException">At least one color channel differs.</exception>
        private static void AssertTint(Color actual, Color expected, string message)
        {
            for (int channel = 0; channel < 4; channel++)
            {
                Assert.That(actual[channel], Is.EqualTo(expected[channel]).Within(0.02f), message);
            }
        }

    }
}
