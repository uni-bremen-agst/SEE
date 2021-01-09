using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddingNodeCanvasScript : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject canvas;
    void Start()
    {
        canvas = Instantiate(Resources.Load("NodeCanvas", typeof(GameObject))) as GameObject;
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
        foreach (Transform child in canvas.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    public void DestroyGameObject()
    {
        Destroy(transform.gameObject.GetComponentInParent<Canvas>().gameObject);
    }
}
