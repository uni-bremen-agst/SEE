using SEE.Game;
using System;
using System.IO;

namespace SEE.Utils
{
    public class ConfigWriter : ConfigIO, IDisposable
    {
        /// <summary>
        /// Where to write the output.
        /// </summary>
        private readonly StreamWriter stream;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">name of the file where the output is to be written</param>
        public ConfigWriter(string filename)
        {
            stream = new StreamWriter(filename);
        }

        /// <summary>
        /// Closes the output stream.
        /// </summary>
        public void Dispose()
        {
            stream.Close();
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

        internal void Save(string label, float value, bool newLine = true)
        {
            InternalSave(stream, label, value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture), newLine);
        }

        internal void Save(string label, string value, bool newLine = true)
        {
            InternalSave(stream, label, "\"" + Escape(value) + "\"", newLine);
        }

        internal void Save(string label, bool value, bool newLine = true)
        {
            InternalSave(stream, label, value.ToString(), newLine);
        }

        internal void Save(string label, DataPath path)
        {
            SaveLabel(stream, label);
            NiceLabelValueSeparator();

            BeginGroup(stream);
            Save(RootLabel, path.Root.ToString(), newLine: false);
            Space(stream);
            Save(RelativePathLabel, path.RelativePath, newLine: false);
            Space(stream);
            Save(AbsolutePathLabel, path.AbsolutePath, newLine: false);
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
