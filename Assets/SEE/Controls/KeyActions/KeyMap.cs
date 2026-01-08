using Newtonsoft.Json;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Gets the <paramref name="keyCode"/> of the <see cref="KeyActionDescriptor"/> bound
        /// to the specified <paramref name="keyAction"/>.
        /// </summary>
        /// <param name="keyAction">The <see cref="KeyAction"/> whose <paramref name="keyCode"/> is requested.</param>
        /// <param name="keyCode">The resulting <see cref="KeyCode"/> of the descriptor bound
        /// to <paramref name="keyAction"/>; defined only if this method returns true.</param>
        /// <returns>True if this <see cref="KeyMap"/> contains an descriptor bound to <paramref name="keyAction"/>;
        /// otherwise, false.</returns>
        internal bool TryGetValue(KeyAction keyAction, out KeyCode keyCode)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                keyCode = descriptor.KeyCode;
                return true;
            }
            else
            {
                keyCode = KeyCode.None;
                return false;
            }
        }

        /// <summary>
        /// Returns the <see cref="KeyAction"/> in <see cref="keyBindings"/> that is
        /// triggered by the given <paramref name="keyCode"/>. If a binding is found,
        /// the <see cref="KeyAction"/> bound to the <paramref name="keyCode"/> is
        /// returned in <paramref name="boundKeyAction"/> and true is returned.
        /// Otherwise false is returned and <paramref name="boundKeyAction"/>
        /// is undefined.
        /// </summary>
        /// <param name="keyCode">A <see cref="KeyCode"/> for which a binding
        /// is to be searched.</param>
        /// <param name="boundKeyAction">The <see cref="KeyAction"/> bound
        /// to <paramref name="keyCode"/> if one exists; otherwise undefined.</param>
        /// <returns>True if and only if there is a <see cref="KeyAction"/>
        /// triggered by <paramref name="keyCode"/>.</returns>
        internal bool TryGetKeyAction(KeyCode keyCode, out KeyAction boundKeyAction)
        {
            KeyValuePair<KeyAction, KeyActionDescriptor> keyValuePair = keyBindings.FirstOrDefault(kv => kv.Value.KeyCode == keyCode);
            // Note: We cannot simply use 'return keyValuePair.Key == default' because KeyAction is an enum and the
            // default of an enum is a valid value.
            if (keyValuePair.Equals(default(KeyValuePair<KeyAction, KeyActionDescriptor>)))
            {
                boundKeyAction = default;
                return false;
            }
            else
            {
                boundKeyAction = keyValuePair.Key;
                return true;
            }
        }

        /// <summary>
        /// Binds <paramref name="keyActionDescriptor"/> to <paramref name="keyAction"/>.
        /// </summary>
        /// <param name="keyAction">Where to bind.</param>
        /// <param name="keyActionDescriptor">What to be bound.</param>
        /// <exception cref="System.ArgumentException">If there is already another action
        /// <paramref name="keyAction"/> is bound to or if the key code of <paramref name="keyActionDescriptor"/>
        /// is already in use.
        /// </exception>
        internal void Bind(KeyAction keyAction, KeyActionDescriptor keyActionDescriptor)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                throw new ArgumentException($"Key action {keyAction} is already bound to {descriptor.Name}.\n");
            }
            else if (TryGetKeyAction(keyActionDescriptor.KeyCode, out KeyAction boundKeyAction))
            {
                throw new ArgumentException($"Key code {keyActionDescriptor.KeyCode} is already bound to key action {boundKeyAction}.\n");
            }
            else
            {
                keyBindings[keyAction] = keyActionDescriptor;
            }
        }

        /// <summary>
        /// Rebinds <paramref name="descriptor"/> to another <paramref name="keyCode"/>.
        /// </summary>
        /// <param name="descriptor">The binding that should be triggered by <paramref name="keyCode"/>.</param>
        /// <param name="keyCode">The key code that should trigger the action represented by <paramref name="descriptor"/>.</param>
        /// <exception cref="Exception">Thrown if <paramref name="keyCode"/> is already bound to an action.</exception>
        internal void ResetKeyCode(KeyActionDescriptor descriptor, KeyCode keyCode)
        {
            if (TryGetKeyAction(keyCode, out KeyAction action))
            {
                throw new KeyBindingsExistsException($"Cannot register key {keyCode} for {descriptor.Name}."
                                                     + $" Key {keyCode} is already bound to {action}.\n");
            }
            else
            {
                descriptor.KeyCode = keyCode;
            }
        }

        /// <summary>
        /// The number of bindings.
        /// </summary>
        /// <returns>Number of bindings.</returns>
        internal int Count => keyBindings.Count;

        #region Persistence

        /// <summary>
        /// Defines the settings for the JSON serialization.
        /// These settings ensure that enums are handled as strings rather than their numeric values,
        /// so that the JSON file is more human readable.
        /// </summary>
        private static readonly JsonSerializerSettings jsonSettings = new()
        {
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        /// <summary>
        /// Defines the content of the JSON file.
        /// </summary>
        private class KeyData
        {
            /// <summary>
            /// The key code triggering an action.
            /// </summary>
            public KeyCode KeyCode { get; set; }
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
        /// Saves the keybindings to the JSON file as a list of key-value pairs <see cref="KeyData"/>
        /// where the key is <see cref="KeyActionDescriptor.KeyCode"/> and its value is
        /// <see cref=">KeyActionDescriptor.Name"/>.
        /// </summary>
        /// <remarks>Should be called whenever <see cref="keyBindings"/> is updated</remarks>
        internal void Save(string keyBindingsPath)
        {
            IList<KeyData> keyList = new List<KeyData>();
            keyList.AddRange(keyBindings.Select(binding => new KeyData
            {
                KeyCode = binding.Value.KeyCode,
                ActionName = binding.Value.Name
            }));
            File.WriteAllText(keyBindingsPath, JsonConvert.SerializeObject(keyList, Formatting.Indented, jsonSettings));
        }

        /// <summary>
        /// Loads the <see cref="keyBindings"/> from <see cref="keyBindingsPath"/>
        /// if this file path exists. Otherwise nothing is done.
        ///
        /// If there is a key code in the file, but not in <see cref="keyBindings"/>,
        /// it will be ignored silently.
        ///
        /// It is not a problem if there is a key code in <see cref="keyBindings"/>
        /// but not in the file. We do not expect the file to be complete.
        ///
        /// If, however, the merge of the key codes in the file into <see cref="keyBindings"/>
        /// would create a situation in which the same key code is bound to multiple
        /// actions, we will log an error, but otherwise do not finalize the merge;
        /// that is, <see cref="keyBindings"/> will be the same as before the call
        /// of this method.
        /// </summary>
        /// <exception cref="Exception">Thrown if there exists at least one
        /// duplicate key code in the file.</exception>
        internal void Load(string keyBindingsPath)
        {
            // If the file exists, we can read from it, otherwise we don't do anything.
            if (File.Exists(keyBindingsPath))
            {
                // We are using a copy of keyBindings to finalize the merge only when
                // there are no duplicate key codes. We are testing for duplicated
                // key codes at the end only because two key code could have been
                // swapped, which would be perfectly okay. Testing each individually
                // would appear as an error erroneously.
                IDictionary<KeyAction, KeyActionDescriptor> newKeyBindings
                    = new Dictionary<KeyAction, KeyActionDescriptor>(keyBindings);

                foreach (KeyData keyData in (IList<KeyData>)JsonConvert.DeserializeObject<List<KeyData>>
                                                                         (File.ReadAllText(keyBindingsPath)))
                {
                    // If the key code is in file, but not in keyBindings, we will ignore it.
                    if (TryGetKeyActionDescriptorByName(newKeyBindings, keyData.ActionName,
                                                        out KeyActionDescriptor keyActionDescriptor))
                    {
                        keyActionDescriptor.KeyCode = keyData.KeyCode;
                    }
                    else
                    {
                        Debug.LogWarning($"Loaded key binding {keyData} with {keyActionDescriptor} "
                            + "that is not contained in key bindings. Will be ignored.\n");
                    }
                }

                // the keyCodes in newKeyBindings must be unique; merge should be all at once or not at all
                if (HasDuplicateKeyCodes(newKeyBindings))
                {
                    throw new("There is at least one key code bound to more than one action.\n"
                        + $"Settings in {keyBindingsPath} will be ignored.\n");
                }
                else
                {
                    keyBindings = newKeyBindings;
                }
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
        /// Returns true if and only if there are duplicate key codes in <paramref name="keyBindings"/>.
        /// </summary>
        /// <param name="keyBindings">The key bindings to be checked for consistency.</param>
        /// <returns>True if there are duplicate key codes in <paramref name="keyBindings"/>.</returns>
        private static bool HasDuplicateKeyCodes(IDictionary<KeyAction, KeyActionDescriptor> keyBindings)
        {
            return ContainsDuplicates(keyBindings.Select(kv => kv.Value.KeyCode));
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
