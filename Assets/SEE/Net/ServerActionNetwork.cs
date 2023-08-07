using SEE.Net.Actions;
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
        public void BroadcastActionServerRpc(string serializedAction)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                ClientActionNetwork clientNetwork = client.PlayerObject.GetComponent<ClientActionNetwork>();
                clientNetwork.ExecuteActionClientRpc(serializedAction);
            }
        }
    }
}
