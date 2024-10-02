using SEE.Controls.Actions;
using SEE.GO;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using SEE.Utils;

/// <summary>
/// This class is used, to open a
/// radial menu in VR.
/// </summary>
public class RadialSelection : MonoBehaviour
{
    /// <summary>
    /// The number of radial-parts.
    /// </summary>
    int numberOfRadialPart = 0;
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
    private List<GameObject> spawnedParts = new List<GameObject>();
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
    /// The button, which triggers the radial-menu.
    /// </summary>
    public InputActionReference radialMenu;
    /// <summary>
    /// All actions, which are currently available.
    /// </summary>
    List<string> actions = new();
    /// <summary>
    /// Alle submenus and their entries.
    /// </summary>
    List<(string, List<string>)> subMenus = new List<(string, List<string>)>();
    /// <summary>
    /// All entry of a submenu.
    /// </summary>
    List<string> menuEntrys = new();
    /// <summary>
    /// This is used for the rotate-action, because in this case we
    /// have a dial, which is getting spawned, and should be despawned, when the action changes.
    /// </summary>
    GameObject actionObject;
    /// <summary>
    /// The position of the submenu.
    /// It should be the same, as the position from the mainmenu.
    /// </summary>
    Vector3? subMenuPosition;
    /// <summary>
    /// The rotation of the submenu.
    /// It should be the same, as the rotation from the mainmenu.
    /// </summary>
    Quaternion? subMenuRotation;
    /// <summary>
    /// The hide-action is a special action, because it has sub-actions, which we need to access.
    /// </summary>
    public static HideModeSelector HideMode { get; set; }
    // Awake is always called before any Start functions.
    private void Awake()
    {
        radialMenu.action.performed += RadialMenu;
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
            // the InnerEntries of the NestedMenuEntry corresponding to the parent.
            // We know that such a NestedMenuEntry must exist, because we are doing a
            // preorder traversal.
            if (parent != null)
            {
                if (parent is ActionStateTypeGroup parentGroup)
                {
                    var search = subMenus.FirstOrDefault(item => item.Item1 == parent.Name);
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
                numberOfRadialPart += 1;
            }
            // Continue with the traversal.
            return true;
        }
    }
    /// <summary>
    /// Is true, when the radial menu is open.
    /// </summary>
    bool radialMenuTrigger;
    /// <summary>
    /// This method gets called, when the button for the radial-menu is pressed.
    /// </summary>
    /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
    private void RadialMenu(InputAction.CallbackContext context)
    {
        radialMenuTrigger = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (radialMenuTrigger && !spawned)
        {
            SpawnRadialPart();
            radialMenuTrigger = false;
        }
        if (spawned)
        {
            GetSelectedRadialPart();
        }
        if (radialMenuTrigger && spawned)
        {
            spawned = false;
            radialMenuTrigger = false;
            HideAndTriggerSelected();
        }
    }
    /// <summary>
    /// Is true, when the current action changed.
    /// It is being used, to trigger the update for the GlobalActionHistory.
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
    /// This method calculates, at which radial-part the user is aiming at.
    /// </summary>
    public void GetSelectedRadialPart()
    {
        Vector3 centerToHand = handTransform.position - radialPartCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartCanvas.forward);

        float angle = Vector3.SignedAngle(radialPartCanvas.up, centerToHandProjected, -radialPartCanvas.forward);

        if (angle < 0)
        {
            angle += 360;
        }

        currentSelectedRadialPart = (int)angle * numberOfRadialPart / 360;

        for (int i = 0; i < spawnedParts.Count; i++)
        {
            if (i == currentSelectedRadialPart)
            {
                spawnedParts[i].GetComponent<Image>().color = Color.yellow;
                spawnedParts[i].transform.localScale = 1.1f * Vector3.one;
                spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().color = Color.yellow;
                spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().fontStyle = (FontStyles)FontStyle.Bold;
            }
            else
            {
                spawnedParts[i].GetComponent<Image>().color = Color.white;
                spawnedParts[i].transform.localScale = Vector3.one;
                spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().color = Color.black;
                spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().fontStyle = (FontStyles)FontStyle.Normal;
            }
        }
    }
    /// <summary>
    /// Is true if the radial menu is open or not.
    /// </summary>
    bool spawned;
    /// <summary>
    /// This method activates the selected action, or opens a submenu/mainmenu.
    /// </summary>
    /// <param name="i"></param>
    public void SelectAction(int i)
    {
        if (menuEntrys.Count != 0)
        {
            if (actions[i] == "Back")
            {
                actions.Clear();
                actions.AddRange(menuEntrys);
                numberOfRadialPart = actions.Count();
                radialMenuTrigger = true;
                menuEntrys.Clear();
                subMenuPosition = radialPartCanvas.position;
                subMenuRotation = radialPartCanvas.rotation;
            }
            if (actions[i] == "HideAll")
            {
                HideMode = HideModeSelector.HideAll;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
                return;
            }
            if (actions[i] == "HideSelected")
            {
                HideMode = HideModeSelector.HideSelected;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideUnselected")
            {
                HideMode = HideModeSelector.HideUnselected;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideOutgoing")
            {
                HideMode = HideModeSelector.HideOutgoing;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideIncoming")
            {
                HideMode = HideModeSelector.HideIncoming;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideAllEdgesOfSelected")
            {
                HideMode = HideModeSelector.HideAllEdgesOfSelected;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideForwardTransitiveClosure")
            {
                HideMode = HideModeSelector.HideForwardTransitiveClosure;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideBackwardTransitiveClosure")
            {
                HideMode = HideModeSelector.HideBackwardTransitiveClosure;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HideAllTransitiveClosure")
            {
                HideMode = HideModeSelector.HideAllTransitiveClosure;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
            }
            if (actions[i] == "HighlightEdges")
            {
                HideMode = HideModeSelector.HighlightEdges;
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == "Hide"));
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
                    numberOfRadialPart = 11;
                    menuEntrys.AddRange(actions);
                    actions.Clear();
                    actions.Add("HideAll");
                    actions.Add("HideSelected");
                    actions.Add("HideUnselected");
                    actions.Add("HideOutgoing");
                    actions.Add("HideIncoming");
                    actions.Add("HideAllEdgesOfSelected");
                    actions.Add("HideForwardTransitiveClosure");
                    actions.Add("HideBackwardTransitiveClosure");
                    actions.Add("HideAllTransitiveClosure");
                    actions.Add("HighlightEdges");
                    actions.Add("Back");
                    radialMenuTrigger = true;
                    subMenuPosition = radialPartCanvas.position;
                    subMenuRotation = radialPartCanvas.rotation;
                }
                if (actions[i] == ActionStateTypes.Rotate.Name)
                {
                    if (actionObject != null)
                    {
                        Destroy(actionObject);
                    }
                    actionObject = PrefabInstantiator.InstantiatePrefab("Prefabs/Dial").transform.gameObject;
                    actionObject.transform.position = handTransform.position;
                    actionObject.SetActive(true);
                }
                else if (actions[i] == ActionStateTypes.Draw.Name)
                {
                    if (actionObject != null)
                    {
                        Destroy(actionObject);
                    }
                    actionObject = PrefabInstantiator.InstantiatePrefab("Prefabs/Pen").transform.gameObject;
                    actionObject.transform.position = handTransform.position;
                    actionObject.SetActive(true);
                }
                else
                {
                    if (actionObject != null)
                    {
                        actionObject.SetActive(false);
                    }
                }
                GlobalActionHistory.Execute((ActionStateType)ActionStateTypes.AllRootTypes.AllElements().FirstOrDefault(a => a.Name == subMenus[i].Item1));
            }
            else
            {
                numberOfRadialPart = subMenus[i].Item2.Count() + 1;
                menuEntrys.AddRange(actions);
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
            Destroy(item);
        }

        spawnedParts.Clear();

        for (int i = 0; i < numberOfRadialPart; i++)
        {
            float angle = -i * 360 / numberOfRadialPart - angleBetweenPart / 2;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);

            GameObject spawnRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);
            spawnRadialPart.transform.position = radialPartCanvas.position;
            spawnRadialPart.transform.localEulerAngles = radialPartEulerAngle;

            spawnRadialPart.GetComponent<Image>().fillAmount = (1 / (float)numberOfRadialPart) - (angleBetweenPart / 360);
            if (i > (numberOfRadialPart / 2))
            {
                spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<RectTransform>().rotation = 
                    spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<RectTransform>().rotation * Quaternion.Euler(0, 0, 180);
                spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            }
            spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().text = actions[i];
            spawnedParts.Add(spawnRadialPart);
        }
        spawned = true;
    }
}
