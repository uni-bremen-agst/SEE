using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectBuilding : MonoBehaviour
{
    bool show = false;
    GameObject house;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision detected");
        if(collision.gameObject.CompareTag("House") && !show)
        {
            house = collision.gameObject;
            collision.gameObject.GetComponent<MeshRenderer>().material = Resources.Load("BrickTextures/BricksTexture01/BricksTexture01", typeof(Material)) as Material;
            show = true;
            Debug.Log("Trigger has been pulled");
        }
        else if(collision.gameObject.CompareTag("House") && show && house == collision.gameObject)
        {
            collision.gameObject.GetComponent<MeshRenderer>().material = Resources.Load("BrickTextures/BricksTexture13/BricksTexture13", typeof(Material)) as Material;
            show = false;
        }
    }
}
