using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AddingNodeCanvasScript : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject canvas;

    void Start()
    {
        
        canvas = Instantiate(Resources.Load("NodeCanvas", typeof(GameObject))) as GameObject;
        canvas.transform.SetParent(gameObject.transform);
      

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InstantiateCanvas()
    {
        Instantiate(canvas);
    }

    public void DestroyAllChilds()
    {
        int anzahl = canvas.transform.GetChildCount();
      
        foreach (Transform child in canvas.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        GameObject.Destroy(canvas);
    }
    
}
