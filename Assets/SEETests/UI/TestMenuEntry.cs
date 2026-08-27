using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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
            Assert.That(entry.Description, Is.Null);
            Assert.That(entry.Title, Is.EqualTo("Test"));
            Assert.That(entry.Enabled, Is.True);
            Assert.That(entry.Icon, Is.EqualTo(' '));
            Assert.That(entry.EntryColor, Is.EqualTo(default(Color)));
            Assert.That(entry.DisabledColor, Is.Not.EqualTo(default(Color)), "Entry color must differ from disabled color!");
            Assert.That(testItems.Count, Is.EqualTo(0), "DoAction() may not be called during initialization!");
            entry.SelectAction();
            Assert.That(testItems.Count, Is.EqualTo(1), "DoAction() must call the given UnityAction!");
        }

        [Test, TestCaseSource(nameof(ValidConstructorSupplier))]
        public void TestConstructor(Action action, string title, string description,
                                    Color entryColor, bool enabled, char icon)
        {
            MenuEntry entry = CreateMenuEntry(action, title, description, entryColor, enabled, icon);
            // Given action must either be null or NOP for this test
            if (action == null)
            {
                Assert.That(entry.SelectAction, Is.Null);
            }
            else
            {
                Assert.That(() => entry.SelectAction(), Throws.Nothing);
            }

            Assert.That(entry.Description, Is.EqualTo(description));
            Assert.That(entry.Title, Is.EqualTo(title));
            Assert.That(entry.Enabled, Is.EqualTo(enabled));
            Assert.That(entry.Icon, Is.EqualTo(icon));
            Assert.That(entry.EntryColor, Is.EqualTo(entryColor));
        }
    }
}
