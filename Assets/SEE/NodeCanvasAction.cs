using UnityEngine;

public class NodeCanvasAction : MonoBehaviour
{
    /// <summary>
    /// The gameObject, which contains the canvas-prefab-clone.
    /// </summary>
    public GameObject canvas;

    /// <summary>
    /// Instantiates a clone of the given prefab and saves it in an attribute.
    /// </summary>
    /// <param name="directory">The directory of the prefab</param>
    public void InstantiatePrefab(string directory)
    {
        canvas = Instantiate(Resources.Load(directory, typeof(GameObject))) as GameObject;
    }

    /// <summary>
    /// Destroys the canvas-gameObject and all its childs.
    /// </summary>
    public void DestroyGOAndAllChilds()
    {
        foreach (Transform child in canvas.transform)
        {
            Destroy(child.gameObject);
        }
        Destroy(canvas);
    }
}
