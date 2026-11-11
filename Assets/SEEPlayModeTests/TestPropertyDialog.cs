using Michsky.MUIP;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tests for property dialog components.
/// </summary>
namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// Test cases for <see cref="PropertyDialog"/>.
    /// </summary>
    internal class TestPropertyDialog : TestUI
    {
        /// <summary>
        /// Dialog owners that must be destroyed even after a failed assertion.
        /// </summary>
        private readonly List<GameObject> dialogOwners = new();

        /// <summary>
        /// Canvases belonging to the scene before a test creates its dialogs.
        /// </summary>
        private HashSet<Canvas> previousCanvases;

        /// <summary>
        /// Records existing canvases so that cleanup preserves the original scene UI.
        /// </summary>
        /// <returns>An enumerator that completes without waiting.</returns>
        [UnitySetUp]
        public IEnumerator RecordExistingCanvases()
        {
            previousCanvases = new(UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));
            yield break;
        }

        /// <summary>
        /// Removes dialog owners and canvases created by the test.
        /// </summary>
        /// <returns>An enumerator that waits for managed UI and canvas destruction.</returns>
        [UnityTearDown]
        public IEnumerator DestroyDialogObjects()
        {
            foreach (GameObject owner in dialogOwners)
            {
                if (owner != null)
                {
                    UnityEngine.Object.Destroy(owner);
                }
            }
            dialogOwners.Clear();
            yield return null;
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!previousCanvases.Contains(canvas))
                {
                    UnityEngine.Object.Destroy(canvas.gameObject);
                }
            }
            yield return null;
        }

        /// <summary>
        /// Checks entered text, selection changes, and the dialog confirmation callback.
        /// </summary>
        /// <returns>An enumerator that waits for dialog initialization and confirmation.</returns>
        /// <exception cref="AssertionException">The dialog content or confirmation differs from the expected result.</exception>
        [UnityTest]
        public IEnumerator TestDialog()
        {
            // Set up the dialog.
            GameObject gameObject = new("Dialog");
            dialogOwners.Add(gameObject);

            StringProperty stringProperty = gameObject.AddComponent<StringProperty>();
            stringProperty.Name = "Enter your name";
            stringProperty.Description = "Your first and last name.";

            SelectionProperty selectionProperty = gameObject.AddComponent<SelectionProperty>();
            selectionProperty.Name = "Make your choice";
            selectionProperty.Description = "Select a single option of this list.";
            IList<string> options = new List<string> { "first", "second", "third"};
            selectionProperty.AddOptions(options);
            selectionProperty.Value = options[1];

            PropertyGroup group = gameObject.AddComponent<PropertyGroup>();
            group.Name = "Personal data";
            group.Icon = Resources.Load<Sprite>("Logos/Uni-Bremen");
            group.AddProperty(stringProperty);
            group.AddProperty(selectionProperty);

            PropertyDialog dialog = gameObject.AddComponent<PropertyDialog>();
            dialog.Title = "Fact Sheet";
            dialog.Description = "All your data";
            dialog.AddGroup(group);

            // Set up the call to occur when the OK button was pressed.
            bool CallbackHasOccurred = false;
            dialog.OnConfirm.AddListener(() => CallbackHasOccurred = true);
            // Go online.
            dialog.DialogShouldBeShown = true;
            yield return new WaitForSeconds(1f);

            GameObject canvas = GameObject.Find("UI Canvas");
            Assert.That(canvas, Is.Not.Null, "There is no UI Canvas.");

            // Simulate entering the text in the input field.
            GameObject stringPropertyGameObject = GameObject.Find(stringProperty.Name);
            Assert.That(stringPropertyGameObject, Is.Not.Null,
                        $"There is no game object named {stringProperty.Name}.");
            TMP_InputField textField = GetInputField(stringPropertyGameObject);
            Assert.That(textField, Is.Not.Null,
                        $"{stringProperty.Name} has no {nameof(TMP_InputField)} component.");
            textField.text = "Expected Value";

            // Simulate forward clicking of the selector (twice).
            GameObject selectionPropertyGameObject = GameObject.Find(selectionProperty.Name);
            Assert.That(selectionPropertyGameObject, Is.Not.Null,
                        $"There is no game object named {selectionProperty.Name}.");
            HorizontalSelector selector = GetHorizontalSelector(selectionPropertyGameObject);
            Assert.That(selector, Is.Not.Null,
                        $"{selectionProperty.Name} has no {nameof(HorizontalSelector)} component.");
            // We have three options and we have initially set the second option, so we can move
            // forward once maximally.
            selector.ForwardClick();
            Assert.That(selectionProperty.Value, Is.EqualTo(options[2]));
            selector.PreviousClick();
            Assert.That(selectionProperty.Value, Is.EqualTo(options[1]));
            selector.PreviousClick();
            Assert.That(selectionProperty.Value, Is.EqualTo(options[0]));

            // Simulate that the OK button is pressed by the user.
            GameObject okButton = GameObject.Find("OK");
            Assert.That(okButton, Is.Not.Null, "There is no OK button.");
            Assert.That(okButton.TryGetComponent(out Button button), Is.True,
                        $"The OK button has no {nameof(Button)} component.");
            ExecuteEvents.Execute(okButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return new WaitForEndOfFrame();

            // The entered text must be present.
            Assert.That(textField.text, Is.EqualTo(stringProperty.Value));
            // The callback has occurred.
            Assert.That(CallbackHasOccurred, Is.True,
                        "The OnConfirm callback must have been invoked.");
        }

        /// <summary>
        /// Checks that dialog selections with one or two options initialize their indicators and values.
        /// </summary>
        /// <returns>An enumerator that waits for each dialog to initialize and close.</returns>
        /// <exception cref="AssertionException">A selection has an incorrect value or indicator count.</exception>
        [UnityTest]
        public IEnumerator DialogSupportsShortSelections()
        {
            for (int optionCount = 1; optionCount <= 2; optionCount++)
            {
                GameObject container = new("Dialog with short selection");
                dialogOwners.Add(container);
                SelectionProperty selection = container.AddComponent<SelectionProperty>();
                selection.Name = "Short selection";
                List<string> options = new() { "First" };
                if (optionCount == 2)
                {
                    options.Add("Second");
                }
                selection.AddOptions(options);
                selection.Value = options[optionCount - 1];

                PropertyGroup group = container.AddComponent<PropertyGroup>();
                group.Name = "Selection";
                group.AddProperty(selection);
                PropertyDialog dialog = container.AddComponent<PropertyDialog>();
                dialog.Title = "Short selection dialog";
                dialog.AddGroup(group);
                dialog.DialogShouldBeShown = true;
                float initializationDeadline = Time.realtimeSinceStartup + 5f;
                while ((selection.HorizontalSelector == null
                        || selection.HorizontalSelector.indicatorParent == null
                        || selection.HorizontalSelector.indicatorParent.childCount != optionCount
                        || selection.Value != options[optionCount - 1])
                       && Time.realtimeSinceStartup < initializationDeadline)
                {
                    yield return null;
                }

                HorizontalSelector selector = selection.HorizontalSelector;
                Assert.That(selector, Is.Not.Null);
                Assert.That(selector.indicatorParent.childCount, Is.EqualTo(optionCount));
                Assert.That(selection.Value, Is.EqualTo(options[optionCount - 1]));
                selector.PreviousClick();
                Assert.That(selection.Value, Is.EqualTo(options[0]));
                selector.ForwardClick();
                Assert.That(selection.Value, Is.EqualTo(options[optionCount - 1]));

                dialog.DialogShouldBeShown = false;
                UnityEngine.Object.Destroy(container);
                yield return null;
            }
        }

        /// <summary>
        /// Yields the <see cref="TMP_InputField"/> of <paramref name="field"/>.
        /// </summary>
        /// <param name="field">Game object from which to retrieve the component.</param>
        /// <returns>The retrieved component.</returns>
        /// <exception cref="Exception">Thrown in case <paramref name="field"/> does not have
        /// the requested component.</exception>
        private static TMP_InputField GetInputField(GameObject field)
        {
            if (field.TryGetComponent(out TMP_InputField inputField))
            {
                return inputField;
            }
            else
            {
                throw new Exception($"Input field {field.name} does not have a {typeof(TMP_InputField)}");
            }
        }

        /// <summary>
        /// Yields the <see cref="HorizontalSelector"/> of <paramref name="field"/>.
        /// </summary>
        /// <param name="field">Game object from which to retrieve the component.</param>
        /// <returns>The retrieved component.</returns>
        /// <exception cref="Exception">Thrown in case <paramref name="field"/> does not have
        /// the requested component.</exception>
        private static HorizontalSelector GetHorizontalSelector(GameObject field)
        {
            if (field.TryGetComponent(out HorizontalSelector horizontalSelector))
            {
                return horizontalSelector;
            }
            else
            {
                throw new Exception($"Selector field {field.name} does not have a {typeof(HorizontalSelector)}");
            }
        }
    }
}
