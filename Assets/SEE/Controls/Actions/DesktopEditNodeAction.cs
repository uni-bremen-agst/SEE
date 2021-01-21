using SEE.GO;
using UnityEngine;

public class DesktopEditNodeAction : DesktopNodeAction
{

    /// <summary>
    /// True, if the Close-Button on the editCanvas is pushed, else false.
    /// </summary>
    private static bool editIsCanceled = false;

    public static bool EditIsCanceled { get => editIsCanceled; set => editIsCanceled = value; }

    // Start is called before the first frame update
    void Start()
    {
        InitialiseCanvasObject();
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

}
