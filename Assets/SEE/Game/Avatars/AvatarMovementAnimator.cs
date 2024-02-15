using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Animates the movements of the avatar this component is attached to
    /// based on the velocity and direction of the avatar's movement.
    /// </summary>
    internal class AvatarMovementAnimator : MonoBehaviour
    {
        /// <summary>
        /// The animator attached to the game object (the avatar) that is used to
        /// animate the movements of the avatar. This animator is assumed to have
        /// the two parameters <see cref="forwardSpeedParameterName"/> and
        /// <see cref="sidewardSpeedParameterName"/>.
        /// </summary>
        [Tooltip("Reference to the animator component.")]
        public Animator Animator;

        /// <summary>
        /// The name of the animator's parameter for forward speed.
        /// </summary>
        private const string forwardSpeedParameterName = "ForwardSpeed";
        /// <summary>
        /// The name of the animator's parameter for sideward speed.
        /// </summary>
        private const string sidewardSpeedParameterName = "SidewardSpeed";

        /// <summary>
        /// The parameter of the <see cref="Animator"/> controller for the forward speed,
        /// namely, <see cref="forwardSpeedParameterName"/>.
        /// </summary>
        private int forwardSpeedParameter;
        /// <summary>
        /// The parameter of the <see cref="Animator"/> controller for the sideward speed,
        /// namely, <see cref="sidewardSpeedParameterName"/>.
        /// </summary>
        private int sidewardSpeedParameter;

        /// <summary>
        /// Position of the avatar at the <see cref="LastUpdate"/> in world space.
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// If <see cref="Animator"/> is defined, initializes the two animator-controller
        /// parameters <see cref="forwardSpeedParameter"/> and <see cref="sidewardSpeedParameter"/>
        /// and sets <see cref="lastPosition"/> to the current position of the avatar.
        /// If <see cref="Animator"/> is undefined, emits an error message and disables this
        /// component.
        /// </summary>
        private void Start()
        {
            if (Animator)
            {
                forwardSpeedParameter = Animator.StringToHash(forwardSpeedParameterName);
                sidewardSpeedParameter = Animator.StringToHash(sidewardSpeedParameterName);
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
            Animator.SetFloat(forwardSpeedParameter, localSpeed.z);
            Animator.SetFloat(sidewardSpeedParameter, localSpeed.x);
            lastPosition = transform.position;
        }
    }
}
