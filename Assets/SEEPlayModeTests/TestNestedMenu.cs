using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// Test cases for <see cref="NestedMenu"/>.
    /// </summary>
    internal class TestNestedMenu : TestMenu
    {
        /// <summary>
        /// Title of option 1 in the menu.
        /// </summary>
        private const string OptionOne = "Option 1";
        /// <summary>
        /// Title of the first option in the sub menu.
        /// </summary>
        private const string NestedOptionOne = "Option 2";
        /// <summary>
        /// Title of the second option in the submenu.
        /// </summary>
        private const string NestedOptionTwo = "Option 2";
        /// <summary>
        /// Title of the menu.
        /// </summary>
        private const string MenuTitle = "Test Menu";
        /// <summary>
        /// Title of the submenu.
        /// </summary>
        private const string SubMenuTitle = "Submenu";

        /// <summary>
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
        /// Test for selecting the first option of the submenu.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        public IEnumerator TestSimpleMenuOption2()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(menu.Title, SubMenuTitle);
            yield return new WaitForEndOfFrame();
            PressButton(menu.Title, NestedOptionOne);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(2, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting the second option of the submenu.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        public IEnumerator TestSimpleMenuOption3()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(menu.Title, SubMenuTitle);
            yield return new WaitForEndOfFrame();
            PressButton(menu.Title, NestedOptionTwo);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(3, selection);
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
        /// The <paramref name="menu"/> has one options (<see cref="OptionOne"/> and  a nested
        /// menu offering two more options <see cref="NestedOptionOne"/> and <see cref="NestedOptionTwo"/>.
        /// </summary>
        /// <param name="menuGO">new game object holding <paramref name="menu"/></param>
        /// <param name="menu">a new menu that can be tested</param>
        protected override void CreateMenu(out GameObject menuGO, out AbstractMenu<MenuEntry> menu)
        {
            menuGO = new GameObject { name = "Container for menu" };
            menu = menuGO.AddComponent<NestedListMenu>();
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
                new NestedMenuEntry<MenuEntry>(innerEntries: new List<MenuEntry>()
                                                      {
                                                         new MenuEntry(action: new UnityAction(() => { selection = 2; }),
                                                                       title: NestedOptionOne,
                                                                       description: "Select option 2a",
                                                                       entryColor: Color.green,
                                                                       enabled: true,
                                                                       icon: GetIcon()),
                                                         new MenuEntry(action: new UnityAction(() => { selection = 3; }),
                                                                       title: NestedOptionTwo,
                                                                       description: "Select option 2b",
                                                                       entryColor: Color.green,
                                                                       enabled: true,
                                                                       icon: GetIcon())
                                                      },
                                    title: SubMenuTitle,
                                    description: "open subselection 2",
                                    entryColor: Color.red,
                                    enabled: true,
                                    icon: GetIcon())
            };

            menu.AddEntries(menuEntries);
            menu.ShowMenu = true;
        }
    }
}
