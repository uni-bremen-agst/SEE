using NUnit.Framework;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Common abstract super class for menu tests.
    /// </summary>
    internal abstract class TestMenu : TestUI
    {
        /// <summary>
        /// The index of the selected option.
        /// </summary>
        protected int selection = 0;

        /// <summary>
        /// The game object holding the <see cref="menu"/>.
        /// </summary>
        protected GameObject menuGO;

        /// <summary>
        /// The menu to be tested.
        /// </summary>
        protected SimpleListMenu<MenuEntry> menu;

        /// <summary>
        /// Set up for every test.
        /// Resets <see cref="selection"/>.
        /// Sets up <see cref="menuGO"/> and <see cref="menu"/>.
        /// </summary>
        /// <returns>waiting <see cref="TimeUntilMenuIsSetup"/></returns>
        /// <remarks>
        /// Will be called after <see cref="TestUI.Setup"/>.
        /// Method must be public. Otherwise it will not be called by the test framework.
        /// </remarks>
        [UnitySetUp]
        public new IEnumerator SetUp()
        {
            selection = 0;
            CreateMenu(out menuGO, out menu);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
        }

        /// <summary>
        /// Creates a new <paramref name="menuGO"/> game object holding a
        /// new <paramref name="menu"/>, which is the menu to be tested.
        /// </summary>
        /// <param name="menuGO">new game object holding <paramref name="menu"/></param>
        /// <param name="menu">a new menu that can be tested</param>
        protected abstract void CreateMenu(out GameObject menuGO, out SimpleListMenu<MenuEntry> menu);

        /// <summary>
        /// Tear down after every test.
        /// Destroys <see cref="menuGO"/> and <see cref="menu"/>.
        /// </summary>
        /// <returns>waiting <see cref="TimeUntilMenuIsSetup"/></returns>
        /// <remarks>
        /// Will be called before <see cref="TestUI.TearDown"/>.
        /// Method must be public. Otherwise it will not be called by the test framework.
        /// </remarks>
        [UnityTearDown]
        public new IEnumerator TearDown()
        {
            Destroyer.Destroy(menuGO);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
        }

        /// <summary>
        /// The time it takes until the menu is up and running in seconds.
        /// </summary>
        protected const float TimeUntilMenuIsSetup = 1f;

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