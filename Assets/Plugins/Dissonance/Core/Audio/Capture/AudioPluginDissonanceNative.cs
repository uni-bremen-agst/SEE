using System;
using System.Runtime.InteropServices;

namespace Dissonance.Audio.Capture
{
    internal static class AudioPluginDissonanceNative
    {
        // ReSharper disable once UnusedMember.Local
        private static readonly Log Log = Logs.Create(LogCategory.Core, nameof(AudioPluginDissonanceNative));

        internal static FilterState GetAecFilterState()
        {
            return (FilterState)Dissonance_GetFilterState();
        }

#if UNITY_IOS && !UNITY_EDITOR
        private const string ImportString = "__Internal";
        private const CallingConvention Convention = default(CallingConvention);
#else
        private const string ImportString = "AudioPluginDissonance";
        private const CallingConvention Convention = CallingConvention.Cdecl;
#endif

        #region rnnoise
#if (!UNITY_EDITOR_OSX) && (UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_WSA) || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_ANDROID)
                [DllImport(ImportString, CallingConvention = Convention)]
                public static extern IntPtr Dissonance_CreateRnnoiseState();

                [DllImport(ImportString, CallingConvention = Convention)]
                public static extern void Dissonance_DestroyRnnoiseState(IntPtr state);

                [DllImport(ImportString, CallingConvention = Convention)]
                public static extern bool Dissonance_RnnoiseProcessFrame(IntPtr state, int count, int sampleRate, float[] input, float[] output);

                [DllImport(ImportString, CallingConvention = Convention)]
                public static extern int Dissonance_RnnoiseGetGains(IntPtr state, float[] output, int length);
#else
                private static bool _rnnoiseWarning = false;

                public static IntPtr Dissonance_CreateRnnoiseState()
                {
                    if (!_rnnoiseWarning)
                    {
                        Log.Warn("Rnnoise is not supported on this platform");
                        _rnnoiseWarning = true;
                    }

                    return new IntPtr(1);
                }

                public static void Dissonance_DestroyRnnoiseState(IntPtr state)
                {
                }

                public static bool Dissonance_RnnoiseProcessFrame(IntPtr state, int count, int sampleRate, float[] input, float[] output)
                {
                    if (input != output)
                        Array.Copy(input, output, count);
                    return true;
                }

                public static int Dissonance_RnnoiseGetGains(IntPtr state, float[] output, int length)
                {
                    return 0;
                }
#endif
        #endregion

        #region webrtc
        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern IntPtr Dissonance_CreatePreprocessor(
            NoiseSuppressionLevels nsLevel,
            AecSuppressionLevels aecLevel,
            bool aecDelayAgnostic,
            bool aecExtended,
            bool aecRefined,
            AecmRoutingMode aecmRoutingMode,
            bool aecmComfortNoise
        );

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern void Dissonance_DestroyPreprocessor(IntPtr handle);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern void Dissonance_ConfigureNoiseSuppression(IntPtr handle, NoiseSuppressionLevels nsLevel);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern void Dissonance_ConfigureVadSensitivity(IntPtr handle, VadSensitivityLevels nsLevel);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern void Dissonance_ConfigureAecSuppression(IntPtr handle, AecSuppressionLevels aecLevel, AecmRoutingMode aecmRouting);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern bool Dissonance_GetVadSpeechState(IntPtr handle);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern ProcessorErrors Dissonance_PreprocessCaptureFrame(IntPtr handle, int sampleRate, float[] input, float[] output, int streamDelay);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern bool Dissonance_PreprocessorExchangeInstance(IntPtr previous, IntPtr replacement);

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern int Dissonance_GetFilterState();

        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern void Dissonance_GetAecMetrics(IntPtr floatBuffer, int bufferLength);

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_WSA)
        [DllImport(ImportString, CallingConvention = Convention)]
        public static extern void Dissonance_SetAgcIsOutputMutedState(IntPtr handle, bool isMuted);
#else
        private static bool _setAgcMutedStatePlatformWarningSent;
        public static void Dissonance_SetAgcIsOutputMutedState(IntPtr handle, bool isMuted)
        {
            if (!_setAgcMutedStatePlatformWarningSent)
                Log.Debug("`Dissonance_SetAgcIsOutputMutedState` is not available on this platform");
            _setAgcMutedStatePlatformWarningSent = true;
        }
#endif

        public enum SampleRates
        {
            // ReSharper disable UnusedMember.Local
            SampleRate8KHz = 8000,
            SampleRate16KHz = 16000,
            SampleRate32KHz = 32000,
            SampleRate48KHz = 48000,
            // ReSharper restore UnusedMember.Local
        }

        public enum ProcessorErrors
        {
            // ReSharper disable UnusedMember.Local
            Ok,

            Unspecified = -1,
            CreationFailed = -2,
            UnsupportedComponent = -3,
            UnsupportedFunction = -4,
            NullPointer = -5,
            BadParameter = -6,
            BadSampleRate = -7,
            BadDataLength = -8,
            BadNumberChannels = -9,
            FileError = -10,
            StreamParameterNotSet = -11,
            NotEnabled = -12,
            // ReSharper restore UnusedMember.Local
        }

        public enum FilterState
        {
            // ReSharper disable UnusedMember.Local
            FilterNotRunning,
            FilterNoInstance,
            FilterNoSamplesSubmitted,

            FilterOk
            // ReSharper restore UnusedMember.Local
        }
        #endregion
    }
}
