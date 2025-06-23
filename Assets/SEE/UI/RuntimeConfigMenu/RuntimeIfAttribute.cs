using System;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Common base class for runtime configuration attributes that
    /// depend on a condition.
    /// </summary>
    public abstract class RuntimeIfAttribute : Attribute
    {
        /// <summary>
        /// Condition.
        /// </summary>
        public readonly string Condition;

        /// <summary>
        /// The value.
        /// </summary>
        public readonly object Value;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        public RuntimeIfAttribute(string condition)
        {
            Condition = condition;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="value">the value.</param>
        public RuntimeIfAttribute(string condition, object value)
        {
            Condition = condition;
            Value = value;
        }
    }
}
