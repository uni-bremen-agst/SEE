namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// Metadata for a user action defined in the Input System asset.
    /// </summary>
    internal class KeyActionDescriptor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Descriptive name of the action shown to the user.</param>
        /// <param name="description">Longer description of the action shown to the user.</param>
        /// <param name="category">Category of the action.</param>
        public KeyActionDescriptor(string name, string description, KeyActionCategory category)
        {
            BindingPath = null;
            Name = name;
            Category = category;
            Description = description;
        }

        /// <summary>
        /// The descriptive name of the action. It will be shown to the user. It should be
        /// short and descriptive.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// A longer description of the action shown to the user. It provides more detail
        /// than <see cref="Name"/>.
        /// </summary>
        public readonly string Description;
        /// <summary>
        /// The scope of the action.
        /// </summary>
        public readonly KeyActionCategory Category;
        /// <summary>
        /// Input System control path currently assigned to this action.
        /// </summary>
        public string BindingPath;

        /// <summary>
        /// Returns this <see cref="KeyActionDescriptor"/> in a human-readable form.
        /// </summary>
        /// <returns>This <see cref="KeyActionDescriptor"/> as a meaningful string.</returns>
        public override string ToString()
        {
            return $"<BindingPath={BindingPath}, Name='{Name}', Category={Category}, Description='{Description}'>";
        }
    }
}
