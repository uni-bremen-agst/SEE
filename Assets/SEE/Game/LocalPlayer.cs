using SEE.GO.Menu;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides access to the game object representing the active local player,
    /// that is, the player executing on this local instance of Unity.
    /// </summary>
    public static class LocalPlayer
    {
        /// <summary>
        /// The game object representing the active local player, that is, the player
        /// executing on this local instance of Unity.
        /// </summary>
        public static GameObject Instance;

        /// <summary>
        /// Returns the <see cref="PlayerMenu"/> attached to the local player <see cref="Instance"/>
        /// or any of its descendants (including inactive ones).
        /// </summary>
        /// <param name="playerMenu">the resulting <see cref="PlayerMenu"/>; <c>null</c> if none
        /// could be found</param>
        /// <returns>true if a <see cref="PlayerMenu"/> could be found</returns>
        internal static bool TryGetPlayerMenu(out PlayerMenu playerMenu)
        {
            if (Instance == null)
            {
                Debug.LogError($"Local player is null'.\n");
                playerMenu = null;
                return false;
            }
            playerMenu = Instance.GetComponentInChildren<PlayerMenu>(includeInactive: true);
            if (playerMenu == null)
            {
                Debug.LogError($"Couldn't find component '{nameof(PlayerMenu)}' "
                               + $"on local player named '{Instance.name}'.\n");
                return false;
            }
            return true;
        }
    }
}
