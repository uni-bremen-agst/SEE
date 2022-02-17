namespace Dissonance.Audio.Playback
{
    public struct PlaybackOptions
    {
        private readonly bool _isPositional;
        private readonly float _amplitudeMultiplier;
        private readonly ChannelPriority _priority;

        public PlaybackOptions(bool isPositional, float amplitudeMultiplier, ChannelPriority priority)
        {
            _isPositional = isPositional;
            _amplitudeMultiplier = amplitudeMultiplier;
            _priority = priority;
        }

        /// <summary>
        /// Get if audio on this channel is positional
        /// </summary>
        public bool IsPositional { get { return _isPositional; } }

        /// <summary>
        /// Get the amplitude multiplier applied to audio played through this channel
        /// </summary>
        public float AmplitudeMultiplier { get { return _amplitudeMultiplier; } }

        /// <summary>
        /// Get the priority of audio on this channel
        /// </summary>
        public ChannelPriority Priority { get { return _priority; } }
    }
}
