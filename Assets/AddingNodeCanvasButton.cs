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
    /// The button on the adding-node-canvas, which is closing the canvas
    /// </summary>
    public Button button;

    /// <summary>
    /// Adds a listener to the button which calls a method when the button is pushed.
    /// </summary>
    void Start()
    {
        button.onClick.AddListener(SetCanvasIsClosed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Sets a bool in the DesktopNewNodeAction-script which closes the adding-node-canvas.
    /// </summary>
    public void SetCanvasIsClosed()
    {
        DesktopNewNodeAction.SetCanvasIsClosed(false);
    }
}
