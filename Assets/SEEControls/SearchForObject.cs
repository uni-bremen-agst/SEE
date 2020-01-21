using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchForObject : MonoBehaviour
{
    /// <summary>
    /// The panel where the found objects are going to be displayed.
    /// Has to be assigned in the editor.
    /// </summary>
    public GameObject panel;

    /// <summary>
    /// A prefab of the button the content is going to be shown on.
    /// Has to be assigned in the editor.
    /// </summary>
    public GameObject button;

    /// <summary>
    /// The onscreen keyboard which puts its text into the attached inputfield.
    /// Has to assigned in the editor.
    /// </summary>
    public GameObject keyboard;

    /// <summary>
    /// A panel that can be opened after clicking an option in the list.
    /// Can be assigned in editor but is not necessary.
    /// </summary>
    public GameObject informationPanel;

    /// <summary>
    /// A list of game objects which where recently selected.
    /// </summary>
    private List<GameObject> RecentFinds;

    /// <summary>
    /// The input field this script is attached to.
    /// </summary>
    private InputField TextInput;


    void Start()
    {
        RecentFinds = new List<GameObject> { };
        TextInput = gameObject.GetComponent<InputField>();
        CloseKeyboard();
        transform.parent.GetComponent<ChangePanel>().OnPanelClose.AddListener(CloseKeyboard);
    }

    private void Update()
    {
        if (TextInput.isFocused)
        {
            keyboard.SetActive(true);
        }
    }

    /// <summary>
    /// Filters all existing game objects with tag "Building" and returns those with the given infix.
    /// </summary>
    /// <param name="name">the infix to look for</param>
    /// <returns>a list of all GameObjects in the game with the given infix</returns>
    private List<GameObject> LookForInfix(string name)
    {
        List<GameObject> finds = new List<GameObject>();
        var allObjects = GameObject.FindGameObjectsWithTag("Building");

        foreach( GameObject obj in allObjects)
        {
            if(obj.name.Contains(name))
            {
                finds.Add(obj);
            }
        }
        return finds;
    }

    /// <summary>
    /// Adds the given GameObject to the list of recently visited GameObjects.
    /// Order: most recently used
    /// </summary>
    /// <param name="find">the GameObject to add to the list</param>
    private void AddToRecentFinds(GameObject find)
    {
        if(RecentFinds.Contains(find))
        {
            RecentFinds.Remove(find);
        }

        RecentFinds.Insert(0, find);
    }

    public List<GameObject> GetRecentFinds()
    {
        return RecentFinds;
    }

    /// <summary>
    /// Is called when the attached input field is changed.
    /// Updates the list on the attached panel.
    /// </summary>
    public void OnInput()
    {
        ClearObject(panel);
        string name = TextInput.text;

        List<GameObject> objs = LookForInfix(name);

        if(name.Length > 0)
        {
            ShowOnPanel(objs);
        }
        else
        {
            ShowOnPanel(RecentFinds);
        }
    }

    /// <summary>
    /// This function can be called to process the string inside the attached text field.
    /// </summary>
    /// <param name="name">the name of the object</param>
    public void OnPressEnter(string name)
    {
        Debug.Log("pressed enter");
    }

    /// <summary>
    /// Calls the control script on the main camera to move the rig towards the specified object.
    /// Also clears the panel and displays the list of recently found Objects.
    /// If the variable "Information Panel" is specified it will be opened.
    /// </summary>
    /// <param name="obj">the GameObject to focus</param>
    public void OnEnter(GameObject obj)
    {
        AddToRecentFinds(obj);
        Camera.main.GetComponent<TouchControlsSEE>().SetTarget(obj.transform);
        ClearObject(panel);
        ShowOnPanel(RecentFinds);

        if(informationPanel != null)
        {
            transform.parent.GetComponent<ChangePanel>().OpenOtherPanel(informationPanel);
        }
    }

    /// <summary>
    /// Creates a button for each element in the given list and attaches it to the szezified panel.
    /// Each button calls the OnEnter method on the onClick event with the GameObject it is representing.
    /// </summary>
    /// <param name="objs">the list of GameObjects to show on the specified panel</param>
    private void ShowOnPanel(List<GameObject> objs)
    {
        foreach (GameObject obj in objs)
        {
            GameObject newObj = Instantiate(button);
            newObj.name = obj.name;

            Text oldText = newObj.GetComponentInChildren<Text>();
            oldText.text = obj.name;

            newObj.GetComponentInChildren<Button>().onClick.AddListener(delegate { OnEnter(obj); });

            newObj.transform.SetParent(panel.transform);
        }
    }

    /// <summary>
    /// Destroys all child objects of the given GameObject.
    /// </summary>
    /// <param name="parent">the GameObject to clear</param>
    private void ClearObject(GameObject parent)
    {
        foreach(Transform child in parent.transform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Closes the attached on screen keyboard.
    /// </summary>
    private void CloseKeyboard()
    {
        keyboard.SetActive(false);
    }
}

