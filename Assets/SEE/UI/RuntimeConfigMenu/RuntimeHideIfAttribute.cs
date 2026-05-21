namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Hide setting for the runtime element if the condition is true.
    /// This annotation can be used as follows, for instance:
    ///      [HideIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap),
    ///      RuntimeHideIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap)]
    /// or:
    ///     [HideIf(nameof(ShowProgressBar)), RuntimeHideIf(nameof(ShowProgressBar))]
    /// </summary>
    public class RuntimeHideIfAttribute : RuntimeIfAttribute
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public RuntimeHideIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="value">The value.</param>
        public RuntimeHideIfAttribute(string condition, object value) : base(condition, value)
        {
        }
    }
}
