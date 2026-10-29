using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
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

        /// <summary>
        /// Controls the Mind Map specific child inclusion behavior of this rotation menu.
        /// </summary>
        private MindMapRotationChildrenController mindMapRotationChildrenController;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static RotationMenu()
        {
            Instance = new RotationMenu();
        }

        /// <summary>
        /// Whether child nodes of the selected Mind Map node should be included
        /// in the current rotation.
        /// </summary>
        public bool IncludeChildren => mindMapRotationChildrenController?.IncludeChildren ?? false;

        /// <summary>
        /// Creates the rotation menu for drawable type objects.
        /// </summary>
        /// <param name="selectedObject">The chosen drawable type object to rotate.</param>
        public void Enable(GameObject selectedObject)
        {
            if (gameObject == null)
            {
                Instantiate(rotationMenuPrefab);

                RotationSliderController slider = gameObject.GetComponentInChildren<RotationSliderController>();

                /// Adds the necessary handler to the slider.
                SetUpSlider(slider, selectedObject);

                /// For Mind Map nodes, provide the option to include their children.
                mindMapRotationChildrenController =
                    new MindMapRotationChildrenController(gameObject, selectedObject);
                mindMapRotationChildrenController.SetUp();
            }
            else
            {
                /// Updates the slider value.
                RotationSliderController slider = gameObject.GetComponentInChildren<RotationSliderController>();
                slider.AssignValue(selectedObject.transform.localEulerAngles.z);
            }
        }

        /// <summary>
        /// Sets up the rotation slider for the selected object.
        /// </summary>
        /// <param name="slider">The rotation slider controller.</param>
        /// <param name="selectedObject">The selected object to rotate.</param>
        private void SetUpSlider(RotationSliderController slider, GameObject selectedObject)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            Transform transform = selectedObject.transform;

            /// Assigns the current degree to the slider.
            slider.AssignValue(transform.localEulerAngles.z);

            /// Adds the handler for rotating the selected object.
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

                /// Executes the rotation if there are changes.
                if (unequal)
                {
                    GameMoveRotator.RotateObject(
                        selectedObject, currentDirection, degreeToMove, IncludeChildren);

                    new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                        currentDirection, degreeToMove, IncludeChildren).Execute();
                }
            });
        }

        /// <summary>
        /// Destroys the rotation menu and resets its Mind Map specific child inclusion state.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            mindMapRotationChildrenController = null;
        }
    }
}
