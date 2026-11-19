using Newtonsoft.Json;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine;

namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// Mapping of every <see cref="KeyAction"/> onto its currently
    /// bound <see cref="KeyActionDescriptor"/>.
    /// </summary>
    /// <remarks>Key actions can be re-bound.</remarks>
    internal class KeyMap : IEnumerable<KeyValuePair<KeyAction, KeyActionDescriptor>>
    {
        /// <summary>
        /// Mapping of every <see cref="KeyAction"/> onto its currently
        /// bound <see cref="KeyActionDescriptor"/>.
        /// </summary>
        private IDictionary<KeyAction, KeyActionDescriptor> keyBindings
            = new Dictionary<KeyAction, KeyActionDescriptor>();

        /// <summary>
        /// Returns all bindings grouped by their <see cref="KeyActionCategory"/>.
        /// </summary>
        /// <returns>All bindings.</returns>
        internal IEnumerable<IGrouping<KeyActionCategory, KeyValuePair<KeyAction, KeyActionDescriptor>>> AllBindings()
        {
            return keyBindings.GroupBy(binding => binding.Value.Category);
        }

        /// <summary>
        /// Gets the <paramref name="descriptor"/> bound to the specified <paramref name="keyAction"/>.
        /// </summary>
        /// <param name="keyAction">The <see cref="KeyAction"/> whose <paramref name="descriptor"/> is requested.</param>
        /// <param name="descriptor">The resulting <see cref="KeyActionDescriptor"/> bound
        /// to <paramref name="keyAction"/; defined only if this method returns true.</param>
        /// <returns>True if this <see cref="KeyMap"/> contains an descriptor bound to <paramref name="keyAction"/>;
        /// otherwise, false.</returns>
        internal bool TryGetValue(KeyAction keyAction, out KeyActionDescriptor descriptor)
        {
            return keyBindings.TryGetValue(keyAction, out descriptor);
        }

        /// <summary>
        /// Binds <paramref name="keyActionDescriptor"/> to <paramref name="keyAction"/>.
        /// </summary>
        /// <param name="keyAction">Where to bind.</param>
        /// <param name="keyActionDescriptor">What to be bound.</param>
        /// <exception cref="System.ArgumentException">If there is already another action
        /// <paramref name="keyAction"/> is bound to or if the control path is already in use.
        /// </exception>
        internal void Bind(KeyAction keyAction, KeyActionDescriptor keyActionDescriptor)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                throw new ArgumentException($"Key action {keyAction} is already bound to {descriptor.Name}.\n");
            }
            else if (!string.IsNullOrEmpty(keyActionDescriptor.BindingPath)
                     && keyBindings.Any(kv => string.Equals(kv.Value.BindingPath,
                                                           keyActionDescriptor.BindingPath,
                                                           StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Control path {keyActionDescriptor.BindingPath} is already bound.\n");
            }
            else
            {
                keyBindings[keyAction] = keyActionDescriptor;
            }
        }

        /// <summary>Rebinds a descriptor while retaining the Input System path directly.</summary>
        internal void ResetBinding(KeyActionDescriptor descriptor, string bindingPath)
        {
            KeyValuePair<KeyAction, KeyActionDescriptor> existingPathBinding = keyBindings.FirstOrDefault(kv =>
                !ReferenceEquals(kv.Value, descriptor)
                && string.Equals(kv.Value.BindingPath, bindingPath, StringComparison.OrdinalIgnoreCase));
            if (existingPathBinding.Value != null)
            {
                throw new KeyBindingsExistsException($"Cannot register control path {bindingPath} for {descriptor.Name}."
                    + $" It is already bound to {existingPathBinding.Key}.\n");
            }
            descriptor.BindingPath = bindingPath;
        }

        /// <summary>
        /// The number of bindings.
        /// </summary>
        /// <returns>Number of bindings.</returns>
        internal int Count => keyBindings.Count;

        #region Persistence

        private class KeyData
        {
            /// <summary>
            /// The key code triggering an action.
            /// </summary>
            public string KeyCode { get; set; }
            /// <summary>Input System control path, when saved by the new input binding format.</summary>
            public string BindingPath { get; set; }
            /// <summary>
            /// The descriptive <see cref="KeyActionDescriptor.Name"/> of the action shown to the user.
            /// </summary>
            public string ActionName { get; set; }

            public override string ToString()
            {
                return $"<{KeyCode}, {ActionName}>";
            }
        }

        /// <summary>
        /// Loads the <see cref="keyBindings"/> from <see cref="keyBindingsPath"/>
        /// if this file path exists. Otherwise nothing is done.
        ///
        /// Entries with unknown actions, invalid key names, or invalid keyboard paths are skipped with a warning.
        ///
        /// It is not a problem if there is a key code in <see cref="keyBindings"/>
        /// but not in the file. We do not expect the file to be complete.
        ///
        /// If the imported paths would create a situation in which the same control path is bound to multiple
        /// actions, we will log an error, but otherwise do not finalize the merge;
        /// that is, <see cref="keyBindings"/> will be the same as before the call
        /// of this method.
        /// </summary>
        /// <exception cref="Exception">Thrown if there exists at least one
        /// duplicate control path in the file.</exception>
        internal void Load(string keyBindingsPath)
        {
            // If the file exists, we can read from it, otherwise we don't do anything.
            if (File.Exists(keyBindingsPath))
            {
                // We are using a copy of keyBindings to finalize the merge only when
                // there are no duplicate control paths. We are testing for duplicates
                // after applying every imported value because two bindings may have
                // been swapped, which is okay. Testing each individually
                // would appear as an error erroneously.
                IDictionary<KeyAction, KeyActionDescriptor> newKeyBindings
                    = keyBindings.ToDictionary(pair => pair.Key, pair =>
                    {
                        KeyActionDescriptor original = pair.Value;
                        return new KeyActionDescriptor(original.Name, original.Description, original.Category)
                        {
                            BindingPath = original.BindingPath
                        };
                    });

                List<KeyData> importedBindings = JsonConvert.DeserializeObject<List<KeyData>>(File.ReadAllText(keyBindingsPath));
                if (importedBindings == null)
                {
                    throw new InvalidDataException($"Settings in {keyBindingsPath} do not contain a binding list.");
                }

                foreach (KeyData keyData in importedBindings)
                {
                    if (keyData == null || string.IsNullOrWhiteSpace(keyData.ActionName))
                    {
                        Debug.LogWarning("Ignoring an incomplete entry in the legacy key bindings.");
                        continue;
                    }
                    if (!TryGetKeyActionDescriptorByName(newKeyBindings, keyData.ActionName,
                                                         out KeyActionDescriptor keyActionDescriptor))
                    {
                        Debug.LogWarning($"Ignoring legacy key binding for unknown action '{keyData.ActionName}'.");
                        continue;
                    }

                    string bindingPath = keyData.BindingPath;
                    if (string.IsNullOrEmpty(bindingPath))
                    {
                        if (!KeyBindings.TryKeyboardPathFor(keyData.KeyCode, out bindingPath))
                        {
                            Debug.LogWarning($"Ignoring invalid legacy key '{keyData.KeyCode}' for action '{keyData.ActionName}'.");
                            continue;
                        }
                    }
                    else if (!IsValidKeyboardBindingPath(bindingPath))
                    {
                        Debug.LogWarning($"Ignoring invalid control path '{bindingPath}' for action '{keyData.ActionName}'.");
                        continue;
                    }

                    keyActionDescriptor.BindingPath = bindingPath;
                }

                // Control paths in newKeyBindings must be unique; merge is all at once or not at all.
                if (HasDuplicateBindingPaths(newKeyBindings))
                {
                    throw new("There is at least one control path bound to more than one action.\n"
                        + $"Settings in {keyBindingsPath} will be ignored.\n");
                }
                else
                {
                    keyBindings = newKeyBindings;
                }
            }
        }

        /// <summary>Checks that an imported path is a syntactically valid keyboard control path.</summary>
        private static bool IsValidKeyboardBindingPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            try
            {
                return InputControlPath.Parse(path).Count() > 1;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        /// <summary>
        /// Returns the <see cref="KeyActionDescriptor"/> in <paramref name="keyBindings"/> that
        /// has the given <paramref name="actionName"/> (<see cref="KeyActionDescriptor.Name"/>).
        /// If a binding is found,the <see cref="KeyActionDescriptor"/> with <paramref name="actionName"/> is
        /// returned in <paramref name="keyActionDescriptor"/> and true is returned.
        /// Otherwise false is returned and <paramref name="keyActionDescriptor"/>
        /// is undefined.
        /// </summary>
        /// <param name="keyBindings">The key bindings where to look up <paramref name="actionName"/>.</param>
        /// <param name="actionName">A <see cref="KeyActionDescriptor.Name"/> to be searched for.</param>
        /// <param name="keyActionDescriptor">The <see cref="KeyActionDescriptor"/> with
        /// <paramref name="actionName"/> if one exists; otherwise undefined.</param>
        /// <returns>True if and only if there is a <see cref="KeyActionDescriptor"/>
        /// in <paramref name="keyBindings"/> with <paramref name="actionName"/>.</returns>
        private static bool TryGetKeyActionDescriptorByName
            (IDictionary<KeyAction, KeyActionDescriptor> keyBindings,
            String actionName,
            out KeyActionDescriptor keyActionDescriptor)
        {
            keyActionDescriptor = keyBindings.FirstOrDefault(kv => kv.Value.Name == actionName).Value;
            return keyActionDescriptor != default;
        }

        /// <summary>
        /// Returns the <see cref="KeyActionDescriptor"/> that has the given <paramref name="actionName"/>
        /// (<see cref="KeyActionDescriptor.Name"/>).
        /// If a binding is found,the <see cref="KeyActionDescriptor"/> with <paramref name="actionName"/> is
        /// returned in <paramref name="keyActionDescriptor"/> and true is returned.
        /// Otherwise false is returned and <paramref name="keyActionDescriptor"/>
        /// is undefined.
        /// </summary>
        /// <param name="actionName">A <see cref="KeyActionDescriptor.Name"/> to be searched for.</param>
        /// <param name="keyActionDescriptor">The <see cref="KeyActionDescriptor"/> with
        /// <paramref name="actionName"/> if one exists; otherwise undefined.</param>
        /// <returns>True if and only if there is a <see cref="KeyActionDescriptor"/>
        /// with <paramref name="actionName"/>.</returns>
        internal bool TryGetKeyActionDescriptorByName(String actionName, out KeyActionDescriptor keyActionDescriptor)
        {
            return TryGetKeyActionDescriptorByName(keyBindings, actionName, out keyActionDescriptor);
        }

        /// <summary>
        /// Returns true if and only if there are duplicate control paths in <paramref name="keyBindings"/>.
        /// </summary>
        /// <param name="keyBindings">The key bindings to be checked for consistency.</param>
        /// <returns>True if there are duplicate control paths in <paramref name="keyBindings"/>.</returns>
        private static bool HasDuplicateBindingPaths(IDictionary<KeyAction, KeyActionDescriptor> keyBindings)
        {
            return ContainsDuplicates(keyBindings.Select(kv => kv.Value.BindingPath?.ToLowerInvariant()));
        }

        /// <summary>
        /// Returns true if and only if there are duplicate key codes in <paramref name="enumerable"/>.
        /// </summary>
        /// <typeparam name="T">any type</typeparam>
        /// <param name="enumerable">To be checked for duplicates.</param>
        /// <returns>True if there are duplicate key codes in <paramref name="enumerable"/>.</returns>
        public static bool ContainsDuplicates<T>(IEnumerable<T> enumerable)
        {
            HashSet<T> set = new();
            return !enumerable.All(set.Add);
        }

        #endregion

        #region Iteration

        /// <summary>
        /// Allows to iterate over all bindings in this <see cref="KeyMap"/>.
        /// Implements <see cref="IEnumerable.GetEnumerator"/>.
        /// </summary>
        /// <returns>Iterator for this <see cref="KeyMap"/>.</returns>
        public IEnumerator<KeyValuePair<KeyAction, KeyActionDescriptor>> GetEnumerator()
        {
            foreach (var binding in keyBindings)
            {
                yield return binding;
            }
        }

        /// <summary>
        /// Allows to iterate over all bindings in this <see cref="KeyMap"/>.
        /// Implements <see cref="IEnumerable.GetEnumerator"/>.
        /// </summary>
        /// <returns>Iterator for this <see cref="KeyMap"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var binding in keyBindings)
            {
                yield return binding;
            }
        }
        #endregion

        /// <summary>
        /// An exception indicating that the attempted rebind would result in a duplicate key binding.
        /// </summary>
        public class KeyBindingsExistsException : Exception
        {
            public KeyBindingsExistsException(string message) : base(message) { }
        };
    }
}
