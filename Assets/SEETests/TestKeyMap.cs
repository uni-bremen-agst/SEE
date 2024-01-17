using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

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
            KeyActionDescriptor expected = new("Help", "Provides help", KeyActionCategory.General, KeyCode.H);
            map.Bind(KeyAction.Help, expected);

            Assert.IsTrue(map.TryGetValue(KeyAction.Help, out KeyActionDescriptor actual));
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
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.KeyCode, actual.KeyCode);
            Assert.AreEqual(expected.Category, actual.Category);
            Assert.AreEqual(expected.Description, actual.Description);
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
                map.Save(filename);

                map.Load(filename);
            }
            finally
            {
                Delete(filename);
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
                savedMap.Bind(keyAction, NewDescriptor(keyActionName, KeyCode.H));
                savedMap.Save(filename);

                // Read the previously saved binding again.
                KeyMap loadedMap = new();
                // Note: Loading will ignore every action whose name does not exist yet.
                // Hence, we need to add a binding for keyAction. We will use a
                // different KeyCode, however, so that we can check whether everything
                // was loaded correctly. The binding file stores only the name of the
                // action and the associated KeyCode.
                loadedMap.Bind(keyAction, NewDescriptor(keyActionName, KeyCode.X));
                loadedMap.Load(filename);

                AreEqual(savedMap, loadedMap);
            }
            finally
            {
                Delete(filename);
            }
        }

        private static KeyActionDescriptor NewDescriptor(string keyActionName, KeyCode keyCode)
        {
            return new(keyActionName, "Provides help", KeyActionCategory.General, keyCode);
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
                savedMap.Bind(helpAction, NewDescriptor(help, KeyCode.H));

                // is contained only in savedMap => should be ignored
                const string snap = "Snap";
                const KeyAction snapAction = KeyAction.Snap;
                savedMap.Bind(snapAction, NewDescriptor(snap, KeyCode.S));

                savedMap.Save(filename);

                KeyMap loadedMap = new();
                // help action should be loaded; if we wouldn't add it before loading, it would be ignored;
                // yet, we are using a different key code
                loadedMap.Bind(helpAction, NewDescriptor(help, KeyCode.X));

                // is contained only in loadedMap => must contained to exist unchanged
                const string menu = "Menu";
                const KeyAction menuAction = KeyAction.ToggleMenu;
                KeyActionDescriptor menuDescriptor = NewDescriptor(menu, KeyCode.Y);
                loadedMap.Bind(menuAction, menuDescriptor);

                loadedMap.Load(filename);

                // menu is only in loadedMap, must not be lost or changed
                {
                    Assert.IsTrue(loadedMap.TryGetKeyActionDescriptorByName(menu, out KeyActionDescriptor actual));
                    AreEqual(menuDescriptor, actual);
                }

                {
                    // helpAction is in both bindings, but its key code was changed
                    Assert.IsTrue(loadedMap.TryGetKeyActionDescriptorByName(help, out KeyActionDescriptor actual));
                    // help was saved with KeyCode.H
                    Assert.AreEqual(KeyCode.H, actual.KeyCode);
                }

                {
                    // snapAction was saved, but is not contained in loadedMap
                    // => must not be added to loadedMap (unknown bindings are ignored)
                    Assert.IsFalse(loadedMap.TryGetKeyActionDescriptorByName(snap, out KeyActionDescriptor _));
                }

                Assert.AreEqual(2, loadedMap.Count);
            }
            finally
            {
                Delete(filename);
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
                loadedMap.Bind(KeyAction.Help, NewDescriptor("Help", KeyCode.X));
                loadedMap.Bind(KeyAction.ToggleMenu, NewDescriptor("Toggle", KeyCode.Y));
                Assert.Throws(Is.TypeOf<Exception>(), () => loadedMap.Load(filename));
            }
            finally
            {
                Delete(filename);
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
            map.Bind(KeyAction.Help, NewDescriptor("Help", KeyCode.X));
            Assert.Throws(Is.TypeOf<ArgumentException>(),
                          () => map.Bind(KeyAction.ToggleMenu, NewDescriptor("Toggle", KeyCode.X)));
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
                Assert.IsTrue(superset.TryGetValue(binding.Key, out KeyActionDescriptor supersetBinding));
                AreEqual(binding.Value, supersetBinding);
            }
        }

        /// <summary>
        /// If a file named <paramref name="filename"/> exists, it will be deleted.
        /// </summary>
        /// <param name="filename">file to be deleted</param>
        private static void Delete(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }
    }
}
