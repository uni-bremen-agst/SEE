using UnityEngine;

namespace SEE.Controls.KeyActions
{
    /// <summary>
    /// A descriptor of a user action that can be triggered by a key on the keyboard.
    /// </summary>
    internal class KeyActionDescriptor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Descriptive name of the action shown to the user.</param>
        /// <param name="description">Longer description of the action shown to the user.</param>
        /// <param name="category">Category of the action.</param>
        /// <param name="keyCode">Key this action is bound to.</param>
        public KeyActionDescriptor(string name, string description, KeyActionCategory category, KeyCode keyCode)
        {
            KeyCode = keyCode;
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
        /// <summary>
        /// The scope of the action.
        /// </summary>
        public readonly KeyActionCategory Category;
        /// The key on the keyboard this action is bound to. If that key is pressed,
        /// the user wants to trigger this action.
        /// </summary>
        public KeyCode KeyCode;

        /// <summary>
        /// Returns this <see cref="KeyActionDescriptor"/> in a human-readable form.
        /// </summary>
        /// <returns>This <see cref="KeyActionDescriptor"/> as a meaningful string.</returns>
        public override string ToString()
        {
            return $"<KeyCode={KeyCode}, Name='{Name}', Category={Category}, Description='{Description}'>";
        }
    }
}
