using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SearchForObject : MonoBehaviour
{

    private List<GameObject> RecentFinds;
    public GameObject panel; //Content Object in Sroll View
    public GameObject button; //Button to be shown in list
    public GameObject keyboard;
    private InputField TextInput;


    void Start()
    {
        RecentFinds = new List<GameObject> { };
        TextInput = gameObject.GetComponent<InputField>();
        keyboard.SetActive(false);
    }

    private void Update()
    {
        if (TextInput.isFocused)
        {
            keyboard.SetActive(true);
        }
        else
        {
            //keyboard.SetActive(false);
        }
    }

    private List<GameObject> LookFor(string name)
    {
        List<GameObject> finds = new List<GameObject> { };
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

    private void AddToRecentFinds(GameObject find)
    {
        if(RecentFinds.Contains(find))
        {
            RecentFinds.Remove(find);
        }

        RecentFinds.Insert(0, find);
    }

    public GameObject[] GetRecentFinds()
    {
        return RecentFinds.ToArray();
    }

    public void OnInput()
    {
        ClearParent(panel);
        string name = TextInput.text;

        if (name.Length > 0)
        {
            List<GameObject> objs = LookFor(name);
            GameObject newObj;
            foreach (GameObject obj in objs)
            {
                newObj = (GameObject)Instantiate(button);
                newObj.name = obj.name;
                Text oldText = newObj.GetComponentInChildren<Text>();
                oldText.text = obj.name;
                newObj.transform.SetParent(panel.transform);
            }
        }
    }

    public void OnEnter()
    {
        Debug.Log("pressed enter");
    }

    private void ClearParent(GameObject parent)
    {
        List<GameObject> objs = new List<GameObject>();
        foreach(Transform child in parent.transform)
        {
            objs.Add(child.gameObject);
        }

        objs.ForEach(Destroy);
    }


    //notes
    /*
     * differentiate between Leaf and Note by tag (GameObject.tag)
     * 
     * looking for infix with String.Contains(string)
     * 
     * change unity Objects into tree structure
     */

    
}

