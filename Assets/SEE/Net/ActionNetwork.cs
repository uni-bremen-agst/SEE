using Cysharp.Threading.Tasks;
using SEE.Net.Actions;
using SEE.Net.Util;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Gateway to the server.
    /// </summary>
    public class ActionNetwork : NetworkBehaviour
    {
        /// <summary>
        /// The server id to verify sender for client RPCs.
        /// </summary>
        public static ulong ServerClientId = NetworkManager.ServerClientId;

        /// <summary>
        /// Fetches the multiplayer city files from the backend on the server or host.
        /// </summary>
        private void Start()
        {
            if (!IsServer && !IsHost)
            {
                Debug.Log("Starting client action network!");
                RequestSynchronizationServerRpc();
                return;
            }
            Debug.Log("Starting server action network!");
            BackendSyncUtil.InitializeCitiesAsync().Forget();
        }

        /// <summary>
        /// Sends an action to all clients in the recipients list, or to all connected clients (except the sender) if <c>recipients</c> is <c>null</c>.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void BroadcastActionServerRpc(string serializedAction, ulong[] recipientIds = null, RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            if (recipientIds != null && recipientIds.Length == 0)
            {
                return;
            }

            AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
            if (deserializedAction.ShouldBeSentToNewClient)
            {
                Network.NetworkActionList.Add(serializedAction);
            }
            deserializedAction.ExecuteOnServer();

            if (recipientIds == null) {
                ulong senderId = rpcParams.Receive.SenderClientId;
                ExecuteActionClientRpc(serializedAction, RpcTarget.Not(senderId, RpcTargetUse.Temp));
            }
            else
            {
                using NativeArray<ulong> targetClientIds = new NativeArray<ulong>(recipientIds, Allocator.Temp);
                ExecuteActionClientRpc(serializedAction, RpcTarget.Group(targetClientIds, RpcTargetUse.Temp));
            }
        }

        /// <summary>
        /// Requests client synchronization.
        /// This RPC is called by the client to initiate the synchronization process.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void RequestSynchronizationServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }

            ulong senderId = rpcParams.Receive.SenderClientId;
            SyncFilesClientRpc(Network.ServerId, Network.BackendDomain, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Syncs the current state of the server with the connecting client.
        /// </summary>
        [Rpc(SendTo.Server)]
        internal void SyncClientServerRpc(ulong clientId)
        {
            foreach (string serializedAction in Network.NetworkActionList.ToList())
            {
                ExecuteActionUnsafeClientRpc(serializedAction, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            }
        }

        /// <summary>
        /// Executes an action, even if the sender and this client are the same. This is used
        /// for synchronizing server state.
        /// </summary>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteActionUnsafeClientRpc(string serializedAction, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a ExecuteActionUnsafeClientRpc from client ID {rpcParams.Receive.SenderClientId}!");
                return;
            }

            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            action.ExecuteOnClient();
        }

        /// <summary>
        /// Executes an action on the client.
        /// </summary>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteActionClientRpc(string serializedAction, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a ExecuteActionClientRpc from client ID {rpcParams.Receive.SenderClientId}!");
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
        [Rpc(SendTo.SpecifiedInParams)]
        private void SyncFilesClientRpc(string backendServerId, string backendDomain, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a SyncFilesClientRpc from client ID {rpcParams.Receive.SenderClientId}!");
                return;
            }

            Network.ServerId = backendServerId;
            Network.BackendDomain = backendDomain;

            BackendSyncUtil.InitializeClientAsync().Forget();
        }
    }
}
