using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuController : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private int MultiplayerSceneIndex;

    private GameObject MainMenu;
    private GameObject SingleplayerMenu;
    private GameObject CreateOnlineRoomMenu;
    private GameObject JoinOnlineRoomMenu;

    private static GameObject TargetMenuAfterConnecting;

    private string RoomName;
    private byte RoomSize;

    private void Start()
    {
        this.MainMenu                  = (GameObject)Resources.Load("UI/MainMenu",             typeof(GameObject));
        this.SingleplayerMenu          = (GameObject)Resources.Load("UI/SingleplayerMenu",     typeof(GameObject));
        this.CreateOnlineRoomMenu      = (GameObject)Resources.Load("UI/CreateOnlineRoomMenu", typeof(GameObject));
        this.JoinOnlineRoomMenu        = (GameObject)Resources.Load("UI/JoinOnlineRoomMenu",   typeof(GameObject));

        Initialize();
    }

    private void Initialize()
    {
        this.RoomName = "";
        this.RoomSize = 1;
    }

    public override void OnEnable()            { PhotonNetwork.AddCallbackTarget(this);                                                               }
    public override void OnDisable()           { PhotonNetwork.RemoveCallbackTarget(this);                                                            }
    public override void OnConnectedToMaster() { PhotonNetwork.AutomaticallySyncScene = true; SwitchToMenu(MenuController.TargetMenuAfterConnecting); }

    // Singleplayer
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void Singleplayer()
    {
        Debug.Log("Starting singleplayer game... soon...");
    }

    // Create room
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void CreateAndJoinRoom()
    {
        RoomOptions roomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = this.RoomSize };
        PhotonNetwork.CreateRoom(this.RoomName, roomOptions);
        PhotonNetwork.LoadLevel(this.MultiplayerSceneIndex);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // TODO
    }

    // Join room
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void JoinOnlineRoom()
    {
        PhotonNetwork.JoinRoom(this.RoomName);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // TODO
    }

    // Setters
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void SetRoomName(string roomName)
    {
        this.RoomName = roomName;
    }

    public void SetRoomSize(int roomSize)
    {
        this.RoomSize = (byte)(roomSize + 1);
    }

    // Quit
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    // Switch menus
    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void GoToMainMenu()                 { SwitchToMenu(this.MainMenu);                                                                 }
    public void GoToSoloRoomMenu()             { SwitchToMenu(this.SingleplayerMenu);                                                         }
    public void GoToCreateOnlineRoomMenu()     { MenuController.TargetMenuAfterConnecting = this.CreateOnlineRoomMenu; PhotonNetwork.ConnectUsingSettings(); }
    public void GoToJoinOnlineRoomMenu()       { MenuController.TargetMenuAfterConnecting = this.JoinOnlineRoomMenu;   PhotonNetwork.ConnectUsingSettings(); }

    private void SwitchToMenu(GameObject prefab)
    {
        Instantiate(prefab, transform.parent.position, transform.parent.rotation, transform.parent);
        Destroy(this.gameObject);
        Initialize();
    }
}
