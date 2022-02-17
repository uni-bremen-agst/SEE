using System;
using Dissonance.Audio.Codecs;
using Dissonance.Audio.Codecs.Identity;
using Dissonance.Audio.Codecs.Opus;
using Dissonance.Audio.Codecs.Silence;
using Dissonance.Config;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    internal class DecoderFactory
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(DecoderFactory).Name);

        [NotNull] public static IVoiceDecoder Create(FrameFormat format)
        {
            try
            {
                switch (format.Codec)
                {
                    case Codec.Identity:
                        return new IdentityDecoder(format.WaveFormat);

                    //ncrunch: no coverage start (Justification: Don't want to pull opus binaries into test context)
                    case Codec.Opus:
                        return new OpusDecoder(format.WaveFormat, VoiceSettings.Instance.ForwardErrorCorrection);
                    //ncrunch: no coverage end

                    default:
                        throw new ArgumentOutOfRangeException("format", "Unknown codec.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Encountered unexpected error creating decoder. Audio playback will be disabled.\n{0}", ex);

                return new SilenceDecoder(format);
            }
        }
    }
}