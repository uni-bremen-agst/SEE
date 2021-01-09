using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scri2 : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject testgameObject2;
    void Start()
    {
        testgameObject2 = Instantiate(Resources.Load("TestPrefab", typeof(GameObject))) as GameObject;
        Debug.Log("Start-Methode ist gerunnt");
        Debug.Log(testgameObject2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InstantiateGameObject()
    {
        Instantiate(testgameObject2);
        Debug.Log("testgameObject2 existiert noch nicht");



    }

    public void DestroyAllChilds()
    {
        foreach (Transform child in testgameObject2.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
    public void DestroyGameObject()
    {
        Destroy(transform.gameObject.GetComponentInParent<Canvas>().gameObject);
    }
}
