using SEE.Utils;
using System;
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
        private ActionStateTypeGroup(string name, string description, Color color, string iconPath)
            : base(name, description, color, iconPath)
        {
        }

        public static ActionStateTypeGroup Hide { get; }
           = new ActionStateTypeGroup("Hide", "Hides nodes or edges",
                                      Color.yellow.Darker(), "Materials/ModernUIPack/Eye");

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
            actionStateType.Parent = this;
            Children.Add(actionStateType);
        }

        #region Equality & Comparators

        public override bool Equals(object obj)
        {
            // FIXME
            throw new NotImplementedException();

            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            // return obj.GetType() == GetType() && ((ActionStateType)obj).CreateReversible == CreateReversible;
        }

        public override int GetHashCode()
        {
            // FIXME
            throw new NotImplementedException();
            //return CreateReversible.GetHashCode();
        }

        #endregion
    }
}
