using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Networking;
using JetBrains.Annotations;

namespace Dissonance.Audio.Playback
{
    internal interface IJitterEstimator
    {
        float Jitter { get; }

        float Confidence { get; }
    }

    /// <summary>
    ///     Converts the sequence of stream start/stop and packet delivery events from the network into a sequence of
    ///     <see cref="SpeechSession" />.
    /// </summary>
    internal class SpeechSessionStream
        : IJitterEstimator
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof (SpeechSessionStream).Name);

        private string _metricArrivalDelay;
        
        private readonly Queue<SpeechSession> _awaitingActivation;
        private readonly IVolumeProvider _volumeProvider;

        /// <summary>
        /// The time when the current head of the queue was first attempted to be dequeued
        /// </summary>
        private DateTime? _queueHeadFirstDequeueAttempt = null;

        private DecoderPipeline _active;
        private uint _currentId;

        private string _playerName;
        public string PlayerName
        {
            get { return _playerName; }
            set
            {
                if (_playerName != value)
                {
                    _metricArrivalDelay = Metrics.MetricName("PacketArrivalDelay", _playerName);

                    _playerName = value;
                    _arrivalJitterMeter.Clear();
                }
            }
        }

        private readonly WindowDeviationCalculator _arrivalJitterMeter = new WindowDeviationCalculator(128);
        float IJitterEstimator.Jitter
        {
            get { return _arrivalJitterMeter.StdDev; }
        }

        float IJitterEstimator.Confidence
        {
            get { return _arrivalJitterMeter.Confidence; }
        }
        #endregion

        public SpeechSessionStream(IVolumeProvider volumeProvider)
        {
            _volumeProvider = volumeProvider;
            _awaitingActivation = new Queue<SpeechSession>();
        }

        /// <summary>
        ///     Starts a new speech session and adds it to the queue for playback
        /// </summary>
        /// <param name="format">The frame format.</param>
        /// <param name="now">Current time, or null for DateTime.UtcNow</param>
        /// <param name="jitter">Jitter estimator, or null for this stream to estimate it's own jitter</param>
        public void StartSession(FrameFormat format, DateTime? now = null, [CanBeNull] IJitterEstimator jitter = null)
        {
            if (PlayerName == null)
                throw Log.CreatePossibleBugException("Attempted to `StartSession` but `PlayerName` is null", "0C0F3731-8D6B-43F6-87C1-33CEC7A26804");

            _active = DecoderPipelinePool.GetDecoderPipeline(format, _volumeProvider);

            var session = SpeechSession.Create(new SessionContext(PlayerName, unchecked(_currentId++)), jitter ?? this, _active, _active, now ?? DateTime.UtcNow);
            _awaitingActivation.Enqueue(session);

            Log.Debug("Created new speech session with buffer time of {0}ms", session.Delay.TotalMilliseconds);
        }

        /// <summary>
        /// Attempt to dequeue a session for immediate playback
        /// </summary>
        /// <param name="now">The current time (or null, to use DateTime.UtcNow)</param>
        /// <returns></returns>
        public SpeechSession? TryDequeueSession(DateTime? now = null)
        {
            var rNow = now ?? DateTime.UtcNow;

            if (_awaitingActivation.Count > 0)
            {
                //Save the time when we first saw this item at the head of the queue
                if (!_queueHeadFirstDequeueAttempt.HasValue)
                    _queueHeadFirstDequeueAttempt = rNow;

                var next = _awaitingActivation.Peek();
                if (next.TargetActivationTime < rNow)
                {
                    next.Prepare(_queueHeadFirstDequeueAttempt.Value);

                    _awaitingActivation.Dequeue();
                    _queueHeadFirstDequeueAttempt = null;

                    return next;
                }
            }

            return null;
        }

        /// <summary>
        ///     Queues an encoded audio frame for playback in the current session.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="now">The current time (or null, to use DateTime.Now)</param>
        public void ReceiveFrame(VoicePacket packet, DateTime? now = null)
        {
            if (packet.SenderPlayerId != PlayerName)
                throw Log.CreatePossibleBugException(string.Format("Attempted to deliver voice from player {0} to playback queue for player {1}", packet.SenderPlayerId, PlayerName), "F55DB7D5-621B-4F5B-8C19-700B1FBC9871");

            if (_active == null)
            {
                Log.Warn(Log.PossibleBugMessage(string.Format("Attempted to deliver voice from player {0} with no active session", packet.SenderPlayerId), "1BD954EC-B455-421F-9D6E-2E3D087BC0A9"));
                return;
            }

            var delay = _active.Push(packet, now ?? DateTime.UtcNow);
            Metrics.Sample(_metricArrivalDelay, delay);

            _arrivalJitterMeter.Update(delay);
        }

        /// <summary>
        /// Force a total reset of this session stream, discarding all open sessions
        /// </summary>
        public void ForceReset()
        {
            // Reset the "active" session (the one at the _back_ of the queue with new packets being added to it)
            if (_active != null)
                _active.Reset();

            // Dequeue all pending sessions except the active one
            // This discards all the pipelines when in theory they could be recycled. However, since this is a hard reset we
            // can probably assume something has gone wrong and it's just safer to throw away all of these pipelines.
            while (_awaitingActivation.Count > 1)
                _awaitingActivation.Dequeue();

            // If this is no active session, discard the last one too.
            if (_active == null && _awaitingActivation.Count > 0)
                _awaitingActivation.Dequeue();

            // Empty all readings from the jitter meter
            _arrivalJitterMeter.Clear();
        }

        /// <summary>
        ///     Stops the current session.
        /// </summary>
        /// <param name="logNoSessionError">If true and no session is currently active this method will log a warning</param>
        public void StopSession(bool logNoSessionError = true)
        {
            if (_active != null)
            {
                _active.Stop();
                _active = null;
            }
            else if (logNoSessionError)
                Log.Warn(Log.PossibleBugMessage("Attempted to stop a session, but there is no active session", "6DB702AA-D683-47AA-9544-BE4857EF8160"));
        }
    }
}