using SEE.Audio;
using SEE.Net.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// DOC
    /// </summary>
    public class ServerActionNetwork : NetworkBehaviour
    { 
        [ServerRpc(RequireOwnership = false)]
        public void SyncClientServerRpc(ulong client)
        {
            NetworkClient networkClient = NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault((connectedClient) => connectedClient.ClientId == client);
            if (networkClient == null)
            {
                return;
            }
            if(!networkClient.PlayerObject.TryGetComponent<ClientActionNetwork>(out var clientNetwork))
            {
                return;
            }

            foreach (string serializedAction in Network.NetworkActionList.ToList())
            {
                clientNetwork.ExecuteActionUnsafeClientRpc(serializedAction);
            }
        } 

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
                if(recipients == null || recipients.Contains(client.ClientId)) {
                    ClientActionNetwork clientNetwork = client.PlayerObject.GetComponent<ClientActionNetwork>();
                    clientNetwork.ExecuteActionClientRpc(serializedAction);
                }
            }
        }
    }
}
