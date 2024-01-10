using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Defines the
    ///      codes for all interaction based on the keyboard in SEE.
    /// </summary>
    internal static partial class KeyBindings
    {

        // IMPORTANT NOTES:
        // (1) Keep in mind that KeyCodes in Unity map directly to a
        //     physical key on an keyboard with an English layout.
        // (2) Ctrl-Z and Ctrl-Y are reserved for Undo and Redo.
        // (3) The digits 0-9 are reserved for shortcuts for the player menu.


        /// <summary>
        /// Mapping of every <see cref="KeyAction"/> onto its currently bound <see cref="KeyActionDescriptor"/>.
        /// </summary>
        /// <remarks>Key actions can be re-bound.</remarks>
        private static readonly IDictionary<KeyAction, KeyActionDescriptor> keyBindings
            = new Dictionary<KeyAction, KeyActionDescriptor>();

        /// <summary>
        /// Returns true if the user has pressed down a key requesting the given <paramref name="keyAction"/>
        /// </summary>
        /// <param name="keyAction">the <see cref="KeyAction"/> to check</param>
        /// <returns>true if the user has pressed a key requesting the given <paramref name="keyAction"/></returns>
        /// <remarks>Note the difference to <see cref="IsPressed(KeyAction)"/> described there.</remarks>
        internal static bool IsDown(KeyAction keyAction)
        {
            return keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor)
                && Input.GetKeyDown(descriptor.KeyCode);
        }

        /// <summary>
        /// Returns true if the user is pressing a key requesting the given <paramref name="keyAction"/>
        /// </summary>
        /// <param name="keyAction">the <see cref="KeyAction"/> to check</param>
        /// <returns>true if the user has pressed a key requesting the given <paramref name="keyAction"/></returns>
        /// <remarks>The difference to <see cref="IsDown(KeyAction)"/> is that the latter
        /// yields true only once when the key is pressed, while this method returns true in
        /// every frame while the user holds the key.</remarks>
        internal static bool IsPressed(KeyAction keyAction)
        {
            return keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor)
                && Input.GetKey(descriptor.KeyCode);
        }

        /// <summary>
        /// Returns true if <paramref name="keyAction"/> is bound, in which case <paramref name="descriptor"/>
        /// will contain the <see cref="KeyActionDescriptor"/> <paramref name="keyAction"/> is bound
        /// to. If <paramref name="keyAction"/> is not bound, <c>false</c> will be returned and
        /// <paramref name="descriptor"/> is undefined.
        /// </summary>
        /// <param name="keyAction">action to be looked up</param>
        /// <param name="descriptor">the descriptor <paramref name="keyAction"/> is bound to or
        /// undefined</param>
        /// <returns>true if <paramref name="keyAction"/> is bound</returns>
        internal static bool TryGetKeyActionDescriptor(KeyAction keyAction, out KeyActionDescriptor descriptor)
        {
            return keyBindings.TryGetValue(keyAction, out descriptor);
        }

        /// <summary>
        /// Returns true if <paramref name="keyAction"/> is bound, in which case <paramref name="keyCode"/>
        /// will contain the <see cref="KeyCode"/> triggering <paramref name="keyAction"/>.
        /// If <paramref name="keyAction"/> is not bound, <c>false</c> will be returned and
        /// <paramref name="keyCode"/> is undefined.
        /// </summary>
        /// <param name="keyAction">action to be looked up</param>
        /// <param name="keyCode">the key code triggering <paramref name="keyAction"/> or
        /// undefined</param>
        /// <returns>true if <paramref name="keyAction"/> is bound</returns>
        internal static bool TryGetKeyCode(KeyAction keyAction, out KeyCode keyCode)
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
        /// True if given <paramref name="key"/> can be re-assigned by a user.
        /// </summary>
        /// <param name="key">the key code to be checked</param>
        /// <returns>True if given <paramref name="key"/> can be re-assigned by a user.</returns>
        internal static bool AssignableKeyCode(KeyCode key)
        {
            // We are using the mouse buttons 0, 1, and 2 for other purposes. We do not want
            // them to be re-assigned.
            return key != KeyCode.Mouse0 && key != KeyCode.Mouse1 && key != KeyCode.Mouse2;
        }

        /// <summary>
        /// Registers the given <paramref name="keyCode"/> for the given <paramref name="scope"/>
        /// and the <paramref name="description"/>. If a <paramref name="keyCode"/> is already registered,
        /// an error message will be emitted.
        /// </summary>
        /// <param name="keyCode">the key code to be registered</param>
        /// <param name="name">the name of the action triggered by the <paramref name="keyCode"/>;
        /// <param name="scope">the scope of the key code</param>
        /// <param name="description">the help message for the key code</param>
        /// this name will be presented to our users</param>
        private static void Register(KeyAction keyAction, KeyCode keyCode, string name, KeyActionCategory scope, string description)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                Debug.LogError($"Key action {keyAction} is already bound to {descriptor.Name}.\n");
            }
            else if (TryGetKeyAction(keyCode, out KeyAction boundKeyAction))
            {
                Debug.LogError($"Key code {keyCode} is already bound to key action {boundKeyAction}.\n");
            }
            else
            {
                keyBindings[keyAction] = new KeyActionDescriptor(name, description, scope, keyCode);
            }
        }

        /// <summary>
        /// Registers all available <see cref="KeyAction"/>s.
        /// </summary>
        static KeyBindings()
        {
            // General
            Register(KeyAction.Help, KeyCode.H, "Help",
                     KeyActionCategory.General, "Provides help");
            Register(KeyAction.ToggleVoiceInput, KeyCode.Period, "Toggle voice chat",
                     KeyActionCategory.General, "Toggles voice input on/off.");
            Register(KeyAction.ToggleMenu, KeyCode.Space, "Toggle menu",
                     KeyActionCategory.General, "Toggles the user-action menu.");
            Register(KeyAction.ToggleSettings, KeyCode.Pause, "Toggle settings",
                     KeyActionCategory.General, "Turns on/off the settings menu.");
            Register(KeyAction.ToggleBrowser, KeyCode.F4, "Toggle browser",
                     KeyActionCategory.General, "Turns on/off the browser.");
            Register(KeyAction.SearchMenu, KeyCode.F, "Search",
                     KeyActionCategory.General, "Opens the search menu.");
            Register(KeyAction.Undo, KeyCode.Z, "Undo",
                     KeyActionCategory.General, "Undoes the last action.");
            Register(KeyAction.Redo, KeyCode.Y, "Redo",
                     KeyActionCategory.General, "Re-does the last action.");
            Register(KeyAction.ConfigMenu, KeyCode.K, "Config",
                     KeyActionCategory.General, "Opens/closes the configuration menu.");
            Register(KeyAction.TreeView, KeyCode.Tab, "Tree view",
                     KeyActionCategory.General, "Opens/closes the tree view window.");
            Register(KeyAction.CloseNotifications, KeyCode.X, "Close notifications",
                     KeyActionCategory.General, "Clears all notifications.");
            Register(KeyAction.ToggleFaceCam, KeyCode.I, "Toggle face cam",
                     KeyActionCategory.General, "Toggles the face camera on or off.");
            Register(KeyAction.ToggleFaceCamPosition, KeyCode.F3, "Toggle face-cam position",
                     KeyActionCategory.General, "Toggles the position of the FaceCam on the player's face.");
            Register(KeyAction.ToggleHolisticMetricsMenu, KeyCode.C, "Toggle metrics menu",
                     KeyActionCategory.General, "Toggles the menu for holistic code metrics");
            // CameraPaths
            Register(KeyAction.SavePathPosition, KeyCode.F11, "Save position",
                     KeyActionCategory.CameraPaths, "Saves the current position when recording paths.");
            Register(KeyAction.TogglePathPlaying, KeyCode.F12, "Toggle path playing",
                     KeyActionCategory.CameraPaths, "Starts/stops the automated camera movement along a path.");
            // MetricCharts
            Register(KeyAction.ToggleCharts, KeyCode.M, "Toggle charts",
                     KeyActionCategory.MetricCharts, "Turns the metric charts on/off.");
            Register(KeyAction.ToggleMetricHoveringSelection, KeyCode.N, "Toggle metric hovering/selection",
                     KeyActionCategory.MetricCharts, "Toggles hovering/selection for markers in metric charts.");
            // Browsing
            Register(KeyAction.ToggleEdges, KeyCode.V, "Toggle edges",
                     KeyActionCategory.Browsing, "Toggles the visibility of all edges of a hovered code city.");
            Register(KeyAction.Unselect, KeyCode.U, "Unselect",
                     KeyActionCategory.Browsing, "Forgets all currently selected objects.");
            Register(KeyAction.Cancel, KeyCode.Escape, "Cancel",
                     KeyActionCategory.Browsing, "Cancels an action.");
            Register(KeyAction.Reset, KeyCode.R, "Reset",
                     KeyActionCategory.Browsing, "Resets a code city to its original position and scale.");
            Register(KeyAction.ZoomInto, KeyCode.G, "Zoom in",
                     KeyActionCategory.Browsing, "To zoom into a city.");
            Register(KeyAction.Snap, KeyCode.LeftAlt, "Snap",
                     KeyActionCategory.Browsing, "Snap move/rotate city.");
            Register(KeyAction.DragHovered, KeyCode.LeftControl, "Drag hovered",
                     KeyActionCategory.Browsing, "Drag code city.");
            Register(KeyAction.ToggleCameraLock, KeyCode.L, "Toggle camera lock",
                     KeyActionCategory.Browsing, "Toggles between the locked and free camera mode.");
            Register(KeyAction.Pointing, KeyCode.P, "Point",
                     KeyActionCategory.Browsing, "Toggles between Pointing.");
            // Movement
            Register(KeyAction.BoostCameraSpeed, KeyCode.LeftShift, "Boost speed",
                     KeyActionCategory.Movement, "Boosts the speed of the player movement. While pressed, movement is faster.");
            Register(KeyAction.MoveForward, KeyCode.W, "Move forward",
                     KeyActionCategory.Movement, "Move forward.");
            Register(KeyAction.MoveBackward, KeyCode.S, "Move backward",
                     KeyActionCategory.Movement, "Move backward.");
            Register(KeyAction.MoveRight, KeyCode.D, "Move right",
                     KeyActionCategory.Movement, "Move to the right.");
            Register(KeyAction.MoveLeft, KeyCode.A, "Move left",
                     KeyActionCategory.Movement, "Move to the left.");
            Register(KeyAction.MoveUp, KeyCode.Q, "Move up",
                     KeyActionCategory.Movement, "Move up.");
            Register(KeyAction.MoveDown, KeyCode.E, "Move down",
                     KeyActionCategory.Movement, "Move down.");
            // Evolution
            Register(KeyAction.SetMarker, KeyCode.Insert, "Set marker",
                     KeyActionCategory.Evolution, "Sets a new marker.");
            Register(KeyAction.DeleteMarker, KeyCode.Delete, "Delete marker",
                     KeyActionCategory.Evolution, "Deletes a marker.");
            Register(KeyAction.ToggleEvolutionCanvases, KeyCode.T, "Toggle evolution canvases",
                     KeyActionCategory.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");
            // Animation
            Register(KeyAction.Previous, KeyCode.LeftArrow, "Previous",
                     KeyActionCategory.Animation, "Go to previous element in the animation.");
            Register(KeyAction.Next, KeyCode.RightArrow, "Next",
                     KeyActionCategory.Animation, "Go to next element in the animation.");
            Register(KeyAction.ToggleAutoPlay, KeyCode.F9, "Toggle auto play",
                     KeyActionCategory.Animation, "Toggles auto play of the animation.");
            Register(KeyAction.IncreaseAnimationSpeed, KeyCode.UpArrow, "Increase animation speed",
                     KeyActionCategory.Animation, "Doubles animation speed.");
            Register(KeyAction.DecreaseAnimationSpeed, KeyCode.DownArrow, "Decrease animation speed",
                     KeyActionCategory.Animation, "Halves animation speed.");
            // Debugging
            Register(KeyAction.ToggleExecutionOrder, KeyCode.O, "Toggle execution order",
                     KeyActionCategory.Debugging, "Toggles execution order (foward/backward).");
            Register(KeyAction.ExecuteToBreakpoint, KeyCode.B, "Execute to breakpoint",
                     KeyActionCategory.Debugging, "Continues execution until next breakpoint is reached.");
            Register(KeyAction.FirstStatement, KeyCode.Home, "First statement",
                     KeyActionCategory.Debugging, "Execution is back to very first statement.");
            // Code Viewer
            Register(KeyAction.ShowWindowMenu, KeyCode.F1, "Show window menu",
                     KeyActionCategory.CodeViewer, "Toggles the menu of the open windows.");
            Register(KeyAction.CodeWindowUndo, KeyCode.F5, "Code-window undo",
                     KeyActionCategory.CodeViewer, "Undoes an edit in the source-code viewer.");
            Register(KeyAction.CodeWindowRedo, KeyCode.F6, "Code-window redo",
                     KeyActionCategory.CodeViewer, "Redoes an undone edit in the source-code viewer.");
            Register(KeyAction.CodeWindowSave, KeyCode.F7, "Code-window save",
                     KeyActionCategory.CodeViewer, "Saves the content of the source-code viewer.");
            Register(KeyAction.RefreshSyntaxHighlighting, KeyCode.F8, "Refresh",
                     KeyActionCategory.CodeViewer, "Refreshes syntax highlighting in the source-code viewer.");
            // Chat
            Register(KeyAction.ToggleTextChat, KeyCode.F2, "Toggle text chat",
                     KeyActionCategory.Chat, "Toggles the text chat.");
        }

        /// <summary>
        /// Returns the <see cref="KeyAction"/> in <see cref="keyBindings"/> that is
        /// triggered by the given <paramref name="keyCode"/>. If a binding is found,
        /// the <see cref="KeyAction"/> bound to the <paramref name="keyCode"/> is
        /// returned in <paramref name="boundKeyAction"/> and <c>true</c> is returned.
        /// Otherwise <c>false</c> is returned and <paramref name="boundKeyAction"/>
        /// is undefined.
        /// </summary>
        /// <param name="keyCode">a <see cref="KeyCode"/> for which a binding
        /// is to be searched</param>
        /// <param name="boundKeyAction">the <see cref="KeyAction"/> bound
        /// to <paramref name="keyCode"/> if one exists; otherwise undefined</param>
        /// <returns><c>true</c> if and only if there is a <see cref="KeyAction"/>
        /// triggered by <paramref name="keyCode"/></returns>
        public static bool TryGetKeyAction(KeyCode keyCode, out KeyAction boundKeyAction)
        {
            foreach (var binding in keyBindings)
            {
                if (binding.Value.KeyCode == keyCode)
                {
                    boundKeyAction = binding.Key;
                    return true;
                }
            }
            boundKeyAction = default;
            return false;
        }

        /// <summary>
        /// Returns the category of given <paramref name="keyAction"/> if this
        /// <paramref name="keyAction"/> is bound at all; otherwise an exception
        /// is thrown.
        /// </summary>
        /// <param name="keyAction"><see cref="KeyAction"/> whose category is to be retrieved</param>
        /// <returns>category for <paramref name="keyAction"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="keyAction"/> is not bound</exception>
        internal static KeyActionCategory GetCategory(KeyAction keyAction)
        {
            if (keyBindings.TryGetValue(keyAction, out KeyActionDescriptor descriptor))
            {
                return descriptor.Category;
            }
            else
            {
                throw new Exception($"{keyAction} is not bound");
            }
        }

        /// <summary>
        /// Rebinds a binding to another key and saves the changed key.
        /// </summary>
        /// <param name="descriptor">the binding that should be triggered by <paramref name="keyCode"/></param>
        /// <param name="keyCode">the key code that should trigger the action represented by <paramref name="descriptor"/></param>
        /// <exception cref="Exception">thrown if <paramref name="keyCode"/> is already bound to an action</exception>
        public static void SetBindingForKey(KeyActionDescriptor descriptor, KeyCode keyCode)
        {
            if (TryGetKeyAction(keyCode, out KeyAction action))
            {
                throw new Exception($"Cannot register key {keyCode} for {descriptor.Name}."
               + $" Key {keyCode} is already bound to {action}.\n");
            }
            else
            {
                KeyCode oldKey = descriptor.KeyCode;
                descriptor.KeyCode = keyCode;
                string path = Application.dataPath + "/StreamingAssets/KeyBindings.json";
                string jsonContent = File.ReadAllText(path);
                List<KeyCode> jsonObject = JsonConvert.DeserializeObject<List<KeyCode>>(jsonContent);
                int index = jsonObject.IndexOf(oldKey);
                jsonObject[index] = keyCode;
                File.WriteAllText(path, JsonConvert.SerializeObject(jsonObject));
            }
        }

        /// <summary>
        /// Returns a list of all keyCodes.
        /// </summary>
        /// <returns>all keyCodes</returns>
        internal static List<KeyCode> GetKeyCodes()
        {
            List<KeyCode> keys = new List<KeyCode>();
            foreach(var binding in keyBindings)
            {
                keys.Add(binding.Value.KeyCode);
            }
            return keys;
        }

        /// <summary>
        /// Returns the current mapping of <see cref="KeyAction"/>s onto their
        /// <see cref="KeyActionDescriptor"/>.
        /// </summary>
        /// <returns>current mapping of key actions onto their descriptor</returns>
        internal static IDictionary<KeyAction, KeyActionDescriptor> AllBindings()
        {
            return keyBindings;
        }
    }
}
