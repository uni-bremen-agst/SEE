using Cysharp.Threading.Tasks;
using SEE.Net.Actions;
using SEE.Net.Util;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Gateway to the server.
    /// </summary>
    public class ServerActionNetwork : NetworkBehaviour
    {
        /// <summary>
        /// Fetches the multiplayer city files from the backend on the server or host.
        /// </summary>
        public void Start()
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
        /// Syncs the current state of the server with the connecting client.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncClientServerRpc(ulong clientId)
        {
            foreach (string serializedAction in Network.NetworkActionList.ToList())
            {
                ExecuteActionUnsafeClientRpc(serializedAction, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            }
        }

        /// <summary>
        /// Sends an action to all clients in the recipients list, or to all connected clients if <c>recipients</c> is <c>null</c>.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void BroadcastActionServerRpc(string serializedAction, ulong[] recipientIds)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            if (recipientIds != null && recipientIds.Length == 0)
            {
                return;
            }
            // TODO check if empty list was ever used for targeting all clients
            // TODO should this be sent to the caller as well or should we filter them out?

            AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
            if (deserializedAction.ShouldBeSentToNewClient)
            {
                Network.NetworkActionList.Add(serializedAction);
            }
            deserializedAction.ExecuteOnServer();


            if (recipientIds == null) {
                ExecuteActionClientRpc(serializedAction);
            }
            else {
                using (var targetClientIds = new NativeArray<ulong>(recipientIds, Allocator.Temp))
                {
                    ExecuteActionClientRpc(serializedAction, RpcTarget.Group(targetClientIds, RpcTargetUse.Temp));
                }
            }
        }

        /// <summary>
        /// Request client synchronization.
        /// This RPC is called by the client to initiate the synchronization process.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSynchronizationServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }

            ulong clientId = serverRpcParams.Receive.SenderClientId;
            SyncFilesClientRpc(Network.ServerId, Network.BackendDomain, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Executes an Action, even if the sender and this client are the same, this is used
        /// for synchronizing server state.
        /// </summary>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        public void ExecuteActionUnsafeClientRpc(string serializedAction, RpcParams rpcParams = default)
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
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        public void ExecuteActionClientRpc(string serializedAction, RpcParams rpcParams = default)
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
        [Rpc(SendTo.SpecifiedInParams)]
        public void SyncFilesClientRpc(string serverId, string backendDomain, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }
            Network.ServerId = serverId;
            Network.BackendDomain = backendDomain;

            BackendSyncUtil.InitializeClientAsync().Forget();
        }
    }
}
