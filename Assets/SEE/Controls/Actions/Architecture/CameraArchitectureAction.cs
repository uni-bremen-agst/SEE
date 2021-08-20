using SEE.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Controls.Actions.Architecture
{
    
    /// <summary>
    /// Action to move the camera over the drawing area. Implementation of <see cref="AbstractArchitectureAction"/>
    /// </summary>
    public class CameraArchitectureAction : AbstractArchitectureAction
    {
        public override ArchitectureActionType GetActionType()
        {
            return ArchitectureActionType.Camera;
        }


        public static AbstractArchitectureAction NewInstance()
        {
            return new CameraArchitectureAction();
        }
        /// <summary>
        /// The transform of the camera gameobject.
        /// </summary>
        private Transform CameraTransform;

        /// <summary>
        /// The start position of the camera.
        /// </summary>
        private Vector3 startPos;
        /// <summary>
        /// The origin of the camera move touch point.
        /// </summary>
        private Vector3 origin;
        /// <summary>
        /// Whether the camera is dragged.
        /// </summary>
        private bool drag;

        public override void Start()
        {
            //Get the Transform of the main Player camera.
            CameraTransform = MainCamera.Camera.transform;
        }


        public override void Update()
        {
            if (Raycasting.IsMouseOverGUI()) return;
            if (Pen.current.tip.wasPressedThisFrame)
            {
                //Start the drag movement
                drag = true;
                startPos = MainCamera.Camera.transform.position;
                origin = MainCamera.Camera.ScreenToViewportPoint(Pen.current.position.ReadValue());
                
            }

            if (Pen.current.tip.isPressed && drag)
            {
                //Execute the drag movement
                Vector3 pos = MainCamera.Camera.ScreenToViewportPoint(Pen.current.position.ReadValue()) - origin;
                float speed = 5f;
                var move = new Vector3(-pos.x, 0, -pos.y);
                CameraTransform.position = startPos + move * speed;
            }

            if (Pen.current.tip.wasReleasedThisFrame)
            {
                //End the drag movement
                drag = false;
            }
        }
    }
}