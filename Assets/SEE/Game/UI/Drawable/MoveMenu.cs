using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the move menu for drawable type objects.
    /// </summary>
    public static class MoveMenu
    {
        /// <summary>
        /// The prefab of the rotation menu.
        /// </summary>
        private const string moveMenuPrefab = "Prefabs/UI/Drawable/Move";
        /// <summary>
        /// The instance of the move menu.
        /// </summary>
        private static GameObject instance;

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
            if (instance == null)
            {
                instance = PrefabInstantiator.InstantiatePrefab(moveMenuPrefab,
                                GameObject.Find("UI Canvas").transform, false);

                speedUpManager = GameFinder.FindChild(instance, "SpeedSwitch").GetComponent<SwitchManager>();
                moveByMouseManager = GameFinder.FindChild(instance, "MoveSwitch").GetComponent<SwitchManager>();

                Vector3 newObjectPosition;
                GameObject drawable = GameFinder.GetDrawable(selectedObject);
                string drawableName = drawable.name;
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                GameFinder.FindChild(instance, "Left").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.LeftArrow, speedUpManager.isOn, includeChildren);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition, includeChildren).Execute();
                }), true);

                GameFinder.FindChild(instance, "Right").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.RightArrow, speedUpManager.isOn, includeChildren);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition, includeChildren).Execute();
                }), true);

                GameFinder.FindChild(instance, "Up").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.UpArrow, speedUpManager.isOn, includeChildren);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition, includeChildren).Execute();
                }), true);

                GameFinder.FindChild(instance, "Down").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.DownArrow, speedUpManager.isOn, includeChildren);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition, includeChildren).Execute();
                }), true);

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
                GameObject drawable = GameFinder.GetDrawable(selectedObject);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);
                Vector3 oldPosition = selectedObject.transform.localPosition;
                MMNodeValueHolder valueHolder = selectedObject.GetComponent<MMNodeValueHolder>();
                Dictionary<GameObject, Vector3> oldPositions = new();
                foreach(KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    oldPositions[pair.Key] = pair.Key.transform.localPosition;
                }

                GameFinder.FindChild(instance, "Content").transform.Find("Children").gameObject.SetActive(true);
                SwitchManager childrenSwitch = GameFinder.FindChild(instance, "ChildrenSwitch").GetComponent<SwitchManager>();
                bool changeSwitch = false;
                childrenSwitch.OnEvents.RemoveAllListeners();
                childrenSwitch.OnEvents.AddListener(() =>
                {
                    includeChildren = true;

                    if (valueHolder.GetChildren().Count > 0)
                    {
                        Vector3 newPosition = selectedObject.transform.localPosition;
                        GameMoveRotator.SetPosition(selectedObject, oldPosition, false);
                        new MoveNetAction(drawable.name, drawableParentName, selectedObject.name, oldPosition, false).Execute();
                        GameMoveRotator.SetPosition(selectedObject, newPosition, true);
                        new MoveNetAction(drawable.name, drawableParentName, selectedObject.name, newPosition, true).Execute();
                    }
                    
                    changeSwitch = true;
                });
                childrenSwitch.OffEvents.RemoveAllListeners();
                childrenSwitch.OffEvents.AddListener(() =>
                {
                    includeChildren = false;
                    if (changeSwitch)
                    {
                        foreach(KeyValuePair<GameObject, Vector3> pair in oldPositions)
                        {
                            GameMoveRotator.SetPosition(pair.Key, pair.Value, false);
                            new MoveNetAction(drawable.name, drawableParentName, pair.Key.name, pair.Value, false).Execute();
                        }
                    }
                });
            }
            else
            {
                GameFinder.FindChild(instance, "Content").transform.Find("Children").gameObject.SetActive(false);
                includeChildren = false;
            }
        }

        /// <summary>
        /// Destroy's the menu.
        /// </summary>
        public static void Disable()
        {
            Destroyer.Destroy(instance);
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