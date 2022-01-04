namespace Dissonance.Audio.Capture
{
    internal interface IChannelPriorityProvider
    {
        ChannelPriority DefaultChannelPriority { get; set; }
    }
}
