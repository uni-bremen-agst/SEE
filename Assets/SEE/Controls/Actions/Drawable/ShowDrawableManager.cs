using Cysharp.Threading.Tasks;
using SEE.GO;
using SEE.UI;
using SEE.UI.Drawable;
using SEE.UI.Window;
using SEE.UI.Window.DrawableManagerWindow;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Allows the user to show or hide the drawable manager window.
    /// </summary>
    /// <remarks>This component is meant to be attached to a player.</remarks>
    public class ShowDrawableManager : MonoBehaviour
    {
        /// <summary>
        /// The local player's window space.
        /// </summary>
        private WindowSpace space;

        /// <summary>
        /// Status indicating the state of the window (open/closed).
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// The drawable manager window.
        /// </summary>
        private DrawableManagerWindow window;

        /// <summary>
        /// Displays the drawable manager window.
        /// </summary>
        internal void ShowDrawableManagerWindow()
        {
            isOpen = true;
            window = UICanvas.Canvas.AddOrGetComponent<DrawableManagerWindow>();
            SetupManager().Forget();
            return;

            async UniTaskVoid SetupManager()
            {
                await UniTask.WaitUntil(() => WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer] != null);
                space = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
                space.AddWindow(window);
                space.ActiveWindow = window;
            }
        }

        /// <summary>
        /// Close the drawable manager window.
        /// </summary>
        internal void HideDrawableManagerWindow()
        {
            isOpen = false;
            bool wasClosed = space.CloseWindow(window);
            if (!wasClosed)
            {
                ShowDrawableManagerWindow();
            }
        }

        /// <summary>
        /// Based on the input, it shows or hides the drawable manager window.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleDrawableManagerView())
            {
                Toggle();
            }
        }

        /// <summary>
        /// Toggles the current state of the drawable manager window.
        /// </summary>
        public void Toggle()
        {
            if (!isOpen)
            {
                ShowDrawableManagerWindow();
            }
            else
            {
                HideDrawableManagerWindow();
            }
        }
    }
}
