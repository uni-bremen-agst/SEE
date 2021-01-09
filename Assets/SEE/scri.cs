using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scri : MonoBehaviour
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
          //  Instantiate(testgameObject2);
            Debug.Log("testgameObject2 sollte sichtbar werden");
        gameObject.AddComponent<scri2>();
        


    }

    public void DestroyGameObject()
    {
        Debug.Log("testgameObject2 sollte unsichtbar werden");
        scri2 scri22 = (scri2)gameObject.GetComponent("scri2");
        scri22.DestroyAllChilds();
        Destroy(gameObject.GetComponent("scri2"));

    }

    private static void SetVisible(GameObject gameObject, bool isVisible)
    {
        gameObject.SetActive(isVisible);

    }
}



