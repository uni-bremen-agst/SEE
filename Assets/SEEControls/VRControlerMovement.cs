using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class VRControlerMovement : MonoBehaviour
{
    public GameObject Rig;
    public GameObject Controler;

    private bool leftTriggerLastFrame = false;

    [SerializeField]
    [FormerlySerializedAs("OnLeftTriggerPulled")]
    private UnityEvent _OnLeftTrigger = new UnityEvent();

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float movementAxis = Input.GetAxis("RightVRTriggerMovement");
        Rig.transform.Translate(Controler.transform.forward * movementAxis);

        float leftTriggerAxis = Input.GetAxis("LeftVRTrigger");
        if(leftTriggerAxis >0.5f && !leftTriggerLastFrame)
        {
            _OnLeftTrigger.Invoke();
            leftTriggerLastFrame = true;
        }
        else if(leftTriggerAxis < 0.5 && leftTriggerLastFrame)
        {
            leftTriggerLastFrame = false;
        }
    }
}
