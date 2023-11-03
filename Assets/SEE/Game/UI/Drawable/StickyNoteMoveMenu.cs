using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
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
        /// It register the necressary Handler to the menu interface.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        public static void Enable(GameObject stickyNoteHolder)
        {
            moveMenu = PrefabInstantiator.InstantiatePrefab(moveMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            GameObject drawable = GameFinder.FindDrawable(stickyNoteHolder);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);

            /// Register the switch for speed up option.
            SwitchManager speedUpManager = GameFinder.FindChild(moveMenu, "SpeedSwitch").GetComponent<SwitchManager>();
            float speed = 0.001f;
            speedUpManager.OnEvents.AddListener(() => speed = 0.01f);
            speedUpManager.OffEvents.AddListener(() => speed = 0.001f);

            /// Register the finish button. It destroys the move menu and set the finish attribut.
            GameFinder.FindChild(moveMenu, "Finish").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(moveMenu);
                StickyNoteAction.finish = true;
            });

            /// In this region the movement buttons will be register.
            /// It executes also the network action.
            #region movement buttons
            GameFinder.FindChild(moveMenu, "Left").AddComponent<ButtonHolded>().SetAction((() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Left, speed);
                new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                    newPos, stickyNoteHolder.transform.eulerAngles).Execute();
            }), true);

            GameFinder.FindChild(moveMenu, "Right").AddComponent<ButtonHolded>().SetAction((() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Right, speed);
                new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                    newPos, stickyNoteHolder.transform.eulerAngles).Execute();
            }), true);

            GameFinder.FindChild(moveMenu, "Up").AddComponent<ButtonHolded>().SetAction((() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Up, speed);
                new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                    newPos, stickyNoteHolder.transform.eulerAngles).Execute();
            }), true);

            GameFinder.FindChild(moveMenu, "Down").AddComponent<ButtonHolded>().SetAction((() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Down, speed);
                new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                    newPos, stickyNoteHolder.transform.eulerAngles).Execute();
            }), true);

            GameFinder.FindChild(moveMenu, "Forward").AddComponent<ButtonHolded>().SetAction((() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Forward, speed);
                new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                    newPos, stickyNoteHolder.transform.eulerAngles).Execute();
            }), true);

            GameFinder.FindChild(moveMenu, "Back").AddComponent<ButtonHolded>().SetAction((() =>
            {
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(stickyNoteHolder, ValueHolder.MoveDirection.Back, speed);
                new StickyNoteMoveNetAction(drawable.name, drawableParentID,
                    newPos, stickyNoteHolder.transform.eulerAngles).Execute();
            }), true);
            #endregion

            /// Register an information button for explaining the individual movement buttons.
            GameFinder.FindChild(moveMenu, "Info").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
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
    }
}