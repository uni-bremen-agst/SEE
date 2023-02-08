using System;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{

    /// <summary>
    /// The type of a state-based action that can be executed. In terms
    /// of the PlayerMenu it is a leaf of the menu.
    /// </summary>
    public class ActionStateType : AbstractActionStateType
    {
        /// <summary>
        /// Delegate to be called to create a new instance of this kind of action.
        /// May be null if none needs to be created (in which case this delegate will
        /// not be called).
        /// </summary>
        public CreateReversibleAction CreateReversible { get; }

        /// <summary>
        /// Constructor for ActionStateType.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to private.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="parent">The parent of this action in the nesting hierarchy in the menu.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="createReversible">The delegate to be called when the action has finished
        /// and a new instance needs to be created to continue.</param>
        /// <param name="parent">The parent of this action state type. Can be <c>null</c>, in
        /// which case this action type is considered a root at the top level of the PlayerMenu.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> is not unique.
        /// </exception>
        /// <remarks>Calling this constructor will add this action type to
        /// <see cref="ActionStateTypes.AllRootTypes"/>.</remarks>
        public ActionStateType(string name, string description, Color color, string iconPath,
                               CreateReversibleAction createReversible,
                               ActionStateTypeGroup parent = null)
            : base(name, description, color, iconPath, parent)
        {
            CreateReversible = createReversible;
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

            return obj.GetType() == GetType() && ((ActionStateType)obj).CreateReversible == CreateReversible;
        }

        public override int GetHashCode()
        {
            // TODO: Is this re-definition actually needed?
            return CreateReversible.GetHashCode();
        }

        #endregion
    }
}
