using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// Provides access to SEE's central Input System actions and legacy preference import.
    /// </summary>
    internal static class KeyBindings
    {
        // Ctrl-Z/Ctrl-Y are reserved for Undo/Redo; digits 0-9 are reserved for the player menu.

        /// <summary>
        /// Defines the path of the file where the <see cref="keybindings"/> are stored.
        /// </summary>
        private static readonly string keyBindingsPath = Application.dataPath + "/StreamingAssets/KeyBindings.json";
        private static readonly string inputBindingOverridesPath = Application.persistentDataPath + "/SEEInputBindingOverrides.json";

        /// <summary>
        /// Mapping of every <see cref="KeyAction"/> onto its currently
        /// bound <see cref="KeyActionDescriptor"/>.
        /// </summary>
        /// <remarks>Key actions can be re-bound.</remarks>
        private static readonly KeyMap keyBindings = new();

        /// <summary>
        /// Central desktop action asset. The action map is loaded from Resources and enabled
        /// after persisted overrides or legacy preferences have been applied.
        /// </summary>
        private static InputActionAsset inputActionAsset;
        private static InputActionMap inputActionMap;
        private static bool bindingsLoaded;

        /// <summary>
        /// The active interactive keyboard rebind operation, if one is running.
        /// </summary>
        private static InputActionRebindingExtensions.RebindingOperation activeRebinding;

        [Serializable]
        private class BindingOverrideData
        {
            public int version;
            public string overrides;
        }

        /// <summary>
        /// Returns true if the user has pressed down a key requesting the given <paramref name="keyAction"/>
        /// in the last frame.
        ///
        /// Unlike <see cref="IsPressed(KeyAction)"/>, it fires only once, then immediately
        /// returns false, even if the user keeps holding the key.
        /// </summary>
        /// <param name="keyAction">The <see cref="KeyAction"/> to check.</param>
        /// <returns>True if the user has pressed a key requesting the given <paramref name="keyAction"/>.</returns>
        /// <remarks>Note the difference to <see cref="IsPressed(KeyAction)"/> described there.</remarks>
        internal static bool IsDown(KeyAction keyAction)
        {
            return TryGetInputAction(keyAction, out InputAction action) && action.WasPressedThisFrame();
        }

        /// <summary>
        /// Returns true if the user is pressing a key requesting the given <paramref name="keyAction"/>.
        ///
        /// Unlike <see cref="IsDown(KeyAction)"/>, it returns true for every single frame that
        /// the key is physically held down.
        /// </summary>
        /// <param name="keyAction">The <see cref="KeyAction"/> to check.</param>
        /// <returns>True if the user has pressed a key requesting the given <paramref name="keyAction"/>.</returns>
        /// <remarks>The difference to <see cref="IsDown(KeyAction)"/> is that the latter
        /// yields true only once when the key is pressed, while this method returns true in
        /// every frame while the user holds the key.</remarks>
        internal static bool IsPressed(KeyAction keyAction)
        {
            return TryGetInputAction(keyAction, out InputAction action) && action.IsPressed();
        }

        /// <summary>
        /// Checks whether the given key was pressed this frame using the Input System.
        /// </summary>
        internal static bool IsKeyDown(Key key)
        {
            return TryGetDirectKeyAction(key, out InputAction action) && action.WasPressedThisFrame();
        }

        /// <summary>
        /// Checks whether the given key is held using the Input System.
        /// </summary>
        internal static bool IsKeyPressed(Key key)
        {
            return TryGetDirectKeyAction(key, out InputAction action) && action.IsPressed();
        }

        /// <summary>
        /// Checks whether the given key was released this frame using the Input System.
        /// </summary>
        internal static bool IsKeyUp(Key key)
        {
            return TryGetDirectKeyAction(key, out InputAction action) && action.WasReleasedThisFrame();
        }

        internal static bool IsPlayerMenuDigitDown(int digit)
        {
            if (digit < 0 || digit > 9)
            {
                return false;
            }
            LoadKeyBindings();
            InputAction action = GetInputActionMap().FindAction("PlayerMenuDigit" + digit, throwIfNotFound: false);
            return action != null && action.WasPressedThisFrame();
        }

        /// <summary>
        /// Checks whether the given mouse button was pressed this frame.
        /// </summary>
        internal static bool IsMouseButtonDown(int button)
        {
            return TryGetMouseButton(button, out ButtonControl control) && control.wasPressedThisFrame;
        }

        /// <summary>
        /// Checks whether the given mouse button is held.
        /// </summary>
        internal static bool IsMouseButtonPressed(int button)
        {
            return TryGetMouseButton(button, out ButtonControl control) && control.isPressed;
        }

        /// <summary>
        /// Checks whether the given mouse button was released this frame.
        /// </summary>
        internal static bool IsMouseButtonUp(int button)
        {
            return TryGetMouseButton(button, out ButtonControl control) && control.wasReleasedThisFrame;
        }

        /// <summary>
        /// Reads the current vertical mouse wheel delta, or zero when no mouse is connected.
        /// </summary>
        internal static float MouseScrollDeltaY => Mouse.current?.scroll.ReadValue().y ?? 0f;

        private static bool TryGetMouseButton(int button, out ButtonControl control)
        {
            Mouse mouse = Mouse.current;
            control = mouse == null ? null : button switch
            {
                0 => mouse.leftButton,
                1 => mouse.rightButton,
                2 => mouse.middleButton,
                _ => null
            };
            return control != null;
        }

        private static bool TryGetDirectKeyAction(Key key, out InputAction action)
        {
            action = null;
            string actionName = DirectActionNameFor(key);
            if (actionName == null)
            {
                return false;
            }
            LoadKeyBindings();
            action = GetInputActionMap().FindAction(actionName, throwIfNotFound: false);
            return action != null;
        }

        private static string DirectActionNameFor(Key key)
        {
            return key.ToString() switch
            {
                "LeftCtrl" => "DirectLeftControl",
                "RightCtrl" => "DirectRightControl",
                "LeftShift" => "DirectLeftShift",
                "RightShift" => "DirectRightShift",
                "Escape" => "DirectEscape",
                _ => null
            };
        }

        /// <summary>
        /// Gets an enabled Input System action corresponding to the currently persisted key binding.
        /// </summary>
        private static bool TryGetInputAction(KeyAction keyAction, out InputAction action)
        {
            LoadKeyBindings();
            action = GetInputActionMap().FindAction(keyAction.ToString(), throwIfNotFound: false);
            return action != null;
        }

        private static InputActionMap GetInputActionMap()
        {
            if (inputActionMap == null)
            {
                inputActionAsset = Resources.Load<InputActionAsset>("SEE Keyboard Actions");
                if (inputActionAsset == null)
                {
                    throw new InvalidOperationException("Could not load the SEE Keyboard Actions Input Action Asset.");
                }
                inputActionMap = inputActionAsset.FindActionMap("SEE", throwIfNotFound: true);
            }
            return inputActionMap;
        }

        private static void ApplyRegisteredBindingPaths()
        {
            InputActionMap actionMap = GetInputActionMap();
            foreach (KeyValuePair<KeyAction, KeyActionDescriptor> binding in keyBindings)
            {
                InputAction action = actionMap.FindAction(binding.Key.ToString(), throwIfNotFound: false);
                if (action == null || action.bindings.Count == 0 || string.IsNullOrEmpty(binding.Value.BindingPath))
                {
                    continue;
                }
                if (!string.Equals(action.bindings[0].path, binding.Value.BindingPath, StringComparison.OrdinalIgnoreCase))
                {
                    action.ApplyBindingOverride(0, binding.Value.BindingPath);
                }
            }
        }

        private static void SaveBindingOverrides()
        {
            BindingOverrideData data = new()
            {
                version = 1,
                overrides = inputActionAsset.SaveBindingOverridesAsJson()
            };
            System.IO.File.WriteAllText(inputBindingOverridesPath, JsonUtility.ToJson(data, prettyPrint: true));
        }

        private static void InitializeDescriptorsFromAssetDefaults(InputActionMap actionMap)
        {
            foreach (KeyValuePair<KeyAction, KeyActionDescriptor> binding in keyBindings)
            {
                InputAction action = actionMap.FindAction(binding.Key.ToString(), throwIfNotFound: false);
                if (action == null || action.bindings.Count != 1)
                {
                    throw new InvalidOperationException($"Input Action asset has no single default binding for {binding.Key}.");
                }
                binding.Value.BindingPath = action.bindings[0].path;
            }
        }

        /// <summary>Converts a stored legacy key name to a keyboard path when it is recognized.</summary>
        internal static bool TryKeyboardPathFor(string keyCodeName, out string bindingPath)
        {
            bindingPath = null;
            if (!Enum.TryParse(keyCodeName, ignoreCase: true, out KeyCode keyCode)
                || !Enum.IsDefined(typeof(KeyCode), keyCode))
            {
                return false;
            }
            bindingPath = KeyboardPathFor(keyCode);
            return !string.IsNullOrEmpty(bindingPath);
        }
        /// <summary>
        /// Converts the legacy Unity KeyCode name to an Input System keyboard control path.
        /// The mapping preserves the physical-key semantics of the existing bindings.
        /// </summary>
        internal static string KeyboardPathFor(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None || keyCode.ToString().StartsWith("Mouse", StringComparison.Ordinal))
            {
                return null;
            }
            string name = keyCode.ToString();
            if (name.StartsWith("Alpha", StringComparison.Ordinal))
            {
                name = name.Substring("Alpha".Length);
            }
            else if (name.StartsWith("Keypad", StringComparison.Ordinal))
            {
                name = "Numpad" + name.Substring("Keypad".Length);
            }
            else
            {
                name = name switch
                {
                    "Return" => "Enter",
                    "LeftControl" => "LeftCtrl",
                    "RightControl" => "RightCtrl",
                    "LeftCommand" => "LeftMeta",
                    "RightCommand" => "RightMeta",
                    "LeftApple" => "LeftMeta",
                    "RightApple" => "RightMeta",
                    "LeftWindows" => "LeftMeta",
                    "RightWindows" => "RightMeta",
                    _ => name
                };
            }

            return "<Keyboard>/" + char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Returns true if <paramref name="keyAction"/> is bound, in which case <paramref name="descriptor"/>
        /// will contain the <see cref="KeyActionDescriptor"/> <paramref name="keyAction"/> is bound
        /// to. If <paramref name="keyAction"/> is not bound, false will be returned and
        /// <paramref name="descriptor"/> is undefined.
        /// </summary>
        /// <param name="keyAction">Action to be looked up.</param>
        /// <param name="descriptor">The descriptor <paramref name="keyAction"/> is bound to or
        /// undefined.</param>
        /// <returns>True if <paramref name="keyAction"/> is bound.</returns>
        internal static bool TryGetKeyActionDescriptor(KeyAction keyAction, out KeyActionDescriptor descriptor)
        {
            LoadKeyBindings();
            return keyBindings.TryGetValue(keyAction, out descriptor);
        }

        /// <summary>
        /// Starts Input System's interactive rebinding workflow for a registered action.
        /// Only keyboard controls are accepted by the current settings interface.
        /// </summary>
        internal static void StartInteractiveRebind(KeyActionDescriptor descriptor,
                                                    Action<string> completed,
                                                    Action cancelled)
        {
            if (!TryGetKeyActionForDescriptor(descriptor, out KeyAction keyAction)
                || !TryGetInputAction(keyAction, out InputAction action))
            {
                cancelled?.Invoke();
                return;
            }

            string candidatePath = null;
            activeRebinding = action.PerformInteractiveRebinding()
                .WithControlsHavingToMatchPath("<Keyboard>")
                .WithCancelingThrough("<Keyboard>/escape")
                // Keep the action unchanged until the user confirms the new binding.
                .OnApplyBinding((_, path) => candidatePath = path)
                .OnComplete(operation =>
                {
                    string path = candidatePath;
                    operation.Dispose();
                    activeRebinding = null;
                    if (string.IsNullOrEmpty(path))
                    {
                        cancelled?.Invoke();
                    }
                    else
                    {
                        completed?.Invoke(path);
                    }
                })
                .OnCancel(operation =>
                {
                    operation.Dispose();
                    activeRebinding = null;
                    cancelled?.Invoke();
                });
            activeRebinding.Start();
        }

        internal static void CancelInteractiveRebind()
        {
            if (activeRebinding != null)
            {
                activeRebinding.Cancel();
                activeRebinding.Dispose();
                activeRebinding = null;
            }
        }

        /// <summary>Returns a localized label for an Input System control path.</summary>
        internal static string BindingDisplayName(string bindingPath)
        {
            return string.IsNullOrEmpty(bindingPath)
                ? "Unbound"
                : InputControlPath.ToHumanReadableString(bindingPath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        internal static bool TryGetBindingDisplayName(KeyAction keyAction, out string displayName)
        {
            LoadKeyBindings();
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                displayName = BindingDisplayName(descriptor.BindingPath);
                return !string.IsNullOrEmpty(descriptor.BindingPath);
            }
            displayName = null;
            return false;
        }

        private static bool TryGetKeyActionForDescriptor(KeyActionDescriptor descriptor, out KeyAction keyAction)
        {
            foreach (KeyValuePair<KeyAction, KeyActionDescriptor> binding in keyBindings)
            {
                if (ReferenceEquals(binding.Value, descriptor))
                {
                    keyAction = binding.Key;
                    return true;
                }
            }
            keyAction = default;
            return false;
        }

        /// <summary>
        /// Registers the metadata for the given action. Its default binding comes from the Input Action asset.
        /// </summary>
        /// <param name="keyAction">The action's stable identifier.</param>
        /// <param name="name">The short name shown for the action.</param>
        /// <param name="scope">The category containing the action.</param>
        /// <param name="description">The help message shown for the action.</param>
        private static void Register(KeyAction keyAction, string name, KeyActionCategory scope, string description)
        {
            try
            {
                keyBindings.Bind(keyAction, new KeyActionDescriptor(name, description, scope));
            }
            catch (Exception ex)
            {
                // Because this method is called by the static initializer, it represents an internal
                // problem. The user is not responsible for it. We do not show a notification.
                Debug.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Registers all available <see cref="KeyAction"/>s.
        /// </summary>
        static KeyBindings()
        {
            // Note: The order of the key actions is important. They are displayed to the
            // user in the order of appearance here.

            // General
            Register(KeyAction.Help, "Help",
                     KeyActionCategory.General, "Provides help");
            Register(KeyAction.ToggleMenu, "Toggle menu",
                     KeyActionCategory.General, "Toggles the user-action menu.");
            Register(KeyAction.ToggleSettings, "Toggle settings",
                     KeyActionCategory.General, "Turns on/off the settings menu.");
            Register(KeyAction.ConfigMenu, "Config",
                     KeyActionCategory.General, "Opens/closes the configuration menu.");
            Register(KeyAction.ToggleBrowser, "Toggle browser",
                     KeyActionCategory.General, "Turns on/off the browser.");
            Register(KeyAction.ToggleMirror, "Toggle mirror",
                     KeyActionCategory.General, "Turns on/off the mirror.");
            Register(KeyAction.Undo, "Undo",
                     KeyActionCategory.General, "Undoes the last action.");
            Register(KeyAction.Redo, "Redo",
                     KeyActionCategory.General, "Re-does the last action.");
            Register(KeyAction.CloseNotifications, "Close notifications",
                     KeyActionCategory.General, "Clears all notifications.");
            Register(KeyAction.ToggleFaceCam, "Toggle face cam",
                     KeyActionCategory.General, "Toggles the face camera on or off.");
            Register(KeyAction.ToggleFaceCamPosition, "Toggle face-cam position",
                     KeyActionCategory.General, "Toggles the position of the FaceCam on the player's face.");
            Register(KeyAction.ToggleVoiceControl, "Toggle voice control",
                     KeyActionCategory.General, "Toggles voice controlled commands.");
            Register(KeyAction.OpenSnapshotWindow, "Open snapshot window",
                     KeyActionCategory.General, "Opens the snapshot window.");


            // Movement
            Register(KeyAction.BoostCameraSpeed, "Boost speed",
                     KeyActionCategory.Movement, "Boosts the speed of the player movement. While pressed, movement is faster.");
            Register(KeyAction.MoveForward, "Move forward",
                     KeyActionCategory.Movement, "Move forward.");
            Register(KeyAction.MoveBackward, "Move backward",
                     KeyActionCategory.Movement, "Move backward.");
            Register(KeyAction.MoveRight, "Move right",
                     KeyActionCategory.Movement, "Move to the right.");
            Register(KeyAction.MoveLeft, "Move left",
                     KeyActionCategory.Movement, "Move to the left.");
            Register(KeyAction.MoveUp, "Move up",
                     KeyActionCategory.Movement, "Move up.");
            Register(KeyAction.MoveDown, "Move down",
                     KeyActionCategory.Movement, "Move down.");

            // MetricCharts
            Register(KeyAction.ToggleCharts, "Toggle charts",
                     KeyActionCategory.MetricCharts, "Turns the metric charts on/off.");
            Register(KeyAction.ToggleMetricHoveringSelection, "Toggle metric hovering/selection",
                     KeyActionCategory.MetricCharts, "Toggles hovering/selection for markers in metric charts.");
            Register(KeyAction.ToggleHolisticMetricsMenu, "Toggle metrics menu",
                     KeyActionCategory.MetricCharts, "Toggles the menu for holistic code metrics");

            // Browsing
            Register(KeyAction.TreeView, "Tree view and Search",
                     KeyActionCategory.Browsing, "Opens/closes the tree view window.");
            Register(KeyAction.ToggleEdges, "Toggle edges",
                     KeyActionCategory.Browsing, "Toggles the visibility of all edges of a hovered code city.");
            Register(KeyAction.Unselect, "Unselect",
                     KeyActionCategory.Browsing, "Forgets all currently selected objects.");
            Register(KeyAction.Cancel, "Cancel",
                     KeyActionCategory.Browsing, "Cancels an action.");
            Register(KeyAction.Reset, "Reset",
                     KeyActionCategory.Browsing, "Resets a code city to its original position and scale.");
            Register(KeyAction.ZoomInto, "Zoom in",
                     KeyActionCategory.Browsing, "To zoom into a city.");
            Register(KeyAction.Snap, "Snap",
                     KeyActionCategory.Browsing, "Snap move/rotate city.");
            Register(KeyAction.DragHovered, "Drag hovered",
                     KeyActionCategory.Browsing, "Drag code city.");
            Register(KeyAction.ToggleCameraLock, "Toggle camera lock",
                     KeyActionCategory.Browsing, "Toggles between the locked and free camera mode.");
            Register(KeyAction.Pointing, "Point",
                     KeyActionCategory.Browsing, "Toggles between Pointing.");
            Register(KeyAction.HandAnimations, "Hand animations",
                KeyActionCategory.Browsing, "Switches the hand animations with MediaPipe on and off.");

            // Evolution
            Register(KeyAction.SetMarker, "Set marker",
                     KeyActionCategory.Evolution, "Sets a new marker.");
            Register(KeyAction.DeleteMarker, "Delete marker",
                     KeyActionCategory.Evolution, "Deletes a marker.");
            Register(KeyAction.ToggleEvolutionCanvases, "Toggle evolution canvases",
                     KeyActionCategory.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");

            // Animation
            Register(KeyAction.Previous, "Previous",
                     KeyActionCategory.Animation, "Go to previous element in the animation.");
            Register(KeyAction.Next, "Next",
                     KeyActionCategory.Animation, "Go to next element in the animation.");
            Register(KeyAction.ToggleAutoPlay, "Toggle auto play",
                     KeyActionCategory.Animation, "Toggles auto play of the animation.");
            Register(KeyAction.IncreaseAnimationSpeed, "Increase animation speed",
                     KeyActionCategory.Animation, "Doubles animation speed.");
            Register(KeyAction.DecreaseAnimationSpeed, "Decrease animation speed",
                     KeyActionCategory.Animation, "Halves animation speed.");

            // Debugging
            Register(KeyAction.ToggleExecutionOrder, "Toggle execution order",
                     KeyActionCategory.Debugging, "Toggles execution order (foward/backward).");
            Register(KeyAction.ExecuteToBreakpoint, "Execute to breakpoint",
                     KeyActionCategory.Debugging, "Continues execution until next breakpoint is reached.");
            Register(KeyAction.FirstStatement, "First statement",
                     KeyActionCategory.Debugging, "Execution is back to very first statement.");

            // Code Viewer
            Register(KeyAction.ShowWindowMenu, "Show window menu",
                     KeyActionCategory.CodeViewer, "Toggles the menu of the open windows.");
            // Chat
            Register(KeyAction.ToggleTextChat, "Toggle text chat",
                     KeyActionCategory.Chat, "Toggles the text chat.");
            Register(KeyAction.ToggleVoiceChat, "Toggle voice chat",
                     KeyActionCategory.Chat, "Toggles the voice chat.");

            // Drawable
            Register(KeyAction.PartUndo, "Part undo",
                     KeyActionCategory.Drawable, "Undoes a part of the currently running action.");
            Register(KeyAction.MoveObjectUp, "Move object up",
                     KeyActionCategory.Drawable, "Moves the object up.");
            Register(KeyAction.MoveObjectDown, "Move object down",
                     KeyActionCategory.Drawable, "Moves the object down.");
            Register(KeyAction.MoveObjectLeft, "Move object left",
                     KeyActionCategory.Drawable, "Moves the object left.");
            Register(KeyAction.MoveObjectRight, "Move object right",
                     KeyActionCategory.Drawable, "Moves the object right.");
            Register(KeyAction.MoveObjectForward, "Move object forward",
                     KeyActionCategory.Drawable, "Moves the object forward.");
            Register(KeyAction.MoveObjectBackward, "Move object backward",
                     KeyActionCategory.Drawable, "Moves the object backward.");
            Register(KeyAction.DrawableManagerView, "Toggle drawable manager menu.",
                     KeyActionCategory.Drawable, "Turns on/off the drawable manager menu.");

            // CameraPaths
            Register(KeyAction.SavePathPosition, "Save position",
                     KeyActionCategory.CameraPaths, "Saves the current position when recording paths.");
            Register(KeyAction.TogglePathPlaying, "Toggle path playing",
                     KeyActionCategory.CameraPaths, "Starts/stops the automated camera movement along a path.");

            // EchoFace
            Register(KeyAction.ToggleEchoFace, "Toggle EchoFace",
                     KeyActionCategory.General, "Toggles facial tracking with EchoFace on and off.");
        }

        internal static void SetBindingForPath(KeyActionDescriptor descriptor, string path)
        {
            LoadKeyBindings();
            keyBindings.ResetBinding(descriptor, path);
            if (!TryGetKeyActionForDescriptor(descriptor, out KeyAction keyAction))
            {
                throw new InvalidOperationException($"Could not find the Input Action for {descriptor.Name}.");
            }
            InputAction action = inputActionMap.FindAction(keyAction.ToString(), throwIfNotFound: true);
            action.Disable();
            action.ApplyBindingOverride(0, path);
            action.Enable();
            SaveBindingOverrides();
        }

        /// <summary>
        /// Returns the current mapping of <see cref="KeyAction"/>s onto their
        /// <see cref="KeyActionDescriptor"/> grouped by their <see cref="KeyActionCategory"/>.
        /// </summary>
        /// <returns>Current mapping of key actions onto their descriptor.</returns>
        internal static IEnumerable<IGrouping<KeyActionCategory, KeyValuePair<KeyAction, KeyActionDescriptor>>> AllBindings()
        {
            LoadKeyBindings();
            return keyBindings.AllBindings();
        }

        /// <summary>
        /// Loads the key bindings.
        /// </summary>
        internal static void LoadKeyBindings()
        {
            if (bindingsLoaded)
            {
                return;
            }

            InputActionMap actionMap = GetInputActionMap();
            InitializeDescriptorsFromAssetDefaults(actionMap);
            if (System.IO.File.Exists(inputBindingOverridesPath))
            {
                try
                {
                    string storedOverrides = System.IO.File.ReadAllText(inputBindingOverridesPath);
                    BindingOverrideData storedData = JsonUtility.FromJson<BindingOverrideData>(storedOverrides);
                    if (storedData != null && storedData.version > 1)
                    {
                        throw new InvalidOperationException($"Unsupported input binding settings version {storedData.version}.");
                    }
                    // Accept an unwrapped Unity override string as well, for compatibility with
                    // development builds created before the versioned settings envelope.
                    string overrideJson = storedData != null && storedData.version == 1
                        ? storedData.overrides : storedOverrides;
                    if (storedData != null && storedData.version == 1 && storedData.overrides == null)
                    {
                        throw new InvalidOperationException("The saved input binding overrides have no payload.");
                    }
                    inputActionAsset.LoadBindingOverridesFromJson(overrideJson);
                    CopyEffectivePathsToDescriptors(actionMap);
                    if (HasDuplicateRegisteredPaths())
                    {
                        throw new InvalidOperationException("The saved input bindings contain duplicate control paths.");
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Could not load saved Input System bindings. SEE will try the legacy settings: {exception.Message}");
                    inputActionAsset.RemoveAllBindingOverrides();
                    InitializeDescriptorsFromAssetDefaults(actionMap);
                    TryImportLegacyBindings();
                    ApplyRegisteredBindingPaths();
                }
            }
            else
            {
                TryImportLegacyBindings();
                ApplyRegisteredBindingPaths();
            }

            actionMap.Enable();
            bindingsLoaded = true;
        }

        private static void CopyEffectivePathsToDescriptors(InputActionMap actionMap)
        {
            foreach (KeyValuePair<KeyAction, KeyActionDescriptor> binding in keyBindings)
            {
                InputAction action = actionMap.FindAction(binding.Key.ToString(), throwIfNotFound: false);
                if (action != null && action.bindings.Count > 0)
                {
                    binding.Value.BindingPath = action.bindings[0].effectivePath;
                }
            }
        }

        private static bool HasDuplicateRegisteredPaths()
        {
            return KeyMap.ContainsDuplicates(keyBindings.Select(binding => binding.Value.BindingPath?.ToLowerInvariant()));
        }

        private static void TryImportLegacyBindings()
        {
            try
            {
                // Import existing user settings. The old file remains readable and is not rewritten.
                keyBindings.Load(keyBindingsPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not import legacy key bindings. SEE will use the asset defaults: {exception.Message}");
            }
        }

    }
}
