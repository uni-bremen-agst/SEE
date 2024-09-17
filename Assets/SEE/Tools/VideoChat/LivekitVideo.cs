using UnityEngine;
using Unity.Netcode;
using SEE.Controls;
using SEE.Utils;

public class LivekitVideo : NetworkBehaviour
{
    private Transform playersFace;
    private const string faceCamOrientationBone = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";
    private bool faceCamOnFront = true;
    private Vector3 offsetAbove = new Vector3(0, 0.35f, 0);
    private Vector3 offsetFront = new Vector3(0, 0.065f, 0.15f);
    
    void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            gameObject.name = "LivekitVideo_" + OwnerClientId;
            playersFace = transform.parent.Find(faceCamOrientationBone);
        }
    }

    void Update()
    {
        if (playersFace != null)
        {
            UpdatePosition();
        }

        if (SEEInput.ToggleFaceCamPosition())
        {
            faceCamOnFront = !faceCamOnFront;
        }
    }

    private void UpdatePosition()
    {
        if (faceCamOnFront)
        {
            transform.SetPositionAndRotation(playersFace.TransformPoint(offsetFront), playersFace.rotation);
        }
        else
        {
            transform.position = playersFace.TransformPoint(offsetAbove);
            if (!IsOwner)
            {
                if (MainCamera.Camera != null)
                {
                    transform.LookAt(MainCamera.Camera.transform);
                }
            }
        }
    }
}
