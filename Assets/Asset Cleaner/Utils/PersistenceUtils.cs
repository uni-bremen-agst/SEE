using System.IO;
using UnityEngine;

namespace Asset_Cleaner {
    static class PersistenceUtils {
        public static void Load(ref Config result) {
            var serializable = Deserialize();
            AufSerializableData.OnDeserialize(in serializable, ref result);
        }

        public static void Save(in Config src) {
            AufSerializableData.OnSerialize(in src, out var serializable);
            var json = JsonUtility.ToJson(serializable);
            File.WriteAllText(Path, json);
        }

        static AufSerializableData Deserialize() {
            AufSerializableData serializableData;
            string json;

            if (!File.Exists(Path)) {
                // not exists - write new
                serializableData = AufSerializableData.Default();
                json = JsonUtility.ToJson(serializableData);
                File.WriteAllText(Path, json);
            }
            else {
                // exists
                json = File.ReadAllText(Path);

                if (string.IsNullOrEmpty(json)) {
                    // but corrupted - overwrite with new
                    serializableData = AufSerializableData.Default();
                    json = JsonUtility.ToJson(serializableData);
                    File.WriteAllText(Path, json);
                }

                serializableData = JsonUtility.FromJson<AufSerializableData>(json);
                if (serializableData.Valid())
                    return serializableData;

                serializableData = AufSerializableData.Default();
                json = JsonUtility.ToJson(serializableData);
                File.WriteAllText(Path, json);
            }

            return serializableData;
        }

        static string Path => $"{Application.temporaryCachePath}/AssetCleaner_{AufSerializableData.CurrentVersion}.json";

        // [MenuItem("Tools/LogPath")]
        static void Log() {
            Debug.Log(Application.temporaryCachePath);
        }
    }
}