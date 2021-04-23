using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Game.UI.Menu;
using UnityEngine;
using UnityEngine.Events;

namespace SEETests.UI
{
    /// <summary>
    /// Tests for the <see cref="MenuEntry"/> class.
    /// </summary>
    internal class TestMenuEntry
    {
        /// <summary>
        /// Path to a sprite we can use for testing.
        /// </summary>
        private const string TEST_SPRITE = "Materials/Charts/MoveIcon";

        protected static IEnumerable<TestCaseData> ValidConstructorSupplier()
        {
            Sprite testSprite = Resources.Load<Sprite>(TEST_SPRITE);
            yield return new TestCaseData(new UnityAction(() => { }), "Test", "Test description", Color.red,
                                          true, testSprite);
            yield return new TestCaseData(null, "Test", "Test description", Color.green,
                                          true, testSprite);
            yield return new TestCaseData(new UnityAction(() => { }), "Test", null, Color.blue,
                                          true, testSprite);
            yield return new TestCaseData(new UnityAction(() => { }), "Test", "Test description", null,
                                          true, testSprite);
            yield return new TestCaseData(new UnityAction(() => { }), "Test", "Test description", Color.white,
                                          false, testSprite);
            yield return new TestCaseData(new UnityAction(() => { }), "Test", "Test description", Color.black,
                                          true, null);
            yield return new TestCaseData(null, "Test", null, null, true, null);
        }

        /// <summary>
        /// Creates a new MenuEntry, calling the constructor with the given parameters.
        /// </summary>
        /// <returns>The newly constructed MenuEntry.</returns>
        protected virtual MenuEntry CreateMenuEntry(UnityAction action, string title, string description = null,
                                                    Color entryColor = default, bool enabled = true, Sprite icon = null)
        {
            return new MenuEntry(action, title, description, entryColor, enabled, icon);
        }

        [Test]
        public void TestConstructorTitleNull()
        {
            Assert.Throws<ArgumentNullException>(() => _ = CreateMenuEntry(null, null));
            Assert.Throws<ArgumentNullException>(() => _ = CreateMenuEntry(() => { }, null));
        }

        [Test]
        public void TestConstructorDefault()
        {
            List<int> testItems = new List<int>();
            void Action() => testItems.Add(1);
            MenuEntry entry = CreateMenuEntry(Action, "Test");
            Assert.AreEqual(null, entry.Description);
            Assert.AreEqual("Test", entry.Title);
            Assert.AreEqual(true, entry.Enabled);
            Assert.AreEqual(null, entry.Icon);
            Assert.AreEqual(default(Color), entry.EntryColor);
            Assert.AreNotEqual(default(Color), entry.DisabledColor, "Entry color must differ from disabled color!");

            Assert.AreEqual(0, testItems.Count, "DoAction() may not be called during initialization!");
            entry.DoAction();
            Assert.AreEqual(1, testItems.Count, "DoAction() must call the given UnityAction!");
        }

        [Test, TestCaseSource(nameof(ValidConstructorSupplier))]
        public void TestConstructor(UnityAction action, string title, string description,
                                    Color entryColor, bool enabled, Sprite icon)
        {
            MenuEntry entry = CreateMenuEntry(action, title, description, entryColor, enabled, icon);
            // Given action must either be null or NOP for this test
            if (action == null)
            {
                Assert.IsNull(entry.DoAction);
            }
            else
            {
                Assert.DoesNotThrow(() => entry.DoAction());
            }

            Assert.AreEqual(description, entry.Description);
            Assert.AreEqual(title, entry.Title);
            Assert.AreEqual(enabled, entry.Enabled);
            Assert.AreEqual(icon, entry.Icon);
            Assert.AreEqual(entryColor, entry.EntryColor);

            Debug.Log($"{entryColor}, {entry.DisabledColor}");
            Assert.AreNotEqual(entryColor, entry.DisabledColor, "Disabled color must differ from normal color!");
        }
    }
}