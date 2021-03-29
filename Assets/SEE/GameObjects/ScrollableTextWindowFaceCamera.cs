/// Code from http://wiki.unity3d.com/index.php?title=CameraFacingBillboard
/// Credits go to Neil Carter (NCarter)
using SEE.Utils;
using UnityEngine;

/// <summary>
/// This script can be added to a Canvas GameObject to make it always face the main camera.
/// It is a component of the prefab ScrollableTextWindow.
/// </summary>
public class ScrollableTextWindowFaceCamera : MonoBehaviour
{
    private Transform mainCamera;

    private void Start()
    {
        mainCamera = MainCamera.Camera.transform;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + mainCamera.localRotation * Vector3.forward, mainCamera.localRotation * Vector3.up);
    }
}
