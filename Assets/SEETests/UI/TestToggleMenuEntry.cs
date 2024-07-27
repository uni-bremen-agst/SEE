using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu
{
    /// <summary>
    /// Tests for the <see cref="MenuEntry"/> class.
    /// Checks whether the unselect callbacks are called properly.
    /// </summary>
    [TestFixture]
    internal class TestToggleMenuEntry: TestMenuEntry
    {
        protected override MenuEntry CreateMenuEntry(Action action, string title, string description = null,
                                                     Color entryColor = default, bool enabled = true,
                                                     char icon = '#')
        {
            return new MenuEntry(action, title, null, description, entryColor, enabled, icon);
        }

        [Test]
        public void TestDefaultExitAction()
        {
            MenuEntry entry1 = new(() => {}, "Test");
            MenuEntry entry2 = new(() => {}, "Test");
            Assert.DoesNotThrow(() => entry1.SelectAction());
            Assert.DoesNotThrow(() => entry2.SelectAction());
        }

        [Test]
        public void TestExitAction()
        {
            List<bool> testItems = new();
            // It is not allowed to create a MonoBehaviour with new. SelectionMenu derives from MonoBehaviour.
            // That is why we create a component and add a SelectionMenu to it.
            GameObject go = new("Test");
            SelectionMenu selectionMenu = go.AddComponent<SelectionMenu>();
            void ExitAction() => testItems.Add(true);
            MenuEntry entry = new(() => {}, "Test", ExitAction);
            selectionMenu.AddEntry(entry);
            Assert.AreNotEqual(entry, selectionMenu.ActiveEntry, "SelectionMenu.ActiveEntry isn't set correctly!");
            Assert.AreEqual(0, testItems.Count, "Entry/ExitAction may not be called during initialization!");
            selectionMenu.ActiveEntry = entry;
            Assert.AreEqual(entry, selectionMenu.ActiveEntry, "SelectionMenu.ActiveEntry isn't set correctly.");
            Assert.AreEqual(0, testItems.Count, "ExitAction isn't called correctly!");
            selectionMenu.ActiveEntry = null;
            Assert.AreNotEqual(entry, selectionMenu.ActiveEntry, "SelectionMenu.ActiveEntry isn't set correctly!");
            Assert.AreEqual(1, testItems.Count, "ExitAction isn't called correctly!");
            selectionMenu.ActiveEntry = entry;
            Assert.AreEqual(entry, selectionMenu.ActiveEntry, "SelectionMenu.ActiveEntry isn't set correctly!");
            Assert.AreEqual(1, testItems.Count, "ExitAction isn't called correctly!");
            selectionMenu.ActiveEntry = null;
            Assert.AreNotEqual(entry, selectionMenu.ActiveEntry, "SelectionMenu.ActiveEntry isn't set correctly!");
            Assert.AreEqual(2, testItems.Count, "ExitAction isn't called correctly!");
            Destroyer.Destroy(go);
        }
    }
}
