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
    }
}