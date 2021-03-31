﻿using System.Collections.Generic;
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
        private static Dictionary<KeyCode, string> bindings = new Dictionary<KeyCode, string>();

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
            foreach (var binding in bindings)
            {
                Debug.Log($"Key {binding.Key}: {binding.Value}\n");
            }
        }

        //-----------------------------------------------------
        // General key bindings
        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        internal static KeyCode Help = Register(KeyCode.H, Scope.Always, "Prints help on the key bindings.");

        //-----------------------------------------------------------------
        // Menu
        // Note: The digits 0-9 are used as short cuts for the menu entries
        //-----------------------------------------------------------------

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        internal static KeyCode ToggleMenu = Register(KeyCode.Space, Scope.Always, "Turns on/off the player-action menu.");

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        internal static KeyCode Undo = Register(KeyCode.Z, Scope.CodeViewer, "Undoes the last action.");

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        internal static KeyCode Redo = Register(KeyCode.Y, Scope.CodeViewer, "Re-does the last action.");

        //-----------------------------------------------------
        // Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        internal static KeyCode SavePathPosition = Register(KeyCode.P, Scope.CameraPaths, "Saves the current position when recording paths.");

        /// <summary>
        /// Starts/stops the automated path replay.
        /// </summary>
        internal static KeyCode TogglePathPlaying = Register(KeyCode.F2, Scope.CodeViewer, "Starts/stops the automated camera movement along a path.");

        //-----------------------------------------------------
        // Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        internal static KeyCode ToggleCharts = Register(KeyCode.M, Scope.MetricCharts, "Turns the metric charts on/off.");

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        internal static KeyCode ToggleMetricHoveringSelection = Register(KeyCode.N, Scope.MetricCharts, "Toggles hovering/selection for markers in metric charts.");

        //----------------------------------------------------
        // Architecture mapping and analysis
        //----------------------------------------------------

        /// <summary>
        /// Architecture mapping: saves the current architecture mapping.
        /// </summary>
        internal static KeyCode SaveArchitectureMapping = Register(KeyCode.F, Scope.Architecture, "Architecture mapping: saves the current architecture mapping.");

        /// <summary>
        /// Architecture mapping: copies/removes selected implementation node to/from clipboard.
        /// </summary>
        internal static KeyCode AddOrRemoveFromClipboard = Register(KeyCode.C, Scope.Architecture, "Architecture mapping: copies/removes selected implementation node to/from clipboard.");

        /// <summary>
        /// Architecture mapping: maps all nodes in clipboard onto selected architecture node.
        /// </summary>
        internal static KeyCode PasteClipboard = Register(KeyCode.V, Scope.Architecture, "Architecture mapping: maps all nodes in clipboard onto selected architecture node.");

        /// <summary>
        /// Architecture mapping: clears clipboard.
        /// </summary>
        internal static KeyCode ClearClipboard = Register(KeyCode.X, Scope.Architecture, "Architecture mapping: clears clipboard.");

        //-----------------------------------------------------
        // Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        internal static KeyCode Unselect = Register(KeyCode.U, Scope.Browsing, "Forgets all currently selected objects.");
        /// <summary>
        /// Cancels an action.
        /// </summary>
        internal static KeyCode Cancel = Register(KeyCode.Escape, Scope.Browsing, "Cancels an action.");
        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        internal static KeyCode Reset = Register(KeyCode.R, Scope.Browsing, "Cancels an action.");
        /// <summary>
        /// To zoom into a city.
        /// </summary>
        internal static KeyCode ZoomInto = Register(KeyCode.G, Scope.Browsing, "To zoom into a city.");
        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        internal static KeyCode Snap = Register(KeyCode.LeftControl, Scope.Browsing, "Snap move/rotate city.");
        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        internal static KeyCode ToggleCameraLock = Register(KeyCode.L, Scope.Browsing, "Toggles between the locked and free camera mode.");
        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>        
        internal static KeyCode BoostCameraSpeed = Register(KeyCode.LeftShift, Scope.Browsing, "Boosts the speed of the player movement. While pressed, movement is faster.");

        //-----------------------------------------------------
        // Player (camera) movements.
        //-----------------------------------------------------
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        internal static KeyCode MoveForward = Register(KeyCode.W, Scope.Movement, "Move forward.");
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        internal static KeyCode MoveBackward = Register(KeyCode.S, Scope.Movement, "Move backward.");
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        internal static KeyCode MoveRight = Register(KeyCode.D, Scope.Movement, "Move to the right.");
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        internal static KeyCode MoveLeft = Register(KeyCode.A, Scope.Movement, "Move to the left.");
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        internal static KeyCode MoveUp = Register(KeyCode.Q, Scope.Movement, "Move up.");
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        internal static KeyCode MoveDown = Register(KeyCode.E, Scope.Movement, "Move down.");

        //--------------------------
        // Evolution 
        //--------------------------

        /// <summary>
        /// The previous revision is to be shown.
        /// </summary>
        internal static KeyCode PreviousRevision = Register(KeyCode.LeftArrow, Scope.Evolution, "The previous revision is to be shown.");
        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        internal static KeyCode NextRevision = Register(KeyCode.RightArrow, Scope.Evolution, "The next revision is to be shown.");
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        internal static KeyCode ToggleAutoPlay = Register(KeyCode.Tab, Scope.Evolution, "Toggles auto play of the animation.");
        /// <summary>
        /// Sets a new marker.
        /// </summary>
        internal static KeyCode SetMarker = Register(KeyCode.Insert, Scope.Evolution, "Sets a new marker.");
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        internal static KeyCode DeleteMarker = Register(KeyCode.Delete, Scope.Evolution, "Deletes a marker.");
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        internal static KeyCode ToggleEvolutionCanvases = Register(KeyCode.T, Scope.Evolution, "Toggles between between the two canvases for the animation and selection of a revision.");

        //----------------------------------------------------
        // Animation speed (shared by Debugging and Evolution)
        //----------------------------------------------------
        /// <summary>
        /// Double animation speed.
        /// </summary>
        internal static KeyCode IncreaseAnimationSpeed = Register(KeyCode.UpArrow, Scope.Animation, "Doubles animation speed.");
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        internal static KeyCode DecreaseAnimationSpeed = Register(KeyCode.DownArrow, Scope.Animation, "Halves animation speed.");

        //--------------------------
        // Debugging 
        //--------------------------

        /// <summary>
        /// Toggles automatic/manual execution mode.
        /// </summary>
        internal static KeyCode ToggleAutomaticManualMode = Register(KeyCode.I, Scope.Debugging, "Toggles automatic/manual execution mode.");
        /// <summary>
        /// Toggles execution order (foward/backward).
        /// </summary>
        internal static KeyCode ToggleExecutionOrder = Register(KeyCode.O, Scope.Debugging, "Toggles execution order (foward/backward).");
        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        internal static KeyCode ExecuteToBreakpoint = Register(KeyCode.B, Scope.Debugging, "Continues execution until next breakpoint is reached.");
        /// <summary>
        /// Executes previous statement.
        /// </summary>
        internal static KeyCode PreviousStatement = Register(KeyCode.PageUp, Scope.Debugging, "Executes previous statement.");
        /// <summary>
        /// Executes next statement.
        /// </summary>
        internal static KeyCode NextStatement = Register(KeyCode.PageDown, Scope.Debugging, "Executes next statement.");
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        internal static KeyCode FirstStatement = Register(KeyCode.Home, Scope.Debugging, "Execution is back to very first statement.");

        //--------------------
        // Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the source-code viewer.
        /// </summary>
        internal static KeyCode ShowCodeWindowMenu = Register(KeyCode.F1, Scope.CodeViewer, "Toggles the menu of the source-code viewer.");
    }
}
