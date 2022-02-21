using System;
using Dissonance.Audio.Codecs;
using Dissonance.Networking;
using Dissonance.Threading;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal class EncoderPipeline
        : IMicrophoneSubscriber, IDisposable
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(EncoderPipeline).Name);

        private readonly byte[] _encodedBytes;
        private readonly float[] _plainSamples;

        private readonly ReadonlyLockedValue<IVoiceEncoder> _encoder;
        private readonly ICommsNetwork _net;

        private readonly BufferedSampleProvider _input;
        private readonly Resampler _resampler;
        private readonly IFrameProvider _output;

        private readonly WaveFormat _inputFormat;

        private volatile bool _stopped;
        /// <summary>
        /// Indicates if the encoder has encoded the last packet
        /// </summary>
        public bool Stopped { get { return _stopped; } }

        private volatile bool _stopping;
        /// <summary>
        /// Indicates if the encoder is waiting to send one final packet
        /// </summary>
        public bool Stopping { get { return _stopping; } }

        private volatile bool _disposed;

        /// <summary>
        /// Expected packet loss of transmitted packet (0 to 1)
        /// </summary>
        public float TransmissionPacketLoss { get; set; }
        #endregion

        #region constructor
        public EncoderPipeline([NotNull] WaveFormat inputFormat, [NotNull] IVoiceEncoder encoder, [NotNull] ICommsNetwork net)
        {
            if (inputFormat == null) throw new ArgumentNullException("inputFormat");
            if (encoder == null) throw new ArgumentNullException("encoder");
            if (net == null) throw new ArgumentNullException("net");

            _net = net;
            _inputFormat = inputFormat;
            _encoder = new ReadonlyLockedValue<IVoiceEncoder>(encoder);

            //Create buffers to store the encoder input (1 frame of floats) and output (twice equivalent amount of bytes)
            _plainSamples = new float[encoder.FrameSize];
            _encodedBytes = new byte[encoder.FrameSize * sizeof(float) * 2];

            //Input buffer to store raw data from microphone
            _input = new BufferedSampleProvider(_inputFormat, encoder.FrameSize * 2);

            //Resample data from microphone rate -> encoder rate
            _resampler = new Resampler(_input, encoder.SampleRate);

            //Provides encoder sized and encoder rate frames of data
            _output = new SampleToFrameProvider(_resampler, (uint)encoder.FrameSize);
        }
        #endregion

        public void ReceiveMicrophoneData(ArraySegment<float> inputSamples, [NotNull] WaveFormat format)
        {
            if (format == null)
                throw new ArgumentNullException("format");
            if (!format.Equals(_inputFormat))
                throw new ArgumentException(string.Format("Samples expected in format {0}, but supplied with format {1}", _inputFormat, format), "format");

            using (var encoderLock = _encoder.Lock())
            {
                var encoder = encoderLock.Value;

                //Early exit if we have been disposed on the main thread
                if (_disposed)
                    return;

                //Early exit if we've sent the last frame of this stream
                if (_stopped)
                    return;

                //Propogate the loss value on to the encoder
                encoder.PacketLoss = TransmissionPacketLoss;

                //Write samples to the pipeline (keep a running total of how many we have sent)
                //Keep sending until we've sent all of these samples
                var offset = 0;
                while (offset != inputSamples.Count)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute (Justification: Array segment cannot be null)
                    offset += _input.Write(new ArraySegment<float>(inputSamples.Array, inputSamples.Offset + offset, inputSamples.Count - offset));

                    //Drain some of those samples just written, encode them and send them off
                    //If we're shutting down send a maximum of 1 packet
                    var encodedFrames = EncodeFrames(encoder, _stopping ? 1 : int.MaxValue);

                    //Don't encode any more frames if we've sent the one final frame
                    if (encodedFrames > 0 && _stopping)
                    {
                        _stopped = true;
                        Log.Debug("Encoder stopped");
                        break;
                    }
                }
            }
        }
        
        private int EncodeFrames([NotNull] IVoiceEncoder encoder, int maxCount)
        {
            var count = 0;

            //Read frames of resampled samples (as many as we can, we want to keep this buffer empty and latency low)
            var encoderInput = new ArraySegment<float>(_plainSamples, 0, encoder.FrameSize);
            while (_output.Read(encoderInput) && count < maxCount)
            {
                //Encode it
                var encoded = encoder.Encode(encoderInput, new ArraySegment<byte>(_encodedBytes));

                //Transmit it
                _net.SendVoice(encoded);
                count++;
            }

            return count;
        }

        public void Reset()
        {
            if (_disposed)
                return;

            using (_encoder.Lock())
            {
                Log.Debug("Applying encoder reset");

                _resampler.Reset();
                _input.Reset();
                _output.Reset();

                _stopping = false;
                _stopped = false;
            }
        }

        public void Stop()
        {
            Log.Debug("Requesting encoder stop");

            using (_encoder.Lock())
                _stopping = true;
        }

        public void Dispose()
        {
            using (var encoderLock = _encoder.Lock())
            {
                _disposed = true;

                _stopping = true;
                _stopped = true;

                encoderLock.Value.Dispose();
            }
        }
    }
}
