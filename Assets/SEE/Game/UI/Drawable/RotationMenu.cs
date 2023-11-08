using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
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
            } else
            {
                RotationSliderController slider = instance.GetComponentInChildren<RotationSliderController>();
                slider.AssignValue(selectedObject.transform.localEulerAngles.z);
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
            GameObject drawable = GameFinder.FindDrawable(selectedObject);
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
                    GameMoveRotator.RotateObject(selectedObject, currentDirection, degreeToMove);
                    new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, currentDirection, degreeToMove).Execute(); ;
                }
            });
        }
    }
}