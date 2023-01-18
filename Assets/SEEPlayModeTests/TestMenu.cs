using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Common abstract super class for menu tests.
    /// </summary>
    internal abstract class TestMenu : TestUI
    {
        /// <summary>
        /// Path to a sprite we can use for testing.
        /// </summary>
        private const string PathOfIcon = "Materials/Charts/MoveIcon";

        /// <summary>
        /// The icon loaded from <see cref="PathOfIcon"/>.
        /// </summary>
        /// <returns>icon loaded from <see cref="PathOfIcon"/></returns>
        protected static Sprite GetIcon()
        {
            return Resources.Load<Sprite>(PathOfIcon);
        }

        /// <summary>
        /// Simulates pressing the button representing the option <paramref name="optionTitle"/>.
        /// </summary>
        /// <param name="menuTitle">title of the selection menu</param>
        /// <param name="optionTitle">relative name of game object holding a <see cref="Button"/> component
        /// and representing a selection option</param>
        protected static void PressButton(string menuTitle, string optionTitle)
        {
            PressButton($"/UI Canvas/{menuTitle}/Main Content/Content Mask/Content/Menu Entries/Scroll Area/List/{optionTitle}");
        }
        /// <summary>
        /// Simulates that a user presses the button identified by <paramref name="buttonPath"/>.
        /// </summary>
        /// <param name="buttonPath">the path name of the game object holding a <see cref="Button"/> component</param>
        private static void PressButton(string buttonPath)
        {
            // Retrieve the button
            GameObject buttonObject = GameObject.Find(buttonPath);
            Assert.NotNull(buttonObject);
            // Make sure the object is really holding a button.
            Assert.That(buttonObject.TryGetComponent(out Button _));
            // Press the button.
            ExecuteEvents.Execute(buttonObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        /// <summary>
        /// Simulates pressing the Close button.
        /// </summary>
        /// <param name="menuTitle">title of the selection menu</param>
        protected static void PressCloseButton(string menuTitle)
        {
            PressButton($"/UI Canvas/{menuTitle}/Main Content/Buttons/Content/Close");
        }
    }
}