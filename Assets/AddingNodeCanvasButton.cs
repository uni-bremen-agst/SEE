using UnityEngine;
using UnityEngine.UI;
using SEE.Controls;

/// <summary>
/// This script is been added to the Button of the adding-node-canvas.
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

    /// <summary>
    /// Adds a listener to the button which calls a method when the button is pushed.
    /// </summary>
    void Start()
    {
        if (addingButton != null)
        {
            addingButton.onClick.AddListener(SetCanvasIsActive);
        }
        if (editNodeCancel != null)
        {
            editNodeCancel.onClick.AddListener(EditIsCanceled);
        }
        if (editNodeButton != null)
        {
            editNodeButton.onClick.AddListener(EditNode);
        }
    }

    /// <summary>
    /// Sets a bool in the DesktopNewNodeAction-script which is finishing the .
    /// </summary>
    public void SetCanvasIsActive()
    {
        DesktopNewNodeAction.CanvasIsActive = false;
    }

    /// <summary>
    /// Sets a bool in the DesktopEditNodeAction-script which closes the adding-node-canvas.
    /// </summary>
    public void EditIsCanceled()
    {
        DesktopEditNodeAction.EditIsCanceled = true;
    }

    public void EditNode()
    {
        EditNodeCanvasScript.EditNode = true;
    }
}
