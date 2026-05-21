using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.UI.Notification;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides the mind-map menu.
    /// </summary>
    public class MindMapMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string mindMapMenuPrefab = "Prefabs/UI/Drawable/MindMapMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private MindMapMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static MindMapMenu Instance { get; private set; }

        static MindMapMenu()
        {
            Instance = new MindMapMenu();
        }

        /// <summary>
        /// Whether this class has an operation in store that hasn't been fetched yet.
        /// </summary>
        private static bool gotOperation;

        /// <summary>
        /// If <see cref="gotOperation"/> is true, this contains the button kind which the player selected.
        /// </summary>
        private static Operation chosenOperation;

        /// <summary>
        /// Contains keywords for the different buttons of the mind-map menu.
        /// </summary>
        public enum Operation
        {
            None,
            Theme,
            Subtheme,
            Leaf
        }

        /// <summary>
        /// Creates and adds the necessary handler to the buttons.
        /// </summary>
        public override void Enable()
        {
            Instance.Instantiate(mindMapMenuPrefab);
            base.Enable();

            /// Initialize the button for spawn a theme.
            ButtonManagerBasic theme = GameFinder.FindChild(Instance.gameObject, "Theme").GetComponent<ButtonManagerBasic>();
            theme.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Theme;
                ShowNotification.Info("Select position", "Choose a suitable position for the new central theme.", 3);
            });

            /// Initialize the button for spawn a subtheme.
            ButtonManagerBasic subtheme = GameFinder.FindChild(Instance.gameObject, "Subtheme").GetComponent<ButtonManagerBasic>();
            subtheme.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Subtheme;
                ShowNotification.Info("Select position", "Choose a suitable position for the new subtheme.", 3);
            });

            /// Initialize the button for spawn a leaf.
            ButtonManagerBasic leaf = GameFinder.FindChild(Instance.gameObject, "Leaf").GetComponent<ButtonManagerBasic>();
            leaf.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Leaf;
                ShowNotification.Info("Select position", "Choose a suitable position for the new leaf.", 3);
            });
        }

        /// <summary>
        /// If <see cref="gotOperation"/> is true, the <paramref name="operation"/> will be the chosen operation by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="operation">The chosen operation the player confirmed, if that doesn't exist, some dummy value.</param>
        /// <returns><see cref="gotOperation"/>.</returns>
        public static bool TryGetOperation(out Operation operation)
        {
            if (gotOperation)
            {
                operation = chosenOperation;
                gotOperation = false;
                return true;
            }

            operation = Operation.None;
            return false;
        }
    }
}
