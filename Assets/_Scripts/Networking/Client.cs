using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Client : MonoBehaviourPunCallbacks
{
    public string roomName { get; set; }
    public GameObject joinRoomButton;

    public override void OnEnable() { PhotonNetwork.AddCallbackTarget(this); }
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
}
