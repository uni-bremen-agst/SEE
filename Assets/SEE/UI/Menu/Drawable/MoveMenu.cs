using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using System.Collections.Generic;
using UnityEngine;
using SEE.UI.Drawable;
using SEE.Game;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides the move menu for drawable type objects.
    /// </summary>
    public class MoveMenu : SingletonMenu
    {
        /// <summary>
        /// The prefab of the rotation menu.
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

        static MoveMenu()
        {
            Instance = new MoveMenu();
        }

        /// <summary>
        /// The instance for the switch manager of the speed up option.
        /// </summary>
        private static SwitchManager speedUpManager;

        /// <summary>
        /// The instance for the switch manager of the move by mouse option.
        /// </summary>
        private static SwitchManager moveByMouseManager;

        /// <summary>
        /// The status if the children should included in the movement.
        /// </summary>
        public static bool includeChildren;

        /// <summary>
        /// Creates the move menu and register the necessary Handler.
        /// </summary>
        /// <param name="selectedObject">the chosen drawable type object to move</param>
        public static void Enable(GameObject selectedObject)
        {
            if (Instance.gameObject == null)
            {
                /// Instantiate the menu.
                Instance.Instantiate(moveMenuPrefab);

                /// Initialize the switches for speed and move by mouse.
                speedUpManager = GameFinder.FindChild(Instance.gameObject, "SpeedSwitch").GetComponent<SwitchManager>();
                moveByMouseManager = GameFinder.FindChild(Instance.gameObject, "MoveSwitch").GetComponent<SwitchManager>();

                /// The new position for the object.
                Vector3 newObjectPosition;

                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceName = surface.name;
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// Initialize the button for left moving.
                /// Enables the functionality to hold down the left mouse button.
                GameFinder.FindChild(Instance.gameObject, "Left").AddComponent<ButtonHeld>().SetAction(() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, ValueHolder.MoveDirection.Left,
                        speedUpManager.isOn, includeChildren);
                    new MoveNetAction(surfaceName, surfaceParentName, selectedObject.name, newObjectPosition,
                        includeChildren).Execute();
                }, true);

                /// Initialize the button for right moving.
                /// Enables the functionality to hold down the left mouse button.
                GameFinder.FindChild(Instance.gameObject, "Right").AddComponent<ButtonHeld>().SetAction(() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, ValueHolder.MoveDirection.Right,
                        speedUpManager.isOn, includeChildren);
                    new MoveNetAction(surfaceName, surfaceParentName, selectedObject.name, newObjectPosition,
                        includeChildren).Execute();
                }, true);

                /// Initialize the button for up moving.
                /// Enables the functionality to hold down the left mouse button.
                GameFinder.FindChild(Instance.gameObject, "Up").AddComponent<ButtonHeld>().SetAction(() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, ValueHolder.MoveDirection.Up,
                        speedUpManager.isOn, includeChildren);
                    new MoveNetAction(surfaceName, surfaceParentName, selectedObject.name, newObjectPosition,
                        includeChildren).Execute();
                }, true);

                /// Initialize the button for down moving.
                /// Enables the functionality to hold down the left mouse button.
                GameFinder.FindChild(Instance.gameObject, "Down").AddComponent<ButtonHeld>().SetAction(() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, ValueHolder.MoveDirection.Down,
                        speedUpManager.isOn, includeChildren);
                    new MoveNetAction(surfaceName, surfaceParentName, selectedObject.name, newObjectPosition,
                        includeChildren).Execute();
                }, true);

                /// For Mind Map Nodes: Provides functionality to also move the children.
                ControlChildren(selectedObject);
            }
        }

        /// <summary>
        /// Manages the level of children,
        /// whether they should be move with or without children.
        /// If a change in the property occurs while movement is already in progress,
        /// an attempt is made to reset it accordingly.
        /// </summary>
        /// <param name="selectedObject">The selected object for the rotation.</param>
        private static void ControlChildren(GameObject selectedObject)
        {
            if (selectedObject.CompareTag(Tags.MindMapNode))
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// The old position of the object.
                /// Is needed to include the children later on.
                Vector3 oldPosition = selectedObject.transform.localPosition;

                MMNodeValueHolder valueHolder = selectedObject.GetComponent<MMNodeValueHolder>();

                /// Save the original positions of the children,
                /// needed in case the inclusion of children is turned off midway.
                Dictionary<GameObject, Vector3> oldPositions = new();
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    oldPositions[pair.Key] = pair.Key.transform.localPosition;
                }

                /// Initializes the switch to turn child inclusion on and off.
                GameFinder.FindChild(Instance.gameObject, "Content").transform.Find("Children").gameObject.SetActive(true);
                SwitchManager childrenSwitch = GameFinder.FindChild(Instance.gameObject, "ChildrenSwitch").GetComponent<SwitchManager>();
                bool changeSwitch = false;
                childrenSwitch.OnEvents.RemoveAllListeners();
                childrenSwitch.OnEvents.AddListener(() =>
                {
                    includeChildren = true;

                    /// Moves the children to the current position of the parent node.
                    /// The parent node is first returned to its original position before
                    /// being moved with the children to the new point.
                    /// This preserves the node arrangement.
                    if (valueHolder.GetChildren().Count > 0)
                    {
                        Vector3 newPosition = selectedObject.transform.localPosition;
                        GameMoveRotator.SetPosition(selectedObject, oldPosition, false);
                        new MoveNetAction(surface.name, surfaceParentName, selectedObject.name, oldPosition, false).Execute();
                        GameMoveRotator.SetPosition(selectedObject, newPosition, true);
                        new MoveNetAction(surface.name, surfaceParentName, selectedObject.name, newPosition, true).Execute();
                    }

                    changeSwitch = true;
                });
                childrenSwitch.OffEvents.RemoveAllListeners();
                childrenSwitch.OffEvents.AddListener(() =>
                {
                    includeChildren = false;
                    if (changeSwitch)
                    {
                        /// Restores the original positions of the child nodes.
                        foreach (KeyValuePair<GameObject, Vector3> pair in oldPositions)
                        {
                            GameMoveRotator.SetPosition(pair.Key, pair.Value, false);
                            new MoveNetAction(surface.name, surfaceParentName, pair.Key.name, pair.Value, false).Execute();
                        }
                    }
                });
            }
            else
            {
                /// Disables the include children button, if the selected object is not a <see cref="MindMapNodeConf"/>
                GameFinder.FindChild(Instance.gameObject, "Content").transform.Find("Children").gameObject.SetActive(false);
                includeChildren = false;
            }
        }

        /// <summary>
        /// Gets the speed up switch manager.
        /// </summary>
        /// <returns>The switch manager of the speed up switch</returns>
        public static SwitchManager GetSpeedUpManager()
        {
            return speedUpManager;
        }

        /// <summary>
        /// Gets the move by mouse switch manager.
        /// </summary>
        /// <returns>The switch manager of the move by mouse switch</returns>
        public static SwitchManager GetMoveByMouseManager()
        {
            return moveByMouseManager;
        }
    }
}
