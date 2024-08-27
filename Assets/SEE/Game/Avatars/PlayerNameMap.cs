using SEE.UI.Notification;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Provides a mapping of <see cref="NetworkBehaviour.NetworkObjectId"/>s onto player names.
    /// </summary>
    /// <remarks>This class is used only by the server.</remarks>
    public static class PlayerNameMap
    {
        /// <summary>
        /// Stores names with their <see cref="NetworkBehaviour.NetworkObjectId"/> as a key.
        /// </summary>
        private static readonly Dictionary<ulong, string> playerNames = new();

        /// <summary>
        /// Sets the name of the given <paramref name="networkObjectId"/> to <paramref name="playerName"/>.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkBehaviour.NetworkObjectId"/> of the
        /// player network object</param>
        /// <param name="playerName">corresponding playerName</param>
        public static void AddOrUpdatePlayerName(ulong networkObjectId, string playerName)
        {
            if (playerNames.TryGetValue(networkObjectId, out string currentName))
            {
                // Only if there is a new player name, we need to update the dictionary.
                if (currentName != playerName)
                {
                    playerNames[networkObjectId] = playerName;
                    ShowNotification.Info("Connection", $"Client {networkObjectId} is now named {playerName}.");
                }
            }
            else
            {
                playerNames[networkObjectId] = playerName;
            }
        }

        /// <summary>
        /// Returns the playerName for the given <paramref name="networkObjectId"/>.
        /// If there is no entry for the given <paramref name="networkObjectId"/>,
        /// "Unknown" is returned.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkBehaviour.NetworkObjectId"/> of the
        /// player whose name is to be retrieved</param>
        /// <returns>corresponding playerName or "Unknown"</returns>
        public static string GetPlayerName(ulong networkObjectId)
        {
            if (playerNames.TryGetValue(networkObjectId, out string playerName))
            {
                return playerName;
            }
            else
            {
                Debug.LogError($"Player name for player {networkObjectId} is unknown.\n");
                return "Unknown";
            }
        }

        /// <summary>
        /// Removes the name of the player identified by <paramref name="networkObjectId"/>
        /// from the dictionary.
        /// </summary>
        /// <param name="networkObjectId"><see cref="NetworkBehaviour.NetworkObjectId"/> of the player
        /// to be removed</param>
        public static void RemovePlayerName(ulong networkObjectId)
        {
            playerNames.Remove(networkObjectId);
        }
    }
}
