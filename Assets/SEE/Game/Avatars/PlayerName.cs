using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.Assertions;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Sends and shows playerName for each player.
    /// </summary>
    /// <remarks>This behaviour is attached to all player prefabs.
    /// A player prefab will be instantiated for remote players and
    /// local players. That is, for instance, if there are three clients,
    /// we have altogether nine player instances, three for each client,
    /// where one of the three is the local player and the other two are
    /// remote players. Whether a player is local or remote is determined
    /// by the <see cref="NetworkObject.IsOwner"/> property (if true,
    /// it is the local player).
    /// </remarks>
    public class PlayerName : NetworkBehaviour
    {
        /// <summary>
        /// Reference to the TextMeshPro component to display the player's name.
        /// </summary>
        [SerializeField]
        private TMP_Text displayNameText;


        private void Start()
        {
            Net.Network networkConfig = FindObjectOfType<Net.Network>() ?? throw new("Network configuration not found");

            // Player name that is set in the network configuration or a default name.
            string playerName = string.IsNullOrEmpty(networkConfig.PlayerName) ? "N.N." : networkConfig.PlayerName;

            if (IsOwner)
            {
                // If this player is the local player, we can use the player name from the network configuration.
                SendPlayerNameFromClientToServerRpc(OwnerClientId, playerName);
            }
            else
            {
                // In the case of a remote player, we need to request the player name from the server.
                RequestPlayerNameFromServerRpc();
            }
        }

        /// <summary>
        /// RPC method to receive the player's name from a client and distribute it to all clients.
        /// </summary>
        /// <param name="clientID">ClientID of the owner of the player</param>
        /// <param name="playername">Playername that will be sent</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void SendPlayerNameFromClientToServerRpc(ulong clientID, string playername)
        {
            PlayerNameManager.AddOrUpdatePlayerName(clientID, playername);

            // The server will send the name to all other clients, including the client
            // that sent the name.
            SendPlayerNameToClientsRpc(playername);
        }

        /// <summary>
        /// RPC method to send the player's name to all clients for rendering.
        /// </summary>
        /// <param name="playername">Playername that will be sent</param>
        /// <remarks>This method is called by the server, but executed on all clients.</remarks>
        [Rpc(SendTo.NotServer)]
        private void SendPlayerNameToClientsRpc(string playername)
        {
            RenderNetworkPlayerName(playername);
        }

        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [Rpc(SendTo.Server)]
        private void RequestPlayerNameFromServerRpc(RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            string playerName = PlayerNameManager.GetPlayerName(clientId);
            Debug.Log($"RequestPlayerNameFromServerRpc(clientId={clientId}) => playerName={playerName}\n");
            SendPlayerNameToClientRpc(playerName, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        /// <remarks>This method is called by the server, but executed on the client which
        /// called <see cref="RequestPlayerNameFromServerRpc"/>.</remarks>
        [Rpc(SendTo.SpecifiedInParams)]
        void SendPlayerNameToClientRpc(string playerName, RpcParams _)
        {
            Debug.Log($"SendPlayerNameToClientRpc({playerName})\n");
            RenderNetworkPlayerName(playerName);
        }

        /// <summary>
        /// Render the player's name on the TextMeshPro component.
        /// </summary>
        /// <param name="playername">Playername that should be set</param>
        private void RenderNetworkPlayerName(string playername)
        {
            displayNameText.text = playername;
        }

        /*
        /// <summary>
        /// Display the player's name on all other clients.
        /// </summary>
        private void DisplayPlayernameOnAllOtherClients()
        {
            // Send the player's name to the server for distribution to other clients.
            if (!IsServer)
            {
                SendPlayernameFromClientToServerRpc(OwnerClientId, playerName);
            }
            else
            {
                // Send the player's name to all clients for rendering.
                SendPlayernameToClientsRpc(playerName);
            }
        }
        */
    }
}
