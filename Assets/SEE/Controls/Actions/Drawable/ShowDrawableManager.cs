using Assets.SEE.UI.Window.DrawableManagerWindow;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Window;
using UnityEngine;

namespace Assets.SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>This component is meant to be attached to a player.</remarks>
    public class ShowDrawableManager : MonoBehaviour
    {
        /// <summary>
        /// The local player's window space.
        /// </summary>
        private WindowSpace space;
        
        /// <summary>
        /// Status indicating the state of the view (open/closed).
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// The drawable manager window.
        /// </summary>
        private DrawableManagerWindow window;

        /// <summary>
        /// Displays the drawable manager view.
        /// </summary>
        internal void ShowDrawableManagerView()
        {
            isOpen = true;
            window = GameObject.Find("UI Canvas").AddOrGetComponent<DrawableManagerWindow>();
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
        /// Close the drawable manager view.
        /// </summary>
        internal void HideDrawableManagerView()
        {
            isOpen = false;
            bool wasClosed = space.CloseWindow(window);
            if (!wasClosed) 
            {
                ShowDrawableManagerView();
            }
        }

        private void Update()
        {
            if (SEEInput.ToggleDrawableManagerView())
            {
                if (!isOpen)
                {
                    ShowDrawableManagerView();
                } else
                {
                    HideDrawableManagerView();
                }
            }
        }
    }
}