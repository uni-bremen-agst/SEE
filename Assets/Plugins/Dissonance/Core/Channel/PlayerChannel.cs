using System;
using Dissonance.Extensions;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// A channel sending voice data to a player. Dispose this struct to close the channel.
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public struct PlayerChannel
        : IChannel<string>, IEquatable<PlayerChannel>
    {
        private readonly ushort _subscriptionId;
        private readonly string _playerId;
        private readonly ChannelProperties _properties;
        private readonly PlayerChannels _channels;

        internal PlayerChannel(ushort subscriptionId, string playerId, PlayerChannels channels, ChannelProperties properties)
        {
            _subscriptionId = subscriptionId;
            _playerId = playerId;
            _channels = channels;
            _properties = properties;
        }

        /// <inheritdoc />
        public ushort SubscriptionId
        {
            get { return _subscriptionId; }
        }

        /// <summary>
        /// The name of the player this channel is sending voice data to
        /// </summary>
        /// ReSharper disable once InheritdocConsiderUsage
        [NotNull] public string TargetId
        {
            get { return _playerId; }
        }

        /// <inheritdoc />
        ChannelProperties IChannel<string>.Properties
        {
            get { return _properties; }
        }

        [NotNull] internal ChannelProperties Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// Get a value indicating if this channel has been closed
        /// </summary>
        public bool IsOpen
        {
            get { return _channels.Contains(this); }
        }

        /// <summary>
        /// Gets or sets a value indicating if this channel should be played on other clients with 3D positional audio.
        /// </summary>
        public bool Positional
        {
            get
            {
                CheckValidProperties();
                return _properties.Positional;
            }
            set
            {
                CheckValidProperties();
                _properties.Positional = value;
            }
        }

        /// <summary>
        /// Gets or sets the speaker priority for this channel.
        /// </summary>
        public ChannelPriority Priority
        {
            get
            {
                CheckValidProperties();
                return _properties.Priority;
            }
            set
            {
                CheckValidProperties();
                _properties.Priority = value;
            }
        }

        /// <summary>
        /// Get or set what amplitude multiplier is applied to this channel
        /// </summary>
        public float Volume
        {
            get
            {
                CheckValidProperties();
                return _properties.AmplitudeMultiplier;
            }
            set
            {
                CheckValidProperties();
                _properties.AmplitudeMultiplier = value;
            }
        }

        /// <summary>
        /// Close this channel (stop sending data)
        /// </summary>
        public void Dispose()
        {
            _channels.Close(this);
        }

        /// <summary>
        /// Check that we're not trying to access a closed channel
        /// </summary>
        private void CheckValidProperties()
        {
            if (_properties.Id != _subscriptionId)
                throw new DissonanceException("Cannot access channel properties on a closed channel.");
        }

        public bool Equals(PlayerChannel other)
        {
            return _subscriptionId == other._subscriptionId
                   && string.Equals(_playerId, other._playerId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is PlayerChannel && Equals((PlayerChannel)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_subscriptionId.GetHashCode() * 397) ^ _playerId.GetFnvHashCode();
            }
        }
    }
}
