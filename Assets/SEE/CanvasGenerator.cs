using UnityEngine;

/// <summary>
/// This class is a component of the AddingNodeCanvas and instantiates and destroys the adding-node-canvas script, which contains the addingNodeCanvas-prefab.
/// This also applies to the EditNodeCanvas.
/// </summary>
public class CanvasGenerator : MonoBehaviour
{
  
    /// <summary>
    /// Instantiates the AddingNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added AddingNodeCanvasScript</returns>
    public AddingNodeCanvasScript InstantiateAddingNodeCanvas()
    {
        gameObject.AddComponent<AddingNodeCanvasScript>();
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        return script;
    }

    /// <summary>
    /// Instantiates the EditNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added EditNodeCanvasScript</returns>
    public EditNodeCanvasScript InstantiateEditNodeCanvas()
    {
        gameObject.AddComponent<EditNodeCanvasScript>();
        EditNodeCanvasScript script = gameObject.GetComponent<EditNodeCanvasScript>();
        return script;

    }

    /// <summary>
    /// Destroys the AddingNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyAddNodeCanvas()
    {
        AddingNodeCanvasScript script = gameObject.GetComponent<AddingNodeCanvasScript>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<AddingNodeCanvasScript>());
        }
    }

    /// <summary>
    /// Destroys the EditNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyEditNodeCanvas()
    {
        EditNodeCanvasScript script = gameObject.GetComponent<EditNodeCanvasScript>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<EditNodeCanvasScript>());
        }
    }

}



