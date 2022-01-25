using System;
using Dissonance.Audio.Playback;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// Represents an open channel the local client is receiving voice with
    /// </summary>
    public struct RemoteChannel
    {
        private readonly string _target;
        private readonly ChannelType _type;
        private readonly PlaybackOptions _options;

        /// <summary>
        /// Get the type of this channel
        /// </summary>
        public ChannelType Type { get { return _type; } }

        /// <summary>
        /// Get the playback options set for this channel
        /// </summary>
        public PlaybackOptions Options { get { return _options; } }

        /// <summary>
        /// Get the name of the target of this channel. Either room name or player name depending upon the type of this channel
        /// </summary>
        public string TargetName { get { return _target; } }

        internal RemoteChannel([NotNull] string targetName, ChannelType type, PlaybackOptions options)
        {
            if (targetName == null) throw new ArgumentNullException("targetName");

            _target = targetName;
            _type = type;
            _options = options;
        }
    }
}
