using System;
using JetBrains.Annotations;

namespace Dissonance
{
    /// <summary>
    /// Interface for channels. Channels are implemented as structs, and should never be cast into this interface! It is used to restrict generic types.
    /// </summary>
    /// <typeparam name="T">Type which represents the target of this channel</typeparam>
    // ReSharper disable once TypeParameterCanBeVariant (Justification: Unity panics if you do this)
    public interface IChannel<T> : IDisposable
    {
        /// <summary>
        /// The target of this channel
        /// </summary>
        T TargetId { get; }

        /// <summary>
        /// A unique ID for this channel (may be re-used by other channels, but only after this channel has been closed)
        /// </summary>
        ushort SubscriptionId { get; }

        /// <summary>
        /// The properties of this channel
        /// </summary>
        [NotNull] ChannelProperties Properties { get; }
    }
}
