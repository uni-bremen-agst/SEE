using SEE.GO;
using UnityEngine;

public class DesktopEditNodeAction : DesktopNodeAction
{

    public enum Progress
    {
        NoNodeSelected,
        NodeSelected,
        EditIsCanceled,
    }

    /// <summary>
    /// An instance of the ProgressEnum, which represents the current state of the Edit-Node-process.
    /// </summary>
    private Progress editProgress = Progress.NoNodeSelected;

    public Progress EditProgress { get => editProgress; set => editProgress = value; }


    void Start()
    {
        InitialiseCanvasObject();
    }

    /// <summary>
    /// The update-method interacts in dependency of the edit-progress-state. (sequencial series)
    /// NoNodeSelected: Waits until a node is selected by pushing mouse button on a gameNode 
    /// NodeSelected: Instantiates the canvasObject if a gameNode is selected 
    /// EditIsCanceled: Removes the canvas and resets values if the process is canceled
    /// </summary>
    void Update()
    {
        switch (editProgress)
        {
            case Progress.NoNodeSelected:

                if (hoveredObject != null && Input.GetMouseButtonDown(0))
                {
                    EditProgress = Progress.NodeSelected;
                }
                break;

            case Progress.NodeSelected:
                if (canvasObject.GetComponent<EditNodeCanvasScript>() == null)
                {
                    CanvasGenerator generator = canvasObject.GetComponent<CanvasGenerator>();
                    EditNodeCanvasScript script = generator.InstantiateEditNodeCanvas();
                    script.nodeToEdit = hoveredObject.GetComponent<NodeRef>().node;
                    script.gameObjectID = hoveredObject.name;
                }
                break;

            case Progress.EditIsCanceled:
                CanvasGenerator canvasGenerator = canvasObject.GetComponent<CanvasGenerator>();
                canvasGenerator.DestroyEditNodeCanvas();
                hoveredObject = null;
                RemoveScript();
                EditProgress = Progress.NoNodeSelected;
                break;
        }
    }

}

