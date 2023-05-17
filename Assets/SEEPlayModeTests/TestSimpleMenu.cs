using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Test cases for <see cref="SimpleListMenu"/>.
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

        /// Test for selecting option 1.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        public IEnumerator TestSimpleMenuOption1()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
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
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
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
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
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
        protected override void CreateMenu(out GameObject menuGO, out SimpleListMenu<MenuEntry> menu)
        {
            menuGO = new GameObject { name = "Container for menu" };
            menu = menuGO.AddComponent<SimpleListMenu>();
            menu.AllowNoSelection = true;
            menu.Title = MenuTitle;
            menu.Description = "Tests the menu";
            menu.HideAfterSelection = true;
            menu.Icon = GetIcon();

            IEnumerable<MenuEntry> menuEntries = new List<MenuEntry>
            {
                new MenuEntry(action: new UnityAction(() => { selection = 1; }),
                              title: OptionOne,
                              description: "Select option 1",
                              entryColor: Color.red,
                              enabled: true,
                              icon: GetIcon()),
                new MenuEntry(action: new UnityAction(() => { selection = 2; }),
                              title: OptionTwo,
                              description: "Select option 2",
                              entryColor: Color.green,
                              enabled: true,
                              icon: GetIcon()),
            };

            menu.AddEntries(menuEntries);
            menu.ShowMenu = true;
        }
    }
}
