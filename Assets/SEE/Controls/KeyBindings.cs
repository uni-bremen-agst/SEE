using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Defines the key codes for all interaction based on the keyboard in SEE.
    /// </summary>
    internal static class KeyBindings
    {
        private static Dictionary<KeyCode, string> bindings = new Dictionary<KeyCode, string>();

        private enum Scope
        {
            Always,
            CameraPaths,
            Debugging,
            Evolution,
            MetricCharts,
            Navigation,
        }

        private static KeyCode Register(KeyCode keyCode, Scope scope, string help)
        {
            if (bindings.ContainsKey(keyCode))
            {
                Debug.LogError($"Key {keyCode} already bound to {bindings[keyCode]}\n");
            }
            else
            {
                bindings[keyCode] = $"[{scope}] {help}";
            }
            return keyCode;
        }

        internal static void PrintBindings()
        {
            foreach (var binding in bindings)
            {
                Debug.Log($"Key {binding.Key}: {binding.Value}\n");
            }
        }

        //-----------------------------------------------------------------
        // Menu
        // Note: The digits 0-9 are used as short cuts for the menu entries
        //-----------------------------------------------------------------

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        internal static KeyCode ToggleMenu = Register(KeyCode.Space, Scope.Always, "Turns on/off the player-action menu.");

        //-----------------------------------------------------
        // Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        internal static KeyCode SavePathPosition = Register(KeyCode.P, Scope.CameraPaths, "Saves the current position when recording paths.");

        //-----------------------------------------------------
        // Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turn on/off the metric charts.
        /// </summary>
        internal static KeyCode ToggleCharts = KeyCode.M;

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        internal static KeyCode ToggleMetricHoveringSelection = KeyCode.N;

        //----------------------------------------------------
        // Architecture mapping and analysis
        //----------------------------------------------------

        /// <summary>
        /// Architecture mapping: saves the current architecture mapping.
        /// </summary>
        internal static KeyCode SaveArchitectureMapping = KeyCode.S;

        /// <summary>
        /// Architecture mapping: copies/removes selected implementation node to/from clipboard.
        /// </summary>
        internal static KeyCode AddOrRemoveFromClipboard = KeyCode.C;

        /// <summary>
        /// Architecture mapping: maps all nodes in clipboard onto selected architecture node.
        /// </summary>
        internal static KeyCode PasteClipboard = KeyCode.V;

        /// <summary>
        /// Architecture mapping: clears clipboard.
        /// </summary>
        internal static KeyCode ClearClipboard = KeyCode.X;

        //-----------------------------------------------------
        // Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        internal static KeyCode Unselect = KeyCode.U;
        /// <summary>
        /// To cancel an action.
        /// </summary>
        internal static KeyCode Cancel = KeyCode.Escape;
        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        internal static KeyCode Reset = KeyCode.R;
        /// <summary>
        /// To zoom into a city.
        /// </summary>
        internal static KeyCode ZoomInto = KeyCode.Z;
        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        internal static KeyCode Snap = KeyCode.LeftControl;
        /// <summary>
        /// To toggle between the locked and free camera mode.
        /// </summary>
        internal static KeyCode ToggleCameraLock = KeyCode.C;
        /// <summary>
        /// Boosts the speed of the camera (player movement).
        /// </summary>
        internal static KeyCode BoostCameraSpeed = KeyCode.LeftShift;
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        internal static KeyCode MoveForward = KeyCode.W;
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        internal static KeyCode MoveBackward = KeyCode.S;
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        internal static KeyCode MoveRight = KeyCode.D;
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        internal static KeyCode MoveLeft = KeyCode.A;
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        internal static KeyCode MoveUp = KeyCode.Q;
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        internal static KeyCode MoveDown = KeyCode.E;

        //--------------------------
        // Evolution 
        //--------------------------

        /// <summary>
        /// The previous revision is to be shown.
        /// </summary>
        internal static KeyCode PreviousRevision = KeyCode.LeftArrow;
        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        internal static KeyCode NextRevision = KeyCode.RightArrow;
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        internal static KeyCode ToggleAutoPlay = KeyCode.Tab;
        /// <summary>
        /// Sets a new marker.
        /// </summary>
        internal static KeyCode SetMarker = KeyCode.M; // FIXME: Avoid conflict.
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        internal static KeyCode DeleteMarker = KeyCode.Delete; // FIXME: Choose better key.
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        internal static KeyCode ToggleEvolutionCanvases = KeyCode.T;

        //----------------------------------------------------
        // Animation speed (shared by Debugging and Evolution)
        //----------------------------------------------------
        /// <summary>
        /// Double animation speed.
        /// </summary>
        internal static KeyCode IncreaseAnimationSpeed = KeyCode.UpArrow;
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        internal static KeyCode DecreaseAnimationSpeed = KeyCode.DownArrow;

        //--------------------------
        // Debugging 
        //--------------------------

        /// <summary>
        /// Toggles automatic/manual execution mode.
        /// </summary>
        internal static KeyCode ToggleAutomaticManualMode = KeyCode.I;
        /// <summary>
        /// Toggles execution order (foward/backward).
        /// </summary>
        internal static KeyCode ToggleExecutionOrder = KeyCode.O;
        /// <summary>
        /// Continue execution until next breakpoint is reached.
        /// </summary>
        internal static KeyCode ExecuteToBreakpoint = KeyCode.B;
        /// <summary>
        /// Execute previous statement.
        /// </summary>
        internal static KeyCode PreviousStatement = KeyCode.Less;
        /// <summary>
        /// Execute next statement.
        /// </summary>
        internal static KeyCode NextStatement = KeyCode.Greater;
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        internal static KeyCode FirstStatement = KeyCode.Hash;
    }
}
