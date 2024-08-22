using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using TMPro;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Class that provides a confirm dialog menu.
    /// </summary>
    public class ConfirmDialogMenu : Menu
    {
        /// <summary>
        /// The prefab of the menu.
        /// </summary>
        private const string confirmMenuPrefab = "Prefabs/UI/Drawable/ConfirmDialog";

        /// <summary>
        /// Enables the dialog menu with given <paramref name="text"/>
        /// </summary>
        /// <param name="text">The text that should be displayed.</param>
        public ConfirmDialogMenu(string text)
        {
            Instantiate(confirmMenuPrefab);

            /// Initialize the buttons.
            ButtonManagerBasic cancelDragger = GameFinder.FindChild(gameObject, "CancelDragger")
                .GetComponent<ButtonManagerBasic>();
            cancelDragger.clickEvent.AddListener(() =>
             {
                 WasCanceled = true;
                 Destroy();
             });
            ButtonManagerBasic confirm = GameFinder.FindChild(gameObject, "Confirm")
                .GetComponent<ButtonManagerBasic>();
            confirm.clickEvent.AddListener(() =>
            {
                WasConfirmed = true;
                Destroy();
            });
            ButtonManagerBasic cancel = GameFinder.FindChild(gameObject, "Cancel")
                .GetComponent<ButtonManagerBasic>();
            cancel.clickEvent.AddListener(() =>
            {
                WasCanceled = true;
                Destroy();
            });
            TextMeshProUGUI description = GameFinder.FindChild(gameObject, "Description")
                .GetComponent<TextMeshProUGUI>();
            description.text = text;
        }

        /// <summary>
        /// Executes the action after confirming the dialog.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <returns>nothing, it waits until the dialog was confirmed or canceled.</returns>
        public async UniTask ExecuteAfterConfirmAsync(UnityAction action)
        {
            while (IsOpen())
            {
                await UniTask.Yield();
            }
            if (WasConfirmed && !WasCanceled)
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// True if the dialog was canceled.
        /// </summary>
        public bool WasCanceled { get; private set; } = false;

        /// <summary>
        /// True if the dialog was confirmed.
        /// </summary>
        public bool WasConfirmed { get; private set; } = false;
    }
}
