using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Graphs.Config
{
    /// <summary>
    /// Abstract super class of input/output of configuration attributes.
    /// </summary>
    public abstract class ConfigIO
    {
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
        protected const char OpenGroup = '{';
        /// <summary>
        /// The closing token for a composite attribute value.
        /// </summary>
        protected const char CloseGroup = '}';
        /// <summary>
        /// The opening token for a list attribute value.
        /// </summary>
        protected const char OpenList = '[';
        /// <summary>
        /// The closing token for a list attribute value.
        /// </summary>
        protected const char CloseList = ']';

        /// <summary>
        /// Looks up the <paramref name="value"/> in <paramref name="attributes"/> using the
        /// key <paramref name="label"/>. If no such <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up value.
        ///
        /// Note: For types <typeparamref name="T"/> that are enums, use <see cref="RestoreEnum()"/>
        /// instead. For Color, use <see cref="Restore(Dictionary{string, object}, string, ref Color)"/>. For int, use
        /// <see cref="Restore(Dictionary{string, object}, string, ref int)"/>.
        /// </summary>
        /// <typeparam name="T">the type of <paramref name="value"/></typeparam>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        public static bool Restore<T>(Dictionary<string, object> attributes, string label, ref T value)
        {
            if (attributes.TryGetValue(label, out object v))
            {
                try
                {
                    value = (T)v;
                    return true;
                }
                catch (InvalidCastException)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: {typeof(T)}. Actual type: {v.GetType()}");
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Looks up the list <paramref name="value"/> in <paramref name="attributes"/> using the
        /// key <paramref name="label"/>. If no such <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// will be cleared and all its elements will be restored from the looked up values.
        /// To restore a single element e, e.Restore(item, "") will be called where 'item' is a single
        /// data element of the list looked up by <paramref name="label"/>.
        ///
        /// Note: For types <typeparamref name="T"/> that are enums, use <see cref="RestoreEnum()"/>
        /// instead. For Color, use <see cref="Restore(Dictionary{string, object}, string, ref Color)"/>. For int, use
        /// <see cref="Restore(Dictionary{string, object}, string, ref int)"/>.
        /// </summary>
        /// <typeparam name="T">the type of elements of the list <paramref name="value"/></typeparam>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        public static bool RestoreList<T>(Dictionary<string, object> attributes, string label, ref IList<T> value) where T : IPersistentConfigItem, new()
        {
            if (attributes.TryGetValue(label, out object v))
            {
                value.Clear();
                try
                {
                    IList items = (IList)v;
                    foreach (object item in items)
                    {
                        Dictionary<string, object> dict = (Dictionary<string, object>)item;
                        T t = new();
                        t.Restore(dict, "");
                        value.Add(t);
                    }
                    return true;
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: IList<{typeof(T)}>. Actual type: {v.GetType()}. Original exception: {e.Message} {e.StackTrace}");
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Looks up the <paramref name="value"/> in <paramref name="attributes"/> using the
        /// key <paramref name="label"/>. If no such <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up value.
        ///
        /// Note: This method is intended for int values. If you would use the generic method
        /// <see cref="Restore{T}(Dictionary{string, object}, string, ref T)"/> instead, you
        /// would run into a conversion error from int64 (long) to int32 (int).
        /// </summary>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        internal static bool Restore(Dictionary<string, object> values, string label, ref int value)
        {
            long v = value;
            bool result = Restore(values, label, ref v);
            value = (int)v;
            return result;
        }

        /// <summary>
        /// Restores <paramref name="value"/> from <paramref name="attributes"/> using the given <paramref name="label"/>.
        ///
        /// If a value can be restored, <paramref name="value"/> will be set to the restored value, that is,
        /// its previous is overridden completely, and true is returned.
        /// </summary>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The restored value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        /// <exception cref="InvalidCastException">In case the looked up value is not the expected type List of string.</exception>
        internal static bool Restore(Dictionary<string, object> attributes, string label, ref HashSet<string> value)
        {
            if (attributes.TryGetValue(label, out object storedValue))
            {
                if (storedValue is not List<object> values)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: List<string>. Actual type: {storedValue.GetType()}");
                }
                else
                {
                    value = new HashSet<string>();
                    foreach (object item in values)
                    {
                        value.Add((string)item);
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Restores a collection of strings retrieved from <paramref name="attributes"/> under
        /// the given <paramref name="label"/>.
        /// </summary>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        internal static bool RestoreStringList(Dictionary<string, object> attributes, string label, ref IList<string> value)
        {
            if (attributes.TryGetValue(label, out object storedValue))
            {
                List<object> values = (List<object>)storedValue;
                value = values.Cast<string>().ToList();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Restores a dictionary with strings as keys and boolean as value under the
        /// the given <paramref name="label"/>.
        /// </summary>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        /// <exception cref="InvalidCastException">Thrown if the looked up value is not
        /// of the expected data type.</exception>
        /// <exception cref="Exception">Thrown if the items stored do not form a pair.</exception>
        internal static bool Restore(Dictionary<string, object> attributes, string label, ref Dictionary<string, bool> value)
        {
            if (attributes.TryGetValue(label, out object list))
            {
                // The original dictionary was flattened as a list of pairs where each
                // pair is represented as a list of two elements: the first one is the key
                // and the second one is the value of the original dictionary.
                if (list is not List<object> values)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}."
                        + " Expected type: Dictionary<string, bool>. Actual type: {list.GetType()}");
                }
                else
                {
                    value = new Dictionary<string, bool>();
                    foreach (var item in values)
                    {
                        List<object> pair = item as List<object>;
                        if (pair.Count == 2)
                        {
                            try
                            {
                                value[(string)pair[0]] = (bool)pair[1];
                            }
                            catch (InvalidCastException e)
                            {
                                object val = pair[1];
                                throw new InvalidCastException($"Value to be cast {val} is expected to be a boolean. Actual type is {val.GetType().Name}: {e.Message}");
                            }
                        }
                        else
                        {
                            throw new Exception("Pair expected.");
                        }
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Looks up the enum <paramref name="value"/> in <paramref name="attributes"/> using the
        /// key <paramref name="label"/>. If no such enum <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up enum value.
        ///
        /// Note: This method is intended for enums <typeparamref name="E"/>; for other types, use <see cref="Restore()"/>
        /// instead. For Color, use <see cref="RestoreColor()"/>.
        /// </summary>
        /// <typeparam name="E">the enum type of <paramref name="value"/></typeparam>
        /// <param name="attributes">Where to look up the <paramref name="label"/>.</param>
        /// <param name="label">The label to look up.</param>
        /// <param name="value">The value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists.</param>
        /// <returns>True if the <paramref name="label"/> was found.</returns>
        public static bool RestoreEnum<E>(Dictionary<string, object> attributes, string label, ref E value) where E : struct, Enum
        {
            // enum values are stored as string
            string stringValue = "";
            if (Restore(attributes, label, ref stringValue))
            {
                if (string.IsNullOrEmpty(stringValue))
                {
                    throw new Exception("Enum value must neither be null nor the empty string.");
                }
                else if (Enum.TryParse(stringValue, out E enumValue))
                {
                    value = enumValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool RestoreEnumDict<E>(Dictionary<string, object> attributes, string label, ref Dictionary<string, E> value) where E : struct, Enum
        {
            // Dictionaries with enums as values are stored by ConfigWriter.Save<K,V>() as a list of
            //  pairs where a pair is a list with two elements: one for the key and one for the value.
            /// Both are stored as strings.
            if (attributes.TryGetValue(label, out object list))
            {
                // The original dictionary was flattened as a list of pairs where each
                // pair is represented as a list of two elements: the first one is the key
                // and the second one is the value of the original dictionary.
                List<object> values = list as List<object>;
                if (values == null)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: Dictionary<string, {typeof(E)}>. Actual type: {list.GetType()}");
                }
                else
                {
                    value = new Dictionary<string, E>();
                    foreach (var item in values)
                    {
                        List<object> pair = item as List<object>;
                        if (pair.Count == 2)
                        {
                            // value part of pair is expected to be of enum type E
                            if (Enum.TryParse((string)pair[1], out E enumValue))
                            {
                                try
                                {
                                    // key part of pair is expected to be of type string
                                    value[(string)pair[0]] = enumValue;
                                }
                                catch (InvalidCastException)
                                {
                                    object key = pair[0];
                                    throw new InvalidCastException($"Key {key} to be cast is expected to be a string. Actual type is {key.GetType().Name}.");
                                }
                            }
                            else
                            {
                                object val = pair[1];
                                throw new InvalidCastException($"Value to be cast {val} is expected to be a {typeof(E)}. Actual type is {val.GetType().Name}.");
                            }
                        }
                        else
                        {
                            throw new Exception("Pair expected.");
                        }
                    }
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
