using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the rotation menu for drawable type objects.
    /// </summary>
    public static class RotationMenu
    {
        /// <summary>
        /// The prefab of the rotation menu.
        /// </summary>
        private const string rotationMenuPrefab = "Prefabs/UI/Drawable/Rotate";
        /// <summary>
        /// The instance of the rotation menu
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// The status if the children should included in the rotation.
        /// </summary>
        public static bool includeChildren = false;

        /// <summary>
        /// Creates the rotation menu for drawable type objects.
        /// </summary>
        /// <param name="selectedObject">The chosen drawable type object to rotate.</param>
        public static void Enable(GameObject selectedObject)
        {
            if (instance == null)
            {
                instance = PrefabInstantiator.InstantiatePrefab(rotationMenuPrefab,
                                        GameObject.Find("UI Canvas").transform, false);
                RotationSliderController slider = instance.GetComponentInChildren<RotationSliderController>();
                SliderListener(slider, selectedObject);
                ControlChildren(selectedObject);
            } else
            {
                RotationSliderController slider = instance.GetComponentInChildren<RotationSliderController>();
                slider.AssignValue(selectedObject.transform.localEulerAngles.z);
            }
        }

        /// <summary>
        /// Manages the level of children, 
        /// whether they should be rotated with or without children. 
        /// If a change in the property occurs while rotation is already in progress, 
        /// an attempt is made to reset it accordingly.
        /// </summary>
        /// <param name="selectedObject">The selected object for the rotation.</param>
        private static void ControlChildren(GameObject selectedObject)
        {
            if (selectedObject.CompareTag(Tags.MindMapNode))
            {
                GameObject drawable = GameFinder.GetDrawable(selectedObject);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);
                Vector3 oldRotation = selectedObject.transform.localEulerAngles;
                MMNodeValueHolder valueHolder = selectedObject.GetComponent<MMNodeValueHolder>();
                Dictionary<GameObject, (Vector3, Vector3)> oldRotations = new();
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    oldRotations[pair.Key] = (pair.Key.transform.localPosition, pair.Key.transform.localEulerAngles);
                }

                GameFinder.FindChild(instance, "Content").transform.Find("Children").gameObject.SetActive(true);
                SwitchManager childrenSwitch = GameFinder.FindChild(instance, "ChildrenSwitch").GetComponent<SwitchManager>();
                bool changeSwitch = false;
                childrenSwitch.OnEvents.RemoveAllListeners();
                childrenSwitch.OnEvents.AddListener(() => 
                {
                    includeChildren = true;
                    Vector3 newRotation = selectedObject.transform.localEulerAngles;
                    GameMoveRotator.SetRotate(selectedObject, oldRotation.z, false);
                    new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, oldRotation.z, false).Execute();
                    GameMoveRotator.SetRotate(selectedObject, newRotation.z, true);
                    new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, newRotation.z, true).Execute();
                    changeSwitch = true;
                });
                childrenSwitch.OffEvents.RemoveAllListeners();
                childrenSwitch.OffEvents.AddListener(() =>
                {
                    includeChildren = false;
                    if (changeSwitch)
                    {
                        foreach(KeyValuePair<GameObject, (Vector3, Vector3)> pair in oldRotations)
                        {
                            (Vector3 pos, Vector3 rot) = pair.Value;
                            GameMoveRotator.SetPosition(pair.Key, pos, false);
                            new MoveNetAction(drawable.name, drawableParentName, pair.Key.name, pos, false).Execute();
                            GameMoveRotator.SetRotate(pair.Key, rot.z, false);
                            new RotatorNetAction(drawable.name, drawableParentName, pair.Key.name, rot.z, false).Execute();
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
        /// Destroys the menu.
        /// </summary>
        public static void Disable()
        {
            Destroyer.Destroy(instance);
        }

        /// <summary>
        /// Adds the AddListener for the Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="selectedObject">The selected object to rotate</param>
        private static void SliderListener(RotationSliderController slider, GameObject selectedObject)
        {
            GameObject drawable = GameFinder.GetDrawable(selectedObject);
            string drawableParentName = GameFinder.GetDrawableParentName(drawable);
            Transform transform = selectedObject.transform;

            slider.AssignValue(transform.localEulerAngles.z);
            slider.onValueChanged.AddListener(degree =>
            {
                float degreeToMove = 0;
                Vector3 currentDirection = Vector3.forward;
                bool unequal = false;
                if (transform.localEulerAngles.z > degree)
                {
                    degreeToMove = transform.localEulerAngles.z - degree;
                    currentDirection = Vector3.back;
                    unequal = true;
                }
                else if (transform.localEulerAngles.z < degree)
                {
                    degreeToMove = degree - transform.localEulerAngles.z;
                    currentDirection = Vector3.forward;
                    unequal = true;
                }
                if (unequal)
                {
                    GameMoveRotator.RotateObject(selectedObject, currentDirection, degreeToMove, includeChildren);
                    new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, currentDirection, degreeToMove, includeChildren).Execute(); ;
                }
            });
        }
    }
}