using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// Test cases for <see cref="PropertyDialog"/>.
    /// </summary>
    internal class TestPropertyDialog : TestUI
    {
        [UnityTest]
        public IEnumerator TestDialog()
        {
            LogAssert.ignoreFailingMessages = true;

            // Set up the dialog.
            GameObject gameObject = new GameObject("Dialog");

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
        /// Yields the <see cref="TMP_InputField"/> of <paramref name="field"/>.
        /// </summary>
        /// <param name="field">game object from which to retrieve the component</param>
        /// <returns>the retrieved component</returns>
        /// <exception cref="Exception">thrown in case <paramref name="field"/> does not have
        /// the requested component</exception>
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
        /// <param name="field">game object from which to retrieve the component</param>
        /// <returns>the retrieved component</returns>
        /// <exception cref="Exception">thrown in case <paramref name="field"/> does not have
        /// the requested component</exception>
        static HorizontalSelector GetHorizontalSelector(GameObject field)
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
