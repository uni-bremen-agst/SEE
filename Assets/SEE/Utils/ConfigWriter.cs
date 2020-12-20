using System;
using System.IO;

namespace SEE.Utils
{
    /// <summary>
    /// A writer for configuration settings.
    /// </summary>
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
            if (groupNesting != 0)
            {
                 throw new InvalidOperationException("The number of calls to BeginGroup() and Endgroup() do not match.");
            }
        }

        /// <summary>
        /// Emits given <paramref name="label"/> followed by <see cref="NiceLabelValueSeparator"/> 
        /// to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">where to emit</param>
        /// <param name="label">label to emit</param>
        private static void SaveLabel(StreamWriter stream, string label)
        {
            stream.Write(label + NiceLabelValueSeparator());
        }

        /// <summary>
        /// Returns <see cref="LabelSeparator"/> by preceeding and trailing single blank.
        /// </summary>
        /// <returns>" " + <see cref="LabelSeparator"/> + " "</returns>
        private static string NiceLabelValueSeparator()
        {
            return " " + LabelSeparator + " ";
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="label">label to be emitted</param>
        /// <param name="value">value to be emitted</param>
        private void InternalSave(string label, string value)
        {
            Indent(stream, groupNesting * 3);
            SaveLabel(stream, label);
            stream.Write(value + AttributeSeparator);
            stream.WriteLine();
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="label">label to be emitted</param>
        /// <param name="value">value to be emitted</param>
        internal void Save(string label, float value)
        {
            InternalSave(label, value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="label">label to be emitted</param>
        /// <param name="value">value to be emitted</param>
        internal void Save(string label, string value)
        {
            InternalSave(label, "\"" + Escape(value) + "\"");
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="label">label to be emitted</param>
        /// <param name="value">value to be emitted</param>
        internal void Save(string label, bool value)
        {
            InternalSave(label, value.ToString());
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

        /// <summary>
        /// The nesting of composite data. Whenever <see cref="BeginGroup(string)"/> is called,
        /// the nesting is increased by one. Whenever <see cref="EndGroup"/> is called, it is
        /// decreased by one. Its value will be used in <see cref="Indent"/> to determine the
        /// indentation of an emitted line.
        /// </summary>
        private int groupNesting = 0;

        /// <summary>
        /// Signals the begin of a composite data structure, which will be emitted as nested
        /// composite value. Every other call to any of the <see cref="Save"/> methods after that
        /// is assumed to deliver a value belonging to this group -- until <see cref="EndGroup"/>
        /// is called eventually.
        /// </summary>
        /// <param name="label">the label of the composite value</param>
        public void BeginGroup(string label)
        {
            SaveLabel(stream, label);
            stream.Write(Open);
            stream.WriteLine();
            groupNesting++;
        }

        /// <summary>
        /// Signals the end of a composite data structure.
        /// </summary>
        /// <exception cref="InvalidOperationException">thrown if the number of calls <see cref="BeginGroup(string)"/>
        /// is lower than the number of calls to <see cref="EndGroup"/></exception>
        public void EndGroup()
        {
            if (groupNesting <= 0)
            {
                throw new InvalidOperationException("Call to Endgroup() where nesting level is zero.");
            }
            else
            {
                stream.Write(Close);
                stream.WriteLine(AttributeSeparator);
                groupNesting--;
            }
        }

        /// <summary>
        /// Emits <paramref name="howMany"/> blanks to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">where to emit the blanks</param>
        /// <param name="howMany">how many blanks to emit</param>
        private static void Indent(StreamWriter stream, int howMany)
        {
            for (int i = 1; i <= howMany; i++)
            {
                stream.Write(" ");
            }
        }
    }
}
