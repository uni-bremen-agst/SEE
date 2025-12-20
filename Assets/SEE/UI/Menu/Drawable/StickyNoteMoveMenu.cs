using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the move menus for the sticky notes.
    /// </summary>
    public class StickyNoteMoveMenu : SingletonMenu
    {
        /// <summary>
        /// The prefab of the sticky note move menu.
        /// </summary>
        private const string moveMenuPrefab = "Prefabs/UI/Drawable/StickyNoteMove";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private StickyNoteMoveMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static StickyNoteMoveMenu Instance { get; private set; }

        static StickyNoteMoveMenu()
        {
            Instance = new StickyNoteMoveMenu();
        }

        /// <summary>
        /// Whether this class has a finished rotation in store that wasn't yet fetched.
        /// </summary>
        private static bool isFinish;

        /// <summary>
        /// Create and enables the sticky note move menu.
        /// It register the necessary Handler to the menu interface.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        public static void Enable(GameObject stickyNoteHolder, bool spawnMode = false)
        {
            Instance.Instantiate(moveMenuPrefab);
            GameObject surface = GameFinder.GetDrawableSurface(stickyNoteHolder);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Register the switch for speed up option.
            SwitchManager speedUpManager = GameFinder.FindChild(Instance.gameObject, "SpeedSwitch")
                .GetComponent<SwitchManager>();
            float speed = ValueHolder.Move;
            /// Turn on increase the speed to <see cref="ValueHolder.MoveFast"/>.
            speedUpManager.OnEvents.AddListener(() => speed = ValueHolder.MoveFast);
            /// Turn off decrease the speed to <see cref="ValueHolder.Move"/>.
            speedUpManager.OffEvents.AddListener(() => speed = ValueHolder.Move);

            /// Register the finish button. It destroys the move menu and set the finish attribut.
            GameFinder.FindChild(Instance.gameObject, "Finish").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(Instance.gameObject);
                isFinish = true;
            });

            /// In this region the movement buttons will be register.
            /// It executes also the network action.
            /// They cannot be outsourced, as otherwise,
            /// the speed switching would no longer work.
            /// Therefore, they are grouped together in a region.
            #region Movement buttons
            GameFinder.FindChild(Instance.gameObject, "Left").AddComponent<ButtonHeld>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Left, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(Instance.gameObject, "Right").AddComponent<ButtonHeld>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Right, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(Instance.gameObject, "Up").AddComponent<ButtonHeld>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Up, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(Instance.gameObject, "Down").AddComponent<ButtonHeld>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Down, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(Instance.gameObject, "Forward").AddComponent<ButtonHeld>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Forward, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(Instance.gameObject, "Back").AddComponent<ButtonHeld>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Back, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);
            #endregion

            /// Register an information button for explaining the individual movement buttons.
            GameFinder.FindChild(Instance.gameObject, "Info").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                ShowNotification.Info("Movement Buttons",
                    "Up-Button moves on Y-axis positiv." +
                    "\nDown-Button moves on Y-axis negativ." +
                    "\nLeft-Button moves on X-axis negativ." +
                    "\nRight-Button moves on X-axispositiv." +
                    "\nForward-Button moves on Z-axis positiv." +
                    "\nBack-Button moves on Z-axis negativ.");
            });
        }

        /// <summary>
        /// If <see cref="isFinish"/> is true, the <paramref name="finish"/> will be the state. Otherwise it will be false.
        /// </summary>
        /// <param name="finish">The finish state.</param>
        /// <returns><see cref="isFinish"/>.</returns>
        public static bool TryGetFinish(out bool finish)
        {
            if (isFinish)
            {
                finish = isFinish;
                isFinish = false;
                return true;
            }

            finish = false;
            return false;
        }

        /// <summary>
        /// Gets the move speed.
        /// </summary>
        /// <returns>The selected move speed.</returns>
        public static float GetSpeed()
        {
            return GameFinder.FindChild(Instance.gameObject, "SpeedSwitch")
                .GetComponent<SwitchManager>().isOn ? ValueHolder.MoveFast : ValueHolder.Move;
        }
    }
}
