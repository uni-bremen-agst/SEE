using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRControlerMovement : MonoBehaviour
{
    public GameObject Rig;
    public GameObject Controler;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float movementAxis = Input.GetAxis("RightVRTriggerMovement");
        Rig.transform.Translate(Controler.transform.forward * movementAxis);
    }
}
