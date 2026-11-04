using Cysharp.Threading.Tasks;
using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
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
            root = new("Runtime button test", typeof(Canvas));
            GameObject owner = new("Inactive menu");
            owner.transform.SetParent(root.transform);
            owner.SetActive(false);
            RuntimeTabMenu menu = owner.AddComponent<RuntimeTabMenu>();
            GameObject instance = Object.Instantiate(
                Resources.Load<GameObject>("Prefabs/UI/RuntimeConfigMenu/RuntimeConfigButton"), root.transform, false);
            ButtonManager manager = instance.GetComponent<ButtonManager>();
            int clicks = 0;
            manager.onClick.AddListener(() => clicks++);
            MethodInfo update = typeof(RuntimeTabMenu).GetMethod("SetInteractableAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            yield return ((UniTask)update.Invoke(menu, new object[] { instance, false })).ToCoroutine();
            PointerEventData pointer = new(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(clicks, Is.Zero);
            Assert.That(manager.isInteractable, Is.False);
            Assert.That(instance.GetComponent<Button>().interactable, Is.False);

            yield return ((UniTask)update.Invoke(menu, new object[] { instance, true })).ToCoroutine();
            ExecuteEvents.Execute(instance, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(manager.isInteractable, Is.True);
            Assert.That(instance.GetComponent<Button>().interactable, Is.True);
        }
    }
}
