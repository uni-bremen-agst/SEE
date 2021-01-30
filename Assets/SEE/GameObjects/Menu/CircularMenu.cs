using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel;
using SEE.Utils;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO.Menu
{
    /// <summary>
    /// Implements the behaviour of the in-game circular menu.
    /// </summary>
    public class CircularMenu : MonoBehaviour
    {
        /// <summary>
        /// The distance between the menu and the camera when the menu is spawned.
        /// </summary>
        [Tooltip("The distance between the menu and the camera when the menu is spawned.")]
        [Range(0, 10)]
        public float CameraDistance = 1.0f;

        /// <summary>
        /// A buffer for hit objects for Physics2D.GetRayIntersectionNonAlloc()
        /// called in <see cref="SelectedMenuEntry(out int)"/>. The significance 
        /// of <see cref="SelectedMenuEntry(out int)"/> is that no memory is allocated 
        /// for the results and so garbage collection performance is improved when such
        /// calls are performed frequently. The colliders will be placed in this
        /// array in order of distance from the start of the ray.
        /// </summary>
        private RaycastHit2D[] raycastHit2s = new RaycastHit2D[10];

        /// <summary>
        /// Identifies the menu entry that was selected. Returns true
        /// if either the menu or one of its menu entries was selected.
        /// If that happened, <paramref name="entry"/> is the index
        /// of the selected item: 0 if the menu itself was selected
        /// or greater than 0 if a menu entry was selected. The first menu 
        /// entry has index 1. If the result of this function is false,
        /// <paramref name="entry"/> is undefined.
        /// </summary>
        /// <param name="entry">selected menu element or undefined</param>
        /// <returns>true if either the menu or one of its menu entries was
        /// selected</returns>
        private bool SelectedMenuEntry(out int entry)
        {
            entry = -1;
            if (Input.GetMouseButtonUp(0))
            {
                Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
                // The integer return value is the number of objects that 
                // intersect the ray (possibly zero) but the results array will 
                // not be resized if it doesn't contain enough elements to report 
                // all the results. 
                int numberOfHits = Physics2D.GetRayIntersectionNonAlloc(ray, raycastHit2s);
                // The colliders will be placed in the returned array in order of distance 
                // from the start of the ray.
                if (numberOfHits > 0)
                {
                    for (int i = 0; i < numberOfHits; i++)
                    {
                        RaycastHit2D hit = raycastHit2s[i];
                        if (hit.collider != null && hit.collider.CompareTag(Tags.UI))
                        {
                            entry = int.Parse(hit.collider.name);
                            // Preference will be given to a menu entry over the menu itself.
                            // The menu has index 0, menu entries have an index greater than 0.
                            if (entry > 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return entry != -1;
        }

        private void SelectMenuEntry(int selectedMenuEntry)
        {
            // hitEntry == 0 => the menu itself was selected
            if (selectedMenuEntry > 0)
            {
                // the index of menu entries starts at 1, while the first child of a game object has index 0
                selectedMenuEntry--;
                if (selectedMenuEntry < transform.childCount)
                {
                    if (transform.GetChild(selectedMenuEntry).TryGetComponent<MenuEntry>(out MenuEntry entry))
                    {
                       // Debug.Log(entry);
                        entry.Selected();
                    }
                    else
                    {
                        Debug.LogErrorFormat("Menu entry {0} has no MenuEntry component attached to it.\n",
                                              selectedMenuEntry + 1);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Invalid index of menu entry: {0}. Menu has only {1} children.\n",
                                         selectedMenuEntry + 1, transform.childCount);
                }

            }
            else if (selectedMenuEntry == 0 && TryGetComponent<MenuEntry>(out MenuEntry menuEntryOfMenu))
            {
                menuEntryOfMenu.Selected();
                Debug.Log(menuEntryOfMenu + "ELSEIF");
            }
        }

        /// <summary>
        /// If true (and only if), the menu is visible.
        /// </summary>
        private bool menuIsOn = false;

        public bool MenuIsOn { get => menuIsOn; set => menuIsOn = value; }


        /// <summary>
        /// Shows the menu.
        /// </summary>
        protected virtual void On()
        {
            gameObject.transform.position = MenuCenterPosition();
            SetVisible(gameObject, true);
        }

        /// <summary>
        /// Hides the menu.
        /// </summary>
        public virtual void Off()
        {
            SetVisible(gameObject, false);
        }

        /// <summary>
        /// Enables/disables the renderers of <paramref name="gameObject"/> and all its
        /// descendants so that they become visible/invisible.
        /// </summary>
        /// <param name="gameObject">objects whose renderer (and those of its children) is to be enabled/disabled</param>
        /// <param name="isVisible">iff true, the renderers will be enabled</param>
        private static void SetVisible(GameObject gameObject, bool isVisible)
        {
            gameObject.GetComponent<Renderer>().enabled = isVisible;
            foreach (Transform child in gameObject.transform)
            {
                SetVisible(child.gameObject, isVisible);
            }
        }

        /// <summary>
        /// Returns the center position in world space where the menu 
        /// should be located when it is spawned.
        /// </summary>
        /// <returns>center position of menu in world space</returns>
        private Vector3 MenuCenterPosition()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = Mathf.Max(MainCamera.Camera.nearClipPlane, CameraDistance);
                return MainCamera.Camera.ScreenToWorldPoint(mousePosition);
            }
            else
            {
                // FIXME
                throw new NotImplementedException("ShowMenu.MenuCenterPosition() implemented only for desktop environment.");
            }
        }

        private readonly ActionState.Type[] actionStateTypes = (ActionState.Type[])Enum.GetValues(typeof(ActionState.Type));

        private void Start()
        {
            ActionState.OnStateChanged += OnStateChanged;
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// </summary>
        private void Update()
        {
            // Select action state via numbers on the keyboard
            Assert.IsTrue(actionStateTypes.Length <= 9, "Only up to 9 (10 if zero is included) entries can be selected via the numbers on the keyboard!");
            for (int i = 0; i < actionStateTypes.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ActionState.Value = actionStateTypes[i];
                    break;
                }
            }

            bool oldState = menuIsOn;
            // space bar toggles menu            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MenuIsOn = !MenuIsOn;
            }
            if (MenuIsOn)
            {
                if (oldState != MenuIsOn)
                {
                    On();
                }

                // Menu should be facing the camera
                gameObject.transform.LookAt(MainCamera.Camera.transform);
                if (SelectedMenuEntry(out int hitEntry))
                {
                    SelectMenuEntry(hitEntry);
                }
            }
            else if (oldState != MenuIsOn)
            {
                Off();
            }
        }

        private void OnStateChanged(ActionState.Type value)
        {
            int selectedMenuEntry = (int)value + 1;
            SelectMenuEntry(selectedMenuEntry);
        }
    }
}