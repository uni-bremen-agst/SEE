using SEE.Controls.KeyActions;
using SEE.Utils;
using UnityEngine;
using SEE.GO;
using SEE.XR;

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
        [UnityEditor.InitializeOnEnterPlayMode]
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
            return KeyboardShortcutsEnabled
                && KeyBindings.IsDown(KeyAction.Help);
        }

        /// <summary>
        /// Toggles voice control (i.e., for voice commands) on/off.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleVoiceControl()
        {
            return KeyboardShortcutsEnabled
                && KeyBindings.IsDown(KeyAction.ToggleVoiceControl);
        }

        /// <summary>
        /// Turns on/off the player-action menu.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMenu()
        {
            return KeyboardShortcutsEnabled
                && KeyBindings.IsDown(KeyAction.ToggleMenu);
        }

        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleSettings()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleSettings);
        }

        /// <summary>
        /// Turns on/off the browser.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleBrowser()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleBrowser);
        }

        /// <summary>
        /// Turns on/off the mirror.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMirror()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleMirror);
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
            if (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.UndoToggle)
            {
                bool undo = XRSEEActions.UndoToggle;
                XRSEEActions.UndoToggle = false;
                return undo;
            }
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
#endif
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Undo);
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
            if (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.RedoToggle)
            {
                bool redo = XRSEEActions.RedoToggle;
                XRSEEActions.RedoToggle = false;
                return redo;
            }
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
#endif
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Redo);
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
            return KeyboardShortcutsEnabled & KeyBindings.IsDown(KeyAction.ConfigMenu);
        }

        /// <summary>
        /// The user wants to toggle the visibility of all edges in a hovered code city.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool ToggleEdges()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleEdges);
        }

        /// <summary>
        /// The user wants to toggle the visibility of the tree view window.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool ToggleTreeView()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.TreeView);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.SavePathPosition);
        }

        /// <summary>
        /// Toggles automatic path playing.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool TogglePathPlaying()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.TogglePathPlaying);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleCharts);
        }

        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleMetricHoveringSelection()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleMetricHoveringSelection);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Unselect);
        }

        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleCameraLock()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleCameraLock);
        }

        /// <summary>
        /// Cancels an action.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Cancel()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Cancel);
        }

        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Reset()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Reset);
        }

        /// <summary>
        /// Zooms into a city.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ZoomInto()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ZoomInto);
        }

        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Snap()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.Snap);
        }

        /// <summary>
        /// The user wants to drag the hovered element of the city on its plane.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        internal static bool DragHovered()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.DragHovered);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.BoostCameraSpeed);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Pointing);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.SetMarker);
        }
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool DeleteMarker()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.DeleteMarker);
        }
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleEvolutionCanvases()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleEvolutionCanvases);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Previous);
        }
        /// <summary>
        /// The next revision is to be shown.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool Next()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.Next);
        }
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleAutoPlay()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleAutoPlay);
        }

        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleExecutionOrder()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleExecutionOrder);
        }

        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool FirstStatement()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.FirstStatement);
        }

        /// <summary>
        /// Double animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool IncreaseAnimationSpeed()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.IncreaseAnimationSpeed);
        }

        /// <summary>
        /// Halve animation speed.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool DecreaseAnimationSpeed()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.DecreaseAnimationSpeed);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ExecuteToBreakpoint);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ShowWindowMenu);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleTextChat);
        }

        /// <summary>
        /// Toggles the voice chat.
        /// </summary>
        /// <returns>true if the user requests this action and <see cref="KeyboardShortcutsEnabled"/></returns>
        public static bool ToggleVoiceChat()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleVoiceChat);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.CloseNotifications);
        }

        #endregion

        #region FaceCam
        /// <summary>
        /// True if the user wants to turn the FaceCam on or off (toggling).
        /// </summary>
        /// <returns>True if the user wants to turn the FaceCam on or off.</returns>
        internal static bool ToggleFaceCam()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleFaceCam);
        }

        /// <summary>
        /// True if the user wants to switch the position of the FaceCam on the player's face (toggling).
        /// </summary>
        /// <returns>True if the user wants to switch the position of the FaceCam.</returns>
        internal static bool ToggleFaceCamPosition()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.ToggleFaceCamPosition);
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
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.PartUndo);
        }

        /// <summary>
        /// Moves an object up.
        /// </summary>
        /// <returns>True if the user wants to moves an object up.</returns>
        internal static bool MoveObjectUp()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectUp);
        }

        /// <summary>
        /// Moves an object down.
        /// </summary>
        /// <returns>True if the user wants to moves an object down.</returns>
        internal static bool MoveObjectDown()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectDown);
        }

        /// <summary>
        /// Moves an object left.
        /// </summary>
        /// <returns>True if the user wants to moves an object left.</returns>
        internal static bool MoveObjectLeft()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectLeft);
        }

        /// <summary>
        /// Moves an object right.
        /// </summary>
        /// <returns>True if the user wants to moves an object right.</returns>
        internal static bool MoveObjectRight()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectRight);
        }

        /// <summary>
        /// Moves an object forward.
        /// </summary>
        /// <returns>True if the user wants to moves an object forward.</returns>
        internal static bool MoveObjectForward()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectForward);
        }

        /// <summary>
        /// Moves an object backward.
        /// </summary>
        /// <returns>True if the user wants to moves an object backward.</returns>
        internal static bool MoveObjectBackward()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsPressed(KeyAction.MoveObjectBackward);
        }

        /// <summary>
        /// Toggles the drawable manager menu.
        /// </summary>
        /// <returns>True if the user wants to toggle the drawable manager menu.</returns>
        internal static bool ToggleDrawableManagerView()
        {
            return KeyboardShortcutsEnabled && KeyBindings.IsDown(KeyAction.DrawableManagerView);
        }
        #endregion
    }
}
