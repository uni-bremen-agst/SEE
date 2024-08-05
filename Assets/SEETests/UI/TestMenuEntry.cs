using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu
{
    /// <summary>
    /// Tests for the <see cref="MenuEntry"/> class.
    /// </summary>
    internal class TestMenuEntry
    {
        /// <summary>
        /// An icon used for testing.
        /// </summary>
        private const char testIcon = '!';

        protected static IEnumerable<TestCaseData> ValidConstructorSupplier()
        {
            yield return new TestCaseData(new Action(() => { }), "Test", "Test description", Color.red,
                                          true, testIcon);
            yield return new TestCaseData(null, "Test", "Test description", Color.green,
                                          true, testIcon);
            yield return new TestCaseData(new Action(() => { }), "Test", null, Color.blue,
                                          true, testIcon);
            yield return new TestCaseData(new Action(() => { }), "Test", "Test description", null,
                                          true, testIcon);
            yield return new TestCaseData(new Action(() => { }), "Test", "Test description", Color.white,
                                          false, testIcon);
            yield return new TestCaseData(new Action(() => { }), "Test", "Test description", Color.black,
                                          true, ' ');
            yield return new TestCaseData(null, "Test", null, null, true, ' ');
        }

        /// <summary>
        /// Creates a new MenuEntry, calling the constructor with the given parameters.
        /// </summary>
        /// <returns>The newly constructed MenuEntry.</returns>
        protected virtual MenuEntry CreateMenuEntry(Action action, string title, string description = null,
                                                    Color entryColor = default, bool enabled = true, char icon = ' ')
        {
            return new MenuEntry(action, title, null, description, entryColor, enabled, icon);
        }

        [Test]
        public void TestConstructorDefault()
        {
            List<int> testItems = new();
            void Action() => testItems.Add(1);
            MenuEntry entry = CreateMenuEntry(Action, "Test");
            Assert.AreEqual(null, entry.Description);
            Assert.AreEqual("Test", entry.Title);
            Assert.AreEqual(true, entry.Enabled);
            Assert.AreEqual(' ', entry.Icon);
            Assert.AreEqual(default(Color), entry.EntryColor);
#if INCLUDE_STEAM_VR

            Assert.AreNotEqual(default(Color), entry.DisabledColor, "Entry color must differ from disabled color!");
#endif
            Assert.AreEqual(0, testItems.Count, "DoAction() may not be called during initialization!");
            entry.SelectAction();
            Assert.AreEqual(1, testItems.Count, "DoAction() must call the given UnityAction!");
        }

        [Test, TestCaseSource(nameof(ValidConstructorSupplier))]
        public void TestConstructor(Action action, string title, string description,
                                    Color entryColor, bool enabled, char icon)
        {
            MenuEntry entry = CreateMenuEntry(action, title, description, entryColor, enabled, icon);
            // Given action must either be null or NOP for this test
            if (action == null)
            {
                Assert.IsNull(entry.SelectAction);
            }
            else
            {
                Assert.DoesNotThrow(() => entry.SelectAction());
            }

            Assert.AreEqual(description, entry.Description);
            Assert.AreEqual(title, entry.Title);
            Assert.AreEqual(enabled, entry.Enabled);
            Assert.AreEqual(icon, entry.Icon);
            Assert.AreEqual(entryColor, entry.EntryColor);
#if INCLUDE_STEAM_VR

            Debug.Log($"{entryColor}, {entry.DisabledColor}");
            Assert.AreNotEqual(entryColor, entry.DisabledColor, "Disabled color must differ from normal color!");
#endif
        }
    }
}
