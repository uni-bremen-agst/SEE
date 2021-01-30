using UnityEngine;
using UnityEngine.UI;
using SEE.Controls.Actions;

/// <summary>
/// This script is been added to the Button of the adding-node-canvas and the edit-node-canvas.
/// </summary>
public class NodeInteractionButtons : MonoBehaviour
{

    /// <summary>
    /// The button on the adding-node-canvas, which is finishing the addNode-process.
    /// </summary>
    public Button addingButton;

    /// <summary>
    /// The button on the editNode-canvas, which is canceling the editNode-process.
    /// </summary>
    public Button editNodeCancel;

    /// <summary>
    /// The button on the editNode-canvas, which is finishing the editNode-process.
    /// </summary>
    public Button editNodeButton;

    /// <summary>
    /// The button on the addNode-canvas, which is canceling the addingNode-process.
    /// </summary>
    public Button addNodeCancel;

    /// <summary>
    /// The Component playerActions, which is the parent of the DesktopNewNodeAction and DesktopEditNodeAction-scripts.
    /// </summary>
    private GameObject playerDesktop;

    /// <summary>
    /// The name of the GameObject which is the parent of the newNodeAction and editNodeAction
    /// </summary>
    private string gameObjectName = "Player Desktop";




    /// <summary>
    /// Adds a listener to the button which calls a method when the button is pushed.
    /// </summary>   
    void Start()
    {
        if (addingButton != null)
        {
            addingButton.onClick.AddListener(SetNextAddingNodeStep);
        }
        if (editNodeCancel != null)
        {
            editNodeCancel.onClick.AddListener(EditIsCanceled);
        }
        if (editNodeButton != null)
        {
            editNodeButton.onClick.AddListener(EditNode);
        }
        if(addNodeCancel != null)
        {
            addNodeCancel.onClick.AddListener(AddingIsCanceled);
        }

         playerDesktop = GameObject.Find(gameObjectName);
    }

    /// <summary>
    /// Increases the progress-enum in the DesktopNewNodeAction-instance. This results in the next step of addingNode.
    /// </summary>
    public void SetNextAddingNodeStep()
    {
        NewNodeAction current = playerDesktop.GetComponent<NewNodeAction>();
        current.Progress1 = NewNodeAction.Progress.CanvasIsClosed;
    }

    /// <summary>
    /// Sets a bool in the DesktopEditNodeAction-script which closes the adding-node-canvas.
    /// </summary>
    public void EditIsCanceled()
    {
        EditNodeAction current = playerDesktop.GetComponent<EditNodeAction>();
        current.EditProgress = EditNodeAction.Progress.EditIsCanceled;
    }

    /// <summary>
    /// Sets a bool in the EditNodeCanvas-script which starts the edit-process and evaluation of the inputFields.
    /// </summary>
    public void EditNode()
    {
        EditNodeCanvasAction.EditNode = true;
    }

    public void AddingIsCanceled()
    {
        NewNodeAction current = playerDesktop.GetComponent<NewNodeAction>();
        current.Progress1 = NewNodeAction.Progress.AddingIsCanceled;
    }
}
