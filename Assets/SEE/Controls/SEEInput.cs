using SEE.Controls.Actions;
using SEE.Controls.KeyActions;
using SEE.GO;
using SEE.Tools.OpenTelemetry;
using SEE.Utils;
using SEE.XR;
using UnityEditor;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Provides a logical abstraction of raw Unity inputs by the user.
    /// </summary>
    public static class SEEInput
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

#if UNITY_EDITOR
        /// <summary>
        /// Sometimes if the game is not stopped correctly, the keyboard shortcuts
        /// might still be disabled. This method ensures that the keyboard shortcuts
        /// are always enabled when the game is started. This is needed only in the
        /// editor, because the executable will start always with a fresh state.
        /// </summary>
        [InitializeOnEnterPlayMode]
        private static void ResetKeyboardShortcutsEnabled()
        {
            KeyboardShortcutsEnabled = true;
        }
#endif

        //-----------------------------------------------------

        #region General key bindings

        //-----------------------------------------------------

        /// <summary>
        /// Prints help on the key bindings.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Help()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Help);

            if (result)
            {
                // Track the Help action when the key is pressed
                TracingHelperService.Instance?.TrackKeyPress("Help");
            }

            return result;
        }

        /// <summary>
        /// Toggles voice control (i.e., for voice commands) on/off.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleVoiceControl()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleVoiceControl);

            if (result)
            {
                // Track the ToggleVoiceControl action when the key is pressed
                TracingHelperService.Instance?.TrackKeyPress("ToggleVoiceControl");
            }

            return result;
        }


        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMenu()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleMenu);

            if (result)
            {
                // Track the ToggleMenu action when the key is pressed
                TracingHelperService.Instance?.TrackKeyPress("ToggleMenu");
            }

            return result;
        }


        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleSettings()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleSettings);

            if (result)
            {
                // Track the ToggleSettings action when the key is pressed
                TracingHelperService.Instance?.TrackKeyPress("ToggleSettings");
            }

            return result;
        }


        /// <summary>
        /// Turns on/off the browser.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleBrowser()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleBrowser);

            if (result)
            {
                // Track the ToggleBrowser action when the key is pressed
                TracingHelperService.Instance?.TrackKeyPress("ToggleBrowser");
            }

            return result;
        }


        /// <summary>
        /// Turns on/off the mirror.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMirror()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleMirror);

            if (result)
            {
                // Track the ToggleMirror action when the key is pressed
                TracingHelperService.Instance?.TrackKeyPress("ToggleMirror");
            }

            return result;
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
            bool result = KeyboardShortcutsEnabled && Input.GetKeyDown(KeyCode.Alpha1 + digit);

            if (result)
            {
                // Track the digit key press action
                TracingHelperService.Instance?.TrackKeyPress($"DigitKeyPressed_{digit}");
            }

            return result;
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Undo()
        {
            if (User.UserSettings.IsVR && XRSEEActions.UndoToggle)
            {
                bool undo = XRSEEActions.UndoToggle;
                XRSEEActions.UndoToggle = false;

                // Track the Undo action
                TracingHelperService.Instance?.TrackKeyPress("Undo");

                return undo;
            }

#if UNITY_EDITOR == false
    // Ctrl keys are not available when running the game in the editor
    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
    {
#endif
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Undo);

            if (result)
            {
                // Track the Undo action
                TracingHelperService.Instance?.TrackKeyPress("Undo");
            }

            return result;
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
            if (User.UserSettings.IsVR && XRSEEActions.RedoToggle)
            {
                bool redo = XRSEEActions.RedoToggle;
                XRSEEActions.RedoToggle = false;

                // Track the Redo action
                TracingHelperService.Instance?.TrackKeyPress("Redo");

                return redo;
            }

#if UNITY_EDITOR == false
    // Ctrl keys are not available when running the game in the editor
    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
    {
#endif
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Redo);

            if (result)
            {
                // Track the Redo action
                TracingHelperService.Instance?.TrackKeyPress("Redo");
            }

            return result;
#if UNITY_EDITOR == false
    }
    else
    {
        return false;
    }
#endif
        }

        /// <summary>
        /// Returns true if the user wants to toggle the run-time configuration
        /// menu allowing him/her to define the settings for code cities.
        /// </summary>
        /// <returns>true if the user wants to toggle the run-time configuration menu</returns>
        internal static bool ToggleConfigMenu()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ConfigMenu);

            if (result)
            {
                // Track the action of toggling the configuration menu
                TracingHelperService.Instance?.TrackKeyPress("ConfigMenu");
            }

            return result;
        }

        /// <summary>
        /// The user wants to toggle the visibility of all edges in a hovered code city.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool ToggleEdges()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleEdges);

            if (result)
            {
                // Track the action of toggling edges visibility
                TracingHelperService.Instance?.TrackKeyPress("ToggleEdges");
            }

            return result;
        }

        /// <summary>
        /// The user wants to toggle the visibility of the tree view window.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool ToggleTreeView()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.TreeView);

            if (result)
            {
                // Track the action of toggling tree view visibility
                TracingHelperService.Instance?.TrackKeyPress("ToggleTreeView");
            }

            return result;
        }

        #endregion

        //-----------------------------------------------------

        #region Camera path recording and playing

        //-----------------------------------------------------

        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool SavePathPosition()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.SavePathPosition);

            if (result)
            {
                // Track the action of saving the path position
                TracingHelperService.Instance?.TrackKeyPress("SavePathPosition");
            }

            return result;
        }

        /// <summary>
        /// Toggles automatic path playing.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool TogglePathPlaying()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.TogglePathPlaying);

            if (result)
            {
                // Track the action of toggling path playing
                TracingHelperService.Instance?.TrackKeyPress("TogglePathPlaying");
            }

            return result;
        }

        #endregion

        //-----------------------------------------------------

        #region Metric charts

        //-----------------------------------------------------

        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMetricCharts()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleCharts);

            if (result)
            {
                // Track the action of toggling metric charts
                TracingHelperService.Instance?.TrackKeyPress("ToggleMetricCharts");
            }

            return result;
        }

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMetricHoveringSelection()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleMetricHoveringSelection);

            if (result)
            {
                // Track the action of toggling metric hovering/selection
                TracingHelperService.Instance?.TrackKeyPress("ToggleMetricHoveringSelection");
            }

            return result;
        }

        #endregion

        //-----------------------------------------------------

        #region Context menu

        //-----------------------------------------------------

        /// <summary>
        /// True if the user starts the mouse interaction to open the context menu.
        /// </summary>
        /// <returns>true if the user starts the mouse interaction to open the context menu</returns>
        internal static bool OpenContextMenuStart()
        {
            return Input.GetMouseButtonDown(rightMouseButton) && !Raycasting.IsMouseOverGUI();
        }

        /// <summary>
        /// True if the user ends the mouse interaction to open the context menu.
        /// </summary>
        /// <returns>true if the user ends the mouse interaction to open the context menu</returns>
        internal static bool OpenContextMenuEnd()
        {
            return Input.GetMouseButtonUp(rightMouseButton) && !Raycasting.IsMouseOverGUI();
        }

        #endregion

        //-----------------------------------------------------

        #region Navigation in a code city

        //-----------------------------------------------------

        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Unselect()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Unselect);

            if (result)
            {
                // Track the Unselect action
                TracingHelperService.Instance?.TrackKeyPress("Unselect");
            }

            return result;
        }

        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleCameraLock()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleCameraLock);

            if (result)
            {
                // Track the ToggleCameraLock action
                TracingHelperService.Instance?.TrackKeyPress("ToggleCameraLock");
            }

            return result;
        }

        /// <summary>
        /// Cancels an action.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Cancel()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Cancel);

            if (result)
            {
                // Track the Cancel action
                TracingHelperService.Instance?.TrackKeyPress("Cancel");
            }

            return result;
        }

        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Reset()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Reset);

            if (result)
            {
                // Track the Reset action
                TracingHelperService.Instance?.TrackKeyPress("Reset");
            }

            return result;
        }

        /// <summary>
        /// Zooms into a city.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ZoomInto()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ZoomInto);

            if (result)
            {
                // Track the ZoomInto action
                TracingHelperService.Instance?.TrackKeyPress("ZoomInto");
            }

            return result;
        }

        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Snap()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.Snap);

            if (result)
            {
                // Track the Snap action
                TracingHelperService.Instance?.TrackKeyPress("Snap");
            }

            return result;
        }

        /// <summary>
        /// The user wants to drag the hovered element of the city on its plane.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool DragHovered()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.DragHovered);

            if (result)
            {
                // Track the DragHovered action
                TracingHelperService.Instance?.TrackKeyPress("DragHovered");
            }

            return result;
        }


        /// <summary>
        /// The user wants to drag the city in its entirety or parts of it.
        /// </summary>
        /// <returns>true if the user requests this action</returns>
        internal static bool Drag()
        {
            return Input.GetMouseButton(middleMouseButton);
        }

        #endregion

        //-----------------------------------------------------

        #region Player (camera) movements.

        //-----------------------------------------------------

        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool BoostCameraSpeed()
        {
            bool isPressed = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.BoostCameraSpeed);

            TracingHelperService.Instance?.UpdateBoostCameraTracking(isPressed);

            return isPressed;
        }

        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveForward()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveForward);
        }

        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveBackward()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveBackward);
        }

        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveRight()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveRight);
        }

        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveLeft()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveLeft);
        }

        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveUp()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveUp);
        }

        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool MoveDown()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveDown);
        }

        /// <summary>
        /// Index of the left mouse button.
        /// </summary>
        private const int leftMouseButton = 0;

        /// <summary>
        /// Index of the right mouse button.
        /// </summary>
        private const int rightMouseButton = 1;

        /// <summary>
        /// Index of the middle mouse button.
        /// </summary>
        private const int middleMouseButton = 2;

        /// <summary>
        /// Rotates the camera.
        /// </summary>
        /// <returns>true if the user requests this action</returns>
        public static bool RotateCamera()
        {
            return Input.GetMouseButton(rightMouseButton)
                   || (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(leftMouseButton));
        }

        /// <summary>
        /// True if the user wishes to point.
        /// </summary>
        /// <returns>true if the user wishes to point</returns>
        public static bool TogglePointing()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Pointing);

            if (result)
            {
                // Track the Pointing action
                TracingHelperService.Instance?.TrackKeyPress("Pointing");
            }

            return result;
        }

        /// <summary>
        /// True if the hand animations with MediaPipe should be activated.
        /// </summary>
        /// <returns>True, if the user wishes to use hand animations with MediaPipe</returns>
        public static bool ToggleHandAnimations()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.HandAnimations);
        }

        #endregion

        //--------------------------

        #region Evolution

        //--------------------------

        /// <summary>
        /// Sets a new marker.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool SetMarker()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.SetMarker);

            if (result)
            {
                // Track the SetMarker action
                TracingHelperService.Instance?.TrackKeyPress("SetMarker");
            }

            return result;
        }

        /// <summary>
        /// Deletes a marker.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool DeleteMarker()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.DeleteMarker);

            if (result)
            {
                // Track the DeleteMarker action
                TracingHelperService.Instance?.TrackKeyPress("DeleteMarker");
            }

            return result;
        }

        /// <summary>
        /// Toggles between the two canvases for the animation and selection of a revision.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleEvolutionCanvases()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleEvolutionCanvases);

            if (result)
            {
                // Track the ToggleEvolutionCanvases action
                TracingHelperService.Instance?.TrackKeyPress("ToggleEvolutionCanvases");
            }

            return result;
        }

        #endregion

        //----------------------------------------------------

        #region Animation (shared by Debugging and Evolution)

        //----------------------------------------------------

        /// <summary>
        /// The previous revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Previous()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Previous);

            if (result)
            {
                // Track the Previous action
                TracingHelperService.Instance?.TrackKeyPress("Previous");
            }

            return result;
        }

        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Next()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Next);

            if (result)
            {
                // Track the Next action
                TracingHelperService.Instance?.TrackKeyPress("Next");
            }

            return result;
        }

        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleAutoPlay()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleAutoPlay);

            if (result)
            {
                // Track the ToggleAutoPlay action
                TracingHelperService.Instance?.TrackKeyPress("ToggleAutoPlay");
            }

            return result;
        }

        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleExecutionOrder()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleExecutionOrder);

            if (result)
            {
                // Track the ToggleExecutionOrder action
                TracingHelperService.Instance?.TrackKeyPress("ToggleExecutionOrder");
            }

            return result;
        }

        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool FirstStatement()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.FirstStatement);

            if (result)
            {
                // Track the FirstStatement action
                TracingHelperService.Instance?.TrackKeyPress("FirstStatement");
            }

            return result;
        }

        /// <summary>
        /// Double animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool IncreaseAnimationSpeed()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.IncreaseAnimationSpeed);

            if (result)
            {
                // Track the IncreaseAnimationSpeed action
                TracingHelperService.Instance?.TrackKeyPress("IncreaseAnimationSpeed");
            }

            return result;
        }

        /// <summary>
        /// Halve animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool DecreaseAnimationSpeed()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.DecreaseAnimationSpeed);

            if (result)
            {
                // Track the DecreaseAnimationSpeed action
                TracingHelperService.Instance?.TrackKeyPress("DecreaseAnimationSpeed");
            }

            return result;
        }

        #endregion

        //--------------------------

        #region Debugging

        //--------------------------

        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ExecuteToBreakpoint()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ExecuteToBreakpoint);

            if (result)
            {
                // Track the ExecuteToBreakpoint action
                TracingHelperService.Instance?.TrackKeyPress("ExecuteToBreakpoint");
            }

            return result;
        }

        #endregion

        //--------------------

        #region Source-code viewer

        //--------------------

        /// <summary>
        /// Toggles the menu of the open windows.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ShowWindowMenu()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ShowWindowMenu);

            if (result)
            {
                // Track the ShowWindowMenu action
                TracingHelperService.Instance?.TrackKeyPress("ShowWindowMenu");
            }

            return result;
        }

        #endregion

        //-------------------

        #region Selection

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
            return SelectionEnabled && Input.GetMouseButtonDown(leftMouseButton) && !Raycasting.IsMouseOverGUI();
        }

        #endregion

        //----------------------------------------------------

        #region Mouse Interaction

        /// <summary>
        /// Registers the users left mouse button input.
        /// </summary>
        /// <returns>true if the user uses the left mouse button.</returns>
        public static bool LeftMouseInteraction()
        {
            return Input.GetMouseButton(leftMouseButton);
        }

        /// <summary>
        /// Registers the users left mouse down input.
        /// </summary>
        /// <remarks>
        /// This is only <c>true</c> in the exact frame the mouse button is pressed down.
        /// </remarks>
        /// <returns>True if the user uses left mouse down.</returns>
        public static bool LeftMouseDown()
        {
            return Input.GetMouseButtonDown(leftMouseButton);
        }

        /// <summary>
        /// Registers the users right mouse button input.
        /// </summary>
        /// <returns>true if the user uses the left mouse button.</returns>
        public static bool RightMouseInteraction()
        {
            return Input.GetMouseButton(rightMouseButton);
        }

        /// <summary>
        /// Registers the uses mouse button up input (release the selected button).
        /// </summary>
        /// <param name="state">The mouse button which should be observed.</param>
        /// <returns>true if the user releases the selected mouse button.</returns>
        public static bool MouseUp(MouseButton state)
        {
            return Input.GetMouseButtonUp((int)state);
        }

        /// <summary>
        /// Registers the uses mouse button input (button holded).
        /// </summary>
        /// <param name="state">The mouse button which should be observed.</param>
        /// <returns>true if the user holds the selected mouse button.</returns>
        public static bool MouseHold(MouseButton state)
        {
            return Input.GetMouseButton((int)state);
        }

        /// <summary>
        /// Registers the uses mouse down button input (button down).
        /// </summary>
        /// <param name="state">The mouse down button which should be observed.</param>
        /// <returns>true if the user press the selected mouse button.</returns>
        public static bool MouseDown(MouseButton state)
        {
            return Input.GetMouseButtonDown((int)state);
        }

        /// <summary>
        /// Registers the use of the mouse wheel for scrolling down.
        /// </summary>
        /// <returns>True if the user scrolls down.</returns>
        public static bool ScrollDown()
        {
            return Input.mouseScrollDelta.y <= -0.1;
        }

        /// <summary>
        /// Registers the use of the mouse wheel for scrolling up.
        /// </summary>
        /// <returns>True if the user scrolls up.</returns>
        public static bool ScrollUp()
        {
            return Input.mouseScrollDelta.y >= 0.1;
        }

        #endregion

        //----------------------------------------------------

        #region Chat

        //----------------------------------------------------

        /// <summary>
        /// Opens the text chat.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool OpenTextChat()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleTextChat);

            if (result)
            {
                // Track the OpenTextChat action
                TracingHelperService.Instance?.TrackKeyPress("OpenTextChat");
            }

            return result;
        }

        /// <summary>
        /// Toggles the voice chat.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleVoiceChat()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleVoiceChat);

            if (result)
            {
                // Track the ToggleVoiceChat action
                TracingHelperService.Instance?.TrackKeyPress("ToggleVoiceChat");
            }

            return result;
        }

        #endregion

        //----------------------------------------------------

        #region Notifications

        //----------------------------------------------------

        /// <summary>
        /// True if the user wants to close all notifications.
        /// </summary>
        /// <returns>True if the user wants to close all notifications.</returns>
        public static bool CloseAllNotifications()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.CloseNotifications);

            if (result)
            {
                // Track the CloseAllNotifications action
                TracingHelperService.Instance?.TrackKeyPress("CloseAllNotifications");
            }

            return result;
        }

        #endregion

        #region FaceCam

        /// <summary>
        /// True if the user wants to turn the FaceCam on or off (toggling).
        /// </summary>
        /// <returns>True if the user wants to turn the FaceCam on or off.</returns>
        internal static bool ToggleFaceCam()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleFaceCam);

            if (result)
            {
                // Track the ToggleFaceCam action
                TracingHelperService.Instance?.TrackKeyPress("ToggleFaceCam");
            }

            return result;
        }

        /// <summary>
        /// True if the user wants to switch the position of the FaceCam on the player's face (toggling).
        /// </summary>
        /// <returns>True if the user wants to switch the position of the FaceCam.</returns>
        internal static bool ToggleFaceCamPosition()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleFaceCamPosition);

            if (result)
            {
                // Track the ToggleFaceCamPosition action
                TracingHelperService.Instance?.TrackKeyPress("ToggleFaceCamPosition");
            }

            return result;
        }

        #endregion

        //----------------------------------------------------

        #region Drawable

        /// <summary>
        /// Undoes a part of the current running action.
        /// Needed for removing a point while drawing a straight line.
        /// </summary>
        /// <returns>True if the user wants to undo a part of the running action.</returns>
        internal static bool PartUndo()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.PartUndo);

            if (result)
            {
                // Track the PartUndo action
                TracingHelperService.Instance?.TrackKeyPress("PartUndo");
            }

            return result;
        }

        /// <summary>
        /// Moves an object up.
        /// </summary>
        /// <returns>True if the user wants to move an object up.</returns>
        internal static bool MoveObjectUp()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectUp);

            if (result)
            {
                // Track the MoveObjectUp action
                TracingHelperService.Instance?.TrackKeyPress("MoveObjectUp");
            }

            return result;
        }

        /// <summary>
        /// Moves an object down.
        /// </summary>
        /// <returns>True if the user wants to move an object down.</returns>
        internal static bool MoveObjectDown()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectDown);

            if (result)
            {
                // Track the MoveObjectDown action
                TracingHelperService.Instance?.TrackKeyPress("MoveObjectDown");
            }

            return result;
        }

        /// <summary>
        /// Moves an object left.
        /// </summary>
        /// <returns>True if the user wants to move an object left.</returns>
        internal static bool MoveObjectLeft()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectLeft);

            if (result)
            {
                // Track the MoveObjectLeft action
                TracingHelperService.Instance?.TrackKeyPress("MoveObjectLeft");
            }

            return result;
        }

        /// <summary>
        /// Moves an object right.
        /// </summary>
        /// <returns>True if the user wants to move an object right.</returns>
        internal static bool MoveObjectRight()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectRight);

            if (result)
            {
                // Track the MoveObjectRight action
                TracingHelperService.Instance?.TrackKeyPress("MoveObjectRight");
            }

            return result;
        }

        /// <summary>
        /// Moves an object forward.
        /// </summary>
        /// <returns>True if the user wants to move an object forward.</returns>
        internal static bool MoveObjectForward()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectForward);

            if (result)
            {
                // Track the MoveObjectForward action
                TracingHelperService.Instance?.TrackKeyPress("MoveObjectForward");
            }

            return result;
        }

        /// <summary>
        /// Moves an object backward.
        /// </summary>
        /// <returns>True if the user wants to move an object backward.</returns>
        internal static bool MoveObjectBackward()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectBackward);

            if (result)
            {
                // Track the MoveObjectBackward action
                TracingHelperService.Instance?.TrackKeyPress("MoveObjectBackward");
            }

            return result;
        }

        /// <summary>
        /// Toggles the drawable manager menu.
        /// </summary>
        /// <returns>True if the user wants to toggle the drawable manager menu.</returns>
        internal static bool ToggleDrawableManagerView()
        {
            bool result = KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.DrawableManagerView);

            if (result)
            {
                // Track the ToggleDrawableManagerView action
                TracingHelperService.Instance?.TrackKeyPress("ToggleDrawableManagerView");
            }

            return result;
        }

        #endregion
    }
}
