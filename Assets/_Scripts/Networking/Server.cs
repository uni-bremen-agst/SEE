using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace SEE
{

    public class Server : MonoBehaviourPunCallbacks
    {
        public Text roomNameText;
        public Dropdown maxPlayersDropdown;
        public Dropdown visibilityDropdown;

        public GameObject createRoomButton;

        public override void OnEnable()  { PhotonNetwork.AddCallbackTarget(this); }
        public override void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

        void Start()
        {
            BatchFileUtil.Run("BatchFiles/start.bat");
            if (!NetworkController.IsConnected())
            {
                NetworkController.Connect();
            }
            else
            {
                OnConnectedToMaster();
            }
        }

        public void CreateRoom()
        {
            string roomName = roomNameText.text;
            byte maxPlayers = (byte)(maxPlayersDropdown.value + 1);
            NetworkController.Visibility v = visibilityDropdown.value == 0
                ? NetworkController.Visibility.Private
                : NetworkController.Visibility.Public;

            NetworkController.CreateRoom(roomName, maxPlayers, v);
        }

        public override void OnConnectedToMaster()
        {
            createRoomButton.GetComponentInChildren<Text>().text = "Create And Join";
            createRoomButton.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
            createRoomButton.GetComponent<Button>().interactable = true;
        }

        public override void OnCreatedRoom()
        {
            SceneController.LoadScene(SceneController.Scene.Multiplayer);
        }

        public static void StartServer()
        {
            BatchFileUtil.Run("BathFiles\\start.bat");
        }

        public static void StopServer()
        {
            BatchFileUtil.Run("BathFiles\\stop.bat");
        }
    }

}
