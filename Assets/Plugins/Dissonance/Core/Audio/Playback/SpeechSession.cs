using System;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Represents a decoder pipeline for a single playback session.
    /// </summary>
    public struct SpeechSession
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(SpeechSession).Name);

        private static readonly float[] DesyncFixBuffer = new float[1024];
        private const float MinimumDelayFactor = 1.5f;
        private const float MaximumDelay = 0.750f;
        private static readonly int FixedDelayToleranceTicks = (int)TimeSpan.FromMilliseconds(33).Ticks;
        private static readonly float InitialBufferDelay = 0.1f;

        private readonly float _minimumDelay;

        private readonly IRemoteChannelProvider _channels;
        private readonly IDecoderPipeline _pipeline;
        private readonly SessionContext _context;
        private readonly DateTime _creationTime;
        private readonly IJitterEstimator _jitter;

        public int BufferCount { get { return _pipeline.BufferCount; } }
        public SessionContext Context { get { return _context; } }
        public PlaybackOptions PlaybackOptions { get { return _pipeline.PlaybackOptions; } }
        [NotNull] public WaveFormat OutputWaveFormat { get { return _pipeline.OutputFormat; } }
        internal float PacketLoss { get { return _pipeline.PacketLoss; } }
        internal IRemoteChannelProvider Channels { get { return _channels; } }

        public DateTime TargetActivationTime
        {
            get { return _creationTime + Delay; }
        }

        public TimeSpan Delay
        {
            get
            {
                //Calculate how much we should be delayed based purely on the jitter measurement
                //
                // Jitter value is the standard deviation of jitter, so we'll keep a buffer of 2.5 standard deviations. This means about
                // 97.5 % of packets will arrive within the buffer time. Assuming normally distributed jitter, which isn't true but it's
                // at least a good guesstimate for now.
                var jitterDelay = Mathf.LerpUnclamped(InitialBufferDelay, _jitter.Jitter * 2.5f, _jitter.Confidence);

                return TimeSpan.FromSeconds(Mathf.Clamp(jitterDelay, _minimumDelay, MaximumDelay));
            }
        }

        public SyncState SyncState
        {
            get { return _pipeline.SyncState; }
        }
        #endregion

        private SpeechSession(SessionContext context, IJitterEstimator jitter, IDecoderPipeline pipeline, IRemoteChannelProvider channels, DateTime now)
        {
            _context = context;
            _pipeline = pipeline;
            _channels = channels;
            _creationTime = now;
            _jitter = jitter;

            _minimumDelay = (float)(MinimumDelayFactor * _pipeline.InputFrameTime.TotalSeconds);

            Log.Debug("Created speech session with min delay={0}s", _minimumDelay);
        }

        internal static SpeechSession Create(SessionContext context, IJitterEstimator jitter, IDecoderPipeline pipeline, IRemoteChannelProvider channels, DateTime now)
        {
            return new SpeechSession(context, jitter, pipeline, channels, now);
        }

        public void Prepare(DateTime timeOfFirstDequeueAttempt)
        {
            _pipeline.Prepare(_context);

            // Log a message if jitter is extremely large
            if (_jitter.Confidence >= 0.75 && _jitter.Jitter >= MaximumDelay * 0.5f)
                Log.Warn("Beginning playback with very large network jitter: {0}s {1}confidence", _jitter.Jitter, _jitter.Confidence);

            // Measure how much bigger this buffer is due to the delay imposed by the previous session playing back.
            var delayTolerance = Math.Max(0, ((timeOfFirstDequeueAttempt - _creationTime) - Delay).Ticks);

            // Some network systems (e.g. HLAPI) will delay packets in some circumstances and dump them all on us at once. This causes the session to have a
            // colossal buffer of unplayed audio which causes a very large delay. Check if the buffer of unplayed audio is oversized and discard as much audio
            // as necessary to bring it back to a reasonable size.
            //  - Delay.Ticks is the size we _want_, check for 3x that
            //  - If Delay.Ticks is very small we could easily overrun it by 3x in a single frame. Add a fixed amount of tolerance
            //  - This buffer could be oversize because the previous session overran. Add how much it overran by.
            var bufferedTime = _pipeline.BufferTime;
            if (bufferedTime.Ticks > Delay.Ticks * 3 + FixedDelayToleranceTicks + delayTolerance)
            {
                //Calculate how much time we're out of sync, and how many samples that corresponds to
                var desyncTime = TimeSpan.FromTicks(bufferedTime.Ticks - Delay.Ticks);
                var desyncSamples = (int)(desyncTime.TotalSeconds * OutputWaveFormat.SampleRate);

                Log.Warn("Detected oversized buffer before playback started. Jitter:{0}ms ({1}) Buffered:{2}ms Expected:{3}ms. Discarding {4}ms of audio...",
                     _jitter.Jitter,
                     _jitter.Confidence,
                     _pipeline.BufferTime.TotalMilliseconds,
                     Delay.TotalMilliseconds,
                     desyncTime.TotalMilliseconds
                );

                //Read out a load of data and discard it, forcing ourselves back into sync
                while (desyncSamples > 0)
                {
                    var count = Math.Min(desyncSamples, DesyncFixBuffer.Length);
                    if (count == 0)
                        break;

                    Read(new ArraySegment<float>(DesyncFixBuffer, 0, count));
                    desyncSamples -= count;
                }

                if (desyncSamples == 0)
                    Log.Debug("...Completed discarding {0}ms of audio", desyncTime.TotalMilliseconds);
                else
                    Log.Debug("...completed attempted discard {0}ms of audio, {1} could not be disposed", desyncTime.TotalMilliseconds, desyncSamples);
            }

            // Now that the initial audio has been read enable dynamic sync. it cannot be done before this point because the huge read of audio that just happened
            // would confuse dynamic sync (it would immediately think it's out of sync and try to "fix" it).
            _pipeline.EnableDynamicSync();
        }

        /// <summary>
        ///     Pulls the specfied number of samples from the pipeline, decoding packets as necessary.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns><c>true</c> if there are more samples available; else <c>false</c>.</returns>
        public bool Read(ArraySegment<float> samples)
        {
            return _pipeline.Read(samples);
        }
    }
}