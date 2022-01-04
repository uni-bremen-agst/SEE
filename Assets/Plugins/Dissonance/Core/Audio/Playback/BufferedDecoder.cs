using System;
using System.Collections.Generic;
using Dissonance.Audio.Codecs;
using Dissonance.Config;
using Dissonance.Networking;
using Dissonance.Threading;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Buffers encoded frames with an internal <see cref="EncodedAudioBuffer" />, and decodes frames in sequence as they
    ///     are requested.
    /// </summary>
    internal class BufferedDecoder
        : IFrameSource, IRemoteChannelProvider
    {
        private readonly EncodedAudioBuffer _buffer;
        private readonly IVoiceDecoder _decoder;
        private readonly uint _frameSize;
        private readonly WaveFormat _waveFormat;
        private readonly Action<VoicePacket> _recycleFrame;

        private AudioFileWriter _diagnosticOutput;
        
        public int BufferCount { get { return _buffer.Count; } }
        public uint SequenceNumber { get { return _buffer.SequenceNumber; } }
        public float PacketLoss { get { return _buffer.PacketLoss; } }

        private readonly LockedValue<PlaybackOptions> _options = new LockedValue<PlaybackOptions>(new PlaybackOptions(false, 1, ChannelPriority.Default));
        public PlaybackOptions LatestPlaybackOptions
        {
            get
            {
                using (var l = _options.Lock())
                    return l.Value;
            }
        }

        private bool _receivedFirstPacket;

        private int _approxChannelCount;
        private readonly ReadonlyLockedValue<List<RemoteChannel>> _channels = new ReadonlyLockedValue<List<RemoteChannel>>(new List<RemoteChannel>());

        public BufferedDecoder([NotNull] IVoiceDecoder decoder, uint frameSize, [NotNull] WaveFormat waveFormat, [NotNull] Action<VoicePacket> recycleFrame)
		{		
			if (decoder == null) throw new ArgumentNullException("decoder");
            if (waveFormat == null) throw new ArgumentNullException("waveFormat");
            if (recycleFrame == null) throw new ArgumentNullException("recycleFrame");

            _decoder = decoder;
            _frameSize = frameSize;
            _waveFormat = waveFormat;
            _recycleFrame = recycleFrame;
            _buffer = new EncodedAudioBuffer(recycleFrame);
        }

        public uint FrameSize
        {
            get { return _frameSize; }
        }

        public WaveFormat WaveFormat
        {
            get { return _waveFormat; }
        }

        public void Prepare(SessionContext context)
        {
            if (DebugSettings.Instance.EnablePlaybackDiagnostics && DebugSettings.Instance.RecordDecodedAudio)
            {
                var filename = string.Format("Dissonance_Diagnostics/Decoded_{0}_{1}_{2}", context.PlayerName, context.Id, DateTime.UtcNow.ToFileTime());
                _diagnosticOutput = new AudioFileWriter(filename, _waveFormat);
            }
        }

        public bool Read(ArraySegment<float> frame)
        {
            VoicePacket? encoded;
            bool peekLostPacket;
            var lastFrame = _buffer.Read(out encoded, out peekLostPacket);

            var p = new EncodedBuffer(encoded.HasValue ? encoded.Value.EncodedAudioFrame : (ArraySegment<byte>?)null, peekLostPacket || !encoded.HasValue);

            //Decode the frame
            int decodedCount = _decoder.Decode(p, frame);

            //If it was not a lost frame, also decode the metadata
            if (!p.PacketLost && encoded.HasValue)
            {
                //Expose the playback options for this packet
                using (var l = _options.Lock())
                    l.Value = encoded.Value.PlaybackOptions;

                //Read the channel data into a separate list
                ExtractChannels(encoded.Value);

                //Recycle the frame for re-use with a future packet. Only done with frames which were not peek ahead frames
                _recycleFrame(encoded.Value);
            }
            
            //Sanity check that decoding got correct number of samples
            if (decodedCount != _frameSize)
                throw new InvalidOperationException(string.Format("Decoding a frame of audio got {0} samples, but should have decoded {1} samples", decodedCount, _frameSize));

            if (_diagnosticOutput != null)
                _diagnosticOutput.WriteSamples(frame);

            return lastFrame;
        }

        private void ExtractChannels(VoicePacket encoded)
        {
            //Expose the channel list for this packet (if it's null just assume the previous value is still correct)
            if (encoded.Channels != null)
            {
                using (var l = _channels.Lock())
                {
                    _approxChannelCount = encoded.Channels.Count;

                    l.Value.Clear();
                    l.Value.AddRange(encoded.Channels);
                }

                _receivedFirstPacket = true;
            }
        }

        public void Reset()
        {
            _buffer.Reset();
            _decoder.Reset();

            _receivedFirstPacket = false;

            using (var l = _options.Lock())
                l.Value = new PlaybackOptions(false, 1, ChannelPriority.Default);

            using (var l = _channels.Lock())
                l.Value.Clear();

            if (_diagnosticOutput != null)
            {
                _diagnosticOutput.Dispose();
                _diagnosticOutput = null;
            }
        }

        public void Push(VoicePacket frame)
        {
            //If this is the first packet extract the channel data ahead of time so that it is available in the PlayerStartedSpeaking event
            if (!_receivedFirstPacket)
                ExtractChannels(frame);

            _buffer.Push(frame);
            _receivedFirstPacket = true;
        }

        public void Stop()
        {
            _buffer.Stop();
        }

        public void GetRemoteChannels(List<RemoteChannel> output)
        {
            //Do as much busywork outside the lock as possible
            output.Clear();
            if (output.Capacity < _approxChannelCount)
                output.Capacity = _approxChannelCount;

            //Copy across the channel data
            using (var l = _channels.Lock())
                output.AddRange(l.Value);
        }
    }
}