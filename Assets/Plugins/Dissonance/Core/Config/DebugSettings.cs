using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Config
{
    public class DebugSettings
#if !NCRUNCH
        : ScriptableObject
#endif
    {
        private const string SettingsFileResourceName = "DebugSettings";
        public static readonly string SettingsFilePath = Path.Combine(DissonanceRootPath.BaseResourcePath, SettingsFileResourceName + ".asset");

#if NCRUNCH
        private const LogLevel DefaultLevel = LogLevel.Trace;
#else
        private const LogLevel DefaultLevel = LogLevel.Info;
#endif

        [SerializeField]
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Breaks unity serialization)
        private List<LogLevel> _levels;

        public bool EnableRecordingDiagnostics;
        public bool RecordMicrophoneRawAudio;
        public bool RecordPreprocessorOutput;

        public bool EnablePlaybackDiagnostics;
        public bool RecordDecodedAudio;
        public bool RecordFinalAudio;

        public bool EnableNetworkSimulation;
        //public int MinimumLatency;
        //public int MaximumLatency;
        public float PacketLoss;

        private static DebugSettings _instance;
        [NotNull] public static DebugSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        public DebugSettings()
        {
            var categories = ((LogCategory[])Enum.GetValues(typeof (LogCategory)))
                .Select(c => (int)c)
                .Max();

            _levels = new List<LogLevel>(categories + 1);
        }

        public LogLevel GetLevel(int category)
        {
            if (_levels.Count > category)
                return _levels[category];

            return DefaultLevel;
        }

        public void SetLevel(int category, LogLevel level)
        {
            if (_levels.Count <= category)
            {
                for (var i = _levels.Count; i <= category; i++)
                    _levels.Add(DefaultLevel);
            }

            _levels[category] = level;
        }

        private static DebugSettings Load()
        {
#if NCRUNCH
            return new DebugSettings();
#else
            var r = Resources.Load<DebugSettings>(SettingsFileResourceName);
            if (r == null)
                r = CreateInstance<DebugSettings>();
            return r;
#endif
        }

        public static void Preload()
        {
            if (_instance == null)
                _instance = Load();
        }
    }
}
