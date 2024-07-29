using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using TMPro;

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
        public void Enable(string text)
        {
            if (menu == null)
            {
                Instantiate(confirmMenuPrefab);

                /// Initialize the buttons.
                ButtonManagerBasic cancelDragger = GameFinder.FindChild(menu, "CancelDragger")
                    .GetComponent<ButtonManagerBasic>();
                cancelDragger.clickEvent.AddListener(() =>
                 {
                     WasCanceled = true;
                     Destroy();
                 });
                ButtonManagerBasic confirm = GameFinder.FindChild(menu, "Confirm")
                    .GetComponent<ButtonManagerBasic>();
                confirm.clickEvent.AddListener(() =>
                {
                    WasConfirmed = true;
                    Destroy();
                });
                ButtonManagerBasic cancel = GameFinder.FindChild(menu, "Cancel")
                    .GetComponent<ButtonManagerBasic>();
                cancel.clickEvent.AddListener(() =>
                {
                    WasCanceled = true;
                    Destroy();
                });
                TextMeshProUGUI description = GameFinder.FindChild(menu, "Description")
                    .GetComponent<TextMeshProUGUI>();
                description.text = text;
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
