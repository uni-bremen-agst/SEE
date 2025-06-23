namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Show if setting for the runtime element.
    /// This annotation can be used as follows, for instance:
    ///      [ShowIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap),
    ///      RuntimeShowIf(nameof(Kind), NodeLayoutKind.IncrementalTreeMap)]
    ///      public IncrementalTreeMapAttributes IncrementalTreeMap = new();
    /// or:
    ///     [ShowIf(nameof(ShowProgressBar)), RuntimeShowIf(nameof(ShowProgressBar))]
    ///     public float ProgressBar;
    /// </summary>
    public class RuntimeShowIfAttribute : RuntimeIfAttribute
    {
        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        public RuntimeShowIfAttribute(string condition) : base(condition)
        {
        }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="condition">the condition</param>
        /// <param name="value">the value.</param>
        public RuntimeShowIfAttribute(string condition, object value) : base(condition, value)
        {
        }
    }
}
