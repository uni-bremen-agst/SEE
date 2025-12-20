using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides the rotation menu for drawable type objects.
    /// </summary>
    public class RotationMenu : SingletonMenu
    {
        /// <summary>
        /// The prefab of the rotation menu.
        /// </summary>
        private const string rotationMenuPrefab = "Prefabs/UI/Drawable/Rotate";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private RotationMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static RotationMenu Instance { get; private set; }

        static RotationMenu()
        {
            Instance = new RotationMenu();
        }

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
            if (Instance.gameObject == null)
            {
                Instance.Instantiate(rotationMenuPrefab);
                RotationSliderController slider = Instance.gameObject.GetComponentInChildren<RotationSliderController>();
                /// Adds the necessary handler to the slider.
                SliderListener(slider, selectedObject);
                /// For Mind Map Nodes: Provides functionality to also rotate the children.
                ControlChildren(selectedObject);
            }
            else
            {
                /// Updates the slider value.
                RotationSliderController slider = Instance.gameObject.GetComponentInChildren<RotationSliderController>();
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
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// The old rotation of the object.
                /// Is needed to include the children later on.
                Vector3 oldRotation = selectedObject.transform.localEulerAngles;

                MMNodeValueHolder valueHolder = selectedObject.GetComponent<MMNodeValueHolder>();

                /// Save the original positions and rotation of the children,
                /// needed in case the inclusion of children is turned off midway.
                Dictionary<GameObject, (Vector3, Vector3)> oldRotations = new();
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    oldRotations[pair.Key] = (pair.Key.transform.localPosition, pair.Key.transform.localEulerAngles);
                }

                /// Initializes the switch to turn child inclusion on and off.
                GameFinder.FindChild(Instance.gameObject, "Content").transform.Find("Children").gameObject.SetActive(true);
                SwitchManager childrenSwitch = GameFinder.FindChild(Instance.gameObject, "ChildrenSwitch")
                    .GetComponent<SwitchManager>();
                bool changeSwitch = false;
                childrenSwitch.OnEvents.RemoveAllListeners();
                childrenSwitch.OnEvents.AddListener(() =>
                {
                    /// Rotates the children to the current roation of the parent node.
                    /// The parent node is first returned to its original rotation before
                    /// being roate with the children to the new degree.
                    /// This preserves the node arrangement.
                    includeChildren = true;
                    Vector3 newRotation = selectedObject.transform.localEulerAngles;
                    GameMoveRotator.SetRotate(selectedObject, oldRotation.z, false);
                    new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name, oldRotation.z,
                        false).Execute();
                    GameMoveRotator.SetRotate(selectedObject, newRotation.z, true);
                    new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name, newRotation.z,
                        true).Execute();
                    changeSwitch = true;
                });
                childrenSwitch.OffEvents.RemoveAllListeners();
                childrenSwitch.OffEvents.AddListener(() =>
                {
                    includeChildren = false;
                    if (changeSwitch)
                    {
                        /// Restores the original rotation and positon of the child nodes.
                        foreach (KeyValuePair<GameObject, (Vector3, Vector3)> pair in oldRotations)
                        {
                            (Vector3 pos, Vector3 rot) = pair.Value;
                            GameMoveRotator.SetPosition(pair.Key, pos, false);
                            new MoveNetAction(surface.name, surfaceParentName, pair.Key.name, pos,
                                false).Execute();
                            GameMoveRotator.SetRotate(pair.Key, rot.z, false);
                            new RotatorNetAction(surface.name, surfaceParentName, pair.Key.name, rot.z,
                                false).Execute();
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
        /// Adds the AddListener for the Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="selectedObject">The selected object to rotate.</param>
        private static void SliderListener(RotationSliderController slider, GameObject selectedObject)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            Transform transform = selectedObject.transform;

            /// Assigns the current degree to the slider.
            slider.AssignValue(transform.localEulerAngles.z);

            /// Adds the handler to rotate to the slider.
            slider.OnValueChanged.AddListener(degree =>
            {
                float degreeToMove = 0;
                Vector3 currentDirection = Vector3.forward;
                bool unequal = false;

                /// Calculates the degree and direction for the rotation.
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

                /// Executes the rotation, if there are changes.
                if (unequal)
                {
                    GameMoveRotator.RotateObject(selectedObject, currentDirection, degreeToMove, includeChildren);
                    new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name, currentDirection,
                        degreeToMove, includeChildren).Execute();
                }
            });
        }
    }
}