using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
                Assert.That(button.targetGraphic.canvasRenderer.GetColor().r,
                            Is.EqualTo(0.84313726f).Within(0.02f), prefabName + " lost its hover tint.");

                int clicks = 0;
                manager.onClick.AddListener(() => clicks++);
                ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(clicks, Is.EqualTo(1), prefabName + " must invoke its action exactly once.");

                manager.Interactable(false);
                yield return new WaitForEndOfFrame();
                Assert.That(button.interactable, Is.False, prefabName + " must also disable the Unity button.");
                ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(clicks, Is.EqualTo(1), prefabName + " must ignore clicks while disabled.");

                Object.Destroy(instance);
                yield return null;
            }
        }
    }
}
