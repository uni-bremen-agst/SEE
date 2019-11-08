using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectBuilding : MonoBehaviour
{
    public Transform direction; // assign in inspector

    private bool active = false;
    private bool show = false;
    private GameObject house;

    private void Start()
    {
        transform.gameObject.SetActive(false);
    }

    private void Update()
    {
        RaycastHit hitInfo = new RaycastHit();
        bool hit = Physics.Raycast(direction.position, direction.TransformDirection(Vector3.forward), out hitInfo, Mathf.Infinity);

        if(hit)
        {
            Debug.DrawRay(direction.position, direction.TransformDirection(Vector3.forward) * hitInfo.distance, Color.red);
            if(hitInfo.collider.gameObject.CompareTag("Building"))
            {
                Debug.Log("Ray hit House");
            }
        }
        else
        {
            Debug.DrawRay(direction.position, direction.TransformDirection(Vector3.forward) * 1000, Color.white);
        }
    }

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

    public void RayOnOff()
    {
        active = !active;
        transform.gameObject.SetActive(active);
        Debug.Log("Ray on/off");
    }
}
