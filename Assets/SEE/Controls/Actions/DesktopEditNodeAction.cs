using SEE.GO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesktopEditNodeAction : MonoBehaviour
{

    /// <summary>
    /// The Object that the Cursor hovers over
    /// </summary>
    public GameObject hoveredObject = null;

    private static bool editIsCanceled = false;

    public GameObject canvasObject;

    // Start is called before the first frame update
    void Start()
    {
        canvasObject = GameObject.Find("CanvasObject");
    }

    // Update is called once per frame
    void Update()
    {
        if (hoveredObject != null && Input.GetMouseButtonDown(0))
        {
            if (canvasObject.GetComponent<EditNodeCanvasScript>() == null)
            {
                CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                EditNodeCanvasScript script = generator.InstantiateEditNodeCanvas();
                script.nodeToEdit = hoveredObject.GetComponent<NodeRef>().node;
            }
        }
        if(editIsCanceled)
        {
            CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
            generator.DestroyEditNodeCanvas();
            hoveredObject = null;
            RemoveScript();
            editIsCanceled = false;
        }
    }

    /// <summary>
    /// Removes The Script
    /// Places the new Node if not placed
    /// </summary>
    public void RemoveScript()
    {
        Destroy(this);
    }

    public static void SetEditIsCanceled(bool newEditIsCaneled)
    {
        editIsCanceled = newEditIsCaneled;
    }

}
