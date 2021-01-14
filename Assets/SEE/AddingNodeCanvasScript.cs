using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AddingNodeCanvasScript : MonoBehaviour
{

    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
        // Note: Its important that the Prefab lays inside of the Resources-Folder to use the Resources.Load-Method.
        canvas = Instantiate(Resources.Load("Prefabs/NewNodeCanvas", typeof(GameObject))) as GameObject;
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
