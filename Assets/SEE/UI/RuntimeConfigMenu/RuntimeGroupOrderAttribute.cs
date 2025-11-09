using System;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Specifies the display order of a field within a group in the <see cref="RuntimeTabMenu"/>.
    /// This annotation can be used as follows, for instance:
    ///      [RuntimeGroupOrder(GroupOrder)]
    ///      public FilePath ConfigurationPath = new();
    /// </summary>
    public class RuntimeGroupOrderAttribute : Attribute
    {
        /// <summary>
        /// The order of the field within the group.
        /// </summary>
        public readonly float Order;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="order">The position of the field within the group.</param>
        public RuntimeGroupOrderAttribute(float order)
        {
            Order = order;
        }
    }
}
