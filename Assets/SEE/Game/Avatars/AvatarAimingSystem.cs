using UnityEngine;
using RootMotion.FinalIK;
using SEE.GO;
using SEE.Utils;
using System;
using SEE.Controls;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A 360-degree aiming system based on Final IK and built with six static aiming poses and
    /// AimIK.
    ///
    /// This code is based on <see cref="RootMotion.Demos.SimpleAimingSystem"/> and adapted
    /// to our needs. Unlike <see cref="RootMotion.Demos.SimpleAimingSystem"/>, this code
    /// provides a means to unaim.
    /// </summary>
    public class AvatarAimingSystem : MonoBehaviour
    {
        [Tooltip("AimPoser is a tool that returns an animation name based on direction.")]
        // TODO: Make it private and search this in the scene.
        public AimPoser AimPoser;

        [Tooltip("Reference to the AimIK component.")]
        public AimIK Aim;

        [Tooltip("Reference to the LookAt component (only used for the head in this instance).")]
        public LookAtIK LookAt;

        [Tooltip("Reference to the Animator component.")]
        public Animator Animator;

        [Tooltip("Time of cross-fading from pose to pose.")]
        public float CrossfadeTime = 0.2f;

        [Tooltip("Will keep the aim target at a distance.")]
        public float MinimalAimDistance = 0.1f;

        /// <summary>
        /// Whether the avatar is right-handed. The value of it decides whether
        /// the aiming object becomes a part of the right or left hand.
        /// NOTE: Although this code allows to make this distinction, there is
        /// more to support left-handedness. The whole chain of bones controlled
        /// by Final IK as well as the animations must be adjusted, too.
        /// Until then, this parameter remains a constant.
        /// </summary>
        [Tooltip("Whether the avatar is right-handed.")]
        private const bool RightHanded = true;

        [Tooltip("The world-space scale of the laser beam for pointing.")]
        public Vector3 LaserScale = new Vector3(0.01f, 0.01f, 1.5f);

        [Tooltip("The color of the laser beam for pointing.")]
        public Color LaserColor = Color.red;

        /// <summary>
        /// Whether the avatar is currently pointing, i.e., whether it has an aiming or looking target.
        /// </summary>
        [Tooltip("If true, the avatar is currently pointing. Its pose will be adjusted according to the aimed target.")]
        public bool IsPointing = false;

        [Tooltip("If true, local interactions control where the avatar is pointing to.")]
        public bool LocallyControlled = true;

        /// <summary>
        /// The transform that represents the search point, i.e., the
        /// object becoming the aiming target when the avatar is scanning for
        /// anything relevant. This is not to be confused with the aiming
        /// transform. The latter will be the object that will be moved
        /// and rotated so that it aims at <see cref="AimedTarget"/>.
        /// So, we have an aim, i.e., <see cref="AimedTarget"/>, and another
        /// transform aiming at it.
        ///
        /// This attribute is made public so that clients can modify the
        /// search point's position. This way local aiming changes can
        /// be propagated through the network.
        ///
        /// Note: If clients set this <see cref="AimedTarget"/>, <see cref="LocallyControlled"/>
        /// should be <code>false</code>; otherwise their changes will be overridden
        /// by local user interactions.
        /// </summary>
        [Tooltip("The avatar's aimed target.")]
        public Transform AimedTarget;

        /// <summary>
        /// The pose of the <see cref="AimPoser"/> while aiming at the target.
        /// </summary>
        private AimPoser.Pose aimPose;

        /// <summary>
        /// The last <see cref="aimPose"/> stored so we know if <see cref="aimPose"/> changed.
        /// </summary>
        private AimPoser.Pose lastPose;

        /// <summary>
        /// The <see cref="AimIK"/> component attached to this avatar. It is used for aiming.
        /// The aiming target is <see cref="aimIK.solver.target"/>.
        /// </summary>
        private AimIK aimIK;

        /// <summary>
        /// The <see cref="LookAtIK"/> component attached to this avatar. It is used for looking
        /// at a particular target (specified by <see cref="lookAtIK.solver.target"/>).
        /// </summary>
        private LookAtIK lookAtIK;

        /// <summary>
        /// Toggles between pointing and not pointing.
        /// </summary>
        public void TogglePointing()
        {
            IsPointing = !IsPointing;
            // Laser beam should be active only while we are pointing.
            laser.SetActive(IsPointing);
        }

        /// <summary>
        /// The laser beam for aiming.
        /// </summary>
        private GameObject laser;

        /// <summary>
        /// Disables the IK components <see cref="Aim"/> and <see cref="LookAt"/>
        /// so that we can manage their updating order by ourselves. Sets
        /// <see cref="aimIK"/>, <see cref="originalAimTarget"/>, <see cref="lookAtIK"/>,
        /// <see cref="originalLookTarget"/>, and <see cref="IsPointing"/>.
        /// </summary>
        private void Start()
        {
            /// We are disabling <see cref="Aim"/> and <see cref="LookAt"/> so that
            /// we can control their update cycle ourselves.
            Aim.enabled = false;
            LookAt.enabled = false;
            gameObject.TryGetComponentOrLog(out aimIK);
            gameObject.TryGetComponentOrLog(out lookAtIK);

            /// The object that is used for the aiming. It will be moved and
            /// rotated by <see cref="aimIK"/> so that it aims at <see cref="AimedTarget"/>.
            (aimIK.solver.transform, laser) = CreateAimTransform();

            // The aim itself.
            AimedTarget = CreateSearchPoint();
            AimedTarget.SetParent(gameObject.transform);
            MoveSearchPoint();

            if (aimIK.solver.target == null)
            {
                aimIK.solver.target = AimedTarget;
            }
            if (lookAtIK.solver.target == null)
            {
                lookAtIK.solver.target = AimedTarget;
            }

            // Returns a game object that represents the search point, i.e., the
            // object becoming the aiming target when the avatar is scanning for
            // anything relevant.
            Transform CreateSearchPoint()
            {
                if (true)
                {
                    return new GameObject("Search Point").transform;
                }
                else
                {
                    // for debugging: the search point is a visible object.
                    GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    result.name = "Search Point";
                    result.transform.localScale = Vector3.one / 10;
                    return result.transform;
                }
            }
        }

        /// <summary>
        /// The name of the game object (child of avatar) representing the right hand.
        /// </summary>
        private const string RightHandName = "RightHand";
        /// <summary>
        /// The name of the game object (child of avatar) representing the left hand.
        /// </summary>
        private const string LeftHandName = "LeftHand";

        /// <summary>
        /// Creates and returns the IK aiming transform and a nested laser pointer. The IK
        /// aiming object is the object that will be moved and rotated by <see cref="aimIK"/>
        /// to aim towards <see cref="AimedTarget"/>.
        /// It consists of an invisible game objects with a visible laser pointer as a child.
        /// The IK aiming transform itself will be created as a child of either
        /// the avatar's hand named either <see cref="RightHandName"/> or <see cref="LeftHandName"/>
        /// depending upon <see cref="RightHanded"/>,
        /// </summary>
        /// <returns>the new IK aiming transform and laser beam</returns>
        /// <exception cref="Exception">thrown if the avatar does not have a hand named
        /// <see cref="RightHandName"/> or <see cref="LeftHandName"/>, respectively</exception>
        private (Transform, GameObject) CreateAimTransform()
        {
            string handName = RightHanded ? RightHandName : LeftHandName;
            Transform hand = gameObject.Ancestor(handName).transform;
            if (hand == null)
            {
                throw new Exception($"Avatar {name} does not have a {handName}.");
            }
            GameObject aimingObject = CreateAimingObject(hand);
            return (aimingObject.transform, CreateLaserBeam(aimingObject));
        }

        /// <summary>
        /// Creates and returns the aiming object (the object used by <see cref="aimIK"/>
        /// to aim towards <see cref="AimedTarget"/>. This object will become a child
        /// of the given <paramref name="hand"/>. It is positioned just before the
        /// hand. The object is invisible, that is, its renderer is turned off,
        /// and has no collider. Its whole purpose is to have a transform that can
        /// used for aiming very close to the <paramref name="hand"/>.
        ///
        /// </summary>
        /// <param name="hand">hand in which to create the aiming object</param>
        /// <returns>the aiming object</returns>
        private static GameObject CreateAimingObject(Transform hand)
        {
            // The aiming transform.
            GameObject aimingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aimingObject.name = "AimTransform";
            aimingObject.transform.SetParent(hand.transform);
            // The following values of the rotation, position, and scale have been determined
            // by trial and error.
            aimingObject.transform.localEulerAngles = new Vector3(180, 80, 80);
            aimingObject.transform.localPosition = new Vector3(-0.2506f, 0.0538f, -0.0412f);
            aimingObject.transform.localScale = new Vector3(0.1f, 0.05f, 0.01f);
            // The aiming object should be invisible. We have the laser pointer instead.
            aimingObject.GetComponent<Renderer>().enabled = false;
            Destroy(aimingObject.GetComponent<Collider>());
            return aimingObject;
        }

        /// <summary>
        /// Creates and returns a laser beam as a child of <paramref name="aimingObject"/>.
        /// Its scale will be determined by <see cref="LaserScale"/> and its color by
        /// <see cref="LaserColor"/>. Whether the laser beam is active initially is
        /// determined by <see cref="IsPointing"/>.
        /// </summary>
        /// <param name="aimingObject">the IK aiming object</param>
        private GameObject CreateLaserBeam(GameObject aimingObject)
        {
            // The laser beam.
            GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laser.name = "Laser";
            // We set the scale before laser is turned into a child so that we
            // have a world-space scale.
            laser.transform.localScale = LaserScale;
            laser.transform.SetParent(aimingObject.transform);
            laser.transform.localEulerAngles = Vector3.zero;
            laser.transform.localPosition = new Vector3(0, 0, laser.transform.localScale.z / 2);
            laser.GetComponent<Renderer>().material.color = LaserColor;
            laser.SetActive(IsPointing);
            return laser;
        }

        /// <summary>
        /// If <see cref="KeyCode.P"/> is entered, toggles pointing.
        /// </summary>
        private void Update()
        {
            if (SEEInput.IsPointing())
            {
                TogglePointing();
            }
            if (LocallyControlled)
            {
                MoveSearchPoint();
            }
        }

        /// <summary>
        /// Moves <see cref="AimedTarget"/> to the end point of the laser beam,
        /// i.e., the point where the user is currently pointing to.
        /// </summary>
        private void MoveSearchPoint()
        {
            // TODO: We need a solution for VR and other environments, too.
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
            AimedTarget.position = ray.origin + ray.direction * LaserScale.z;
        }

        /// <summary>
        /// If <see cref="IsPointing"/>, adjusts the pose and updates the solver of <see cref="Aim"/>
        /// and <see cref="LookAt"/> to aim at the target; otherwise a neutral pose will be taken.
        /// </summary>
        private void LateUpdate()
        {
            if (IsPointing)
            {
                // Switch aim poses (Legacy animation)
                Pose();

                // Update IK solvers
                Aim.solver.Update();
                if (LookAt != null)
                {
                    LookAt.solver.Update();
                }
            }
            else
            {
                // Pointing down is our neutral pose.
                AimTowards(Vector3.down);
            }
        }

        /// <summary>
        /// Updates the pose of the avatar so that the avatar is pointing towards <see cref="AimedTarget"/>.
        /// </summary>
        private void Pose()
        {
            // Make sure aiming target is not too close (might make the solver instable when the
            // target is closer to the first bone than the last bone is).
            LimitAimTarget();

            // Get the aiming direction
            Vector3 direction = Aim.solver.IKPosition - Aim.solver.bones[0].transform.position;

            // Getting the direction relative to the root transform
            Vector3 localDirection = transform.InverseTransformDirection(direction);

            AimTowards(localDirection);
        }

        /// <summary>
        /// Takes a pose aiming towards <paramref name="localDirection"/> (cross-fading towards
        /// this direction).
        /// </summary>
        /// <param name="localDirection">direction to aim at relative to the avatar</param>
        private void AimTowards(Vector3 localDirection)
        {
            // Get the Pose from AimPoser
            aimPose = AimPoser.GetPose(localDirection);

            // If the Pose has changed
            if (aimPose != lastPose)
            {
                // Increase the angle buffer of the pose so we won't switch back too soon if
                // the direction changes a bit.
                AimPoser.SetPoseActive(aimPose);

                // Store the pose so we know if it changes.
                lastPose = aimPose;
            }

            // Direct blending
            foreach (AimPoser.Pose pose in AimPoser.poses)
            {
                if (pose == aimPose)
                {
                    DirectCrossFade(pose.name, 1f);
                }
                else
                {
                    DirectCrossFade(pose.name, 0f);
                }
            }
        }

        /// <summary>
        /// Makes sure the aiming target is not too close (might make the solver instable when
        /// the target is closer to the first bone than the last bone is).
        /// </summary>
        private void LimitAimTarget()
        {
            Vector3 aimFrom = Aim.solver.bones[0].transform.position;
            Vector3 direction = Aim.solver.IKPosition - aimFrom;
            direction = direction.normalized * Mathf.Max(direction.magnitude, MinimalAimDistance);
            Aim.solver.IKPosition = aimFrom + direction;
        }

        /// <summary>
        /// Uses Mecanim's Direct blend trees for cross-fading.
        /// </summary>
        /// <param name="pose">the pose parameter of the <see cref="Animator"/></param>
        /// <param name="target">target to be reached</param>
        private void DirectCrossFade(string pose, float target)
        {
            float newStateValue = Mathf.MoveTowards(Animator.GetFloat(pose), target, Time.deltaTime * (1f / CrossfadeTime));
            Animator.SetFloat(pose, newStateValue);
        }
    }
}
