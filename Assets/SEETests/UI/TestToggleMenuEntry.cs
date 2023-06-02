using System.Collections.Generic;
using NUnit.Framework;
using SEE.Game.UI.Menu;
using UnityEngine;
using UnityEngine.Events;

namespace SEETests.UI
{
    /// <summary>
    /// Tests for the <see cref="MenuEntry"/> class.
    /// Checks whether the unselect callbacks are called properly.
    /// </summary>
    [TestFixture]
    internal class TestToggleMenuEntry: TestMenuEntry
    {
        protected override MenuEntry CreateMenuEntry(UnityAction action, string title, string description = null,
                                                     Color entryColor = default, bool enabled = true,
                                                     Sprite icon = null)
        {
            return new MenuEntry(action, null, title, description, entryColor, enabled, icon);
        }

        [Test]
        public void TestDefaultExitAction()
        {
            MenuEntry entry1 = new( () => {}, null, "Test");
            MenuEntry entry2 = new( () => {}, null, "Test");
            Assert.DoesNotThrow(() => entry1.SelectAction());
            Assert.DoesNotThrow(() => entry2.SelectAction());
        }

        [Test]
        public void TestExitAction()
        {
            List<bool> testItems = new();
            SelectionMenu selectionMenu = new();
            void ExitAction() => testItems.Add(true);
            MenuEntry entry = new(() => {}, ExitAction, "Test");
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
        }
    }
}
