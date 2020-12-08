using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rearanges the annoationEditor and the annoations to face the main camera.
/// </summary>
public class AutoRotate : MonoBehaviour
{

    public Camera cameraToLookAt;
    
    void Start()
    {
        cameraToLookAt = Camera.main;
    }

    void Update()
    {
        Vector3 v = cameraToLookAt.transform.position - transform.position;
        v.x = v.z = 0.0f;
        transform.LookAt(cameraToLookAt.transform.position - v);
        transform.Rotate(0, 180, 0);
    }
}
