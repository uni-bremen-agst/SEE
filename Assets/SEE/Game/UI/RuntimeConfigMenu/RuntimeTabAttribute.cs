using System;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Groups fields/settings for the <see cref="RuntimeTabMenu"/> into a tab.
    /// </summary>
    public class RuntimeTabAttribute : Attribute
    {
        /// <summary>
        /// Group name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="name">group name</param>
        public RuntimeTabAttribute(string name)
        {
            Name = name;
        }
    }
}
