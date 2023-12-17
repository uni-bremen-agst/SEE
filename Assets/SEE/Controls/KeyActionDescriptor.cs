using UnityEngine;

namespace SEE.Controls
{
    internal static partial class KeyBindings
    {
        /// <summary>
        /// A descriptor of a user action that can be triggered by a key on the keyboard.
        /// </summary>
        /// <param name="Name">The descriptive name of the action. It will be shown to the user. It should be
        /// short and descriptive.</param>
        /// <param name="Description">A longer description of the action shown to the user. It provides more detail
        /// than <see cref="Name"/>.</param>
        /// <param name="Category">The scope of the action.</param>
        /// <param name="KeyCode">The key on the keyboard this action is bound to. If that key is pressed,
        /// the user wants to trigger this action.</param>
        internal record KeyActionDescriptor(string Name, string Description, KeyActionCategory Category, KeyCode KeyCode)
        {
            /// <summary>
            /// The key on the keyboard this action is bound to. If that key is pressed,
            /// the user wants to trigger this action.
            /// </summary>
            public KeyCode KeyCode { get; set; }
        }
    }
}
