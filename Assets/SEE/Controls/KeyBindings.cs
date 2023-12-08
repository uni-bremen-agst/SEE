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
        private static readonly IDictionary<KeyCode, string> bindings = new Dictionary<KeyCode, string>();
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
        /// <param name="name">the name of the binding</param>
        /// <returns>a new keyCode that gets added to the bindings, when it's not already present in the bindings</returns>
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
        /// <returns>the binding names</returns>
        /// </summary>
        public static string[] GetBindingNames()
        {
            // Extract the scope part from the string value.
            List<string> buttons = new();
            foreach (var binding in bindings)
            {
                int endIndex = binding.Value.IndexOf("[") - 1;
                buttons.Add(binding.Value.Substring(0, endIndex));
            }
            return buttons.ToArray();
        }

        /// <summary>
        /// Rebinds a binding to another key and updates the <see cref="bindings"/>.
        /// <param name="bindingName">the binding, which will be set to a given <param name="keyCode"></param></param>
        /// <returns>false, when the key is already boud to another binding, and true otherwise</returns>
        /// </summary>
        public static bool SetBindingForKey(string bindingName, KeyCode keyCode)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Cannot register key {keyCode} for {bindingName}\n");
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
                return false;
            }
            else
            {
                string bind = bindings.FirstOrDefault(x => x.Value.Contains(bindingName)).Value.ToString();
                KeyCode oldKey = bindings.FirstOrDefault(x => x.Value.Contains(bindingName)).Key;
                bindings.Remove(oldKey);
                bindings[keyCode] = bind;
                SEEInput.UpdateBindings();
                return true;
            }
        }

        /// <summary>
        /// Returns a string of the keyName for a binding.
        /// <param name="bindingName">the binding, for which the key is being returned</param>
        /// <returns>returns the key for a given binding.</returns>
        /// </summary>
        public static string GetKeyNameForBinding(string bindingName)
        {
            return bindings.FirstOrDefault(x => x.Value.Contains(bindingName)).Key.ToString();
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
        /// Returns the <see cref="bindings"/> dictionary.
        /// <returns>returns the current <see cref="bindings"/>.</returns>
        /// </summary>
        public static IDictionary<KeyCode, string> GetBindings()
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
        internal static readonly KeyCode Help = Register(KeyCode.H, HelpBinding, Scope.Always, "Prints help on the key bindings.");
        public const string HelpBinding = "Help";
        /// <summary>
        /// Toggles voice input (i.e., for voice commands) on/off.
        /// </summary>
        internal static readonly KeyCode ToggleVoiceInput = Register(KeyCode.Period, ToggleVoiceInputBinding, Scope.Always,
                                                                     "Toggles voice input on/off.");
        public const string ToggleVoiceInputBinding = "ToggleVoiceInput";

        #endregion

        //-----------------------------------------------------------------
        #region Menu
        // Note: The digits 0-9 are used as short cuts for the menu entries
        //-----------------------------------------------------------------

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        internal static readonly KeyCode ToggleMenu = Register(KeyCode.Space, ToggleMenuBinding, Scope.Always, "Turns on/off the player-action menu.");
        public const string ToggleMenuBinding = "ToggleMenu";

        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        internal static readonly KeyCode ToggleSettings = Register(KeyCode.Pause, ToggleSettingsBinding, Scope.Always, "Turns on/off the settings menu.");
        public const string ToggleSettingsBinding = "ToggleSettings";

        /// <summary>
        /// Turns on/off the browser.
        /// </summary>
        internal static readonly KeyCode ToggleBrowser = Register(KeyCode.F4, ToggleBrowserBinding, Scope.Always, "Turns on/off the browser.");
        public const string ToggleBrowserBinding = "ToggleBrowser";

        /// <summary>
        /// Opens the search menu.
        /// </summary>
        internal static readonly KeyCode SearchMenu = Register(KeyCode.F, SearchMenuBinding, Scope.Always, "Opens the search menu.");
        public const string SearchMenuBinding = "SearchMenu";

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        internal static readonly KeyCode Undo = Register(KeyCode.Z, UndoBinding, Scope.Always, "Undoes the last action.");
        public const string UndoBinding = "Undo";

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        internal static readonly KeyCode Redo = Register(KeyCode.Y, RedoBinding, Scope.Always, "Re-does the last action.");
        public const string RedoBinding = "Redo";

        /// <summary>
        /// Opens/closes the configuration menu.
        /// </summary>
        internal static readonly KeyCode ConfigMenu = Register(KeyCode.K, ConfigMenuBinding, Scope.Always, "Opens/closes the configuration menu.");
        public const string ConfigMenuBinding = "ConfigMenu";

        /// <summary>
        /// Opens/closes the tree view window.
        /// </summary>
        internal static readonly KeyCode TreeView = Register(KeyCode.Tab, TreeViewBinding, Scope.Always, "Opens/closes the tree view window.");
        public const string TreeViewBinding = "TreeView";

        #endregion

        //-----------------------------------------------------
        #region Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        internal static readonly KeyCode SavePathPosition = Register(KeyCode.F11, SavePathPositionBinding, Scope.CameraPaths, "Saves the current position when recording paths.");
        public const string SavePathPositionBinding = "SavePathPosition";

        /// <summary>
        /// Starts/stops the automated path replay.
        /// </summary>
        internal static readonly KeyCode TogglePathPlaying = Register(KeyCode.F12, TogglePathPlayingBinding, Scope.CameraPaths, "Starts/stops the automated camera movement along a path.");
        public const string TogglePathPlayingBinding = "TogglePathPlaying";

        #endregion

        //-----------------------------------------------------
        #region Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        internal static KeyCode ToggleCharts = Register(KeyCode.M, ToggleChartsBinding, Scope.MetricCharts, "Turns the metric charts on/off.");
        public const string ToggleChartsBinding = "ToggleCharts";

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        internal static readonly KeyCode ToggleMetricHoveringSelection = Register(KeyCode.N, ToggleMetricHoveringSelectionBinding, Scope.MetricCharts, "Toggles hovering/selection for markers in metric charts.");
        public const string ToggleMetricHoveringSelectionBinding = "ToggleMetricHoveringSelection";

        #endregion


        //-----------------------------------------------------
        #region Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the visibility of all edges of a hovered code city.
        /// </summary>
        internal static KeyCode ToggleEdges = Register(KeyCode.V, ToggleEdgesBinding, Scope.Browsing, "Toggles the visibility of all edges of a hovered code city.");
        public const string ToggleEdgesBinding = "ToggleEdges";

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        internal static readonly KeyCode Unselect = Register(KeyCode.U, UnselectBinding, Scope.Browsing, "Forgets all currently selected objects.");
        public const string UnselectBinding = "Unselect";

        /// <summary>
        /// Cancels an action.
        /// </summary>
        internal static readonly KeyCode Cancel = Register(KeyCode.Escape, CancelBinding, Scope.Browsing, "Cancels an action.");
        public const string CancelBinding = "Cancel";

        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        internal static readonly KeyCode Reset = Register(KeyCode.R, ResetBinding, Scope.Browsing, "Resets a code city to its original position and scale.");
        public const string ResetBinding = "Reset";

        /// <summary>
        /// Zooms into a city.
        /// </summary>
        internal static readonly KeyCode ZoomInto = Register(KeyCode.G, ZoomIntoBinding, Scope.Browsing, "To zoom into a city.");
        public const string ZoomIntoBinding = "ZoomInto";

        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        internal static readonly KeyCode Snap = Register(KeyCode.LeftAlt, SnapBinding, Scope.Browsing, "Snap move/rotate city.");
        public const string SnapBinding = "Snap";

        /// <summary>
        /// The user drags the city as a whole on the plane.
        /// </summary>
        internal static KeyCode DragHovered = Register(KeyCode.LeftControl, DragHoveredBinding, Scope.Browsing, "Drag code city.");
        public const string DragHoveredBinding = "DragHovered";

        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        internal static readonly KeyCode ToggleCameraLock = Register(KeyCode.L, ToggleCameraLockBinding, Scope.Browsing, "Toggles between the locked and free camera mode.");
        public const string ToggleCameraLockBinding = "ToggleCameraLock";

        /// <summary>
        /// Toggles between pointing.
        /// </summary>
        internal static readonly KeyCode Pointing = Register(KeyCode.P, PointingBinding, Scope.Browsing, "Toggles between Pointing.");
        public const string PointingBinding = "Pointing";

        #endregion

        //-----------------------------------------------------
        #region Player (camera) movements.
        //-----------------------------------------------------

        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        internal static readonly KeyCode BoostCameraSpeed = Register(KeyCode.LeftShift, BoostCameraSpeedBinding, Scope.Movement, "Boosts the speed of the player movement. While pressed, movement is faster.");
        public const string BoostCameraSpeedBinding = "BoostCameraSpeed";

        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        internal static readonly KeyCode MoveForward = Register(KeyCode.W, MoveForwardBinding, Scope.Movement, "Move forward.");
        public const string MoveForwardBinding = "MoveForward";

        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        internal static readonly KeyCode MoveBackward = Register(KeyCode.S, MoveBackwardBinding, Scope.Movement, "Move backward.");
        public const string MoveBackwardBinding = "MoveBackward";

        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        internal static readonly KeyCode MoveRight = Register(KeyCode.D, MoveRightBinding, Scope.Movement, "Move to the right.");
        public const string MoveRightBinding = "MoveRight";

        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        internal static readonly KeyCode MoveLeft = Register(KeyCode.A, MoveLeftBinding, Scope.Movement, "Move to the left.");
        public const string MoveLeftBinding = "MoveLeft";

        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        internal static readonly KeyCode MoveUp = Register(KeyCode.Q, MoveUpBinding, Scope.Movement, "Move up.");
        public const string MoveUpBinding = "MoveUp";

        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        internal static readonly KeyCode MoveDown = Register(KeyCode.E, MoveDownBinding, Scope.Movement, "Move down.");
        public const string MoveDownBinding = "MoveDown";

        #endregion

        //--------------------------
        #region Evolution
        //--------------------------

        /// <summary>
        /// Sets a new marker.
        /// </summary>
        internal static readonly KeyCode SetMarker = Register(KeyCode.Insert, SetMarkerBinding, Scope.Evolution, "Sets a new marker.");
        public const string SetMarkerBinding = "SetMarker";

        /// <summary>
        /// Deletes a marker.
        /// </summary>
        internal static readonly KeyCode DeleteMarker = Register(KeyCode.Delete, DeleteMarkerBinding, Scope.Evolution, "Deletes a marker.");
        public const string DeleteMarkerBinding = "DeleteMarker";

        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        internal static readonly KeyCode ToggleEvolutionCanvases = Register(KeyCode.T, ToggleEvolutionCanvasesBinding, Scope.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");
        public const string ToggleEvolutionCanvasesBinding = "ToggleEvolutionCanvases";

        #endregion

        //----------------------------------------------------
        #region Animation (shared by Debugging and Evolution)
        //----------------------------------------------------

        /// <summary>
        /// The previous element in the animation is to be shown.
        /// </summary>
        internal static readonly KeyCode Previous = Register(KeyCode.LeftArrow, PreviousBinding, Scope.Animation, "Go to previous element in the animation.");
        public const string PreviousBinding = "Previous";

        /// <summary>
        /// The next element in the animation is to be shown.
        /// </summary>
        internal static readonly KeyCode Next = Register(KeyCode.RightArrow, NextBinding, Scope.Animation, "Go to next element in the animation.");
        public const string NextBinding = "Next";

        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        internal static readonly KeyCode ToggleAutoPlay = Register(KeyCode.F9, ToggleAutoPlayBinding, Scope.Animation, "Toggles auto play of the animation.");
        public const string ToggleAutoPlayBinding = "ToggleAutoPlay";

        /// <summary>
        /// Double animation speed.
        /// </summary>
        internal static readonly KeyCode IncreaseAnimationSpeed = Register(KeyCode.UpArrow, IncreaseAnimationSpeedBinding, Scope.Animation, "Doubles animation speed.");
        public const string IncreaseAnimationSpeedBinding = "IncreaseAnimationSpeed";

        /// <summary>
        /// Halve animation speed.
        /// </summary>
        internal static readonly KeyCode DecreaseAnimationSpeed = Register(KeyCode.DownArrow, DecreaseAnimationSpeedBinding, Scope.Animation, "Halves animation speed.");
        public const string DecreaseAnimationSpeedBinding = "DecreaseAnimationSpeed";

        #endregion

        //--------------------------
        #region Debugging
        //--------------------------

        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        internal static readonly KeyCode ToggleExecutionOrder = Register(KeyCode.O, ToggleExecutionOrderBinding, Scope.Debugging, "Toggles execution order (foward/backward).");
        public const string ToggleExecutionOrderBinding = "ToggleExecutionOrder";

        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        internal static readonly KeyCode ExecuteToBreakpoint = Register(KeyCode.B, ExecuteToBreakpointBinding, Scope.Debugging, "Continues execution until next breakpoint is reached.");
        public const string ExecuteToBreakpointBinding = "ExecuteToBreakpoint";

        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        internal static readonly KeyCode FirstStatement = Register(KeyCode.Home, FirstStatementBinding, Scope.Debugging, "Execution is back to very first statement.");
        public const string FirstStatementBinding = "FirstStatement";

        #endregion

        //--------------------
        #region Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the available windows.
        /// </summary>
        internal static readonly KeyCode ShowWindowMenu = Register(KeyCode.F1, ShowWindowMenuBinding, Scope.CodeViewer, "Toggles the menu of the open windows.");
        public const string ShowWindowMenuBinding = "ShowWindowMenu";

        /// <summary>
        /// Undoes an edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowUndo = Register(KeyCode.F5, CodeWindowUndoBinding, Scope.CodeViewer, "Undoes an edit in the source-code viewer.");
        public const string CodeWindowUndoBinding = "CodeWindowUndo";

        /// <summary>
        /// Redoes an undone edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowRedo = Register(KeyCode.F6, CodeWindowRedoBinding, Scope.CodeViewer, "Redoes an undone edit in the source-code viewer.");
        public const string CodeWindowRedoBinding = "CodeWindowRedo";

        /// <summary>
        /// Saves the content of the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowSave = Register(KeyCode.F7, CodeWindowSaveBinding, Scope.CodeViewer, "Saves the content of the source-code viewer.");
        public const string CodeWindowSaveBinding = "CodeWindowSave";

        /// <summary>
        /// Refreshes syntax highlighting in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode RefreshSyntaxHighlighting = Register(KeyCode.F8, RefreshSyntaxHighlightingBinding, Scope.CodeViewer, "Refreshes syntax highlighting in the source-code viewer.");
        public const string RefreshSyntaxHighlightingBinding = "RefreshSyntaxHighlighting";

        #endregion

        //-----------------------------------------------------
        #region Text chat to communicate with other remote players
        //-----------------------------------------------------

        /// <summary>
        /// Opens the text chat.
        /// </summary>
        internal static readonly KeyCode OpenTextChat = Register(KeyCode.F2, OpenTextChatBinding, Scope.Chat, "Opens the text chat.");
        public const string OpenTextChatBinding = "OpenTextChat";

        #endregion

        //-----------------------------------------------------
        #region Notifications
        //-----------------------------------------------------

        /// <summary>
        /// Closes all open notifications.
        /// </summary>
        internal static readonly KeyCode CloseNotifications = Register(KeyCode.X, CloseNotificationsBinding, Scope.Always, "Clears all notifications.");
        public const string CloseNotificationsBinding = "CloseNotifications";

        #endregion

        #region FaceCam

        /// <summary>
        /// Toggles the face camera.
        /// </summary>
        internal static readonly KeyCode ToggleFaceCam
            = Register(KeyCode.I, ToggleFaceCamBinding, Scope.Always, "Toggles the face camera on or off.");
        public const string ToggleFaceCamBinding = "ToggleFaceCam";

        /// <summary>
        /// Toggles the position of the FaceCam on the player's face.
        /// </summary>
        internal static readonly KeyCode ToggleFaceCamPosition
            = Register(KeyCode.F3, ToggleFaceCamPositionBinding, Scope.Always, "Toggles the position of the FaceCam on the player's face.");
        public const string ToggleFaceCamPositionBinding = "ToggleFaceCamPosition";

        #endregion

        #region Holistic Metric Menu

        //-----------------------------------------------------
        // Holistic metrics menu
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the menu for holistic code metrics.
        /// </summary>
        internal static readonly KeyCode ToggleHolisticMetricsMenu = Register(KeyCode.C, ToggleHolisticMetricsMenuBinding, Scope.Always,
                                                                              "Toggles the menu for holistic code metrics");
        public const string ToggleHolisticMetricsMenuBinding = "ToggleHolisticMetricsMenu";

        #endregion

    }
}
