namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Enables the setting if the condition is true.
    /// This annotation can be used as follows, for instance:
    ///      [EnableIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap),
    ///       RuntimeEnableIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap)]
    /// or:
    ///     [EnableIf(nameof(ShowProgressBar)),
    ///     RuntimeEnableIf(nameof(ShowProgressBar))]
    /// </summary>
    public class RuntimeEnableIfAttribute : RuntimeIfAttribute
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        public RuntimeEnableIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="value">the value.</param>
        public RuntimeEnableIfAttribute(string condition, object value) : base(condition, value)
        {
        }
    }
}
