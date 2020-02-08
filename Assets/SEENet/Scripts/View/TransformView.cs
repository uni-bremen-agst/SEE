using SEE.Net.Internal;
using System;
using System.Diagnostics;
using UnityEngine;

namespace SEE.Net
{

    public class TransformView : View
    {
        public const float UPDATE_TIME_START_OFFSET = 1.0f;
        public const float UPDATE_TIME_VALUE = 0.1f;

        [SerializeField] private Transform transformToSynchronize = null;
        [SerializeField] private bool synchronizePosition = false;
        [SerializeField] private bool synchronizeRotation = false;
        [SerializeField] private bool synchronizeScale = false;

        public Transform TransformToSynchronize { get => transformToSynchronize; }
        public bool SynchronizingPosition { get => synchronizePosition; }
        public bool SynchronizingRotation { get => synchronizeRotation; }
        public bool SynchronizingScale { get => synchronizeScale; }

        private Stopwatch positionUpdateStopwatch = new Stopwatch();
        private Vector3 positionLast;
        private Vector3 positionNext;

        private Stopwatch rotationUpdateStopwatch = new Stopwatch();
        private Quaternion rotationLast;
        private Quaternion rotationNext;

        private Stopwatch scaleUpdateStopwatch = new Stopwatch();
        private Vector3 scaleLast;
        private Vector3 scaleNext;

        protected override void InitializeImpl()
        {
            if (viewContainer.IsOwner() && !Network.UseInOfflineMode)
            {
                if (synchronizePosition)
                {
                    InvokeRepeating("SynchronizePosition", UPDATE_TIME_START_OFFSET, UPDATE_TIME_VALUE);
                }
                if (synchronizeRotation)
                {
                    InvokeRepeating("SynchronizeRotation", UPDATE_TIME_START_OFFSET, UPDATE_TIME_VALUE);
                }
                if (synchronizeScale)
                {
                    InvokeRepeating("SynchronizeScale", UPDATE_TIME_START_OFFSET, UPDATE_TIME_VALUE);
                }
            }
        }
        protected override void UpdateImpl()
        {
            if (!viewContainer.IsOwner())
            {
                if (synchronizePosition)
                {
                    transformToSynchronize.position = Vector3.LerpUnclamped(
                        positionLast,
                        positionNext,
                        (float)(positionUpdateStopwatch.Elapsed.TotalSeconds / UPDATE_TIME_VALUE)
                    );
                }
                if (synchronizeRotation)
                {
                    transformToSynchronize.rotation = Quaternion.SlerpUnclamped(
                        rotationLast,
                        rotationNext,
                        (float)(rotationUpdateStopwatch.Elapsed.TotalSeconds / UPDATE_TIME_VALUE)
                    );
                }
                if (synchronizeScale)
                {
                    transformToSynchronize.localScale = Vector3.LerpUnclamped(
                        scaleLast,
                        scaleNext,
                        (float)(scaleUpdateStopwatch.Elapsed.TotalSeconds / UPDATE_TIME_VALUE)
                    );
                }
            }
        }

        public void SetNextPosition(Vector3 nextPosition)
        {
            positionUpdateStopwatch.Restart();
            positionLast = transformToSynchronize.position;
            positionNext = nextPosition;
        }
        public void SetNextRotation(Quaternion nextRotation)
        {
            rotationUpdateStopwatch.Restart();
            rotationLast = transformToSynchronize.rotation;
            rotationNext = nextRotation;
        }
        public void SetNextScale(Vector3 nextScale)
        {
            scaleUpdateStopwatch.Restart();
            scaleLast = transformToSynchronize.localScale;
            scaleNext = nextScale;
        }
        private void SynchronizePosition()
        {
            Network.Send(
                Client.Connection,
                Server.PACKET_PREFIX + TransformViewPositionPacketData.PACKET_NAME,
                new TransformViewPositionPacketData(this, transformToSynchronize.position, DateTime.Now).Serialize()
            );
        }
        private void SynchronizeRotation()
        {
            Network.Send(
                Client.Connection,
                Server.PACKET_PREFIX + TransformViewRotationPacketData.PACKET_NAME,
                new TransformViewRotationPacketData(this, transformToSynchronize.rotation, DateTime.Now).Serialize()
            );
        }
        private void SynchronizeScale()
        {
            Network.Send(
                Client.Connection,
                Server.PACKET_PREFIX + TransformViewScalePacketData.PACKET_NAME,
                new TransformViewScalePacketData(this, transformToSynchronize.localScale, DateTime.Now).Serialize()
            );
        }
    }

}
