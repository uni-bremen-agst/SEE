using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SEE
{
    /*
     * Allows to move the camera with WASD, Shift and Space.
     * 
    Written by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.
    Converted to C# 27-02-13 - no credit wanted.
    Reformatted and cleaned by Ryan Breaker 23-6-18
    Original comment:
    Simple flycam I made, since I couldn't find any others made public.
    Made simple to use (drag and drop, done) for regular keyboard layout.
    Controls:
    WASD  : Directional movement
    Shift : Increase speed
    Space : Moves camera directly up per its local Y-axis

    See also: 
     https://answers.unity.com/questions/666905/in-game-camera-movement-like-editor.html
     https://gist.github.com/Mahelita/f82d5b574ab6333f0c834178582280c3
    */

    /// <summary>
    /// Moves the main camera based on keyboard input.
    /// </summary>
    public class FlyCamera : MonoBehaviour
    {
        // These variables are exposed to the editor and can be changed by the user.
        public float normalSpeed = 10.0f;   // Normal speed without acceleration
        public float acceleration = 25.0f;  // Amount to accelerate when shift is pressed
        public float maximalSpeed = 100.0f; // Maximum speed when holding shift

        public float camSens = 0.15f;   // Mouse sensitivity
        public float rotationFactor = 100f; // degree of spinning for each tick without acceleration

        // the position of the mouse cursor of the last tick
        private Vector3 lastMouse = new Vector3(255, 255, 255);  // kind of in the middle of the screen, rather than at the top (play)

        // the accumulated time of the acceleration across ticks
        private float accelerationPeriod = 1.0f;

        private Vector3 previousPosition = new Vector3(0f, 0f, 0f);
        private Quaternion previousRotation = new Quaternion(0f, 0f, 0f, 0f);

        // The scene graph this camera observes.
        private SceneGraph sceneGraph = null;

        // The GUI text field showing the object name of a the currently selected node.
        private GameObject guiObjectNameTextField = null;

        // Spin the object in given direction around the origin of the object at rotationFactor per tick.
        private void Rotate(Vector3 direction, bool accelerationMode)
        {
            float degree = accelerationMode ? accelerationPeriod * rotationFactor : rotationFactor;
            transform.RotateAround(transform.position, direction, degree * Time.deltaTime);
        }

        /// <summary>
        /// Intializes sceneGraph and guiObjectNameTextField.
        /// 
        /// Called by unity on the frame when a script is enabled just before any of the Update 
        /// methods are called the first time. It is called only once.
        /// 
        /// This is a Unity message.  It is not defined in the base MonoBehaviour class, 
        /// so you we don't have to specify override in your derived class. Instead Unity 
        /// uses reflection to see if we've put a method named "Start" in our class and 
        /// if we have then it will call that method as needed.
        /// 
        /// See also https://docs.unity3d.com/Manual/ExecutionOrder.html on the execution order
        /// of messages.
        /// </summary>
        void Start()
        {
            if (sceneGraph == null)
            {
                sceneGraph = SceneGraph.GetInstance();
            }
            if (guiObjectNameTextField == null)
            {
                guiObjectNameTextField = GameObject.Find("Objectname");
            }
        }

        private int updateCounts = 0;

        /// <summary>
        /// Unity message called for every frame.
        /// 
        /// Reacts to user interactions by moving the camera or showing information
        /// on selected entities.
        /// Note: Update is called once per frame by Unity.
        /// </summary>
        void Update()
        {
            //updateCounts++;
            //if (updateCounts > )
            if (Input.GetMouseButtonDown(0))
            {
                // If a node is hit by a left mouse click, the name of the selected
                // node is shown in the guiObjectNameTextField.

                Camera camera = gameObject.GetComponentInParent<Camera>();

                if (camera == null)
                {
                    Debug.Log("Parent has no camera.\n");
                    camera = Camera.current;
                }
                if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                    // Note: The object to be hit needs a collider.
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // if the hit object is a node, we show the Source.Name
                        // of the graph node the object represents if it has a
                        // name; in all other cases, the name attribute of object
                        // is used instead.

                        GameObject objectHit = hit.transform.gameObject;
                        if (objectHit.tag == sceneGraph.houseTag)
                        {
                            // objectHit.SetActive(false); // hide the hidden object
                            if (guiObjectNameTextField != null)
                            {
                                Text text = guiObjectNameTextField.GetComponent<Text>();
                                INode node = sceneGraph.GetNode(objectHit.name);
                                if (node == null)
                                {
                                    text.text = objectHit.name;
                                }
                                else
                                {
                                    if (node.TryGetString("Source.Name", out string nodeName))
                                    {
                                        text.text = nodeName;
                                    }
                                    else
                                    {
                                        text.text = objectHit.name;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("No text field named Objectname");
                            }
                        }
                        else
                        {
                            Debug.Log("Hidden object is no building.\n");
                        }
                    }
                    else
                    {
                        Debug.Log("No oject hit.\n");
                    }
                }
                else
                {
                    Debug.LogError("No current camera found.\n");
                }
            }

            // if the user wants us to accelerate by holding the shift key
            bool accelerationMode = Input.GetKey(KeyCode.LeftShift);

            // Handle acceleration
            if (accelerationMode)
            {
                // still speeding up
                accelerationPeriod += Time.deltaTime;
            }
            else
            {
                // cool down acceleration by half
                accelerationPeriod = Mathf.Clamp(accelerationPeriod * 0.5f, 1f, 1000f);
            }

            // Rotation; keys have higher priority than mouse pointer
            if (Input.GetKey(KeyCode.E))
            {
                Rotate(Vector3.up, accelerationMode);
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                Rotate(Vector3.down, accelerationMode);
            }
            else if (Input.GetKey(KeyCode.Y))
            {
                Rotate(Vector3.left, accelerationMode);
            }
            else if (Input.GetKey(KeyCode.X))
            {
                Rotate(Vector3.right, accelerationMode);
            }
            else
            {
                lastMouse = Input.mousePosition - lastMouse;
                lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
                lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
                transform.eulerAngles = lastMouse;
                lastMouse = Input.mousePosition;
            }
            // Rotation of the object is done.

            // Moving the object.
            // Keyboard commands give the basic direction.
            Vector3 newPosition = GetBaseInput();

            if (accelerationMode)
            {
                // handle acceleration
                newPosition *= accelerationPeriod * acceleration;
                newPosition.x = Mathf.Clamp(newPosition.x, -maximalSpeed, maximalSpeed);
                newPosition.y = Mathf.Clamp(newPosition.y, -maximalSpeed, maximalSpeed);
                newPosition.z = Mathf.Clamp(newPosition.z, -maximalSpeed, maximalSpeed);
            }
            else
            {
                newPosition *= normalSpeed;
            }

            // Make the move to the new position
            transform.Translate(newPosition * Time.deltaTime);

            if (previousPosition != transform.position)
            {
                // Debug.Log("position: " + transform.position + "\n");
                previousPosition = transform.position;
            }
            if (previousRotation != transform.rotation)
            {
                // Debug.Log("rotation: " + transform.rotation + "\n");
                previousRotation = transform.rotation;
            }
        }

        // Returns the basic values, if it's 0 than it's not active.
        private Vector3 GetBaseInput()
        {
            Vector3 p_Velocity = new Vector3();

            // Forwards
            if (Input.GetKey(KeyCode.W))
                p_Velocity += Vector3.forward;

            // Backwards
            if (Input.GetKey(KeyCode.S))
                p_Velocity += Vector3.back;

            // Left
            if (Input.GetKey(KeyCode.A))
                p_Velocity += Vector3.left;

            // Right
            if (Input.GetKey(KeyCode.D))
                p_Velocity += Vector3.right;

            // Up
            if (Input.GetKey(KeyCode.Space))
                p_Velocity += Vector3.up;

            // Down
            if (Input.GetKey(KeyCode.LeftControl))
                p_Velocity += Vector3.down;

            return p_Velocity;
        }

        /*
        void OnScene(SceneView sceneview)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                Ray ray = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, Camera.current.pixelHeight - e.mousePosition.y));
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            }
        } */
    }
}