using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
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
        /// Creates the move menu and register the necressary Handler.
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
                GameObject drawable = GameFinder.FindDrawable(selectedObject);
                string drawableName = drawable.name;
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                GameFinder.FindChild(instance, "Left").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.LeftArrow, speedUpManager.isOn);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition).Execute();
                }), true);

                GameFinder.FindChild(instance, "Right").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.RightArrow, speedUpManager.isOn);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition).Execute();
                }), true);

                GameFinder.FindChild(instance, "Up").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.UpArrow, speedUpManager.isOn);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition).Execute();
                }), true);

                GameFinder.FindChild(instance, "Down").AddComponent<ButtonHolded>().SetAction((() =>
                {
                    moveByMouseManager.isOn = false;
                    moveByMouseManager.UpdateUI();
                    newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, KeyCode.DownArrow, speedUpManager.isOn);
                    new MoveNetAction(drawableName, drawableParent, selectedObject.name, newObjectPosition).Execute();
                }), true);
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