using SEE.Controls.Actions;
using UnityEngine;

/// <summary>
/// This class instantiates and destroys the adding-node canvas, which contains the addingNodeCanvas prefab.
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
        return gameObject.AddComponent<AddingNodeCanvasAction>();
    }

    /// <summary>
    /// Instantiates the EditNodeCanvasScript and adds it to the CanvasObject gameObject.
    /// </summary>
    /// <returns>the added EditNodeCanvasScript</returns>
    public EditNodeCanvasAction InstantiateEditNodeCanvas()
    {
        return gameObject.AddComponent<EditNodeCanvasAction>();
    }

    /// <summary>
    /// Destroys the AddingNodeCanvasScript and each of its children.
    /// </summary>
    public void DestroyAddNodeCanvas()
    {
        if (gameObject.TryGetComponent(out AddingNodeCanvasAction action))
        {
            action.DestroyGOAndAllChilds();
        }
    }

    /// <summary>
    /// Destroys the EditNodeCanvasScript and each of its children.
    /// </summary>
    public void DestroyEditNodeCanvas()
    {
        if (gameObject.TryGetComponent(out EditNodeCanvasAction action))
        {
            action.DestroyGOAndAllChilds();
        }
    }
}
