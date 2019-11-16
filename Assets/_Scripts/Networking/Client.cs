using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace SEE
{

    public class Client : MonoBehaviourPunCallbacks
    {
        public Text roomNameText;

        public GameObject joinRoomButton;

        public override void OnEnable()  { PhotonNetwork.AddCallbackTarget(this); }
        public override void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

        void Start() {
            if (!NetworkController.IsConnected())
            {
                NetworkController.Connect();
            }
            else
            {
                OnConnectedToMaster();
            }
        }

        public void JoinRoom()
        {
            string roomName = roomNameText.text;
            // TODO password
            NetworkController.JoinRoom(roomName);
        }

        public void LeaveRoom()
        {
            NetworkController.LeaveRoom();
        }

        public override void OnConnectedToMaster()
        {
            joinRoomButton.GetComponentInChildren<Text>().text = "Join";
            joinRoomButton.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
            joinRoomButton.GetComponent<Button>().interactable = true;
        }

        public override void OnJoinedRoom()
        {
            SceneController.LoadScene(SceneController.Scene.Multiplayer);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            base.OnJoinRandomFailed(returnCode, message);
            Debug.Log("Joining room failed!");
            Debug.Log("Error code: \"" + returnCode + "\". Error message: \"" + message + "\"");
        }
    }

}
