using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the move menu for sticky notes.
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

        /// <summary>
        /// Whether this menu has a finished movement that has not yet been consumed.
        /// </summary>
        private bool isFinish;

        /// <summary>
        /// The switch controlling the movement speed.
        /// </summary>
        private SwitchManager speedUpManager;

        /// <summary>
        /// The currently selected movement speed.
        /// </summary>
        private float speed;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static StickyNoteMoveMenu()
        {
            Instance = new StickyNoteMoveMenu();
        }

        /// <summary>
        /// Creates and enables the sticky note move menu and registers the required handlers.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        /// <param name="spawnMode">
        /// Whether the menu is used while spawning a sticky note.
        /// Network actions are omitted in this mode.
        /// </param>
        public void Enable(GameObject stickyNoteHolder, bool spawnMode = false)
        {
            Instantiate(moveMenuPrefab);
            isFinish = false;

            GameObject surface = GameFinder.GetDrawableSurface(stickyNoteHolder);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Register the switch for the speed-up option.
            speedUpManager = GameFinder.FindAttachedOrLocalDescendant(
                gameObject, "SpeedSwitch").GetComponent<SwitchManager>();

            speed = speedUpManager.isOn ? ValueHolder.MoveFast : ValueHolder.Move;

            speedUpManager.OnEvents.RemoveAllListeners();
            speedUpManager.OffEvents.RemoveAllListeners();

            /// Turning the switch on increases the movement speed.
            speedUpManager.OnEvents.AddListener(() => speed = ValueHolder.MoveFast);

            /// Turning the switch off restores the normal movement speed.
            speedUpManager.OffEvents.AddListener(() => speed = ValueHolder.Move);

            /// Register the finish button.
            ButtonManagerBasic finishButton = GameFinder.FindAttachedOrLocalDescendant(
                gameObject, "Finish").GetComponent<ButtonManagerBasic>();

            finishButton.clickEvent.RemoveAllListeners();
            finishButton.clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(gameObject);
                isFinish = true;
            });

            /// Register the movement buttons.
            SetUpMovementButton(stickyNoteHolder, "Left", ValueHolder.MoveDirection.Left,
                surface, surfaceParentName, spawnMode);
            SetUpMovementButton(stickyNoteHolder, "Right", ValueHolder.MoveDirection.Right,
                surface, surfaceParentName, spawnMode);
            SetUpMovementButton(stickyNoteHolder, "Up", ValueHolder.MoveDirection.Up,
                surface, surfaceParentName, spawnMode);
            SetUpMovementButton(stickyNoteHolder, "Down", ValueHolder.MoveDirection.Down,
                surface, surfaceParentName, spawnMode);
            SetUpMovementButton(stickyNoteHolder, "Forward", ValueHolder.MoveDirection.Forward,
                surface, surfaceParentName, spawnMode);
            SetUpMovementButton(stickyNoteHolder, "Back", ValueHolder.MoveDirection.Back,
                surface, surfaceParentName, spawnMode);

            /// Register an information button explaining the movement controls.
            SetUpInfoButton();
        }

        /// <summary>
        /// Registers a movement button for the given direction.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        /// <param name="buttonName">The name of the movement button.</param>
        /// <param name="direction">The movement direction assigned to the button.</param>
        /// <param name="surface">The drawable surface of the sticky note.</param>
        /// <param name="surfaceParentName">The ID of the drawable surface parent.</param>
        /// <param name="spawnMode">
        /// Whether network actions should be omitted because the sticky note is still being spawned.
        /// </param>
        private void SetUpMovementButton(GameObject stickyNoteHolder, string buttonName,
            ValueHolder.MoveDirection direction, GameObject surface, string surfaceParentName,
            bool spawnMode)
        {
            GameFinder.FindAttachedOrLocalDescendant(gameObject, buttonName)
                .AddComponent<ButtonHeld>().SetAction(() =>
                {
                    Vector3 newPosition = GameStickyNoteManager.MoveByMenu(
                        stickyNoteHolder, direction, speed);

                    if (!spawnMode)
                    {
                        new StickyNoteMoveNetAction(surface.name, surfaceParentName,
                            newPosition, stickyNoteHolder.transform.eulerAngles).Execute();
                    }
                }, true);
        }

        /// <summary>
        /// Registers the information button describing the available movement controls.
        /// </summary>
        private void SetUpInfoButton()
        {
            ButtonManagerBasic infoButton = GameFinder.FindAttachedOrLocalDescendant(
                gameObject, "Info").GetComponent<ButtonManagerBasic>();

            infoButton.clickEvent.RemoveAllListeners();
            infoButton.clickEvent.AddListener(() =>
            {
                ShowNotification.Info("Movement Buttons",
                    "Up-Button moves on Y-axis positiv."
                    + "\nDown-Button moves on Y-axis negativ."
                    + "\nLeft-Button moves on X-axis negativ."
                    + "\nRight-Button moves on X-axis positiv."
                    + "\nForward-Button moves on Z-axis positiv."
                    + "\nBack-Button moves on Z-axis negativ.");
            });
        }

        /// <summary>
        /// Tries to consume a previously completed movement.
        /// </summary>
        /// <param name="finish">Whether a completed movement was available.</param>
        /// <returns>True if a completed movement was available; otherwise, false.</returns>
        public bool TryGetFinish(out bool finish)
        {
            if (isFinish)
            {
                finish = true;
                isFinish = false;
                return true;
            }

            finish = false;
            return false;
        }

        /// <summary>
        /// Gets the currently selected movement speed.
        /// </summary>
        /// <returns>The selected movement speed.</returns>
        public float GetSpeed()
        {
            return speed;
        }

        /// <summary>
        /// Destroys the sticky note move menu and resets its transient state.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();

            isFinish = false;
            speedUpManager = null;
            speed = ValueHolder.Move;
        }
    }
}
