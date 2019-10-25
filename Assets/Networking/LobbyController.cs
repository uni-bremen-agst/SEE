using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LobbyController : MonoBehaviourPunCallbacks
{

    [SerializeField]
    private GameObject QuickStartButton;
    [SerializeField]
    private GameObject QuickCancelButton;
    [SerializeField]
    private int RoomSize;

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        QuickStartButton.SetActive(true);
    }

    public void QuickStart()
    {
        QuickStartButton.SetActive(false);
        QuickCancelButton.SetActive(true);
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();
    }

    void CreateRoom()
    {
        int randomRoomNumber = Random.Range(0, 10000);
        RoomOptions roopOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)RoomSize };
        PhotonNetwork.CreateRoom("Room" + randomRoomNumber, roopOps);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CreateRoom();
    }

    public void QuickCancel()
    {
        QuickCancelButton.SetActive(false);
        QuickStartButton.SetActive(true);
        PhotonNetwork.LeaveRoom();
    }
}
