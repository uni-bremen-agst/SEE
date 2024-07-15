using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Class that provides a confirm dialog menu.
    /// </summary>
    public class ConfirmDialogMenu
    {
        /// <summary>
        /// The prefab of the menu.
        /// </summary>
        private const string confirmMenuPrefab = "Prefabs/UI/Drawable/ConfirmDialog";

        /// <summary>
        /// The instance of the menu.
        /// </summary>
        private GameObject instance;

        /// <summary>
        /// Whether the dialog was confirmed.
        /// </summary>
        private bool wasConfirmed = false;

        /// <summary>
        /// Whether the dialog was canceled.
        /// </summary>
        private bool wasCanceled = false;

        /// <summary>
        /// Enables the menu.
        /// </summary>
        /// <param name="text">The text that should be displayed.</param>
        public void Enable(string text)
        {
            if (instance == null)
            {
                instance = PrefabInstantiator.InstantiatePrefab(confirmMenuPrefab,
                    GameObject.Find("UI Canvas").transform, false);

                /// Initialize the buttons.
                ButtonManagerBasic x = GameFinder.FindChild(instance, "CancelDragger")
                    .GetComponent<ButtonManagerBasic>();
                x.clickEvent.AddListener(() =>
                 {
                     wasCanceled = true;
                     Disable();
                 });
                ButtonManagerBasic confirm = GameFinder.FindChild(instance, "Confirm")
                    .GetComponent<ButtonManagerBasic>();
                confirm.clickEvent.AddListener(() =>
                {
                    wasConfirmed = true;
                    Disable();
                });
                ButtonManagerBasic cancel = GameFinder.FindChild(instance, "Cancel")
                    .GetComponent<ButtonManagerBasic>();
                cancel.clickEvent.AddListener(() =>
                {
                    wasCanceled = true;
                    Disable();
                });
                TextMeshProUGUI description = GameFinder.FindChild(instance, "Description")
                    .GetComponent<TextMeshProUGUI>();
                description.text = text;
            }
        }

        /// <summary>
        /// Destroy's the menu.
        /// </summary>
        public void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
        }

        /// <summary>
        /// Gets the state, if the menu is opened.
        /// </summary>
        /// <returns>The respective status of whether the menu is open.</returns>
        public bool IsOpen()
        {
            return instance != null;
        }

        /// <summary>
        /// Query whether the dialog was canceled.
        /// </summary>
        /// <returns>Whether the dialog was canceled.</returns>
        public bool WasCanceled()
        {
            return wasCanceled;
        }

        /// <summary>
        /// Query whether the dialog was confirmed.
        /// </summary>
        /// <returns>Whether the dialog was confirmed.</returns>
        public bool WasConfirmed()
        {
            return wasConfirmed;
        }
    }
}