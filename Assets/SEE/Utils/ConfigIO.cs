using System;
using System.Collections.Generic;

namespace SEE.Utils
{
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
        protected const char Open = '{';
        /// <summary>
        /// The closing token for a composite attribute value.
        /// </summary>
        protected const char Close = '}';

        /// <summary>
        /// Looks up the <paramref name="value"/> in <paramref name="attributes"/> using the 
        /// key <paramref name="label"/>. If no such <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up value.
        /// 
        /// Note: for types <typeparamref name="T"/> that are enums, use <see cref="RestoreEnum()"/>
        /// instead.
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
                catch (InvalidCastException e)
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
        /// Looks up the enum <paramref name="value"/> in <paramref name="attributes"/> using the 
        /// key <paramref name="label"/>. If no such enum <paramref name="label"/> exists, false
        /// is returned and <paramref name="value"/> remains unchanged. Otherwise <paramref name="value"/>
        /// receives the looked up enum value.
        /// 
        /// Note: this method is intended for enums <typeparamref name="E"/>; for other types, use <see cref="Restore()"/>
        /// instead.
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
    }
}
