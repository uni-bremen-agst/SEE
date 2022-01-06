using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Animates the movements of the avatar this component is attached to
    /// based on the velocity and direction of the avatar's movement.
    /// </summary>
    class AvatarMovementAnimator : MonoBehaviour
    {
        /// <summary>
        /// The animator attached to the game object (the avatar) that is used to
        /// animate the movements of the avatar. This animator is assumed to have
        /// the two parameters <see cref="ForwardSpeedParameterName"/> and
        /// <see cref="SidewardSpeedParameterName"/>.
        /// </summary>
        [Tooltip("Reference to the animator component.")]
        public Animator animator;

        /// <summary>
        /// The name of the animator's parameter for forward speed.
        /// </summary>
        private const string ForwardSpeedParameterName = "ForwardXSpeed";
        /// <summary>
        /// The name of the animator's parameter for sideward speed.
        /// </summary>
        private const string SidewardSpeedParameterName = "SidewardSpeed";

        /// <summary>
        /// The parameter of the <see cref="animator"/> controller for the forward speed,
        /// namely, <see cref="ForwardSpeedParameterName"/>.
        /// </summary>
        private int forwardSpeedParameter;
        /// <summary>
        /// The parameter of the <see cref="animator"/> controller for the sideward speed,
        /// namely, <see cref="SidewardSpeedParameterName"/>.
        /// </summary>
        private int sidewardSpeedParameter;

        /// <summary>
        /// Position of the avatar at the <see cref="LastUpdate"/> in world space.
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// If <see cref="animator"/> is defined, initializes the two animator-controller
        /// parameters <see cref="forwardSpeedParameter"/> and <see cref="sidewardSpeedParameter"/>
        /// and sets <see cref="lastPosition"/> to the current position of the avatar.
        /// If <see cref="animator"/> is undefined, emits an error message and disables this
        /// component.
        /// </summary>
        private void Start()
        {
            if (animator)
            {
                forwardSpeedParameter = Animator.StringToHash(ForwardSpeedParameterName);
                sidewardSpeedParameter = Animator.StringToHash(SidewardSpeedParameterName);
                lastPosition = transform.position;
            }
            else
            {
                Debug.LogError($"No animator assigned to {name}.\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Sets the two animator controller parameters <see cref="forwardSpeedParameter"/>
        /// and <see cref="sidewardSpeedParameter"/> according to the movement of the
        /// avatar relative to its <see cref="lastPosition"/>.
        /// We do that in <see cref="LastUpdate"/> so that we can be sure that
        /// the avatar's new position has been set already.
        /// </summary>
        private void LateUpdate()
        {
            Vector3 localSpeed = transform.InverseTransformDirection(transform.position - lastPosition) / Time.deltaTime;
            animator.SetFloat(forwardSpeedParameter, localSpeed.z);
            animator.SetFloat(sidewardSpeedParameter, localSpeed.x);
            lastPosition = transform.position;
        }
    }
}
