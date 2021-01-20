using SEE.Controls;
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
    /// Instantiates the AddingNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added AddingNodeCanvasScript</returns>
    public GameObject InstantiateAddingNodeCanvas()
    {
        gameObject.AddComponent<AddingNodeCanvasScript>();
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        return script.canvas;
    }

    /// <summary>
    /// Instantiates the EditNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added EditNodeCanvasScript</returns>
    public EditNodeCanvasScript InstantiateEditNodeCanvas()
    {
        gameObject.AddComponent<EditNodeCanvasScript>();
        EditNodeCanvasScript script = gameObject.GetComponent<EditNodeCanvasScript>();
        return script;

    }

    /// <summary>
    /// Destroys the AddingNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyAddNodeCanvas()
    {
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<AddingNodeCanvasScript>());
        }
    }

    /// <summary>
    /// Destroys the EditNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyEditNodeCanvas()
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
        string inputNodename;
        string inputNodetype;


        //this part has to be removed by the new UI-Team
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



