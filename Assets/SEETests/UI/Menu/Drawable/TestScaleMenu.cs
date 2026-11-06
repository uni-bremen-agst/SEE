using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the Drawable scale menu.
    /// </summary>
    [TestFixture]
    public class TestScaleMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The root object of the Drawable hierarchy used by the tests.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// The Drawable surface used by the tests.
        /// </summary>
        private GameObject surface;

        /// <summary>
        /// The container holding the objects attached to the Drawable.
        /// </summary>
        private GameObject attachedObjects;

        /// <summary>
        /// The object scaled by the menu.
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// Creates the Drawable hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("DrawableRoot");

            surface = new GameObject("Surface");
            surface.tag = Tags.Drawable;
            surface.transform.SetParent(root.transform);

            attachedObjects = new GameObject("AttachedObjects");
            attachedObjects.tag = Tags.AttachedObjects;
            attachedObjects.transform.SetParent(root.transform);

            selectedObject = new GameObject("SelectedObject");
            selectedObject.transform.SetParent(attachedObjects.transform);
            selectedObject.transform.localScale = new Vector3(1.25f, 1.75f, 1);
        }

        /// <summary>
        /// Destroys the scale menu and all objects created for the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ScaleMenu.Instance.Destroy();

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Verifies that opening the menu initializes its scale controls with
        /// the current scale of the selected object.
        /// </summary>
        [Test]
        public void TestEnableAssignsCurrentScale()
        {
            ScaleMenu.Instance.Enable(selectedObject);

            Assert.That(FindScaleInput("XScale").GetValue(), Is.EqualTo(1.25f));
            Assert.That(FindScaleInput("YScale").GetValue(), Is.EqualTo(1.75f));
        }

        /// <summary>
        /// Verifies that assigning a new object scale refreshes both scale controls.
        /// </summary>
        [Test]
        public void TestAssignValueRefreshesScaleControls()
        {
            ScaleMenu.Instance.Enable(selectedObject);

            selectedObject.transform.localScale = new Vector3(2.25f, 2.5f, 1);
            ScaleMenu.Instance.AssignValue(selectedObject);

            Assert.That(FindScaleInput("XScale").GetValue(), Is.EqualTo(2.25f));
            Assert.That(FindScaleInput("YScale").GetValue(), Is.EqualTo(2.5f));
        }

        /// <summary>
        /// Verifies that proportional scaling is enabled when the menu is opened.
        /// </summary>
        [Test]
        public void TestProportionalScalingIsEnabledInitially()
        {
            ScaleMenu.Instance.Enable(selectedObject);

            InputFieldWithButtons xScale = FindScaleInput("XScale");
            InputFieldWithButtons yScale = FindScaleInput("YScale");

            Assert.That(xScale.OnProportionalValueChanged, Is.Not.Null);
            Assert.That(yScale.OnProportionalValueChanged, Is.Not.Null);
        }

        /// <summary>
        /// Verifies that the finish button is hidden during regular Drawable scaling.
        /// </summary>
        [Test]
        public void TestFinishButtonIsHiddenInRegularMode()
        {
            ScaleMenu.Instance.Enable(selectedObject);

            Assert.That(FindButton("Done").gameObject.activeSelf, Is.False);
        }

        /// <summary>
        /// Verifies that the return button is hidden when no return callback is provided.
        /// </summary>
        [Test]
        public void TestReturnButtonIsHiddenWithoutCallback()
        {
            ScaleMenu.Instance.Enable(selectedObject);

            Assert.That(FindButton("ReturnBtn").gameObject.activeSelf, Is.False);
        }

        /// <summary>
        /// Verifies that the finish state in sticky-note mode can only be consumed once.
        /// </summary>
        [Test]
        public void TestStickyNoteFinishCanOnlyBeConsumedOnce()
        {
            ScaleMenu.Instance.Enable(selectedObject, true);

            ButtonManagerBasic doneButton = FindButton("Done");
            Assert.That(doneButton.gameObject.activeSelf, Is.True);

            doneButton.clickEvent.Invoke();

            Assert.That(ScaleMenu.Instance.TryGetFinish(out bool firstFinish), Is.True);
            Assert.That(firstFinish, Is.True);

            Assert.That(ScaleMenu.Instance.TryGetFinish(out bool secondFinish), Is.False);
            Assert.That(secondFinish, Is.False);
        }

        /// <summary>
        /// Verifies that the return button invokes the provided callback.
        /// </summary>
        [Test]
        public void TestReturnButtonInvokesCallback()
        {
            bool callbackInvoked = false;
            UnityAction returnCall = () => callbackInvoked = true;

            ScaleMenu.Instance.Enable(selectedObject, returnCall: returnCall);

            ButtonManagerBasic returnButton = FindButton("ReturnBtn");
            Assert.That(returnButton.gameObject.activeSelf, Is.True);

            returnButton.clickEvent.Invoke();

            Assert.That(callbackInvoked, Is.True);
        }

        /// <summary>
        /// Finds a scale input field with the given GameObject name.
        /// </summary>
        /// <param name="name">The name of the scale input field.</param>
        /// <returns>The matching scale input field.</returns>
        private static InputFieldWithButtons FindScaleInput(string name)
        {
            foreach (InputFieldWithButtons candidate in Object.FindObjectsByType<InputFieldWithButtons>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Could not find the scale input field '{name}'.");
            return null;
        }

        /// <summary>
        /// Finds a button with the given GameObject name.
        /// </summary>
        /// <param name="name">The name of the button.</param>
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

            Assert.Fail($"Could not find the button '{name}'.");
            return null;
        }
    }
}
