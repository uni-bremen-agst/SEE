using UnityEngine;
using Unity.Netcode;
using SEE.Controls;
using SEE.Utils;

namespace SEE.Tools.LiveKit
{
    /// <summary>
    /// Handles the positioning of the LiveKit video stream relative to the player's head.
    /// </summary>
    /// <remarks>
    /// The component is attached to Prefabs/Players/CC4/BaseAvatar.prefab.
    /// </remarks>
    public class LiveKitVideo : NetworkBehaviour
    {
        /// <summary>
        /// The Transform representing the player's head.
        /// </summary>
        private Transform playerHead;

        /// <summary>
        /// The bone path leading to the player's head. Used to position the video.
        /// </summary>
        private const string FaceCamOrientationBone =
            "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/" +
            "CC_Base_Spine02/CC_Base_NeckTwist01/CC_Base_NeckTwist02/CC_Base_Head";

        /// <summary>
        /// Determines whether the video is in front of the face or above the head.
        /// True if positioned in front of the face.
        /// </summary>
        private bool faceCamOnFront = true;

        /// <summary>
        /// Offset for positioning the video in front of the player's face.
        /// </summary>
        private readonly Vector3 offsetInFrontOfFace = new(0, 0.065f, 0.15f);

        /// <summary>
        /// Offset for positioning the video above the player's head.
        /// </summary>
        private readonly Vector3 offsetAboveHead = new(0, 0.35f, 0);

        /// <summary>
        /// Prefix used for naming LiveKit video GameObjects, combining with the client ID
        /// to form a unique name, e.g., "LiveKitVideo_123".
        /// </summary>
        public const string Prefix = "LiveKitVideo_";

        /// <summary>
        /// Called when the network object is spawned.
        /// Registers this instance inside <see cref="LiveKitVideoRegistry"/>.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            LiveKitVideoRegistry.Register(OwnerClientId, this);
        }

        /// <summary>
        /// Called when the network object is despawned.
        /// Unregisters this instance from <see cref="LiveKitVideoRegistry"/>.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            LiveKitVideoRegistry.Unregister(OwnerClientId);
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Initializes the player head reference and names the object according to the owner ID.
        /// Logs an error and disables the component if the player head cannot be found.
        /// </summary>
        private void Start()
        {
            gameObject.name = Prefix + OwnerClientId;
            // Localizes the player's head bone for the positioning of the video.
            playerHead = transform.parent.Find(FaceCamOrientationBone);

            if (playerHead == null)
            {
                Debug.LogError($"Player head not found for client ID {OwnerClientId}. Disabling {nameof(LiveKitVideo)} component.");
                enabled = false;
            }
        }

        /// <summary>
        /// Updates the position of the video every frame. Toggles the video position based on input.
        /// </summary>
        private void Update()
        {
            UpdatePosition();

            if (SEEInput.ToggleFaceCamPosition())
            {
                faceCamOnFront = !faceCamOnFront;
            }
        }

        /// <summary>
        /// Updates the position and rotation of the video based on the player's head.
        /// </summary>
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
}
