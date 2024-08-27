using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Sends and shows the player name for each player.
    /// </summary>
    /// <remarks>This behaviour is attached to all player prefabs.
    /// A player prefab will be instantiated for remote players and
    /// local players. That is, for instance, if there are three clients,
    /// we have altogether nine player instances, three for each client,
    /// where one of the three is the local player and the other two are
    /// remote players. Whether a player is local or remote is determined
    /// by the <see cref="NetworkObject.IsLocalPlayer"/> property (if true,
    /// it is the local player).
    /// </remarks>
    /// <remarks>Players are identified here by <see cref="NetworkBehaviour.NetworkObjectId"/>s,
    /// not be confused with client ids.</remarks>
    public class PlayerName : NetworkBehaviour
    {
        /// <summary>
        /// Reference to the TextMeshPro component to display the player's name.
        /// </summary>
        [SerializeField]
        private TMP_Text displayNameText;

        /// <summary>
        /// If this is executed by the local player, the player name is retrieved from the
        /// user configuration in <see cref="Net.Network.PlayerName"/> and sent to the server.
        /// If this is executed by a remote player, the player name is requested from the server.
        /// </summary>
        /// <remarks>
        /// OnNetworkSpawn is invoked on each NetworkBehaviour associated with a NetworkObject
        /// when it's spawned. This is where all netcode-related initialization should occur.
        /// You can still use Awake and Start to do things like finding components and assigning
        /// them to local properties, but if NetworkBehaviour.IsSpawned is false then don't
        /// expect netcode-distinguishing properties (like IsClient, IsServer, IsHost,
        /// for example) to be accurate within Awake and Start methods.
        /// </remarks>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Net.Network networkConfig = FindObjectOfType<Net.Network>() ?? throw new("Network configuration not found");

            // Player name that is set in the network configuration or a default name.
            string playerName = string.IsNullOrEmpty(networkConfig.PlayerName) ? "N.N." : networkConfig.PlayerName;

            if (IsLocalPlayer)
            {
                // If this player is the local player, we can use the player name from the network configuration.
                SendPlayerNameToServerRpc(NetworkObjectId, playerName);
            }
            else
            {
                // In the case of a remote player, we need to request the player name from the server.
                RequestPlayerNameFromServerRpc(NetworkObjectId);
            }
        }

        /// <summary>
        /// Called by Unity Netcode when the network object is despawned. Removes the player's name
        /// from the server.
        /// </summary>
        /// <remarks><see cref="NetworkBehaviour.OnNetworkDespawn"/> is invoked on each
        /// <see cref=">NetworkBehaviour"/> associated with a <see cref="NetworkObject"/>
        /// when it's despawned. This is where all netcode cleanup code should occur,
        /// but isn't to be confused with destroying.</remarks>
        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                // It is sufficent to remove the name from the server only
                // once for all instances of the player on all clients.
                // We let the local player do this.
                RemovePlayerNameFromServerRpc(NetworkObjectId);
            }
        }

        /// <summary>
        /// RPC method to remove a player's name from the server.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player whose
        /// player name should be deregistered</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void RemovePlayerNameFromServerRpc(ulong networkObjectId)
        {
            PlayerNameMap.RemovePlayerName(networkObjectId);
        }

        /// <summary>
        /// RPC method to send the player's name from a client to the server, which in turn
        /// distributes it to all clients.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player receiving given
        /// <paramref name="playername"/></param>
        /// <param name="playername">Player name that will be sent</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void SendPlayerNameToServerRpc(ulong networkObjectId, string playername)
        {
            PlayerNameMap.AddOrUpdatePlayerName(networkObjectId, playername);

            // The server will send the name to all other clients, including the client
            // that sent the name. Actually, this call goes to all player instances on
            // all clients. Thus, if there are three clients, each client will receive
            // this call three times, once for each player.
            SendPlayerNameToAllClientsRpc(networkObjectId, playername);
        }

        /// <summary>
        /// RPC method to send a player's new name from the server to all clients for rendering.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player receiving
        /// given <paramref name="playername"/></param>
        /// <param name="playername">Playername that will be sent</param>
        /// <remarks>This method is called by the server, but executed on all clients including
        /// the client who is possibly acting as a server, too (i.e., the host).</remarks>
        [Rpc(SendTo.ClientsAndHost)]
        private void SendPlayerNameToAllClientsRpc(ulong networkObjectId, string playername)
        {
            RenderNetworkPlayerName(networkObjectId, playername);
        }

        /// <summary>
        /// Called by a client to request its player name from the server. The server response is
        /// not immediate, but will be sent by the server to the client via <see cref="SendPlayerNameToClientRpc"/>.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player requesting its player name</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void RequestPlayerNameFromServerRpc(ulong networkObjectId)
        {
            string playerName = PlayerNameMap.GetPlayerName(networkObjectId);
            SendPlayerNameToClientRpc(networkObjectId, playerName, RpcTarget.Single(networkObjectId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Called by the server to send the player's name to the specific client requesting it
        /// (<seealso cref="RequestPlayerNameFromServerRpc"/>).
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player receiving the player name</param>
        /// <param name="playerName">New player name for <paramref name="networkObjectId"/></param>
        /// <remarks>This method is called by the server, but executed on the client which
        /// called <see cref="RequestPlayerNameFromServerRpc"/>.</remarks>
        [Rpc(SendTo.SpecifiedInParams)]
        void SendPlayerNameToClientRpc(ulong networkObjectId, string playerName, RpcParams _)
        {
            RenderNetworkPlayerName(networkObjectId, playerName);
        }

        /// <summary>
        /// Render the player's name on the TextMeshPro component <see cref="displayNameText"/>
        /// (and also renames the top-most game object this component is attached to accordingly),
        /// yet only if the given <paramref name="networkObjectId"/> matches the <see cref="NetworkObjectId"/> of
        /// this player. Otherwise the player name is for another player (name changes are
        /// broadcasted to all clients).
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkObjectId"/> of the player receiving the player name</param>
        /// <param name="playername">Player name that should be set</param>
        private void RenderNetworkPlayerName(ulong networkObjectId, string playername)
        {
            if (NetworkObjectId == networkObjectId)
            {
                displayNameText.text = playername;
                gameObject.transform.root.name = playername;
            }
        }
    }
}
