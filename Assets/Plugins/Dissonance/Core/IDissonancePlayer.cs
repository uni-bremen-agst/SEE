using UnityEngine;

namespace Dissonance
{
    public enum NetworkPlayerType
    {
        /// <summary>
        /// Whether this is a remote or local player is currently unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// This is a local player
        /// </summary>
        Local,

        /// <summary>
        /// This is a remote player
        /// </summary>
        Remote
    }

    /// <summary>
    ///     Represents the player entity of a local or remote player on the voice network.
    /// </summary>
    public interface IDissonancePlayer
    {
        /// <summary>
        /// The ID of the player this object represents
        /// </summary>
        string PlayerId { get; }

        /// <summary>
        /// The position of this player
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// The rotation of this player
        /// </summary>
        Quaternion Rotation { get; }

        /// <summary>
        /// The type of the player this object represents
        /// </summary>
        NetworkPlayerType Type { get; }

        /// <summary>
        /// Indicates if this tracker is currently tracking a player (may be false while the player tracking is still initializing)
        /// </summary>
        bool IsTracking { get; }
    }
}