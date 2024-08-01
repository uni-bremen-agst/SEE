using SEE.Controls.Actions;
using SEE.GO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using SEE.Utils;

public class RadialSelection : MonoBehaviour
{
    int numberOfRadialPart = 0;
    public GameObject radialPartPrefab;
    public Transform radialPartCanvas;
    public float angleBetweenPart = 10;
    private List<GameObject> spawnedParts = new List<GameObject>();
    public Transform handTransform;
    private int currentSelectedRadialPart = -1;
    public UnityEvent<int> OnPartSelected;
    public InputActionReference radialMenu;
    List<string> actions = new();
    List<(string, List<string>)> subMenus = new List<(string, List<string>)>();
    List<string> menuEntrys = new();
    Dictionary<string, List<string>> menus;
    GameObject actionObject;
    Vector3? subMenuPosition;
    Quaternion? subMenuRotation;
    public static HideModeSelector HideMode { get; set; }
    // Start is called before the first frame update
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

    public static bool RadialMenuTrigger { get; set; }
    private void RadialMenu(InputAction.CallbackContext context)
    {
        Debug.Log("uuuuuuuuuuuuuuuuuu");
        RadialMenuTrigger = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (RadialMenuTrigger && !spawned)
        {
            SpawnRadialPart();
            RadialMenuTrigger = false;
        }
        if (spawned)
        {
            GetSelectedRadialPart();
        }
        if (RadialMenuTrigger && spawned)
        {
            spawned = false;
            RadialMenuTrigger = false;
            HideAndTriggerSelected();
        }
    }

    bool select;
    bool unselect;

    public void OnSelectEnter(SelectEnterEventArgs args)
    {
        Debug.Log("vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv");
        select = true;
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        unselect = true;
    }

    public void HideAndTriggerSelected()
    {
        radialPartCanvas.gameObject.SetActive(false);
        OnPartSelected.Invoke(currentSelectedRadialPart);
    }

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
                spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().color = Color.white;
                spawnedParts[i].gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().fontStyle = (FontStyles)FontStyle.Normal;
            }
        }
    }

    bool spawned;

    public void SelectAction(int i)
    {
        Debug.Log("cwazy");
        if (menuEntrys.Count != 0)
        {
            if (actions[i] == "Back")
            {
                actions.Clear();
                actions.AddRange(menuEntrys);
                numberOfRadialPart = actions.Count();
                RadialMenuTrigger = true;
                menuEntrys.Clear();
                subMenuPosition = radialPartCanvas.position;
                subMenuRotation = radialPartCanvas.rotation;
            }
            if (actions[i] == "HideAll")
            {
                Debug.Log("letsgo");
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
                Debug.Log("komisch" + actions[0] + numberOfRadialPart);
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
                    RadialMenuTrigger = true;
                    subMenuPosition = radialPartCanvas.position;
                    subMenuRotation = radialPartCanvas.rotation;
                }
                if (actions[i] == ActionStateTypes.Rotate.Name)
                {
                    if (actionObject != null)
                    {
                        Destroy(actionObject);
                    }
                    Debug.Log("jjjjjjjjjjjj");
                    actionObject = PrefabInstantiator.InstantiatePrefab("Prefabs/Dial").transform.gameObject;
                    Debug.Log("spawned" + actionObject.name);
                    Debug.Log("location" + actionObject.transform.position);
                    Debug.Log("handlocation" + handTransform.position);
                    actionObject.transform.position = handTransform.position;
                    actionObject.SetActive(true);
                }
                else if (actions[i] == ActionStateTypes.Draw.Name)
                {
                    if (actionObject != null)
                    {
                        Destroy(actionObject);
                    }
                    Debug.Log("jjjjjjjjjjjj");
                    actionObject = PrefabInstantiator.InstantiatePrefab("Prefabs/Pen").transform.gameObject;
                    Debug.Log("spawned" + actionObject.name);
                    Debug.Log("location" + actionObject.transform.position);
                    Debug.Log("handlocation" + handTransform.position);
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
                RadialMenuTrigger = true;
                subMenuPosition = radialPartCanvas.position;
                subMenuRotation = radialPartCanvas.rotation;
            }
        }
    }

    public void SpawnRadialPart()
    {
        Debug.Log("SoViele:" + numberOfRadialPart);
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
                spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<RectTransform>().rotation * Quaternion.Euler(0,0,180);
                spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            }
            spawnRadialPart.gameObject.transform.Find("TextField").gameObject.MustGetComponent<TextMeshProUGUI>().text = actions[i];
            Debug.Log("Action:" + actions[i]);
            spawnedParts.Add(spawnRadialPart);
        }
        spawned = true;
    }
}
