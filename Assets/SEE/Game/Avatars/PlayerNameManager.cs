using SEE.UI.Notification;
using System.Collections.Generic;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Provides a mapping of client IDs onto player names.
    /// </summary>
    /// <remarks>This class is used only by the server.</remarks>
    public static class PlayerNameManager
    {
        /// <summary>
        /// Dictionary to store player names with their client IDs as keys.
        /// </summary>
        private static readonly Dictionary<ulong, string> playerNames = new();

        /// <summary>
        /// Sets the name of the given <paramref name="clientId"/> to <paramref name="playerName"/>.
        /// </summary>
        /// <param name="clientId">client id of the connected client</param>
        /// <param name="playerName">corresponding playerName</param>
        public static void AddOrUpdatePlayerName(ulong clientId, string playerName)
        {
            if (playerNames.TryGetValue(clientId, out string currentName))
            {
                if (currentName != playerName)
                {
                    playerNames[clientId] = playerName;
                    ShowNotification.Info("Connection", $"Client {clientId} is now named {playerName}.");
                }
            }
            else
            {
                playerNames[clientId] = playerName;
            }
        }

        /// <summary>
        /// Returns the playerName for the given <paramref name="clientId"/>.
        /// </summary>
        /// <param name="clientId">key in dictionary</param>
        /// <returns>corresponding playerName</returns>
        public static string GetPlayerName(ulong clientId)
        {
            return playerNames.ContainsKey(clientId) ? playerNames[clientId] : "Unknown";
        }

        /// <summary>
        /// Removes dictionary entry for the given <paramref name="clientId"/>.
        /// </summary>
        /// <param name="clientId">clientId as key</param>
        public static void RemovePlayerName(ulong clientId)
        {
            playerNames.Remove(clientId);
        }
    }
}
