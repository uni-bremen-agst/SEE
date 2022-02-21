#if !NCRUNCH

using System;
using Dissonance.Config;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(DebugSettings))]
    public class DebugSettingsEditor : UnityEditor.Editor
    {
        private Texture2D _logo;
        
        private bool _showLogSettings;
        private string[] _categoryNames;
        private int[] _categoryValues;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");

            _showLogSettings = true;
            _categoryNames = Enum.GetNames(typeof (LogCategory));
            _categoryValues = (int[])Enum.GetValues(typeof (LogCategory));

        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                GUILayout.Label(_logo);

                EditorGUILayout.Space();

                var settings = (DebugSettings)target;

                DrawLogSettings(settings);

                EditorGUILayout.Space();

                DrawRecordingSettings(settings);

                EditorGUILayout.Space();

                DrawPlaybackSettings(settings);

                EditorGUILayout.Space();

                //DrawNetworkSettings(settings);

                if (changed.changed)
                    EditorUtility.SetDirty(settings);
            }
        }

        private static void DrawNetworkSettings([NotNull] DebugSettings settings)
        {
            settings.EnableNetworkSimulation = EditorGUILayout.BeginToggleGroup("Network Simulation", settings.EnableNetworkSimulation);
            GUI.enabled = settings.EnableNetworkSimulation;
            EditorGUI.indentLevel++;

            //float minLatency = settings.MinimumLatency;
            //float maxLatency = settings.MaximumLatency;
            //EditorGUILayout.MinMaxSlider(new GUIContent("Latency (ms)"), ref minLatency, ref maxLatency, 0, 1000);
            //EditorGUI.indentLevel++;
            //settings.MinimumLatency = Math.Max(0, EditorGUILayout.IntField("Minimum", (int) minLatency));
            //settings.MaximumLatency = Math.Min(1000, EditorGUILayout.IntField("Maximum", (int) maxLatency));
            //EditorGUI.indentLevel--;

            settings.PacketLoss = EditorGUILayout.Slider("Packet Loss (%)", settings.PacketLoss * 100, 0, 100) / 100;

            EditorGUI.indentLevel--;
            GUI.enabled = true;
            EditorGUILayout.EndToggleGroup();
        }

        private static void DrawPlaybackSettings([NotNull] DebugSettings settings)
        {
            settings.EnablePlaybackDiagnostics = EditorGUILayout.BeginToggleGroup("Playback Diagnostics", settings.EnablePlaybackDiagnostics);
            GUI.enabled = settings.EnablePlaybackDiagnostics;
            EditorGUI.indentLevel++;

            settings.RecordDecodedAudio = EditorGUILayout.Toggle("Record Decoded Audio", settings.RecordDecodedAudio);
            settings.RecordFinalAudio = EditorGUILayout.Toggle("Record Final Audio", settings.RecordFinalAudio);

            EditorGUI.indentLevel--;
            GUI.enabled = true;
            EditorGUILayout.EndToggleGroup();
        }

        private static void DrawRecordingSettings([NotNull] DebugSettings settings)
        {
            settings.EnableRecordingDiagnostics = EditorGUILayout.BeginToggleGroup("Recording Diagnostics", settings.EnableRecordingDiagnostics);
            GUI.enabled = settings.EnableRecordingDiagnostics;
            EditorGUI.indentLevel++;

            settings.RecordMicrophoneRawAudio = EditorGUILayout.Toggle("Record Microphone", settings.RecordMicrophoneRawAudio);
            settings.RecordPreprocessorOutput = EditorGUILayout.Toggle("Record Preprocessor Output", settings.RecordPreprocessorOutput);

            EditorGUI.indentLevel--;
            GUI.enabled = true;
            EditorGUILayout.EndToggleGroup();
        }

        private void DrawLogSettings([NotNull] DebugSettings settings)
        {
            _showLogSettings = EditorGUILayout.Foldout(_showLogSettings, "Log Levels");
            if (_showLogSettings)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < _categoryNames.Length; i++)
                    settings.SetLevel(_categoryValues[i], (LogLevel) EditorGUILayout.EnumPopup(_categoryNames[i], settings.GetLevel(_categoryValues[i])));

                EditorGUI.indentLevel--;
            }
        }

        public static void GoToSettings()
        {
            var logSettings = LoadLogSettings();
            EditorApplication.delayCall += () =>
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = logSettings;
            };
        }

        private static DebugSettings LoadLogSettings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DebugSettings>(DebugSettings.SettingsFilePath);
            if (asset == null)
            {
                asset = CreateInstance<DebugSettings>();
                AssetDatabase.CreateAsset(asset, DebugSettings.SettingsFilePath);
                AssetDatabase.SaveAssets();
            }

            return asset;
        }
    }
}
#endif