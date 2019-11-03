using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

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
}
