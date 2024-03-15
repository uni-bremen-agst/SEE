using RootMotion.FinalIK;
using SEE.Game.Avatars;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Updates values of <see cref="VRIK"/> on all remote clients.
    /// </summary>
    public class VRIKNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        /// <summary>
        /// The remote position of the head.
        /// </summary>
        public Vector3 RemoteHeadPosition;

        /// <summary>
        /// The remote position of the left hand.
        /// </summary>
        public Vector3 RemoteLeftHandPosition;

        /// <summary>
        /// The remote position of the right hand.
        /// </summary>
        public Vector3 RemoteRightHandPosition;

        /// <summary>
        /// The remote rotation of the head.
        /// </summary>
        public Quaternion RemoteHeadRotation;

        /// <summary>
        /// The remote rotation of the left hand.
        /// </summary>
        public Quaternion RemoteLeftHandRotation;

        /// <summary>
        /// The remote rotation of the right hand.
        /// </summary>
        public Quaternion RemoteRightHandRotation;

        /// <summary>
        /// The path to the animator controller that should be used when the avatar
        /// is set up for VR. This controller will be assigned to the remote UMA avatar
        /// as the default race animation controller.
        /// </summary>
        private const string animatorForVrik = "Prefabs/Players/VRIKAnimatedLocomotion";

        /// <summary>
        /// Initializes all variables that should be transferred to the remote avatars.
        /// </summary>
        /// <param name="networkObjectID">NetworkObject ID of the spawned avatar game object.</param>
        /// <param name="vrik">VRIK component to be synchronized.</param>
        public VRIKNetAction(ulong networkObjectID, VRIK vrik)
        {
            GameObject remoteHeadTarget = vrik.solver.spine.headTarget.gameObject;
            GameObject remoteLeftArm = vrik.solver.leftArm.target.gameObject;
            GameObject remoteRightArm = vrik.solver.rightArm.target.gameObject;

            NetworkObjectID = networkObjectID;

            RemoteHeadPosition = remoteHeadTarget.transform.position;
            RemoteLeftHandPosition = remoteLeftArm.transform.position;
            RemoteRightHandPosition = remoteRightArm.transform.position;

            RemoteHeadRotation = remoteHeadTarget.transform.rotation;
            RemoteLeftHandRotation = remoteLeftArm.transform.rotation;
            RemoteRightHandRotation = remoteRightArm.transform.rotation;
        }

        /// <summary>
        /// If executed by the initiating client, nothing happens.
        /// If executed by the remote avatar, the usual positional and rotational model
        /// connections are established.
        /// </summary>
        public override void ExecuteOnClient()
        {
            VRIKActions.ExecuteOnClient(NetworkObjectID, animatorForVrik,
                RemoteHeadPosition, RemoteRightHandPosition, RemoteLeftHandPosition,
                RemoteHeadRotation, RemoteRightHandRotation, RemoteLeftHandRotation);
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}