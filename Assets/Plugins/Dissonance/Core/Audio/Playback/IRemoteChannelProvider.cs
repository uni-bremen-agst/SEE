using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    internal interface IRemoteChannelProvider
    {
        void GetRemoteChannels([NotNull] List<RemoteChannel> output);
    }
}
