using SEE.Net.Actions;
using System;
using System.Linq;
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
        /// Syncs the current state of the server with the connecting client.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncClientServerRpc(ulong client)
        {
            NetworkClient networkClient = NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault
                                                ((connectedClient) => connectedClient.ClientId == client);
            if (networkClient == null)
            {
                Debug.LogError($"There is no {nameof(NetworkClient)} for the client {client}.\n");
                return;
            }
            if (!networkClient.PlayerObject.TryGetComponentOrLog(out ClientActionNetwork clientNetwork))
            {
                return;
            }

            foreach (string serializedAction in Network.NetworkActionList.ToList())
            {
                clientNetwork.ExecuteActionUnsafeClientRpc(serializedAction);
            }
        }

        /// <summary>
        /// Broadcasts an action to all clients in the recipients list, or to all connected clients if the list is empty
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void BroadcastActionServerRpc(string serializedAction, ulong[] recipients)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }

            AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
            if (deserializedAction.ShouldBeSentToNewClient)
            {
                Network.NetworkActionList.Add(serializedAction);
            }
            deserializedAction.ExecuteOnServer();
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (recipients == null || recipients.Contains(client.ClientId))
                {
                    ClientActionNetwork clientNetwork = client.PlayerObject.GetComponent<ClientActionNetwork>();
                    clientNetwork.ExecuteActionClientRpc(serializedAction);
                }
            }
        }

        /// <summary>
        /// Sends the server id to the client, this is not the internal id but the id given to the server by the backend.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncFilesServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            if (NetworkManager.Singleton.ConnectedClients[clientId] != null)
            {
                NetworkClient client = NetworkManager.Singleton.ConnectedClients[clientId];
                ClientActionNetwork clientNetwork = client.PlayerObject.GetComponent<ClientActionNetwork>();
                clientNetwork.SyncFilesClientRpc(Network.ServerId, Network.BackendDomain);
            }
        }
    }
}
