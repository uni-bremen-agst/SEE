using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlMode : MonoBehaviour
{
    public bool LeapMotion = false;
    public bool ViveControler = false;

    void Start()
    {
        GameObject LeapModels = GameObject.Find("/Player Rig/Hand Models");
        GameObject HandAttachments = GameObject.Find("/Player Rig/Attachment Hands");
        GameObject VRControlerRight = GameObject.Find("/Player Rig/Interaction Manager/VR Vive-style Controller (Left)");
        GameObject VRControlerLeft = GameObject.Find("/Player Rig/Interaction Manager/VR Vive-style Controller (Left)");
        GameObject MovementControl = GameObject.Find("/Player Rig/Movement Control");

        GameObject InteractionManager = GameObject.Find("/Player Rig/Interaction Manager");

        if(LeapMotion && ViveControler)
        {

        }
        else if (ViveControler)
        {
            LeapMotion = false;
            LeapModels.SetActive(false);
            MovementControl.GetComponent<LeapMovementSEE>().enabled = false;
            HandAttachments.SetActive(false);
        }
        else if(LeapMotion)
        {
            ViveControler = false;
            //VRControlerLeft.SetActive(false);
            //VRControlerRight.SetActive(false);
            //MovementControl.GetComponent<VRControlerMovement>().enabled = false;
        }
    }


}
