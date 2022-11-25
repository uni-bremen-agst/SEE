using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabButtonSwitcher : MonoBehaviour
{
    public GameObject Tab;

    private string debug = "";
    private void OnGUI()
    {
        GUI.TextField(new Rect(0, 0, 300, 50),
            debug.ToString());
    }
    
    
    // Start is called before the first frame update
    void Start()
    {
        
        Button button = this.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            debug = this.transform.parent.parent.parent.Find("ContentView").gameObject.name;
            // disable all other content panels
            foreach (Transform childTransform in this.transform.parent.parent.parent.Find("ContentView"))
            {
                childTransform.gameObject.SetActive(false);
            }
            
            
            // open connected panel
            Tab.SetActive(true);
        });
    }
}