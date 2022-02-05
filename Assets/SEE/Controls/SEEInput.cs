using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Provides a logical abstraction of raw Unity inputs by the user.
    /// </summary>
    internal static class SEEInput
    {
        /// <summary>
        /// If true, all logical inputs that require keyboard interactions are enabled.
        /// If false, we will not listen to keyboard inputs for any of the logical
        /// input queries. This flag is provided to disable the keyboard shortcuts
        /// when there are dialogs asking the user for keybord inputs. If the shortcuts
        /// were enabled, they would interfere with the user's input for the dialog.
        /// For instance, pressing W would enter the text "W" and move the player
        /// forward.
        /// </summary>
        public static bool KeyboardShortcutsEnabled { set; get; } = true;

        //----------------------------------------------------
        // Chat
        //----------------------------------------------------
        public static bool GlobalChat()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyCode.Y);
        }
        public static bool RedChat()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyCode.U);
        }
        public static bool BlueChat()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyCode.I);
        }



        //-----------------------------------------------------
        // General key bindings
        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Help()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Help);
        }

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMenu()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleMenu);
        }

        /// <summary>
        /// Opens/closes the search menu.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool ToggleSearch()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.SearchMenu);
        }

        /// <summary>
        /// True if KeyboardShortcutsEnabled and the key for the given <paramref name="digit"/>
        /// was pressed. Used as shortcuts for the menu entries.
        ///
        /// Precondition: 0 &lt;= <paramref name="digit"/> &lt;= 9.
        /// </summary>
        /// <param name="digit">the checked digit</param>
        /// <returns>true if KeyboardShortcutsEnabled and the key for the given <paramref name="digit"/>
        /// was pressed.</returns>
        public static bool DigitKeyPressed(int digit)
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyCode.Alpha1 + digit);
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Undo()
        {
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
#endif
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Undo);
#if UNITY_EDITOR == false
            }
            else
            {
                return false;
            }
#endif
        }

        /// <summary>
        /// Re-does the last action.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Redo()
        {
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
#endif
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Redo);
#if UNITY_EDITOR == false
            }
            else
            {
                return false;
            }
#endif
        }

        /// <summary>
        /// Un-does the last change in the CodeWindow
        /// </summary>
        /// <returns>true if the user requests this action and not <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool CodeWindowUndo()
        {
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
               return !KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Undo);
            } 
            else
            {
                return false;
            }
#endif
#if UNITY_EDITOR == true
            //ctrl keys replaced with f5 in the editor
            if (Input.GetKeyDown(KeyCode.F5))
            {
                return !KeyboardShortcutsEnabled;
            }
            else
            {
                return false;
            }
#endif

        }

        /// <summary>
        /// Saves the changes made in an active code window
        /// </summary>
        /// <returns>true if the user requests this action and not <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool SaveCodeWindow()
        {
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
            return !KeyboardShortcutsEnabled && Input.GetKeyDown(KeyCode.S);

            } 
            else
            {
                return false;
            }
#endif
#if UNITY_EDITOR == true
            //ctrl keys replaced with f5 in the editor
            if (Input.GetKeyDown(KeyCode.F7))
            {
                return !KeyboardShortcutsEnabled;
            }
            else
            {
                return false;
            }
#endif

        }

        /// <summary>
        /// Re-does the last change in the CodeWindow
        /// </summary>
        /// <returns>true if the user requests this action and not <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool CodeWindowRedo()
        {
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return !KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Redo);
            } 
            return false;
#endif
#if UNITY_EDITOR == true
            //ctrl keys replaced with F6 in the editor
            if (Input.GetKeyDown(KeyCode.F6))
            {
                return !KeyboardShortcutsEnabled;
            }
            return false;
#endif
        }

        /// <summary>
        /// Recalculates the Syntaxhighliting
        /// </summary>
        /// <returns>true if the user requests this action and not <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ReCalculateSyntaxHighliting()
        {
#if UNITY_EDITOR == false
           // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                return !KeyboardShortcutsEnabled && Input.GetKeyDown(KeyCode.R);
            } 
            return false;
#endif
#if UNITY_EDITOR == true
            //ctrl keys replaced with F8 in the editor
            if (Input.GetKeyDown(KeyCode.F8))
            {
                return !KeyboardShortcutsEnabled;
            }
            return false;
#endif  
        }

        /// <summary>
        /// Whether the left or right shift key was pressed down (and not again released).
        /// </summary>
        private static bool isModPressed = false;

        /// <summary>
        /// Returns true if the user wants to toggle the run-time configuration
        /// menu allowing him/her to define the settings for code cities.
        /// </summary>
        /// <returns>true if the user wants to toggle the run-time configuration menu</returns>
        internal static bool ToggleConfigMenu()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                isModPressed = true;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
            {
                isModPressed = false;
            }
            return isModPressed && Input.GetKeyUp(KeyCode.Escape);
        }

        /// <summary>
        /// The user wants to map an implementation node onto an architecture node for the architecture analysis.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool Mapping()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Mapping);
        }

        //-----------------------------------------------------
        // Camera path recording and playing
        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool SavePathPosition()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.SavePathPosition);
        }

        /// <summary>
        /// Toggles automatic path playing.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool TogglePathPlaying()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.TogglePathPlaying);
        }

        //-----------------------------------------------------
        // Metric charts
        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMetricCharts()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleCharts);
        }

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMetricHoveringSelection()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.ToggleMetricHoveringSelection);
        }

        //-----------------------------------------------------
        // Manipulating nodes
        //-----------------------------------------------------

        /// <summary>
        /// The user wants to scale a selected node.
        /// </summary>
        /// <returns>true if the user requests this action</returns>
        internal static bool Scale()
        {
            return Input.GetMouseButton(0);
        }

        //-----------------------------------------------------
        // Navigation in a code city
        //-----------------------------------------------------

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Unselect()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Unselect);
        }

        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleCameraLock()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleCameraLock);
        }

        /// <summary>
        /// Cancels an action.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Cancel()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Cancel);
        }

        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Reset()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.Reset);
        }

        /// <summary>
        /// Zooms into a city.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ZoomInto()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ZoomInto);
        }

        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Snap()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.Snap);
        }

        /// <summary>
        /// The user wants to drag the hovered element of the city on its plane.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool DragHovered()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.DragHovered);
        }

        /// <summary>
        /// The user wants to start dragging the city in its entirety or parts of it.
        /// </summary>
        /// <returns>true if the user requests this action</returns>
        internal static bool StartDrag()
        {
            return Input.GetMouseButtonDown(2);
        }

        /// <summary>
        /// The user wants to drag the city in its entirety or parts of it.
        /// </summary>
        /// <returns>true if the user requests this action</returns>
        internal static bool Drag()
        {
            return Input.GetMouseButton(2);
        }

        //-----------------------------------------------------
        // Player (camera) movements.
        //-----------------------------------------------------

        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool BoostCameraSpeed()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.BoostCameraSpeed);
        }
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveForward()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveForward);
        }
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveBackward()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveBackward);
        }
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveRight()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveRight);
        }
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveLeft()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveLeft);
        }
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveUp()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveUp);
        }
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveDown()
        {
            return KeyboardShortcutsEnabled && Input.GetKey(KeyBindings.MoveDown);
        }

        /// <summary>
        /// Index of the left mouse button.
        /// </summary>
        private const int LeftMouseButton = 0;

        /// <summary>
        /// Index of the right mouse button.
        /// </summary>
        private const int RightMouseButton = 1;

        /// <summary>
        /// Rotates the camera.
        /// </summary>
        /// <returns>true if the user requests this action</returns>
        public static bool RotateCamera()
        {
            return Input.GetMouseButton(RightMouseButton)
                || (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(LeftMouseButton));
        }

        //--------------------------
        // Evolution
        //--------------------------

        /// <summary>
        /// The previous revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool PreviousRevision()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.PreviousRevision);
        }
        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool NextRevision()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.NextRevision);
        }
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleAutoPlay()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleAutoPlay);
        }
        /// <summary>
        /// Sets a new marker.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool SetMarker()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.SetMarker);
        }
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool DeleteMarker()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.DeleteMarker);
        }
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleEvolutionCanvases()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleEvolutionCanvases);
        }
        //----------------------------------------------------
        // Animation speed (shared by Debugging and Evolution)
        //----------------------------------------------------
        /// <summary>
        /// Double animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool IncreaseAnimationSpeed()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.IncreaseAnimationSpeed);
        }
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool DecreaseAnimationSpeed()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.DecreaseAnimationSpeed);
        }

        //--------------------------
        // Debugging
        //--------------------------

        /// <summary>
        /// Toggles automatic/manual execution mode.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleAutomaticManualMode()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleAutomaticManualMode);
        }
        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleExecutionOrder()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ToggleExecutionOrder);
        }
        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ExecuteToBreakpoint()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ExecuteToBreakpoint);
        }
        /// <summary>
        /// Executes previous statement.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool PreviousStatement()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.PreviousStatement);
        }
        /// <summary>
        /// Executes next statement.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool NextStatement()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.NextStatement);
        }
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool FirstStatement()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.FirstStatement);
        }

        //--------------------
        // Source-code viewer
        //--------------------

        /// <summary>
        /// Toggles the menu of the source-code viewer.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ShowCodeWindowMenu()
        {
            return KeyboardShortcutsEnabled && Input.GetKeyDown(KeyBindings.ShowCodeWindowMenu);
        }

        //-------------------
        // Selection
        //-------------------

        /// <summary>
        /// If true, selection is enabled. Selection can be disabled by action directly
        /// determining whether anything is selected; for instance, the <see cref="DeleteAction"/>
        /// listens to a selection interaction to determine the graph element to be deleted.
        /// This selection interaction should not interfere with the general <see cref="SelectAction"/>.
        /// </summary>
        public static bool SelectionEnabled = true;

        /// <summary>
        /// True if the user selects a game object (in a desktop environment, the user
        /// presses the left mouse but while the mouse cursor is not over a GUI element).
        /// Selection is enabled only if <see cref="SelectionEnabled"/>.
        /// </summary>
        /// <returns>true if the user selects a game object and <see cref="SelectionEnabled"/></returns>
        public static bool Select()
        {
            return SelectionEnabled && Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI();
        }
    }
}
