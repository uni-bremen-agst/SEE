using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Game.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SEETests.UI
{
    /// <summary>
    /// Tests for the <see cref="MenuEntry"/> class.
    /// </summary>
    public class TestMenuEntry
    {

        /// <summary>
        /// Path to a sprite we can use for testing.
        /// </summary>
        private const string TEST_SPRITE = "Materials/Charts/MoveIcon";
        
        [Test]
        public void TestConstructorTitleNull()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new MenuEntry(action: null, title: null));
            Assert.Throws<ArgumentNullException>(() => _ = new MenuEntry(action: () => {}, title: null));
        }
        
        [Test]
        public void TestConstructorDefault()
        {
            List<int> testItems = new List<int>();
            void Action() => testItems.Add(1);
            MenuEntry entry = new MenuEntry(Action, "Test");
            Assert.AreEqual(null, entry.Description);
            Assert.AreEqual("Test", entry.Title);
            Assert.AreEqual(true, entry.Enabled);
            Assert.AreEqual(null, entry.Icon);
            Assert.AreEqual(default(Color), entry.EntryColor);
            Assert.AreNotEqual(default(Color), entry.DisabledColor);

            Assert.AreEqual(0, testItems.Count, "DoAction() may not be called during initialization!");
            entry.DoAction();
            Assert.AreEqual(1, testItems.Count, "DoAction() must call the given UnityAction!");
        }

        public static IEnumerable<TestCaseData> ValidConstructorSupplier()
        {
            Sprite testSprite = Resources.Load<Sprite>(TEST_SPRITE);
            yield return new TestCaseData(new UnityAction(() => {}), "Test", "Test description", Color.red, 
                                          true, testSprite);
            yield return new TestCaseData(null, "Test", "Test description", Color.green, 
                                          true, testSprite);
            yield return new TestCaseData(new UnityAction(() => {}), "Test", null, Color.blue, 
                                          true, testSprite);
            yield return new TestCaseData(new UnityAction(() => {}), "Test", "Test description", null, 
                                          true, testSprite);
            yield return new TestCaseData(new UnityAction(() => {}), "Test", "Test description", Color.white, 
                                          false, testSprite);
            yield return new TestCaseData(new UnityAction(() => {}), "Test", "Test description", Color.black, 
                                          true, null);
            yield return new TestCaseData(null, "Test", null, null, true, null);
        }

        [Test, TestCaseSource(nameof(ValidConstructorSupplier))]
        public void TestConstructor(UnityAction action, string title, string description, 
                                    Color entryColor, bool enabled, Sprite icon)
        {
            MenuEntry entry = new MenuEntry(action, title, description, entryColor, enabled, icon);
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