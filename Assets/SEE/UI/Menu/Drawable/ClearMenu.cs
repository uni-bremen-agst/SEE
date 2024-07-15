using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using System.Collections;
using TMPro;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// this class provides a menu, with which the player can
    /// configurate the clear action.
    /// </summary>
    public class ClearMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private string clearMenuPrefab = "Prefabs/UI/Drawable/ClearMenu";

        /// <summary>
        /// The instance for the menu.
        /// </summary>
        private readonly GameObject instance;

        /// <summary>
        /// The types of clearing.
        /// </summary>
        internal enum Type
        {
            Current,
            All
        }

        /// <summary>
        /// The current selected clearing type.
        /// </summary>
        private Type currentType;

        /// <summary>
        /// The current selected state for deleting.
        /// </summary>
        private bool shouldDeletePage;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClearMenu()
        {
            instance = PrefabInstantiator.InstantiatePrefab(clearMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            TextMeshProUGUI text = GameFinder.FindChild(instance, "DeleteText")
                .GetComponent<TextMeshProUGUI>();

            SwitchManager typeManager = GameFinder.FindChild(instance, "TypeSwitch")
                .GetComponent<SwitchManager>();

            typeManager.OnEvents.AddListener(() => 
            { 
                currentType = Type.All;
                text.text = "Delete Pages";
            });

            typeManager.OffEvents.AddListener(() =>
            {
                currentType = Type.Current;
                text.text = "Delete Page";
            });

            SwitchManager deleteManager = GameFinder.FindChild(instance, "DeleteSwitch")
                .GetComponent<SwitchManager>();
            deleteManager.OnEvents.AddListener(() => shouldDeletePage = true);
            deleteManager.OffEvents.AddListener(() => shouldDeletePage = false);

            
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
        /// Property for the current selected <see cref="Type"/>.
        /// </summary>
        internal Type CurrentType
        {
            get { return currentType; }
        }

        /// <summary>
        /// Property for the current selected deleting state.
        /// </summary>
        public bool ShouldDeletePage
        {
            get { return shouldDeletePage; }
        }
    }
}