using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE
{

    public class RoomCameraController : MonoBehaviour
    {
        private Vector3 target;

        private float distanceFromTarget = 4.0f;
        private float rotationSensibility = 0.1f;
        private float maxPitch = 30.0f;
        private float maxYaw = 60.0f;

        private Vector3 lastMouse;
        private float pitch;
        private float yaw;
        private float roll;

        private bool isActive;

        void Start()
        {
            target = HeadGenerator.HEAD_POSITION;
            lastMouse = Input.mousePosition;
            pitch = 0.0f;
            yaw = 0.0f;
            roll = 0.0f;
            isActive = true;
        }

        void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                isActive = false;
                return;
            }

            if (!isActive)
            {
                if (!Input.GetMouseButton(0))
                {
                    isActive = true;
                }
                else
                {
                    return;
                }
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 mouse = Input.mousePosition;
                float deltaX = (mouse.x - lastMouse.x) * rotationSensibility;
                float deltaY = (mouse.y - lastMouse.y) * rotationSensibility;

                pitch -= deltaY;
                yaw += deltaX;

                if (pitch > maxPitch)
                {
                    pitch = maxPitch;
                } else if (pitch < -maxPitch)
                {
                    pitch = -maxPitch;
                }

                if (yaw > maxYaw)
                {
                    yaw = maxYaw;
                } else if (yaw < -maxYaw)
                {
                    yaw = -maxYaw;
                }

                transform.position = target;
                Vector3 euler = new Vector3(pitch, yaw, roll);
                transform.rotation = Quaternion.Euler(euler);
                transform.position -= transform.forward * distanceFromTarget;
            }
            lastMouse = Input.mousePosition;
        }
    }

}
