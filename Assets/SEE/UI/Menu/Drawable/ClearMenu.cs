using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Utils;
using TMPro;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu, with which the player can
    /// configure the clear action.
    /// </summary>
    public class ClearMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string clearMenuPrefab = "Prefabs/UI/Drawable/ClearMenu";

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
        /// Constructor.
        /// </summary>
        public ClearMenu()
        {
            instance = PrefabInstantiator.InstantiatePrefab(clearMenuPrefab,
                                                            UICanvas.Canvas.transform, false);
            TextMeshProUGUI text = GameFinder.FindChild(instance, "DeleteText")
                .GetComponent<TextMeshProUGUI>();

            SwitchManager typeManager = GameFinder.FindChild(instance, "TypeSwitch")
                .GetComponent<SwitchManager>();

            typeManager.OnEvents.AddListener(() =>
            {
                CurrentType = Type.All;
                text.text = "Delete Pages";
            });

            typeManager.OffEvents.AddListener(() =>
            {
                CurrentType = Type.Current;
                text.text = "Delete Page";
            });

            SwitchManager deleteManager = GameFinder.FindChild(instance, "DeleteSwitch")
                .GetComponent<SwitchManager>();
            deleteManager.OnEvents.AddListener(() => ShouldDeletePage = true);
            deleteManager.OffEvents.AddListener(() => ShouldDeletePage = false);
        }

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
        }

        /// <summary>
        /// The currently selected <see cref="Type"/>.
        /// </summary>
        internal Type CurrentType
        {
            get; private set;
        }

        /// <summary>
        /// The currently selected deleting state.
        /// </summary>
        public bool ShouldDeletePage
        {
            get; private set;
        }
    }
}
