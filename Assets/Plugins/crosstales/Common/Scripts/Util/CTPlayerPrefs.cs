using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Wrapper for the PlayerPrefs.</summary>
   public abstract class CTPlayerPrefs
   {
/*
#if UNITY_EDITOR
      private static readonly SerializableDictionary<string, string> content = new SerializableDictionary<string, string>();

      private static readonly string fileName = $"{Application.persistentDataPath}/crosstales.cfg";

      static CTPlayerPrefs()
      {
         if (System.IO.File.Exists(fileName))
            content = XmlHelper.DeserializeFromFile<SerializableDictionary<string, string>>(fileName);

         if (content == null)
            content = new SerializableDictionary<string, string>();
      }
#endif
*/
      /// <summary>Exists the key?</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static bool HasKey(string key)
      {
         return !string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key);
         //throw new System.ArgumentNullException(nameof(key));

//#if UNITY_EDITOR
//         return content.ContainsKey(key);
//#else
//#endif
      }

      /// <summary>Deletes all keys.</summary>
      public static void DeleteAll()
      {
//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         PlayerPrefs.DeleteAll();
//#else
//         content.Clear();
//#endif
      }

      /// <summary>Delete the key.</summary>
      /// <param name="key">Key to delete in the PlayerPrefs.</param>
      public static void DeleteKey(string key)
      {
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         PlayerPrefs.DeleteKey(key);
//#else
//         content.Remove(key);
//#endif
      }

      /// <summary>Saves all modifications.</summary>
      public static void Save()
      {
//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         PlayerPrefs.Save();
/*
#else
         if (content != null && content.Count > 0)
         {
            XmlHelper.SerializeToFile(content, fileName);
         }
#endif
*/
      }


      #region Getter

      /// <summary>Allows to get a string from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static string GetString(string key)
      {
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         return PlayerPrefs.GetString(key);
//#else
//         return content[key];
//#endif
      }

      /// <summary>Allows to get a float from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static float GetFloat(string key)
      {
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         return PlayerPrefs.GetFloat(key);
//#else
//         float.TryParse(GetString(key), out float result);
//         return result;
//#endif
      }

      /// <summary>Allows to get an int from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static int GetInt(string key)
      {
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         return PlayerPrefs.GetInt(key);
//#else
//         int.TryParse(GetString(key), out int result);
//         return result;
//#endif
      }

      /// <summary>Allows to get a bool from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static bool GetBool(string key)
      {
         return "true".CTEquals(GetString(key));
      }

      /// <summary>Allows to get a DateTime from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static System.DateTime GetDate(string key)
      {
         System.DateTime.TryParseExact(GetString(key), "yyyyMMddHHmmsss", null, System.Globalization.DateTimeStyles.None, out System.DateTime result);

         return result;
      }

      /// <summary>Allows to get a Vector2 from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static Vector2 GetVector2(string key)
      {
         string[] values = GetString(key).Split(';');

         float x = float.Parse(values[0]);
         float y = float.Parse(values[1]);

         return new Vector2(x, y);
      }

      /// <summary>Allows to get a Vector3 from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static Vector3 GetVector3(string key)
      {
         string[] values = GetString(key).Split(';');

         float x = float.Parse(values[0]);
         float y = float.Parse(values[1]);
         float z = float.Parse(values[2]);

         return new Vector3(x, y, z);
      }

      /// <summary>Allows to get a Vector4 from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static Vector4 GetVector4(string key)
      {
         string[] values = GetString(key).Split(';');

         float x = float.Parse(values[0]);
         float y = float.Parse(values[1]);
         float z = float.Parse(values[2]);
         float w = float.Parse(values[3]);

         return new Vector4(x, y, z, w);
      }

      /// <summary>Allows to get a Quaternion from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static Quaternion GetQuaternion(string key)
      {
         return GetVector4(key).CTQuaternion();
      }

      /// <summary>Allows to get a Color from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static Color GetColor(string key)
      {
         return GetVector4(key).CTColorRGBA();
      }

      /// <summary>Allows to get a SystemLanguage from a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <returns>Value for the key.</returns>
      public static SystemLanguage GetLanguage(string key)
      {
         return (SystemLanguage)System.Enum.Parse(typeof(SystemLanguage), GetString(key));
      }

      #endregion


      #region Setter

      /// <summary>Allows to set a string for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetString(string key, string value)
      {
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         PlayerPrefs.SetString(key, value);
/*         
#else
         if (content.ContainsKey(key))
         {
            content[key] = value;
         }
         else
         {
            content.Add(key, value);
         }
#endif
*/
      }

      /// <summary>Allows to set a float for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetFloat(string key, float value)
      {
//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

         PlayerPrefs.SetFloat(key, value);
//#else
//         SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
//#endif
      }

      /// <summary>Allows to set an int for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetInt(string key, int value)
      {
//#if (UNITY_WSA || UNITY_WEBGL) && !UNITY_EDITOR
         if (string.IsNullOrEmpty(key))
            throw new System.ArgumentNullException(nameof(key));

         PlayerPrefs.SetInt(key, value);
//#else
//         SetString(key, value.ToString());
//#endif
      }

      /// <summary>Allows to set a bool for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetBool(string key, bool value)
      {
         SetString(key, value ? "true" : "false");
      }

      /// <summary>Allows to set a DateTime for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetDate(string key, System.DateTime value)
      {
         if (value == null)
            throw new System.ArgumentNullException(nameof(value));

         SetString(key, value.ToString("yyyyMMddHHmmsss"));
      }

      /// <summary>Allows to set a Vector2 for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetVector2(string key, Vector2 value)
      {
         if (value == null)
            throw new System.ArgumentNullException(nameof(value));

         SetString(key, $"{value.x};{value.y}");
      }

      /// <summary>Allows to set a Vector3 for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetVector3(string key, Vector3 value)
      {
         if (value == null)
            throw new System.ArgumentNullException(nameof(value));

         SetString(key, $"{value.x};{value.y};{value.z}");
      }

      /// <summary>Allows to set a Vector4 for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetVector4(string key, Vector4 value)
      {
         if (value == null)
            throw new System.ArgumentNullException(nameof(value));

         SetString(key, $"{value.x};{value.y};{value.z};{value.w}");
      }

      /// <summary>Allows to set a Quaternion for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetQuaternion(string key, Quaternion value)
      {
         if (value == null)
            throw new System.ArgumentNullException(nameof(value));

         SetVector4(key, value.CTVector4());
      }

      /// <summary>Allows to set a Color for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetColor(string key, Color value)
      {
         if (value == null)
            throw new System.ArgumentNullException(nameof(value));

         SetVector4(key, value.CTVector4());
      }

      /// <summary>Allows to set a SystemLanguage for a key.</summary>
      /// <param name="key">Key for the PlayerPrefs.</param>
      /// <param name="value">Value for the PlayerPrefs.</param>
      public static void SetLanguage(string key, SystemLanguage language)
      {
         SetString(key, language.ToString());
      }

      #endregion
   }
}
// © 2015-2021 crosstales LLC (https://www.crosstales.com)