namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Manages state that is shared between consecutive instances of drawable actions
    /// and must be reset when their action state is left.
    /// </summary>
    internal static class DrawableActionLifecycle
    {
        /// <summary>
        /// Whether this lifecycle has already subscribed to action state changes.
        /// </summary>
        private static bool initialized;

        /// <summary>
        /// Initializes the drawable action lifecycle.
        /// </summary>
        internal static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            GlobalActionHistory.ActionStateChanged += OnActionStateChanged;
            initialized = true;
        }

        /// <summary>
        /// Resets state belonging to the action state that was left.
        /// </summary>
        /// <param name="previousActionState">The action state that was left.</param>
        /// <param name="newActionState">The newly entered action state.</param>
        private static void OnActionStateChanged(ActionStateType previousActionState, ActionStateType newActionState)
        {
            if (previousActionState == ActionStateTypes.WriteText)
            {
                WriteTextAction.Reset();
            }

            if (previousActionState == ActionStateTypes.Edit)
            {
                EditAction.Reset();
            }

            if (previousActionState == ActionStateTypes.CutCopyPaste)
            {
                CutCopyPasteAction.Reset();
            }

            if (previousActionState == ActionStateTypes.Scale)
            {
                ScaleAction.Reset();
            }

            if (previousActionState == ActionStateTypes.LayerChanger)
            {
                LayerChangeAction.Reset();
            }
        }
    }
}
