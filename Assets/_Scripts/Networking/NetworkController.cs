using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace SEE
{

    public class NetworkController : MonoBehaviourPunCallbacks
    {
        void Start()                             { PhotonNetwork.AddCallbackTarget(this); }
        void OnDestroy()                         { PhotonNetwork.RemoveCallbackTarget(this); }

        public override void OnEnable()          { base.OnEnable(); PhotonNetwork.AddCallbackTarget(this); }
        public override void OnDisable()         { base.OnDisable(); PhotonNetwork.RemoveCallbackTarget(this); }

        public static bool IsConnected()         { return PhotonNetwork.IsConnected; }

        public static void Connect()
        {
            if (IsConnected()) return;
            
            AppSettings appSettings = PhotonNetwork.PhotonServerSettings.AppSettings;
            appSettings.Server = "127.0.0.1";
            PhotonNetwork.ConnectUsingSettings();
        }

        public static void Disconnect()
        {
            if (IsConnected()) PhotonNetwork.Disconnect();
        }

        public static void JoinRoom(string name) { PhotonNetwork.JoinRoom(name);  }
        public static void JoinRandomRoom()      { PhotonNetwork.JoinRandomRoom(); }
        public static void LeaveRoom()           { PhotonNetwork.LeaveRoom(); }

        public enum Visibility
        {
            Private, Public
        }

        #region Non Overrides
        public static void CreateRoom(string name, byte maxPlayers, Visibility visibility)
        {
            RoomOptions roomOptions = new RoomOptions();

            switch (visibility)
            {
                case Visibility.Private: roomOptions.IsVisible = false; break;
                case Visibility.Public:  roomOptions.IsVisible = true;  break;
            }

            roomOptions.IsOpen = true;
            roomOptions.MaxPlayers = (byte)maxPlayers;

            PhotonNetwork.CreateRoom(name, roomOptions);
        }

        public static bool IsMasterClient()
        {
            return PhotonNetwork.IsMasterClient;
        }
        
        [PunRPC]
        public static void OnPlayerConnected(PhotonView photonView)
        {

        }
        #endregion

        #region Overrides
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
            Debug.Log("Successfully created room!");
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
        #endregion
    }

}
