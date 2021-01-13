using SEE.Controls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class is a component of the AddingNodeCanvas and instantiates and destroys the adding-node-canvas script, which contains the addinNodeCanvas-prefab.
/// </summary>
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

    /// <summary>
    /// Instantiates the AddingNodeCanvasScript and adds it to the AddingNodeCanvas gameObject.
    /// </summary>
    /// <returns>the gameObject of the AddingNodeCanvas-script, which contains the canvas-prefab</returns>
    public GameObject InstantiateAddingNodeCanvas()
    {
        gameObject.AddComponent<AddingNodeCanvasScript>();
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        return script.canvas;
        
    }

    public GameObject InstantiateEditNodeCanvasScript()
    {
        gameObject.AddComponent<EditNodeCanvasScript>();
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        return script.canvas;

    }


    /// <summary>
    /// Destroys the AddingNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyAddCanvas()
    {
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<AddingNodeCanvasScript>());
        }
    }

    public void DestroyEditCanvas()
    {
        EditNodeCanvasScript script = gameObject.GetComponent<EditNodeCanvasScript>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<EditNodeCanvasScript>());
        }
    }

    /// <summary>
    /// Extracts the given Nodename, the nodetype and wether it is a inner node or a leaf from the canvas.
    /// </summary>
    public void GetNodeMetrics()
    {
        AddingNodeCanvasScript script = (AddingNodeCanvasScript)gameObject.GetComponent<AddingNodeCanvasScript>();

        //Gets the texts from the InputFields. The sequence in the array is given by the sequence of the components in the prefab.
        Component[] c = script.canvas.GetComponentsInChildren<InputField>();
        InputField inputname = (InputField)c[0];
        InputField inputtype = (InputField)c[1];

        //Gets the selection of the toggleGroup. The sequence in the toggleComponent-Array is given by the sequence of the components in the prefab.
        Component toggleGroup = script.canvas.GetComponentInChildren<ToggleGroup>();
        Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();
        if (toggles[0].isOn)
        {
            DesktopNewNodeAction.is_innerNode = true;
        }
        if (toggles[1].isOn)
        {
            DesktopNewNodeAction.is_innerNode = false;
        }

        DesktopNewNodeAction.nodename = inputname.text;
        DesktopNewNodeAction.nodetype = inputtype.text;

    }

}



