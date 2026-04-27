using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// A group of other <see cref="AbstractActionStateType"/>s. It is not itself executable but
    /// just serves as a container for <see cref="AbstractActionStateType"/>s. In terms of menu,
    /// it represents a submenu.
    /// </summary>
    public class ActionStateTypeGroup : AbstractActionStateType
    {
        /// <summary>
        /// Constructor for <see cref="ActionStateTypeGroup"/>.
        /// </summary>
        /// <param name="name">The name of this <see cref="ActionStateTypeGroup"/>. Must be unique.</param>
        /// <param name="description">Description for this <see cref="ActionStateTypeGroup"/>.</param>
        /// <param name="parent">The parent of this action in the nesting hierarchy in the menu.</param>
        /// <param name="color">Color for this <see cref="ActionStateTypeGroup"/>.</param>
        /// <param name="icon">Icon for this <see cref="ActionStateTypeGroup"/>, given as a FontAwesome codepoint.</param>
        /// <param name="register">If true, this action state type will be registered in <see cref="ActionStateTypes"/>.</param>
        public ActionStateTypeGroup
            (string name, string description, Color color, char icon, ActionStateTypeGroup parent = null, bool register = true)
            : base(name, description, color, icon, parent, register)
        {
        }

        /// <summary>
        /// Ordered list of child action state types.
        /// </summary>
        public IList<AbstractActionStateType> Children = new List<AbstractActionStateType>();

        /// <summary>
        /// Adds <paramref name="actionStateType"/> as a member of this group.
        /// </summary>
        /// <param name="actionStateType">Child to be added.</param>
        public void Add(AbstractActionStateType actionStateType)
        {
            actionStateType.Parent = this;
            Children.Add(actionStateType);
        }
    }
}
