using Dissonance.Audio.Capture;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// A collection of channels to players
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public sealed class PlayerChannels
        : Channels<PlayerChannel, string>
    {
        internal PlayerChannels([NotNull] IChannelPriorityProvider priorityProvider)
            : base(priorityProvider)
        {
            OpenedChannel += (id, _) => Log.Debug("Opened channel to player '{0}'", id);
            ClosedChannel += (id, _) => Log.Debug("Closed channel to player '{0}'", id);
        }

        protected override PlayerChannel CreateChannel(ushort subscriptionId, string channelId, ChannelProperties properties)
        {
            return new PlayerChannel(subscriptionId, channelId, this, properties);
        }
    }
}
