using SEE.Controls.Actions;
using UnityEngine;

/// <summary>
/// This class instantiates and destroys the adding-node-canvas script, which contains the addingNodeCanvas-prefab.
/// This also applies to the EditNodeCanvas.
/// </summary>
public class CanvasGenerator : MonoBehaviour
{
  
    /// <summary>
    /// Instantiates the AddingNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added AddingNodeCanvasScript</returns>
    public AddingNodeCanvasAction InstantiateAddingNodeCanvas()
    {
        gameObject.AddComponent<AddingNodeCanvasAction>();
        AddingNodeCanvasAction script = gameObject.GetComponent<AddingNodeCanvasAction>();
        return script;
    }

    /// <summary>
    /// Instantiates the EditNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added EditNodeCanvasScript</returns>
    public EditNodeCanvasAction InstantiateEditNodeCanvas()
    {
        gameObject.AddComponent<EditNodeCanvasAction>();
        EditNodeCanvasAction script = gameObject.GetComponent<EditNodeCanvasAction>();
        return script;

    }

    /// <summary>
    /// Destroys the AddingNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyAddNodeCanvas()
    {
        AddingNodeCanvasAction script = gameObject.GetComponent<AddingNodeCanvasAction>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<AddingNodeCanvasAction>());
        }
    }

    /// <summary>
    /// Destroys the EditNodeCanvasScript and all childs of it.
    /// </summary>
    public void DestroyEditNodeCanvas()
    {
        EditNodeCanvasAction script = gameObject.GetComponent<EditNodeCanvasAction>();
        if (script != null)
        {
            script.DestroyGOAndAllChilds();
            Destroy(gameObject.GetComponent<EditNodeCanvasAction>());
        }
    }

}



