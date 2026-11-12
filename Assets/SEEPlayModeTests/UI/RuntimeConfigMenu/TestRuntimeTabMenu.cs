using Cysharp.Threading.Tasks;
using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tests for runtime configuration menus.
/// </summary>
namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Tests the interaction of runtime configuration controls.
    /// </summary>
    [Category("ModernUIPack")]
    internal class TestRuntimeTabMenu
    {
        /// <summary>
        /// Owns every object created by the test.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// Removes test objects even if an assertion failed.
        /// </summary>
        /// <returns>An enumerator that waits for object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        /// <summary>
        /// Checks that the runtime menu disables and re-enables actual method-button clicks.
        /// </summary>
        /// <returns>An enumerator that waits for the menu's asynchronous state updates.</returns>
        /// <exception cref="AssertionException">A disabled action executes or an enabled action does not execute exactly once.</exception>
        [UnityTest]
        public IEnumerator DisabledMethodButtonsIgnorePointerClicks()
        {
            EventSystem events = CreateRaycastCanvas("Runtime button test");
            GameObject owner = new("Inactive menu");
            owner.transform.SetParent(root.transform);
            owner.SetActive(false);
            RuntimeTabMenu menu = owner.AddComponent<RuntimeTabMenu>();
            GameObject instance = Object.Instantiate(
                Resources.Load<GameObject>("Prefabs/UI/RuntimeConfigMenu/RuntimeConfigButton"), root.transform, false);
            RectTransform rectangle = (RectTransform)instance.transform;
            rectangle.anchorMin = rectangle.anchorMax = new Vector2(0.5f, 0.5f);
            rectangle.anchoredPosition = Vector2.zero;
            ButtonManager manager = instance.GetComponent<ButtonManager>();
            int clicks = 0;
            manager.onClick.AddListener(() => clicks++);
            MethodInfo update = typeof(RuntimeTabMenu).GetMethod("SetInteractableAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            yield return ((UniTask)update.Invoke(menu, new object[] { instance, false })).ToCoroutine();
            Click(rectangle, instance, events);
            Assert.That(clicks, Is.Zero);
            Assert.That(manager.isInteractable, Is.False);
            Assert.That(instance.GetComponent<Button>().interactable, Is.False);

            yield return ((UniTask)update.Invoke(menu, new object[] { instance, true })).ToCoroutine();
            Click(rectangle, instance, events);
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(manager.isInteractable, Is.True);
            Assert.That(instance.GetComponent<Button>().interactable, Is.True);
        }

        /// <summary>
        /// Checks pointer and submit events on the actual dropdown resource used by the runtime menu.
        /// </summary>
        /// <returns>An enumerator that waits for dropdown layout and visible animation states.</returns>
        /// <exception cref="AssertionException">A click is handled twice, an item cannot be reached,
        /// or the dropdown fails to display, select, close, or reopen.</exception>
        [UnityTest]
        public IEnumerator RuntimeDropdownOpensSelectsAndReopensThroughInputEvents()
        {
            EventSystem events = CreateRaycastCanvas("Runtime dropdown test");

            // Resolve the factory's resource rather than testing a different dropdown prefab.
            FieldInfo prefabField = typeof(RuntimeTabMenu).GetField("dropDownPrefab",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(prefabField, Is.Not.Null);
            GameObject instance = Object.Instantiate(
                Resources.Load<GameObject>((string)prefabField.GetRawConstantValue()), root.transform, false);
            RectTransform widget = (RectTransform)instance.transform;
            widget.anchorMin = widget.anchorMax = new Vector2(0.5f, 0.5f);
            widget.anchoredPosition = Vector2.zero;
            CustomDropdown dropdown = instance.GetComponentInChildren<CustomDropdown>();
            dropdown.items.Clear();
            dropdown.items.Add(new() { itemName = "First" });
            dropdown.items.Add(new() { itemName = "Second" });
            dropdown.selectedItemIndex = 0;
            dropdown.SetupDropdown();
            int changes = 0;
            dropdown.onValueChanged.AddListener(_ => changes++);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            Click((RectTransform)dropdown.selectedText.transform, dropdown.gameObject, events);
            Assert.That(dropdown.isOn, Is.True, "One pointer click must leave the dropdown open.");
            yield return WaitForAnimation(dropdown);
            Assert.That(dropdown.listCG.alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(dropdown.listCG.interactable && dropdown.listCG.blocksRaycasts, Is.True);
            Assert.That(dropdown.listRect.rect.height, Is.GreaterThan(0f));

            GameObject second = dropdown.itemParent.Find("Second").gameObject;
            Click((RectTransform)second.transform, second, events);
            Assert.That(changes, Is.EqualTo(1), "Selecting one item must notify exactly once.");
            Assert.That(dropdown.selectedItemIndex, Is.EqualTo(1));
            Assert.That(dropdown.selectedText.text, Is.EqualTo("Second"));
            Assert.That(dropdown.isOn, Is.False);
            yield return WaitForAnimation(dropdown);
            Assert.That(dropdown.listCG.alpha, Is.EqualTo(0f).Within(0.01f));

            Click((RectTransform)dropdown.selectedText.transform, dropdown.gameObject, events);
            Assert.That(dropdown.isOn, Is.True, "The first pointer click after selection must reopen the list.");
            yield return WaitForAnimation(dropdown);
            instance.SetActive(false);
            instance.SetActive(true);
            yield return null;
            Assert.That(dropdown.isOn, Is.False);
            Assert.That(dropdown.triggerObject.activeSelf, Is.False);
            Assert.That(dropdown.selectedText.text, Is.EqualTo("Second"));

            ExecuteEvents.Execute(dropdown.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
            Assert.That(dropdown.isOn, Is.True, "One submit must open the reactivated dropdown.");
            yield return WaitForAnimation(dropdown);
            Assert.That(dropdown.listCG.alpha, Is.EqualTo(1f).Within(0.01f));
            ExecuteEvents.Execute(dropdown.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
            Assert.That(dropdown.isOn, Is.False, "One submit must close an open dropdown.");
            yield return WaitForAnimation(dropdown);
            Assert.That(dropdown.listCG.alpha, Is.EqualTo(0f).Within(0.01f));
            Assert.That(changes, Is.EqualTo(1), "Opening and closing must not notify another selection.");
        }

        /// <summary>
        /// Creates the test canvas and an event system if the scene has none.
        /// </summary>
        /// <param name="name">The name of the test canvas.</param>
        /// <returns>The event system used for the test clicks.</returns>
        private EventSystem CreateRaycastCanvas(string name)
        {
            root = new(name, typeof(Canvas), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            if (EventSystem.current != null)
            {
                return EventSystem.current;
            }
            GameObject eventObject = new("Runtime menu test events", typeof(EventSystem));
            eventObject.transform.SetParent(root.transform, false);
            return eventObject.GetComponent<EventSystem>();
        }

        /// <summary>
        /// Raycasts at the rectangle center and dispatches a pointer click to the actual hit handler.
        /// </summary>
        /// <param name="rectangle">The rectangle to click.</param>
        /// <param name="expectedTarget">The expected pointer-click handler.</param>
        /// <param name="events">The event system used for raycasts and pointer events.</param>
        /// <exception cref="AssertionException">The intended control cannot be reached by a raycast.</exception>
        private static void Click(RectTransform rectangle, GameObject expectedTarget, EventSystem events)
        {
            Canvas.ForceUpdateCanvases();
            PointerEventData pointer = new(events)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rectangle.TransformPoint(rectangle.rect.center))
            };
            List<RaycastResult> hits = new();
            events.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, expectedTarget.name + " must be visible to the raycaster.");
            GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(target, Is.EqualTo(expectedTarget), "Another UI object intercepts the click.");
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>
        /// Waits for the dropdown's longest animation clip and its final UI update.
        /// </summary>
        /// <param name="dropdown">The animated dropdown.</param>
        /// <returns>An enumerator that waits for the animation to finish.</returns>
        private static IEnumerator WaitForAnimation(CustomDropdown dropdown)
        {
            float duration = 0f;
            foreach (AnimationClip clip in dropdown.dropdownAnimator.runtimeAnimatorController.animationClips)
            {
                duration = Mathf.Max(duration, clip.length);
            }
            yield return new WaitForSecondsRealtime(duration + 0.1f);
        }
    }
}
