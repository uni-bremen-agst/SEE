using SEE.Game;
using System;
using System.Collections.Generic;
using System.IO;

namespace SEE.Utils
{
    public class ConfigIO
    {        
        /// <summary>
        /// The attribute label for the relative path of a DataPath in the stored configuration file.
        /// </summary>
        protected const string RelativePathLabel = "RelativePath";
        /// <summary>
        /// The attribute label for the absolute path of a DataPath in the stored configuration file.
        /// </summary>
        protected const string AbsolutePathLabel = "AbsolutePath";
        /// <summary>
        /// The attribute label for the root kind of a DataPath in the stored configuration file.
        /// </summary>
        protected const string RootLabel = "Root";

        /// <summary>
        /// The separator between a label and its value.
        /// </summary>
        protected const char LabelSeparator = ':';

        /// <summary>
        /// The separator between attribute specifications.
        /// </summary>
        protected const char AttributeSeparator = ';';

        /// <summary>
        /// The opening token for a composite attribute value.
        /// </summary>
        protected const char Open = '{';
        /// <summary>
        /// The closing token for a composite attribute value.
        /// </summary>
        protected const char Close = '}';

        public static void Restore<T>(Dictionary<string, object> attributes, string label, ref T value)
        {
            if (attributes.TryGetValue(label, out object v))
            {
                try
                {
                    value = (T)v;
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: {typeof(T)}. Actual type: {v.GetType()}");
                }
            }
        }

        public static void RestorePath(Dictionary<string, object> attributes, string label, ref DataPath dataPath)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> path = dictionary as Dictionary<string, object>;
                {
                    string value = "";
                    Restore<string>(path, RelativePathLabel, ref value);
                    dataPath.RelativePath = value;
                }
                {
                    string value = "";
                    Restore<string>(path, AbsolutePathLabel, ref value);
                    dataPath.AbsolutePath = value;
                }
                RestoreEnum<DataPath.RootKind>(path, RootLabel, ref dataPath.Root);
            }
        }

        private static void RestoreEnum<E>(Dictionary<string, object> dict, string label, ref E value) where E : struct, IConvertible
        {
            if (!typeof(E).IsEnum)
            {
                throw new ArgumentException("Generic type parameter E must be an enumerated type");
            }
            // enum values are stored as string
            string stringValue = "";
            Restore<string>(dict, RootLabel, ref stringValue);
            if (string.IsNullOrEmpty(stringValue))
            {
                throw new Exception("Enum value must neither be null nor the empty string.");
            }
            if (Enum.TryParse<E>(stringValue, out E enumValue))
            {
                value = enumValue;
            }
        }

        // ---------------------------------------------------------
        // Output
        // ---------------------------------------------------------

        private static void SaveLabel(StreamWriter stream, string label)
        {
            stream.Write(label + NiceLabelValueSeparator());
        }

        private static string NiceLabelValueSeparator()
        {
            return " " + LabelSeparator + " ";
        }

        private static void InternalSave(StreamWriter stream, string label, string value, bool newLine)
        {
            SaveLabel(stream, label);
            stream.Write(value + AttributeSeparator);
            if (newLine)
            {
                stream.WriteLine();
            }
        }

        internal static void Save(StreamWriter stream, string label, float value, bool newLine = true)
        {
            InternalSave(stream, label, value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture), newLine);
        }

        internal static void Save(StreamWriter stream, string label, string value, bool newLine = true)
        {
            InternalSave(stream, label, "\"" + Escape(value) + "\"", newLine);
        }

        internal static void Save(StreamWriter stream, string label, bool value, bool newLine = true)
        {
            InternalSave(stream, label, value.ToString(), newLine);
        }

        internal static void Save(StreamWriter stream, string label, DataPath path)
        {
            SaveLabel(stream, label);
            NiceLabelValueSeparator();

            BeginGroup(stream);
            Save(stream, RootLabel, path.Root.ToString(), newLine: false);
            Space(stream);
            Save(stream, RelativePathLabel, path.RelativePath, newLine: false);
            Space(stream);
            Save(stream, AbsolutePathLabel, path.AbsolutePath, newLine: false);
            EndGroup(stream);
            stream.WriteLine(AttributeSeparator);
        }

        /// <summary>
        /// Returns <paramref name="value"/> where every quote " has been replaced by a double quote "".
        /// </summary>
        /// <param name="value">the string where " is to be escaped</param>
        /// <returns>replacement of " by ""</returns>
        private static string Escape(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private static void BeginGroup(StreamWriter stream)
        {
            stream.Write(Open);
        }

        private static void EndGroup(StreamWriter stream)
        {
            stream.Write(Close);
        }

        private static void Space(StreamWriter stream)
        {
            stream.Write(" ");
        }
    }
}
