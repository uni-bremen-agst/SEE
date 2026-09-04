using SEE.Controls.Actions;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Manages menus whose lifetime is bound to a drawable action state rather than
    /// to an individual reversible action instance.
    /// </summary>
    internal static class DrawableMenuLifecycle
    {
        /// <summary>
        /// Whether this lifecycle has already subscribed to action state changes.
        /// </summary>
        private static bool initialized;

        /// <summary>
        /// Initializes the drawable menu lifecycle.
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
        /// Updates the tool-session menus when the current action state changes.
        /// </summary>
        /// <param name="previousActionState">The action state that was left.</param>
        /// <param name="newActionState">The newly entered action state.</param>
        private static void OnActionStateChanged(ActionStateType previousActionState, ActionStateType newActionState)
        {
            DisableMenu(previousActionState);
            EnableMenu(newActionState);
        }

        /// <summary>
        /// Disables the menu associated with <paramref name="actionState"/>.
        /// </summary>
        /// <param name="actionState">The action state whose menu should be disabled.</param>
        private static void DisableMenu(ActionStateType actionState)
        {
            if (actionState == ActionStateTypes.DrawFreehand)
            {
                LineMenu.Instance.Disable();
            }
            else if (actionState == ActionStateTypes.DrawShapes)
            {
                ShapeMenu.Disable();
            }
            else if (actionState == ActionStateTypes.WriteText)
            {
                TextMenu.Instance.Disable();
            }
        }

        /// <summary>
        /// Enables the menu associated with <paramref name="actionState"/>.
        /// </summary>
        /// <param name="actionState">The action state whose menu should be enabled.</param>
        private static void EnableMenu(ActionStateType actionState)
        {
            if (actionState == ActionStateTypes.DrawFreehand)
            {
                LineMenu.Instance.EnableForDrawing();
            }
            else if (actionState == ActionStateTypes.DrawShapes)
            {
                ShapeMenu.Enable();
            }
            else if (actionState == ActionStateTypes.WriteText)
            {
                TextMenu.EnableForWriting();
            }
        }
    }
}