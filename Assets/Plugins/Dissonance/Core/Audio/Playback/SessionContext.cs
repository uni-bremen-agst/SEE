using System;
using Dissonance.Extensions;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    public struct SessionContext
        : IEquatable<SessionContext>
    {
        /// <summary>
        /// Name of the player who is speaking in this session
        /// </summary>
        public readonly string PlayerName;

        /// <summary>
        /// Unique ID for this session (IDs may be re-used after a *very* long time)
        /// </summary>
        public readonly uint Id;

        public SessionContext([NotNull] string playerName, uint id)
        {
            if (playerName == null)
                throw new ArgumentNullException("playerName", "Cannot create a session context with a null player name");

            PlayerName = playerName;
            Id = id;
        }

        public bool Equals(SessionContext other)
        {
            return string.Equals(PlayerName, other.PlayerName)
                && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is SessionContext && Equals((SessionContext)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (PlayerName.GetFnvHashCode() * 397) ^ (int)Id;
            }
        }
    }
}
