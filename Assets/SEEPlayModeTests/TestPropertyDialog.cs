using System.Collections;
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
            // A player-settings object must be present in the scene.
            GameObject playerSettings = new GameObject("Player Settings");
            playerSettings.AddComponent<PlayerSettings>().playerInputType = GO.PlayerInputType.DesktopPlayer;
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

            PropertyGroup group = gameObject.AddComponent<PropertyGroup>();
            group.Name = "Personal data";
            group.Icon = Resources.Load<Sprite>("Logos/Uni-Bremen");
            group.AddProperty(stringProperty);

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

            // Simulate entering the text in the input field.
            GameObject canvas = GameObject.Find("UI Canvas");
            Assert.NotNull(canvas);
            GameObject stringPropertyGameObject = GameObject.Find(stringProperty.Name);
            Assert.NotNull(stringPropertyGameObject);
            Transform textField = stringPropertyGameObject.transform.Find("Text Area/Text");
            Assert.NotNull(textField);
            Assert.That(textField.TryGetComponent(out TextMeshProUGUI text));
            text.text = "Expected Value";

            // Simulate that the OK button is pressed by the user.
            GameObject okButton = GameObject.Find("OK");
            Assert.NotNull(okButton);
            Assert.That(okButton.TryGetComponent(out Button button));
            ExecuteEvents.Execute(okButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return new WaitForEndOfFrame();

            // The entered text must be present.
            Assert.AreEqual(stringProperty.Value, text.text);
            // The call back has occurred.
            Assert.That(CallbackHasOccurred);
        }
    }
}
