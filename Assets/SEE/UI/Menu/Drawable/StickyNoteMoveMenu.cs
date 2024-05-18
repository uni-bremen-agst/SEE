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
    public class StickyNoteMoveMenu
    {
        /// <summary>
        /// The prefab of the sticky note move menu.
        /// </summary>
        private const string moveMenuPrefab = "Prefabs/UI/Drawable/StickyNoteMove";

        /// <summary>
        /// The instance for the sticky note move menu.
        /// </summary>
        private static GameObject moveMenu;

        /// <summary>
        /// Whether this class has a finished rotation in store that wasn't yet fetched.
        /// </summary>
        private static bool isFinish;

        /// <summary>
        /// Method to disable the move menu
        /// </summary>
        public static void Disable()
        {
            if (moveMenu != null)
            {
                Destroyer.Destroy(moveMenu);
            }
        }

        /// <summary>
        /// Create and enables the sticky note move menu.
        /// It register the necessary Handler to the menu interface.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        public static void Enable(GameObject stickyNoteHolder, bool spawnMode = false)
        {
            moveMenu = PrefabInstantiator.InstantiatePrefab(moveMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            GameObject drawable = GameFinder.GetDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            /// Register the switch for speed up option.
            SwitchManager speedUpManager = GameFinder.FindChild(moveMenu, "SpeedSwitch")
                .GetComponent<SwitchManager>();
            float speed = ValueHolder.Move;
            /// Turn on increase the speed to <see cref="ValueHolder.MoveFast"/>.
            speedUpManager.OnEvents.AddListener(() => speed = ValueHolder.MoveFast);
            /// Turn off decrease the speed to <see cref="ValueHolder.Move"/>.
            speedUpManager.OffEvents.AddListener(() => speed = ValueHolder.Move);

            /// Register the finish button. It destroys the move menu and set the finish attribut.
            GameFinder.FindChild(moveMenu, "Finish").GetComponent<ButtonManagerBasic>()
                .clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(moveMenu);
                isFinish = true;
            });

            /// In this region the movement buttons will be register.
            /// It executes also the network action.
            /// They cannot be outsourced, as otherwise, 
            /// the speed switching would no longer work. 
            /// Therefore, they are grouped together in a region.
            #region Movement buttons
            GameFinder.FindChild(moveMenu, "Left").AddComponent<ButtonHolded>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Left, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(moveMenu, "Right").AddComponent<ButtonHolded>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Right, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(moveMenu, "Up").AddComponent<ButtonHolded>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Up, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(moveMenu, "Down").AddComponent<ButtonHolded>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Down, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(moveMenu, "Forward").AddComponent<ButtonHolded>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Forward, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);

            GameFinder.FindChild(moveMenu, "Back").AddComponent<ButtonHolded>().SetAction(() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Back, speed);
                if (!spawnMode)
                {
                    new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                        newPos, stickyNoteHolder.transform.eulerAngles).Execute();
                }
            }, true);
            #endregion

            /// Register an information button for explaining the individual movement buttons.
            GameFinder.FindChild(moveMenu, "Info").GetComponent<ButtonManagerBasic>()
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
        /// <param name="finish">The finish state</param>
        /// <returns><see cref="isFinish"/></returns>
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
        /// <returns>The selected move speed</returns>
        public static float GetSpeed()
        {
            return GameFinder.FindChild(moveMenu, "SpeedSwitch")
                .GetComponent<SwitchManager>().isOn ? ValueHolder.MoveFast : ValueHolder.Move;
        }

        /// <summary>
        /// Gets the is active state of the menu.
        /// </summary>
        /// <returns>true, if the menu is not null.</returns>
        public static bool IsActive()
        {
            return moveMenu != null;
        }
    }
}