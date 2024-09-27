using UnityEngine;
using Unity.Netcode;
using SEE.Controls;
using SEE.Utils;

public class LivekitVideo : NetworkBehaviour
{
    /// <summary>
    /// The object with the position where the player's head is located.
    /// </summary>
    private Transform playerHead;

    /// <summary>
    /// The relative path to the bone of the player's head.
    /// This is used to position the Livekit video.
    /// </summary>
    private const string faceCamOrientationBone = "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";

    /// <summary>
    /// The status of the position of the Livekit video.
    /// Can be positioned in front of the face or above the face, tilted towards the observer.
    /// When <c>faceCamOnFront</c> is true, the video is positioned in front of the face using <c>offsetInFrontOfFace</c>.
    /// When <c>faceCamOnFront</c> is false, the video is positioned above the head using <c>offsetAboveHead</c>.
    /// </summary>
    private bool faceCamOnFront = true;

    /// <summary>
    /// Offset for positioning the video in front of the player's face.
    /// Used when <c>faceCamOnFront</c> is true.
    /// </summary>
    private Vector3 offsetInFrontOfFace = new Vector3(0, 0.065f, 0.15f);

    /// <summary>
    /// Offset for positioning the video above the player's head.
    /// Used when <c>faceCamOnFront</c> is false.
    /// </summary>
    private Vector3 offsetAboveHead = new Vector3(0, 0.35f, 0);

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes the player head reference and sets the object name so that it contains the owner ID of the client.
    /// </summary>
    private void Start()
    {
        // Set the name of the object to "LivekitVideo_" followed by the owner client ID.
        gameObject.name = "LivekitVideo_" + OwnerClientId;

        // Localizes the player's head bone for the positioning of the video.
        playerHead = transform.parent.Find(faceCamOrientationBone);
    }

    /// <summary>
    /// Called once per frame to update the position and rotation of the video.
    /// Toggles the position of the video between above the player's head or in front of it.
    /// The position can be toggled with <see cref="SEEInput.ToggleFaceCamPosition"/>.
    /// </summary>
    private void Update()
    {
        // Update the video position when the player's head is found.
        if (playerHead != null)
        {
            UpdatePosition();
        }

        // Check for input to toggle the video position.
        if (SEEInput.ToggleFaceCamPosition())
        {
            faceCamOnFront = !faceCamOnFront;
        }
    }

    /// <summary>
    /// Updates the position and orientation of the video based on the player's head.
    /// </summary>
    /// <remarks>If the video is positioned in front of the player's face, it follows
    /// the head's position and rotation. If positioned above the head, it faces the camera
    /// for remote clients.</remarks>
    private void UpdatePosition()
    {
        if (faceCamOnFront)
        {
            // Position the video in front of the player's face.
            transform.SetPositionAndRotation(playerHead.TransformPoint(offsetInFrontOfFace), playerHead.rotation);
        }
        else
        {
            // Position the video above the player's head.
            transform.position = playerHead.TransformPoint(offsetAboveHead);

            // If this object is not owned by the local client, make it face the main camera.
            if (!IsOwner && MainCamera.Camera != null)
            {
                transform.LookAt(MainCamera.Camera.transform);
            }
        }
    }
}
