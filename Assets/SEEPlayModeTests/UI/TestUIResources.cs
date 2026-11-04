using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

/// <summary>
/// User interface components and their resource tests.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Tests serialized dependencies and animated states of migrated UI prefabs.
    /// </summary>
    /// <remarks>
    /// This fixture tests prefab resources rather than introducing a runtime component.
    /// Selector controls are instantiated separately from their scene-dependent menu controllers.
    /// </remarks>
    [Category("ModernUIPack")]
    internal class TestUIResources
    {
        /// <summary>
        /// Owns every UI object created by a test.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// The event system used for simulated pointer events.
        /// </summary>
        private EventSystem events;

        /// <summary>
        /// Creates a canvas and reuses the scene's event system when available.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new("UI resource test", typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            events = EventSystem.current;
            if (events == null)
            {
                GameObject eventObject = new("UI resource events", typeof(EventSystem));
                eventObject.transform.SetParent(root.transform);
                events = eventObject.GetComponent<EventSystem>();
            }
        }

        /// <summary>
        /// Removes UI objects even after a failed assertion.
        /// </summary>
        /// <returns>An enumerator that waits for object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        /// <summary>
        /// Checks the animation and selection callbacks of all previously broken selector arrows.
        /// </summary>
        /// <returns>An enumerator that waits for initialization and animation transitions.</returns>
        /// <exception cref="AssertionException">An arrow loses its controller, visual state,
        /// or exactly-once selection callback.</exception>
        [UnityTest]
        public IEnumerator SelectorArrowsKeepAnimationsAndSelectionCallbacks()
        {
            int checkedArrows = 0;
            foreach ((string prefabName, int expectedArrows) in new[]
            {
                ("Drawable/MMSelectParent", 2),
                ("Drawable/MMChangeNodeKind", 2),
                ("Drawable/LineMenu", 8),
                ("Drawable/ShapeMenu", 10),
                ("ConfigMenu", 2)
            })
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/" + prefabName);
                Assert.That(prefab, Is.Not.Null, prefabName);
                int prefabArrows = 0;
                foreach (HorizontalSelector source in prefab.GetComponentsInChildren<HorizontalSelector>(true))
                {
                    foreach (string arrowName in new[] { "Next", "Prev" })
                    {
                        Animator sourceAnimator = source.transform.Find(arrowName).GetComponent<Animator>();
                        Assert.That(sourceAnimator.runtimeAnimatorController, Is.Not.Null,
                                    prefabName + "/" + arrowName + " has no animation controller.");
                    }
                    GameObject instance = Object.Instantiate(source.gameObject, root.transform, false);
                    instance.SetActive(true);
                    HorizontalSelector selector = instance.GetComponent<HorizontalSelector>();
                    selector.items.Clear();
                    foreach (string title in new[] { "First", "Second", "Third" })
                    {
                        selector.CreateNewItem(title);
                    }
                    selector.defaultIndex = 1;
                    selector.SetupSelector();
                    int changes = 0;
                    selector.onValueChanged.AddListener(_ => changes++);
                    yield return null;

                    foreach (string arrowName in new[] { "Next", "Prev" })
                    {
                        selector.defaultIndex = 1;
                        selector.SetupSelector();
                        int previousChanges = changes;
                        GameObject arrow = instance.transform.Find(arrowName).gameObject;
                        Animator animator = arrow.GetComponent<Animator>();
                        UnityEngine.UI.Image filled = arrow.transform.Find("Filled").GetComponent<UnityEngine.UI.Image>();
                        PointerEventData pointer = new(events) { button = PointerEventData.InputButton.Left };
                        ExecuteEvents.Execute(arrow, pointer, ExecuteEvents.pointerEnterHandler);
                        yield return new WaitForSecondsRealtime(0.35f);
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Highlighted"), Is.True, prefabName);
                        Assert.That(filled.color.a, Is.EqualTo(1f).Within(0.01f), prefabName);
                        Assert.That(arrow.transform.localScale.x, Is.EqualTo(1.2f).Within(0.01f), prefabName);

                        ExecuteEvents.Execute(arrow, pointer, ExecuteEvents.pointerDownHandler);
                        yield return new WaitForSecondsRealtime(0.35f);
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Pressed"), Is.True, prefabName);
                        Assert.That(arrow.transform.localScale.x, Is.EqualTo(1.2f).Within(0.01f), prefabName);
                        ExecuteEvents.Execute(arrow, pointer, ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(arrow, pointer, ExecuteEvents.pointerClickHandler);
                        Assert.That(selector.index, Is.EqualTo(arrowName == "Next" ? 2 : 0), prefabName);
                        Assert.That(changes, Is.EqualTo(previousChanges + 1), prefabName);

                        ExecuteEvents.Execute(arrow, pointer, ExecuteEvents.pointerExitHandler);
                        yield return new WaitForSecondsRealtime(0.35f);
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Normal"), Is.True, prefabName);
                        Assert.That(filled.color.a, Is.EqualTo(0f).Within(0.01f), prefabName);
                        Assert.That(arrow.transform.localScale.x, Is.EqualTo(1f).Within(0.01f), prefabName);
                        prefabArrows++;
                    }
                    Object.Destroy(instance);
                    yield return null;
                }
                Assert.That(prefabArrows, Is.EqualTo(expectedArrows), prefabName);
                checkedArrows += prefabArrows;
            }
            Assert.That(checkedArrows, Is.EqualTo(24));
        }

        /// <summary>
        /// Checks that the three stale icon references resolve to current imported sprites.
        /// </summary>
        /// <returns>An enumerator that waits for each image to join the test canvas.</returns>
        /// <exception cref="AssertionException">An icon has a missing or incorrect sprite or cannot render.</exception>
        [UnityTest]
        public IEnumerator MigratedIconsResolveAndRenderCurrentSprites()
        {
            foreach ((string prefabName, string imagePath, string spriteName) in new[]
            {
                ("Drawable/ShapeSwitch", "Dragger/Icon", "Switch"),
                ("Drawable/TextMenu", "Dragger/Icon", "Input Filed"),
                ("HelpSystemEntry", "Buttons/Content/Close/Normal/Icon", "Close")
            })
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/" + prefabName);
                Assert.That(prefab, Is.Not.Null, prefabName);
                UnityEngine.UI.Image source = prefab.transform.Find(imagePath).GetComponent<UnityEngine.UI.Image>();
                Assert.That(source.sprite, Is.Not.Null, prefabName + "/" + imagePath);
                Assert.That(source.sprite.name, Is.EqualTo(spriteName), prefabName);
                GameObject instance = Object.Instantiate(source.gameObject, root.transform, false);
                UnityEngine.UI.Image image = instance.GetComponent<UnityEngine.UI.Image>();
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                image.rectTransform.anchoredPosition = Vector2.zero;
                image.rectTransform.localScale = Vector3.one;
                image.rectTransform.sizeDelta = new Vector2(64f, 64f);
                instance.SetActive(true);
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.That(image.mainTexture.width, Is.GreaterThan(0), prefabName);
                Assert.That(image.mainTexture.height, Is.GreaterThan(0), prefabName);
                Assert.That(image.canvasRenderer.cull, Is.False, prefabName);
                Object.Destroy(instance);
                yield return null;
            }
        }
    }
}
