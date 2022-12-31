using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
            if (context.Count > 0)
            {
                if (context.Peek() == ContextInfo.InComposite)
                {
                    throw new InvalidOperationException("The number of calls to BeginGroup() and EndGroup() do not match.");
                }
                else if (context.Peek() == ContextInfo.InList)
                {
                    throw new InvalidOperationException("The number of calls to BeginList() and EndList() do not match.");
                }
                else
                {
                    throw new InvalidOperationException("Unbalanced scopes.");
                }
            }
        }

        private enum ContextInfo
        {
            InComposite, // if we are about to emit the values of a composite data structure
            InList,      // if we are about to emit the values of a list
        }

        /// <summary>
        /// The context stack. Whenever we enter a composite group, an InComposite is pushed.
        /// Whenever we enter a list, an InList ist pushed. Upon leaving a composite or list
        /// value, the context stack is popped.
        /// </summary>
        private readonly Stack<ContextInfo> context = new Stack<ContextInfo>();

        /// <summary>
        /// Emits given <paramref name="label"/> followed by <see cref="NiceLabelValueSeparator"/>
        /// to <paramref name="stream"/> preceeded by indentation if label is neither null nor empty.
        /// If it null or empty, nothing but the indentation will be emitted.
        /// </summary>
        /// <param name="stream">where to emit</param>
        /// <param name="label">label to emit</param>
        private void SaveLabel(StreamWriter stream, string label)
        {
            Indent();
            if (!string.IsNullOrEmpty(label))
            {
                stream.Write(label + NiceLabelValueSeparator());
            }
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
        /// The label will be omitted if we write the values of a list. The value will be followed
        /// by a <see cref="AttributeSeparator"/>.
        /// </summary>
        /// <param name="label">label to be emitted</param>
        /// <param name="value">value to be emitted</param>
        private void InternalSave(string label, string value)
        {
            if (context.Count == 0 || context.Peek() != ContextInfo.InList)
            {
                SaveLabel(stream, label);
            }
            else
            {
                Indent();
            }
            stream.Write(value + AttributeSeparator);
            stream.WriteLine();
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="value">value to be emitted</param>
        /// <param name="label">label to be emitted</param>
        internal void Save(float value, string label = "")
        {
            InternalSave(label, value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="value">value to be emitted</param>
        /// <param name="label">label to be emitted</param>
        internal void Save(int value, string label = "")
        {
            InternalSave(label, value.ToString());
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="value">value to be emitted</param>
        /// <param name="label">label to be emitted</param>
        internal void Save(string value, string label = "")
        {
            InternalSave(label, "\"" + Escape(value) + "\"");
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>.
        /// </summary>
        /// <param name="value">value to be emitted</param>
        /// <param name="label">label to be emitted</param>
        internal void Save(bool value, string label = "")
        {
            InternalSave(label, value.ToString());
        }

        /// <summary>
        /// Saves the given <paramref name="collection"/> as a list of strings.
        /// </summary>
        /// <param name="collection">items to be saved</param>
        /// <param name="label">label to be emitted</param>
        internal void Save(ICollection<string> collection, string label = "")
        {
            BeginList(label);
            foreach (string item in collection)
            {
                Save(item);
            }
            EndList();
        }

        /// <summary>
        /// Saves the given <paramref name="collection"/> as a list.
        ///
        /// Note: If either key or value should better not be saved as a string, you must use
        /// a more specific save operation instead.
        /// </summary>
        /// <typeparam name="K">type of the key of <paramref name="collection"/></typeparam>
        /// <typeparam name="V">type of the value of <paramref name="collection"/></typeparam>
        /// <param name="collection">the dictionary to be saved</param>
        /// <param name="label">the label to be added in front of the value</param>
        internal void Save<T>(ICollection<T> collection, string label = "") where T : PersistentConfigItem
        {
            BeginList(label);
            foreach (var item in collection)
            {
                item.Save(this);
            }
            EndList();
        }

        /// <summary>
        /// Saves the given <paramref name="dictionary"/> as a list of pairs where
        /// a pair is a list with two elements: one for the key and one for the value.
        /// The key is saved as a string and the value is saved as a plain literal:
        /// either True or False.
        ///
        /// Note: This method is more specific than <see cref="SaveAsStrings{K, V}(Dictionary{K, V}, string)"/>
        /// in that the key is not saved as a string but as a boolean literal (no quotes around it).
        /// <param name="dictionary">the dictionary to be saved</param>
        /// <param name="label">the label to be added in front of the value</param>
        internal void Save(Dictionary<string, bool> dictionary, string label = "")
        {
            BeginList(label);
            foreach (var item in dictionary)
            {
                BeginList();
                Save(item.Key);
                Save(item.Value);
                EndList();
            }
            EndList();
        }

        /// <summary>
        /// Saves the given <paramref name="dictionary"/> as a list of pairs where
        /// a pair is a list with two elements: one for the key and one for the value.
        /// Both are stored as strings.
        ///
        /// Note: If either key or value should better not be saved as a string, you must use
        /// a more specific save operation instead.
        /// </summary>
        /// <typeparam name="K">type of the key of <paramref name="dictionary"/></typeparam>
        /// <typeparam name="V">type of the value of <paramref name="dictionary"/></typeparam>
        /// <param name="dictionary">the dictionary to be saved</param>
        /// <param name="label">the label to be added in front of the value</param>
        internal void SaveAsStrings<K,V>(Dictionary<K,V> dictionary, string label = "")
        {
            BeginList(label);
            foreach (var item in dictionary)
            {
                BeginList();
                Save(item.Key.ToString());
                Save(item.Value.ToString());
                EndList();
            }
            EndList();
        }

        /// <summary>
        /// Writes <paramref name="label"/> and its <paramref name="value"/> to <see cref="stream"/>
        /// as a composite value of its constituents (Red, Green, Blue, Alpha).
        /// </summary>
        /// <param name="label">label to be emitted</param>
        /// <param name="value">value to be emitted</param>
        internal void Save(Color color, string label = "")
        {
            BeginGroup(label);
            Save(color.r, RedLabel);
            Save(color.g, GreenLabel);
            Save(color.b, BlueLabel);
            Save(color.a, AlphaLabel);
            EndGroup();
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
        /// Signals the begin of a composite data structure, which will be emitted as nested
        /// composite value. Every other call to any of the <see cref="Save"/> methods after that
        /// is assumed to deliver a value belonging to this group -- until <see cref="EndGroup"/>
        /// is called eventually.
        /// </summary>
        /// <param name="label">the label of the composite value</param>
        public void BeginGroup(string label = "")
        {
            SaveLabel(stream, label);
            context.Push(ContextInfo.InComposite);
            stream.WriteLine(OpenGroup);
        }

        /// <summary>
        /// Signals the end of a composite data structure.
        /// </summary>
        /// <exception cref="InvalidOperationException">thrown if the number of calls <see cref="BeginGroup(string)"/>
        /// is lower than the number of calls to <see cref="EndGroup"/></exception>
        public void EndGroup()
        {
            if (context.Count > 0 && context.Peek() != ContextInfo.InComposite)
            {
                throw new InvalidOperationException("Unexpected call to EndGroup().");
            }
            else
            {
                context.Pop();
                Indent();
                stream.Write(CloseGroup);
                stream.WriteLine(AttributeSeparator);
            }
        }

        /// <summary>
        /// Signals the begin of a list data structure, which will be emitted as a
        /// sequence of values (without individual label). Every other call to any of the
        /// <see cref="SaveListItem"/> methods after that is assumed to deliver a value
        /// belonging to this list -- until <see cref="EndList"/> is called eventually.
        /// </summary>
        /// <param name="label">the label of the list value</param>
        public void BeginList(string label = "")
        {
            SaveLabel(stream, label);
            context.Push(ContextInfo.InList);
            stream.WriteLine(OpenList);
        }

        /// <summary>
        /// Signals the end of a list value.
        /// </summary>
        /// <exception cref="InvalidOperationException">thrown if the number of calls <see cref="BeginList(string)"/>
        /// is lower than the number of calls to <see cref="EndList"/></exception>
        public void EndList()
        {
            if (context.Count > 0 && context.Peek() != ContextInfo.InList)
            {
                throw new InvalidOperationException("Unexpected call to EndList().");
            }
            else
            {
                context.Pop();
                Indent();
                stream.Write(CloseList);
                stream.WriteLine(AttributeSeparator);
            }
        }

        /// <summary>
        /// Emits 3 blanks per element of <see cref="context"/> to <see cref="stream"/>.
        /// </summary>
        private void Indent()
        {
            int indentation = context.Count * 3;
            for (int i = 1; i <= indentation; i++)
            {
                stream.Write(" ");
            }
        }
    }
}
