#if !NCRUNCH

using Dissonance.Audio.Capture;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceSettings))]
    public class VoiceSettingsEditor : UnityEditor.Editor
    {
        private Texture2D _logo;
        private bool _showAecAdvanced;
        private bool _showAecmAdvanced;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                var settings = (VoiceSettings)target;

                GUILayout.Label(_logo);

                GUILayout.Space(8);
                if (GUILayout.Button("Reset To Defaults"))
                    settings.Reset();

                DrawQualitySettings(settings);
                EditorGUILayout.Space();
                DrawPreprocessorSettings(settings);
                EditorGUILayout.Space();
                DrawOtherSettings(settings);

                if (changed.changed)
                    EditorUtility.SetDirty(settings);
            }
        }

        private void DrawOtherSettings([NotNull] VoiceSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                settings.VoiceDuckLevel = Helpers.FromDecibels(EditorGUILayout.Slider("Audio Duck Attenuation (dB)", Helpers.ToDecibels(settings.VoiceDuckLevel), Helpers.MinDecibels, 0));
                EditorGUILayout.HelpBox("• How much remote voice volume will be reduced when local speech is being transmitted.\n" +
                                        "• A lower value will attenuate more but risks making remote speakers inaudible.", MessageType.Info);
            }
        }

        private void DrawPreprocessorSettings([NotNull] VoiceSettings settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                settings.DenoiseAmount = (NoiseSuppressionLevels)EditorGUILayout.EnumPopup(new GUIContent("Noise Suppression"), settings.DenoiseAmount);
                EditorGUILayout.HelpBox("• A higher value will remove more background noise (e.g. fans) but risks attenuating speech.\n" +
                                        "• A lower value will remove less noise, but will attenuate speech less.",
                                        MessageType.Info);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox("Background Sound Removal is currently experimental!", MessageType.Warning);

                settings.BackgroundSoundRemovalEnabled = EditorGUILayout.Toggle(new GUIContent("Background Sound Removal"), settings.BackgroundSoundRemovalEnabled);
                EditorGUILayout.HelpBox("• Enable machine learning based background sound removal (Rnnoise).\n" +
                                        "• Removes more non-speech background sounds (e.g. keyboard sounds) than classic noise suppression but risks distorting speech.", MessageType.Info);

                using (new EditorGUI.DisabledGroupScope(!settings.BackgroundSoundRemovalEnabled))
                {
                    settings.BackgroundSoundRemovalAmount = EditorGUILayout.Slider("Background Sound Removal Intensity", settings.BackgroundSoundRemovalAmount, 0, 1);

                    EditorGUILayout.HelpBox("• A higher value will remove more background sound but risks distorting speech.\n" +
                                            "• A lower value will remove less background sound but will distort speech less.",
                                            MessageType.Info);
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                settings.VadSensitivity = (VadSensitivityLevels)EditorGUILayout.EnumPopup(new GUIContent("Voice Detector Sensitivity"), settings.VadSensitivity);
                EditorGUILayout.HelpBox("• A higher value will detect more voice, but may also allow through more non-voice.\n" +
                                        "• A lower value will allow through less non-voice, but may not detect some speech.",
                                        MessageType.Info);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox("Ensure that you have followed the AEC setup instructions before enabling AEC:\n" +
                                        "https://placeholder-software.co.uk/dissonance/docs/Tutorials/Acoustic-Echo-Cancellation", MessageType.Warning);

                settings.AecmRoutingMode = (AecmRoutingMode)EditorGUILayout.EnumPopup(new GUIContent("Mobile Echo Cancellation"), settings.AecmRoutingMode);
                settings.AecSuppressionAmount = (AecSuppressionLevels)EditorGUILayout.EnumPopup(new GUIContent("Desktop Echo Cancellation"), settings.AecSuppressionAmount);
                EditorGUILayout.HelpBox("• A higher value will remove more echo, but risks distorting speech.\n" +
                                        "• A lower value will remove less echo, but will distort speech less.",
                                        MessageType.Info);

                EditorGUI.indentLevel++;
                _showAecAdvanced = EditorGUILayout.Foldout(_showAecAdvanced, new GUIContent("Advanced Desktop Options"), true);
                EditorGUI.indentLevel--;
                if (_showAecAdvanced)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                    {
                        if (Application.isPlaying)
                            EditorGUILayout.HelpBox("AEC advanced configuration cannot be changed at runtime", MessageType.Warning);

                        settings.AecDelayAgnostic = EditorGUILayout.Toggle(new GUIContent("Delay Agnostic Mode"), settings.AecDelayAgnostic);
                        settings.AecExtendedFilter = EditorGUILayout.Toggle(new GUIContent("Extended Filter"), settings.AecExtendedFilter);
                        settings.AecRefinedAdaptiveFilter = EditorGUILayout.Toggle(new GUIContent("Refined Adaptive Filter"), settings.AecRefinedAdaptiveFilter);
                    }
                }

                EditorGUI.indentLevel++;
                _showAecmAdvanced = EditorGUILayout.Foldout(_showAecmAdvanced, new GUIContent("Advanced Mobile Options"), true);
                EditorGUI.indentLevel--;
                if (_showAecmAdvanced)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                    {
                        if (Application.isPlaying)
                            EditorGUILayout.HelpBox("AECM advanced configuration cannot be changed at runtime", MessageType.Warning);

                        settings.AecmComfortNoise = EditorGUILayout.Toggle(new GUIContent("Comfort Noise"), settings.AecmComfortNoise);
                    }
                }
            }
        }

        private void DrawQualitySettings([NotNull] VoiceSettings settings)
        {
            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                EditorGUILayout.Space();

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var f = (FrameSize)EditorGUILayout.EnumPopup("Frame Size", settings.FrameSize);
                    if (!Application.isPlaying)
                        settings.FrameSize = f;

                    if (f == FrameSize.Tiny)
                        EditorGUILayout.HelpBox(string.Format("'{0}' frame size is only suitable for LAN usage due to very high bandwidth overhead!", FrameSize.Tiny), MessageType.Warning);

                    EditorGUILayout.HelpBox(
                        "• A smaller frame size will send smaller packets of data more frequently, improving latency at the expense of some network and CPU performance.\n" +
                        "• A larger frame size will send larger packets of data less frequently, gaining some network and CPU performance at the expense of latency.",
                        MessageType.Info
                    );
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var q = (AudioQuality)EditorGUILayout.EnumPopup("Audio Quality", settings.Quality);
                    if (!Application.isPlaying)
                        settings.Quality = q;
                    EditorGUILayout.HelpBox(
                        "• A lower quality setting uses less CPU and bandwidth, but sounds worse.\n" +
                        "• A higher quality setting uses more CPU and bandwidth, but sounds better.",
                        MessageType.Info);
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var fec = EditorGUILayout.Toggle("Forward Error Correction", settings.ForwardErrorCorrection);
                    if (!Application.isPlaying)
                        settings.ForwardErrorCorrection = fec;
                    EditorGUILayout.HelpBox(
                        "When network conditions are bad (high packet loss) use slightly more bandwidth to significantly improve audio quality.",
                        MessageType.Info);
                }

                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "Quality settings cannot be changed at runtime",
                        MessageType.Warning);
                }
            }
        }

        #region static helpers
        public static void GoToSettings()
        {
            var settings = LoadVoiceSettings();
            EditorApplication.delayCall += () =>
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = settings;
            };
        }

        private static VoiceSettings LoadVoiceSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VoiceSettings>(VoiceSettings.SettingsFilePath);
            if (asset == null)
            {
                asset = CreateInstance<VoiceSettings>();
                AssetDatabase.CreateAsset(asset, VoiceSettings.SettingsFilePath);
                AssetDatabase.SaveAssets();
            }

            return asset;
        }
        #endregion
    }
}
#endif