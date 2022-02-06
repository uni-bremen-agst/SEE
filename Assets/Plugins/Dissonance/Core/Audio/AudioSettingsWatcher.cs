using UnityEngine;

namespace Dissonance.Audio
{
    internal class AudioSettingsWatcher
    {
        private static readonly AudioSettingsWatcher Singleton = new AudioSettingsWatcher();
        public static AudioSettingsWatcher Instance
        {
            get { return Singleton; }
        }

        private readonly object _lock = new object();

        private bool _started;

        private AudioConfiguration _config;
        public AudioConfiguration Configuration
        {
            get
            {
                lock (_lock)
                {
                    return _config;
                }
            }
        }

        internal void Start()
        {
            if (_started)
                return;

            lock (_lock)
            {
                if (!_started)
                {
                    AudioSettings.OnAudioConfigurationChanged += OnAudioConfigChanged;
                    OnAudioConfigChanged(true);
                }
                _started = true;
            }
        }

        private void OnAudioConfigChanged(bool devicewaschanged)
        {
#if NCRUNCH
            _config = new AudioConfiguration { sampleRate = 44100, dspBufferSize = 441, numRealVoices = 32, numVirtualVoices = 1024, speakerMode = AudioSpeakerMode.Mono };
#else
            lock (_lock)
                _config = AudioSettings.GetConfiguration();
#endif
        }
    }
}
