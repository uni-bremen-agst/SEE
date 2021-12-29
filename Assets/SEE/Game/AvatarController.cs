using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

namespace SEE.Game
{
    /// <summary>
    /// Controls the behavior of an avatar. This component is intended
    /// to be added to a UMA character.
    /// </summary>
    public class AvatarController : MonoBehaviour
    {
        /// <summary>
        /// The agent of the navigation mesh used to move the avatar
        /// towards a destination without avoiding static obstacles.
        /// </summary>
        private NavMeshAgent agent;

        /// <summary>
        /// The animator of the controlled avatar.
        /// </summary>
        private Animator animator;

        /// <summary>
        /// The world-space destination the avatar should reach.
        /// </summary>
        [Tooltip("The destination of the avatar")]
        public Vector3 Destination;

        /// <summary>
        /// The rotation angle of the avatar along the y axis (world space) in degree.
        /// Assumed to be in [0, 360).
        /// </summary>
        [Tooltip("The rotation of the avatar along the y axis in degree"), Range(0, 359)]
        public int Rotation = 90;

        /// <summary>
        /// The unique ID of the Speed parameter of the <see cref="animator"/>.
        /// </summary>
        private int SpeedID;

        /// <summary>
        /// The speed of the movement animation. Note: This is not the speed by
        /// which the avatar actually moves, it is just the speed of the walk
        /// animation being played while the animator walks.
        /// </summary>
        private const float AnimationSpeed = 0.5f;

        /// <summary>
        /// The angle step for each call to <see cref="FixedUpdate"/> to be
        /// added to or removed from, respectively, from the current y rotation
        /// of the avatar to eventually reach <see cref="Rotation"/>.
        /// </summary>
        private float rotationStepPerFixedUpdate;

        /// <summary>
        /// The number of seconds for a complete rotation of 360 degrees.
        /// </summary>
        private const float secondsForCompleteRotation = 3;

        /// <summary>
        /// Sets <see cref="SpeedID"/>.
        /// </summary>
        private void Awake()
        {
            SpeedID = Animator.StringToHash("Speed");
        }

        /// <summary>
        /// Sets the desination and <see cref="rotationStepPerFixedUpdate"/>.
        ///
        /// Disables this component if on instance of <see cref="NavMeshAgent"/> is attached
        /// to the game object.
        /// </summary>
        void Start()
        {
            if (TryGetComponent(out agent))
            {
                agent.destination = Destination;
                float callsPerSecond = 1 / Time.fixedDeltaTime;
                rotationStepPerFixedUpdate = (360 / secondsForCompleteRotation) / callsPerSecond;
            }
            else
            {
                Debug.LogError($"Game object {name} has no {nameof(agent)} attached to it.\n");
                enabled = false;
            }
            // Test();
        }

        /// <summary>
        /// Returns true if one should turn right (go clockwise) to turn from the
        /// <paramref name="startAngle"/> to the <paramref name="targetAngle"/> on
        /// the shortest path. If <paramref name="startAngle"/> and <paramref name="targetAngle"/>
        /// are the same, true will be returned.
        ///
        /// Precondition: <paramref name="startAngle"/> and <paramref name="targetAngle"/>
        /// must be in the range [0, 360).
        /// </summary>
        /// <param name="startAngle">angle to start from in the range [0, 360)</param>
        /// <param name="targetAngle">angle to reach in the range [0, 360)</param>
        /// <returns>true if one should clockwise to take the shortest route</returns>
        public static bool GoClockwise(float startAngle, float targetAngle)
        {
            float alpha = targetAngle - startAngle;
            float beta = targetAngle - startAngle + 360;
            float gamma = targetAngle - startAngle - 360;

            // Now, whichever of |alpha|, |beta|, and |gamma| is the smallest tells us which
            // of alpha, beta, and gamma is relevant and if the one with the smallest
            // absolute value is positive, go clockwise, and if it's negative, go
            // counterclockwise.
            float absAlpha = Mathf.Abs(alpha);
            float betaAlpha = Mathf.Abs(beta);
            float gammaAlpha = Mathf.Abs(gamma);

            if (absAlpha <= betaAlpha && absAlpha <= gammaAlpha)
            {
                return alpha >= 0;
            }
            else if (betaAlpha <= absAlpha && betaAlpha <= gammaAlpha)
            {
                return beta >= 0;
            }
            else
            {
                return gamma >= 0;
            }
        }

        /// <summary>
        /// Tests of <see cref="GoClockwise(float, float)"/>.
        /// </summary>
        private static void Test()
        {
            UnityEngine.Assertions.Assert.IsTrue(GoClockwise(90, 180));
            UnityEngine.Assertions.Assert.IsFalse(GoClockwise(180, 90));
            UnityEngine.Assertions.Assert.IsTrue(GoClockwise(90, 90));
            UnityEngine.Assertions.Assert.IsTrue(GoClockwise(300, 10));
            UnityEngine.Assertions.Assert.IsFalse(GoClockwise(10, 300));
            UnityEngine.Assertions.Assert.IsTrue(GoClockwise(0, 0));
            UnityEngine.Assertions.Assert.IsTrue(GoClockwise(0, 1));
            UnityEngine.Assertions.Assert.IsFalse(GoClockwise(1, 0));
            UnityEngine.Assertions.Assert.IsFalse(GoClockwise(1, 359));
            UnityEngine.Assertions.Assert.IsTrue(GoClockwise(0.3905f, 90));
        }

        /// <summary>
        /// Updates <see cref="agent.destination"/> if <see cref="Destination"/> changed.
        /// While <see cref="Destination"/> is not reached, sets the speed of the <see cref="animator"/>
        /// to <see cref="AnimationSpeed"/>.
        /// </summary>
        void Update()
        {
            /// In case our <see cref="Destination"/> changed, we need to update the <see cref="agent"/>.
            if (agent.destination != Destination)
            {
                agent.destination = Destination;
            }

            if (animator != null || TryGetComponent(out animator))
            {
                // Speed of the animation must be set anew at every Update().
                if (FinalDestinationReached())
                {
                    // Destination is considered to be reached.
                    animator.SetFloat(id: SpeedID, 0);
                }
                else
                {
                    animator.SetFloat(id: SpeedID, AnimationSpeed);
                }
            }
        }

        /// <summary>
        /// True if the game object has reached its final <see cref="Destination"/>.
        /// </summary>
        /// <returns>True if the game object has reached its <see cref="Destination/"></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FinalDestinationReached()
        {
            return Vector3.Distance(transform.position, Destination) < 0.05f;
        }

        /// <summary>
        /// Called every <see cref="Time.fixedDeltaTime"/> by Unity (Physics system).
        /// If the avatar is currently not moving and not rotated to <see cref="Rotation"/>,
        /// the avatar is rotated towards <see cref="Rotation"/> on the shortest route
        /// by <see cref="rotationStepPerFixedUpdate"/> degrees.
        /// </summary>
        private void FixedUpdate()
        {
            // Rotate the avatar only if it is currently not moving.
            if (FinalDestinationReached())
            {
                float actualRotation = (transform.rotation.eulerAngles.y + 360) % 360;
                Rotation %= 360;
                float angleDistance = Mathf.Abs(actualRotation - Rotation);
                if (angleDistance > 0.01f)
                {
                    // We have not reached the target rotation.
                    if (angleDistance <= rotationStepPerFixedUpdate)
                    {
                        /// We are very close but not yet there, however, closer than
                        /// <see cref="rotationStepPerFixedUpdate"/>. Adding
                        /// <see cref="rotationStepPerFixedUpdate"/> to the avatar's
                        /// current rotation would go beyond the wanted <see cref="Rotation"/>.
                        /// Hence, we take the last minimal step by simply using the
                        /// requesting <see cref="Rotation"/>.
                        Vector3 rotation = transform.rotation.eulerAngles;
                        rotation.y = Rotation;
                        transform.rotation = Quaternion.Euler(rotation);
                    }
                    else
                    {
                        // Should we turn left or right? We will use the shorter direction.
                        int direction = GoClockwise(startAngle: actualRotation, targetAngle: Rotation) ? 1 : -1;
                        float angleDelta = direction * rotationStepPerFixedUpdate;
                        transform.Rotate(xAngle: 0, yAngle: angleDelta, zAngle: 0, relativeTo: Space.Self);
                    }
                }
            }
        }
    }
}