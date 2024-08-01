using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using TMPro;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu, with which the player can
    /// configure the clear action.
    /// </summary>
    public class ClearMenu : Menu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string clearMenuPrefab = "Prefabs/UI/Drawable/ClearMenu";

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
            Instantiate(clearMenuPrefab);
            TextMeshProUGUI text = GameFinder.FindChild(gameObject, "DeleteText")
                .GetComponent<TextMeshProUGUI>();

            SwitchManager typeManager = GameFinder.FindChild(gameObject, "TypeSwitch")
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

            SwitchManager deleteManager = GameFinder.FindChild(gameObject, "DeleteSwitch")
                .GetComponent<SwitchManager>();
            deleteManager.OnEvents.AddListener(() => ShouldDeletePage = true);
            deleteManager.OffEvents.AddListener(() => ShouldDeletePage = false);
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
