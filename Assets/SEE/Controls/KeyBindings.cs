using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Defines the key codes for all interaction based on the keyboard in SEE.
    /// </summary>
    internal static class KeyBindings
    {
        // IMPORTANT NOTES:
        // (1) Keep in mind that KeyCodes in Unity map directly to a
        //     physical key on an keyboard with an English layout.
        // (2) Ctrl-Z and Ctrl-Y are reserved for Undo and Redo.
        // (3) The digits 0-9 are reserved for shortcuts for the player menu.

        /// <summary>
        /// The registered keyboard shortcuts. The value is a help message on the shortcut.
        /// </summary>
        private static readonly Dictionary<KeyCode, string> bindings = new Dictionary<KeyCode, string>();

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
        private static KeyCode Register(KeyCode keyCode, Scope scope, string helpMessage)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Cannot register key {keyCode} for [{scope}] {helpMessage}\n");
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
            }
            else
            {
                bindings[keyCode] = $"[{scope}] {helpMessage}";
            }
            return keyCode;
        }

        /// <summary>
        /// Prints the current key bindings to the debugging console along with their
        /// help message.
        /// </summary>
        internal static void PrintBindings()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("Key Bindings:\n");
            foreach (var binding in bindings)
            {
                sb.Append($"Key {binding.Key}: {binding.Value}\n");
            }
            Debug.Log(sb.ToString());
        }

        //-----------------------------------------------------
        #region General key bindings
        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        internal static readonly KeyCode Help = Register(KeyCode.H, Scope.Always, "Prints help on the key bindings.");

        #endregion
        
        //-----------------------------------------------------------------
        #region Menu
        // Note: The digits 0-9 are used as short cuts for the menu entries
        //-----------------------------------------------------------------

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        internal static readonly KeyCode ToggleMenu = Register(KeyCode.Space, Scope.Always, "Turns on/off the player-action menu.");

        /// <summary>
        /// Opens the search menu.
        /// </summary>
        internal static readonly KeyCode SearchMenu = Register(KeyCode.F, Scope.Always, "Opens the search menu.");

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        internal static readonly KeyCode Undo = Register(KeyCode.Z, Scope.Always, "Undoes the last action.");

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        internal static readonly KeyCode Redo = Register(KeyCode.Y, Scope.Always, "Re-does the last action.");

        #endregion
        
        //-----------------------------------------------------
        #region Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        internal static readonly KeyCode SavePathPosition = Register(KeyCode.F11, Scope.CameraPaths, "Saves the current position when recording paths.");

        /// <summary>
        /// Starts/stops the automated path replay.
        /// </summary>
        internal static readonly KeyCode TogglePathPlaying = Register(KeyCode.F12, Scope.CameraPaths, "Starts/stops the automated camera movement along a path.");

        #endregion
        
        //-----------------------------------------------------
        #region Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        internal static KeyCode ToggleCharts = Register(KeyCode.M, Scope.MetricCharts, "Turns the metric charts on/off.");

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        internal static readonly KeyCode ToggleMetricHoveringSelection = Register(KeyCode.N, Scope.MetricCharts, "Toggles hovering/selection for markers in metric charts.");

        #endregion
        
        //----------------------------------------------------
        #region Architecture mapping and analysis
        //----------------------------------------------------

        /// <summary>
        /// Architecture mapping: maps implementation entity onto architecture entity.
        /// </summary>
        internal static KeyCode Mapping = Register(KeyCode.V, Scope.Architecture, "Architecture mapping: maps implementation entity onto architecture entity.");

        /// <summary>
        /// Architecture mapping: saves the current architecture mapping.
        /// </summary>
        //internal static KeyCode SaveArchitectureMapping = Register(KeyCode.F, Scope.Architecture, "Architecture mapping: saves the current architecture mapping.");

        /// <summary>
        /// Architecture mapping: copies/removes selected implementation node to/from clipboard.
        /// </summary>
        //internal static KeyCode AddOrRemoveFromClipboard = Register(KeyCode.C, Scope.Architecture, "Architecture mapping: copies/removes selected implementation node to/from clipboard.");

        /// <summary>
        /// Architecture mapping: maps all nodes in clipboard onto selected architecture node.
        /// </summary>
        //internal static KeyCode PasteClipboard = Register(KeyCode.V, Scope.Architecture, "Architecture mapping: maps all nodes in clipboard onto selected architecture node.");

        /// <summary>
        /// Architecture mapping: clears clipboard.
        /// </summary>
        //internal static KeyCode ClearClipboard = Register(KeyCode.X, Scope.Architecture, "Architecture mapping: clears clipboard.");

        #endregion
        
        //-----------------------------------------------------
        #region Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        internal static readonly KeyCode Unselect = Register(KeyCode.U, Scope.Browsing, "Forgets all currently selected objects.");
        /// <summary>
        /// Cancels an action.
        /// </summary>
        internal static readonly KeyCode Cancel = Register(KeyCode.Escape, Scope.Browsing, "Cancels an action.");
        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        internal static readonly KeyCode Reset = Register(KeyCode.R, Scope.Browsing, "Cancels an action.");
        /// <summary>
        /// Zooms into a city.
        /// </summary>
        internal static readonly KeyCode ZoomInto = Register(KeyCode.G, Scope.Browsing, "To zoom into a city.");
        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        internal static readonly KeyCode Snap = Register(KeyCode.LeftAlt, Scope.Browsing, "Snap move/rotate city.");
        /// <summary>
        /// The user drags the city as a whole on the plane.
        /// </summary>
        internal static KeyCode DragHovered = Register(KeyCode.LeftControl, Scope.Browsing, "Drag city element.");
        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        internal static readonly KeyCode ToggleCameraLock = Register(KeyCode.L, Scope.Browsing, "Toggles between the locked and free camera mode.");
        /// <summary>
        /// Toggles between pointing.
        /// </summary>
        internal static readonly KeyCode Pointing = Register(KeyCode.P, Scope.Browsing, "Toggles between Pointing.");

        #endregion
        
        //-----------------------------------------------------
        #region Player (camera) movements.
        //-----------------------------------------------------
        
        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        internal static readonly KeyCode BoostCameraSpeed = Register(KeyCode.LeftShift, Scope.Movement, "Boosts the speed of the player movement. While pressed, movement is faster.");
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        internal static readonly KeyCode MoveForward = Register(KeyCode.W, Scope.Movement, "Move forward.");
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        internal static readonly KeyCode MoveBackward = Register(KeyCode.S, Scope.Movement, "Move backward.");
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        internal static readonly KeyCode MoveRight = Register(KeyCode.D, Scope.Movement, "Move to the right.");
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        internal static readonly KeyCode MoveLeft = Register(KeyCode.A, Scope.Movement, "Move to the left.");
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        internal static readonly KeyCode MoveUp = Register(KeyCode.Q, Scope.Movement, "Move up.");
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        internal static readonly KeyCode MoveDown = Register(KeyCode.E, Scope.Movement, "Move down.");

        #endregion
        
        //--------------------------
        #region Evolution
        //--------------------------

        /// <summary>
        /// The previous revision is to be shown.
        /// </summary>
        internal static readonly KeyCode PreviousRevision = Register(KeyCode.LeftArrow, Scope.Evolution, "The previous revision is to be shown.");
        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        internal static readonly KeyCode NextRevision = Register(KeyCode.RightArrow, Scope.Evolution, "The next revision is to be shown.");
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        internal static readonly KeyCode ToggleAutoPlay = Register(KeyCode.Tab, Scope.Evolution, "Toggles auto play of the animation.");
        /// <summary>
        /// Sets a new marker.
        /// </summary>
        internal static readonly KeyCode SetMarker = Register(KeyCode.Insert, Scope.Evolution, "Sets a new marker.");
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        internal static readonly KeyCode DeleteMarker = Register(KeyCode.Delete, Scope.Evolution, "Deletes a marker.");
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        internal static readonly KeyCode ToggleEvolutionCanvases = Register(KeyCode.T, Scope.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");

        #endregion
        
        //----------------------------------------------------
        #region Animation speed (shared by Debugging and Evolution)
        //----------------------------------------------------
        
        /// <summary>
        /// Double animation speed.
        /// </summary>
        internal static readonly KeyCode IncreaseAnimationSpeed = Register(KeyCode.UpArrow, Scope.Animation, "Doubles animation speed.");
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        internal static readonly KeyCode DecreaseAnimationSpeed = Register(KeyCode.DownArrow, Scope.Animation, "Halves animation speed.");

        #endregion
        
        //--------------------------
        #region Debugging
        //--------------------------

        /// <summary>
        /// Toggles automatic/manual execution mode.
        /// </summary>
        internal static readonly KeyCode ToggleAutomaticManualMode = Register(KeyCode.I, Scope.Debugging, "Toggles automatic/manual execution mode.");
        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        internal static readonly KeyCode ToggleExecutionOrder = Register(KeyCode.O, Scope.Debugging, "Toggles execution order (foward/backward).");
        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        internal static readonly KeyCode ExecuteToBreakpoint = Register(KeyCode.B, Scope.Debugging, "Continues execution until next breakpoint is reached.");
        /// <summary>
        /// Executes previous statement.
        /// </summary>
        internal static readonly KeyCode PreviousStatement = Register(KeyCode.PageUp, Scope.Debugging, "Executes previous statement.");
        /// <summary>
        /// Executes next statement.
        /// </summary>
        internal static readonly KeyCode NextStatement = Register(KeyCode.PageDown, Scope.Debugging, "Executes next statement.");
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        internal static readonly KeyCode FirstStatement = Register(KeyCode.Home, Scope.Debugging, "Execution is back to very first statement.");

        #endregion
        
        //--------------------
        #region Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the source-code viewer.
        /// </summary>
        internal static readonly KeyCode ShowCodeWindowMenu = Register(KeyCode.F1, Scope.CodeViewer, "Toggles the menu of the source-code viewer.");

        /// <summary>
        /// Undoes an edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowUndo = Register(KeyCode.F5, Scope.CodeViewer, "Undoes an edit in the source-code viewer.");

        /// <summary>
        /// Redoes an undone edit in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowRedo = Register(KeyCode.F6, Scope.CodeViewer, "Redoes an undone edit in the source-code viewer.");

        /// <summary>
        /// Saves the content of the source-code viewer.
        /// </summary>
        internal static readonly KeyCode CodeWindowSave = Register(KeyCode.F7, Scope.CodeViewer, "Saves the content of the source-code viewer.");

        /// <summary>
        /// Refreshes syntax highlighting in the source-code viewer.
        /// </summary>
        internal static readonly KeyCode RefreshSyntaxHighlighting = Register(KeyCode.F8, Scope.CodeViewer, "Refreshes syntax highlighting in the source-code viewer.");

        #endregion
        
        //-----------------------------------------------------
        #region Text chat to communicate with other remote players
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the global chat room.
        /// </summary>
        internal static readonly KeyCode ToggleGlobalChat = Register(KeyCode.F2, Scope.Chat, "Toggles the global chat room.");

        /// <summary>
        /// Toggles the chat room for team 1.
        /// </summary>
        internal static readonly KeyCode ToggleTeam1Channel = Register(KeyCode.F3, Scope.Chat, "Toggles the chat room for team 1.");

        /// <summary>
        /// Toggles the chat room for team 2.
        /// </summary>
        internal static readonly KeyCode ToggleTeam2Channel = Register(KeyCode.F4, Scope.Chat, "Toggles the chat room for team 2.");
        
        #endregion
        
        //-----------------------------------------------------
        #region Notifications
        //-----------------------------------------------------

        /// <summary>
        /// Closes all open notifications.
        /// </summary>
        internal static readonly KeyCode CloseNotifications = Register(KeyCode.X, Scope.Always, "Clears all notifications.");

        #endregion
        
        //-----------------------------------------------------
        // Holistic metrics menu
        //-----------------------------------------------------

        /// <summary>
        /// Toggles the menu for holistic code metrics.
        /// </summary>
        internal static readonly KeyCode ToggleHolisticMetricsMenu = Register(KeyCode.C, Scope.Always, "Toggles the menu for holistic code metrics");
    }
}
