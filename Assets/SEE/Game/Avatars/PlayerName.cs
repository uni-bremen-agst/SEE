using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Sends and shows playerName for each player.
    /// </summary>
    /// <remarks>This behaviour is attached to the player prefabs. It will be executed
    /// by both server and clients.</remarks>
    public class PlayerName : NetworkBehaviour
    {
        /// <summary>
        /// Reference to the TextMeshPro component to display the player's name.
        /// </summary>
        [SerializeField]
        private TMP_Text displayNameText;

        /// <summary>
        /// Variable to store the player's name.
        /// </summary>
        private string playerName;

        private void Start()
        {
            Net.Network networkConfig = FindObjectOfType<Net.Network>() ?? throw new("Network configuration not found");

            playerName = string.IsNullOrEmpty(networkConfig.PlayerName) ? "Unknown" : networkConfig.PlayerName;

            // Display the local player's name as "Me".
            if (IsOwner)
            {
                displayNameText.text = "Me";
            }

            if (IsServer)
            {
                // Add or update the player's name in a dictionary managed by server.
                PlayerNameManager.AddOrUpdatePlayerName(OwnerClientId, playerName);
            }
        }

        private void Update()
        {
            // If the NetworkObject is not yet spawned, exit early.
            if (!IsSpawned)
            {
                return;
            }

            if (IsOwner)
            {
                DisplayPlayernameOnAllOtherClients();
            }
        }

        /// <summary>
        /// RPC method to send the player's name to all clients for rendering.
        /// </summary>
        /// <param name="playername">Playername that will be sent</param>
        [ClientRpc]
        private void SendPlayernameToClientsClientRPC(string playername)
        {
            // Only update the display name if this is not the owner.
            if (!IsOwner)
            {
                RenderNetworkPlayerName(playername);
            }
        }

        /// <summary>
        /// RPC method to receive the player's name from a client and distribute it to all clients.
        /// </summary>
        /// <param name="playername">Playername that will be sent</param>
        /// <remarks>This method is called by clients, but executed on the server.</remarks>
        [ServerRpc]
        private void SendPlayernameFromClientsToServerServerRPC(ulong clientID, string playername)
        {
            // The server will render this playername onto his instance of the TextMeshPro.
            RenderNetworkPlayerName(playername);

            // Update the player's name in the dictionary
            if (IsServer)
            {
                PlayerNameManager.AddOrUpdatePlayerName(clientID, playername);
            }

            // The server will send the name to all other clients
            SendPlayernameToClientsClientRPC(playername);
        }

        /// <summary>
        /// Render the player's name on the TextMeshPro component.
        /// </summary>
        /// <param name="playername">Playername that should be set</param>
        private void RenderNetworkPlayerName(string playername)
        {
            displayNameText.text = playername;
        }

        /// <summary>
        /// Display the player's name on all other clients.
        /// </summary>
        private void DisplayPlayernameOnAllOtherClients()
        {
            // Send the player's name to the server for distribution to other clients.
            if (!IsServer)
            {
                SendPlayernameFromClientsToServerServerRPC(OwnerClientId, playerName);
            }
            else
            {
                // Send the player's name to all clients for rendering.
                SendPlayernameToClientsClientRPC(playerName);
            }
        }
    }
}
