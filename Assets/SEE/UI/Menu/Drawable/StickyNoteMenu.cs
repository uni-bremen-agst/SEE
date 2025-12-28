using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.UI.Notification;
using static SEE.Controls.Actions.Drawable.StickyNoteAction;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu, with which the player can select
    /// an operation for the sticky notes.
    /// </summary>
    public class StickyNoteMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string stickyNoteMenuPrefab = "Prefabs/UI/Drawable/StickyNoteMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private StickyNoteMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static StickyNoteMenu Instance { get; private set; }

        static StickyNoteMenu()
        {
            Instance = new StickyNoteMenu();
        }

        /// <summary>
        /// Whether this class has an operation in store that wasn't yet fetched.
        /// </summary>
        private static bool gotOperation;

        /// <summary>
        /// If <see cref="gotOperation"/> is true, this contains the operation which the player selected.
        /// </summary>
        private static Operation chosenOperation;

        /// <summary>
        /// Enables the image source menu and register the needed Handler to the button's.
        /// </summary>
        public override void Enable()
        {
            /// Instantiate the menu.
            Instance.Instantiate(stickyNoteMenuPrefab);
            base.Enable();

            /// Initialize the button for the spawn option.
            ButtonManagerBasic spawn = GameFinder.FindChild(Instance.gameObject, "Spawn")
                .GetComponent<ButtonManagerBasic>();
            spawn.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Spawn;
                ShowNotification.Info("Select position", "Choose a suitable position for the sticky note.", 2);
            });

            /// Initialize the button for the move option.
            ButtonManagerBasic move = GameFinder.FindChild(Instance.gameObject, "Move")
                .GetComponent<ButtonManagerBasic>();
            move.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Move;
                ShowNotification.Info("Select for moving", "Choose the sticky note that you want to move.", 2);
            });

            /// Initialize the button for the edit option.
            ButtonManagerBasic edit = GameFinder.FindChild(Instance.gameObject, "Edit")
                .GetComponent<ButtonManagerBasic>();
            edit.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Edit;
                ShowNotification.Info("Select for editing", "Choose the sticky note that you want to edit.", 2);
            });

            /// Initialize the button for the delete option.
            ButtonManagerBasic delete = GameFinder.FindChild(Instance.gameObject, "Delete")
                .GetComponent<ButtonManagerBasic>();
            delete.clickEvent.AddListener(() =>
            {
                gotOperation = true;
                chosenOperation = Operation.Delete;
                ShowNotification.Info("Select for deleting", "Choose the sticky note that you want to delete.", 2);
            });

        }

        /// <summary>
        /// If <see cref="gotOperation"/> is true, the <paramref name="operation"/> will be the chosen operation by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="operation">The operation the player confirmed, if that doesn't exist, some dummy value.</param>
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
