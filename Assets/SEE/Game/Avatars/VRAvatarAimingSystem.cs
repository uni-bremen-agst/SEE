using SEE.Controls;
using SEE.GO;
using SEE.Tools.OpenTelemetry;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Manages a laser beamer for simple pointing gestures for a VR avatar.
    /// </summary>
    /// <remarks>This component is expected to be added to an avatar that
    /// represents a local player in VR, that is, <see cref="gameObject"/>
    /// is referring to a game object that is the root of an avatar.</remarks>
    internal class VRAvatarAimingSystem : MonoBehaviour
    {
        [Tooltip("The laser beam for pointing. If null, one will be created at run-time.")]
        public LaserPointer Laser;

        [Tooltip("If true, local interactions control where the avatar is pointing to.")]
        public bool IsLocallyControlled = true;

        ///<summary>
        /// The bone of the hand that is used to point.
        /// </summary>
        private Transform HandBone;

        /// <summary>
        /// The source from which to start the laser beam. Assigning
        /// a value to this Source will always turn the laser on.
        /// </summary>
        [ShowInInspector]
        public Transform Source
        {
            set
            {
                if (Laser == null)
                {
                    Laser = gameObject.AddOrGetComponent<LaserPointer>();
                }

                Laser.Source = value;
                Laser.On = true;
            }
            get { return Laser.Source; }
        }

        /// <summary>
        /// The target (end) of the laser beam. This target is assumed to be the AimTarget
        /// of the avatar. This transform will be moved by this component during
        /// <see cref="Update"/>. It is assumed that <see cref="Target"/>
        /// has a <see cref="ClientNetworkTransform"/> attached to it, which will then
        /// automatically broadcast the positions of all corresponding aim targets of all remote
        /// representations of this avatar.
        /// </summary>
        public Transform Target;

        /// <summary>
        /// Adds a <see cref="LaserPointer"/> if necessary and turns it on.
        /// Sets <see cref="HandBone"/> if it is not set."/>
        /// </summary>
        private void Awake()
        {
            Laser = gameObject.AddOrGetComponent<LaserPointer>();
            Laser.On = true;
            if (HandBone == null)
            {
                HandBone = gameObject.transform
                    .Find(
                        "CC_Base_BoneRoot/CC_Base_Hip/CC_Base_Waist/CC_Base_Spine01/CC_Base_Spine02/CC_Base_R_Clavicle/CC_Base_R_Upperarm/CC_Base_R_Forearm/CC_Base_R_Hand")
                    ?.transform;
            }

            if (HandBone == null)
            {
                Debug.LogError(
                    $"Hand bone not assigned in component {nameof(VRAvatarAimingSystem)} in game object {gameObject.FullName()}.\n");
            }
        }

        private GameObject lastLaserTarget;
        private float laserTargetStartTime;
        private readonly float hoverThreshold = 5.0f; 

        /// <summary>
        /// Retrieves the direction from the pointing device and aims the laser beam
        /// towards this direction. The position of <see cref="Target"/> is set to
        /// the end of the laser beam.
        /// Also distinguishes between locally controlled and remote players.
        /// If it's the remote player, <see cref="Laser.Draw(Vector3)"/> is called directly.
        ///
        /// In addition, for local players, this method tracks how long the laser pointer is
        /// directed at <see cref="InteractableObject"/>s. If the pointing duration exceeds a
        /// configurable threshold, the object is traced using OpenTelemetry for behavioral analysis.
        /// </summary>
        private void Update()
        {
            if (IsLocallyControlled)
            {
                if (HandBone != null)
                {
                    Vector3 direction = HandBone.rotation * Vector3.up;
                    // Move the aim target to the tip of the laser beam.
                    Target.position = Laser.PointTowards(direction);

                    // --- Laser Hover Tracking ---
                    if (Physics.Raycast(HandBone.position, direction, out RaycastHit hitInfo))
                    {
                        GameObject currentHit = hitInfo.collider.gameObject;

                        // Only track interactable objects
                        if (currentHit.GetComponent<InteractableObject>() != null)
                        {
                            if (currentHit != lastLaserTarget)
                            {
                                if (lastLaserTarget != null)
                                {
                                    float duration = Time.time - laserTargetStartTime;
                                    TracingHelperService.Instance?.TrackHoverDuration(lastLaserTarget, duration,hoverThreshold);
                                }

                                lastLaserTarget = currentHit;
                                laserTargetStartTime = Time.time;
                            }
                        }
                        else if (lastLaserTarget != null)
                        {
                            // Laser moved away from previous valid object
                            float duration = Time.time - laserTargetStartTime;
                            TracingHelperService.Instance?.TrackHoverDuration(lastLaserTarget, duration,hoverThreshold);

                            lastLaserTarget = null;
                        }
                    }
                    else if (lastLaserTarget != null)
                    {
                        float duration = Time.time - laserTargetStartTime;
                        TracingHelperService.Instance?.TrackHoverDuration(lastLaserTarget, duration,hoverThreshold);

                        lastLaserTarget = null;
                    }
                }
            }
            else
            {
                Laser.Draw(Target.position);
            }
        }
    }
}