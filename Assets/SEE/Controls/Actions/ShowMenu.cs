using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class ShowMenu : InteractableObjectHoveringAction
    {

        private GameObject menu;

        private static CubeFactory cubeFactory;

        private static CubeFactory CreateCubeFactory()
        {
            ColorRange colorRange = new ColorRange(Color.gray, Color.gray, 1);            
            return new CubeFactory(Materials.ShaderType.Transparent, colorRange);
        }

        protected override void Awake()
        {
            base.Awake();
            if (cubeFactory == null)
            {
                cubeFactory = CreateCubeFactory();
            }
        }

        protected override void On(bool isOwner)
        {
            Debug.Log("ShowMenuOn.\n");
            // If menu already exists or the object does not represent a graph node, nothing needs to be done
            if (menu == null && gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                Node node = nodeRef.node;
                if (node != null)
                {
                    menu = CreateMenu(node);
                }
                else
                {
                    Debug.LogErrorFormat("Invalid node reference in game object {0}\n.", name);
                }
            }            
        }

        protected override void Off(bool isOwner)
        {
            Debug.Log("ShowMenu.Off\n");
            if (menu != null)
            {
                Object.Destroy(menu);
            }
        }

        private GameObject CreateMenu(Node node)
        {
            // This menu may refer to either an inner node or a leaf.
            // We want to draw the menu in front of all other nodes. Nodes will
            // be put into the render queue according to their graph level. 
            // Leaves will have the maximal graph level. The higher the offset
            // in the render queue, the later the object will be drawn. Later
            // drawn objects cover objects drawn earlier. By putting the menu
            // after the highest possible node nesting level, we make sure the
            // menu is drawn in front of all nodes.
            GameObject menu = cubeFactory.NewBlock(0, node.ItsGraph.MaxDepth + 1);
            menu.name = "Mouse menu";

            Vector3 menuScale = new Vector3(0.05f, 0.05f, 0.001f);
            menu.transform.localScale = menuScale;
            Vector3 position = MenuPosition();
            position.x += menuScale.x / 2.0f;
            position.y += menuScale.y / 2.0f;
            menu.transform.position = position;
            //menu.transform.SetParent(gameObject.transform);
            Portal.SetInfinitePortal(menu);
            return menu;
        }

        private const float distanceBetweenObjectAndMenu = 0.0f;

        private const float distanceBetweenObjectAndCamera = 0.3f;

        private Vector3 MenuPosition()
        {
            // Just the mouse position plus a distance from the camera for the z axis.
            //Vector3 result = Input.mousePosition;
            //result.z = distanceBetweenObjectAndCamera;
            //return Camera.main.ScreenToWorldPoint(result);

            Vector3 result;
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop
                && Raycasting.RaycastNodes(out RaycastHit raycastHit))
            {
                result = raycastHit.point;
            }
            else
            {
                // FIXME: What to do in VR?
                result = gameObject.transform.position;
            }
            return result;
        }

        private void Update()
        {
            if (menu != null)
            {
                // FIXME: We want to save Camera.main.
                menu.transform.LookAt(Camera.main.transform);
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