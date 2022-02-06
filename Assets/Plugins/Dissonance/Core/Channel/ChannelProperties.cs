using System;
using Dissonance.Audio.Capture;

namespace Dissonance
{
    public sealed class ChannelProperties
    {
        private readonly IChannelPriorityProvider _defaultPriority;

        /// <summary>
        /// Get a unique ID for this channel
        /// </summary>
        public ushort Id { get; internal set; }

        /// <summary>
        /// Get or set if audio sent though this channel should be played back with positional 3D audio.
        /// </summary>
        public bool Positional { get; set; }

        /// <summary>
        /// Get or set what priority should this channel have. If set to None then a fallback value will be used (DissonanceComms:PlayerPriority)
        /// </summary>
        public ChannelPriority Priority { get; set; }

        /// <summary>
        /// This calculates what priority to actually use to transmit with. If Priority is set to None then this will fall back to some other default value
        /// </summary>
        internal ChannelPriority TransmitPriority
        {
            get
            {
                if (Priority == ChannelPriority.None)
                    return _defaultPriority.DefaultChannelPriority;
                return Priority;
            }
        }

        private float _amplitudeMultiplier;

        /// <summary>
        /// Get or set what volume this channel should have
        /// </summary>
        internal float AmplitudeMultiplier
        {
            get { return _amplitudeMultiplier; }
            set { _amplitudeMultiplier = Math.Min(2, Math.Max(0, value)); }
        }

        internal ChannelProperties(IChannelPriorityProvider defaultPriority)
        {
            _defaultPriority = defaultPriority;
        }
    }
}
