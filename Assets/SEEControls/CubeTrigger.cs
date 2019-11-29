using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeTrigger : MonoBehaviour
{
    private BoxCollider triggerBox;

    void Start()
    {
        triggerBox = gameObject.AddComponent<BoxCollider>() as BoxCollider;
        triggerBox.isTrigger = true;
        triggerBox.size = new Vector3(2, 2, 2);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject[] temp = new GameObject[2];
        temp[0] = gameObject;
        temp[1] = other.gameObject;
        triggerBox.SendMessageUpwards("ShowUp", temp);
    }

    private void OnTriggerExit(Collider other)
    {
        triggerBox.SendMessageUpwards("Hide", gameObject);
    }
}
