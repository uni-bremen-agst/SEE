using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class ShowMenu : MonoBehaviour
    {

        /// <summary>
        /// The game object representing the menu.
        /// </summary>
        private GameObject menu;

        /// <summary>
        /// The factory to create the cube used as a canvas.
        /// </summary>
        private static CubeFactory cubeFactory;

        /// <summary>
        /// The main camera the menu should be facting to.
        /// </summary>
        private static Camera camera;

        private static Vector3 menuScale = new Vector3(0.25f, 0.25f, 0.001f);

        private const float distanceBetweenObjectAndCamera = 1.0f;

        private static CubeFactory CreateCubeFactory()
        {
            ColorRange colorRange = new ColorRange(Color.gray, Color.gray, 1);            
            return new CubeFactory(Materials.ShaderType.Transparent, colorRange);
        }

        /// <summary>
        /// If true (and only if), the menu is visible.
        /// </summary>
        private bool menuIsOn = false;

        /// <summary>
        /// Creates the <see cref="cubeFactory"/> if it does not exist yet.
        /// </summary>
        protected virtual void Start()
        {
            if (cubeFactory == null)
            {
                cubeFactory = CreateCubeFactory();
            }
            if (camera == null)
            {
                if (Camera.allCameras.Length > 1)
                {
                    Debug.LogFormat("There are {0} cameras in the scene. Expect unexpected visual results.\n", 
                                    Camera.allCameras.Length);
                    foreach (Camera c in Camera.allCameras)
                    {
                        Debug.LogFormat("Camera: {0}\n", c.name);
                    }
                }
                camera = Camera.main;
            }
            if (menu == null)
            {
                menu = CreateMenu(menuScale);
                Off();
            }
        }

        /// <summary>
        /// Shows the menu.
        /// </summary>
        protected virtual void On()
        {
            menu.transform.position = MenuCenterPosition(menuScale);
            menu.GetComponent<Renderer>().enabled = true; // make it visible
        }

        /// <summary>
        /// Hides the menu.
        /// </summary>
        protected virtual void Off()
        {
            menu.GetComponent<Renderer>().enabled = false; // make it invisible
        }

        private GameObject CreateMenu(Vector3 menuScale)
        {
            // This menu may refer to either an inner node or a leaf.
            // We want to draw the menu in front of all other nodes. Nodes will
            // be put into the render queue according to their graph level. 
            // Leaves will have the maximal graph level. The higher the offset
            // in the render queue, the later the object will be drawn. Later
            // drawn objects cover objects drawn earlier. By putting the menu
            // after the highest possible node nesting level, we make sure the
            // menu is drawn in front of all nodes.
            //GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //GameObject menu = cubeFactory.NewBlock(0,100); // , node.ItsGraph.MaxDepth + 1
            menu.name = "Mouse menu";
            // cubeFactory will tag the object as Node; we want it be untagged
            menu.tag = "Untagged";
            // add the menu to the UI layer
            menu.layer = LayerMask.NameToLayer("UI");        

            if (false)
            { // FIXME: Remove the collider for the time being.
                if (menu.TryGetComponent<Collider>(out Collider collider))
                {
                    Destroy(collider);
                }
            }            
           
            menu.transform.localScale = menuScale;
            menu.transform.position = Vector3.zero; // the real position will be set when it becomes visible
            //menu.transform.SetParent(gameObject.transform);
            //Portal.SetInfinitePortal(menu);
            return menu;
        }

        private Vector3 MenuCenterPosition(Vector3 menuScale)
        {
            // Viewport space is normalized and relative to the camera. The bottom-left 
            // of the camera is (0,0); the top-right is (1,1). 
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = Mathf.Max(camera.nearClipPlane, distanceBetweenObjectAndCamera);
            Vector3 r = camera.ScreenToWorldPoint(mousePosition);
            Debug.LogFormat("mouse position {0}, world position {1}\n", mousePosition, r);
            return r;


            Vector3 viewport = camera.ScreenToViewportPoint(mousePosition);
            Debug.LogFormat("mouse position {0}, viewport position {1}\n", mousePosition, viewport);
            viewport.x = Mathf.Clamp(viewport.x, 0, 1);
            viewport.y = Mathf.Clamp(viewport.y, 0, 1);
            // The z value is the distance from the camera in world units.
            //viewport.z = distanceBetweenObjectAndCamera;
            Vector3 result = camera.ViewportToWorldPoint(viewport);
            Debug.LogFormat("adjusted viewport position {0}, result position {1}\n", viewport, result);

            // The menu position is interpreted as the left lower corner. A transform.position,
            // however, identifies the center of an object in Unity, hence, we need to shift
            // the menu position accordingly to make it the center.
            // FIXME
            //result.x += menuScale.x / 2.0f;
            //result.y += menuScale.y / 2.0f;
            return result;

            //Vector3 result = mousePosition;
            //result.z = distanceBetweenObjectAndCamera;
            //result = camera.ScreenToWorldPoint(result);

            //Debug.LogFormat("2D mouse {0}, viewport mouse {1} => 3D mouse {2}\n", mousePosition, viewport, result);
            //return result;

            //Vector3 result;
            //if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop
            //    && Raycasting.RaycastNodes(out RaycastHit raycastHit))
            //{
            //    result = raycastHit.point;
            //}
            //else
            //{
            //    // FIXME: What to do in VR?
            //    result = gameObject.transform.position;
            //}
            //return result;
        }

        private void Update()
        {
            bool oldState = menuIsOn;
            // space bar toggles menu            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                menuIsOn = !menuIsOn;
            }
            if (menuIsOn)
            {
                if (oldState != menuIsOn)
                {
                    On();
                }
                // Menu should be facing the camera
                menu.transform.LookAt(camera.transform);
            }
            else
            {
                if (oldState != menuIsOn)
                {
                    Off();
                }
            }
        }

        private void OnMouseEnter()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                //Debug.LogFormat("mouse enters {0}\n", name);
            }
        }

        private void OnMouseOver()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                //Debug.LogFormat("mouse over {0}\n", name);
            }
        }

        private void OnMouseExit()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                //Debug.LogFormat("mouse exits {0}\n", name);
            }
        }
    }
}