using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchControlsSEE : MonoBehaviour
{
    public GameObject Rig;
    public float ZoomFactor = 50;
    public float SpeedFactor = 25;
    public float movementEnv = 50;

    private int mode = 0;

    private bool hit = false;
    private RaycastHit hitInfo;

    private Transform target;

    private float distance = 0;
    private float distanceDelta = 0;

    private Vector2 pos;

    void Start()
    {
        target = GameObject.Find("Plane").transform;
        MoveToDefault();
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            if(Input.GetTouch(0).phase == TouchPhase.Began)
            {
                mode = 1;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            hit = Physics.Raycast(ray, out hitInfo, Mathf.Infinity);

            if(hit && Input.GetTouch(0).phase == TouchPhase.Ended && mode == 1)
            {
                target = hitInfo.transform;
                MoveToDefault();
                Debug.Log(hitInfo.transform.name);
            }
        }
        else if(Input.touchCount == 2)
        {
            if (Input.GetTouch(1).phase == TouchPhase.Began)
            {
                mode = 2;
                distance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved && mode == 2)
            {
                distanceDelta = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position) - distance;
                distance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                Rig.transform.position += Rig.transform.forward * distanceDelta * ZoomFactor *Time.deltaTime;
            }
        }
        else if(Input.touchCount == 3)
        {
            if (Input.GetTouch(2).phase == TouchPhase.Began)
            {
                mode = 3;
                pos = Input.GetTouch(2).position;
                Debug.Log(pos.y);
            }
            else if(Input.GetTouch(2).phase == TouchPhase.Moved)
            {
                float distanceFactor = Vector3.Distance(Rig.transform.position, target.position)/movementEnv;
                Rig.transform.position += Rig.transform.right * (Input.GetTouch(2).position.x - pos.x) * SpeedFactor * distanceFactor* Time.deltaTime;
                Rig.transform.position += Rig.transform.up * (Input.GetTouch(2).position.y - pos.y) * SpeedFactor *distanceFactor* Time.deltaTime;
                Rig.transform.LookAt(target);
                pos = Input.GetTouch(2).position;
            }
        }
    }

    private void MoveToDefault()
    {
        Rig.transform.LookAt(target);
    }
}
