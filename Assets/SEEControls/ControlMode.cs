using UnityEngine;

public class ControlMode : MonoBehaviour
{
    /// <summary>
    /// If true, leap motion controller is activated.
    /// </summary>
    public bool LeapMotion = false;
    /// <summary>
    /// If true, HTC Vive handheld controller is activated.
    /// </summary>
    public bool ViveController = true;

    void Start()
    {
        GameObject LeapModels = GameObject.Find("/Player Rig/Hand Models");
        GameObject HandAttachments = GameObject.Find("/Player Rig/Attachment Hands");
        GameObject VRControlerRight = GameObject.Find("/Player Rig/Interaction Manager/VR Vive-style Controller (Left)");
        GameObject VRControlerLeft = GameObject.Find("/Player Rig/Interaction Manager/VR Vive-style Controller (Left)");
        GameObject MovementControl = GameObject.Find("/Player Rig/Movement Control");

        GameObject InteractionManager = GameObject.Find("/Player Rig/Interaction Manager");

        if(LeapMotion && ViveController)
        {

        }
        else if (ViveController)
        {
            LeapMotion = false;
            LeapModels.SetActive(false);
            MovementControl.GetComponent<LeapMovementSEE>().enabled = false;
            HandAttachments.SetActive(false);
        }
        else if(LeapMotion)
        {
            ViveController = false;
            //VRControlerLeft.SetActive(false);
            //VRControlerRight.SetActive(false);
            //MovementControl.GetComponent<VRControlerMovement>().enabled = false;
        }
    }


}
