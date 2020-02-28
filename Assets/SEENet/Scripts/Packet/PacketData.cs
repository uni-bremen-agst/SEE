using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SEE.Net.Internal
{

    public abstract class PacketData
    {
        public static readonly string DATE_TIME_FORMAT = "yyyy.MM.dd HH:mm:ss.fffffff";
        private const char DELIM = ';';
        private static readonly char[] DELIMS = new char[] { DELIM };

        #region Serialization
        public abstract string Serialize();
        protected static string Serialize(object[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                Debug.LogWarning("Count of tokens is 0!");
                return "";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(Serialize(tokens[0]));
            for (int i = 1; i < tokens.Length; i++)
            {
                sb.Append(DELIM + Serialize(tokens[i]));
            }
            return sb.ToString();
        }
        protected static string Serialize(object o)
        {
            Dictionary<Type, Func<string>> switchDict = new Dictionary<Type, Func<string>>
            {
                { typeof(Color), () => Serialize((Color)o) },
                { typeof(DateTime), () => Serialize((DateTime)o) },
                { typeof(float), () => Serialize((float)o) },
                { typeof(int), () => Serialize((int)o) },
                { typeof(Vector2), () => Serialize((Vector2)o) },
                { typeof(Vector3), () => Serialize((Vector3)o) },
                { typeof(Vector4), () => Serialize((Vector4)o) },
                { typeof(Quaternion), () => Serialize((Quaternion)o) },
                { typeof(string), () => (string)o }
            };
            bool result = result = switchDict.TryGetValue(o.GetType(), out Func<string> func);
            return result ? func() : throw new ArgumentException("Object '" + o + "' of type '" + o.GetType() + "' can not be serialized!");
        }
        protected static string Serialize(Color c)
        {
            return Serialize(new object[] { c.r, c.g, c.b, c.a });
        }
        protected static string Serialize(DateTime dt)
        {
            return dt.ToString(DATE_TIME_FORMAT);
        }
        protected static string Serialize(float f)
        {
            return f.ToString(CultureInfo.InvariantCulture);
        }
        protected static string Serialize(Vector2 v)
        {
            return Serialize(v.x) + DELIM + Serialize(v.y);
        }
        protected static string Serialize(Vector3 v)
        {
            return Serialize(v.x) + DELIM + Serialize(v.y) + DELIM + Serialize(v.z);
        }
        protected static string Serialize(Vector4 v)
        {
            return Serialize(v.x) + DELIM + Serialize(v.y) + DELIM + Serialize(v.z) + DELIM + Serialize(v.w);
        }
        protected static string Serialize(Quaternion q)
        {
            return Serialize(q.x) + DELIM + Serialize(q.y) + DELIM + Serialize(q.z) + DELIM + Serialize(q.w);
        }
        #endregion

        #region Deserialization
        protected static Color DeserializeColor(string data, out string croppedData)
        {
            return new Color(
                DeserializeFloat(data, out croppedData),
                DeserializeFloat(croppedData, out croppedData),
                DeserializeFloat(croppedData, out croppedData),
                DeserializeFloat(croppedData, out croppedData)
            );
        }
        protected static DateTime DeserializeDateTime(string data, out string croppedData)
        {
            return DateTime.Parse(DeserializeString(data, out croppedData));
        }
        protected static float DeserializeFloat(string data, out string croppedData)
        {
            return float.Parse(GetNextToken(data, out croppedData), CultureInfo.InvariantCulture);
        }
        protected static int DeserializeInt(string data, out string croppedData)
        {
            return int.Parse(GetNextToken(data, out croppedData), CultureInfo.InvariantCulture);
        }
        protected static Vector2 DeserializeVector2(string data, out string croppedData)
        {
            Vector2 v = Vector3.zero;
            croppedData = data;
            for (int i = 0; i < 2; i++)
            {
                v[i] = DeserializeFloat(croppedData, out croppedData);
            }
            return v;
        }
        protected static Vector3 DeserializeVector3(string data, out string croppedData)
        {
            Vector3 v = Vector3.zero;
            croppedData = data;
            for (int i = 0; i < 3; i++)
            {
                v[i] = DeserializeFloat(croppedData, out croppedData);
            }
            return v;
        }
        protected static Vector4 DeserializeVector4(string data, out string croppedData)
        {
            Vector4 v = Vector3.zero;
            croppedData = data;
            for (int i = 0; i < 4; i++)
            {
                v[i] = DeserializeFloat(croppedData, out croppedData);
            }
            return v;
        }
        protected static Quaternion DeserializeQuaternion(string data, out string croppedData)
        {
            Quaternion q = Quaternion.identity;
            croppedData = data;
            for (int i = 0; i < 4; i++)
            {
                q[i] = DeserializeFloat(croppedData, out croppedData);
            }
            return q;
        }
        protected static string DeserializeString(string data, out string croppedData)
        {
            return GetNextToken(data, out croppedData);
        }
        private static string GetNextToken(string data, out string croppedData)
        {
            string[] tokens = data.Split(DELIMS, 2);
            croppedData = tokens.Length == 2 ? tokens[1] : "";
            return tokens[0];
        }
        #endregion
    }

}
