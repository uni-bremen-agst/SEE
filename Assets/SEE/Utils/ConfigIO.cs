using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Abstract super class of input/output of configuration attributes.
    /// </summary>
    public abstract class ConfigIO
    {
        public interface PersistentConfigItem
        {
            /// <summary>
            /// Saves the configuration attributes by way of the given <paramref name="writer"/>
            /// with the given <paramref name="label"/>.
            /// </summary>
            /// <param name="writer">the configuration writer to be used for the output</param>
            /// <param name="label">the label to be emitted in front of the configuration attributes</param>
            void Save(ConfigWriter writer, string label = "");
            /// <summary>
            /// Restores the attributes of this instance from <paramref name="attributes"/> as follows:
            /// If label is neither empty nor null, <paramref name="attributes"/>[<paramref name="label"/>]
            /// is looked up. If it does not exist, nothing else happens and false is returned. If it
            /// exists, the data available in <paramref name="attributes"/>[<paramref name="label"/>] will
            /// be used to restore the attributes of this instance. If at least one such attribute was
            /// restored, true is returned; otherwise false is returned.
            /// If the label is empty or null, <paramref name="attributes"/> direclty is assumed to hold the
            /// data to restore the attributes of this instance.
            /// </summary>
            /// <param name="attributes">if <paramref name="label"/> is null or empty,  holds the data
            /// for restoring the attributes; otherwise <paramref name="attributes"/>[<paramref name="label"/>]
            /// is assumed to hold the necessary data</param>
            /// <param name="label">the label for the lookup of the data to restore the attributes,
            /// or null or empty</param>
            /// <returns>true if at least one attribute was successfully restored</returns>
            bool Restore(Dictionary<string, object> attributes, string label = "");
        }

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
        /// Label for the red part of a color.
        /// </summary>
        protected const string RedLabel = "Red";
        /// <summary>
        /// Label for the green part of a color.
        /// </summary>
        protected const string GreenLabel = "Green";
        /// <summary>
        /// Label for the blue part of a color.
        /// </summary>
        protected const string BlueLabel = "Blue";
        /// <summary>
        /// Label for the alpha part (transparency) of a color.
        /// </summary>
        protected const string AlphaLabel = "Alpha";

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
        /// <param name="attributes">where to look up the <paramref name="label"/></param>
        /// <param name="label">the label to look up</param>
        /// <param name="value">the value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists</param>
        /// <returns>true if the <paramref name="label"/> was found</returns>
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
        /// <param name="attributes">where to look up the <paramref name="label"/></param>
        /// <param name="label">the label to look up</param>
        /// <param name="value">the value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists</param>
        /// <returns>true if the <paramref name="label"/> was found</returns>
        public static bool RestoreList<T>(Dictionary<string, object> attributes, string label, ref IList<T> value) where T : PersistentConfigItem, new()
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
                        T t = new T();
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
        /// <param name="attributes">where to look up the <paramref name="label"/></param>
        /// <param name="label">the label to look up</param>
        /// <param name="value">the value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists</param>
        /// <returns>true if the <paramref name="label"/> was found</returns>
        internal static bool Restore(Dictionary<string, object> values, string label, ref int value)
        {
            long v = value;
            bool result = Restore(values, label, ref v);
            value = (int)v;
            return result;
        }

        /// <summary>
        /// Looks up the <paramref name="value"/> in <paramref name="attributes"/> using the
        /// key <paramref name="label"/>. If no such <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up value. Note that only those parts of the color (red, green, blue,
        /// alpha) will be updated in <paramref name="value"/> that are actually found in <paramref name="attributes"/>;
        /// all others remain unchanged.
        ///
        /// Note: This method is intended specifically for Color. For enums use <see cref="RestoreEnum()"/>
        /// and for all other types, use <see cref="Restore{T}()"/> instead.
        /// </summary>
        /// <param name="attributes">where to look up the <paramref name="label"/></param>
        /// <param name="label">the label to look up</param>
        /// <param name="value">the value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists</param>
        /// <returns>true if the <paramref name="label"/> was found</returns>
        internal static bool Restore(Dictionary<string, object> attributes, string label, ref Color value)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;
                if (values == null)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: Dictionary<string, float>. Actual type: {dictionary.GetType()}");
                }
                if (values.TryGetValue(RedLabel, out object red))
                {
                    value.r = (float)red;
                }
                if (values.TryGetValue(GreenLabel, out object green))
                {
                    value.g = (float)green;
                }
                if (values.TryGetValue(BlueLabel, out object blue))
                {
                    value.b = (float)blue;
                }
                if (values.TryGetValue(AlphaLabel, out object alpha))
                {
                    value.a = (float)alpha;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool Restore(Dictionary<string, object> attributes, string label, ref HashSet<string> value)
        {
            if (attributes.TryGetValue(label, out object storedValue))
            {
                List<object> values = storedValue as List<object>;
                if (values == null)
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
        /// <param name="attributes">where to look up the <paramref name="label"/></param>
        /// <param name="label">the label to look up</param>
        /// <param name="value">the value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists</param>
        /// <returns>true if the <paramref name="label"/> was found</returns>
        internal static bool RestoreStringList(Dictionary<string, object> attributes, string label, ref IList<string> value)
        {
            if (attributes.TryGetValue(label, out object storedValue))
            {
                List<object> values = storedValue as List<object>;
                if (values == null)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: List<string>. Actual type: {storedValue.GetType()}");
                }
                else
                {
                    value = new List<string>();
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

        internal static bool Restore(Dictionary<string, object> attributes, string label, ref Dictionary<string, bool> value)
        {
            if (attributes.TryGetValue(label, out object list))
            {
                // The original dictionary was flattened as a list of pairs where each
                // pair is represented as a list of two elements: the first one is the key
                // and the second one is the value of the original dictionary.
                List<object> values = list as List<object>;
                if (values == null)
                {
                    throw new InvalidCastException($"Types are not assignment compatible for attribute {label}. Expected type: Dictionary<string, bool>. Actual type: {list.GetType()}");
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
                            catch(InvalidCastException e)
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
        /// <param name="attributes">where to look up the <paramref name="label"/></param>
        /// <param name="label">the label to look up</param>
        /// <param name="value">the value of the looked up <paramref name="label"/> if the <paramref name="label"/>
        /// exists</param>
        /// <returns>true if the <paramref name="label"/> was found</returns>
        public static bool RestoreEnum<E>(Dictionary<string, object> attributes, string label, ref E value) where E : struct, IConvertible
        {
            if (!typeof(E).IsEnum)
            {
                throw new ArgumentException("Generic type parameter E must be an enumerated type");
            }
            else
            {
                // enum values are stored as string
                string stringValue = "";
                if (Restore<string>(attributes, label, ref stringValue))
                {
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        throw new Exception("Enum value must neither be null nor the empty string.");
                    }
                    else if (Enum.TryParse<E>(stringValue, out E enumValue))
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
        }

        public static bool RestoreEnumDict<E>(Dictionary<string, object> attributes, string label, ref Dictionary<string, E> value) where E : struct, IConvertible
        {
            // Dictionaries with enums as values are stored by ConfigWriter.Save<K,V>() as a list of
            //  pairs where a pair is a list with two elements: one for the key and one for the value.
            /// Both are stored as strings.
            if (!typeof(E).IsEnum)
            {
                throw new ArgumentException("Generic type parameter E must be an enumerated type");
            }
            else
            {
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
                                if (Enum.TryParse<E>((string)pair[1], out E enumValue))
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
}
