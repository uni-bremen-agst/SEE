using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Config
{
    public class ChatRoomSettings
#if !NCRUNCH
        : ScriptableObject
#endif
    {
        private const string SettingsFileResourceName = "ChatRoomSettings";
        public static readonly string SettingsFilePath = Path.Combine(DissonanceRootPath.BaseResourcePath, SettingsFileResourceName + ".asset");
        private static readonly List<string> DefaultRooms = new List<string> { "Global", "Red Team", "Blue Team", "Room A", "Room B" };

        // ReSharper disable once FieldCanBeMadeReadOnly.Global (Justification: Confuses unity serialization)
        [SerializeField] internal List<string> Names;

        [NonSerialized] private Dictionary<ushort, string> _nameLookup;

        private static ChatRoomSettings _instance;
        [NotNull]
        public static ChatRoomSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        public ChatRoomSettings()
        {
            Names = new List<string>(DefaultRooms);
        }

        [CanBeNull] public string FindRoomById(ushort id)
        {
            //Lazily initialize the lookup table
            if (_nameLookup == null)
            {
                var d = new Dictionary<ushort, string>();
                for (var i = 0; i < Names.Count; i++)
                    d[Names[i].ToRoomId()] = Names[i];

                _nameLookup = d;
            }

            string value;
            if (!_nameLookup.TryGetValue(id, out value))
                return null;
            else
                return value;
        }

        public static ChatRoomSettings Load()
        {
#if NCRUNCH
            return new ChatRoomSettings();
#else
            var r = Resources.Load<ChatRoomSettings>(SettingsFileResourceName);
            if (r == null)
                r = CreateInstance<ChatRoomSettings>();
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