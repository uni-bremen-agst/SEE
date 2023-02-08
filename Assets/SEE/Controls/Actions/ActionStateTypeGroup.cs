using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// A group of other <see cref="AbstractActionStateType"/>s. It is not itself
    /// executable but just serves as a container for nested
    /// <see cref="AbstractActionStateType"/>s. In terms of menu, it represents
    /// a submenu.
    /// </summary>
    public class ActionStateTypeGroup : AbstractActionStateType
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="parent">The parent of this action state type. Can be <c>null</c>, in
        /// which case this action type is considered a root at the top level of the PlayerMenu.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> is not unique.
        /// </exception>
        /// <remarks>Calling this constructor will add this action type to
        /// <see cref="ActionStateTypes.AllRootTypes"/>.</remarks>
        public ActionStateTypeGroup(string name, string description, Color color, string iconPath,
                                    ActionStateTypeGroup parent = null)
            : base(name, description, color, iconPath, parent)
        {
        }

        /// <summary>
        /// Ordered list of child action state types.
        /// </summary>
        public IList<AbstractActionStateType> Children = new List<AbstractActionStateType>();

        /// <summary>
        /// Adds <paramref name="actionStateType"/> as a member of this group.
        /// </summary>
        /// <param name="actionStateType">child to be added</param>
        public void Add(AbstractActionStateType actionStateType)
        {
            Children.Add(actionStateType);
        }

        #region Equality & Comparators

        public override bool Equals(object obj)
        {
            // TODO: Is this re-definition actually needed?
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }
            ActionStateTypeGroup other = (ActionStateTypeGroup)obj;
            if (Children.Count != other.Children.Count)
            {
                return false;
            }
            for (int i = 0; i < Children.Count; i++)
            {
                if (!Children[i].Equals(other.Children[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            // TODO: Is this re-definition actually needed?
            int result = Name.GetHashCode() + Description.GetHashCode();
            foreach (AbstractActionStateType child in Children)
            {
                result += child.GetHashCode();
            }
            return result;
        }
        #endregion
    }
}
