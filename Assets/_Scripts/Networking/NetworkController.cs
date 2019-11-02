using Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkController : MonoBehaviourPunCallbacks
{
    public override void OnEnable()          { PhotonNetwork.AddCallbackTarget(this); }
    public override void OnDisable()         { PhotonNetwork.RemoveCallbackTarget(this); }

    public static bool IsConnected()         { return PhotonNetwork.IsConnected; }
    public static void Connect()             { if (!IsConnected()) PhotonNetwork.ConnectUsingSettings(); }
    public static void Disconnect()          { if (IsConnected()) PhotonNetwork.Disconnect(); }

    public static void JoinRoom(string name) { PhotonNetwork.JoinRoom(name);  }
    public static void LeaveRoom()           { PhotonNetwork.LeaveRoom(); }

    public static void CreateRoom(string name, byte maxPlayers)
    {
        RoomOptions roomOptions = new RoomOptions();

        roomOptions.IsVisible = false;
        roomOptions.IsOpen = true;
        roomOptions.MaxPlayers = maxPlayers;

        PhotonNetwork.CreateRoom(name, roomOptions);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.AutomaticallySyncScene = true;
        Debug.Log("Successfully connected to server!");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Successfully joined room!");
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        Debug.Log("Successfully created and joined room!");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Debug.Log("Failed to create room!");
        Debug.Log(message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.Log("Failed to join room!");
        Debug.Log(message);
    }
}
