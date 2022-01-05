using System.Diagnostics;
using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// Synchronizes the transform of a game object.
    /// </summary>
    public class TransformView : AbstractView
    {
        /// <summary>
        /// The delay after construction of this object, until the synchronization
        /// begins.
        /// </summary>
        public const float UpdateTimeStartDelay = 1.0f;

        /// <summary>
        /// The repeat rate of updating via network in seconds.
        /// </summary>
        public const float UpdateRepeatRate = 0.1f;



        /// <summary>
        /// The transform view to synchronize.
        /// </summary>
        [SerializeField] private Transform transformToSynchronize = null;

        /// <summary>
        /// Whether the position should be synchronized.
        /// </summary>
        [SerializeField] private bool synchronizePosition = false;

        /// <summary>
        /// Whether the rotation should be synchronized.
        /// </summary>
        [SerializeField] private bool synchronizeRotation = false;

        /// <summary>
        /// Whether the scale should be synchronized.
        /// </summary>
        [SerializeField] private bool synchronizeScale = false;

        /// <summary>
        /// Whether game objects should teleport for great distances.
        /// </summary>
        [SerializeField] private bool teleportForGreatDistances = true;

        /// <summary>
        /// The minimal distance, at which the transform is teleported, rather than
        /// smoothly moved. Is ignored, if <see cref="teleportForGreatDistances"/> is
        /// <code>false</code>.
        /// </summary>
        [SerializeField] private float teleportMinDistance = 8.0f;

        /// <summary>
        /// The squared minimal teleport distance.
        /// </summary>
        private float teleportMinDistanceSquared;



        /// <summary>
        /// The transform to synchronize.
        /// </summary>
        public Transform TransformToSynchronize => transformToSynchronize;

        /// <summary>
        /// Whether the position should be synchronized.
        /// </summary>
        public bool SynchronizingPosition => synchronizePosition;

        /// <summary>
        /// Whether the rotation should be synchronized.
        /// </summary>
        public bool SynchronizingRotation => synchronizeRotation;

        /// <summary>
        /// Whether the scale should be synchronized.
        /// </summary>
        public bool SynchronizingScale => synchronizeScale;



        /// <summary>
        /// Stopwatch for determining whether a positional update should be submitted.
        /// </summary>
        private readonly Stopwatch positionUpdateStopwatch = new Stopwatch();

        /// <summary>
        /// The previous received position. Used for linear interpolation between
        /// positions.
        /// </summary>
        private Vector3 positionLast;

        /// <summary>
        /// The newest received position. Used for linear interpolation between
        /// positions.
        /// </summary>
        private Vector3 positionNext;



        /// <summary>
        /// Stopwatch for determining whether a rotational update should be submitted.
        /// </summary>
        private readonly Stopwatch rotationUpdateStopwatch = new Stopwatch();

        /// <summary>
        /// The previous received rotation. Used for linear interpolation between
        /// rotations.
        /// </summary>
        private Quaternion rotationLast;

        /// <summary>
        /// The newest received rotation. Used for linear interpolation between
        /// rotations.
        /// </summary>
        private Quaternion rotationNext;



        /// <summary>
        /// Stopwatch for determining whether a scale update should be submitted.
        /// </summary>
        private readonly Stopwatch scaleUpdateStopwatch = new Stopwatch();

        /// <summary>
        /// The previous received scale. Used for linear interpolation between scales.
        /// </summary>
        private Vector3 scaleLast;

        /// <summary>
        /// The newest received scale. Used for linear interpolation between scales.
        /// </summary>
        private Vector3 scaleNext;



        /// <summary>
        /// Initializes the transform view to invoke the update function every
        /// <see cref="UpdateRepeatRate"/> seconds.
        /// </summary>
        protected override void InitializeImpl()
        {
            if (viewContainer.IsOwner() && !Network.UseInOfflineMode)
            {
                if (synchronizePosition)
                {
                    InvokeRepeating(nameof(SynchronizePosition), UpdateTimeStartDelay, UpdateRepeatRate);
                }
                if (synchronizeRotation)
                {
                    InvokeRepeating(nameof(SynchronizeRotation), UpdateTimeStartDelay, UpdateRepeatRate);
                }
                if (synchronizeScale)
                {
                    InvokeRepeating(nameof(SynchronizeScale), UpdateTimeStartDelay, UpdateRepeatRate);
                }
            }
            teleportMinDistanceSquared = teleportMinDistance * teleportMinDistance;
        }

        /// <summary>
        /// Is this client is not the owner, the transform is updated given the last two
        /// received states of the transforms position/rotation/scale.
        /// </summary>
        protected override void UpdateImpl()
        {
            if (viewContainer != null && !viewContainer.IsOwner())
            {
                if (synchronizePosition)
                {
                    if (teleportForGreatDistances && Vector3.SqrMagnitude(positionNext - positionLast) >= teleportMinDistanceSquared)
                    {
                        transformToSynchronize.position = positionNext;
                    }
                    else
                    {
                        transformToSynchronize.position = Vector3.LerpUnclamped(
                            positionLast,
                            positionNext,
                            (float)(positionUpdateStopwatch.Elapsed.TotalSeconds / UpdateRepeatRate)
                        );
                    }
                }
                if (synchronizeRotation)
                {
                    transformToSynchronize.rotation = Quaternion.Slerp(
                        rotationLast,
                        rotationNext,
                        (float)(rotationUpdateStopwatch.Elapsed.TotalSeconds / UpdateRepeatRate)
                    );
                }
                if (synchronizeScale)
                {
                    transformToSynchronize.localScale = Vector3.Lerp(
                        scaleLast,
                        scaleNext,
                        (float)(scaleUpdateStopwatch.Elapsed.TotalSeconds / UpdateRepeatRate)
                    );
                }
            }
        }



        /// <summary>
        /// Sets the next desired position of the transform. This must only ever be
        /// called by <see cref="TransformViewPositionAction"/>!
        /// </summary>
        /// <param name="nextPosition">The desired position of the transform.</param>
        internal void SetNextPosition(Vector3 nextPosition)
        {
            positionUpdateStopwatch.Restart();
            positionLast = transformToSynchronize.position;
            positionNext = nextPosition;
        }

        /// <summary>
        /// Sets the next desired rotation of the transform. This must only ever be
        /// called by <see cref="TransformViewRotationAction"/>!
        /// </summary>
        /// <param name="nextRotation">The desired rotation of the transform.</param>
        internal void SetNextRotation(Quaternion nextRotation)
        {
            rotationUpdateStopwatch.Restart();
            rotationLast = transformToSynchronize.rotation;
            rotationNext = nextRotation;
        }

        /// <summary>
        /// Sets the next desired scale of the transform. This must only ever be called
        /// by <see cref="TransformViewScaleAction"/>!
        /// </summary>
        /// <param name="nextScale">The desired scale of the transform.</param>
        internal void SetNextScale(Vector3 nextScale)
        {
            scaleUpdateStopwatch.Restart();
            scaleLast = transformToSynchronize.localScale;
            scaleNext = nextScale;
        }

        /// <summary>
        /// Executes a new action for position synchronization.
        /// </summary>
        private void SynchronizePosition()
        {
            new TransformViewPositionAction(this, transformToSynchronize.position).Execute();
        }

        /// <summary>
        /// Executes a new action for rotation synchronization.
        /// </summary>
        private void SynchronizeRotation()
        {
            new TransformViewRotationAction(this, transformToSynchronize.rotation).Execute();
        }

        /// <summary>
        /// Executes a new action for scale synchronization.
        /// </summary>
        private void SynchronizeScale()
        {
            new TransformViewScaleAction(this, transformToSynchronize.localScale).Execute();
        }
    }

}
