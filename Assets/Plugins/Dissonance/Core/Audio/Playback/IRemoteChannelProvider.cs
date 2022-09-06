using System.Collections.Generic;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    public interface IRemoteChannelProvider
    {
        void GetRemoteChannels([NotNull] List<RemoteChannel> output);
    }
}
