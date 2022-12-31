using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// Test cases for <see cref="PropertyDialog"/>.
    /// </summary>
    internal class TestPropertyDialog
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneSettings.InputType = GO.PlayerInputType.DesktopPlayer;
            yield return null;
        }

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
            Assert.NotNull(canvas);

            // Simulate entering the text in the input field.
            GameObject stringPropertyGameObject = GameObject.Find(stringProperty.Name);
            Assert.NotNull(stringPropertyGameObject);
            TMP_InputField textField = GetInputField(stringPropertyGameObject);
            Assert.NotNull(textField);
            textField.text = "Expected Value";

            // Simulate forward clicking of the selector (twice).
            GameObject selectionPropertyGameObject = GameObject.Find(selectionProperty.Name);
            Assert.NotNull(selectionPropertyGameObject);
            HorizontalSelector selector = GetHorizontalSelector(selectionPropertyGameObject);
            Assert.NotNull(selectionPropertyGameObject);
            // We have three options and we have initially set the second option, so we can move
            // forward once maximally.
            selector.ForwardClick();
            Assert.AreEqual(options[2], selectionProperty.Value);
            selector.PreviousClick();
            Assert.AreEqual(options[1], selectionProperty.Value);
            selector.PreviousClick();
            Assert.AreEqual(options[0], selectionProperty.Value);

            // Simulate that the OK button is pressed by the user.
            GameObject okButton = GameObject.Find("OK");
            Assert.NotNull(okButton);
            Assert.That(okButton.TryGetComponent(out Button button));
            ExecuteEvents.Execute(okButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return new WaitForEndOfFrame();

            // The entered text must be present.
            Assert.AreEqual(stringProperty.Value, textField.text);
            // The callback has occurred.
            Assert.That(CallbackHasOccurred);
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
