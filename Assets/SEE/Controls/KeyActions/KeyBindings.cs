using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// Defines the codes for all interaction based on the keyboard in SEE.
    /// </summary>
    internal static class KeyBindings
    {
        // IMPORTANT NOTES:
        // (1) Keep in mind that KeyCodes in Unity map directly to a
        //     physical key on a keyboard with an English layout.
        // (2) Ctrl-Z and Ctrl-Y are reserved for Undo and Redo.
        // (3) The digits 0-9 are reserved for shortcuts for the player menu.

        /// <summary>
        /// Defines the path of the file where the <see cref="keybindings"/> are stored.
        /// </summary>
        private static readonly string keyBindingsPath = Application.dataPath + "/StreamingAssets/KeyBindings.json";

        /// <summary>
        /// Mapping of every <see cref="KeyAction"/> onto its currently
        /// bound <see cref="KeyActionDescriptor"/>.
        /// </summary>
        /// <remarks>Key actions can be re-bound.</remarks>
        private static readonly KeyMap keyBindings = new();

        /// <summary>
        /// Returns true if the user has pressed down a key requesting the given <paramref name="keyAction"/>
        /// in the last frame.
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
            return keyBindings.TryGetValue(keyAction, out keyCode);
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
            try
            {
                keyBindings.Bind(keyAction, new KeyActionDescriptor(name, description, scope, keyCode));
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
            Register(KeyAction.Help, KeyCode.H, "Help",
                     KeyActionCategory.General, "Provides help");
            Register(KeyAction.ToggleMenu, KeyCode.Space, "Toggle menu",
                     KeyActionCategory.General, "Toggles the user-action menu.");
            Register(KeyAction.ToggleSettings, KeyCode.Pause, "Toggle settings",
                     KeyActionCategory.General, "Turns on/off the settings menu.");
            Register(KeyAction.ConfigMenu, KeyCode.K, "Config",
                     KeyActionCategory.General, "Opens/closes the configuration menu.");
            Register(KeyAction.ToggleBrowser, KeyCode.F4, "Toggle browser",
                     KeyActionCategory.General, "Turns on/off the browser.");
            Register(KeyAction.ToggleMirror, KeyCode.F8, "Toggle mirror",
                     KeyActionCategory.General, "Turns on/off the mirror.");
            Register(KeyAction.Undo, KeyCode.Z, "Undo",
                     KeyActionCategory.General, "Undoes the last action.");
            Register(KeyAction.Redo, KeyCode.Y, "Redo",
                     KeyActionCategory.General, "Re-does the last action.");
            Register(KeyAction.CloseNotifications, KeyCode.X, "Close notifications",
                     KeyActionCategory.General, "Clears all notifications.");
            Register(KeyAction.ToggleFaceCam, KeyCode.I, "Toggle face cam",
                     KeyActionCategory.General, "Toggles the face camera on or off.");
            Register(KeyAction.ToggleFaceCamPosition, KeyCode.F3, "Toggle face-cam position",
                     KeyActionCategory.General, "Toggles the position of the FaceCam on the player's face.");
            Register(KeyAction.ToggleVoiceControl, KeyCode.Period, "Toggle voice control",
                     KeyActionCategory.General, "Toggles voice controlled commands.");

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

            // MetricCharts
            Register(KeyAction.ToggleCharts, KeyCode.M, "Toggle charts",
                     KeyActionCategory.MetricCharts, "Turns the metric charts on/off.");
            Register(KeyAction.ToggleMetricHoveringSelection, KeyCode.N, "Toggle metric hovering/selection",
                     KeyActionCategory.MetricCharts, "Toggles hovering/selection for markers in metric charts.");
            Register(KeyAction.ToggleHolisticMetricsMenu, KeyCode.C, "Toggle metrics menu",
                     KeyActionCategory.MetricCharts, "Toggles the menu for holistic code metrics");

            // Browsing
            Register(KeyAction.SearchMenu, KeyCode.F, "Search",
                     KeyActionCategory.Browsing, "Opens the search menu.");
            Register(KeyAction.TreeView, KeyCode.Tab, "Tree view",
                     KeyActionCategory.Browsing, "Opens/closes the tree view window.");
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
            // Chat
            Register(KeyAction.ToggleTextChat, KeyCode.F2, "Toggle text chat",
                     KeyActionCategory.Chat, "Toggles the text chat.");
            Register(KeyAction.ToggleVoiceChat, KeyCode.F5, "Toggle voice chat",
                     KeyActionCategory.Chat, "Toggles the voice chat.");

            // Drawable
            Register(KeyAction.PartUndo, KeyCode.CapsLock, "Part undo",
                     KeyActionCategory.Drawable, "Undoes a part of the currently running action.");
            Register(KeyAction.MoveObjectUp, KeyCode.Keypad8, "Move object up",
                     KeyActionCategory.Drawable, "Moves the object up.");
            Register(KeyAction.MoveObjectDown, KeyCode.Keypad2, "Move object down",
                     KeyActionCategory.Drawable, "Moves the object down.");
            Register(KeyAction.MoveObjectLeft, KeyCode.Keypad4, "Move object left",
                     KeyActionCategory.Drawable, "Moves the object left.");
            Register(KeyAction.MoveObjectRight, KeyCode.Keypad6, "Move object right",
                     KeyActionCategory.Drawable, "Moves the object right.");
            Register(KeyAction.MoveObjectForward, KeyCode.Keypad9, "Move object forward",
                     KeyActionCategory.Drawable, "Moves the object forward.");
            Register(KeyAction.MoveObjectBackward, KeyCode.Keypad3, "Move object backward",
                     KeyActionCategory.Drawable, "Moves the object backward.");
            Register(KeyAction.DrawableManagerView, KeyCode.Keypad0, "Toggle drawable manager menu.",
                     KeyActionCategory.Drawable, "Turns on/off the drawable manager menu.");

            // CameraPaths
            Register(KeyAction.SavePathPosition, KeyCode.F11, "Save position",
                     KeyActionCategory.CameraPaths, "Saves the current position when recording paths.");
            Register(KeyAction.TogglePathPlaying, KeyCode.F12, "Toggle path playing",
                     KeyActionCategory.CameraPaths, "Starts/stops the automated camera movement along a path.");
        }

        /// <summary>
        /// Rebinds a binding to another key and saves the changed key.
        /// </summary>
        /// <param name="descriptor">the binding that should be triggered by <paramref name="keyCode"/></param>
        /// <param name="keyCode">the key code that should trigger the action represented by <paramref name="descriptor"/></param>
        /// <exception cref="Exception">thrown if <paramref name="keyCode"/> is already bound to an action</exception>
        internal static void SetBindingForKey(KeyActionDescriptor descriptor, KeyCode keyCode)
        {
            keyBindings.ResetKeyCode(descriptor, keyCode);
            keyBindings.Save(keyBindingsPath);
        }

        /// <summary>
        /// Returns the current mapping of <see cref="KeyAction"/>s onto their
        /// <see cref="KeyActionDescriptor"/> grouped by their <see cref="KeyActionCategory"/>.
        /// </summary>
        /// <returns>current mapping of key actions onto their descriptor</returns>
        internal static IEnumerable<IGrouping<KeyActionCategory, KeyValuePair<KeyAction, KeyActionDescriptor>>> AllBindings()
        {
            return keyBindings.AllBindings();
        }

        /// <summary>
        /// Loads the key bindings.
        /// </summary>
        internal static void LoadKeyBindings()
        {
            keyBindings.Load(keyBindingsPath);
        }
    }
}
