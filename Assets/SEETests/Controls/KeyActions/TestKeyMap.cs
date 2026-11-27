using NUnit.Framework;
using SEE.Utils;
using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.TestTools;

namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// Test for <see cref="KeyMap"/>
    /// </summary>
    internal class TestKeyMap
    {
        /// <summary>
        /// Tests <see cref="KeyMap.Bind(KeyAction, KeyActionDescriptor)."/>
        /// </summary>
        [Test]
        public void TestBind()
        {
            KeyMap map = new();
            KeyActionDescriptor expected = NewDescriptor("Help", "<Keyboard>/h");
            map.Bind(KeyAction.Help, expected);

            Assert.That(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor actual), Is.True,
                        $"There is no binding for {KeyAction.Help}.");
            AreEqual(expected, actual);
        }

        /// <summary>
        /// Checks whether <paramref name="expected"/> and <paramref name="actual"/> are the same.
        /// </summary>
        /// <param name="expected">expected value</param>
        /// <param name="actual">actual value</param>
        /// <remarks>Only the two attributes mentioned above are compared because
        /// only these are stored in a binding file.</remarks>
        private void AreEqual(KeyActionDescriptor expected, KeyActionDescriptor actual)
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name), "Name");
            Assert.That(actual.BindingPath, Is.EqualTo(expected.BindingPath), "BindingPath");
            Assert.That(actual.Category, Is.EqualTo(expected.Category), "Category");
            Assert.That(actual.Description, Is.EqualTo(expected.Description), "Description");
        }

        /// <summary>
        /// Tests loading an empty binding file.
        /// </summary>
        [Test]
        public void TestEmpty()
        {
            string filename = Path.GetTempFileName();
            try
            {
                KeyMap map = new();
                File.WriteAllText(filename, "[]");
                map.Load(filename);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Tests saving and loading a key map with a single binding.
        /// </summary>
        [Test]
        public void TestSingleEntry()
        {
            string filename = Path.GetTempFileName();
            try
            {
                // Create a binding and save it.
                KeyMap savedMap = new();
                const string keyActionName = "Help";
                const KeyAction keyAction = KeyAction.Help;
                savedMap.Bind(keyAction, NewDescriptor(keyActionName, "<Keyboard>/h"));
                File.WriteAllText(filename, "[{\"KeyCode\":\"H\",\"ActionName\":\"Help\"}]");

                // Read the previously saved binding again.
                KeyMap loadedMap = new();
                // Note: Loading will ignore every action whose name does not exist yet.
                // Hence, we need to add a binding for keyAction. We will use a
                // different path, so we can check that the saved path is imported.
                loadedMap.Bind(keyAction, NewDescriptor(keyActionName, "<Keyboard>/x"));
                loadedMap.Load(filename);

                AreEqual(savedMap, loadedMap);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>Checks legacy spelling conversions, including platform-specific modifier aliases.</summary>
        /// <param name="legacy">Stored KeyCode name.</param>
        /// <param name="expected">Expected keyboard path.</param>
        [TestCase("a", "<Keyboard>/a")]
        [TestCase("Alpha0", "<Keyboard>/0")]
        [TestCase("Keypad3", "<Keyboard>/numpad3")]
        [TestCase("Return", "<Keyboard>/enter")]
        [TestCase("LeftControl", "<Keyboard>/leftCtrl")]
        [TestCase("RightControl", "<Keyboard>/rightCtrl")]
        [TestCase("LeftCommand", "<Keyboard>/leftMeta")]
        [TestCase("RightCommand", "<Keyboard>/rightMeta")]
        [TestCase("LeftApple", "<Keyboard>/leftMeta")]
        [TestCase("RightApple", "<Keyboard>/rightMeta")]
        [TestCase("LeftWindows", "<Keyboard>/leftMeta")]
        [TestCase("RightWindows", "<Keyboard>/rightMeta")]
        public void TestLegacyConversion(string legacy, string expected)
        {
            KeyMap map = LoadEntries(new { ActionName = "Help", KeyCode = legacy });
            Assert.That(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor descriptor), Is.True);
            Assert.That(descriptor.BindingPath, Is.EqualTo(expected));
        }

        /// <summary>Invalid legacy codes are rejected and leave the existing binding unchanged.</summary>
        /// <param name="legacy">Invalid or unsupported stored key.</param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("NotAKey")]
        [TestCase("999999")]
        [TestCase("None")]
        [TestCase("Mouse0")]
        public void TestInvalidLegacyConversion(string legacy)
        {
            LogAssert.Expect(LogType.Warning, $"Ignoring invalid legacy key '{legacy}' for action 'Help'.");
            AssertHelpUnchanged(LoadEntries(new { ActionName = "Help", KeyCode = legacy }));
        }

        /// <summary>Explicit keyboard paths take precedence over the legacy KeyCode field.</summary>
        [Test]
        public void TestExplicitBindingPath()
        {
            KeyMap map = LoadEntries(new { ActionName = "Help", BindingPath = "<Keyboard>/f12", KeyCode = "H" });
            Assert.That(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor descriptor), Is.True);
            Assert.That(descriptor.BindingPath, Is.EqualTo("<Keyboard>/f12"));
        }

        /// <summary>Malformed and non-keyboard paths are skipped even with a valid legacy fallback.</summary>
        /// <param name="path">Invalid imported path.</param>
        [TestCase(" ")]
        [TestCase("Keyboard/h")]
        [TestCase("<Keyboard>")]
        [TestCase("<Mouse>/leftButton")]
        [TestCase("<Gamepad>/buttonSouth")]
        public void TestInvalidBindingPath(string path)
        {
            LogAssert.Expect(LogType.Warning, $"Ignoring invalid control path '{path}' for action 'Help'.");
            AssertHelpUnchanged(LoadEntries(new { ActionName = "Help", BindingPath = path, KeyCode = "H" }));
        }

        /// <summary>Null entries and missing action names are skipped; later valid entries still import.</summary>
        [Test]
        public void TestIncompleteEntries()
        {
            for (int i = 0; i < 3; i++)
            {
                LogAssert.Expect(LogType.Warning, "Ignoring an incomplete entry in the legacy key bindings.");
            }
            LogAssert.Expect(LogType.Warning, "Ignoring legacy key binding for unknown action 'Unknown'.");
            KeyMap map = LoadEntries(null, new { KeyCode = "H" }, new { ActionName = " " },
                                     new { ActionName = "Unknown", KeyCode = "H" },
                                     new { ActionName = "Help", KeyCode = "H" });
            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor descriptor), Is.True);
            Assert.That(descriptor.BindingPath, Is.EqualTo("<Keyboard>/h"));
        }

        /// <summary>Case-insensitive conflicts with retained bindings reject the entire import atomically.</summary>
        [Test]
        public void TestImportConflictIsAtomic()
        {
            KeyMap map = new();
            map.Bind(KeyAction.Help, NewDescriptor("Help", "<Keyboard>/x"));
            map.Bind(KeyAction.ToggleMenu, NewDescriptor("Menu", "<Keyboard>/y"));
            Assert.That(() => ImportEntries(map, new { ActionName = "Help", BindingPath = "<Keyboard>/Y" }),
                        Throws.TypeOf<Exception>());
            AssertHelpUnchanged(map);
            Assert.That(map.TryGetValue(KeyAction.ToggleMenu, out KeyActionDescriptor menu), Is.True);
            Assert.That(menu.BindingPath, Is.EqualTo("<Keyboard>/y"));
        }

        /// <summary>Swapped keys are valid when the complete import contains no duplicates.</summary>
        [Test]
        public void TestImportSwappedBindings()
        {
            KeyMap map = new();
            map.Bind(KeyAction.Help, NewDescriptor("Help", "<Keyboard>/x"));
            map.Bind(KeyAction.ToggleMenu, NewDescriptor("Menu", "<Keyboard>/y"));
            ImportEntries(map, new { ActionName = "Help", BindingPath = "<Keyboard>/y" },
                          new { ActionName = "Menu", BindingPath = "<Keyboard>/x" });
            Assert.That(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor help), Is.True);
            Assert.That(help.BindingPath, Is.EqualTo("<Keyboard>/y"));
            Assert.That(map.TryGetValue(KeyAction.ToggleMenu, out KeyActionDescriptor menu), Is.True);
            Assert.That(menu.BindingPath, Is.EqualTo("<Keyboard>/x"));
        }

        /// <summary>Creates an isolated map and imports the supplied entries.</summary>
        /// <param name="entries">JSON entries to import.</param>
        /// <returns>The imported map.</returns>
        private static KeyMap LoadEntries(params object[] entries)
        {
            KeyMap map = new();
            map.Bind(KeyAction.Help, NewDescriptor("Help", "<Keyboard>/x"));
            ImportEntries(map, entries);
            return map;
        }

        /// <summary>Imports through a unique temporary file which is removed even when loading fails.</summary>
        /// <param name="map">Isolated destination map.</param>
        /// <param name="entries">JSON entries to import.</param>
        private static void ImportEntries(KeyMap map, params object[] entries)
        {
            string filename = Path.GetTempFileName();
            try
            {
                File.WriteAllText(filename, JsonConvert.SerializeObject(entries));
                map.Load(filename);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>Checks that a skipped or rejected entry preserves the original shortcut.</summary>
        /// <param name="map">Imported map.</param>
        private static void AssertHelpUnchanged(KeyMap map)
        {
            Assert.That(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor descriptor), Is.True);
            Assert.That(descriptor.BindingPath, Is.EqualTo("<Keyboard>/x"));
            Assert.That(descriptor.Name, Is.EqualTo("Help"));
            Assert.That(descriptor.Description, Is.EqualTo("Provides help"));
            Assert.That(descriptor.Category, Is.EqualTo(KeyActionCategory.General));
        }

        private static KeyActionDescriptor NewDescriptor(string keyActionName, string bindingPath)
        {
            return new KeyActionDescriptor(keyActionName, "Provides help", KeyActionCategory.General)
            {
                BindingPath = bindingPath
            };
        }

        /// <summary>
        /// Tests loading the content of a binding file, F, that only overlaps
        /// with an existing binding, B. Entries in F not contained in B must
        /// be ignored. Entries in B not contained in F must be untouched.
        /// Let B' be B after the merge. B' must not have any additional entry
        /// beyond those present in B or F.
        /// </summary>
        [Test]
        public void TestOverlap()
        {
            string filename = Path.GetTempFileName();
            try
            {
                // Create a binding and save it.
                KeyMap savedMap = new();

                // is contained in both savedMap and loadedMap, yet with different keys
                const string help = "Help";
                const KeyAction helpAction = KeyAction.Help;
                savedMap.Bind(helpAction, NewDescriptor(help, "<Keyboard>/h"));

                // is contained only in savedMap => should be ignored
                const string snap = "Snap";
                const KeyAction snapAction = KeyAction.Snap;
                savedMap.Bind(snapAction, NewDescriptor(snap, "<Keyboard>/s"));
                File.WriteAllText(filename,
                    "[{\"KeyCode\":\"H\",\"ActionName\":\"Help\"},{\"KeyCode\":\"S\",\"ActionName\":\"Snap\"}]");

                KeyMap loadedMap = new();
                // help action should be loaded; if we wouldn't add it before loading, it would be ignored;
                // yet, we are using a different path.
                loadedMap.Bind(helpAction, NewDescriptor(help, "<Keyboard>/x"));

                // is contained only in loadedMap => must contained to exist unchanged
                const string menu = "Menu";
                const KeyAction menuAction = KeyAction.ToggleMenu;
                KeyActionDescriptor menuDescriptor = NewDescriptor(menu, "<Keyboard>/y");
                loadedMap.Bind(menuAction, menuDescriptor);

                loadedMap.Load(filename);

                // menu is only in loadedMap, must not be lost or changed
                {
                    Assert.That(loadedMap.TryGetKeyActionDescriptorByName(menu, out KeyActionDescriptor actual),
                                Is.True, $"There is no binding named {menu}.");
                    AreEqual(menuDescriptor, actual);
                }

                {
                    // helpAction is in both bindings, but its path was changed
                    Assert.That(loadedMap.TryGetKeyActionDescriptorByName(help, out KeyActionDescriptor actual),
                                Is.True, $"There is no binding named {help}.");
                    // help was saved with <Keyboard>/h
                    Assert.That(actual.BindingPath, Is.EqualTo("<Keyboard>/h"));
                }

                {
                    // snapAction was saved, but is not contained in loadedMap
                    // => must not be added to loadedMap (unknown bindings are ignored)
                    Assert.That(loadedMap.TryGetKeyActionDescriptorByName(snap, out KeyActionDescriptor _),
                                Is.False, $"There must be no binding named {snap}.");
                }

                Assert.That(loadedMap.Count, Is.EqualTo(2));
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Checks whether inconsistent bindings stored in a file are detected.
        /// </summary>
        [Test]
        public void TestInconsistencyInFile()
        {
            string filename = Path.GetTempFileName();
            try
            {
                // Note that we are taking advantage of the knowledge of how those key bindings
                // are stored. That's not good.
                const string content =
                    "[{\"KeyCode\": \"Slash\", \"ActionName\": \"Help\"}, {\"KeyCode\": \"Slash\", \"ActionName\": \"Toggle\"}]";
                File.WriteAllText(filename, content);

                KeyMap loadedMap = new();
                loadedMap.Bind(KeyAction.Help, NewDescriptor("Help", "<Keyboard>/x"));
                loadedMap.Bind(KeyAction.ToggleMenu, NewDescriptor("Toggle", "<Keyboard>/y"));
                Assert.That(() => loadedMap.Load(filename), Throws.TypeOf<Exception>());
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Checks whether inconsistent bindings (two bindings bound to the same key code)
        /// are detected.
        /// </summary>
        [Test]
        public void TestInconsistency()
        {
            KeyMap map = new();
            map.Bind(KeyAction.Help, NewDescriptor("Help", "<Keyboard>/x"));
            Assert.That(() => map.Bind(KeyAction.ToggleMenu, NewDescriptor("Toggle", "<Keyboard>/x")),
                        Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        /// Checks whether <paramref name="expected"/> and <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected value</param>
        /// <param name="actual">actual value</param>
        private void AreEqual(KeyMap expected, KeyMap actual)
        {
            IsSubSet(expected, actual);
            IsSubSet(actual, expected);
        }

        /// <summary>
        /// Checks whether <paramref name="subset"/> is a subset of <paramref name="superset"/>.
        /// </summary>
        /// <param name="subset">the subset to be checked</param>
        /// <param name="superset">the set that should subsume <paramref name="subset"/></param>
        private void IsSubSet(KeyMap subset, KeyMap superset)
        {
            foreach (var binding in subset)
            {
                Assert.That(superset.TryGetValue(binding.Key, out KeyActionDescriptor supersetBinding), Is.True,
                            $"The superset has no binding for {binding.Key}.");
                AreEqual(binding.Value, supersetBinding);
            }
        }
    }
}
