using SEE.Net.Actions;
using System.Linq;
using Unity.Netcode;

namespace SEE.Net
{
    /// <summary>
    /// DOC
    /// </summary>
    public class ServerActionNetwork : NetworkBehaviour
    {
        [ServerRpc(RequireOwnership = false)]
        public void BroadcastActionServerRpc(string serializedAction, ulong[] recipients)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
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
