using UnityEngine;
using System.Linq;

namespace Crosstales
{
   /// <summary>Various extension methods.</summary>
   public static class ExtensionMethods
   {
      #region Variables

      private static readonly Vector3 flat = new Vector3(1, 0, 1);

      #endregion


      #region Strings

      /// <summary>
      /// Extension method for strings.
      /// Converts a string to title case (first letter uppercase).
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>Converted string in title case.</returns>
      public static string CTToTitleCase(this string str)
      {
         if (str == null)
            return str;
#if UNITY_WSA || UNITY_XBOXONE
         return toTitleCase(str);
#else
         return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
#endif
      }

#if UNITY_WSA || UNITY_XBOXONE
     /// <summary>
     /// Converts to title case: each word starts with an upper case.
     /// </summary>
     private static string toTitleCase(string str)
     {
         if (str.Length == 0)
             return str;

         System.Text.StringBuilder result = new System.Text.StringBuilder(str);

         result[0] = char.ToUpper(result[0]);

         for (int ii = 1; ii < result.Length; ii++)
         {
             if (char.IsWhiteSpace(result[ii - 1]))
                 result[ii] = char.ToUpper(result[ii]);
             else
                 result[ii] = char.ToLower(result[ii]);
         }

         return result.ToString();
     }
#endif

      /// <summary>
      /// Extension method for strings.
      /// Reverses a string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>Reversed string.</returns>
      public static string CTReverse(this string str)
      {
         if (str == null)
            return str;

         char[] charArray = str.ToCharArray();
         System.Array.Reverse(charArray);

         return new string(charArray);
      }

      /// <summary>
      /// Extension method for strings.
      /// Case insensitive 'Replace'.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="oldString">String to replace.</param>
      /// <param name="newString">New replacement string.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>Replaced string.</returns>
      public static string CTReplace(this string str, string oldString, string newString, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            return str;

         if (oldString == null)
            return str;

         if (newString == null)
            return str;

         bool matchFound;
         do
         {
            int index = str.IndexOf(oldString, comp);

            matchFound = index >= 0;

            if (matchFound)
            {
               str = str.Remove(index, oldString.Length);

               str = str.Insert(index, newString);
            }
         } while (matchFound);

         return str;
      }

      /// <summary>
      /// Extension method for strings.
      /// Case insensitive 'Equals'.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String to check.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>True if the string contains the given string.</returns>
      public static bool CTEquals(this string str, string toCheck, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            return false;

         //if (toCheck == null)
         //    throw new System.ArgumentNullException("toCheck");

         return str.Equals(toCheck, comp);
      }

      /// <summary>
      /// Extension method for strings.
      /// Case insensitive 'Contains'.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String to check.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>True if the string contains the given string.</returns>
      public static bool CTContains(this string str, string toCheck, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            return false;

         //if (toCheck == null)
         //    throw new System.ArgumentNullException("toCheck");

         return str.IndexOf(toCheck, comp) >= 0;
      }

      /// <summary>
      /// Extension method for strings.
      /// Contains any given string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="searchTerms">Search terms separated by the given split-character.</param>
      /// <param name="splitChar">Split-character (default: ' ', optional)</param>
      /// <returns>True if the string contains any parts of the given string.</returns>
      public static bool CTContainsAny(this string str, string searchTerms, char splitChar = ' ')
      {
         if (str == null)
            return false;

         if (string.IsNullOrEmpty(searchTerms))
            return true;

         char[] split = {splitChar};

         return searchTerms.Split(split, System.StringSplitOptions.RemoveEmptyEntries).Any(searchTerm => str.CTContains(searchTerm));
      }

      /// <summary>
      /// Extension method for strings.
      /// Contains all given strings.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="searchTerms">Search terms separated by the given split-character.</param>
      /// <param name="splitChar">Split-character (default: ' ', optional)</param>
      /// <returns>True if the string contains all parts of the given string.</returns>
      public static bool CTContainsAll(this string str, string searchTerms, char splitChar = ' ')
      {
         if (str == null)
            return false;

         if (string.IsNullOrEmpty(searchTerms))
            return true;

         char[] split = {splitChar};

         return searchTerms.Split(split, System.StringSplitOptions.RemoveEmptyEntries).All(searchTerm => str.CTContains(searchTerm));
      }

      /// <summary>
      /// Extension method for strings.
      /// Replaces new lines with a replacement string pattern.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="replacement">Replacement string pattern (default: "#nl#", optional).</param>
      /// <param name="newLine">New line string (default: System.Environment.NewLine, optional).</param>
      /// <returns>Replaced string without new lines.</returns>
      public static string CTRemoveNewLines(this string str, string replacement = "#nl#", string newLine = null)
      {
         if (str == null)
            return str;

         return str.Replace(string.IsNullOrEmpty(newLine) ? System.Environment.NewLine : newLine, replacement);
      }

      /// <summary>
      /// Extension method for strings.
      /// Replaces a given string pattern with new lines in a string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="replacement">Replacement string pattern (default: "#nl#", optional).</param>
      /// <param name="newLine">New line string (default: System.Environment.NewLine, optional).</param>
      /// <returns>Replaced string with new lines.</returns>
      public static string CTAddNewLines(this string str, string replacement = "#nl#", string newLine = null)
      {
         if (str == null)
            return str;

         return str.CTReplace(replacement, string.IsNullOrEmpty(newLine) ? System.Environment.NewLine : newLine);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is numeric.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is numeric.</returns>
      public static bool CTisNumeric(this string str)
      {
         if (str == null)
            return false;

         return double.TryParse(str, out double output);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is integer.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is integer.</returns>
      public static bool CTisInteger(this string str)
      {
         if (str == null)
            return false;

         return !str.Contains(".") && long.TryParse(str, out long output);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is an email address.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is an email address.</returns>
      public static bool CTisEmail(this string str)
      {
         if (str == null)
            return false;

         return Common.Util.BaseConstants.REGEX_EMAIL.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is a website address.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is a website address.</returns>
      public static bool CTisWebsite(this string str)
      {
         if (str == null)
            return false;

         return Common.Util.BaseConstants.REGEX_URL_WEB.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is a creditcard.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is a creditcard.</returns>
      public static bool CTisCreditcard(this string str)
      {
         if (str == null)
            return false;

         return Common.Util.BaseConstants.REGEX_CREDITCARD.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is an IPv4 address.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is an IPv4 address.</returns>
      public static bool CTisIPv4(this string str)
      {
         if (str == null)
            return false;

         return Common.Util.BaseConstants.REGEX_IP_ADDRESS.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string is alphanumeric.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string is alphanumeric.</returns>
      public static bool CTisAlphanumeric(this string str)
      {
         if (str == null)
            return false;

         return Common.Util.BaseConstants.REGEX_ALPHANUMERIC.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string has line endings.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string has line endings.</returns>
      public static bool CThasLineEndings(this string str)
      {
         if (str == null)
            return false;

         return Crosstales.Common.Util.BaseConstants.REGEX_LINEENDINGS.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string has invalid characters.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <returns>True if the string has invalid characters.</returns>
      public static bool CThasInvalidChars(this string str)
      {
         if (str == null)
            return false;

         return Common.Util.BaseConstants.REGEX_INVALID_CHARS.IsMatch(str);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string starts with another string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String to check.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>True if the string is integer.</returns>
      public static bool CTStartsWith(this string str, string toCheck, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            return false;

         return string.IsNullOrEmpty(toCheck) || str.StartsWith(toCheck, comp);
      }

      /// <summary>
      /// Extension method for strings.
      /// Checks if the string ends with another string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String to check.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>True if the string is integer.</returns>
      public static bool CTEndsWith(this string str, string toCheck, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            return false;

         return string.IsNullOrEmpty(toCheck) || str.EndsWith(toCheck, comp);
      }

      /// <summary>
      /// Extension method for strings.
      /// Returns the index of the last occurence of a given string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String for the index.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>The index of the last occurence of the given string if the string is integer.</returns>
      public static int CTLastIndexOf(this string str, string toCheck, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            throw new System.ArgumentNullException(nameof(str));

         return string.IsNullOrEmpty(toCheck) ? 0 : str.LastIndexOf(toCheck, comp);
      }

      /// <summary>
      /// Extension method for strings.
      /// Returns the index of the first occurence of a given string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String for the index.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>The index of the first occurence of the given string if the string is integer.</returns>
      public static int CTIndexOf(this string str, string toCheck, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            throw new System.ArgumentNullException(nameof(str));

         return string.IsNullOrEmpty(toCheck) ? 0 : str.IndexOf(toCheck, comp);
      }

      /// <summary>
      /// Extension method for strings.
      /// Returns the index of the first occurence of a given string.
      /// </summary>
      /// <param name="str">String-instance.</param>
      /// <param name="toCheck">String for the index.</param>
      /// <param name="startIndex">Start index for the check.</param>
      /// <param name="comp">StringComparison-method (default: StringComparison.OrdinalIgnoreCase, optional)</param>
      /// <returns>The index of the first occurence of the given string if the string is integer.</returns>
      public static int CTIndexOf(this string str, string toCheck, int startIndex, System.StringComparison comp = System.StringComparison.OrdinalIgnoreCase)
      {
         if (str == null)
            throw new System.ArgumentNullException(nameof(str));

         return string.IsNullOrEmpty(toCheck) ? 0 : str.IndexOf(toCheck, startIndex, comp);
      }

      /// <summary>
      /// Extension method for strings.
      /// Converts the value of a string to a Base64-string.
      /// </summary>
      /// <param name="str">Input string.</param>
      /// <returns>String value as converted Base64-string.</returns>
      public static string CTToBase64(this string str)
      {
         if (str == null)
            return null;

         byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(str);
         return System.Convert.ToBase64String(plainTextBytes);
      }

      /// <summary>
      /// Extension method for strings.
      /// Converts the value of a Base64-string to a string.
      /// </summary>
      /// <param name="str">Input Base64-string.</param>
      /// <returns>Base64-string value as converted string.</returns>
      public static string CTFromBase64(this string str)
      {
         if (str == null)
            return null;

         byte[] base64EncodedBytes = System.Convert.FromBase64String(str);
         return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
      }

      /// <summary>
      /// Extension method for strings.
      /// Converts the value of a string to a Hex-string (with Unicode support).
      /// </summary>
      /// <param name="str">Input string.</param>
      /// <param name="addPrefix">Add "0x"-as prefix (default: false, optional).</param>
      /// <returns>String value as converted Hex-string.</returns>
      public static string CTToHex(this string str, bool addPrefix = false)
      {
         if (str == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         if (addPrefix)
            sb.Append("0x");

         byte[] bytes = System.Text.Encoding.Unicode.GetBytes(str);
         foreach (byte t in bytes)
         {
            sb.Append(t.ToString("X2"));
         }

         return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
      }

      /// <summary>
      /// Extension method for strings.
      /// Converts the Hex-value of a string to a string (with Unicode support).
      /// </summary>
      /// <param name="hexString">Input as Hex-string.</param>
      /// <returns>Hex-string value as converted string.</returns>
      public static string CTHexToString(this string hexString)
      {
         if (hexString == null)
            return null;

         string _hex = hexString;

         if (_hex.StartsWith("0x"))
            _hex = _hex.Substring(2);

         if (hexString.Length % 2 != 0)
            throw new System.FormatException($"String seems to be an invalid hex-code: {hexString}");

         var bytes = new byte[_hex.Length / 2];
         for (var i = 0; i < bytes.Length; i++)
         {
            bytes[i] = System.Convert.ToByte(hexString.Substring(i * 2, 2), 16);
         }

         //return System.Text.Encoding.ASCII.GetString(bytes);
         return System.Text.Encoding.Unicode.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
      }

      /// <summary>
      /// Extension method for strings.
      /// Converts the Hex-value of a string to a color.
      /// </summary>
      /// <param name="hexString">Input as Hex-string.</param>
      /// <returns>Hex-string value as Color.</returns>
      public static Color CTHexToColor(this string hexString)
      {
         if (hexString == null)
            throw new System.ArgumentNullException(nameof(hexString));

         string _hex = hexString;

         if (_hex.StartsWith("0x"))
            _hex = _hex.Substring(2);

         if (_hex.StartsWith("#"))
            _hex = _hex.Substring(1);

         if (_hex.Length != 6 && _hex.Length != 8)
            throw new System.FormatException($"String seems to be an invalid color: {_hex}");

         byte r = System.Convert.ToByte(_hex.Substring(0, 2), 16);
         byte g = System.Convert.ToByte(_hex.Substring(2, 2), 16);
         byte b = System.Convert.ToByte(_hex.Substring(4, 2), 16);
         byte a = 0xFF;

         if (_hex.Length == 0)
            a = System.Convert.ToByte(_hex.Substring(6, 2), 16);

         Color32 color = new Color32(r, g, b, a);

         //Debug.Log("Hex orig: '" + _hex + "'");
         //Debug.Log("Color: " + color);

         return color;
      }

      #endregion


      #region Arrays

      /// <summary>
      /// Extension method for arrays.
      /// Shuffles an array.
      /// </summary>
      /// <param name="array">Array-instance to shuffle.</param>
      /// <param name="seed">Seed for the PRNG (default: 0 (=standard), optional)</param>
      public static void CTShuffle<T>(this T[] array, int seed = 0)
      {
         if (array == null || array.Length <= 0)
            throw new System.ArgumentNullException(nameof(array));

         System.Random rnd = seed == 0 ? new System.Random() : new System.Random(seed);
         int n = array.Length;
         while (n > 1)
         {
            int k = rnd.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
         }
      }

      /// <summary>
      /// Extension method for arrays.
      /// Dumps an array to a string.
      /// </summary>
      /// <param name="array">Array-instance to dump.</param>
      /// <param name="prefix">Prefix for every element (default: empty, optional).</param>
      /// <param name="postfix">Postfix for every element (default: empty, optional).</param>
      /// <returns>String with lines for all array entries.</returns>
      public static string CTDump<T>(this T[] array, string prefix = "", string postfix = "")
      {
         if (array == null) // || array.Length <= 0)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (T element in array)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(prefix);
            sb.Append(element);
            sb.Append(postfix);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Quaternion-arrays.
      /// Dumps an array to a string.
      /// </summary>
      /// <param name="array">Quaternion-array-instance to dump.</param>
      /// <returns>String with lines for all array entries.</returns>
      public static string CTDump(this Quaternion[] array)
      {
         if (array == null) // || array.Length <= 0)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Quaternion element in array)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
            sb.Append(", ");
            sb.Append(element.z);
            sb.Append(", ");
            sb.Append(element.w);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Vector2-arrays.
      /// Dumps an array to a string.
      /// </summary>
      /// <param name="array">Vector2-array-instance to dump.</param>
      /// <returns>String with lines for all array entries.</returns>
      public static string CTDump(this Vector2[] array)
      {
         if (array == null) // || array.Length <= 0)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Vector2 element in array)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Vector3-arrays.
      /// Dumps an array to a string.
      /// </summary>
      /// <param name="array">Vector3-array-instance to dump.</param>
      /// <returns>String with lines for all array entries.</returns>
      public static string CTDump(this Vector3[] array)
      {
         if (array == null) // || array.Length <= 0)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Vector3 element in array)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
            sb.Append(", ");
            sb.Append(element.z);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Vector4-arrays.
      /// Dumps an array to a string.
      /// </summary>
      /// <param name="array">Vector4-array-instance to dump.</param>
      /// <returns>String with lines for all array entries.</returns>
      public static string CTDump(this Vector4[] array)
      {
         if (array == null) // || array.Length <= 0)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Vector4 element in array)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
            sb.Append(", ");
            sb.Append(element.z);
            sb.Append(", ");
            sb.Append(element.w);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for arrays.
      /// Generates a string array with all entries (via ToString).
      /// </summary>
      /// <param name="array">Array-instance to ToString.</param>
      /// <returns>String array with all entries (via ToString).</returns>
      public static string[] CTToString<T>(this T[] array)
      {
         if (array == null) // || array.Length <= 0)
            throw new System.ArgumentNullException(nameof(array));

         string[] result = new string[array.Length];

         for (int ii = 0; ii < array.Length; ii++)
         {
            result[ii] = null == array[ii] ? "null" : array[ii].ToString();
         }

         return result;
      }

      /// <summary>
      /// Extension method for byte-arrays.
      /// Converts a byte-array to a float-array.
      /// </summary>
      /// <param name="array">Array-instance to convert.</param>
      /// <param name="count">Number of bytes to convert (optional).</param>
      /// <returns>Converted float-array.</returns>
      public static float[] CTToFloatArray(this byte[] array, int count = 0)
      {
         if (array == null) // || array.Length <= 0)
            throw new System.ArgumentNullException(nameof(array));

         int _count = count;

         if (_count <= 0)
            _count = array.Length;

         float[] floats = new float[_count / 2];

         int ii = 0;
         for (int zz = 0; zz < _count; zz += 2)
         {
            floats[ii] = bytesToFloat(array[zz], array[zz + 1]);
            ii++;
         }

         return floats;
      }

      /// <summary>
      /// Extension method for float-arrays.
      /// Converts a float-array to a byte-array.
      /// </summary>
      /// <param name="array">Array-instance to convert.</param>
      /// <param name="count">Number of floats to convert (optional).</param>
      /// <returns>Converted byte-array.</returns>
      public static byte[] CTToByteArray(this float[] array, int count = 0)
      {
         if (array == null) // || array.Length <= 0)
            throw new System.ArgumentNullException(nameof(array));

         int _count = count;

         if (_count <= 0)
            _count = array.Length;

         byte[] bytes = new byte[_count * 2];
         int byteIndex = 0;

         for (int ii = 0; ii < _count; ii++)
         {
            short outsample = (short)(array[ii] * short.MaxValue);

            bytes[byteIndex] = (byte)(outsample & 0xff);

            bytes[byteIndex + 1] = (byte)((outsample >> 8) & 0xff);

            byteIndex += 2;
         }

         return bytes;
      }

      #endregion


      #region Lists

      /// <summary>
      /// Extension method for IList.
      /// Shuffles a List.
      /// </summary>
      /// <param name="list">IList-instance to shuffle.</param>
      /// <param name="seed">Seed for the PRNG (default: 0 (=standard), optional)</param>
      public static void CTShuffle<T>(this System.Collections.Generic.IList<T> list, int seed = 0)
      {
         if (list == null)
            throw new System.ArgumentNullException(nameof(list));

         System.Random rnd = seed == 0 ? new System.Random() : new System.Random(seed);
         int n = list.Count;

         while (n > 1)
         {
            int k = rnd.Next(n--);
            T temp = list[n];
            list[n] = list[k];
            list[k] = temp;
         }
      }

      /// <summary>
      /// Extension method for IList.
      /// Dumps a list to a string.
      /// </summary>
      /// <param name="list">IList-instance to dump.</param>
      /// <param name="prefix">Prefix for every element (default: empty, optional).</param>
      /// <param name="postfix">Postfix for every element (default: empty, optional).</param>
      /// <returns>String with lines for all list entries.</returns>
      public static string CTDump<T>(this System.Collections.Generic.IList<T> list, string prefix = "", string postfix = "")
      {
         if (list == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (T element in list)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(prefix);
            sb.Append(element);
            sb.Append(postfix);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Quaternion-IList.
      /// Dumps a list to a string.
      /// </summary>
      /// <param name="list">Quaternion-IList-instance to dump.</param>
      /// <returns>String with lines for all list entries.</returns>
      public static string CTDump(this System.Collections.Generic.IList<Quaternion> list)
      {
         if (list == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Quaternion element in list)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
            sb.Append(", ");
            sb.Append(element.z);
            sb.Append(", ");
            sb.Append(element.w);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Vector2-IList.
      /// Dumps a list to a string.
      /// </summary>
      /// <param name="list">Vector2-IList-instance to dump.</param>
      /// <returns>String with lines for all list entries.</returns>
      public static string CTDump(this System.Collections.Generic.IList<Vector2> list)
      {
         if (list == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Vector2 element in list)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Vector3-IList.
      /// Dumps a list to a string.
      /// </summary>
      /// <param name="list">Vector3-IList-instance to dump.</param>
      /// <returns>String with lines for all list entries.</returns>
      public static string CTDump(this System.Collections.Generic.IList<Vector3> list)
      {
         if (list == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Vector3 element in list)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
            sb.Append(", ");
            sb.Append(element.z);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for Vector4-IList.
      /// Dumps a list to a string.
      /// </summary>
      /// <param name="list">Vector4-IList-instance to dump.</param>
      /// <returns>String with lines for all list entries.</returns>
      public static string CTDump(this System.Collections.Generic.IList<Vector4> list)
      {
         if (list == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (Vector4 element in list)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(element.x);
            sb.Append(", ");
            sb.Append(element.y);
            sb.Append(", ");
            sb.Append(element.z);
            sb.Append(", ");
            sb.Append(element.w);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for IList.
      /// Generates a string list with all entries (via ToString).
      /// </summary>
      /// <param name="list">IList-instance to ToString.</param>
      /// <returns>String list with all entries (via ToString).</returns>
      public static System.Collections.Generic.List<string> CTToString<T>(this System.Collections.Generic.IList<T> list)
      {
         if (list == null)
            throw new System.ArgumentNullException(nameof(list));

         System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>(list.Count);
         result.AddRange(list.Select(element => null == element ? "null" : element.ToString()));

         return result;
      }

      #endregion


      #region Dictionaries

      /// <summary>
      /// Extension method for IDictionary.
      /// Dumps a dictionary to a string.
      /// </summary>
      /// <param name="dict">IDictionary-instance to dump.</param>
      /// <param name="prefix">Prefix for every element (default: empty, optional).</param>
      /// <param name="postfix">Postfix for every element (default: empty, optional).</param>
      /// <returns>String with lines for all dictionary entries.</returns>
      public static string CTDump<K, V>(this System.Collections.Generic.IDictionary<K, V> dict, string prefix = "", string postfix = "")
      {
         if (dict == null)
            return null;

         System.Text.StringBuilder sb = new System.Text.StringBuilder();

         foreach (System.Collections.Generic.KeyValuePair<K, V> kvp in dict)
         {
            if (0 < sb.Length)
               sb.Append(System.Environment.NewLine);

            sb.Append(prefix);
            sb.Append("Key = ");
            sb.Append(kvp.Key);
            sb.Append(", Value = ");
            sb.Append(kvp.Value);
            sb.Append(postfix);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Extension method for IDictionary.
      /// Adds a dictionary to an existing one.
      /// </summary>
      /// <param name="dict">IDictionary-instance.</param>
      /// <param name="collection">Dictionary to add.</param>
      public static void CTAddRange<K, V>(this System.Collections.Generic.IDictionary<K, V> dict, System.Collections.Generic.IDictionary<K, V> collection)
      {
         if (dict == null)
            throw new System.ArgumentNullException(nameof(dict));

         if (collection == null)
            throw new System.ArgumentNullException(nameof(collection));

         foreach (System.Collections.Generic.KeyValuePair<K, V> item in collection)
         {
            if (!dict.ContainsKey(item.Key))
            {
               dict.Add(item.Key, item.Value);
            }
            else
            {
               // handle duplicate key issue here
               Debug.LogWarning($"Duplicate key found: {item.Key}");
            }
         }
      }

      #endregion


      #region Streams

      /// <summary>
      /// Extension method for Stream.
      /// Reads the full content of a Stream.
      /// </summary>
      /// <param name="input">Stream-instance to read.</param>
      /// <returns>Byte-array of the Stream content.</returns>
      public static byte[] CTReadFully(this System.IO.Stream input)
      {
         if (input == null)
            throw new System.ArgumentNullException(nameof(input));

         using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
         {
            input.CopyTo(ms);
            return ms.ToArray();
         }
      }

      #endregion


      #region Color

      /// <summary>
      /// Extension method for Color.
      /// Converts the value of a color to a Hex-string.
      /// </summary>
      /// <param name="input">Color to convert.</param>
      /// <returns>Color value as Hex.</returns>
      public static string CTToHex(this Color input)
      {
         Color32 color = input;
         return $"{(color.r + 0x01):X2}{(color.g + 0x01):X2}{(color.b + 0x01):X2}";
      }

      /// <summary>
      /// Extension method for Color.
      /// Convert it to a Vector3.
      /// </summary>
      /// <param name="color">Color-instance to convert.</param>
      /// <returns>Vector3 from color.</returns>
      public static Vector3 CTVector3(this Color color)
      {
         return new Vector3(color.r, color.g, color.b);
      }

      /// <summary>
      /// Extension method for Color.
      /// Convert it to a Vector4.
      /// </summary>
      /// <param name="color">Color-instance to convert.</param>
      /// <returns>Vector3 from color.</returns>
      public static Vector4 CTVector4(this Color color)
      {
         return new Vector4(color.r, color.g, color.b, color.a);
      }

      #endregion


      #region Vector2

      /// <summary>
      /// Allows you to multiply two Vector2s together, something Unity sorely lacks by default.
      /// </summary>
      /// <param name="right">Second vector</param>
      /// <returns>The x*x, y*y result.</returns>
      public static Vector2 CTMultiply(this Vector2 a, Vector2 b)
      {
         return new Vector2(a.x * b.x, a.y * b.y);
      }

      #endregion


      #region Vector3

      /// <summary>
      /// Allows you to multiply two Vector3s together, something Unity sorely lacks by default.
      /// </summary>
      /// <param name="right">Second vector</param>
      /// <returns>The x*x, y*y, z*z result.</returns>
      public static Vector3 CTMultiply(this Vector3 a, Vector3 b)
      {
         return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
      }

      /// <summary>
      /// Returns a Vector3 with a 0 y-axis. This is useful for keeping entities oriented perpendicular to the ground.
      /// </summary>
      public static Vector3 CTFlatten(this Vector3 a)
      {
         return a.CTMultiply(flat);
      }

      /// <summary>
      /// Extension method for Vector3.
      /// Convert it to a Quaternion.
      /// </summary>
      /// <param name="eulerAngle">Vector3-instance to convert.</param>
      /// <returns>Quaternion from euler angles.</returns>
      public static Quaternion CTQuaternion(this Vector3 eulerAngle)
      {
         return Quaternion.Euler(eulerAngle);
      }

      /// <summary>
      /// Extension method for Vector3.
      /// Convert it to a Color.
      /// </summary>
      /// <param name="rgb">Vector3-instance to convert (RGB = xyz).</param>
      /// <param name="alpha">Alpha-value of the color (default: 1, optional).</param>
      /// <returns>Color from RGB.</returns>
      public static Color CTColorRGB(this Vector3 rgb, float alpha = 1f)
      {
         return new Color(Mathf.Clamp01(rgb.x), Mathf.Clamp01(rgb.y), Mathf.Clamp01(rgb.z), Mathf.Clamp01(alpha));
      }

      #endregion


      #region Vector4

      /// <summary>
      /// Allows you to multiply two Vector4s together, something Unity sorely lacks by default.
      /// </summary>
      /// <param name="right">Second vector</param>
      /// <returns>The x*x, y*y, z*z, w*w result.</returns>
      public static Vector4 CTMultiply(this Vector4 a, Vector4 b)
      {
         return new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
      }

      /// <summary>
      /// Extension method for Vector4.
      /// Convert it to a Quaternion.
      /// </summary>
      /// <param name="angle">Vector4-instance to convert.</param>
      /// <returns>Quaternion from Vector4.</returns>
      public static Quaternion CTQuaternion(this Vector4 angle)
      {
         return new Quaternion(angle.x, angle.y, angle.z, angle.w);
      }

      /// <summary>
      /// Extension method for Vector4.
      /// Convert it to a Color.
      /// </summary>
      /// <param name="rgba">Vector4-instance to convert (RGBA = xyzw).</param>
      /// <returns>Color from RGBA.</returns>
      public static Color CTColorRGBA(this Vector4 rgba)
      {
         return new Color(Mathf.Clamp01(rgba.x), Mathf.Clamp01(rgba.y), Mathf.Clamp01(rgba.z), Mathf.Clamp01(rgba.w));
      }

      #endregion


      #region Quaternion

      /// <summary>
      /// Extension method for Quaternion.
      /// Convert it to a Vector3.
      /// </summary>
      /// <param name="angle">Quaternion-instance to convert.</param>
      /// <returns>Vector3 from Quaternion.</returns>
      public static Vector3 CTVector3(this Quaternion angle)
      {
         return angle.eulerAngles;
      }

      /// <summary>
      /// Extension method for Quaternion.
      /// Convert it to a Vector4.
      /// </summary>
      /// <param name="angle">Quaternion-instance to convert.</param>
      /// <returns>Vector4 from Quaternion.</returns>
      public static Vector4 CTVector4(this Quaternion angle)
      {
         return new Vector4(angle.x, angle.y, angle.z, angle.w);
      }

      #endregion


      #region Canvas

      /// <summary>
      /// Extension method for Canvas.
      /// Convert current resolution scale.
      /// </summary>
      /// <param name="canvas">Canvas to convert.</param>
      /// <returns>Vector3 with the correct scale.</returns>
      public static Vector3 CTCorrectLossyScale(this Canvas canvas)
      {
         if (!Application.isPlaying)
            return Vector3.one;

         if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
         {
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler && scaler.enabled)
            {
               scaler.enabled = false;
               var before = canvas.GetComponent<RectTransform>().lossyScale;
               scaler.enabled = true;
               var after = canvas.GetComponent<RectTransform>().lossyScale;
               return new Vector3(after.x / before.x, after.y / before.y, after.z / before.z);
            }

            return Vector3.one;
         }

         return canvas.GetComponent<RectTransform>().lossyScale;
      }

      #endregion


      #region RectTransform

      /// <summary>
      /// Extension method for RectTransform.
      /// Sets the corners of a RectTransform.
      /// </summary>
      /// <param name="rt">RectTransform-instance.</param>
      /// <param name="fourCornersArray">Corners for the RectTransform.</param>
      /// <param name="canvas">Relevant canvas.</param>
      /// <param name="inset">Inset from the corners.</param>
      public static void CTGetLocalCorners(this RectTransform rt, Vector3[] fourCornersArray, Canvas canvas, float inset)
      {
         rt.GetLocalCorners(fourCornersArray);
         if (inset != 0)
         {
            var uis = canvas.CTCorrectLossyScale();
            fourCornersArray[0].x += inset * uis.x;
            fourCornersArray[0].y += inset * uis.y;
            fourCornersArray[1].x += inset * uis.x;
            fourCornersArray[1].y -= inset * uis.y;
            fourCornersArray[2].x -= inset * uis.x;
            fourCornersArray[2].y -= inset * uis.y;
            fourCornersArray[3].x -= inset * uis.x;
            fourCornersArray[3].y += inset * uis.y;
         }
      }

      /// <summary>
      /// Extension method for RectTransform.
      /// Sets the world corners of a RectTransform.
      /// </summary>
      /// <param name="rt">RectTransform-instance.</param>
      /// <param name="fourCornersArray">Corners for the RectTransform.</param>
      /// <param name="canvas">Relevant canvas.</param>
      /// <param name="inset">Inset from the corners.</param>
      public static void CTGetScreenCorners(this RectTransform rt, Vector3[] fourCornersArray, Canvas canvas, float inset)
      {
         // if screen space overlay mode then world corners are already in screen space
         // if screen space camera mode then screen settings are in world and need to be converted to screen
         rt.GetWorldCorners(fourCornersArray);
         if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            for (int i = 0; i < 4; i++)
            {
               fourCornersArray[i] = canvas.worldCamera.WorldToScreenPoint(fourCornersArray[i]);
               fourCornersArray[i].z = 0;
            }

         if (inset != 0)
         {
            var uis = canvas.CTCorrectLossyScale();
            fourCornersArray[0].x += inset * uis.x;
            fourCornersArray[0].y += inset * uis.y;
            fourCornersArray[1].x += inset * uis.x;
            fourCornersArray[1].y -= inset * uis.y;
            fourCornersArray[2].x -= inset * uis.x;
            fourCornersArray[2].y -= inset * uis.y;
            fourCornersArray[3].x -= inset * uis.x;
            fourCornersArray[3].y += inset * uis.y;
         }
      }

      #endregion


      #region MonoBehaviour

      /// <summary>
      /// Extension method for MonoBehaviour.
      /// Recursively searches all children of a parent MonoBehaviour for specific named GameObject
      /// </summary>
      /// <param name="parent">Parent of the current children.</param>
      /// <param name="name">Name of the GameObject.</param>
      /// <returns>GameObject with the given name or null.</returns>
      public static GameObject CTFind(this MonoBehaviour parent, string name)
      {
         if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

         return parent.transform.CTFind(name).gameObject;
      }

      /// <summary>
      /// Extension method for MonoBehaviour.
      /// Recursively searches all children of a parent MonoBehaviour for specific named GameObject and returns a component.
      /// </summary>
      /// <param name="parent">Parent of the current children.</param>
      /// <param name="name">Name of the GameObject.</param>
      /// <returns>Component with the given type or null.</returns>
      public static T CTFind<T>(this MonoBehaviour parent, string name)
      {
         if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

         return parent.transform.CTFind<T>(name);
      }

      #endregion


      #region GameObject

      /// <summary>
      /// Extension method for GameObject.
      /// Recursively searches all children of a parent GameObject for specific named GameObject
      /// </summary>
      /// <param name="parent">Parent of the current children.</param>
      /// <param name="name">Name of the GameObject.</param>
      /// <returns>GameObject with the given name or null.</returns>
      public static GameObject CTFind(this GameObject parent, string name)
      {
         if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

         return parent.transform.CTFind(name).gameObject;
      }

      /// <summary>
      /// Extension method for GameObject.
      /// Recursively searches all children of a parent GameObject for specific named GameObject and returns a component.
      /// </summary>
      /// <param name="parent">Parent of the current children.</param>
      /// <param name="name">Name of the GameObject.</param>
      /// <returns>Component with the given type or null.</returns>
      public static T CTFind<T>(this GameObject parent, string name)
      {
         if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

         return parent.transform.CTFind<T>(name);
      }

      #endregion


      #region Transform

      /// <summary>
      /// Extension method for Transform.
      /// Recursively searches all children of a parent transform for specific named transform
      /// </summary>
      /// <param name="parent">Parent of the current children.</param>
      /// <param name="name">Name of the transform.</param>
      /// <returns>Transform with the given name or null.</returns>
      public static Transform CTFind(this Transform parent, string name)
      {
         if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

         if (name == null)
            throw new System.ArgumentNullException(nameof(name));

         return deepSearch(parent, name);
      }

      /// <summary>
      /// Extension method for Transform.
      /// Recursively searches all children of a parent transform for specific named transform and returns a component.
      /// </summary>
      /// <param name="parent">Parent of the current children.</param>
      /// <param name="name">Name of the transform.</param>
      /// <returns>Component with the given type or null.</returns>
      public static T CTFind<T>(this Transform parent, string name)
      {
         if (parent == null)
            throw new System.ArgumentNullException(nameof(parent));

         Transform tf = parent.CTFind(name);

         if (tf != null)
            return tf.gameObject.GetComponent<T>();

         return default(T);
      }

      #endregion

      #region Unity specific

      /// <summary>
      /// Extension method for Renderer.
      /// Determines if the renderer is visible from a certain camera.
      /// </summary>
      /// <param name="renderer">Renderer to test the visibility.</param>
      /// <param name="camera">Camera for the test.</param>
      /// <returns>True if the renderer is visible by the given camera.</returns>
      public static bool CTIsVisibleFrom(this Renderer renderer, Camera camera)
      {
         if (renderer == null)
            throw new System.ArgumentNullException(nameof(renderer));

         if (camera == null)
            throw new System.ArgumentNullException(nameof(camera));

         Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
         return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
      }

      #endregion


      #region Private methods

      private static Transform deepSearch(Transform parent, string name)
      {
         Transform tf = parent.Find(name);

         if (tf != null)
            return tf;

         foreach (Transform child in parent)
         {
            tf = deepSearch(child, name);
            if (tf != null)
               return tf;
         }

         return null;
      }

      private static float bytesToFloat(byte firstByte, byte secondByte)
      {
         // convert two bytes to one short (little endian) and convert it to range from -1 to (just below) 1
         return (short)((secondByte << 8) | firstByte) / Common.Util.BaseConstants.FLOAT_32768;
      }

      #endregion

      /*
      /// <summary>
      /// Perform a deep Copy of the object.
      /// </summary>
      /// <typeparam name="T">The type of object being copied.</typeparam>
      /// <param name="source">The object instance to copy.</param>
      /// <returns>The copied object.</returns>
      public static T Clone<T>(this T source)
      {
          if (!typeof(T).IsSerializable)
          {
              throw new ArgumentException("The type must be serializable.", "source");
          }

          // Don't serialize a null object, simply return the default for that object
          if (Object.ReferenceEquals(source, null))
          {
              return default(T);
          }

          IFormatter formatter = new BinaryFormatter();
          Stream stream = new MemoryStream();
          using (stream)
          {
              formatter.Serialize(stream, source);
              stream.Seek(0, SeekOrigin.Begin);
              return (T)formatter.Deserialize(stream);
          }
      }
      */
      /*
          /// <summary>
          /// Clone a List with elememts containing a copy constructor.
          /// </summary>
          /// <param name="list">List-instance to clone.</param>
          /// <returns>Clones list.</returns>
          public static List<T> CTClone<T>(this List<T> listToClone) where T : ICopyable
          {
              List<T> newList = new List<T>(listToClone.Count);
  
              listToClone.ForEach((item) =>
              {
                  newList.Add(new T(item));
              });
  
              return newList;
  
              //return listToClone.Select(item => (T)item.Clone()).ToList();
          }
        */

      /*
      public static string[] CTToUppercase(string[] array)
      {
          if (array == null || array.Length <= 0)
              throw new ArgumentNullException("array");

          string[] result = new string[array.Length];

          for (int ii = 0; ii < array.Length; ii++)
          {
              result[ii] = array[ii].ToUpper();
          }

          return result;
      }

      public static string[] CTToLowercase(string[] array)
      {
          if (array == null || array.Length <= 0)
              throw new ArgumentNullException("array");

          string[] result = new string[array.Length];

          for (int ii = 0; ii < array.Length; ii++)
          {
              result[ii] = array[ii].ToLower();
          }

          return result;
      }
  */
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)