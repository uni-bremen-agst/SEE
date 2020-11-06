using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides an in-game menu.
    /// 
    /// NOTE: This class is currently under construction and not yet ready.
    /// </summary>
    public class ShowMenu : MonoBehaviour
    {
        /// <summary>
        /// The game object representing the menu.
        /// </summary>
        private GameObject menu;

        /// <summary>
        /// The main camera the menu should be facting to.
        /// </summary>
        private static Camera mainCamera;

        /// <summary>
        /// Radius of the menu.
        /// </summary>
        [Tooltip("The radius of the circular menu.")]
        [Range(0, 2)]
        public float Radius = 0.3f;

        /// <summary>
        /// Radius of the menu.
        /// </summary>
        [Tooltip("The depth of the circular menu (z axis).")]
        [Range(0, 0.1f)]
        public float Depth = 0.01f;

        /// <summary>
        /// The distance between the menu and the camera when the menu is spawned.
        /// </summary>
        [Tooltip("The distance between the menu and the camera when the menu is spawned.")]
        [Range(0, 10)]
        public float CameraDistance = 1.0f;

        /// <summary>
        /// If true (and only if), the menu is visible.
        /// </summary>
        private bool menuIsOn = false;

        /// <summary>
        /// Creates the <see cref="menu"/> if it does not exist yet.
        /// Sets <see cref="mainCamera"/>.
        /// </summary>
        protected virtual void Start()
        {
            if (mainCamera == null)
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
                mainCamera = Camera.main;
            }
            if (menu == null)
            {
                menu = CreateMenu(entries, Radius, Depth);
                Off();
            }
        }

        public int entries = 2;

        /// <summary>
        /// Shows the menu.
        /// </summary>
        protected virtual void On()
        {
            menu.transform.position = MenuCenterPosition();
            menu.GetComponent<Renderer>().enabled = true; // make it visible
        }

        /// <summary>
        /// Hides the menu.
        /// </summary>
        protected virtual void Off()
        {
            menu.GetComponent<Renderer>().enabled = false; // make it invisible
        }

        /// <summary>
        /// Creates the circular menu.
        /// </summary>
        /// <param name="radius">the radius the circular menu should have</param>
        /// <returns>a new circular menu with given <paramref name="radius"/></returns>
        private static GameObject CreateMenu(int entries, float radius, float depth)
        {
            GameObject menu = NewCircle(radius, depth);
            menu.transform.position = Vector3.zero; // the real position will be set when it becomes visible
            menu.name = "Mouse menu";

            AddInnerCircles(menu, radius, depth, entries);
            return menu;
        }

        private static void AddInnerCircles(GameObject menu, float radius, float depth, int entries)
        {
            if (entries < 1 || entries > circles.Length)
            {
                throw new Exception("Unsupported number of inner circles: " + entries);
            } 
            else if (entries > 1)
            {
                InnerCircles selectedInnerCircles = circles[entries - 1];
                float innerRadius = selectedInnerCircles.radius;

                int i = 1;
                foreach (Vector2 center in selectedInnerCircles.centers)
                {
                    GameObject inner = NewCircle(radius * innerRadius, depth);
                    inner.name = "menu entry " + i;
                    i++;
                    inner.transform.SetParent(menu.transform);
                    inner.transform.localPosition = new Vector3(center.x / 2.0f, center.y / 2.0f, 2.0f);           
                }
            }
        }

        private static GameObject NewCircle(float radius, float depth)
        {
            GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            
            menu.tag = "Untagged";
            // add the circle to the UI layer so that it does not collide with other game objects
            menu.layer = LayerMask.NameToLayer("UI");
            if (menu.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            menu.transform.localScale = new Vector3(2 * radius, 2 * radius, depth);
            return menu;
        }

        /// <summary>
        /// Returns the center position in world space where the menu 
        /// should be located when it is spawned.
        /// </summary>
        /// <returns>center position of menu in world space</returns>
        private Vector3 MenuCenterPosition()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                Vector3 mousePosition = Input.mousePosition;
                mousePosition.z = Mathf.Max(mainCamera.nearClipPlane, CameraDistance);
                return mainCamera.ScreenToWorldPoint(mousePosition);
            }
            else
            {
                // FIXME
                throw new NotImplementedException("ShowMenu.MenuCenterPosition() implemented only for desktop environment.");
            }
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
                menu.transform.LookAt(mainCamera.transform);
                int hitEntry = SelectedMenuEntry();
            }
            else
            {
                if (oldState != menuIsOn)
                {
                    Off();
                }
            }
        }

        private int SelectedMenuEntry()
        {
            int result = -1;
            if (Input.GetMouseButton(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == menu)
                    {
                        Debug.LogFormat("hit {0}\n", hit.collider.gameObject.name);
                    }
                    else
                    {
                        Debug.LogFormat("hit other object {0}\n", hit.collider.gameObject.name);
                    }
                    
                    Vector2 hitPoint = hit.point; // z axis will be ignored
                    // normalize hitPoint to a circle with radius 1 (diameter 2)
                    // where the circle's origin has 
                }               
            }
            return result;
        }

        struct InnerCircles
        {
            public float radius;   
            public Vector2[] centers;
            public InnerCircles(float radius, Vector2[] centers)
            {
                this.radius = radius;
                this.centers = centers;
            }
        }

        /// <summary>
        /// A mapping of the number of equally sized inner circles to be enclosed
        /// in an outer circle with radius 1 onto the radius and co-ordinates of
        /// those inner circles.
        /// 
        /// The data were retrieved from http://hydra.nat.uni-magdeburg.de/packing/cci/cci.html.
        /// </summary>
        private static readonly InnerCircles[] circles =
        {
            /* 1 */ new InnerCircles(1.0f, new Vector2[]{ Vector2.zero}),
            /* 2 */ new InnerCircles(0.5f, 
                                     new Vector2[]{ 
                                         new Vector2(-0.5f, 0), 
                                         new Vector2(0.5f, 0)}),
            /* 3 */ new InnerCircles(0.46410161518f, 
                                     new Vector2[]{ 
                                         new Vector2(-0.464101615137754587054892683011f, -0.267949192431122706472553658494f), 
                                         new Vector2( 0.464101615137754587054892683011f, -0.267949192431122706472553658494f), 
                                         new Vector2( 0.000000000000000000000000000000f,  0.535898384862245412945107316988f)}),
        };

    }
}