using Leap.Unity;
using UnityEngine;

/// <summary>
/// Enables/disables the controller (leap motion or vive style).
/// </summary>
public class ControlMode : MonoBehaviour
{
    /// <summary>
    /// If true, the leap motion controller is activated. Will be set in the inspector.
    /// </summary>
    [Tooltip("If ticked, the leap motion controller is activated; otherwise a Vive-style controller will be used.")]
    public bool EnableLeapMotion = false;

    void Start()
    {
        if (! EnableLeapMotion)
        {
            GameObject LeapModels = GameObject.Find("/Player Rig/Hand Models");
            LeapModels.SetActive(false);

            GameObject HandAttachments = GameObject.Find("/Player Rig/Attachment Hands");
            HandAttachments.SetActive(false);

            GameObject MovementControl = GameObject.Find("/Player Rig/Movement Control");
            MovementControl.GetComponent<LeapMovementSEE>().enabled = false;

            // We are turning off the LeapXRServiceProvider to avoid warning messages
            // by Leap Motion of the kind:
            // "Leap Service not connected; attempting to reconnect for try..."
            GameObject RigCamera = GameObject.Find("/Player Rig/Main Camera");
            RigCamera.GetComponent<LeapXRServiceProvider>().enabled = false;
        }
    }
}
