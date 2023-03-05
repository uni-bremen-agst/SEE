using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadCameraSmoothing : MonoBehaviour
{
    public Vector3 lastPos;
    public Quaternion lastRot;



    public void LateUpdate() {


        lastPos = transform.position;
        lastRot = transform.rotation;
    }
}
