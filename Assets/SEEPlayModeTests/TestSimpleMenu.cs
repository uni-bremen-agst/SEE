using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Menu
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
        [LoadScene]
        public IEnumerator TestMenuOption1()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(MenuTitle, OptionOne);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(1, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting option 2.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestMenuOption2()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(MenuTitle, OptionTwo);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(2, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting no option at all.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator TestMenuNoOption()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressCloseButton(MenuTitle);
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
            menu.Icon = GetIconSprite();

            IEnumerable<MenuEntry> menuEntries = new List<MenuEntry>
            {
                new(SelectAction: () => selection = 1,
                    Title: OptionOne,
                    Description: "Select option 1",
                    EntryColor: Color.red,
                    Icon: ExampleIcon),
                new(SelectAction: () => selection = 2,
                    Title: OptionTwo,
                    Description: "Select option 2",
                    EntryColor: Color.green,
                    Icon: ExampleIcon),
            };

            menu.AddEntries(menuEntries);
            menu.ShowMenu = true;
        }
    }
}
