using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviourPunCallbacks
{
    public string roomName           { get; set; }
    public int maxPlayers            { get; set; }
    public GameObject createRoomButton;

    public override void OnEnable()  { PhotonNetwork.AddCallbackTarget(this); }
    public override void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

    void Start()
    {
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
        NetworkController.CreateRoom(roomName, (byte)maxPlayers);
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
}
