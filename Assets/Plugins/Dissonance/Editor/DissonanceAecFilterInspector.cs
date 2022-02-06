using System.Collections.Generic;
using System.Globalization;
using Dissonance.Audio.Capture;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public class DissonanceAecFilterInspector
        : IAudioEffectPluginGUI
    {
        private bool _initialized;
        private Texture2D _logo;

        private void Initialize()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");

            _initialized = true;
        }

        public override bool OnGUI([NotNull] IAudioEffectPlugin plugin)
        {
            if (!_initialized)
                Initialize();

            GUILayout.Label(_logo);
            EditorGUILayout.HelpBox("This filter captures data to drive acoustic echo cancellation. All audio which passes through this filter will be played through your " +
                                    "speakers. When the audio enters your microphone (as echo) it will be removed.", MessageType.Info);

            if (Application.isPlaying)
            {
                var state = WebRtcPreprocessingPipeline.GetAecFilterState();
                switch (state)
                {
                    case AudioPluginDissonanceNative.FilterState.FilterNoInstance:
                        EditorGUILayout.HelpBox("AEC filter is running, but it is not associated with a microphone preprocessor - Microphone not running?", MessageType.Info);
                        break;

                    case AudioPluginDissonanceNative.FilterState.FilterNoSamplesSubmitted:
                        EditorGUILayout.HelpBox("AEC filter is running, but no samples were submitted in the last frame - Could indicate audio thread starvation", MessageType.Warning);
                        break;

                    case AudioPluginDissonanceNative.FilterState.FilterNotRunning:
                        EditorGUILayout.HelpBox("AEC filter is not running - Audio device not initialized?", MessageType.Warning);
                        break;

                    case AudioPluginDissonanceNative.FilterState.FilterOk:
                        break;

                    default:
                        EditorGUILayout.HelpBox("Unknown Filter State!", MessageType.Error);
                        break;
                }

                // `GetFloatBuffer` (a built in Unity method) causes a null reference exception when called. This bug seems to be limited to Unity 2019.3 on MacOS.
                // See tracking issue: https://github.com/Placeholder-Software/Dissonance/issues/177
#if (UNITY_EDITOR_OSX && UNITY_2019_3)
                EditorGUILayout.HelpBox("Cannot show detailed statistics in Unity 2019.3 due to an editor bug. Please update to Unity 2019.4 or newer!", MessageType.Error);
#else
                float[] data;
                if (plugin.GetFloatBuffer("AecMetrics", out data, 10))
                {
                    EditorGUILayout.LabelField(
                        new GUIContent("Delay Median (samples)"),
                        FormatNumber(data[0])
                    );

                    EditorGUILayout.LabelField(
                        new GUIContent("Delay Deviation"),
                        FormatNumber(data[1])
                    );

                    EditorGUILayout.LabelField(
                        new GUIContent("Fraction Poor Delays"),
                        FormatPercentage(data[2])
                    );

                    EditorGUILayout.LabelField(
                        new GUIContent("Echo Return Loss"),
                        FormatNumber(data[3])
                    );

                    EditorGUILayout.LabelField(
                        new GUIContent("Echo Return Loss Enhancement"),
                        FormatNumber(data[6])
                    );

                    EditorGUILayout.LabelField(
                        new GUIContent("Residual Echo Likelihood"),
                        FormatPercentage(data[9])
                    );

                    ShowHints(data);
                }
#endif
            }

            return false;
        }

        private void ShowHints([NotNull] IReadOnlyList<float> data)
        {
            var delayHigh = data[0] > 250;
            var delayDevHigh = data[0] > 0 && data[1] > 0 && data[1] > (data[0] * 0.25f);
            var poorDelaysHigh = data[2] > 0 && data[2] > 0.25f;
            var erlHigh = data[3] > 40;
            var erlLow = data[3] > 0 && data[3] < 6;

            if (delayHigh)
                EditorGUILayout.HelpBox("`Delay Median (samples)` is very high. This may indicate a very high latency audio system (e.g. bluetooth headphone/microphone) which will prevent AEC from working.", MessageType.Warning);

            if (delayDevHigh || poorDelaysHigh)
                EditorGUILayout.HelpBox("`Delay Deviation` or `Fraction Poor Delays` is very high. This may indicate an overworked CPU (low FPS).", MessageType.Warning);

            if (erlHigh)
                EditorGUILayout.HelpBox("`Echo Return Loss` is very high. This may indicate that there is not much feedback or no audio is playing.", MessageType.Warning);

            if (erlLow)
                EditorGUILayout.HelpBox("`Echo Return Loss` is very low. This indicates that there is a lot of feedback.", MessageType.Warning);
        }

        [NotNull] private static GUIContent FormatNumber(float value)
        {
            if (value < 0)
                return new GUIContent("Initialising...");
            else
                return new GUIContent(value.ToString(CultureInfo.InvariantCulture));
        }

        [NotNull] private static GUIContent FormatPercentage(float value)
        {
            if (value < 0)
                return new GUIContent("Initialising...");
            else
                return new GUIContent((value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%");
        }

        [NotNull] public override string Name
        {
            get { return "Dissonance Echo Cancellation"; }
        }

        [NotNull] public override string Description
        {
            get { return "Captures audio for Dissonance Acoustic Echo Cancellation"; }
        }

        [NotNull] public override string Vendor
        {
            get { return "Placeholder Software"; }
        }
    }
}
