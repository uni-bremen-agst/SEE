using SEE.Controls;
using SEE.UI.Window;

namespace SEE.Game
{
    /// <summary>
    /// Provides utility methods for managing game windows within the local player's window space.
    /// Handles activation and ensures that windows are properly registered and focused.
    /// </summary>
    public static class GameWindowManager
    {
        /// <summary>
        /// Activates the specified <see cref="BaseWindow"/> for the local player.
        /// If the window is not yet part of the player's <see cref="WindowSpace"/>,
        /// it will be added first. The window is then brought to the foreground
        /// and set as the currently active window.
        /// </summary>
        /// <param name="window">
        /// The window to activate. Must not be null.
        /// </param>
        public static void ActivateWindow(BaseWindow window)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (!manager.Windows.Contains(window))
            {
                manager.AddWindow(window);
            }
            manager.ActiveWindow = window;
        }
    }
}
