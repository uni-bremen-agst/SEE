using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Provides a logical abstraction of raw Unity inputs by the user.
    /// </summary>
    internal static class SEEInput
    {
        internal static bool KeyboardShortcutsEnabled = true;

        //-----------------------------------------------------
        // General key bindings
        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Help()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Help);
        }

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleMenu()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleMenu);
        }

        /// <summary>
        /// True if KeyboardShortcutsEnabled and the key for the given <paramref name="digit"/>
        /// was pressed. Used as shortcuts for the menu entries.
        /// 
        /// Precondition: 0 <= <paramref name="digit"/> <= 9.
        /// </summary>
        /// <param name="digit">the checked digit</param>
        /// <returns>true if KeyboardShortcutsEnabled and the key for the given <paramref name="digit"/>
        /// was pressed./returns>
        internal static bool DigitKeyPressed(int digit)
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyCode.Alpha1 + digit);
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Undo()
        {            
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
#endif
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Undo);
        }

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Redo()
        {
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
#endif
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Redo);
        }

        //-----------------------------------------------------
        // Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool SavePathPosition()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.SavePathPosition);
        }

        //-----------------------------------------------------
        // Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleMetricCharts()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleCharts);
        }

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleMetricHoveringSelection()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.ToggleMetricHoveringSelection);
        }

        //-----------------------------------------------------
        // Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Unselect()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Unselect);
        }

        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleCameraLock()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleCameraLock);
        }

        /// <summary>
        /// Cancels an action.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Cancel()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Cancel);
        }

        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Reset()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Reset);
        }

        /// <summary>
        /// To zoom into a city.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ZoomInto()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ZoomInto);
        }

        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool Snap()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.Snap);
        }

        internal static bool Drag()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.Drag);
        }

        //-----------------------------------------------------
        // Player (camera) movements.
        //-----------------------------------------------------

        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool BoostCameraSpeed()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.BoostCameraSpeed);
        }
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool MoveForward()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveForward);
        }
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool MoveBackward()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveBackward);
        }
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool MoveRight()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveRight);
        }
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool MoveLeft()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveLeft);
        }
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool MoveUp()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveUp);
        }
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool MoveDown()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveDown);
        }

        //--------------------------
        // Evolution 
        //--------------------------

        /// <summary>
        /// The previous revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool PreviousRevision()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.PreviousRevision);
        }
        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool NextRevision()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.NextRevision);
        }
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleAutoPlay()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleAutoPlay);
        }
        /// <summary>
        /// Sets a new marker.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool SetMarker()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.SetMarker);
        }
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool DeleteMarker()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.DeleteMarker);
        }
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleEvolutionCanvases()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleEvolutionCanvases);
        }
        //----------------------------------------------------
        // Animation speed (shared by Debugging and Evolution)
        //----------------------------------------------------
        /// <summary>
        /// Double animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool IncreaseAnimationSpeed()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.IncreaseAnimationSpeed);
        }
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool DecreaseAnimationSpeed()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.DecreaseAnimationSpeed);
        }

        //--------------------------
        // Debugging 
        //--------------------------

        /// <summary>
        /// Toggles automatic/manual execution mode.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleAutomaticManualMode()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleAutomaticManualMode);
        }
        /// <summary>
        /// Toggles execution order (foward/backward).
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ToggleExecutionOrder()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleExecutionOrder);
        }
        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ExecuteToBreakpoint()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ExecuteToBreakpoint);
        }
        /// <summary>
        /// Executes previous statement.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool PreviousStatement()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.PreviousStatement);
        }
        /// <summary>
        /// Executes next statement.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool NextStatement()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.NextStatement);
        }
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool FirstStatement()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.FirstStatement);
        }

        //--------------------
        // Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the source-code viewer.
        /// </summary>
        /// <returns>true if the user requests this action and KeyboardShortcutsEnabled</returns>
        internal static bool ShowCodeWindowMenu()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ShowCodeWindowMenu);
        }
    }
}
