namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Disables the setting if the condition is true.
    /// This annotation can be used as follows, for instance:
    ///      [EnableIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap),
    ///       RuntimeEnableIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap)]
    /// or:
    ///     [EnableIf(nameof(ShowProgressBar)),
    ///      RuntimeEnableIf(nameof(ShowProgressBar))]
    /// </summary>
    public class RuntimeDisableIfAttribute : RuntimeIfAttribute
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public RuntimeDisableIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="value">The value.</param>
        public RuntimeDisableIfAttribute(string condition, object value) : base(condition, value)
        {
        }
    }
}
