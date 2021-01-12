using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AddingNodeCanvasScript : MonoBehaviour
{ 
    /// <summary>
    /// The canvas-prefab for the new-node-process.
    /// </summary>
    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
        canvas = Instantiate(Resources.Load("NodeCanvas", typeof(GameObject))) as GameObject;
        canvas.transform.SetParent(gameObject.transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Destroys the canvas-gameObject and all its childs.
    /// </summary>
    public void DestroyGOAndAllChilds()
    {
        foreach (Transform child in canvas.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        GameObject.Destroy(canvas);
    }
    
}
