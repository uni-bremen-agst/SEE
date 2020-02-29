using UnityEngine;

namespace SEE.Net.Internal
{

    public interface IInitializer<T>
    {
        void Initialize(T value);
    }

    public static class Serializer
    {
        public static string Serialize(object arg)
        {
            return JsonUtility.ToJson(arg);
        }
        public static T Deserialize<T>(string arg)
        {
            return JsonUtility.FromJson<T>(arg);
        }

        public static SMesh ToSerializableObject(Mesh arg)
        {
            return new SMesh(arg);
        }
        public static SMeshFilter ToSerializableObject(MeshFilter arg)
        {
            return new SMeshFilter(arg);
        }
    }

}
