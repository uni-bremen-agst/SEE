using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class TouchControlsSEE : MonoBehaviour
{
    //basic movement stats
    public GameObject Rig;
    public float ZoomFactor = 50;
    public float SpeedFactor = 25;
    public float movementEnv = 50;

    //the mode avoids triggering actions with less fingers after lifting them from the screen
    private int mode = 0;

    //contains the hit results after selection
    private bool hit = false;
    private RaycastHit hitInfo;

    //contains the targets transform after selection
    private Transform target;
    public UnityEvent OnTargetChanged;

    //variables to hold the distance from 2 fingers on the screen while zooming
    private float distance = 0;
    private float distanceDelta = 0;

    //holds the starting position on beginning touch with 3 fingers for changing camera angle
    private Vector2 pos;

    //variables used for camera auto movement
    private Transform startPos;
    //this Object represents the camera end transform
    private GameObject transObject;
    private float timeCount = 1f;

    //locks all actions for the time of camera auto movement
    private bool Lock = false;

    //holds if the UI was touched last frame
    private bool UITouch = false;

    void Start()
    {
        transObject = new GameObject();
        target = GameObject.Find("Plane").transform;
        Rig.transform.LookAt(target);

        if(OnTargetChanged == null)
        {
            OnTargetChanged = new UnityEvent();
        }
    }

    void Update()
    {
        if (!Lock && Input.touchCount > 0)
        {
            if (Input.touchCount == 1 && !UiIsTouched())
            {
                if (Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    mode = 1;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                hit = Physics.Raycast(ray, out hitInfo, Mathf.Infinity);

                if (hit && Input.GetTouch(0).phase == TouchPhase.Ended && mode == 1)
                {
                    SetTarget(target = hitInfo.transform);
                }
            }
            else if (Input.touchCount == 2)
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

                    Vector3 newPosition = Rig.transform.position + Rig.transform.forward * distanceDelta * ZoomFactor * Time.deltaTime;

                    //prevents the camera from moving beneath the city
                    if (newPosition.y >= 1)
                        Rig.transform.position = newPosition;
                }
            }
            else if (Input.touchCount == 3)
            {
                if (Input.GetTouch(2).phase == TouchPhase.Began)
                {
                    mode = 3;
                    pos = Input.GetTouch(2).position;
                }
                else if (Input.GetTouch(2).phase == TouchPhase.Moved && mode == 3)
                {
                    float distanceFactor = Vector3.Distance(Rig.transform.position, target.position) / movementEnv;

                    GameObject PositionAhead = new GameObject();
                    PositionAhead.transform.position = Rig.transform.position;
                    PositionAhead.transform.position += Rig.transform.right * (Input.GetTouch(2).position.x - pos.x) * SpeedFactor * distanceFactor * Time.deltaTime;
                    PositionAhead.transform.position += Rig.transform.up * (Input.GetTouch(2).position.y - pos.y) * SpeedFactor * distanceFactor * Time.deltaTime;
                    PositionAhead.transform.LookAt(target);

                    //prevents the camera from moving beneath the city
                    if (PositionAhead.transform.position.y >= 1 && Vector3.Angle(PositionAhead.transform.forward, target.transform.up) < 160)
                    {
                        Rig.transform.position = PositionAhead.transform.position;
                        Rig.transform.LookAt(target);
                    }
                }
                pos = Input.GetTouch(2).position;
            }
            else if (Input.touchCount == 4)
            {
                if(Input.GetTouch(3).phase == TouchPhase.Began)
                {
                    mode = 4;
                }
            }

            UITouch = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        
        if(Lock && Rig.transform.position != transObject.transform.position)
        {
            MoveToTarget();
        }
        else
        {
            Lock = false;
        }
    }

    //sets the offset position vector relativ to the targets position in root and its radius
    private void SetOffset()
    {
        Vector3 offSet = new Vector3();
        float radius = target.transform.localScale.x;
        Vector3 dir = target.transform.position.normalized;
        offSet.x = dir.x * radius;
        offSet.z = dir.z * radius;
        offSet.y = radius;

        transObject.transform.position = target.position + offSet;
        transObject.transform.LookAt(target);

    }
    //moves the camera to offset position relative to the target
    private void MoveToTarget()
    {
        Rig.transform.position = Vector3.Slerp(startPos.position, transObject.transform.position, timeCount);
        Rig.transform.rotation = Quaternion.Slerp(startPos.rotation, transObject.transform.rotation, timeCount);
        timeCount = timeCount + (Time.deltaTime / 5);
    }

    private bool UiIsTouched()
    {
        if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && !UITouch)
            return false;
        else
            return true;
    }

    public void SetTarget(Transform obj)
    {
        target = obj;
        SetOffset();
        startPos = Rig.transform;
        timeCount = 0;
        Lock = true;
        OnTargetChanged.Invoke();
    }

    public Transform GetTarget()
    {
        return target;
    }
}
