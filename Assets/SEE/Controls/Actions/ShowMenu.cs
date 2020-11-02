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

        protected override void On(bool isOwner)
        {
            // If menu already exists or the object does not represent a graph node, nothing needs to be done
            if (menu == null && gameObject.TryGetComponent(out NodeRef _))
            {
                menu = CreateMenu();
            }            
        }

        protected override void Awake()
        {
            base.Awake();
            if (cubeFactory == null)
            {
                cubeFactory = CreateCubeFactory();
            }
        }

        private GameObject CreateMenu()
        {
            GameObject menu = cubeFactory.NewBlock(0, 5000);
            menu.transform.localScale = new Vector3(0.2f, 0.3f, 0.001f);
            Vector3 position = MenuPosition();

            menu.transform.position = position;
            menu.transform.SetParent(gameObject.transform);
            Portal.SetInfinitePortal(menu);
            return menu;
        }

        private const float distanceBetweenObjectAndMenu = 0.0f;

        private Vector3 MenuPosition()
        {
            Vector3 result;
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop 
                && Raycasting.RaycastNodes(out RaycastHit raycastHit))
            {
                result = raycastHit.point;
            }
            else
            {
                result = gameObject.transform.position;                
            }
            result.y += gameObject.transform.lossyScale.y / 2.0f + distanceBetweenObjectAndMenu;
            return result;
        }

        protected override void Off(bool isOwner)
        {            
            if (menu != null)
            {
                Object.Destroy(menu);
            }
        }

        private void OnMouseEnter()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                Debug.LogFormat("mouse enters {0}\n", name);
            }
        }

        private void OnMouseOver()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop)
            {
                Debug.LogFormat("mouse over {0}\n", name);
            }
        }

        private void OnMouseExit()
        {
            if (PlayerSettings.GetInputType() == PlayerSettings.PlayerInputType.Desktop && !Raycasting.IsMouseOverGUI())
            {
                Debug.LogFormat("mouse exits {0}\n", name);
            }
        }
    }
}