namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// The available user actions that can be triggered by a key on the keyboard.
    /// </summary>
    internal enum KeyAction
    {
        /// <summary>
        /// User asks for help.
        /// </summary>
        Help,
        /// <summary>
        /// User toggles the player menu.
        /// </summary>
        ToggleMenu,
        /// <summary>
        /// User toggles the voice control (interpreted by the personal assistant).
        /// </summary>
        ToggleVoiceControl,
        /// <summary>
        /// Turns on/off the settings menu.
        /// </summary>
        ToggleSettings,
        /// <summary>
        /// Turns on/off the built-in Internet browser.
        /// </summary>
        ToggleBrowser,
        /// <summary>
        /// Turns on/off the mirror.
        /// </summary>
        ToggleMirror,
        /// <summary>
        /// Undoes the last action.
        /// </summary>
        Undo,
        /// <summary>
        /// Re-does the last action.
        /// </summary>
        Redo,
        /// <summary>
        /// Opens/closes the configuration menu.
        /// </summary>
        ConfigMenu,
        /// <summary>
        /// Opens/closes the tree view window.
        /// </summary>
        TreeView,
        /// <summary>
        /// Saves the current position when recording paths.
        /// </summary>
        SavePathPosition,
        /// <summary>
        /// Starts/stops the automated path replay.
        /// </summary>
        TogglePathPlaying,
        /// <summary>
        /// Turns the metric charts on/off.
        /// </summary>
        ToggleCharts,
        /// <summary>
        /// Toggles hovering/selection for markers in metric charts.
        /// </summary>
        ToggleMetricHoveringSelection,
        /// <summary>
        /// Toggles the visibility of all edges of a hovered code city.
        /// </summary>
        ToggleEdges,
        /// <summary>
        /// Forgets all currently selected objects.
        /// </summary>
        Unselect,
        /// <summary>
        /// Cancels an action.
        /// </summary>
        Cancel,
        /// <summary>
        /// To reset a NavigationAction: resets position/rotation to the original position/rotation.
        /// </summary>
        Reset,
        /// <summary>
        /// Zooms into a city.
        /// </summary>
        ZoomInto,
        /// <summary>
        /// While moving the city, snaps to one of eight predefined directions.
        /// While rotating the city, rotates in 45 degree steps.
        /// </summary>
        Snap,
        /// <summary>
        /// The user drags the city as a whole on the plane.
        /// </summary>
        DragHovered,
        /// <summary>
        /// Toggles between the locked and free camera mode.
        /// </summary>
        ToggleCameraLock,
        /// <summary>
        /// Toggles between pointing.
        /// </summary>
        Pointing,
        /// <summary>
        /// Boosts the speed of the player movement. While pressed, movement is faster.
        /// </summary>
        BoostCameraSpeed,
        /// <summary>
        /// Move camera (player) forward.
        /// </summary>
        MoveForward,
        /// <summary>
        /// Move camera (player) backward.
        /// </summary>
        MoveBackward,
        /// <summary>
        /// Move camera (player) to the right.
        /// </summary>
        MoveRight,
        /// <summary>
        /// Move camera (player) to the left.
        /// </summary>
        MoveLeft,
        /// <summary>
        /// Move camera (player) up.
        /// </summary>
        MoveUp,
        /// <summary>
        /// Move camera (player) down.
        /// </summary>
        MoveDown,
        /// <summary>
        /// Sets a new marker.
        /// </summary>
        SetMarker,
        /// <summary>
        /// Deletes a marker.
        /// </summary>
        DeleteMarker,
        /// <summary>
        /// Toggles between between the two canvases for the animation and selection of a revision.
        /// </summary>
        ToggleEvolutionCanvases,
        /// <summary>
        /// The previous element in the animation is to be shown.
        /// </summary>
        Previous,
        /// <summary>
        /// The next element in the animation is to be shown.
        /// </summary>
        Next,
        /// <summary>
        /// Toggles auto play of the animation.
        /// </summary>
        ToggleAutoPlay,
        /// <summary>
        /// Double animation speed.
        /// </summary>
        IncreaseAnimationSpeed,
        /// <summary>
        /// Halve animation speed.
        /// </summary>
        DecreaseAnimationSpeed,
        /// <summary>
        /// Toggles execution order (forward/backward).
        /// </summary>
        ToggleExecutionOrder,
        /// <summary>
        /// Continues execution until next breakpoint is reached.
        /// </summary>
        ExecuteToBreakpoint,
        /// <summary>
        /// Execution is back to very first statement.
        /// </summary>
        FirstStatement,
        /// <summary>
        /// Toggles the menu of the available windows.
        /// </summary>
        ShowWindowMenu,
        /// <summary>
        /// Opens the text chat.
        /// </summary>
        ToggleTextChat,
        /// <summary>
        /// Toggles the voice chat.
        /// </summary>
        ToggleVoiceChat,
        /// <summary>
        /// Closes all open notifications.
        /// </summary>
        CloseNotifications,
        /// <summary>
        /// Toggles the face camera.
        /// </summary>
        ToggleFaceCam,
        /// <summary>
        /// Toggles the position of the FaceCam on the player's face.
        /// </summary>
        ToggleFaceCamPosition,
        /// <summary>
        /// Toggles the menu for holistic code metrics.
        /// </summary>
        ToggleHolisticMetricsMenu,
        /// <summary>
        /// Undoes a part of the current running action.
        /// Needed for drawing a straight line.
        /// </summary>
        PartUndo,
        /// <summary>
        /// Moves a drawable type object up.
        /// </summary>
        MoveObjectUp,
        /// <summary>
        /// Moves a drawable type object down.
        /// </summary>
        MoveObjectDown,
        /// <summary>
        /// Moves a drawable type object left.
        /// </summary>
        MoveObjectLeft,
        /// <summary>
        /// Moves a drawable type object right.
        /// </summary>
        MoveObjectRight,
        /// <summary>
        /// Moves a drawable object forward.
        /// </summary>
        MoveObjectForward,
        /// <summary>
        /// Moves a drawable object backward.
        /// </summary>
        MoveObjectBackward,
        /// <summary>
        /// Opens/closes the drawable manager view.
        /// </summary>
        DrawableManagerView,
        /// <summary>
        /// This is to load the graphdatabase for RASA
        /// </summary>
        LoadDB
    }
}
