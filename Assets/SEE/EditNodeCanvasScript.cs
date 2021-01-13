using SEE.DataModel.DG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class EditNodeCanvasScript : MonoBehaviour
{
    /// <summary>
    /// The canvas-prefab for the new-node-process.
    /// </summary>
    public GameObject canvas;

    public Node nodeToEdit; 

    // Start is called before the first frame update
    void Start()
    {
        // Note: Its important that the Prefab lays inside of the Resources-Folder to use the Resources.Load-Method.
        canvas = Instantiate(Resources.Load("Prefabs/EditNodeCanvas", typeof(GameObject))) as GameObject;
        canvas.transform.SetParent(gameObject.transform);

        Component[] c = canvas.GetComponentsInChildren<InputField>();
        InputField inputname = (InputField)c[0];
        InputField inputtype = (InputField)c[1];
        inputname.text = nodeToEdit.SourceName;
        inputtype.text = nodeToEdit.Type;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Destroys the canvas-gameObject and all its childs.
    /// </summary>
    public void DestroyGOAndAllChilds()
    {
        foreach (Transform child in canvas.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        GameObject.Destroy(canvas);
    }

}
