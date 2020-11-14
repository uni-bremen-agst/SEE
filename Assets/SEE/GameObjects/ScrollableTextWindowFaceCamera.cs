/// Code from http://wiki.unity3d.com/index.php?title=CameraFacingBillboard
/// Credits go to Neil Carter (NCarter)
using UnityEngine;

/// <summary>
/// This script can be added to a Canvas GameObject to make it always face the main camera.
/// </summary>
public class ScrollableTextWindowFaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.localRotation * Vector3.forward, Camera.main.transform.localRotation * Vector3.up);
    }
}
