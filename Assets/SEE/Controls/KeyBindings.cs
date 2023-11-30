using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Defines the key codes for all interaction based on the keyboard in SEE.
    /// </summary>
    public static class KeyBindings
    {
        // IMPORTANT NOTES:
        // (1) Keep in mind that KeyCodes in Unity map directly to a
        //     physical key on an keyboard with an English layout.
        // (2) Ctrl-Z and Ctrl-Y are reserved for Undo and Redo.
        // (3) The digits 0-9 are reserved for shortcuts for the player menu.

        /// <summary>
        /// The registered keyboard shortcuts. The value is a help message on the shortcut.
        /// </summary>
        private static readonly Dictionary<KeyCode, string> bindings = new();
        /// <summary>
        /// Categories for the keyboard shortcuts.
        /// </summary>
        private enum Scope
        {
            Always,
            Animation,     // animation speed
            Architecture,  // use case architecture; related to architecture mapping and analysis
            Browsing,      // browsing a code city (panning, zooming, etc.)
            CameraPaths,   // recording a camera (player) path
            Chat,          // text chatting with other remote players
            CodeViewer,    // source-code viewer
            Debugging,     // use case debugging
            Evolution,     // use case evolution; observing the series of revisions of a city
            MetricCharts,  // showing metric charts
            Movement,      // moving the player within the world
        }

        /// <summary>
        /// Registers the given <paramref name="keyCode"/> for the given <paramref name="scope"/>
        /// and the <paramref name="helpMessage"/>. If a <paramref name="keyCode"/> is already registered,
        /// an error message will be emitted.
        /// </summary>
        /// <param name="keyCode">the key code to be registered</param>
        /// <param name="scope">the scope of the key code</param>
        /// <param name="helpMessage">the help message for the key code</param>
        /// <returns></returns>
        private static KeyCode Register(KeyCode keyCode, string name, Scope scope, string helpMessage)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Cannot register key {keyCode} for [{scope}] {helpMessage}\n");
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
            }
            else
            {
                bindings[keyCode] = $"{name} [{scope}] {helpMessage}";
            }
            return keyCode;
        }
        /// <summary>
        /// Returns the scope of given key-binding description <paramref name="value"/>.
        /// </summary>
        /// <param name="value">the key-binding description from which to extract the scope</param>
        /// <returns>the scope</returns>
        public static string GetScope(string value)
        {
            // Extract the scope part from the string value.
            int startIndex = value.IndexOf("[") + 1;
            int endIndex = value.IndexOf("]");
            return value[startIndex..endIndex];
        }

        /// <summary>
        /// Returns a string array of the binding names.
        /// </summary>
        public static string[] GetButtonNames()
        {
            // Extract the scope part from the string value.
            List<string> buttons = new List<string>();
            foreach (var binding in bindings)
            {
                int endIndex = binding.Value.IndexOf("[")-1;
                buttons.Add(binding.Value.Substring(0, endIndex));

            }
            return buttons.ToArray();
        }

        /// <summary>
        /// Rebinds a binding to another key and updates the keyBindings.
        /// </summary>
        public static bool SetButtonForKey(string buttonName, KeyCode keyCode)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Cannot register key {keyCode} for {buttonName}\n");
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
                return false;
            }
            else
            {
                string bind = bindings.FirstOrDefault(x => x.Value.Contains(buttonName)).Value.ToString();
                KeyCode oldKey = bindings.FirstOrDefault(x => x.Value.Contains(buttonName)).Key;
                bindings.Remove(oldKey);
                bindings[keyCode] = bind;
                SEEInput.UpdateBindings();
                return true;
            }
        }

        /// <summary>
        /// Returns a string of the keyName for a button.
        /// </summary>
        public static string GetKeyNameForButton(string buttonName)
        {
            return bindings.FirstOrDefault(x => x.Value.Contains(buttonName)).Key.ToString(); ;
        }

        /// <summary>
        /// Prints the current key bindings to the debugging console along with their
        /// help message.
        /// </summary>
        internal static void PrintBindings()
        {
            System.Text.StringBuilder sb = new("Key Bindings:\n");
            foreach (var binding in bindings)
            {
                sb.Append($"Key {binding.Key}: {binding.Value}\n");
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Returns the keyBindings dictionary.
        /// </summary>
        public static Dictionary<KeyCode, string> GetBindings()
        {
            return bindings;
        }

        /// <summary>
        /// Returns a string of the current key bindings along with their help message.
        /// </summary>
        internal static string GetBindingsText()
        {
            var groupedBindings = bindings.GroupBy(pair => GetScope(pair.Value));
            System.Text.StringBuilder sb = new();
            foreach (var group in groupedBindings)
            {
                // Display the scope
                sb.Append("\n\n\n" + new string(' ', 10) + $"—{group.Key}—\n\n\n" + new string('-', 105) + "\n");

                foreach (var binding in group)
                {
                    int index = binding.Value.IndexOf(']')+1;
                    // Display individual binding details
                    sb.Append($"Key {binding.Key}:\n\n {binding.Value.Substring(index)}\n" + new string('-', 105));
                }
            }
            return sb.ToString();
        }

        //-----------------------------------------------------
        #region General key bindings
        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        internal static readonly KeyCode Help = Register(KeyCode.H, "Help", Scope.Always, "Prints help on the key bindings.");

        /// <summary>
        /// Toggles voice input (i.e., for voice commands) on/off.
        /// </summary>
        internal static readonly KeyCode ToggleVoiceInput = Register(KeyCode.Period, "ToggleVoiceInput", Scope.Always,
                                                                     "Toggles voice input on/off.");

        #endregion

        //-----------------------------------------------------------------
        #region Menu
        // Note: The digits 0-9 are used as short cuts for the menu entries
        //-----------------------------------------------------------------

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        internal static readonly KeyCode ToggleMenu = Register(KeyCode.Space, "ToggleMenu", Scope.Always, "Turns on/off the player-action menu.");

        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        internal static readonly KeyCode ToggleSettings = Register(KeyCode.Pause, "ToggleSettings", Scope.Always, "Turns on/off the settings menu.");

        /// <summary>
        /// Turns on/off the browser.
        /// </summary>
        internal static readonly KeyCode ToggleBrowser = Register(KeyCode.F4, "ToggleBrowser", Scope.Always, "Turns on/off the browser.");

        /// <summary>
        /// Opens the search menu.
        /// </summary>
        internal static readonly KeyCode SearchMenu = Register(KeyCode.F, "SearchMenu", Scope.Always, "Opens the search menu.");

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        internal static readonly KeyCode Undo = Register(KeyCode.Z, "Undo", Scope.Always, "Undoes the last action.");

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        internal static readonly KeyCode Redo = Register(KeyCode.Y, "Redo", Scope.Always, "Re-does the last action.");

        /// <summary>
        /// Opens/closes the configuration menu.
        /// </summary>
        internal static readonly KeyCode ConfigMenu = Register(KeyCode.K, "ConfigMenu", Scope.Always, "Opens/closes the configuration menu.");

        /// <summary>
        /// Opens/closes the tree view window.
        /// </summary>
        internal static readonly KeyCode TreeView = Register(KeyCode.Tab, "TreeView", Scope.Always, "Opens/closes the tree view window.");

        #endregion

        //-----------------------------------------------------
        #region Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        internal static readonly KeyCode SavePathPosition = Register(KeyCode.F11, "SavePathPosition", Scope.CameraPaths, "Saves the current position when recording paths.");

        /// <summary>
        /// Starts/stops the automated path replay.
        /// </summary>
        internal static readonly KeyCode TogglePathPlaying = Register(KeyCode.F12, "TogglePathPlaying", Scope.CameraPaths, "Starts/stops the automated camera movement along a path.");

        #endregion

        //-----------------------------------------------------
        #region Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        internal static KeyCode ToggleCharts = Register(KeyCode.M, "ToggleCharts", Scope.MetricCharts, "Turns the metric charts on/off.");

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        internal static readonly KeyCode ToggleMetricHoveringSelection = Register(KeyCode.N, "ToggleMetricHoveringSelection", Scope.MetricCharts, "Toggles hovering/selection for markers in metric charts.");

        #endregion


        //-----------------------------------------------------
        #region Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the visibility of all edges of a hovered code city.
        /// </summary>
        internal static KeyCode ToggleEdges = Register(KeyCode.V, "ToggleEdges", Scope.Browsing, "Toggles the visibility of all edges of a hovered code city.");


        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        internal static readonly KeyCode Unselect = Register(KeyCode.U, "Unselect", Scope.Browsing, "Forgets all currently selected objects.");
        /// <summary>
        /// Cancels an action.
        /// </summary>
        internal static readonly KeyCode Cancel = Register(KeyCode.Escape, "Cancel", Scope.Browsing, "Cancels an action.");
        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        internal static readonly KeyCode Reset = Register(KeyCode.R, "Reset", Scope.Browsing, "Resets a code city to its original position and scale.");
        /// <summary>
        /// Zooms into a city.
        /// </summary>
        internal static readonly KeyCode ZoomInto = Register(KeyCode.G, "ZoomInto", Scope.Browsing, "To zoom into a city.");
        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        internal static readonly KeyCode Snap = Register(KeyCode.LeftAlt, "Snap", Scope.Browsing, "Snap move/rotate city.");
        /// <summary>
        /// The user drags the city as a whole on the plane.
        /// </summary>
        internal static KeyCode DragHovered = Register(KeyCode.LeftControl, "DragHovered", Scope.Browsing, "Drag code city.");
        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        internal static readonly KeyCode ToggleCameraLock = Register(KeyCode.L, "ToggleCameraLock", Scope.Browsing, "Toggles between the locked and free camera mode.");
        /// <summary>
        /// Toggles between pointing.
        /// </summary>
        internal static readonly KeyCode Pointing = Register(KeyCode.P, "Pointing", Scope.Browsing, "Toggles between Pointing.");

        #endregion

        //-----------------------------------------------------
        #region Player (camera) movements.
        //-----------------------------------------------------

        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        internal static readonly KeyCode BoostCameraSpeed = Register(KeyCode.LeftShift, "BoostCameraSpeed", Scope.Movement, "Boosts the speed of the player movement. While pressed, movement is faster.");
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        internal static readonly KeyCode MoveForward = Register(KeyCode.W, "MoveForward", Scope.Movement, "Move forward.");
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        internal static readonly KeyCode MoveBackward = Register(KeyCode.S, "MoveBackward", Scope.Movement, "Move backward.");
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        internal static readonly KeyCode MoveRight = Register(KeyCode.D, "MoveRight", Scope.Movement, "Move to the right.");
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        internal static readonly KeyCode MoveLeft = Register(KeyCode.A, "MoveLeft", Scope.Movement, "Move to the left.");
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        internal static readonly KeyCode MoveUp = Register(KeyCode.Q, "MoveUp", Scope.Movement, "Move up.");
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        internal static readonly KeyCode MoveDown = Register(KeyCode.E, "MoveDown", Scope.Movement, "Move down.");

        #endregion

        //--------------------------
        #region Evolution
        //--------------------------

        /// <summary>
        /// Sets a new marker.
        /// </summary>
        internal static readonly KeyCode SetMarker = Register(KeyCode.Insert, "SetMarker", Scope.Evolution, "Sets a new marker.");
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        internal static readonly KeyCode DeleteMarker = Register(KeyCode.Delete, "DeleteMarker", Scope.Evolution, "Deletes a marker.");
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        internal static readonly KeyCode ToggleEvolutionCanvases = Register(KeyCode.T, "ToggleEvolutionCanvases", Scope.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");

        #endregion

        //----------------------------------------------------
        #region Animation (shared by Debugging and Evolution)
        //----------------------------------------------------

        /// <summary>
        /// The previous element in the animation is to be shown.
        /// </summary>
        internal static readonly KeyCode Previous = Register(KeyCode.LeftArrow, "Previous", Scope.Animation, "Go to previous element in the animation.");
        /// <summary>
        /// The next element in the animation is to be shown.
        /// </summary>
        internal static readonly KeyCode Next = Register(KeyCode.RightArrow, "Next", Scope.Animation, "Go to next element in the animation.");
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        internal static readonly KeyCode ToggleAutoPlay = Register(KeyCode.F9, "ToggleAutoPlay", Scope.Animation, "Toggles auto play of the animation.");
        /// <summary>
        /// Double animation speed.
        /// </summary>
        internal static readonly KeyCode IncreaseAnimationSpeed = Register(KeyCode.UpArrow, "IncreaseAnimationSpeed", Scope.Animation, "Doubles animation speed.");
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        internal static readonly KeyCode DecreaseAnimationSpeed = Register(KeyCode.DownArrow, "DecreaseAnimationSpeed", Scope.Animation, "Halves animation speed.");

        #endregion

        //--------------------------
        #region Debugging
        //--------------------------

        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        internal static readonly KeyCode ToggleExecutionOrder = Register(KeyCode.O, "ToggleExecutionOrder", Scope.Debugging, "Toggles execution order (foward/backward).");
        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        internal static readonly KeyCode ExecuteToBreakpoint = Register(KeyCode.B, "ExecuteToBreakpoint", Scope.Debugging, "Continues execution until next breakpoint is reached.");
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        internal static readonly KeyCode FirstStatement = Register(KeyCode.Home, "FirstStatement", Scope.Debugging, "Execution is back to very first statement.");

        #endregion

        //--------------------
        #region Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the available windows.
        /// </summary>
        internal static readonly KeyCode ShowWindowMenu = Register(KeyCode.F1, "ShowWindowMenu", Scope.CodeViewer, "Toggles the menu of the open windows.");

        /// <summary>
        /// Undoes an edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowUndo = Register(KeyCode.F5, "CodeWindowUndo", Scope.CodeViewer, "Undoes an edit in the source-code viewer.");

        /// <summary>
        /// Redoes an undone edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowRedo = Register(KeyCode.F6, "CodeWindowRedo", Scope.CodeViewer, "Redoes an undone edit in the source-code viewer.");

        /// <summary>
        /// Saves the content of the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowSave = Register(KeyCode.F7, "CodeWindowSave", Scope.CodeViewer, "Saves the content of the source-code viewer.");

        /// <summary>
        /// Refreshes syntax highlighting in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode RefreshSyntaxHighlighting = Register(KeyCode.F8, "RefreshSyntaxHighlighting", Scope.CodeViewer, "Refreshes syntax highlighting in the source-code viewer.");

        #endregion

        //-----------------------------------------------------
        #region Text chat to communicate with other remote players
        //-----------------------------------------------------

        /// <summary>
        /// Opens the text chat.
        /// </summary>
        internal static readonly KeyCode OpenTextChat = Register(KeyCode.F2, "OpenTextChat", Scope.Chat, "Opens the text chat.");

        #endregion

        //-----------------------------------------------------
        #region Notifications
        //-----------------------------------------------------

        /// <summary>
        /// Closes all open notifications.
        /// </summary>
        internal static readonly KeyCode CloseNotifications = Register(KeyCode.X, "CloseNotifications", Scope.Always, "Clears all notifications.");

        #endregion

        #region FaceCam

        /// <summary>
        /// Toggles the face camera.
        /// </summary>
        internal static readonly KeyCode ToggleFaceCam
            = Register(KeyCode.I, "ToggleFaceCam", Scope.Always, "Toggles the face camera on or off.");

        /// <summary>
        /// Toggles the position of the FaceCam on the player's face.
        /// </summary>
        internal static readonly KeyCode ToggleFaceCamPosition
            = Register(KeyCode.F3, "ToggleFaceCamPosition", Scope.Always, "Toggles the position of the FaceCam on the player's face.");

        #endregion

        #region Holistic Metric Menu

        //-----------------------------------------------------
        // Holistic metrics menu
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the menu for holistic code metrics.
        /// </summary>
        internal static readonly KeyCode ToggleHolisticMetricsMenu = Register(KeyCode.C, "ToggleHolisticMetricsMenu", Scope.Always,
                                                                              "Toggles the menu for holistic code metrics");

        #endregion

    }
}
