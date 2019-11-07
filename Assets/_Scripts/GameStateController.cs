using UnityEngine;

namespace SEE
{

    public class GameStateController : MonoBehaviour
    {
        private GameObject ingameMenu;
        private FlyCamera cameraScript;

        void Start()
        {
            ingameMenu = GameObject.Find("IngameMenu");
            cameraScript = Camera.main.GetComponent<FlyCamera>();

            ingameMenu.SetActive(false); //TODO: singleplayer should also have menu
            cameraScript.isActive = true;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleMenu();
            }
        }

        public void ToggleMenu()
        {
            bool menuOpened = !ingameMenu.activeSelf;
            ingameMenu.SetActive(menuOpened);
            cameraScript.isActive = !menuOpened;
        }

        public void OpenMenu()
        {
            ingameMenu.SetActive(true);
            cameraScript.isActive = false;
        }

        public void CloseMenu()
        {
            ingameMenu.SetActive(false);
            cameraScript.isActive = true;
        }
    }

}// namespace SEE
