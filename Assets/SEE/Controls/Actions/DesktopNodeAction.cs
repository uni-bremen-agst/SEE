using UnityEngine;

public class DesktopNodeAction : MonoBehaviour
{
    /// <summary>
    /// The gameObject which contains the CanvasGenerator-Scripts and the actual CanvasObject-Script
    /// </summary>
    protected GameObject canvasObject;

    /// <summary>
    /// The Object that the Cursor hovers over
    /// </summary>
    public GameObject hoveredObject = null;

    /// <summary>
    /// The current name of the gameObject which contains the Canvas-operations and components.
    /// </summary>
    private readonly string nameOfCanvasObject = "CanvasObject";

    /// <summary>
    /// Removes this script or rather the child-script.
    /// </summary>
    public virtual void RemoveScript()
    {
        Destroy(this);
    }

    /// <summary>
    /// Finds the GameObject, which contains the CanvasOperations and components
    /// and saves it in the canvasObject-variable.
    /// </summary>
    public void InitialiseCanvasObject()
    {
        canvasObject = GameObject.Find(nameOfCanvasObject);
    }

}
