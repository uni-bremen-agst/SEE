using SEE.Controls;
using UnityEngine;
using UnityEngine.UI;

public class AddingNodeCanvasScript : NodeCanvasScript
{
    private string prefabDirectory = "Prefabs/NewNodeCanvas";
    // Start is called before the first frame update
    void Start()
    {
        // Note: Its important that the Prefab lays inside of the Resources-Folder to use the Resources.Load-Method.
        InstantiatePrefab(prefabDirectory);
        canvas.transform.SetParent(gameObject.transform);
    }

    /// <summary>
    /// Extracts the given Nodename, the nodetype and wether it is a inner node or a leaf from the canvas.
    /// </summary>
    public void GetNodeMetrics()
    {
        string inputNodename;
        string inputNodetype;


        //this part has to be removed by the new UI-Team
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();

        //Gets the texts from the InputFields. The sequence in the array is given by the sequence of the components in the prefab.
        Component[] c = script.canvas.GetComponentsInChildren<InputField>();
        InputField inputname = (InputField)c[0];
        InputField inputtype = (InputField)c[1];

        //Gets the selection of the toggleGroup. The sequence in the toggleComponent-Array is given by the sequence of the components in the prefab.
        Component toggleGroup = script.canvas.GetComponentInChildren<ToggleGroup>();
        Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();
        if (toggles[0].isOn)
        {
            DesktopNewNodeAction.IsInnerNode = true;
        }
        if (toggles[1].isOn)
        {
            DesktopNewNodeAction.IsInnerNode = false;
        }
        inputNodename = inputname.text;
        inputNodetype = inputtype.text;
        //until here 


        DesktopNewNodeAction.Nodename = inputNodename;
        DesktopNewNodeAction.Nodetype = inputNodetype;

    }

}

