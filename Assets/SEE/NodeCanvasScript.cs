using UnityEngine;

public class NodeCanvasScript : MonoBehaviour
{

    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
