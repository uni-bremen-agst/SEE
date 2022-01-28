#if !UNITY_ANDROID
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls.Actions
{
    public class XRRay : MonoBehaviour
    {
        private class DelayedToggle
        {
            [Tooltip("The least amount of seconds the selection and grab buttons must have been pressed to be considered activated."),
             Range(0.01f, 1.0f)]
            public float ButtonDurationThreshold = 0.5f;

            /// <summary>
            /// Returns the state and if the button is currently pressed, it resets the state to false.
            /// </summary>
            public bool TransientState
            {
                get
                {
                    bool result = State;
                    if (State)
                    {
                        State = false;
                        buttonEventConsumed = false;
                    }
                    return result;
                }
            }
            /// <summary>
            /// Current state: true for down and false for up.
            /// 
            /// Whether this toggle has been pressed long enough. The selection works as a toggle
            /// that can be turned on and off. Each mode continues until the button is pressed again.
            /// </summary>
            public bool State { get; private set; } = false;

            /// <summary>
            /// Indicates whether the change of the button has already triggered
            /// the wanted behaviour, that is, lead to toggling the state.
            /// A user could press the button longer than the necessary time,
            /// in which case we want to maintain the state rather than wobbling
            /// its value. If this value is false, continuing pressing the button
            /// will be ignored until the button is released again.
            /// </summary>
            private bool buttonEventConsumed = false;

            private readonly SteamVR_Action_Boolean button;

            public DelayedToggle(SteamVR_Action_Boolean button)
            {
                this.button = button;
            }

            public void OnUpdate()
            {
                if (button.stateDown)
                {
                    buttonEventConsumed = false;
                }
                else if (button.stateUp)
                {
                    buttonEventConsumed = true;
                }
                else if (button.state)
                {
                    if (!buttonEventConsumed
                        && Time.realtimeSinceStartup - button.changedTime >= ButtonDurationThreshold)
                    {
                        State = !State;
                        buttonEventConsumed = true;
                    }
                }
            }
        }

        [Tooltip("The VR controller for pointing")]
        public Hand PointingHand;

        /// <summary>
        /// The maximal length the casted ray can reach.
        /// </summary>
        [Tooltip("The maximal length the selection ray can reach.")]
        public float RayDistance = 5.0f;

        [Tooltip("The color of the selection ray used when an object was hit.")]
        public Color colorOnSelectionHit = Color.green;

        [Tooltip("The color of the selection ray used when no object was hit.")]
        public Color colorOnSelectionMissed = Color.red;

        [Tooltip("The color of the grabbing ray used when an object was hit.")]
        public Color colorOnGrabbingHit = Color.blue;

        [Tooltip("The color of the grabbing ray used when no object was hit.")]
        public Color colorOnGrabbingMissed = Color.yellow;

        /// <summary>
        /// The width of the ray line.
        /// </summary>
        [Tooltip("The width of the selection ray line")]
        public float rayWidth = 0.005f;

        private readonly SteamVR_Action_Single GrabAction = SteamVR_Input.GetSingleAction(XRInput.DefaultActionSetName, "Grab");

        private readonly SteamVR_Action_Boolean SelectionButton = SteamVR_Input.GetBooleanAction(XRInput.DefaultActionSetName, "Select");

        private DelayedToggle selectionButton;

        /// <summary>
        /// The position of the PointingHand.
        /// </summary>
        public Vector3 Position => PointingHand.transform.position;

        /// <summary>
        /// The direction the PointingHand points to.
        /// </summary>
        public Vector3 Direction => SteamVR_Actions.default_Pose.GetLocalRotation(PointingHand.handType) * Vector3.forward;

        /// <summary>
        /// The line renderer to draw the ray.
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// The game object holding the line renderer.
        /// </summary>
        private GameObject lineRendererGameObject;

        /// <summary>
        /// True if the user presses the grabbing button ("Grab" in SteamVR) deeply enough.
        /// </summary>
        public bool IsGrabbing => GrabAction.axis >= 0.9f;

        private void Start()
        {
            selectionButton = new DelayedToggle(SelectionButton);

            // We create game object holding the line renderer for the ray.
            // This game object will be added to the game object this component
            // is attached to.
            lineRendererGameObject = new GameObject { name = "Ray" };
            lineRendererGameObject.transform.parent = transform;
            lineRendererGameObject.transform.localPosition = Vector3.up;

            lineRenderer = lineRendererGameObject.AddComponent<LineRenderer>();

            // simplify rendering; no shadows
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth;
        }

        private void Update()
        {
            selectionButton.OnUpdate();
            bool isGrabbing = IsGrabbing;
            bool isSelecting = selectionButton.State;

            if (isGrabbing || isSelecting)
            {
                bool result = Physics.Raycast(Position, Direction, out RaycastHit hitInfo, RayDistance);
                GameObject hitObject = result ? hitInfo.collider.gameObject : null;
                ShowRay(hitObject, hitInfo);
            }
            if (!isGrabbing && !isSelecting)
            {
                HideRay();
            }
        }

        private void ShowRay(GameObject selectedObject, RaycastHit hitInfo)
        {
            Vector3 origin = Position;
            lineRenderer.SetPosition(0, origin);

            if (selectedObject != null)
            {
                lineRenderer.SetPosition(1, hitInfo.point);
                if (IsGrabbing)
                {
                    lineRenderer.material.color = colorOnGrabbingHit;
                }
                else
                {
                    lineRenderer.material.color = colorOnSelectionHit;
                }
            }
            else
            {
                if (IsGrabbing)
                {
                    lineRenderer.material.color = colorOnGrabbingMissed;
                }
                else
                {
                    lineRenderer.material.color = colorOnSelectionMissed;
                }
                lineRenderer.SetPosition(1, origin + RayDistance * Direction.normalized);
            }
        }

        private void HideRay()
        {
            Vector3 origin = Position;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin);
        }
    }
}
#endif