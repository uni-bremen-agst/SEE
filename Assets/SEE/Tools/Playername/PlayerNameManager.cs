using SEE.UI.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Assets.SEE.Tools.Playername
{

    /// <summary>
    /// Manger for Server to store playernames of the connected clients
    /// </summary>
    public static class PlayerNameManager
    {
        // Dictionary to store player names with their client IDs as keys
        private static Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();

        /// <summary>
        /// Adds all clients in the dictonary
        /// </summary>
        /// <param name="clientId"> client id of the connected client</param>
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
        /// Method to recievce a playerName by its key
        /// </summary>
        /// <param name="clientId">key in dictionary</param>
        /// <returns>corresponding playerName</returns>
        public static string GetPlayerName(ulong clientId)
        {
            return playerNames.ContainsKey(clientId) ? playerNames[clientId] : "UnknownPlayerName";
        }

        /// <summary>
        /// Removes dictionary entry by its key
        /// </summary>
        /// <param name="clientId">clientId as key</param>
        public static void RemovePlayerName(ulong clientId)
        {
            if (playerNames.ContainsKey(clientId))
            {
                playerNames.Remove(clientId);
            }
        }

    }

}
