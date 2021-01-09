using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject testgameObject2;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InstantiateGameObject()
    {
        gameObject.AddComponent<AddingNodeCanvasScript>();
        


    }

    public void DestroyGameObject()
    {
        Debug.Log("testgameObject2 sollte unsichtbar werden");
        AddingNodeCanvasScript addingNodeCanvasScript = (AddingNodeCanvasScript)gameObject.GetComponent("AddingNodeCanvasScript");
        addingNodeCanvasScript.DestroyAllChilds();
        Destroy(gameObject.GetComponent("AddingNodeCanvasScript"));

    }

}



