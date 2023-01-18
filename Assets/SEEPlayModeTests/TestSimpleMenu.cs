using NUnit.Framework;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Test cases for <see cref="SimpleMenu"/>.
    /// </summary>
    internal class TestSimpleMenu : TestMenu
    {
        /// <summary>
        /// Title of option 1 in the menu.
        /// </summary>
        private const string OptionOne = "Option 1";
        /// <summary>
        /// Title of option 2 in the menu.
        /// </summary>
        private const string OptionTwo = "Option 2";
        /// <summary>
        /// Title of the menu.
        /// </summary>
        private const string MenuTitle = "Test Menu";

        /// <summary>
        /// The index of the selected option.
        /// </summary>
        private int selection = 0;

        /// <summary>
        /// The game object holding the <see cref="menu"/>.
        /// </summary>
        private GameObject menuGO;
        /// <summary>
        /// The menu to be tested.
        /// </summary>
        private SimpleMenu menu;

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
        public new IEnumerator Setup()
        {
            selection = 0;
            CreateMenu(out menuGO, out menu);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
        }

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
        private const float TimeUntilMenuIsSetup = 1f;

        /// <summary>
        /// Test for selecting option 1.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        public IEnumerator TestSimpleMenuOption1()
        {
            PressButton(menu.Title, OptionOne);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(1, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting option 2.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        public IEnumerator TestSimpleMenuOption2()
        {
            PressButton(menu.Title, OptionTwo);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(2, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting no option at all.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        public IEnumerator TestSimpleMenuNoOption()
        {
            PressCloseButton(menu.Title);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(0, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Creates a new <paramref name="menuGO"/> game object holding a new <paramref name="menu"/>.
        /// The <paramref name="menu"/> has two options (<see cref="OptionOne"/> and <see cref="OptionTwo"/>.
        /// </summary>
        /// <param name="menuGO">new game object holding <paramref name="menu"/></param>
        /// <param name="menu">a new menu that can be tested</param>
        private void CreateMenu(out GameObject menuGO, out SimpleMenu menu)
        {
            menuGO = new GameObject { name = "Container for menu" };
            menu = menuGO.AddComponent<SimpleMenu>();
            menu.AllowNoSelection(true);
            menu.Title = MenuTitle;
            menu.Description = "Tests the menu";
            menu.HideAfterSelection(true);
            menu.Icon = GetIcon();

            IEnumerable<MenuEntry> menuEntries = new List<MenuEntry>
            {
                new MenuEntry(action: new UnityAction(() => { Debug.Log("Selection 1\n");  selection = 1; }),
                              title: OptionOne,
                              description: "Select option 1",
                              entryColor: Color.red,
                              enabled: true,
                              icon: GetIcon()),
                new MenuEntry(action: new UnityAction(() => { Debug.Log("Selection 2\n");  selection = 2; }),
                              title: OptionTwo,
                              description: "Select option 2",
                              entryColor: Color.green,
                              enabled: true,
                              icon: GetIcon()),
            };

            menu.AddEntries(menuEntries);
            menu.ShowMenu(true);
        }
    }
}
