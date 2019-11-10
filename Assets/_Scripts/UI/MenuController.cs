using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SEE
{

    public class MenuController : MonoBehaviour
    {
        private GameObject MainMenu;
        private GameObject CreateOnlineRoomMenu;
        private GameObject JoinOnlineRoomMenu;

        private void Start()
        {
            this.MainMenu             = (GameObject)Resources.Load("Prefabs/MainMenu",             typeof(GameObject));
            this.CreateOnlineRoomMenu = (GameObject)Resources.Load("Prefabs/CreateOnlineRoomMenu", typeof(GameObject));
            this.JoinOnlineRoomMenu   = (GameObject)Resources.Load("Prefabs/JoinOnlineRoomMenu",   typeof(GameObject));
        }

        public void GoToMainMenu()             { SwitchToMenu(this.MainMenu); }
        public void StartSingleplayer()        { SceneController.LoadScene(SceneController.Scene.Singleplayer); }
        public void GoToCreateOnlineRoomMenu() { SwitchToMenu(this.CreateOnlineRoomMenu); }
        public void GoToJoinOnlineRoomMenu()   { SwitchToMenu(this.JoinOnlineRoomMenu); }

        private void SwitchToMenu(GameObject prefab)
        {
            Instantiate(prefab, transform.parent.position, transform.parent.rotation, transform.parent);
            Destroy(this.gameObject);
        }

        public void Quit()
        {
            NetworkController.Disconnect();
            Application.Quit();
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #endif
        }
    }

}// namespace SEE
