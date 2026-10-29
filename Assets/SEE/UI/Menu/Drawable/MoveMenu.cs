using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides the move menu for drawable type objects.
    /// </summary>
    public class MoveMenu : SingletonMenu
    {
        /// <summary>
        /// The prefab of the move menu.
        /// </summary>
        private const string moveMenuPrefab = "Prefabs/UI/Drawable/Move";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private MoveMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static MoveMenu Instance { get; private set; }

        /// <summary>
        /// The switch manager of the speed up option.
        /// </summary>
        private SwitchManager speedUpManager;

        /// <summary>
        /// The switch manager of the move by mouse option.
        /// </summary>
        private SwitchManager moveByMouseManager;

        /// <summary>
        /// Controls the Mind Map specific child inclusion behavior of this move menu.
        /// </summary>
        private MindMapMoveChildrenController mindMapMoveChildrenController;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static MoveMenu()
        {
            Instance = new MoveMenu();
        }

        /// <summary>
        /// Whether child nodes of the selected Mind Map node should be included
        /// in the current movement.
        /// </summary>
        public bool IncludeChildren => mindMapMoveChildrenController?.IncludeChildren ?? false;

        /// <summary>
        /// Creates the move menu and registers the necessary handlers.
        /// </summary>
        /// <param name="selectedObject">The chosen drawable type object to move.</param>
        public void Enable(GameObject selectedObject)
        {
            if (gameObject == null)
            {
                /// Instantiate the menu.
                Instantiate(moveMenuPrefab);

                /// Initialize the switches for speed and move by mouse.
                speedUpManager = GameFinder.FindAttachedOrLocalDescendant(
                    gameObject, "SpeedSwitch").GetComponent<SwitchManager>();
                moveByMouseManager = GameFinder.FindAttachedOrLocalDescendant(
                    gameObject, "MoveSwitch").GetComponent<SwitchManager>();

                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceName = surface.name;
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// Initialize the movement buttons.
                SetUpMoveButton(selectedObject, "Left", ValueHolder.MoveDirection.Left,
                    surfaceName, surfaceParentName);
                SetUpMoveButton(selectedObject, "Right", ValueHolder.MoveDirection.Right,
                    surfaceName, surfaceParentName);
                SetUpMoveButton(selectedObject, "Up", ValueHolder.MoveDirection.Up,
                    surfaceName, surfaceParentName);
                SetUpMoveButton(selectedObject, "Down", ValueHolder.MoveDirection.Down,
                    surfaceName, surfaceParentName);

                /// For Mind Map nodes, provide the option to include their children.
                mindMapMoveChildrenController =
                    new MindMapMoveChildrenController(gameObject, selectedObject);
                mindMapMoveChildrenController.SetUp();
            }
        }

        /// <summary>
        /// Sets up a movement button for the given direction.
        /// Holding the button repeatedly moves the selected object.
        /// </summary>
        /// <param name="selectedObject">The object that should be moved.</param>
        /// <param name="buttonName">The name of the movement button.</param>
        /// <param name="direction">The direction in which the object should be moved.</param>
        /// <param name="surfaceName">The name of the Drawable surface.</param>
        /// <param name="surfaceParentName">The name of the Drawable surface parent.</param>
        private void SetUpMoveButton(GameObject selectedObject, string buttonName,
            ValueHolder.MoveDirection direction, string surfaceName, string surfaceParentName)
        {
            GameFinder.FindAttachedOrLocalDescendant(gameObject, buttonName)
                .AddComponent<ButtonHeld>().SetAction(() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();

                    Vector3 newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(
                        selectedObject, direction, speedUpManager.isOn, IncludeChildren);

                    new MoveNetAction(surfaceName, surfaceParentName, selectedObject.name,
                        newObjectPosition, IncludeChildren).Execute();
                }, true);
        }

        /// <summary>
        /// Gets the speed up switch manager.
        /// </summary>
        /// <returns>The switch manager of the speed up switch.</returns>
        public SwitchManager GetSpeedUpManager()
        {
            return speedUpManager;
        }

        /// <summary>
        /// Gets the move by mouse switch manager.
        /// </summary>
        /// <returns>The switch manager of the move by mouse switch.</returns>
        public SwitchManager GetMoveByMouseManager()
        {
            return moveByMouseManager;
        }

        /// <summary>
        /// Destroys the move menu and resets its cached controls and
        /// Mind Map specific child inclusion state.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            speedUpManager = null;
            moveByMouseManager = null;
            mindMapMoveChildrenController = null;
        }
    }
}
