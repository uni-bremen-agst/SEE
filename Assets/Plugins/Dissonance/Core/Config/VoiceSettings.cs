using System;
using System.ComponentModel;
using System.IO;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Config
{
    public sealed class VoiceSettings
        :
#if !NCRUNCH
        ScriptableObject,
#endif
        INotifyPropertyChanged
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(VoiceSettings).Name);

        // ReSharper disable InconsistentNaming
        private const string PersistName_Quality = "Dissonance_Audio_Quality";
        private const string PersistName_FrameSize = "Dissonance_Audio_FrameSize";
        private const string PersistName_Fec = "Dissonance_Audio_DisableFEC";

        private const string PersistName_DenoiseAmount = "Dissonance_Audio_Denoise_Amount";
        private const string PersistName_PttDuckAmount = "Dissonance_Audio_Duck_Amount";
        private const string PersistName_VadSensitivity = "Dissonance_Audio_Vad_Sensitivity";

        private const string PersistName_BgDenoiseEnabled = "Dissonance_Audio_BgDenoise_Enabled";
        private const string PersistName_BgDenoiseWetmix = "Dissonance_Audio_BgDenoise_Amount";

        private const string PersistName_AecSuppressionAmount = "Dissonance_Audio_Aec_Suppression_Amount";
        private const string PersistName_AecDelayAgnostic = "Dissonance_Audio_Aec_Delay_Agnostic";
        private const string PersistName_AecExtendedFilter = "Dissonance_Audio_Aec_Extended_Filter";
        private const string PersistName_AecRefinedAdaptiveFilter = "Dissonance_Audio_Aec_Refined_Adaptive_Filter";
        private const string PersistName_AecmRoutingMode = "Dissonance_Audio_Aecm_Routing_Mode";
        private const string PersistName_AecmComfortNoise = "Dissonance_Audio_Aecm_Comfort_Noise";
        // ReSharper restore InconsistentNaming

        private const string SettingsFileResourceName = "VoiceSettings";
        public static readonly string SettingsFilePath = Path.Combine(DissonanceRootPath.BaseResourcePath, SettingsFileResourceName + ".asset");

        #region codec settings
        [SerializeField]private AudioQuality _quality;
        public AudioQuality Quality
        {
            get { return _quality; }
            set
            {
                Preferences.Set(PersistName_Quality, ref _quality, value, (key, q) => PlayerPrefs.SetInt(key, (int)q), Log);
                OnPropertyChanged("Quality");
            }
        }

        [SerializeField]private FrameSize _frameSize;
        public FrameSize FrameSize
        {
            get { return _frameSize; }
            set
            {
                Preferences.Set(PersistName_FrameSize, ref _frameSize, value, (key, f) => PlayerPrefs.SetInt(key, (int)f), Log);
                OnPropertyChanged("FrameSize");
            }
        }

        [SerializeField]private int _forwardErrorCorrection;
        public bool ForwardErrorCorrection
        {
            get { return Convert.ToBoolean(_forwardErrorCorrection); }
            set
            {
                Preferences.Set(PersistName_Fec, ref _forwardErrorCorrection, Convert.ToInt32(value), PlayerPrefs.SetInt, Log);
                OnPropertyChanged("ForwardErrorCorrection");
            }
        }
        #endregion

        #region preprocessor settings
        [SerializeField]private int _denoiseAmount;
        public NoiseSuppressionLevels DenoiseAmount
        {
            get { return (NoiseSuppressionLevels)_denoiseAmount; }
            set
            {
                Preferences.Set(PersistName_DenoiseAmount, ref _denoiseAmount, (int)value, PlayerPrefs.SetInt, Log);
                OnPropertyChanged("DenoiseAmount");
            }
        }

        [SerializeField]private int _bgSoundRemovalEnabled;
        public bool BackgroundSoundRemovalEnabled
        {
            get { return Convert.ToBoolean(_bgSoundRemovalEnabled); }
            set
            {
                Preferences.Set(PersistName_BgDenoiseEnabled, ref _bgSoundRemovalEnabled, Convert.ToInt32(value), PlayerPrefs.SetInt, Log);
                OnPropertyChanged("BackgroundSoundRemovalEnabled");
            }
        }

        [SerializeField]private float _bgSoundRemovalAmount;
        public float BackgroundSoundRemovalAmount
        {
            get { return _bgSoundRemovalAmount; }
            set
            {
                Preferences.Set(PersistName_BgDenoiseWetmix, ref _bgSoundRemovalAmount, Mathf.Clamp01(value), PlayerPrefs.SetFloat, Log);
                OnPropertyChanged("BackgroundSoundRemovalAmount");
            }
        }

        [SerializeField]private int _vadSensitivity;
        public VadSensitivityLevels VadSensitivity
        {
            get { return (VadSensitivityLevels)_vadSensitivity; }
            set
            {
                Preferences.Set(PersistName_VadSensitivity, ref _vadSensitivity, (int)value, PlayerPrefs.SetInt, Log);
                OnPropertyChanged("VadSensitivity");
            }
        }

        [SerializeField] private int _aecAmount;
        public AecSuppressionLevels AecSuppressionAmount
        {
            get { return (AecSuppressionLevels)_aecAmount; }
            set
            {
                Preferences.Set(PersistName_AecSuppressionAmount, ref _aecAmount, (int)value, PlayerPrefs.SetInt, Log);
                OnPropertyChanged("AecSuppressionAmount");
            }
        }

        [SerializeField] private int _aecDelayAgnostic;
        public bool AecDelayAgnostic
        {
            get { return Convert.ToBoolean(_aecDelayAgnostic); }
            set
            {
                Preferences.Set(PersistName_AecDelayAgnostic, ref _aecDelayAgnostic, Convert.ToInt32(value), PlayerPrefs.SetInt, Log);
                OnPropertyChanged("AecDelayAgnostic");
            }
        }

        [SerializeField] private int _aecExtendedFilter;
        public bool AecExtendedFilter
        {
            get { return Convert.ToBoolean(_aecExtendedFilter); }
            set
            {
                Preferences.Set(PersistName_AecExtendedFilter, ref _aecExtendedFilter, Convert.ToInt32(value), PlayerPrefs.SetInt, Log);
                OnPropertyChanged("AecExtendedFilter");
            }
        }

        [SerializeField] private int _aecRefinedAdaptiveFilter;
        public bool AecRefinedAdaptiveFilter
        {
            get { return Convert.ToBoolean(_aecRefinedAdaptiveFilter); }
            set
            {
                Preferences.Set(PersistName_AecRefinedAdaptiveFilter, ref _aecRefinedAdaptiveFilter, Convert.ToInt32(value), PlayerPrefs.SetInt, Log);
                OnPropertyChanged("AecRefinedAdaptiveFilter");
            }
        }

        [SerializeField] private int _aecmRoutingMode;
        public AecmRoutingMode AecmRoutingMode
        {
            get { return (AecmRoutingMode)_aecmRoutingMode; }
            set
            {
                Preferences.Set(PersistName_AecmRoutingMode, ref _aecmRoutingMode, (int)value, PlayerPrefs.SetInt, Log);
                OnPropertyChanged("AecmRoutingMode");
            }
        }

        [SerializeField] private int _aecmComfortNoise;
        public bool AecmComfortNoise
        {
            get { return Convert.ToBoolean(_aecmComfortNoise); }
            set
            {
                Preferences.Set(PersistName_AecmComfortNoise, ref _aecmComfortNoise, Convert.ToInt32(value), PlayerPrefs.SetInt, Log);
                OnPropertyChanged("AecmComfortNoise");
            }
        }
        #endregion

        [SerializeField] private float _voiceDuckLevel;
        public float VoiceDuckLevel
        {
            get { return _voiceDuckLevel; }
            set
            {
                Preferences.Set(PersistName_PttDuckAmount, ref _voiceDuckLevel, value, PlayerPrefs.SetFloat, Log);
                OnPropertyChanged("VoiceDuckLevel");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static VoiceSettings _instance;
        [NotNull] public static VoiceSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }
        #endregion

        public VoiceSettings()
        {
            LoadDefaults();
        }

        /// <summary>
        /// Load defaults into fields, but do not clear prefs
        /// </summary>
        private void LoadDefaults()
        {
            _quality = AudioQuality.Medium;
            _frameSize = FrameSize.Medium;
            _forwardErrorCorrection = Convert.ToInt32(true);

            _denoiseAmount = (int)NoiseSuppressionLevels.High;
            _vadSensitivity = (int)VadSensitivityLevels.MediumSensitivity;

            _bgSoundRemovalEnabled = Convert.ToInt32(false);
            _bgSoundRemovalAmount = 0.65f;

            _aecAmount = (int)AecSuppressionLevels.Disabled;
            _aecDelayAgnostic = Convert.ToInt32(true);
            _aecExtendedFilter = Convert.ToInt32(true);
            _aecRefinedAdaptiveFilter = Convert.ToInt32(true);

            _aecmRoutingMode = (int)AecmRoutingMode.Disabled;
            _aecmComfortNoise = Convert.ToInt32(true);

            _voiceDuckLevel = 0.5f;
        }

        /// <summary>
        /// Reset all options to default and clear the PlayerPrefs
        /// </summary>
        public void Reset()
        {
            // Clear all prefs
            PlayerPrefs.DeleteKey(PersistName_Quality);
            PlayerPrefs.DeleteKey(PersistName_FrameSize);
            PlayerPrefs.DeleteKey(PersistName_Fec);

            PlayerPrefs.DeleteKey(PersistName_DenoiseAmount);
            PlayerPrefs.DeleteKey(PersistName_VadSensitivity);

            PlayerPrefs.DeleteKey(PersistName_BgDenoiseEnabled);
            PlayerPrefs.DeleteKey(PersistName_BgDenoiseWetmix);

            PlayerPrefs.DeleteKey(PersistName_AecSuppressionAmount);
            PlayerPrefs.DeleteKey(PersistName_AecDelayAgnostic);
            PlayerPrefs.DeleteKey(PersistName_AecExtendedFilter);
            PlayerPrefs.DeleteKey(PersistName_AecRefinedAdaptiveFilter);

            PlayerPrefs.DeleteKey(PersistName_AecmRoutingMode);
            PlayerPrefs.DeleteKey(PersistName_AecmComfortNoise);

            PlayerPrefs.DeleteKey(PersistName_PttDuckAmount);

            // Initialised cached values in fields back to defaults
            LoadDefaults();
        }

        /// <summary>
        /// Ensure that the `Instance` is loaded. Calling this ensures that the `Instance` won't be lazily loaded later.
        /// </summary>
        public static void Preload()
        {
            if (_instance == null)
                _instance = Load();
        }

        [NotNull] private static VoiceSettings Load()
        {
#if NCRUNCH
            return new VoiceSettings();
#else
            var settings = Resources.Load<VoiceSettings>(SettingsFileResourceName);
            if (settings == null)
                settings = CreateInstance<VoiceSettings>();

            //Get all the settings values
            Preferences.Get(PersistName_Quality, ref settings._quality, (s, q) => (AudioQuality)PlayerPrefs.GetInt(s, (int)q), Log);
            Preferences.Get(PersistName_FrameSize, ref settings._frameSize, (s, f) => (FrameSize)PlayerPrefs.GetInt(s, (int)f), Log);
            Preferences.Get(PersistName_Fec, ref settings._forwardErrorCorrection, PlayerPrefs.GetInt, Log);

            Preferences.Get(PersistName_DenoiseAmount, ref settings._denoiseAmount, PlayerPrefs.GetInt, Log);
            Preferences.Get(PersistName_VadSensitivity, ref settings._vadSensitivity, PlayerPrefs.GetInt, Log);

            Preferences.Get(PersistName_BgDenoiseEnabled, ref settings._bgSoundRemovalEnabled, PlayerPrefs.GetInt, Log);
            Preferences.Get(PersistName_BgDenoiseWetmix, ref settings._bgSoundRemovalAmount, PlayerPrefs.GetFloat, Log);

            Preferences.Get(PersistName_AecSuppressionAmount, ref settings._aecAmount, PlayerPrefs.GetInt, Log);
            Preferences.Get(PersistName_AecDelayAgnostic, ref settings._aecDelayAgnostic, PlayerPrefs.GetInt, Log);
            Preferences.Get(PersistName_AecExtendedFilter, ref settings._aecExtendedFilter, PlayerPrefs.GetInt, Log);
            Preferences.Get(PersistName_AecRefinedAdaptiveFilter, ref settings._aecRefinedAdaptiveFilter, PlayerPrefs.GetInt, Log);

            Preferences.Get(PersistName_AecmRoutingMode, ref settings._aecmRoutingMode, PlayerPrefs.GetInt, Log);
            Preferences.Get(PersistName_AecmComfortNoise, ref settings._aecmRoutingMode, PlayerPrefs.GetInt, Log);

            Preferences.Get(PersistName_PttDuckAmount, ref settings._voiceDuckLevel, PlayerPrefs.GetFloat, Log);

            return settings;
#endif
        }

        public override string ToString()
        {
            return string.Format(
                "Quality: {0}, FrameSize: {1}, FEC: {2}, DenoiseAmount: {3}, RNN: {4} ({5:0.0#}) VoiceDuckLevel: {6} VAD: {7}",
                Quality,
                FrameSize,
                ForwardErrorCorrection,
                DenoiseAmount,
                BackgroundSoundRemovalEnabled,
                BackgroundSoundRemovalAmount,
                VoiceDuckLevel,
                VadSensitivity
            );
        }
    }
}
