using System;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Groups fields/settings for the <see cref="RuntimeTabMenu"/> into a tab.
    /// This annotation can be used as follows, for instance:
    ///      [TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
    ///      public FilePath ConfigurationPath = new();
    /// </summary>
    public class RuntimeTabAttribute : Attribute
    {
        /// <summary>
        /// Group name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="name">Group name.</param>
        public RuntimeTabAttribute(string name)
        {
            Name = name;
        }
    }
}
