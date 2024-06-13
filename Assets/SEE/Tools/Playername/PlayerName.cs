using UnityEngine;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Sends and show playerName for each player
/// </summary>
public class PlayerName : NetworkBehaviour
{
    /// <summary>
    /// Reference to the TextMeshPro component to display the player's name.
    /// </summary>
    [SerializeField] private TMP_Text displayNameText;

    /// <summary>
    /// Variable to store the player's name.
    /// </summary>
    private string playerName;

    /// <summary>
    /// network config to read playername
    /// </summary>
    private SEE.Net.Network networkConfig;

    private void Start()
    {
        networkConfig = FindObjectOfType<SEE.Net.Network>();
        if (networkConfig == null)
        {
            Debug.LogError("Network configuration not found");
            return;
        }
        // Read the player's name when the script starts.
        ReadPlayerName();
    }

    /// <summary>
    /// Read the player's name from networkconfig.
    /// </summary>
    private void ReadPlayerName()
    {
        string playerName = networkConfig.PlayerName;

        // If a player name is retrieved, store it.
        if (playerName != null && playerName.Length != 0)
        {
            this.playerName = playerName;
        }
        else
        {
            this.playerName = "uknown Playername";
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
            DisplayLocalPlayername();
            DisplayPlayernameOnAllOtherClients();
        }

    }

    /// <summary>
    /// RPC method to send the player's name to all clients for rendering.
    /// </summary>
    /// <param name="playername">Playername that will be sent</param>
    [ClientRpc]
    private void SendPlayernameToClientsToRenderItClientRPC(string playername)
    {
        RenderNetworkPlayername(playername);
    }

    /// <summary>
    /// RPC method to receive the player's name from a client and distribute it to all clients for rendering.
    /// </summary>
    /// <param name="playername">Playername that will be sent</param>
    /// <param name="serverRpcParams"></param>
    [ServerRpc]
    private void GetPlayernameFromClientAndSendItToClientsToRenderItServerRPC(string playername)
    {
        // The server will render this playername onto his instance of the TextMeshPro.
        RenderNetworkPlayername(playername);

        // The server will send the name to all other clients (not the owner and server)
        SendPlayernameToClientsToRenderItClientRPC(playername);
    }

    /// <summary>
    /// Render the player's name on the TextMeshPro component.
    /// </summary>
    /// <param name="playername">Playername that should be set</param>
    private void RenderNetworkPlayername(string playername)
    {
        displayNameText.text = playername;
    }

    /// <summary>
    /// Display the local player's name as "Me".
    /// </summary>
    private void DisplayLocalPlayername()
    {
        displayNameText.text = "Me";
    }

    /// <summary>
    /// Display the player's name on all other clients.
    /// </summary>
    private void DisplayPlayernameOnAllOtherClients()
    {
        // Send the player's name to the server for distribution to other clients.
        if (!IsServer)
        {
            GetPlayernameFromClientAndSendItToClientsToRenderItServerRPC(playerName);
        }
        else
        {
            // Send the player's name to all clients for rendering.
            SendPlayernameToClientsToRenderItClientRPC(playerName);
        }
    }
}
