using System;
using System.Runtime.InteropServices;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;

namespace Dissonance.Audio
{
    public static class AecDiagnostics
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string ImportString = "__Internal";
        private const CallingConvention Convention = default(CallingConvention);
#else
        private const string ImportString = "AudioPluginDissonance";
        private const CallingConvention Convention = CallingConvention.Cdecl;
#endif

        [DllImport(ImportString, CallingConvention=Convention)]
        private static extern void Dissonance_GetAecMetrics(IntPtr buffer, int length);

        /// <summary>
        /// Get the current state of the AEC output filter
        /// </summary>
        /// <returns></returns>
        public static AecState GetState()
        {
            return (AecState)WebRtcPreprocessingPipeline.GetAecFilterState();
        }

        /// <summary>
        /// Read realtime statistics from the acoustic echo cancellation system. Refer to docs for
        /// correct AEC setup: https://placeholder-software.co.uk/dissonance/docs/Tutorials/Acoustic-Echo-Cancellation.html
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        public static AecStats GetStats([CanBeNull] ref float[] temp)
        {
            if (temp == null || temp.Length < 10)
                temp = new float[10];

            var handle = GCHandle.Alloc(temp, GCHandleType.Pinned);
            try
            {
                Dissonance_GetAecMetrics(handle.AddrOfPinnedObject(), temp.Length);
            }
            finally
            {
                handle.Free();
            }

            return new AecStats {
                DelayMedian = temp[0],
                DelayStdDev = temp[1],
                FractionPoorDelays = temp[2],
                EchoReturnLossAverage = temp[3],
                EchoReturnLossMin = temp[4],
                EchoReturnLossMax = temp[5],
                EchoReturnLossEnhancementAverage = temp[6],
                EchoReturnLossEnhancementMin = temp[7],
                EchoReturnLossEnhancementMax = temp[8],
                ResidualEchoLikelihood = temp[9],
            };
        }

        /// <summary>
        /// Statistics from the AEC filter.
        /// Negative values indicate that the filter is still in the "startup" phase.
        /// </summary>
        public struct AecStats
        {
            /// <summary>
            /// The average delay between audio output (through the Unity mixer) and audio input (through the microphone).
            /// Should be small (20-60) for AEC to function.
            /// </summary>
            public float DelayMedian;

            /// <summary>
            /// The standard deviation of delay.
            /// Should be small (0-10) for AEC to function.
            /// </summary>
            public float DelayStdDev;

            /// <summary>
            /// What fraction of the delays are bad.
            /// Should be zero for AEC to function.
            /// </summary>
            public float FractionPoorDelays;

            /// <summary>
            /// How much quieter is the recorded signal than the original output signal (dB).
            /// </summary>
            public float EchoReturnLossAverage;

            /// <summary>
            /// Minimum value of ERL in the last timeframe.
            /// </summary>
            public float EchoReturnLossMin;

            /// <summary>
            /// Maximum value of ERL in the last timeframe.
            /// </summary>
            public float EchoReturnLossMax;

            /// <summary>
            /// How much echo was removed from the signal (dB). This is a direct measurement of how well AEC is working.
            /// </summary>
            public float EchoReturnLossEnhancementAverage;

            /// <summary>
            /// Minimum value of ERLE in the last timeframe.
            /// </summary>
            public float EchoReturnLossEnhancementMin;

            /// <summary>
            /// Maximum value of ERLE in the last timeframe.
            /// </summary>
            public float EchoReturnLossEnhancementMax;

            /// <summary>
            /// Chance that echoes which the AEC cannot remove are present in the signal.
            /// </summary>
            public float ResidualEchoLikelihood;
        }

        public enum AecState
        {
            // ReSharper disable UnusedMember.Local

            /// <summary>
            /// Output filter is not yet running (no audio output).
            /// </summary>
            FilterNotRunning,

            /// <summary>
            /// Output filter has been created but there is no preprocessing pipeline to apply
            /// result to (no audio input).
            /// </summary>
            FilterNoInstance,

            /// <summary>
            /// Output filter was not delivered any audio samples in the last update tick (CPU starvation).
            /// </summary>
            FilterNoSamplesSubmitted,

            /// <summary>
            /// Output filter is running correctly.
            /// </summary>
            FilterOk
            // ReSharper restore UnusedMember.Local
        }
    }
}
