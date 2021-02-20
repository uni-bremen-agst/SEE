using System.Collections.Generic;
using NUnit.Framework;
using SEE.Game.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SEETests.UI
{
    [TestFixture]
    internal class TestToggleMenuEntry: TestMenuEntry
    {
        protected override MenuEntry CreateMenuEntry(UnityAction action, string title, string description = null, 
                                                     Color entryColor = default, bool enabled = true,
                                                     Sprite icon = null)
        {
            return new ToggleMenuEntry(false, action, null, title, description, entryColor, icon, enabled);
        }

        [Test]
        public void TestDefaultExitAction()
        {
            ToggleMenuEntry entry1 = new ToggleMenuEntry(false, () => {}, null, "Test");
            ToggleMenuEntry entry2 = new ToggleMenuEntry(true, () => {}, null, "Test");
            Assert.DoesNotThrow(() => entry1.DoAction());
            Assert.DoesNotThrow(() => entry2.DoAction());
        }

        [Test]
        public void TestExitAction()
        {
            List<bool> testItems = new List<bool>();
            void ExitAction() => testItems.Add(true);
            ToggleMenuEntry entry = new ToggleMenuEntry(true, () => {}, ExitAction, "Test");
            Assert.IsTrue(entry.Active, "ToggleMenuEntry.Active isn't set correctly!");
            Assert.AreEqual(0, testItems.Count, "Entry/ExitAction may not be called during initialization!");
            entry.Active = true;
            Assert.IsTrue(entry.Active, "ToggleMenuEntry.Active isn't set correctly!");
            Assert.AreEqual(0, testItems.Count, "ExitAction isn't called correctly!");
            entry.Active = false;
            Assert.IsFalse(entry.Active, "ToggleMenuEntry.Active isn't set correctly!");
            Assert.AreEqual(1, testItems.Count, "ExitAction isn't called correctly!");
            entry.Active = true;
            Assert.IsTrue(entry.Active, "ToggleMenuEntry.Active isn't set correctly!");
            Assert.AreEqual(1, testItems.Count, "ExitAction isn't called correctly!");
            entry.Active = false;
            Assert.IsFalse(entry.Active, "ToggleMenuEntry.Active isn't set correctly!");
            Assert.AreEqual(2, testItems.Count, "ExitAction isn't called correctly!");
        }
    }
}