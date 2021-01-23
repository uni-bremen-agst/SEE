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

    private readonly string nameOfCanvasObject = "CanvasObject";

    /// <summary>
    /// Removes The Script
    /// Places the new Node if not placed
    /// </summary>
    public virtual void RemoveScript()
    {
        Destroy(this);
    }

    public void InitialiseCanvasObject()
    {
        canvasObject = GameObject.Find(nameOfCanvasObject);
    }

}
