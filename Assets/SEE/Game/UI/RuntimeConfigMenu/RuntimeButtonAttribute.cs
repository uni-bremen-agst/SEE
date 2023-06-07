using System;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Marks a method as a button for the runtime config menu.
    /// </summary>
    public class RuntimeButtonAttribute : Attribute
    {
        /// <summary>
        /// The label of the button.
        /// </summary>
        public readonly string Label;

        /// <summary>
        /// The displayed name of the button.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="label">label</param>
        public RuntimeButtonAttribute(string name, string label)
        {
            Name = name;
            Label = label;
        }
    }
}
