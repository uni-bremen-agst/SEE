using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using Cysharp.Threading.Tasks;
using SEE.Net.Actions;
using SEE.Net.Util;
using SEE.Game.City;
using SEE.Utils.Paths;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

namespace SEE.Net
{
    /// <summary>
    /// RPCs on the client side.
    /// </summary>
    public class ClientActionNetwork : NetworkBehaviour
    {
        /// <summary>
        /// Fetches the multiplayer city files from the backend and syncs the current
        /// server state with this client.
        /// </summary>
        public void Start()
        {
            Debug.Log("Starting client action network!");
            if (IsServer)
            {
                Debug.LogWarning("The server should never execute this...");
            }
            else if (IsHost)
            {
                Debug.Log("This is the host, synchronizing Multiplayer files...");
                InitializeCitiesAsync().Forget();
            }
            else
            {
                Debug.Log("This is a client, registering with server...");
                Network.ServerNetwork.Value?.RegisterClientServerRpc();
            }
        }

        /// <summary>
        /// Executes an Action, even if the sender and this client are the same, this is used
        /// for synchronizing server state.
        /// </summary>
        [ClientRpc]
        public void ExecuteActionUnsafeClientRpc(string serializedAction)
        {
            if (IsHost || IsServer)
            {
                return;
            }
            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            action.ExecuteOnClient();
        }

        /// <summary>
        /// Executes an action on the client.
        /// </summary>
        [ClientRpc]
        public void ExecuteActionClientRpc(string serializedAction)
        {
            if (IsHost || IsServer)
            {
                return;
            }
            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            if (action.Requester != NetworkManager.Singleton.LocalClientId)
            {
                action.ExecuteOnClient();
            }
        }

        /// <summary>
        /// Initiates the synchronization process with the backend and game server.
        /// This RPC is called by the game server after the client has registered itself.
        /// </summary>
        [ClientRpc]
        public void SyncFilesClientRpc(string serverId, string backendDomain)
        {
            Network.ServerId = serverId;
            Network.BackendDomain = backendDomain;

            InitializeClientAsync().Forget();
        }

        /// <summary>
        /// Initializes the client by initializing cities and synchronizing with the server.
        /// </summary>
        private async UniTask InitializeClientAsync()
        {
            await InitializeCitiesAsync();
            Network.ServerNetwork.Value?.SyncClientServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Downloads the Multiplayer files and instantes Code Cities.
        /// </summary>
        private async UniTask InitializeCitiesAsync()
        {
            if (!string.IsNullOrWhiteSpace(Network.ServerId) && !string.IsNullOrWhiteSpace(Network.BackendDomain))
            {
                BackendSyncUtil.ClearMultiplayerData();
                await BackendSyncUtil.DownloadAllFilesAsync();
            }
            Debug.Log("Instantiating SEECity...");
            await BackendSyncUtil.LoadCitiesAsync();
        }

    }
}
