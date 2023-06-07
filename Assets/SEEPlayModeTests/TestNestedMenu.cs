﻿using NUnit.Framework;
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
        /// The selection value to be set when <see cref="OptionOne"/> is selected.
        /// </summary>
        private const int OptionOneValue = 1;

        /// <summary>
        /// Title of the first option in the sub menu.
        /// </summary>
        private const string NestedOptionOne = "Option 2a";
        /// <summary>
        /// The selection value to be set when <see cref="NestedOptionOne"/> is selected.
        /// </summary>
        private const int NestedOptionOneValue = 2;

        /// <summary>
        /// Title of the second option in the submenu.
        /// </summary>
        private const string NestedOptionTwo = "Option 2b";
        /// <summary>
        /// The selection value to be set when <see cref="NestedOptionTwo"/> is selected.
        /// </summary>
        private const int NestedOptionTwoValue = 3;

        /// <summary>
        /// Title of the menu.
        /// </summary>
        private const string MenuTitle = "Test Menu";
        /// <summary>
        /// Title of the submenu.
        /// </summary>
        private const string SubMenuTitle = "Submenu";

        /// <summary>
        /// Test for selecting <see cref="OptionOne"/>.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        [LoadScene()]
        public IEnumerator TestMenuOption1()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(MenuTitle, OptionOne);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(OptionOneValue, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting <see cref="NestedOptionOne"/> of the submenu.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        [LoadScene()]
        public IEnumerator TestMenuNestedOptionOne()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(MenuTitle, SubMenuTitle);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(SubMenuTitle, NestedOptionOne);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
             Assert.AreEqual(NestedOptionOneValue, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting <see cref="NestedOptionTwo"/> of the submenu.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        [LoadScene()]
        public IEnumerator TestMenuNestedOptionTwo()
        {
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(MenuTitle, SubMenuTitle);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            PressButton(SubMenuTitle, NestedOptionTwo);
            yield return new WaitForSeconds(TimeUntilMenuIsSetup);
            Assert.AreEqual(NestedOptionTwoValue, selection);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Test for selecting no option at all.
        /// </summary>
        /// <returns><see cref="WaitForEndOfFrame"/></returns>
        [UnityTest]
        [LoadScene()]
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
        /// The <paramref name="menu"/> has one options (<see cref="OptionOne"/> and  a nested
        /// menu offering two more options <see cref="NestedOptionOne"/> and <see cref="NestedOptionTwo"/>.
        /// </summary>
        /// <param name="menuGO">new game object holding <paramref name="menu"/></param>
        /// <param name="menu">a new menu that can be tested</param>
        protected override void CreateMenu(out GameObject menuGO, out SimpleListMenu<MenuEntry> menu)
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
                new MenuEntry(selectAction: new UnityAction(() => { selection = OptionOneValue; }),
                              unselectAction: null,
                              title: OptionOne,
                              description: "Select option 1",
                              entryColor: Color.red,
                              enabled: true,
                              icon: GetIcon()),
                new NestedMenuEntry<MenuEntry>(innerEntries: new List<MenuEntry>()
                                                      {
                                                         new MenuEntry(selectAction: new UnityAction(() => { selection = NestedOptionOneValue; }),
                                                                       unselectAction: null,
                                                                       title: NestedOptionOne,
                                                                       description: "Select option 2a",
                                                                       entryColor: Color.green,
                                                                       enabled: true,
                                                                       icon: GetIcon()),
                                                         new MenuEntry(selectAction: new UnityAction(() => { selection = NestedOptionTwoValue; }),
                                                                       unselectAction: null,
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
