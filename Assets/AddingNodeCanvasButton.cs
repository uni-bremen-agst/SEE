using UnityEngine;
using UnityEngine.UI;
using SEE.Controls;
using SEE.Controls.Actions;

/// <summary>
/// This script is been added to the Button of the adding-node-canvas and the edit-node-canvas.
/// </summary>
public class AddingNodeCanvasButton : MonoBehaviour
{
    // Start is called before the first frame update

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

    public PlayerActions playerActions;




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

        GameObject g = GameObject.Find("DesktopPlayer");
        playerActions = g.GetComponent<PlayerActions>();
    }

    /// <summary>
    /// Increases the enum in the DesktopNewNodeAction-instance. This results in the next step of addingNode.
    /// </summary>
    public void SetNextAddingNodeStep()
    {
        DesktopNewNodeAction current = playerActions.GetComponent<DesktopNewNodeAction>();
        current.Progress1 = DesktopNewNodeAction.Progress.CanvasIsClosed;
    }

    /// <summary>
    /// Sets a bool in the DesktopEditNodeAction-script which closes the adding-node-canvas.
    /// </summary>
    public void EditIsCanceled()
    {
        DesktopEditNodeAction current = playerActions.GetComponent<DesktopEditNodeAction>();
        current.EditProgress = DesktopEditNodeAction.Progress.EditIsCanceled;
    }

    /// <summary>
    /// Sets a bool in the EditNodeCanvas-script which starts the edit-process and evaluation of the inputFields.
    /// </summary>
    public void EditNode()
    {
        EditNodeCanvasScript.EditNode = true;
    }
}
