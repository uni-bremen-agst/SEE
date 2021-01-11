using SEE.Controls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public GameObject InstantiateGameObject()
    {
        gameObject.AddComponent<AddingNodeCanvasScript>();
        AddingNodeCanvasScript script = (AddingNodeCanvasScript)gameObject.GetComponent("AddingNodeCanvasScript");
        return script.canvas;
        
    }

    public void DestroyGameObject()
    {
        AddingNodeCanvasScript script = (AddingNodeCanvasScript)gameObject.GetComponent("AddingNodeCanvasScript");

        Component[] c = script.canvas.GetComponentsInChildren<InputField>();
        InputField inputname = (InputField)c[0];
        InputField inputtype = (InputField)c[1];

        Component toggleGroup = script.canvas.GetComponentInChildren<ToggleGroup>();

        Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();
     
        if(toggles[0].isOn)
        {
            Debug.Log("InnerNode");
            DesktopNewNodeAction.is_innerNode = true;
        }
        if (toggles[1].isOn)
        {
            Debug.Log("Leaf");
            DesktopNewNodeAction.is_innerNode = false;
        }


        DesktopNewNodeAction.nodename = inputname.text;
        DesktopNewNodeAction.nodetype = inputtype.text;
        
        script.DestroyAllChilds();
        Destroy(gameObject.GetComponent("AddingNodeCanvasScript"));

    }

}



