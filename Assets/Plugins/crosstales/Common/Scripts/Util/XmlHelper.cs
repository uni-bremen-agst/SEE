using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Helper-class for XML.</summary>
   public abstract class XmlHelper
   {
      /// <summary>Serialize an object to a XML-file.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <param name="filename">File name of the XML.</param>
      public static void SerializeToFile<T>(T obj, string filename)
      {
         if (null == obj)
            throw new System.ArgumentNullException(nameof(obj));

         if (filename == null)
            throw new System.ArgumentNullException(nameof(filename));

         try
         {
            System.IO.File.WriteAllText(filename, SerializeToString(obj));
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not serialize the object to a file: {ex}");
         }
      }

      /// <summary>Serialize an object to a XML-string.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <returns>Object as XML-string</returns>
      public static string SerializeToString<T>(T obj)
      {
         if (null == obj)
            throw new System.ArgumentNullException(nameof(obj));

         byte[] result = SerializeToByteArray(obj);

         return result != null ? System.Text.Encoding.UTF8.GetString(result) : string.Empty;
      }

      /// <summary>Serialize an object to a XML byte-array.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <returns>Object as byte-array</returns>
      public static byte[] SerializeToByteArray<T>(T obj)
      {
         if (null == obj)
            throw new System.ArgumentNullException(nameof(obj));

         try
         {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            System.Xml.XmlTextWriter xmlTextWriter = new System.Xml.XmlTextWriter(ms, System.Text.Encoding.UTF8);
            xs.Serialize(xmlTextWriter, obj);

            ms = (System.IO.MemoryStream)xmlTextWriter.BaseStream;
            return ms.ToArray();
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not serialize the object to a byte-array: {ex}");
         }

         return null;
      }

      /// <summary>Deserialize a XML-file to an object.</summary>
      /// <param name="filename">XML-file of the object</param>
      /// <param name="skipBOM">Skip BOM (optional, default: false)</param>
      /// <returns>Object</returns>
      public static T DeserializeFromFile<T>(string filename, bool skipBOM = false)
      {
         if (filename == null)
            throw new System.ArgumentNullException(nameof(filename));

         try
         {
            if (System.IO.File.Exists(filename))
            {
               string data = System.IO.File.ReadAllText(filename);

               if (string.IsNullOrEmpty(data))
               {
                  Debug.LogWarning($"Data was null: {filename}");
               }
               else
               {
                  return DeserializeFromString<T>(data, skipBOM);
               }
            }
            else
            {
               Debug.LogError($"File does not exist: {filename}");
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not deserialize the object from a file: {ex}");
         }

         return default;
      }

      /// <summary>Deserialize a XML-string to an object.</summary>
      /// <param name="xmlAsString">XML of the object</param>
      /// <param name="skipBOM">Skip BOM (optional, default: true)</param>
      /// <returns>Object</returns>
      public static T DeserializeFromString<T>(string xmlAsString, bool skipBOM = true)
      {
         if (string.IsNullOrEmpty(xmlAsString))
            throw new System.ArgumentNullException(nameof(xmlAsString));

         try
         {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (System.IO.StringReader sr = new System.IO.StringReader(xmlAsString.Trim()))
            {
               if (skipBOM)
                  sr.Read(); //skip BOM

               return (T)xs.Deserialize(sr);
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not deserialize the object from a string: {ex}");
         }

         return default;
      }

      /// <summary>Deserialize a XML byte-array to an object.</summary>
      /// <param name="data">XML of the object</param>
      /// <returns>Object</returns>
      public static T DeserializeFromByteArray<T>(byte[] data)
      {
         if (data == null)
            throw new System.ArgumentNullException(nameof(data));

         try
         {
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
            System.IO.MemoryStream ms = new System.IO.MemoryStream(data);

            return (T)xs.Deserialize(ms);
         }
         catch (System.Exception ex)
         {
            Debug.LogError($"Could not deserialize the object from a byte-array: {ex}");
         }

         return default;
      }

      /// <summary>Deserialize a Unity XML resource (TextAsset) to an object.</summary>
      /// <param name="resourceName">Name of the resource</param>
      /// <param name="skipBOM">Skip BOM (optional, default: true)</param>
      /// <returns>Object</returns>
      public static T DeserializeFromResource<T>(string resourceName, bool skipBOM = true)
      {
         if (string.IsNullOrEmpty(resourceName))
            throw new System.ArgumentNullException(nameof(resourceName));

         // Load the resource
         TextAsset xml = Resources.Load(resourceName) as TextAsset;

         return xml != null ? DeserializeFromString<T>(xml.text, skipBOM) : default;
      }
   }
}
// © 2014-2022 crosstales LLC (https://www.crosstales.com)