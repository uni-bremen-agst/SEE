using UnityEngine;

namespace SEE.Controls
{

    public class PlayerMovement : MonoBehaviour
    {
        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
        }

        private CameraState cameraState;

        private const int RightMouseButton = 1;

        [Tooltip("The code city at which the player is hooked.")]
        public Plane cullingPlane;

        // Start is called before the first frame update
        void Start()
        {
            if (cullingPlane != null)
            {
                Camera.main.transform.position = cullingPlane.CenterTop;
            }
            else
            {
                Debug.LogErrorFormat("Player {0} has no culling plane assigned.\n", name);
            }
            cameraState.distance = 1.0f;
            cameraState.yaw = 0.0f;
            cameraState.pitch = 30.0f;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
        }

        // Update is called once per frame
        void Update()
        {

            // Camera
            const float Speed = 2.0f; // TODO(torben): this is arbitrary
            float speed = Input.GetKey(KeyCode.LeftShift) ? 4.0f * Speed : Speed;
            if (Input.GetKey(KeyCode.W))
            {
                cameraState.distance -= speed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S))
            {
                cameraState.distance += speed * Time.deltaTime;
            }
            if (Input.GetMouseButton(RightMouseButton))
            {
                float x = Input.GetAxis("mouse x");
                float y = Input.GetAxis("mouse y");
                cameraState.yaw += x;
                cameraState.pitch -= y;
            }
            if (cullingPlane != null)
            {
                Camera.main.transform.position = cullingPlane.CenterTop;
            }
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;

        }
    }
}