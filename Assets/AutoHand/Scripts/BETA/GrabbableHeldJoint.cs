using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand
{
    [RequireComponent(typeof(Grabbable)), DefaultExecutionOrder(10)]
    public class GrabbableHeldJoint : MonoBehaviour
    {
        [Tooltip("The grabbable that this grabbable is connected to")]
        public Grabbable connectedGrabbable;
        [Tooltip("Offsets the center of this joint")]
        public Vector3 pivotOffset;
        [Tooltip("This will multiply the hands strength while holding this grabbable to give it more or less positional priority while holding this joint as a second hand (good to reduce when you dont want this joint having movement priority while being held)")]
        public float heldMassScale = 1f;
        [Space]
        [Tooltip("The maximum distance this joint is allowed to move in the local positive x-axis")]
        public float xMaxLimit = 0f;
        [Tooltip("The maximum distance this joint is allowed to move in the local negative x-axis")]
        public float xMinLimit = 0f;
        [Tooltip("This will force the joint back into its center position based on the given spring strength while not being held along this axis")]
        public float xSpring = 0f;
        [Space]
        [Tooltip("The maximum distance this joint is allowed to move in the local positive y-axis")]
        public float yMaxLimit = 0f;
        [Tooltip("The maximum distance this joint is allowed to move in the local negative y-axis")]
        public float yMinLimit = 0f;
        [Tooltip("This will force the joint back into its center position based on the given spring strength while not being held along this axis")]
        public float ySpring = 0f;
        [Space]
        [Tooltip("The maximum distance this joint is allowed to move in the local positive z-axis")]
        public float zMaxLimit = 0f;
        [Tooltip("The maximum distance this joint is allowed to move in the local negative z-axis")]
        public float zMinLimit = 0f;
        [Tooltip("This will force the joint back into its center position based on the given spring strength while not being held along this axis")]
        public float zSpring = 0f;

        [Space]
        [Range(0, 1), Tooltip("The percentage from the min/max distance needed to trigger the event, good for creating a buffer for the event to trigger slightly before the max range to help prevent missed event")]
        public float eventOffset = 0.05f;

        public UnityHandGrabEvent OnMinDistanceEvent;
        public UnityHandGrabEvent OnMaxDistanceEvent;

        bool triggeredMaxEvent;
        bool triggeredMinEvent;

        Vector3 handLocalPosition;
        Vector3 localPosition;
        Vector3 localTargetPosition;

        Grabbable grabbable;
        Vector3 localOrigin;
        Hand jointHand;

        public void Awake()
        {
            ResetOrigin();
            if(grabbable == null) {
                grabbable = GetComponent<Grabbable>();
                grabbable.OnGrabEvent += OnGrabbed;
                grabbable.OnReleaseEvent += OnReleased;
            }
            grabbable.singleHandOnly = true;

            if(xMinLimit == 0 && yMinLimit == 0 && zMinLimit == 0)
                triggeredMinEvent = true;
        }

        public void ResetOrigin()
        {
            localOrigin = connectedGrabbable.rootTransform.InverseTransformPoint(transform.position) + pivotOffset;
        }

        internal void OnGrabbed(Hand hand, Grabbable grab)
        {
            jointHand = hand;
            hand.body.mass *= heldMassScale;
        }

        void OnReleased(Hand hand, Grabbable grab)
        {
            jointHand = null;
            hand.body.mass /= heldMassScale;
        }

        private void FixedUpdate()
        {

            UpdateJoint();
        }

        public void SetJointMax() {
            transform.position = connectedGrabbable.rootTransform.TransformPoint(localOrigin + pivotOffset + new Vector3(xMaxLimit, yMaxLimit, zMaxLimit));
        }

        public void SetJointMin() {
            transform.position = connectedGrabbable.rootTransform.TransformPoint(localOrigin + pivotOffset + new Vector3(xMinLimit, yMinLimit, zMinLimit));
        }

        public void UpdateJoint()
        {
            if (connectedGrabbable.HeldCount() > 0 && grabbable.HeldCount() > 0 && jointHand != null)
            {
                handLocalPosition = connectedGrabbable.rootTransform.InverseTransformPoint(jointHand.moveTo.position) + pivotOffset;
                localPosition = jointHand.heldJoint.connectedAnchor + pivotOffset;
                localTargetPosition = new Vector3(
                    Mathf.MoveTowards(localPosition.x, Mathf.Clamp(handLocalPosition.x, localOrigin.x + xMinLimit, localOrigin.x + xMaxLimit), 1f),
                    Mathf.MoveTowards(localPosition.y, Mathf.Clamp(handLocalPosition.y, localOrigin.y + yMinLimit, localOrigin.y + yMaxLimit), 1f),
                    Mathf.MoveTowards(localPosition.z, Mathf.Clamp(handLocalPosition.z, localOrigin.z + zMinLimit, localOrigin.z + zMaxLimit), 1f)
                );

                if(localPosition != localTargetPosition) {
                    transform.position = connectedGrabbable.rootTransform.TransformPoint(localTargetPosition);
                    var deltaPosition = connectedGrabbable.rootTransform.TransformDirection(localTargetPosition - localPosition);
                    jointHand.heldJoint.connectedAnchor += (localTargetPosition - localPosition);
                    //jointHand.transform.position += deltaPosition;
                }

            }
            else
            {
                Vector3 localPosition = connectedGrabbable.transform.InverseTransformPoint(transform.position) + pivotOffset;
                Vector3 localTargetPosition = new Vector3(
                    Mathf.MoveTowards(localPosition.x, localOrigin.x, Time.fixedDeltaTime * xSpring),
                    Mathf.MoveTowards(localPosition.y, localOrigin.y, Time.fixedDeltaTime * ySpring),
                    Mathf.MoveTowards(localPosition.z, localOrigin.z, Time.fixedDeltaTime * zSpring)
                );

                if(localPosition != localTargetPosition)
                    transform.position = connectedGrabbable.rootTransform.TransformPoint(localTargetPosition);
            }
                    
            if(OnMaxDistanceEvent != null) {
                var localPos = connectedGrabbable.rootTransform.InverseTransformPoint(transform.position);
                bool greaterOrEqual =
                    localPos.x >= localOrigin.x + xMaxLimit - xMaxLimit * eventOffset - 0.001f &&
                    localPos.y >= localOrigin.y + yMaxLimit - yMaxLimit * eventOffset - 0.001f &&
                    localPos.z >= localOrigin.z + zMaxLimit - zMaxLimit * eventOffset - 0.001f; 

                if(greaterOrEqual && !triggeredMaxEvent) {
                    OnMaxDistanceEvent.Invoke(jointHand, connectedGrabbable);
                    triggeredMaxEvent = true;
                    triggeredMinEvent = false;
                }
            }

            if(OnMinDistanceEvent != null) {
                var localPos = connectedGrabbable.rootTransform.InverseTransformPoint(transform.position);
                bool lessOrEqual =
                    localPos.x <= localOrigin.x + xMinLimit - xMinLimit * eventOffset + 0.001f &&
                    localPos.y <= localOrigin.y + yMinLimit - yMinLimit * eventOffset + 0.001f &&
                    localPos.z <= localOrigin.z + zMinLimit - zMinLimit * eventOffset + 0.001f;

                if(lessOrEqual && !triggeredMinEvent) {
                    OnMinDistanceEvent.Invoke(jointHand, connectedGrabbable);
                    triggeredMinEvent = true;
                    triggeredMaxEvent = false;
                }
            }
        }


    }
}