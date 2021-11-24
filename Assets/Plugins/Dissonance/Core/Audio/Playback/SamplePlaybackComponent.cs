using System;
using System.Threading;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    /// Plays back an ISampleProvider to an AudioSource
    /// <remarks>Uses OnAudioFilterRead, so the source it is playing back on will be whichever the filter attaches itself to.</remarks>
    /// </summary>
    /// ReSharper disable once InheritdocConsiderUsage
    public class SamplePlaybackComponent
        : MonoBehaviour
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(SamplePlaybackComponent).Name);
        
        /// <summary>
        /// Temporary buffer to hold data read from source
        /// </summary>
        private float[] _temp;

        [CanBeNull]private AudioFileWriter _diagnosticOutput;

        public bool HasActiveSession
        {
            get { return Session.HasValue; }
        }

        private SessionContext _lastPlayedSessionContext;
        private readonly ReaderWriterLockSlim _sessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        public SpeechSession? Session { get; private set; }

        private volatile float _arv;
        /// <summary>
        /// Average rectified value of the audio signal currently playing (a measure of amplitude)
        /// </summary>
        public float ARV { get { return _arv; } }
        #endregion

        public void Play(SpeechSession session)
        {
            if (Session != null)
                throw Log.CreatePossibleBugException("Attempted to play a session when one is already playing", "C4F19272-994D-4025-AAEF-37BB62685C2E");

            Log.Debug("Began playback of speech session. id={0}", session.Context.Id);

            if (DebugSettings.Instance.EnablePlaybackDiagnostics && DebugSettings.Instance.RecordFinalAudio)
            {
                var filename = string.Format("Dissonance_Diagnostics/Output_{0}_{1}_{2}", session.Context.PlayerName, session.Context.Id, DateTime.UtcNow.ToFileTime());
                Interlocked.Exchange(ref _diagnosticOutput, new AudioFileWriter(filename, session.OutputWaveFormat));
            }

            _sessionLock.EnterWriteLock();
            try
            {
                ApplyReset();
                Session = session;
            }
            finally
            {
                _sessionLock.ExitWriteLock();
            }
        }

        public void Start()
        {
            //Create a temporary buffer to hold audio. We don't know how big the buffer needs to be,
            //but this buffer is *one second long* which is way larger than we could ever need!
            _temp = new float[AudioSettings.outputSampleRate];
        }

        public void OnEnable()
        {
            Session = null;
            ApplyReset();
        }

        public void OnDisable()
        {
            Session = null;
            ApplyReset();
        }

        public void OnAudioFilterRead([NotNull] float[] data, int channels)
        {
            //If there is no session, clear filter and early exit
            var maybeSession = Session;
            if (!maybeSession.HasValue)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }

            _sessionLock.EnterUpgradeableReadLock();
            try
            {
                //Check if there is no session again, this time protected by a lock
                maybeSession = Session;
                if (!maybeSession.HasValue)
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                //Detect if the session has changed since the last call to this method, if so reset
                var session = maybeSession.Value;
                if (!session.Context.Equals(_lastPlayedSessionContext))
                {
                    _lastPlayedSessionContext = maybeSession.Value.Context;
                    ApplyReset();
                }

                // Read data from pipeline
                float arv;
                var complete = Filter(session, data, channels, _temp, _diagnosticOutput, out arv);
                _arv = arv;

                //Clean up now that this session is complete
                if (complete)
                {
                    Log.Debug("Finished playback of speech session. id={0}. player={1}", session.Context.Id, session.Context.PlayerName);

                    //Clear the session
                    _sessionLock.EnterWriteLock();
                    try
                    {
                        Session = null;
                    }
                    finally
                    {
                        _sessionLock.ExitWriteLock();
                    }

                    //Reset the state
                    ApplyReset();

                    //Discard the diagnostic recorder if necessary
                    if (_diagnosticOutput != null)
                    {
                        _diagnosticOutput.Dispose();
                        _diagnosticOutput = null;
                    }
                }
            }
            finally
            {
                _sessionLock.ExitUpgradeableReadLock();
            }
        }

        private void ApplyReset()
        {
            Log.Debug("Resetting playback component");

            _arv = 0;
        }

        internal static bool Filter(SpeechSession session, [NotNull] float[] output, int channels, [NotNull] float[] temp, [CanBeNull]AudioFileWriter diagnosticOutput, out float arv)
        {
            //Read out data from source (exactly as much as we need for one channel)
            var samplesRequired = output.Length / channels;
            var complete = session.Read(new ArraySegment<float>(temp, 0, samplesRequired));

            //Write the audio we're about to play to the diagnostics writer (on disk)
            if (diagnosticOutput != null)
                diagnosticOutput.WriteSamples(new ArraySegment<float>(temp, 0, samplesRequired));

            //Step through samples, stretching them (i.e. play mono input in all output channels)
            float accumulator = 0;
            var sampleIndex = 0;
            for (var i = 0; i < output.Length; i += channels)
            {
                //Get a single sample from the source data
                var sample = temp[sampleIndex++];

                //Accumulate the sum of the audio signal
                accumulator += Mathf.Abs(sample);

                //Copy data into all channels
                for (var c = 0; c < channels; c++)
                    output[i + c] *= sample;
            }

            arv = accumulator / output.Length;

            return complete;
        }
    }
}
