using SEE.Controls.Actions;
using SEE.GO;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SEE.Utils;
using System;

namespace SEE.XR
{
    /// <summary>
    /// This component is used to open a radial menu in VR.
    /// This script is based on this tutorial: https://www.youtube.com/watch?v=n-xPN1v3dvA
    /// </summary>
    public class RadialSelection : MonoBehaviour
    {
        /// <summary>
        /// The number of radial-parts.
        /// </summary>
        private int numberOfRadialParts = 0;

        /// <summary>
        /// The prefab for the single radial-parts.
        /// </summary>
        public GameObject radialPartPrefab;

        /// <summary>
        /// The canvas on which the radial should be shown.
        /// </summary>
        public Transform radialPartCanvas;

        /// <summary>
        /// The angle between the radial-parts.
        /// </summary>
        public float angleBetweenPart = 10;

        /// <summary>
        /// The radial-parts which got spawned.
        /// </summary>
        private List<GameObject> spawnedParts = new();

        /// <summary>
        /// The transform of the controller.
        /// </summary>
        public Transform handTransform;

        /// <summary>
        /// The currently selected radial-part.
        /// </summary>
        private int currentSelectedRadialPart = -1;

        /// <summary>
        /// The selected radial-part.
        /// </summary>
        public UnityEvent<int> OnPartSelected;

        /// <summary>
        /// The button that triggers the radial-menu.
        /// </summary>
        public InputActionReference RadialMenuActionRef;

        /// <summary>
        /// All actions which are currently available.
        /// </summary>
        private List<string> actions = new();

        /// <summary>
        /// All submenus and their entries.
        /// </summary>
        private List<(string, List<string>)> subMenus = new();

        /// <summary>
        /// All entries of a submenu.
        /// This list is used to swap between the main-menu and sub-menus.
        /// The main menu contains all "main-actions" and the sub-menus more
        /// specific sub-actions. We save the main-actions here for the case
        /// that the user wants to go back to the main-menu.
        /// </summary>
        private List<string> menuEntries = new();

        /// <summary>
        /// This is used for the rotate-action, because in this case we
        /// have a dial, which is getting spawned, and should be despawned, when the action changes.
        /// </summary>
        private GameObject actionObject;

        /// <summary>
        /// The position of the submenu.
        /// It should be the same as the position from the mainmenu.
        /// </summary>
        private Vector3? subMenuPosition;

        /// <summary>
        /// The rotation of the submenu.
        /// It should be the same as the rotation from the mainmenu.
        /// </summary>
        private Quaternion? subMenuRotation;

        /// <summary>
        /// The selected HideMode.
        /// The hide-action is a special action, because it has sub-actions, which we need to access.
        /// </summary>
        public static HideModeSelector HideMode { get; set; }

        private void Awake()
        {
            RadialMenuActionRef.action.performed += RadialMenu;
            ActionStateTypes.AllRootTypes.PreorderTraverse(Visit);
            bool Visit(AbstractActionStateType child, AbstractActionStateType parent)
            {
                string name;

                if (child is ActionStateType actionStateType)
                {
                    name = child.Name;
                }
                else if (child is ActionStateTypeGroup actionStateTypeGroup)
                {
                    name = child.Name;
                }
                else
                {
                    throw new System.NotImplementedException($"{nameof(child)} not handled.");
                }

                // If child is not a root (i.e., has a parent), we will add the entry to
                // the InnerEntries of the menu corresponding to the parent.
                // We know that such a menu must exist, because we are doing a
                // preorder traversal.
                if (parent != null)
                {
                    if (parent is ActionStateTypeGroup parentGroup)
                    {
                        (string, List<string>) search = subMenus.FirstOrDefault(item => item.Item1 == parent.Name);
                        for (int i = 0; i < subMenus.Count; i++)
                        {
                            if (subMenus[i].Item1 == parent.Name && subMenus[i].Item2 == null)
                            {
                                subMenus[i] = (subMenus[i].Item1, new());
                                subMenus[i].Item2.Add(name);
                            }
                            else if (subMenus[i].Item1 == parent.Name && subMenus[i].Item2 != null)
                            {
                                subMenus[i].Item2.Add(name);
                            }
                        }
                    }
                    else
                    {
                        throw new System.InvalidCastException($"Parent is expected to be an {nameof(ActionStateTypeGroup)}.");
                    }
                }
                else
                {
                    subMenus.Add((name, null));
                    actions.Add(name);
                    numberOfRadialParts += 1;
                }
                // Continue with the traversal.
                return true;
            }
        }

        /// <summary>
        /// Whether the radial menu is open.
        /// </summary>
        private bool radialMenuTrigger;

        /// <summary>
        /// This method gets called when the button for the radial-menu is pressed.
        /// </summary>
        /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
        private void RadialMenu(InputAction.CallbackContext context)
        {
            radialMenuTrigger = true;
        }

        private void Update()
        {
            if (radialMenuTrigger && !spawned)
            {
                SpawnRadialPart();
                radialMenuTrigger = false;
            }
            if (spawned)
            {
                UpdateSelectedRadialPart();
            }
            if (radialMenuTrigger && spawned)
            {
                spawned = false;
                radialMenuTrigger = false;
                HideAndTriggerSelected();
            }
        }

        /// <summary>
        /// Whether the current action changed.
        /// It is used to trigger the update for the GlobalActionHistory.
        /// </summary>
        public static bool IndicatorChange { set; get; }

        /// <summary>
        /// This method activates the selected action and deactivates the radial menu.
        /// </summary>
        public void HideAndTriggerSelected()
        {
            radialPartCanvas.gameObject.SetActive(false);
            OnPartSelected.Invoke(currentSelectedRadialPart);
            IndicatorChange = true;
        }

        /// <summary>
        /// This method calculates at which radial-part the user is aiming at.
        /// The selected radial part is saved in currentSelectedRadialPart, so that
        /// if the user decides to activate this part, the matching action gets triggered.
        /// </summary>
        public void UpdateSelectedRadialPart()
        {
            Vector3 centerToHand = handTransform.position - radialPartCanvas.position;
            Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartCanvas.forward);

            float angle = Vector3.SignedAngle(radialPartCanvas.up, centerToHandProjected, -radialPartCanvas.forward);

            if (angle < 0)
            {
                angle += 360;
            }

            currentSelectedRadialPart = (int)angle * numberOfRadialParts / 360;

            for (int i = 0; i < spawnedParts.Count; i++)
            {
                if (i == currentSelectedRadialPart)
                {
                    spawnedParts[i].GetComponent<Image>().color = Color.yellow;
                    spawnedParts[i].transform.localScale = 1.1f * Vector3.one;
                    TextMeshProUGUI tmpGUI = spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>();
                    tmpGUI.color = Color.yellow;
                    tmpGUI.fontStyle = (FontStyles)FontStyle.Bold;
                }
                else
                {
                    spawnedParts[i].GetComponent<Image>().color = Color.white;
                    spawnedParts[i].transform.localScale = Vector3.one;
                    TextMeshProUGUI tmpGUI = spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>();
                    tmpGUI.color = Color.black;
                    tmpGUI.fontStyle = (FontStyles)FontStyle.Normal;
                }
            }
        }

        /// <summary>
        /// Is true if the radial menu is open.
        /// </summary>
        private bool spawned;

        /// <summary>
        /// This list represents all hideActions.
        /// </summary>
        private static List<string> hideActions = new()
        {
            "HideAll",
            "HideSelected",
            "HideUnselected",
            "HideOutgoing",
            "HideIncoming",
            "HideAllEdgesOfSelected",
            "HideForwardTransitiveClosure",
            "HideBackwardTransitiveClosure",
            "HideAllTransitiveClosure",
            "HighlightEdges",
            "Back"
        };


        /// <summary>
        /// This method activates the selected action, or opens a submenu/mainmenu.
        /// </summary>
        /// <param name="i">the current selected radialPart.</param>
        public void SelectAction(int i)
        {
            if (menuEntries.Count != 0)
            {
                if (actions[i] == "Back")
                {
                    actions.Clear();
                    actions.AddRange(menuEntries);
                    numberOfRadialParts = actions.Count();
                    radialMenuTrigger = true;
                    menuEntries.Clear();
                    subMenuPosition = radialPartCanvas.position;
                    subMenuRotation = radialPartCanvas.rotation;
                }
                else if (Enum.TryParse(actions[i], out HideModeSelector mode))
                {
                    HideMode = mode;
                    TriggerHideAction();
                }
                else
                {
                    GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == actions[i]));
                }
            }
            else
            {
                if (subMenus[i].Item2 == null)
                {
                    if (subMenus[i].Item1 == "Hide")
                    {
                        numberOfRadialParts = hideActions.Count();
                        menuEntries.AddRange(actions);
                        actions.Clear();
                        actions.AddRange(hideActions);
                        radialMenuTrigger = true;
                        subMenuPosition = radialPartCanvas.position;
                        subMenuRotation = radialPartCanvas.rotation;
                    }
                    if (actions[i] == ActionStateTypes.Rotate.Name)
                    {
                        if (actionObject != null)
                        {
                            Destroyer.Destroy(actionObject);
                        }
                        actionObject = PrefabInstantiator.InstantiatePrefab("Prefabs/Dial").transform.gameObject;
                        actionObject.transform.position = handTransform.position;
                        actionObject.SetActive(true);
                    }
                    else if (actionObject != null)
                    {
                        actionObject.SetActive(false);
                    }
                    GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == subMenus[i].Item1));
                }
                else
                {
                    numberOfRadialParts = subMenus[i].Item2.Count() + 1;
                    menuEntries.AddRange(actions);
                    actions.Clear();
                    actions.AddRange(subMenus[i].Item2);
                    actions.Add("Back");
                    radialMenuTrigger = true;
                    subMenuPosition = radialPartCanvas.position;
                    subMenuRotation = radialPartCanvas.rotation;
                }
            }
        }

        /// <summary>
        /// This triggers the HideAction.
        /// </summary>
        private void TriggerHideAction()
        {
            GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
        }

        /// <summary>
        /// This method spawns all radial-parts for the current menu.
        /// </summary>
        public void SpawnRadialPart()
        {
            radialPartCanvas.gameObject.SetActive(true);
            if (subMenuPosition != null && subMenuRotation != null)
            {
                radialPartCanvas.position = (Vector3)subMenuPosition;
                radialPartCanvas.rotation = (Quaternion)subMenuRotation;
                subMenuPosition = null;
                subMenuRotation = null;
            }
            else
            {
                radialPartCanvas.position = handTransform.position + new Vector3(0, 0, 0.2f);
                radialPartCanvas.rotation = handTransform.rotation;
            }


            foreach (GameObject item in spawnedParts)
            {
                Destroyer.Destroy(item);
            }

            spawnedParts.Clear();

            for (int i = 0; i < numberOfRadialParts; i++)
            {
                float angle = -i * 360 / numberOfRadialParts - angleBetweenPart / 2;
                Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

                GameObject spawnRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);
                spawnRadialPart.transform.position = radialPartCanvas.position;
                spawnRadialPart.transform.localEulerAngles = radialPartEulerAngle;

                spawnRadialPart.GetComponent<Image>().fillAmount = (1 / (float)numberOfRadialParts) - (angleBetweenPart / 360);
                TextMeshProUGUI tmpUGUI = spawnRadialPart.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>();
                if (i > (numberOfRadialParts / 2))
                {
                    RectTransform rectTransform = spawnRadialPart.transform.Find("TextField").gameObject.MustGetComponent<RectTransform>();
                    rectTransform.rotation *= Quaternion.Euler(0, 0, 180);
                    tmpUGUI.alignment = TextAlignmentOptions.Right;
                }
                tmpUGUI.text = actions[i];
                spawnedParts.Add(spawnRadialPart);
            }
            spawned = true;
        }
    }
}
