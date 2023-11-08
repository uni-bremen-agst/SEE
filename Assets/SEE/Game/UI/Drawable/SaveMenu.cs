using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.LowLevel;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// The save menu for drawable type objects
    /// </summary>
    public static class SaveMenu
    {
        /// <summary>
        /// The location where the save menu prefeb is placed.
        /// </summary>
        private const string saveMenuPrefab = "Prefabs/UI/Drawable/SaveMenu";

        /// <summary>
        /// The instance for the save menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// The instance for the save single or more drawable button.
        /// </summary>
        private static ButtonManagerBasic saveButton;

        /// <summary>
        /// The instance for the save all drawables button.
        /// </summary>
        private static ButtonManagerBasic saveAllButton;

        /// <summary>
        /// Creates the save menu.
        /// </summary>
        /// <param name="saveButtonCall">The action that should be executed when the save button is pressed.</param>
        /// <param name="saveAllButtonCall">The action that should be executed when the save all button is pressed.</param>
        public static void Enable(UnityAction saveButtonCall, UnityAction saveAllButtonCall)
        {
            instance = PrefabInstantiator.InstantiatePrefab(saveMenuPrefab,
                 GameObject.Find("UI Canvas").transform, false);
            saveButton = GameFinder.FindChild(instance, "Save").GetComponent<ButtonManagerBasic>();
            saveAllButton = GameFinder.FindChild(instance, "SaveAll").GetComponent<ButtonManagerBasic>();

            saveButton.clickEvent.AddListener(saveButtonCall);
            saveAllButton.clickEvent.AddListener(saveAllButtonCall);
        }

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public static void Disable()
        {
            Destroyer.Destroy(instance);
        }
    }
}