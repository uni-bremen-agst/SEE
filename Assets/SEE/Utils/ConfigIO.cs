using SEE.Game;
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

        public static void RestoreEnum<E>(Dictionary<string, object> dict, string label, ref E value) where E : struct, IConvertible
        {
            if (!typeof(E).IsEnum)
            {
                throw new ArgumentException("Generic type parameter E must be an enumerated type");
            }
            // enum values are stored as string
            string stringValue = "";
            Restore<string>(dict, label, ref stringValue);
            if (string.IsNullOrEmpty(stringValue))
            {
                throw new Exception("Enum value must neither be null nor the empty string.");
            }
            if (Enum.TryParse<E>(stringValue, out E enumValue))
            {
                value = enumValue;
            }
        }
    }
}
