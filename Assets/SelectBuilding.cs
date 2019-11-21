using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectBuilding : MonoBehaviour
{
    private Transform direction;
    public LineRenderer line; // assigne in inspector
    public TextMeshPro text; // assign in inspector
    public Color colorOnHit = Color.green;
    public Color defaultColor = Color.red;

    private bool active = false;
    private bool show = false;
    private GameObject house;

    private void Start()
    {
        transform.gameObject.SetActive(false);
        direction = transform.parent.transform;
    }

    private void Update()
    {
        RaycastHit hitInfo = new RaycastHit();
        bool hit = Physics.Raycast(direction.position, direction.TransformDirection(Vector3.down), out hitInfo, Mathf.Infinity);

            line.SetPosition(0, direction.position);
            line.SetPosition(1, direction.TransformDirection(Vector3.down)*1000);

            if (hit && hitInfo.collider.gameObject.CompareTag("Building"))
            {
                Debug.Log("Ray hit House");
                line.SetPosition(1, hitInfo.point);
                line.material.color = colorOnHit;
                text.text = hitInfo.collider.gameObject.name;

            }
            else
            {
                text.text = "";
                line.material.color = defaultColor;
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

    private void MarkBuilding()
    {

    }
}
