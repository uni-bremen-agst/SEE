using System;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Enables the setting if the condition is true.
    /// This annotation can be used as follows, for instance:
    ///      [EnableIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap), RuntimeEnableIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap)]
    /// or:
    ///     [EnableIf(nameof(ShowProgressBar)), RuntimeEnableIf(nameof(ShowProgressBar))]
    /// </summary>
    public class RuntimeEnableIfAttribute : Attribute
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
        public RuntimeEnableIfAttribute(string condition)
        {
            Condition = condition;
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="value">the value.</param>
        public RuntimeEnableIfAttribute(string condition, object value)
        {
            Condition = condition;
            Value = value;
        }
    }
}
