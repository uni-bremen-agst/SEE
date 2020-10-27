using UnityEngine;

namespace SEE.Controls
{

    public class DesktopPlayerMovement : MonoBehaviour
    {
        private const float Speed = 2.0f;
        private const float BoostFactor = 2.0f;

        //temp save the new node
        Node newNode;

        private struct CameraState
        {
            internal float distance;
            internal float yaw;
            internal float pitch;
            internal bool freeMode;
        }

        private CameraState cameraState;
        private const int RightMouseButton = 1;

        [Tooltip("The code city which the player is focusing on.")]
        public GO.Plane focusedObject;

        private void Start()
        {
            if (focusedObject != null)
            {
                Camera.main.transform.position = focusedObject.CenterTop;
            }
            else
            {
                Debug.LogErrorFormat("Player {0} has no focus object assigned.\n", name);
            }
            cameraState.distance = 2.0f;
            cameraState.yaw = 0.0f;
            cameraState.pitch = 45.0f;
            Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (cameraState.freeMode)
                {
                    Vector3 positionToFocusedObject = focusedObject.CenterTop - transform.position;
                    cameraState.distance = positionToFocusedObject.magnitude;
                    transform.forward = positionToFocusedObject;
                    Vector3 pitchYawRoll = transform.rotation.eulerAngles;
                    cameraState.pitch = pitchYawRoll.x;
                    cameraState.yaw = pitchYawRoll.y;
                }
                cameraState.freeMode = !cameraState.freeMode;
            }

            float speed = Speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= BoostFactor;
            }

            if (!cameraState.freeMode)
            {
                float d = 0.0f;
                if (Input.GetKey(KeyCode.W))
                {
                    d += speed;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    d -= speed;
                }
                cameraState.distance -= d;

                if (Input.GetMouseButton(RightMouseButton))
                {
                    float x = Input.GetAxis("mouse x");
                    float y = Input.GetAxis("mouse y");
                    cameraState.yaw += x;
                    cameraState.pitch -= y;
                }
                Camera.main.transform.position = focusedObject.CenterTop;
                Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
                Camera.main.transform.position -= Camera.main.transform.forward * cameraState.distance;
            }
            else // cameraState.freeMode == true
            {
                Vector3 v = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                {
                    v += Camera.main.transform.forward;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    v -= Camera.main.transform.forward;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    v += Camera.main.transform.right;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    v -= Camera.main.transform.right;
                }
                if (Input.GetKey(KeyCode.Space))
                {
                    v += Vector3.up;
                }
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    v += Vector3.down;
                }
                v.Normalize();
                v *= speed;
                Camera.main.transform.position += v;

                if (Input.GetMouseButton(RightMouseButton))
                {
                    float x = Input.GetAxis("mouse x");
                    float y = Input.GetAxis("mouse y");
                    cameraState.yaw += x;
                    cameraState.pitch -= y;
                }
                Camera.main.transform.rotation = Quaternion.Euler(cameraState.pitch, cameraState.yaw, 0.0f);
            }


            //Test for insert a new node
            if (Input.GetKeyDown(KeyCode.N && newNode == null){
                //create new Node and let him stick to the 
            } 
            if(newNode != null)
            {
                if (Input.GetKeyDown(KeyCode.M))
                {
                    //Change node type to the next in the list
                }
                if (Input.GetMouseButton(LeftMouseButton))
                {
                    //Place node and set newNode to Null
                }
            }
            if(Input.GetKeyDown(KeyCode.N && newNode != null){
                //exit node adding
                //remove node from cursor
                newNode = null;
            }
        }
    }
}