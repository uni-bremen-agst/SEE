using SEE.DataModel;
using SEE.Layout;
using UnityEngine;

namespace SEE
{

    public class CameraController : MonoBehaviour
    {
        private Camera mainCamera;

        private float baseSpeed = 12.0f;
        private const float speedUpFactor = 6.0f;
        private const float groundDistanceFactor = 0.1f;
        private const float rotationSpeed = 4.0f;

        private float yaw;
        private float pitch;
        private Vector3 position;
        private Quaternion rotation;

        private const string textFieldObjectName = "Objectname";
        private GameObject guiObjectNameTextField;

        void Start()
        {
            mainCamera = GetComponent<Camera>();

            yaw = Input.GetAxis("Mouse X");
            pitch = Input.GetAxis("Mouse Y");

            position = mainCamera.transform.position;
            rotation = Quaternion.identity;

            guiObjectNameTextField = GameObject.Find(textFieldObjectName);
            if (!guiObjectNameTextField)
            {
                throw new System.ArgumentException("'guiObjectNameTextField' could not be found!");
            }
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (guiObjectNameTextField != null)
                {
                    if (mainCamera != null)
                    {
                        ShowSelectedObject(mainCamera);
                    }
                    else
                    {
                        Debug.LogError("No main camera found.\n");
                    }
                }
                else
                {
                    Debug.LogWarningFormat("No UI textfield named {0} found. Please add one to the scene within the Unity editor.\n", textFieldObjectName);
                }
            }

            bool speedUp = Input.GetKey(KeyCode.LeftShift);
            float speedFactor = speedUp ? speedUpFactor : 1.0f;
            float relativeSpeed = baseSpeed * speedFactor * groundDistanceFactor * Mathf.Max(Mathf.Abs(mainCamera.transform.position.y), 1.0f);
            float absoluteSpeed = relativeSpeed * Time.deltaTime;

            Vector3 velocity = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
                velocity += transform.forward;
            if (Input.GetKey(KeyCode.A))
                velocity -= transform.right;
            if (Input.GetKey(KeyCode.S))
                velocity -= transform.forward;
            if (Input.GetKey(KeyCode.D))
                velocity += transform.right;
            if (Input.GetKey(KeyCode.Space))
                velocity += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl))
                velocity -= Vector3.up;
            position += velocity * absoluteSpeed;

            Vector3 mousePosition = Input.mousePosition;
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * rotationSpeed;
                pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
                rotation.eulerAngles = new Vector3(pitch, yaw, 0.0f);
            }

            mainCamera.transform.localPosition = position;
            mainCamera.transform.localRotation = rotation;
        }

        public void AdjustSettings(float unit)
        {
            baseSpeed *= unit;
        }

        private void ShowSelectedObject(Camera camera)
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject objectHit = hit.transform.gameObject;
                UnityEngine.UI.Text text = guiObjectNameTextField.GetComponent<UnityEngine.UI.Text>();

                if (objectHit.TryGetComponent(out NodeRef nodeRef))
                {
                    Debug.Log("Node hit.");
                    if (nodeRef.node.TryGetString("Source.Name", out string nodeName))
                    {
                        text.text = nodeName;
                    }
                    else
                    {
                        text.text = nodeRef.node.Type;
                        Debug.Log("Node has neither Source.Name nor unique linkname.\n");
                        Debug.Log("Selected: " + objectHit.name + "\n");
                        if (objectHit.TryGetComponent(out Node node))
                        {
                            Debug.Log(node.ToString() + "\n");
                        }
                    }
                }
                else if (objectHit.TryGetComponent(out EdgeRef edge))
                {
                    text.text = "Edge " + objectHit.name;
                    Debug.Log("Edge hit.");
                }
            }
        }
    }

}
