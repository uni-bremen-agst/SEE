/// Code from http://wiki.unity3d.com/index.php?title=CameraFacingBillboard
/// Credits go to Neil Carter (NCarter)
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
        if (Camera.allCamerasCount > 1)
        {
            Debug.LogWarning("There is more than one camera in the scene.\n");
        }
        mainCamera = Camera.main.transform;
        Debug.LogFormat("ScrollableTextWindows will be facing camera in {0}.\n", mainCamera.name);
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + mainCamera.localRotation * Vector3.forward, mainCamera.localRotation * Vector3.up);
    }
}
