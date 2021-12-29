using System;
using Dissonance.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// A channel sending voice data to a room. Dispose this struct to close the channel.
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public struct RoomChannel
        : IChannel<string>, IEquatable<RoomChannel>
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(RoomChannel).Name);

        private readonly ushort _subscriptionId;
        private readonly string _roomId;
        private readonly ChannelProperties _properties;
        private readonly RoomChannels _channels;

        internal RoomChannel(ushort subscriptionId, string roomId, RoomChannels channels, ChannelProperties properties)
        {
            _subscriptionId = subscriptionId;
            _roomId = roomId;
            _channels = channels;
            _properties = properties;
        }

        /// <inheritdoc />
        public ushort SubscriptionId
        {
            get { return _subscriptionId; }
        }

        /// <summary>
        /// The name of the room this channel is sending voice data to
        /// </summary>
        /// ReSharper disable once InheritdocConsiderUsage
        [NotNull] public string TargetId
        {
            get { return _roomId; }
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
                _properties.AmplitudeMultiplier = Mathf.Clamp(value, 0, 2);
            }
        }

        /// <summary>
        /// Close this channel (stop sending data)
        /// </summary>
        public void Dispose()
        {
            _channels.Close(this);
        }

        private void CheckValidProperties()
        {
            if (_properties.Id != _subscriptionId)
            {
                throw Log.CreateUserErrorException(
                    "Attempted to access a disposed channel",
                    "Attempting to get or set channel properties after calling Dispose() on a channel",
                    "https://placeholder-software.co.uk/dissonance/docs/Tutorials/Directly-Using-Channels",
                    "DE77DE73-8DBF-4802-A413-B9A5D77A5189"
                );
            }
        }

        public bool Equals(RoomChannel other)
        {
            return _subscriptionId == other._subscriptionId
                   && string.Equals(_roomId, other._roomId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is RoomChannel && Equals((RoomChannel)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_subscriptionId.GetHashCode() * 397) ^ _roomId.GetFnvHashCode();
            }
        }
    }
}
