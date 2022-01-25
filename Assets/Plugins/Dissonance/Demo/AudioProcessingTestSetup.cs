using System;
using System.Linq;
using Dissonance.Audio.Capture;
using Dissonance.Config;
using Dissonance.VAD;
using JetBrains.Annotations;
using NAudio.Wave;
using UnityEngine;
using UnityEngine.UI;

namespace Dissonance.Demo
{
    public class AudioProcessingTestSetup
        : MonoBehaviour, IVoiceActivationListener
    {
        public Slider InputVolumeSlider;
        public Slider OutputVolumeSlider;
        public Slider OutputCutoffSlider;
        public Button PlayPauseButton;
        public Dropdown ClipsDropdown;
        public Dropdown NoiseSuppressionDropdown;
        public Dropdown VadSensitivityDropdown;
        public Text VoiceIndicator;
        public Toggle BackgroundSoundRemoval;
        public Slider BackgroundSoundRemovalSlider;

        public AudioClip[] Clips;

        private WebRtcPreprocessingPipeline _preprocessor;

        private bool _enabled;

        private bool _reading;
        private float _pendingSamples = 0;
        private int _readHead;
        private AudioClip _clip;
        private readonly float[] _buffer = new float[128];
        private bool _vad;

        private void OnEnable()
        {
            VoiceSettings.Preload();
            DebugSettings.Preload();

            ClipsDropdown.options.Clear();
            for (var i = 0; i < Clips.Length; i++)
                ClipsDropdown.options.Add(new Dropdown.OptionData(Clips[i].name));

            NoiseSuppressionDropdown.options.Clear();
            foreach (var item in Enum.GetNames(typeof(NoiseSuppressionLevels)))
                NoiseSuppressionDropdown.options.Add(new Dropdown.OptionData(item));
            NoiseSuppressionDropdown.value = (int)VoiceSettings.Instance.DenoiseAmount;

            VadSensitivityDropdown.options.Clear();
            foreach (var item in Enum.GetNames(typeof(VadSensitivityLevels)))
                VadSensitivityDropdown.options.Add(new Dropdown.OptionData(item));
            NoiseSuppressionDropdown.value = (int)VoiceSettings.Instance.VadSensitivity;

            BackgroundSoundRemoval.isOn = VoiceSettings.Instance.BackgroundSoundRemovalEnabled;
            BackgroundSoundRemovalSlider.value = VoiceSettings.Instance.BackgroundSoundRemovalAmount;

            OutputCutoffSlider.value = GetComponent<AudioLowPassFilter>().cutoffFrequency / 20000;

            _enabled = true;

            OnAudioSelectionChanged();
            OnPlayPauseClicked();
        }

        private void Update()
        {
            VoiceIndicator.text = _vad ? "True" : "False";

            if (_reading && _clip != null)
            {
                _pendingSamples += (int)(_clip.frequency * Time.unscaledDeltaTime);
                while (_pendingSamples >= _buffer.Length)
                {
                    _pendingSamples -= _buffer.Length;
                    _clip.GetData(_buffer, _readHead);
                    _readHead = (_readHead + _buffer.Length) % _clip.samples;
                    for (var i = 0; i < _buffer.Length; i++)
                        _buffer[i] *= InputVolumeSlider.value;
                    ProcessSamples(_buffer);
                }
            }
        }

        private void ProcessSamples([NotNull] float[] floats)
        {
            // Deliver audio to preprocessor
            ((IMicrophoneSubscriber)_preprocessor).ReceiveMicrophoneData(
                new ArraySegment<float>(floats),
                new WaveFormat(_clip.frequency, 1)
            );
        }

        public void OnAudioSelectionChanged()
        {
            var index = ClipsDropdown.value;
            var clip = Clips[index];
            Debug.Log("Changed clip to: " + clip.name);
            ChangeAudioClip(clip);
        }

        public void OnVolumeChanged(float _)
        {
            GetComponent<AudioSource>().volume = OutputVolumeSlider.value;
        }

        public void OnLowPassCutoffChanged(float _)
        {
            GetComponent<AudioLowPassFilter>().cutoffFrequency = OutputCutoffSlider.value * 20000;
        }

        public void OnPlayPauseClicked()
        {
            _reading = !_reading;
            PlayPauseButton.GetComponentInChildren<Text>().text = _reading ? "Pause" : "Play";
        }

        public void OnNoiseSuppressionChanged()
        {
            if (!_enabled)
                return;

            var index = NoiseSuppressionDropdown.value;
            VoiceSettings.Instance.DenoiseAmount = (NoiseSuppressionLevels)Enum.Parse(typeof(NoiseSuppressionLevels), NoiseSuppressionDropdown.options[index].text);
        }

        public void OnVadSensitivityChanged()
        {
            if (!_enabled)
                return;

            var index = VadSensitivityDropdown.value;
            VoiceSettings.Instance.VadSensitivity = (VadSensitivityLevels)Enum.Parse(typeof(VadSensitivityLevels), VadSensitivityDropdown.options[index].text);
        }

        public void OnBackgroundSoundRemovalChanged()
        {
            if (!_enabled)
                return;

            VoiceSettings.Instance.BackgroundSoundRemovalEnabled = BackgroundSoundRemoval.isOn;
            VoiceSettings.Instance.BackgroundSoundRemovalAmount = BackgroundSoundRemovalSlider.value;
        }

        private void ChangeAudioClip([NotNull] AudioClip clip)
        {
            if (clip.channels != 1)
            {
                Debug.LogError("Audio clip must be mono!");
                return;
            }
            _clip = clip;
            var format = new WaveFormat(clip.frequency, 1);

            var sub = GetComponent<MicSubscriberPlayer>();
            sub.SetFormat(format);

            if (_preprocessor != null)
            {
                _preprocessor.Dispose();
                _preprocessor = null;
            }
            
            _preprocessor = new WebRtcPreprocessingPipeline(format, false);
            _preprocessor.Subscribe(GetComponent<MicSubscriberPlayer>());
            _preprocessor.Start();

            VoiceActivationStop();
            _preprocessor.Subscribe(this);

            _pendingSamples = -clip.frequency * 5;
            _readHead = 0;
            _clip = clip;
        }

        public void OnDestroy()
        {
            _preprocessor.Dispose();
        }

        public void VoiceActivationStart()
        {
            _vad = true;
        }

        public void VoiceActivationStop()
        {
            _vad = false;
        }

        public int GetGains(float[] output)
        {
            if (_preprocessor != null)
                return _preprocessor.GetBackgroundNoiseRemovalGains(output);
            return 0;
        }
    }
}
