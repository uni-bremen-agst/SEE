using SEE.DataModel.DG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class EditNodeCanvasScript : MonoBehaviour
{
    public GameObject canvas;

    public Node nodeToEdit;

    public static bool editNode = false;

    InputField inputname;
    InputField inputtype;

    public GameObject canvasObject;

// Start is called before the first frame update
void Start()
    {
        // Note: Its important that the Prefab lays inside of the Resources-Folder to use the Resources.Load-Method.
        canvas = Instantiate(Resources.Load("Prefabs/EditNodeCanvas", typeof(GameObject))) as GameObject;
        canvas.transform.SetParent(gameObject.transform);

        Component[] c = canvas.GetComponentsInChildren<InputField>();
        inputname = (InputField)c[0];
        inputtype = (InputField)c[1];
        inputname.text = nodeToEdit.SourceName;
        inputtype.text = nodeToEdit.Type;
        canvasObject = GameObject.Find("CanvasObject");
    }

    // Update is called once per frame
    void Update()
    {
        if(editNode)
        {
            if (!inputname.text.Equals(nodeToEdit.SourceName))
            {
                nodeToEdit.SourceName = inputname.text;
            }
            if (!inputtype.text.Equals(nodeToEdit.Type))
            {
                nodeToEdit.Type = inputtype.text;
            }
            CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
            generator.DestroyEditNodeCanvas();
            editNode = false;
        }
    }

    /// <summary>
    /// Destroys the canvas-gameObject and all its childs.
    /// </summary>
    public void DestroyGOAndAllChilds()
    {
        foreach (Transform child in canvas.transform)
        {
            Destroy(child.gameObject);
        }
        Destroy(canvas);
    }

}
