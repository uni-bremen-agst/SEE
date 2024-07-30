using SEE.Net.Actions;
using SEE.UI.Notification;
using System;
using System.Collections.Generic;
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
        /// Collect and preserve the fragments of packages.
        public Dictionary<string, List<Fragment>> fragmentsGatherer = new();

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
            if (!networkClient.PlayerObject.TryGetComponent(out ClientActionNetwork clientNetwork))
            {
                Debug.LogError($"The player object does not have a {nameof(ClientActionNetwork)} component.\n");
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
                    clientNetwork.ExecuteActionClientRpc(serializedAction, recipients);
                }
            }
        }

        /// <summary>
        /// Receives the fragments of a packet and performs the broadcast when all fragments of the packet are present.
        /// </summary>
        /// <param name="id">The packet id.</param>
        /// <param name="packetSize">The size of fragments of the packet.</param>
        /// <param name="currentFragment">The current fragment.</param>
        /// <param name="data">The data of the fragment</param>
        /// <param name="recipients">The recipients of the call.</param>
        [ServerRpc(RequireOwnership = false)]
        public void BroadcastActionServerRpc(string id, int packetSize, int currentFragment, string data, ulong[] recipients)
        {
            Fragment fragment = new(id, packetSize, currentFragment, data);
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> fragments))
            {
                fragments.Add(fragment);
            } else
            {
                List<Fragment> frags = new() { fragment };
                fragmentsGatherer.Add(fragment.PacketID, frags);
            }
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> f) 
                && Fragment.CombineFragments(f) != "")
            {
                BroadcastActServerRpc(fragment.PacketID, recipients);
            }
        }

        /// <summary>
        /// Performs the broadcast. First, the serialized string is assembled.
        /// </summary>
        /// <param name="key">The packet id.</param>
        /// <param name="recipients">The recipients of the call.</param>
        [ServerRpc(RequireOwnership = false)]
        private void BroadcastActServerRpc(string key, ulong[] recipients)
        {
            if (!IsServer && !IsHost)
            {
                return;
            } 
            if (fragmentsGatherer.TryGetValue(key, out List<Fragment> fragments))
            {
                string serializedAction = Fragment.CombineFragments(fragments);
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
                        foreach(Fragment fragment in fragments)
                        {
                            clientNetwork.ReceiveActionClientRpc(fragment.PacketID, fragment.PacketSize, 
                                fragment.CurrentFragment, fragment.Data, recipients);
                        }
                    }
                }
                fragmentsGatherer.Remove(key);
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
